using System.Diagnostics;
using System.Runtime.InteropServices;
using Azure.Core;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shahab.Extensions.Configuration.SqlServerConfiguration.Abstractions;
using Shahab.Extensions.Configuration.SqlServerConfiguration.Internals;

namespace Shahab.Extensions.Configuration.SqlServerConfiguration;

public sealed class SqlServerConfigurationProvider : ConfigurationProvider, IDisposable, IConfigurationRefresher
{
    private static readonly TimeSpan MinDelayForUnhandledFailure = TimeSpan.FromSeconds(5.0);
    // private static readonly TimeSpan DefaultMaxSetDirtyDelay = TimeSpan.FromSeconds(30.0);


    private readonly SqlServerConfigurationOptions _options;
    private readonly SqlConnection _sqlConnection;


    private ILogger? _logger;
    private ILoggerFactory _loggerFactory = default!;
    private int _networkOperationsInProgress;
    private bool _isInitialLoadComplete;

    public ILoggerFactory LoggerFactory
    {
        get => this._loggerFactory;
        set
        {
            this._loggerFactory = value;
            this._logger =
                this._loggerFactory.CreateLogger("Shahab.Extensions.Configuration.SqlServerConfiguration.Refresh");
        }
    }

    public SqlServerConfigurationProvider(SqlServerConfigurationOptions options)
    {
        _options = options;
        _sqlConnection = new SqlConnection(_options.ConnectionString);

        _sqlConnection.AccessToken = _options.Token?.GetToken(
            new TokenRequestContext(new[] { "https://database.windows.net/.default" }),
            new CancellationToken()).Token;
    }


    /// <summary>Loads (or reloads) the data for this provider.</summary>
    public override void Load()
    {
        CreateConfigurationTableIfNotExists();

        var stopwatch = Stopwatch.StartNew();
        try
        {
            LoadKeyValueConfigurations().ConfigureAwait(false).GetAwaiter().GetResult();
            foreach (var keyValueWatcher in _options.WatchedSettings)
                keyValueWatcher.CacheExpires = DateTimeOffset.Now.Add(keyValueWatcher.CacheExpirationInterval);
        }
        catch
        {
            var delay = MinDelayForUnhandledFailure.Subtract(stopwatch.Elapsed);
            if (delay.Ticks > 0L)
                Task.Delay(delay).ConfigureAwait(false).GetAwaiter().GetResult();

            if (!_options.Optional)
                throw;
        }
        finally
        {
            ((SqlServerConfigurationRefresher)_options.GetRefresher()).SetProvider(this);
            this._isInitialLoadComplete = true;
        }
    }

    private async Task LoadKeyValueConfigurations()
    {
        if (_options.KeyPrefixes.Count == 0) return;

        var fetchedKeyValueConfigurations = await FetchKeyValues().ConfigureAwait(false);

        Data = ApplyTrimming(fetchedKeyValueConfigurations);
    }

    private Dictionary<string, string?> ApplyTrimming(List<KeyValueConfiguration> fetchedConfigurations)
    {
        var data = new Dictionary<string, string?>();
        foreach (var configuration in CollectionsMarshal.AsSpan(fetchedConfigurations))
        {
            if (_options.WatchedSettings.Any(kw => kw.Key == configuration.Key))
            {
                data.Add(configuration.Key, configuration.Value);
                continue;
            }

            var trimmed = false;
            foreach (var keyPrefix in _options.KeyPrefixes)
            {
                var index = configuration.Key.IndexOf(keyPrefix, StringComparison.InvariantCultureIgnoreCase);
                if (index <= -1)
                    continue;

                var trimmedKey = configuration.Key[(index + keyPrefix.Length)..];
                data.Add(trimmedKey, configuration.Value);
                trimmed = true;
                break;
            }

            if (trimmed)
                continue;

            var key = configuration.Key;
            data.Add(key, configuration.Value);
        }

        return data;
    }

