namespace Transactional.Abstractions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an active database transaction that can be committed or rolled back.
    /// </summary>
    public interface ITransactionContext : IAsyncDisposable
    {
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
    }
}
