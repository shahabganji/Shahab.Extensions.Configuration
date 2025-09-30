using Microsoft.Extensions.Logging;
using Shahab.Extensions.Configuration.SqlServerConfiguration.Abstractions;

namespace Shahab.Extensions.Configuration.SqlServerConfiguration.Internals;

internal sealed class SqlServerConfigurationRefresher : IConfigurationRefresher
{
    private SqlServerConfigurationProvider _provider = default!;

    public ILoggerFactory LoggerFactory
    {
        get
        {
            this.ThrowIfNullProvider(nameof(LoggerFactory));
            return this._provider.LoggerFactory;
        }
        set
        {
            this.ThrowIfNullProvider(nameof(LoggerFactory));
            this._provider.LoggerFactory = value;
        }
    }

    public void SetProvider(SqlServerConfigurationProvider provider)
    {
        this._provider = provider ?? throw new ArgumentNullException(nameof(provider));
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        this.ThrowIfNullProvider(nameof(RefreshAsync));
        await this._provider.RefreshAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> TryRefreshAsync(CancellationToken cancellationToken = default)
    {
        this.ThrowIfNullProvider(nameof(RefreshAsync));
        return await this._provider.TryRefreshAsync(cancellationToken).ConfigureAwait(false);
    }


    public void SetDirty(TimeSpan? maxDelay)
    {
        this.ThrowIfNullProvider(nameof(SetDirty));
        // this._provider.SetDirty(maxDelay);
    }

    private void ThrowIfNullProvider(string operation)
    {
        if (this._provider == null)
            throw new InvalidOperationException("ConfigurationBuilder.Build() must be called before " + operation +
                                                " can be accessed.");
    }
}
