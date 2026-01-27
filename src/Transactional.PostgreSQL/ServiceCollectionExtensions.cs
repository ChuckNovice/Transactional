namespace Microsoft.Extensions.DependencyInjection;

using Npgsql;
using Transactional.PostgreSQL;

/// <summary>
/// Extension methods for registering PostgreSQL transaction services with dependency injection.
/// </summary>
public static class PostgresTransactionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the PostgreSQL transaction manager with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// This method requires an <see cref="NpgsqlDataSource"/> to already be registered in the service collection.
    /// The transaction manager is registered as a singleton.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddSingleton(NpgsqlDataSource.Create("Host=localhost;Database=myapp"));
    /// services.AddPostgresTransactionManager();
    /// </code>
    /// </example>
    public static IServiceCollection AddPostgresTransactionManager(this IServiceCollection services)
    {
        services.AddSingleton<IPostgresTransactionManager>(sp =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            return new PostgresTransactionManager(dataSource);
        });

        return services;
    }

    /// <summary>
    /// Registers the PostgreSQL transaction manager with the dependency injection container using a connection string.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddPostgresTransactionManager("Host=localhost;Database=myapp;Username=postgres;Password=secret");
    /// </code>
    /// </example>
    public static IServiceCollection AddPostgresTransactionManager(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddSingleton<IPostgresTransactionManager>(sp =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            return new PostgresTransactionManager(dataSource);
        });

        return services;
    }

    /// <summary>
    /// Registers the PostgreSQL transaction manager with the dependency injection container using a custom factory.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <param name="dataSourceFactory">A factory function that creates the <see cref="NpgsqlDataSource"/> instance.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// services.AddPostgresTransactionManager(sp =>
    /// {
    ///     var config = sp.GetRequiredService&lt;IConfiguration&gt;();
    ///     return NpgsqlDataSource.Create(config.GetConnectionString("PostgreSQL")!);
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddPostgresTransactionManager(
        this IServiceCollection services,
        Func<IServiceProvider, NpgsqlDataSource> dataSourceFactory)
    {
        services.AddSingleton(dataSourceFactory);
        services.AddSingleton<IPostgresTransactionManager>(sp =>
        {
            var dataSource = sp.GetRequiredService<NpgsqlDataSource>();
            return new PostgresTransactionManager(dataSource);
        });

        return services;
    }
}
