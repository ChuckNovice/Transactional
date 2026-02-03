namespace Transactional.PostgreSQL;

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

/// <summary>
/// Creates PostgreSQL transactions with standard isolation level options.
/// </summary>
public interface IPostgresTransactionManager
{
    /// <summary>
    /// Begins a new PostgreSQL transaction with specified isolation level.
    /// </summary>
    /// <param name="isolationLevel">The transaction isolation level. Defaults to ReadCommitted.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns the transaction context.</returns>
    Task<IPostgresTransactionContext> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Wraps an existing NpgsqlTransaction into a transaction context.
    /// The transaction and connection lifecycle remain managed externally and will not be disposed when the context is disposed.
    /// </summary>
    /// <param name="transaction">An active Npgsql transaction.</param>
    /// <returns>A transaction context wrapping the existing transaction.</returns>
    /// <exception cref="ArgumentNullException">Thrown when transaction is null.</exception>
    /// <exception cref="ArgumentException">Thrown when transaction is in an invalid state.</exception>
    IPostgresTransactionContext WrapExistingTransaction(NpgsqlTransaction transaction);
}
