namespace Transactional.MongoDB.Tests;

using System;
using global::MongoDB.Driver;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

public class ServiceCollectionExtensionsTests
{
    private readonly Mock<IMongoClient> _mockClient;

    public ServiceCollectionExtensionsTests()
    {
        _mockClient = new Mock<IMongoClient>();
    }

    // --- Existing method: AddMongoDbTransactionManager() ---

    [Fact]
    public void AddMongoDbTransactionManager_WithPreregisteredClient_RegistersTransactionManager()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_mockClient.Object);

        services.AddMongoDbTransactionManager();

        var provider = services.BuildServiceProvider();
        var manager = provider.GetService<IMongoTransactionManager>();

        Assert.NotNull(manager);
        Assert.IsType<MongoTransactionManager>(manager);
    }

    [Fact]
    public void AddMongoDbTransactionManager_ReturnsServiceCollectionForChaining()
    {
        var services = new ServiceCollection();

        var result = services.AddMongoDbTransactionManager();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddMongoDbTransactionManager_RegistersAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddSingleton(_mockClient.Object);
        services.AddMongoDbTransactionManager();

        var provider = services.BuildServiceProvider();
        var manager1 = provider.GetRequiredService<IMongoTransactionManager>();
        var manager2 = provider.GetRequiredService<IMongoTransactionManager>();

        Assert.Same(manager1, manager2);
    }

    // --- New method: AddMongoDbTransactionManager(factory) ---

    [Fact]
    public void AddMongoDbTransactionManager_WithFactory_RegistersTransactionManager()
    {
        var services = new ServiceCollection();

        services.AddMongoDbTransactionManager(_ => _mockClient.Object);

        var provider = services.BuildServiceProvider();
        var manager = provider.GetService<IMongoTransactionManager>();

        Assert.NotNull(manager);
        Assert.IsType<MongoTransactionManager>(manager);
    }

    [Fact]
    public void AddMongoDbTransactionManager_WithFactory_InvokesFactory()
    {
        var services = new ServiceCollection();
        var factoryInvoked = false;

        services.AddMongoDbTransactionManager(_ =>
        {
            factoryInvoked = true;
            return _mockClient.Object;
        });

        var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IMongoTransactionManager>();

        Assert.True(factoryInvoked);
    }

    [Fact]
    public void AddMongoDbTransactionManager_WithFactory_FactoryReceivesServiceProvider()
    {
        var services = new ServiceCollection();
        var marker = new MarkerService();
        services.AddSingleton(marker);
        IServiceProvider? receivedProvider = null;

        services.AddMongoDbTransactionManager(sp =>
        {
            receivedProvider = sp;
            return _mockClient.Object;
        });

        var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredService<IMongoTransactionManager>();

        Assert.NotNull(receivedProvider);
        Assert.Same(marker, receivedProvider!.GetService<MarkerService>());
    }

    // --- New method: AddKeyedMongoDbTransactionManager(serviceKey) ---

    [Fact]
    public void AddKeyedMongoDbTransactionManager_WithKey_RegistersKeyedTransactionManager()
    {
        var services = new ServiceCollection();
        const string key = "analytics";
        services.AddKeyedSingleton(key, _mockClient.Object);

        services.AddKeyedMongoDbTransactionManager(key);

        var provider = services.BuildServiceProvider();
        var manager = provider.GetKeyedService<IMongoTransactionManager>(key);

        Assert.NotNull(manager);
        Assert.IsType<MongoTransactionManager>(manager);
    }

    [Fact]
    public void AddKeyedMongoDbTransactionManager_WithKey_DoesNotRegisterDefaultService()
    {
        var services = new ServiceCollection();
        const string key = "analytics";
        services.AddKeyedSingleton(key, _mockClient.Object);

        services.AddKeyedMongoDbTransactionManager(key);

        var provider = services.BuildServiceProvider();
        var defaultManager = provider.GetService<IMongoTransactionManager>();

        Assert.Null(defaultManager);
    }

    [Fact]
    public void AddKeyedMongoDbTransactionManager_WithKey_MultipleKeysCanCoexist()
    {
        var services = new ServiceCollection();
        var mockClient1 = new Mock<IMongoClient>();
        var mockClient2 = new Mock<IMongoClient>();
        services.AddKeyedSingleton("primary", mockClient1.Object);
        services.AddKeyedSingleton("analytics", mockClient2.Object);

        services.AddKeyedMongoDbTransactionManager("primary");
        services.AddKeyedMongoDbTransactionManager("analytics");

        var provider = services.BuildServiceProvider();
        var manager1 = provider.GetKeyedService<IMongoTransactionManager>("primary");
        var manager2 = provider.GetKeyedService<IMongoTransactionManager>("analytics");

        Assert.NotNull(manager1);
        Assert.NotNull(manager2);
        Assert.NotSame(manager1, manager2);
    }

    // --- New method: AddMongoDbTransactionManager(serviceKey, factory) ---

    [Fact]
    public void AddMongoDbTransactionManager_WithKeyAndFactory_RegistersKeyedClient()
    {
        var services = new ServiceCollection();
        const string key = "analytics";

        services.AddMongoDbTransactionManager(key, _ => _mockClient.Object);

        var provider = services.BuildServiceProvider();
        var client = provider.GetKeyedService<IMongoClient>(key);

        Assert.NotNull(client);
        Assert.Same(_mockClient.Object, client);
    }

    [Fact]
    public void AddMongoDbTransactionManager_WithKeyAndFactory_RegistersKeyedTransactionManager()
    {
        var services = new ServiceCollection();
        const string key = "analytics";

        services.AddMongoDbTransactionManager(key, _ => _mockClient.Object);

        var provider = services.BuildServiceProvider();
        var manager = provider.GetKeyedService<IMongoTransactionManager>(key);

        Assert.NotNull(manager);
        Assert.IsType<MongoTransactionManager>(manager);
    }

    [Fact]
    public void AddMongoDbTransactionManager_WithKeyAndFactory_InvokesFactory()
    {
        var services = new ServiceCollection();
        const string key = "analytics";
        var factoryInvoked = false;

        services.AddMongoDbTransactionManager(key, _ =>
        {
            factoryInvoked = true;
            return _mockClient.Object;
        });

        var provider = services.BuildServiceProvider();
        _ = provider.GetRequiredKeyedService<IMongoTransactionManager>(key);

        Assert.True(factoryInvoked);
    }

    [Fact]
    public void AddMongoDbTransactionManager_WithKeyAndFactory_DoesNotRegisterDefaultServices()
    {
        var services = new ServiceCollection();
        const string key = "analytics";

        services.AddMongoDbTransactionManager(key, _ => _mockClient.Object);

        var provider = services.BuildServiceProvider();
        var defaultClient = provider.GetService<IMongoClient>();
        var defaultManager = provider.GetService<IMongoTransactionManager>();

        Assert.Null(defaultClient);
        Assert.Null(defaultManager);
    }

    private sealed class MarkerService;
}
