
## SQL Server Configuration Provider

This packages allow developers to add Microsoft SQL Server service, Azure SQL included, as a configuration source in their .net applications, especially ASP.NET Core apps.

There are two packages, the first one provides the fundamental functionalities and the second one adds integration functionalities for the ASP.NET Core:

1. [Shahab.Extensions.Configuration.SqlServerConfiguration](https://www.nuget.org/packages/Shahab.Extensions.Configuration.SqlServerConfiguration/)
2. [Shahab.SqlServerConfiguration.AspNetCore](https://www.nuget.org/packages/Shahab.SqlServerConfiguration.AspNetCore/)

### Basic Usage: 

To use it is required to first add a reference to `Shahab.Extensions.Configuration.SqlServerConfiguration` package and then add the SQL Server as a configuration source to the `IConfigurationBuilder`.

```csharp
var sqlServerConnectionString = builder.Configuration.GetConnectionString("Database")!;
builder.Configuration.AddSqlServer(options =>
    options.Connect(sqlServerConnectionString)
        .Select("Sample:Settings:*")
);
```

Adding the above code, will add a table named 'dbo.Configuration' and fetch every configuration value in that table starting with `Sample:Setting`
from SQL Server instance and adds them to the configuration layer of the application. 
It creates the table if it does not exist, you could change table name by passing `tableName` and `schema`.

```csharp
builder.Configuration.AddSqlServer(options =>
    options.Connect(sqlServerConnectionString, tableName:"Configuration", schema: "config")
        .Select("Sample:Settings:*")
);
```

If the configuration source is shared between different applications, it is common to add a prefix as the name of the application, e.g. `Sample`, 
in the application itself, it is not needed to have that prefix as part of the configuration, you could trim it by providing a trimming key:

```csharp
builder.Configuration.AddSqlServer(options =>
    options.Connect(sqlServerConnectionString, tableName:"Configuration", schema: "config")
        .Select("Sample:Settings:*")
        .TrimKeyPrefix("Sample:") // <-- this line tells what should be trimmed from the beginning of the keys
);
```

Multiple trim keys and select keys could be added in a fluent way.

My settings in the `config.Configuration` table would be like, `Sample:Settings:MaxNumberOfRecords` with the value of `10`

### Refresh Keys at Runtime

It is very common to change the configuration values at runtime, and not want to restart the application, especially when it comes to microservices, that would be troublesome;
one of the main goals of the [External Configuration Store pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/external-configuration-store) is to enable a centralized means of configuration for all the instances of the application and change them on the fly.

However, we do not want to change the configurations on the application until a related set of the configuration values are set to their new values, we want to set them all, and then 
tell the application to refresh. To achieve this, we could register a sentinel key, and register that in the application, so when that key has changed, the configuration will be reloaded.

```csharp
builder.Configuration.AddSqlServer(options =>
    options.Connect(sqlServerConnectionString, tableName:"Configuration", schema: "config")
        .Select("Sample:Settings:*")
        .TrimKeyPrefix("Sample:")
        .ConfigureRefresh(refreshOptions =>
            refreshOptions.Register("Sample:Sentinel")) // <-- this line registers a sentinel key
    );
```

The next step is to register the services that could be used at runtime to refresh the keys, then an instance of the `IConfigurationRefresherProvider` 
could be injected to the services. 

```csharp
builder.Services.AddSqlServerConfiguration(); // <-- register all needed services 
```

#### Automatic Refresh

In ASP.NET Core applications, it is recommended to use the [Options Pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-7.0),
therefore it would be beneficial to have this refresh mechanism automatic and 
embedded with the Options Pattern. First add the [`Shahab.SqlServerConfiguration.ASpNetCore`](https://www.nuget.org/packages/Shahab.SqlServerConfiguration.AspNetCore/) nuget package
and then add the middleware in the appropriate place on your middleware pipeline. 

```csharp
app.UseSqlServerConfiguration();
```
**PS:** if this line is not added then automatic refresh will not work.

You could add more sentinel keys by calling the `Register` method, and you could set a cache expiration time for them by calling the `SetCacheExpiration` method:

```csharp
builder.Configuration.AddSqlServer(options =>
    options.Connect(sqlServerConnectionString, tableName:"Configuration", schema: "config")
        .Select("Sample:Settings:*")
        .TrimKeyPrefix("Sample:")
        .ConfigureRefresh(refreshOptions =>
            refreshOptions
                .Register("Sample:Sentinel")
                .Register("Sample:Sentinel:Settings")
                .SetCacheExpiration(TimeSpan.FromSeconds(10)) // <-- Set Cache Expiration Interval 
        )
    );
```

The minimum cache expiration value is `30` **seconds** and it cannot be less than **one second**; the keys will not get refreshed, 
if the cache expiration time has not expired yet.

### Azure SQL

You could connect to Azure SQL by providing a normal connection string the way you connect to any SQL Server instance;
however, when working in Azure Cloud, it is recommended to avoid secrets and use [Azure Managed Identities](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview) 
as far as possible. The `Connect` method has an overload that accepts a `TokenCredential`. By using that overload, Managed Identities could be used, 
there is also the possibility to have a [User Assigned Identity](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview#managed-identity-types) and use that.

```csharp
var sqlServerConnectionString = builder.Configuration.GetConnectionString("Database")!;
var userAssignedIdentity = "E55046F8-02C8-42C8-B41F-A8C1EAC0893B";
builder.Configuration.AddSqlServer(options =>
    options.Connect(sqlServerConnectionString,
            new DefaultAzureCredential(new DefaultAzureCredentialOptions()
                { ManagedIdentityClientId = userAssignedIdentity }))
        .Select("Sample:Settings:*")
        .TrimKeyPrefix("Sample:")
        .ConfigureRefresh(refreshOptions =>
            refreshOptions.Register("Sample:Sentinel")
                .Register("Sample:Sentinel:Settings")
                .SetCacheExpiration(TimeSpan.FromSeconds(10))
        )
);
```

<hr />

**PS:** In 2020, I wrote an [article](https://medium.com/@shahabganji/custom-configuration-providers-in-asp-net-core-ad583604220b) about how to create a custom configuration provider; however, when working with Azure Resources, I realized that it would be interesting 
to extend what I knew and dive deep into the topic, the APIs of this library are inspired by the official 
[Microsoft.Extensions.Configuration.AzureAppConfiguration](https://github.com/Azure/AppConfiguration) library. I would like to work on this one and adapt more
functionalities and environments as far as possible. I hope you enjoy this and any suggestion are more than welcome.

### Resources

You could find more articles describing how to write custom configuration providers:

* [Mine](https://medium.com/@shahabganji/custom-configuration-providers-in-asp-net-core-ad583604220b)
* [Microsoft - Implement a custom configuration provider in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/custom-configuration-provider)
* [Morteza Mousavi - A refreshable SQL Server Configuration Provider For .NET Core](https://mousavi310.github.io/posts/a-refreshable-sql-server-configuration-provider-for-net-core/)
* [William Rees - .NET 6 implementing a custom configuration provider](https://wil-rees.medium.com/net-6-implementing-a-custom-configuration-provider-980741cea2f5)
