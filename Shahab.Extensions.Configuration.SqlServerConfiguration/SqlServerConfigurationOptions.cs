using Azure.Core;
using Shahab.Extensions.Configuration.SqlServerConfiguration.Abstractions;
using Shahab.Extensions.Configuration.SqlServerConfiguration.Internals;

namespace Shahab.Extensions.Configuration.SqlServerConfiguration;

public sealed class SqlServerConfigurationOptions
{
    private  IConfigurationRefresher Refresher { get; }
    internal SortedSet<string> KeyPrefixes { get; }
    internal SortedSet<string> KeySelectors { get; }
    internal TokenCredential? Token { get; private set; }
    internal string ConnectionString { get; private set; } = default!;
    internal string Table { get; private set; }
    internal string Schema { get; private set; }
    internal bool Optional { get; }
    
    internal ISet<SqlServerKeyValueWatcher> WatchedSettings { get; private set; }


    public SqlServerConfigurationOptions(bool optional)
    {
        Table = "Configurations";
        Schema = "dbo";
        Optional = optional;
        KeySelectors = new SortedSet<string>();
        KeyPrefixes = new SortedSet<string>();
        WatchedSettings = new HashSet<SqlServerKeyValueWatcher>();
        Refresher = new SqlServerConfigurationRefresher();
    }

    public IConfigurationRefresher GetRefresher() => this.Refresher;

    public SqlServerConfigurationOptions Connect(string endpoint, TokenCredential tokenCredential,
        string tableName = "Configurations",
        string schema = "dbo")
    {
        Token = tokenCredential;
        ConnectionString = endpoint;

        Table = tableName;
        Schema = schema;
        
        return this;
    }



    public SqlServerConfigurationOptions Connect(string connectionString, string tableName = "Configurations",
        string schema = "dbo")
    {
        ConnectionString = connectionString;
        Token = null;

        Table = tableName;
        Schema = schema;
        return this;
    }

    public SqlServerConfigurationOptions Select(string selector)
    {
        if (string.IsNullOrWhiteSpace(selector))
            throw new ArgumentNullException(selector);

        KeySelectors.Add(selector);

        return this;
    }

    public SqlServerConfigurationOptions TrimKeyPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentNullException(nameof(prefix));

        KeyPrefixes.Add(prefix);
        return this;
    }

    public SqlServerConfigurationOptions ConfigureRefresh(
        Action<SqlServerConfigurationRefreshOptions> refreshOptionsAction)
    {
        if (refreshOptionsAction is null)
            throw new ArgumentNullException(nameof(refreshOptionsAction));

        var refreshOptions = new SqlServerConfigurationRefreshOptions();
        refreshOptionsAction.Invoke(refreshOptions);

        foreach (var keyValueWatcher in refreshOptions.KeyValueWatchers)
        {
            keyValueWatcher.CacheExpirationInterval = refreshOptions.CacheExpirationInterval;
            this.WatchedSettings.Add(keyValueWatcher);
        }

        return this;
    }
}
