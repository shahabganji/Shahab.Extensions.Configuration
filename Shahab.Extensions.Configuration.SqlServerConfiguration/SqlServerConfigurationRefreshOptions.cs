using Shahab.Extensions.Configuration.SqlServerConfiguration.Internals;

namespace Shahab.Extensions.Configuration.SqlServerConfiguration;

public sealed class SqlServerConfigurationRefreshOptions
{
    internal ISet<SqlServerKeyValueWatcher> KeyValueWatchers { get; }
    internal TimeSpan CacheExpirationInterval { get; private set; }

    public SqlServerConfigurationRefreshOptions()
    {
        KeyValueWatchers = new HashSet<SqlServerKeyValueWatcher>();
        CacheExpirationInterval = RefreshConstants.DefaultCacheExpirationInterval;
    }

    public SqlServerConfigurationRefreshOptions Register(string sentinelKey, bool refreshAll = true)
    {
        KeyValueWatchers.Add(new SqlServerKeyValueWatcher { Key = sentinelKey, RefreshAll = refreshAll });
        return this;
    }

    public SqlServerConfigurationRefreshOptions SetCacheExpiration(TimeSpan cacheExpiration)
    {
        this.CacheExpirationInterval = !(cacheExpiration < RefreshConstants.MinimumCacheExpirationInterval)
            ? cacheExpiration
            : throw new ArgumentOutOfRangeException(nameof(cacheExpiration),
                cacheExpiration.TotalMilliseconds,
                $"The cache expiration time cannot be less than {RefreshConstants.MinimumCacheExpirationInterval.TotalMilliseconds} milliseconds.");
        return this;
    }
}
