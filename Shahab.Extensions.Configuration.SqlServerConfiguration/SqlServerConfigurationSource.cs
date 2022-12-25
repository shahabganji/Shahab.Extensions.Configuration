using Microsoft.Extensions.Configuration;

namespace Shahab.Extensions.Configuration.SqlServerConfiguration;

public sealed class SqlServerConfigurationSource : IConfigurationSource
{
    private readonly SqlServerConfigurationOptions _configurationOptions;

    public SqlServerConfigurationSource(SqlServerConfigurationOptions configurationOptions)
    {
        _configurationOptions = configurationOptions;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new SqlServerConfigurationProvider(_configurationOptions);
}
