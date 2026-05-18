namespace Aqua.Data.Sessions;

/// <summary>
/// Begin/Commit/Rollback wrapper that ensures Outbox-table writes happen inside the same transaction.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
