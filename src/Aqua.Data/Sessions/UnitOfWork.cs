using NHibernate;

namespace Aqua.Data.Sessions;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ISessionScope _scope;
    private ITransaction? _tx;
    private bool _disposed;

    public UnitOfWork(ISessionScope scope) => _scope = scope;

    public Task BeginAsync(CancellationToken ct = default)
    {
        _tx ??= _scope.Session.BeginTransaction();
        return Task.CompletedTask;
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_tx is null) throw new InvalidOperationException("BeginAsync() must be called before CommitAsync().");
        await _tx.CommitAsync(ct);
        _tx.Dispose();
        _tx = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_tx is { IsActive: true })
        {
            await _tx.RollbackAsync(ct);
            _tx.Dispose();
            _tx = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _tx?.Dispose();
        _disposed = true;
    }
}
