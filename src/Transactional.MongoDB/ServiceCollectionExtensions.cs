namespace Microsoft.Extensions.DependencyInjection;

using global::MongoDB.Driver;
using Transactional.MongoDB;

/// <summary>
/// Extension methods for registering MongoDB transaction services with dependency injection.
/// </summary>
public static class MongoDbTransactionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MongoDB transaction manager with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method requires an <see cref="IMongoClient"/> to already be registered in the service collection.
    /// The transaction manager is registered as a singleton.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddSingleton&lt;IMongoClient&gt;(new MongoClient("mongodb://localhost:27017"));
    /// services.AddMongoDbTransactionManager();
    /// </code>
    /// </example>
    public static IServiceCollection AddMongoDbTransactionManager(this IServiceCollection services)
    {
        services.AddSingleton<IMongoTransactionManager>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return new MongoTransactionManager(client);
        });

        return services;
    }

    /// <summary>
    /// Registers the MongoDB transaction manager with the dependency injection container using a custom factory.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="clientFactory">A factory function that creates the <see cref="IMongoClient"/> instance.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddMongoDbTransactionManager(sp => new MongoClient("mongodb://localhost:27017"));
    /// </code>
    /// </example>
    public static IServiceCollection AddMongoDbTransactionManager(
        this IServiceCollection services,
        Func<IServiceProvider, IMongoClient> clientFactory)
    {
        services.AddSingleton<IMongoClient>(clientFactory);
        services.AddSingleton<IMongoTransactionManager>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return new MongoTransactionManager(client);
        });

        return services;
    }
}
