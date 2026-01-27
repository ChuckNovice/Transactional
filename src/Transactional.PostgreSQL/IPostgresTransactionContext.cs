namespace Transactional.PostgreSQL;

using Npgsql;
using Transactional.Abstractions;

/// <summary>
/// PostgreSQL-specific transaction context providing access to the underlying NpgsqlTransaction.
/// </summary>
public interface IPostgresTransactionContext : ITransactionContext
{
    /// <summary>
    /// Gets the native Npgsql transaction for use in database operations.
    /// </summary>
    NpgsqlTransaction Transaction { get; }
}
