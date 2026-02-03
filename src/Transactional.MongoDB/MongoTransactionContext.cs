namespace Transactional.MongoDB;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Driver;
using Transactional.Abstractions;

/// <summary>
/// MongoDB transaction context implementation wrapping IClientSessionHandle.
/// </summary>
public sealed class MongoTransactionContext : IMongoTransactionContext
{
    private readonly IClientSessionHandle _session;
    private readonly bool _ownsSession;
    private readonly List<Func<CancellationToken, Task>> _onCommittedCallbacks = new();
    private readonly List<Func<CancellationToken, Task>> _onRolledBackCallbacks = new();
    private bool _disposed;

    /// <inheritdoc />
    public IClientSessionHandle Session => _session;

    /// <inheritdoc />
    public bool IsCommitted { get; private set; }

    /// <inheritdoc />
    public bool IsRolledBack { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoTransactionContext"/> class.
    /// </summary>
    /// <param name="session">The MongoDB client session handle.</param>
    /// <param name="options">Optional transaction options.</param>
    /// <exception cref="ArgumentNullException">Thrown when session is null.</exception>
    public MongoTransactionContext(IClientSessionHandle session, TransactionOptions? options = null)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _ownsSession = true;
        _session.StartTransaction(options);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoTransactionContext"/> class
    /// wrapping an existing transaction. The session lifecycle is managed externally.
    /// </summary>
    /// <param name="session">The MongoDB client session handle with an active transaction.</param>
    /// <param name="existingTransaction">Must be true to indicate this wraps an existing transaction.</param>
    /// <exception cref="ArgumentNullException">Thrown when session is null.</exception>
    internal MongoTransactionContext(IClientSessionHandle session, bool existingTransaction)
    {
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _ownsSession = !existingTransaction;
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

        await _session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
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

        await _session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
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
            await _session.AbortTransactionAsync().ConfigureAwait(false);
        }

        if (_ownsSession)
        {
            _session.Dispose();
        }

        _disposed = true;
    }
}
