using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shahab.Extensions.Configuration.SqlServerConfiguration;
using Shahab.Extensions.Configuration.SqlServerConfiguration.Abstractions;
using Shahab.Extensions.Configuration.SqlServerConfiguration.Internals;
using Shahab.SqlServerConfiguration.AspNetCore.Internals;

// ReSharper disable once CheckNamespace
namespace Shahab.Extensions;

public static class ConfigurationExtensions
{
    public static IApplicationBuilder UseSqlServerConfiguration(this IApplicationBuilder builder)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        return builder.ApplicationServices.GetService(typeof(IConfigurationRefresherProvider))
               != null
            ? builder.UseMiddleware<SqlServerConfigurationMiddleware>()
            : throw new InvalidOperationException(
                "Unable to find the required services. Please add all the required services by calling 'IServiceCollection.AddSqlServerConfiguration' inside the call to 'ConfigureServices(...)' in the application startup code.");
    }
}
