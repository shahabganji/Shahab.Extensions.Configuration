
## SQL Server Configuration Provider

This packages allow developers to add Microsoft SQL Server service, Azure SQL included, as a configuration source in their .net applications, especially ASP.NET Core apps.

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



