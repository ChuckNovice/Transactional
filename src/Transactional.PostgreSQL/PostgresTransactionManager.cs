namespace Transactional.PostgreSQL
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Npgsql;

    /// <summary>
    /// Default implementation of <see cref="IPostgresTransactionManager"/> using NpgsqlDataSource.
    /// </summary>
    public sealed class PostgresTransactionManager : IPostgresTransactionManager
    {
        private readonly NpgsqlDataSource _dataSource;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresTransactionManager"/> class.
        /// </summary>
        /// <param name="dataSource">The Npgsql data source.</param>
        /// <exception cref="ArgumentNullException">Thrown when dataSource is null.</exception>
        public PostgresTransactionManager(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        /// <inheritdoc />
        public async Task<IPostgresTransactionContext> BeginTransactionAsync(
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
            return new PostgresTransactionContext(connection, isolationLevel);
        }
    }
}
