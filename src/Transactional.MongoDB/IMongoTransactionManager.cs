namespace Transactional.MongoDB;

using System;
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

    /// <summary>
    /// Wraps an existing MongoDB session with an active transaction into a transaction context.
    /// The session lifecycle remains managed externally and will not be disposed when the context is disposed.
    /// </summary>
    /// <param name="session">A session handle with an active transaction.</param>
    /// <returns>A transaction context wrapping the existing transaction.</returns>
    /// <exception cref="ArgumentNullException">Thrown when session is null.</exception>
    /// <exception cref="ArgumentException">Thrown when session is not in an active transaction.</exception>
    IMongoTransactionContext WrapExistingTransaction(IClientSessionHandle session);
}
