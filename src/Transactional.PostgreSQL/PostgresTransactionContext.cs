namespace Transactional.PostgreSQL;

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Npgsql;
using Transactional.Abstractions;

/// <summary>
/// PostgreSQL transaction context implementation wrapping NpgsqlTransaction.
/// </summary>
public sealed class PostgresTransactionContext : IPostgresTransactionContext
{
    private readonly NpgsqlTransaction _transaction;
    private readonly NpgsqlConnection? _connectionToDispose;
    private readonly bool _ownsTransaction;
    private readonly List<Func<CancellationToken, Task>> _onCommittedCallbacks = new();
    private readonly List<Func<CancellationToken, Task>> _onRolledBackCallbacks = new();
    private bool _disposed;

    /// <inheritdoc />
    public NpgsqlTransaction Transaction => _transaction;

    /// <inheritdoc />
    public bool IsCommitted { get; private set; }

    /// <inheritdoc />
    public bool IsRolledBack { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PostgresTransactionContext"/> class with an existing transaction.
    /// Transaction and connection lifecycle is managed externally.
    /// </summary>
    /// <param name="transaction">The Npgsql transaction.</param>
    /// <exception cref="ArgumentNullException">Thrown when transaction is null.</exception>
    public PostgresTransactionContext(NpgsqlTransaction transaction)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        _ownsTransaction = false;
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
        _ownsTransaction = true;
    }

    /// <inheritdoc />
    public void OnCommitted(Func<CancellationToken, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (IsCommitted || IsRolledBack)
        {
            throw new InvalidOperationException("Cannot register callbacks after transaction has been committed or rolled back.");
        }

        _onCommittedCallbacks.Add(callback);
    }

    /// <inheritdoc />
    public void OnRolledBack(Func<CancellationToken, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        if (IsCommitted || IsRolledBack)
        {
            throw new InvalidOperationException("Cannot register callbacks after transaction has been committed or rolled back.");
        }

        _onRolledBackCallbacks.Add(callback);
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (IsRolledBack)
        {
            throw new InvalidOperationException("Cannot commit after rollback.");
        }

        if (IsCommitted)
        {
            return;
        }

        await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        IsCommitted = true;

        await TransactionCallbackInvoker.InvokeAsync(_onCommittedCallbacks, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (IsCommitted)
        {
            throw new InvalidOperationException("Cannot rollback after commit.");
        }

        if (IsRolledBack)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
        IsRolledBack = true;

        await TransactionCallbackInvoker.InvokeAsync(_onRolledBackCallbacks, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (!IsCommitted && !IsRolledBack)
        {
            await _transaction.RollbackAsync().ConfigureAwait(false);
        }

        if (_ownsTransaction)
        {
            await _transaction.DisposeAsync().ConfigureAwait(false);
        }

        if (_connectionToDispose != null)
        {
            await _connectionToDispose.DisposeAsync().ConfigureAwait(false);
        }

        _disposed = true;
    }
}
