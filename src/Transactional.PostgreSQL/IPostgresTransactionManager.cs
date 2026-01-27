namespace Transactional.PostgreSQL;

using System.Data;
using System.Threading;
using System.Threading.Tasks;

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
}
