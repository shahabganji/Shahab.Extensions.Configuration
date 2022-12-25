using Microsoft.Extensions.Logging;

namespace Shahab.Extensions.Configuration.SqlServerConfiguration.Abstractions;

public interface IConfigurationRefresher
{
    ILoggerFactory LoggerFactory { get; set; }
    Task RefreshAsync(CancellationToken cancellationToken = default);
    Task<bool> TryRefreshAsync(CancellationToken cancellationToken = default);
}
