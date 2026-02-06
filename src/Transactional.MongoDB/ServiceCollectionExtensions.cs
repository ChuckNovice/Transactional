namespace Microsoft.Extensions.DependencyInjection;

using MongoDB.Driver;
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
    /// <remarks>
    /// This method registers both an <see cref="IMongoClient"/> and an <see cref="IMongoTransactionManager"/> as singletons.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMongoDbTransactionManager(sp =>
    /// {
    ///     var config = sp.GetRequiredService&lt;IConfiguration&gt;();
    ///     return new MongoClient(config.GetConnectionString("MongoDB"));
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMongoDbTransactionManager(
        this IServiceCollection services,
        Func<IServiceProvider, IMongoClient> clientFactory)
    {
        services.AddSingleton(clientFactory);
        services.AddSingleton<IMongoTransactionManager>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return new MongoTransactionManager(client);
        });

        return services;
    }

    /// <summary>
    /// Registers a keyed MongoDB transaction manager with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="serviceKey">The key to associate with the transaction manager.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method requires a keyed <see cref="IMongoClient"/> with the same key to already be registered.
    /// The transaction manager is registered as a keyed singleton.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddKeyedSingleton&lt;IMongoClient&gt;("analytics", new MongoClient("mongodb://analytics-host:27017"));
    /// services.AddKeyedMongoDbTransactionManager("analytics");
    /// </code>
    /// </example>
    public static IServiceCollection AddKeyedMongoDbTransactionManager(
        this IServiceCollection services,
        object serviceKey)
    {
        services.AddKeyedSingleton<IMongoTransactionManager>(serviceKey, (sp, key) =>
        {
            var client = sp.GetRequiredKeyedService<IMongoClient>(key);
            return new MongoTransactionManager(client);
        });

        return services;
    }

    /// <summary>
    /// Registers a keyed MongoDB transaction manager with the dependency injection container using a custom factory.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="serviceKey">The key to associate with the transaction manager.</param>
    /// <param name="clientFactory">A factory function that creates the <see cref="IMongoClient"/> instance.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method registers both a keyed <see cref="IMongoClient"/> and a keyed <see cref="IMongoTransactionManager"/> as singletons.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddMongoDbTransactionManager("analytics", sp =>
    /// {
    ///     var config = sp.GetRequiredService&lt;IConfiguration&gt;();
    ///     return new MongoClient(config.GetConnectionString("AnalyticsMongoDB"));
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddMongoDbTransactionManager(
        this IServiceCollection services,
        object serviceKey,
        Func<IServiceProvider, IMongoClient> clientFactory)
    {
        services.AddKeyedSingleton<IMongoClient>(serviceKey, (sp, _) => clientFactory(sp));
        services.AddKeyedSingleton<IMongoTransactionManager>(serviceKey, (sp, key) =>
        {
            var client = sp.GetRequiredKeyedService<IMongoClient>(key);
            return new MongoTransactionManager(client);
        });

        return services;
    }
}
