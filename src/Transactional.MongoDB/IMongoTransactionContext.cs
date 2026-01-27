namespace Transactional.MongoDB
{
    using global::MongoDB.Driver;
    using Transactional.Abstractions;

    /// <summary>
    /// MongoDB-specific transaction context providing access to the underlying IClientSessionHandle.
    /// </summary>
    public interface IMongoTransactionContext : ITransactionContext
    {
        /// <summary>
        /// Gets the native MongoDB session handle for use in database operations.
        /// </summary>
        IClientSessionHandle Session { get; }
    }
}
