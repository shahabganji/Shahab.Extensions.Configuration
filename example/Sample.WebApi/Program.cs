using Azure.Identity;
using Dapper;
using Sample.WebApi;
using Shahab.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

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

builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));

builder.Services.AddSqlServerConfiguration();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSqlServerConfiguration();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
