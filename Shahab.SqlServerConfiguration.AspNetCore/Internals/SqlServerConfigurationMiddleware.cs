using Microsoft.AspNetCore.Http;
using Shahab.Extensions.Configuration.SqlServerConfiguration.Abstractions;

namespace Shahab.SqlServerConfiguration.AspNetCore.Internals;

internal sealed class SqlServerConfigurationMiddleware
{
    private readonly IEnumerable<IConfigurationRefresher> _refreshers;

    private readonly RequestDelegate _next;

    public SqlServerConfigurationMiddleware(
        RequestDelegate next,
        IConfigurationRefresherProvider refresherProvider)
    {
        _next = next;
        _refreshers = refresherProvider.Refreshers;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        foreach (var refresher in _refreshers)
            await refresher.TryRefreshAsync();

        await _next(context);
    }
}
