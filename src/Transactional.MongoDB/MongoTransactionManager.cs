namespace Transactional.MongoDB;

using System;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Driver;

/// <summary>
/// Default implementation of <see cref="IMongoTransactionManager"/>.
/// </summary>
public sealed class MongoTransactionManager : IMongoTransactionManager
{
    private readonly IMongoClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoTransactionManager"/> class.
    /// </summary>
    /// <param name="client">The MongoDB client.</param>
    /// <exception cref="ArgumentNullException">Thrown when client is null.</exception>
    public MongoTransactionManager(IMongoClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    /// <inheritdoc />
    public async Task<IMongoTransactionContext> BeginTransactionAsync(
        TransactionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var session = await _client.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        return new MongoTransactionContext(session, options);
    }
}
