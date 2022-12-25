using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Shahab.Extensions.Configuration.SqlServerConfiguration.Abstractions;

public interface IConfigurationRefresherProvider
{
    /// <summary>
    /// List of instances of <see cref="T:Microsoft.Extensions.Configuration.AzureAppConfiguration.IConfigurationRefresher" /> for App Configuration.
    /// </summary>
    IEnumerable<IConfigurationRefresher> Refreshers { get; }
}

public sealed class SqlServerConfigurationRefresherProvider : IConfigurationRefresherProvider
{
    public IEnumerable<IConfigurationRefresher> Refreshers { get; }

    public SqlServerConfigurationRefresherProvider(IConfiguration configuration, ILoggerFactory loggerFactory)
    {
        var source = new List<IConfigurationRefresher>();
        if (configuration is IConfigurationRoot configurationRoot)
        {
            foreach (var provider in configurationRoot.Providers)
            {
                if (provider is not IConfigurationRefresher configurationRefresher)
                    continue;
                
                configurationRefresher.LoggerFactory ??= loggerFactory;
                source.Add(configurationRefresher);
            }
        }

        Refreshers = source.Any()
            ? source
            : throw new InvalidOperationException(
                "Unable to access the Sql Server Configuration provider. Please ensure that it has been configured correctly.");
    }
}