    private async Task<List<KeyValueConfiguration>> FetchKeyValues()
    {
        var fetchQuery = $$"""
                                SELECT 
                                     c.[Key]        AS 'Key'
                                ,    c.[Value]      AS 'Value'
                                FROM   [{{_options.Schema}}].[{{_options.Table}}] c
                             """;
        var whereClause = new List<string>();
        foreach (var keySelector in _options.KeySelectors)
        {
            if (keySelector[0] == '*' && keySelector[^1] == '*')
                whereClause.Add($"c.[Key] LIKE '%{keySelector[1..^1]}%'");
            else if (keySelector[0] == '*')
                whereClause.Add($"c.[Key] LIKE '%{keySelector[1..]}'");
            else if (keySelector[^1] == '*')
                whereClause.Add($"c.[Key] LIKE '{keySelector[..^1]}%'");
            else if (!keySelector.Contains('*'))
                whereClause.Add($"c.[Key] = '{keySelector}'");
        }

        if (whereClause.Count > 0)
        {
            var where = string.Join(" OR ", whereClause);
            fetchQuery += $" WHERE {where}";
        }

        var fetchedConfigurations = await _sqlConnection.QueryAsync<KeyValueConfiguration>(fetchQuery)
            .ConfigureAwait(false);

        return fetchedConfigurations.ToList();
    }

    private void CreateConfigurationTableIfNotExists()
    {
        var createSchemaCommand = $"""
                                    IF schema_id('{_options.Schema}') IS NULL
                                        EXEC('CREATE SCHEMA [{_options.Schema}]');

                                    IF OBJECT_ID('{_options.Schema}.{_options.Table}') IS NULL
                                        BEGIN
                                            CREATE TABLE [{_options.Schema}].[{_options.Table}]
                                            (
                                                [Key]        nvarchar(256) NOT NULL PRIMARY KEY,
                                                [Value]      nvarchar(256) NOT NULL,
                                                Label        nvarchar(256) NOT NULL DEFAULT (''),
                                                LastModified DateTimeOffset DEFAULT (GETDATE())
                                            )
                                        END
                                 """;
        _sqlConnection.ExecuteAsync(createSchemaCommand).ConfigureAwait(false).GetAwaiter().GetResult();
    }


    public void Dispose() => _sqlConnection.DisposeAsync().GetAwaiter().GetResult();


    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (!_isInitialLoadComplete)
            return;

        if (Interlocked.Exchange(ref this._networkOperationsInProgress, 1) != 0)
            return;

        try
        {
            if (_options.WatchedSettings.Count == 0)
                return;
            
            foreach (var keyValueWatcher in _options.WatchedSettings)
            {
                var key = keyValueWatcher.Key;
                if (keyValueWatcher.CacheExpires > DateTimeOffset.Now)
                    continue;
                
                var providerCurrentValue = await _sqlConnection.ExecuteScalarAsync<string>(
                        $"SELECT c.[Value] FROM [{_options.Schema}].[{_options.Table}] c WHERE c.[Key] = '{key}'")
                    .ConfigureAwait(false);

                Data.TryGetValue(key, out var value);
                if (keyValueWatcher.RefreshAll && value != providerCurrentValue)
                {
                    await LoadKeyValueConfigurations().ConfigureAwait(false);
                }
                else
                {
                    Data.TryAdd(key, providerCurrentValue);
                }

                keyValueWatcher.CacheExpires = DateTimeOffset.Now.Add(keyValueWatcher.CacheExpirationInterval);
            }
        }
        finally
        {
            Interlocked.Exchange(ref this._networkOperationsInProgress, 0);
        }
    }

    public async Task<bool> TryRefreshAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await RefreshAsync(cancellationToken);
        }
        catch (SqlException ex)
        {
            _logger?.LogWarning(ex, "A refresh operation failed");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "A refresh operation failed");
            return false;
        }

        return true;
    }
}
