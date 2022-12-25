namespace Shahab.Extensions.Configuration.SqlServerConfiguration.Internals;

internal static class RefreshConstants
{
    public static readonly TimeSpan DefaultCacheExpirationInterval = TimeSpan.FromSeconds(30.0);
    public static readonly TimeSpan MinimumCacheExpirationInterval = TimeSpan.FromSeconds(1.0);
}
