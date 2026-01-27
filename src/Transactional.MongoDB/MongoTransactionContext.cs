namespace Transactional.MongoDB
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using global::MongoDB.Driver;

    /// <summary>
    /// MongoDB transaction context implementation wrapping IClientSessionHandle.
    /// </summary>
    public sealed class MongoTransactionContext : IMongoTransactionContext
    {
        private readonly IClientSessionHandle _session;
        private bool _committed;
        private bool _rolledBack;
        private bool _disposed;

        /// <inheritdoc />
        public IClientSessionHandle Session => _session;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoTransactionContext"/> class.
        /// </summary>
        /// <param name="session">The MongoDB client session handle.</param>
        /// <param name="options">Optional transaction options.</param>
        /// <exception cref="ArgumentNullException">Thrown when session is null.</exception>
        public MongoTransactionContext(IClientSessionHandle session, TransactionOptions? options = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _session.StartTransaction(options);
        }

        /// <inheritdoc />
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (_rolledBack)
            {
                throw new InvalidOperationException("Cannot commit after rollback.");
            }

            if (_committed)
            {
                return;
            }

            await _session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
            _committed = true;
        }

        /// <inheritdoc />
        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            if (_committed)
            {
                throw new InvalidOperationException("Cannot rollback after commit.");
            }

            if (_rolledBack)
            {
                return;
            }

            await _session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
            _rolledBack = true;
        }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            if (!_committed && !_rolledBack)
            {
                await _session.AbortTransactionAsync().ConfigureAwait(false);
            }

            _session.Dispose();
            _disposed = true;
        }
    }
}
