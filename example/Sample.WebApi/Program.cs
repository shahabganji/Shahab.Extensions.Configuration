using Azure.Identity;
using Dapper;
using Sample.WebApi;
using Shahab.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var sqlServerConnectionString = builder.Configuration.GetConnectionString("Database")!;
builder.Configuration.AddSqlServer(options =>
    options.Connect(sqlServerConnectionString, tableName:"Configuration", schema: "config")
        .Select("Sample:Settings:*")
        .TrimKeyPrefix("Sample:")
    );

builder.Services.Configure<Settings>(builder.Configuration.GetSection("Settings"));


// builder.Services.AddSqlServerConfiguration();

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

// app.UseSqlServerConfiguration();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
