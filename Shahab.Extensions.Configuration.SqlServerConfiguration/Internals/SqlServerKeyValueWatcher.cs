namespace Shahab.Extensions.Configuration.SqlServerConfiguration.Internals;

internal class SqlServerKeyValueWatcher
{
    /// <summary>Key of the key-value to be watched.</summary>
    public string Key { get; init; } = default!;

    public bool RefreshAll { get; init; }
    
    /// <summary>
    /// The minimum time that must elapse before the key-value is refreshed.
    /// </summary>
    public TimeSpan CacheExpirationInterval { get; set; }

    /// <summary>The cache expiration time for the key-value.</summary>
    public DateTimeOffset CacheExpires { get; set; }
    
    
    public override bool Equals(object? obj) => obj is SqlServerKeyValueWatcher keyValueWatcher &&
                                                this.Key == keyValueWatcher.Key;

    public override int GetHashCode() => this.Key.GetHashCode();
}
