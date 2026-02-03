namespace Transactional.Abstractions;

using System;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Represents an active database transaction that can be committed or rolled back.
/// </summary>
public interface ITransactionContext : IAsyncDisposable
{
    /// <summary>
    /// Gets whether the transaction has been committed.
    /// </summary>
    bool IsCommitted { get; }

    /// <summary>
    /// Gets whether the transaction has been rolled back.
    /// </summary>
    bool IsRolledBack { get; }

    /// <summary>
    /// Commits the transaction, persisting all changes. Idempotent - safe to call multiple times.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when called after RollbackAsync.</exception>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction, discarding all changes. Idempotent - safe to call multiple times.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when called after CommitAsync.</exception>
    Task RollbackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Registers a callback to be invoked after the transaction is successfully committed.
    /// Multiple callbacks can be registered and will execute in registration order.
    /// </summary>
    /// <param name="callback">The async callback to invoke after commit.</param>
    /// <exception cref="ArgumentNullException">Thrown when callback is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when called after commit or rollback.</exception>
    void OnCommitted(Func<CancellationToken, Task> callback);

    /// <summary>
    /// Registers a callback to be invoked after the transaction is rolled back.
    /// Multiple callbacks can be registered and will execute in registration order.
    /// Callbacks are only invoked for explicit rollback, not for implicit rollback during disposal.
    /// </summary>
    /// <param name="callback">The async callback to invoke after rollback.</param>
    /// <exception cref="ArgumentNullException">Thrown when callback is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when called after commit or rollback.</exception>
    void OnRolledBack(Func<CancellationToken, Task> callback);
}
