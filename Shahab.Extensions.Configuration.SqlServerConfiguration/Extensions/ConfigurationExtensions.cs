using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shahab.Extensions.Configuration.SqlServerConfiguration;
using Shahab.Extensions.Configuration.SqlServerConfiguration.Abstractions;

// ReSharper disable once CheckNamespace
namespace Shahab.Extensions;

public static class ConfigurationExtensions
{
    public static IConfigurationBuilder AddSqlServer(
        this IConfigurationBuilder configurationBuilder,
        Action<SqlServerConfigurationOptions> options, bool optional = false)
    {
        var sqlServerConfigurationOptions = new SqlServerConfigurationOptions(optional);
        options.Invoke(sqlServerConfigurationOptions);

        configurationBuilder.Add(new SqlServerConfigurationSource(sqlServerConfigurationOptions));

        return configurationBuilder;
    }

    public static IServiceCollection AddSqlServerConfiguration(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.AddLogging();
        services.AddSingleton<IConfigurationRefresherProvider, SqlServerConfigurationRefresherProvider>();
        return services;
    }
}
