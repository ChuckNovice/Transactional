namespace Transactional.Abstractions
{
    /// <summary>
    /// Provides access to the current ambient transaction context. Must be registered with Scoped lifetime in dependency injection.
    /// </summary>
    public interface ITransactionContextAccessor
    {
        /// <summary>
        /// Gets or sets the current transaction context. Returns null when no transaction is active.
        /// Thread-safe within the same DI scope.
        /// </summary>
        ITransactionContext? Current { get; set; }
    }
}
