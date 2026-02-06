namespace Transactional.PostgreSQL.Tests;

using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using global::Npgsql;
using Xunit;

/// <summary>
/// Tests for PostgreSQL ServiceCollectionExtensions.
/// Uses descriptor-based verification since NpgsqlDataSource is sealed and cannot be mocked.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    // --- Existing method: AddPostgresTransactionManager() ---

    [Fact]
    public void AddPostgresTransactionManager_RegistersTransactionManagerDescriptor()
    {
        var services = new ServiceCollection();

        services.AddPostgresTransactionManager();

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPostgresTransactionManager) &&
            !d.IsKeyedService);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddPostgresTransactionManager_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddPostgresTransactionManager();

        Assert.Same(services, result);
    }

    // --- Existing method: AddPostgresTransactionManager(connectionString) ---

    [Fact]
    public void AddPostgresTransactionManager_WithConnectionString_RegistersDataSourceDescriptor()
    {
        var services = new ServiceCollection();

        services.AddPostgresTransactionManager("Host=localhost;Database=test");

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(NpgsqlDataSource) &&
            !d.IsKeyedService);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddPostgresTransactionManager_WithConnectionString_RegistersTransactionManagerDescriptor()
    {
        var services = new ServiceCollection();

        services.AddPostgresTransactionManager("Host=localhost;Database=test");

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPostgresTransactionManager) &&
            !d.IsKeyedService);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    // --- Existing method: AddPostgresTransactionManager(factory) ---

    [Fact]
    public void AddPostgresTransactionManager_WithFactory_RegistersDataSourceDescriptor()
    {
        var services = new ServiceCollection();

        services.AddPostgresTransactionManager(_ => NpgsqlDataSource.Create("Host=localhost;Database=test"));

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(NpgsqlDataSource) &&
            !d.IsKeyedService);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddPostgresTransactionManager_WithFactory_RegistersTransactionManagerDescriptor()
    {
        var services = new ServiceCollection();

        services.AddPostgresTransactionManager(_ => NpgsqlDataSource.Create("Host=localhost;Database=test"));

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPostgresTransactionManager) &&
            !d.IsKeyedService);

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    // --- New method: AddKeyedPostgresTransactionManager(serviceKey) ---

    [Fact]
    public void AddKeyedPostgresTransactionManager_WithKey_RegistersKeyedTransactionManagerDescriptor()
    {
        var services = new ServiceCollection();
        const string key = "reporting";

        services.AddKeyedPostgresTransactionManager(key);

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPostgresTransactionManager) &&
            d.IsKeyedService &&
            Equals(d.ServiceKey, key));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddKeyedPostgresTransactionManager_WithKey_DoesNotRegisterDefaultService()
    {
        var services = new ServiceCollection();
        const string key = "reporting";

        services.AddKeyedPostgresTransactionManager(key);

        var defaultDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPostgresTransactionManager) &&
            !d.IsKeyedService);

        Assert.Null(defaultDescriptor);
    }

    // --- New method: AddPostgresTransactionManager(serviceKey, connectionString) ---

    [Fact]
    public void AddPostgresTransactionManager_WithKeyAndConnectionString_RegistersKeyedDataSourceDescriptor()
    {
        var services = new ServiceCollection();
        const string key = "reporting";

        services.AddPostgresTransactionManager(key, "Host=localhost;Database=reports");

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(NpgsqlDataSource) &&
            d.IsKeyedService &&
            Equals(d.ServiceKey, key));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddPostgresTransactionManager_WithKeyAndConnectionString_RegistersKeyedTransactionManagerDescriptor()
    {
        var services = new ServiceCollection();
        const string key = "reporting";

        services.AddPostgresTransactionManager(key, "Host=localhost;Database=reports");

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPostgresTransactionManager) &&
            d.IsKeyedService &&
            Equals(d.ServiceKey, key));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddPostgresTransactionManager_WithKeyAndConnectionString_DoesNotRegisterDefaultServices()
    {
        var services = new ServiceCollection();
        const string key = "reporting";

        services.AddPostgresTransactionManager(key, "Host=localhost;Database=reports");

        var defaultDataSource = services.FirstOrDefault(d =>
            d.ServiceType == typeof(NpgsqlDataSource) &&
            !d.IsKeyedService);
        var defaultManager = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPostgresTransactionManager) &&
            !d.IsKeyedService);

        Assert.Null(defaultDataSource);
        Assert.Null(defaultManager);
    }

    // --- New method: AddPostgresTransactionManager(serviceKey, factory) ---

    [Fact]
    public void AddPostgresTransactionManager_WithKeyAndFactory_RegistersKeyedDataSourceDescriptor()
    {
        var services = new ServiceCollection();
        const string key = "reporting";

        services.AddPostgresTransactionManager(key, _ => NpgsqlDataSource.Create("Host=localhost;Database=reports"));

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(NpgsqlDataSource) &&
            d.IsKeyedService &&
            Equals(d.ServiceKey, key));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddPostgresTransactionManager_WithKeyAndFactory_RegistersKeyedTransactionManagerDescriptor()
    {
        var services = new ServiceCollection();
        const string key = "reporting";

        services.AddPostgresTransactionManager(key, _ => NpgsqlDataSource.Create("Host=localhost;Database=reports"));

        var descriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPostgresTransactionManager) &&
            d.IsKeyedService &&
            Equals(d.ServiceKey, key));

        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddPostgresTransactionManager_WithKeyAndFactory_DoesNotRegisterDefaultServices()
    {
        var services = new ServiceCollection();
        const string key = "reporting";

        services.AddPostgresTransactionManager(key, _ => NpgsqlDataSource.Create("Host=localhost;Database=reports"));

        var defaultDataSource = services.FirstOrDefault(d =>
            d.ServiceType == typeof(NpgsqlDataSource) &&
            !d.IsKeyedService);
        var defaultManager = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPostgresTransactionManager) &&
            !d.IsKeyedService);

        Assert.Null(defaultDataSource);
        Assert.Null(defaultManager);
    }

    [Fact]
    public void AddPostgresTransactionManager_WithKey_MultipleKeysCanCoexist()
    {
        var services = new ServiceCollection();

        services.AddPostgresTransactionManager("primary", "Host=primary-db;Database=app");
        services.AddPostgresTransactionManager("reporting", "Host=reporting-db;Database=reports");

        var primaryManager = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPostgresTransactionManager) &&
            d.IsKeyedService &&
            Equals(d.ServiceKey, "primary"));
        var reportingManager = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IPostgresTransactionManager) &&
            d.IsKeyedService &&
            Equals(d.ServiceKey, "reporting"));

        Assert.NotNull(primaryManager);
        Assert.NotNull(reportingManager);
    }
}
