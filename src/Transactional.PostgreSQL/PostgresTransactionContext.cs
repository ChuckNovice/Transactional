namespace Transactional.PostgreSQL;

using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;

/// <summary>
/// PostgreSQL transaction context implementation wrapping NpgsqlTransaction.
/// </summary>
public sealed class PostgresTransactionContext : IPostgresTransactionContext
{
    private readonly NpgsqlTransaction _transaction;
    private readonly NpgsqlConnection? _connectionToDispose;
    private bool _committed;
    private bool _rolledBack;
    private bool _disposed;

    /// <inheritdoc />
    public NpgsqlTransaction Transaction => _transaction;

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresTransactionContext"/> class with an existing transaction.
    /// Connection lifecycle is managed externally.
    /// </summary>
    /// <param name="transaction">The Npgsql transaction.</param>
    /// <exception cref="ArgumentNullException">Thrown when transaction is null.</exception>
    public PostgresTransactionContext(NpgsqlTransaction transaction)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresTransactionContext"/> class with a connection and isolation level.
    /// Connection will be disposed with the context.
    /// </summary>
    /// <param name="connection">The Npgsql connection.</param>
    /// <param name="isolationLevel">The transaction isolation level.</param>
    /// <exception cref="ArgumentNullException">Thrown when connection is null.</exception>
    internal PostgresTransactionContext(NpgsqlConnection connection, IsolationLevel isolationLevel)
    {
        _connectionToDispose = connection ?? throw new ArgumentNullException(nameof(connection));
        _transaction = connection.BeginTransaction(isolationLevel);
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

        await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
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

        await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
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
            await _transaction.RollbackAsync().ConfigureAwait(false);
        }

        await _transaction.DisposeAsync().ConfigureAwait(false);

        if (_connectionToDispose != null)
        {
            await _connectionToDispose.DisposeAsync().ConfigureAwait(false);
        }

        _disposed = true;
    }
}
