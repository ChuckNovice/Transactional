namespace Transactional.MongoDB;

using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Driver;

/// <summary>
/// Creates MongoDB transactions with access to MongoDB-specific transaction options.
/// </summary>
public interface IMongoTransactionManager
{
    /// <summary>
    /// Begins a new MongoDB transaction with optional configuration.
    /// </summary>
    /// <param name="options">Optional MongoDB transaction options.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation that returns the transaction context.</returns>
    Task<IMongoTransactionContext> BeginTransactionAsync(TransactionOptions? options = null, CancellationToken cancellationToken = default);
}
