namespace Transactional.MongoDB.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Driver;
using Moq;
using Xunit;

public class MongoTransactionManagerTests
{
    private readonly Mock<IMongoClient> _mockClient;
    private readonly Mock<IClientSessionHandle> _mockSession;

    public MongoTransactionManagerTests()
    {
        _mockClient = new Mock<IMongoClient>();
        _mockSession = new Mock<IClientSessionHandle>();
    }

    [Fact]
    public void Constructor_NullClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MongoTransactionManager(null!));
    }

    [Fact]
    public async Task BeginTransactionAsync_CallsStartSessionAsync()
    {
        _mockClient
            .Setup(c => c.StartSessionAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockSession.Object);

        var manager = new MongoTransactionManager(_mockClient.Object);

        await manager.BeginTransactionAsync();

        _mockClient.Verify(c => c.StartSessionAsync(null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BeginTransactionAsync_PassesCancellationToken()
    {
        var cts = new CancellationTokenSource();
        _mockClient
            .Setup(c => c.StartSessionAsync(null, cts.Token))
            .ReturnsAsync(_mockSession.Object);

        var manager = new MongoTransactionManager(_mockClient.Object);

        await manager.BeginTransactionAsync(cancellationToken: cts.Token);

        _mockClient.Verify(c => c.StartSessionAsync(null, cts.Token), Times.Once);
    }

    [Fact]
    public async Task BeginTransactionAsync_ReturnsMongoTransactionContext()
    {
        _mockClient
            .Setup(c => c.StartSessionAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockSession.Object);

        var manager = new MongoTransactionManager(_mockClient.Object);

        var result = await manager.BeginTransactionAsync();

        Assert.NotNull(result);
        Assert.IsType<MongoTransactionContext>(result);
    }

    [Fact]
    public async Task BeginTransactionAsync_ContextHasSession()
    {
        _mockClient
            .Setup(c => c.StartSessionAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockSession.Object);

        var manager = new MongoTransactionManager(_mockClient.Object);

        var result = await manager.BeginTransactionAsync();

        Assert.Same(_mockSession.Object, result.Session);
    }

    [Fact]
    public async Task BeginTransactionAsync_WithOptions_PassesOptionsToContext()
    {
        var options = new TransactionOptions();
        _mockClient
            .Setup(c => c.StartSessionAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mockSession.Object);

        var manager = new MongoTransactionManager(_mockClient.Object);

        await manager.BeginTransactionAsync(options);

        _mockSession.Verify(s => s.StartTransaction(options), Times.Once);
    }

    // WrapExistingTransaction tests

    [Fact]
    public void WrapExistingTransaction_NullSession_ThrowsArgumentNullException()
    {
        var manager = new MongoTransactionManager(_mockClient.Object);

        Assert.Throws<ArgumentNullException>(() => manager.WrapExistingTransaction(null!));
    }

    [Fact]
    public void WrapExistingTransaction_SessionNotInTransaction_ThrowsArgumentException()
    {
        _mockSession.Setup(s => s.IsInTransaction).Returns(false);

        var manager = new MongoTransactionManager(_mockClient.Object);

        var ex = Assert.Throws<ArgumentException>(() => manager.WrapExistingTransaction(_mockSession.Object));
        Assert.Contains("not in an active transaction", ex.Message);
    }

    [Fact]
    public void WrapExistingTransaction_ValidSession_ReturnsContext()
    {
        _mockSession.Setup(s => s.IsInTransaction).Returns(true);

        var manager = new MongoTransactionManager(_mockClient.Object);

        var result = manager.WrapExistingTransaction(_mockSession.Object);

        Assert.NotNull(result);
        Assert.IsType<MongoTransactionContext>(result);
    }

    [Fact]
    public void WrapExistingTransaction_ContextHasCorrectSession()
    {
        _mockSession.Setup(s => s.IsInTransaction).Returns(true);

        var manager = new MongoTransactionManager(_mockClient.Object);

        var result = manager.WrapExistingTransaction(_mockSession.Object);

        Assert.Same(_mockSession.Object, result.Session);
    }

    [Fact]
    public void WrapExistingTransaction_DoesNotStartTransaction()
    {
        _mockSession.Setup(s => s.IsInTransaction).Returns(true);

        var manager = new MongoTransactionManager(_mockClient.Object);

        _ = manager.WrapExistingTransaction(_mockSession.Object);

        _mockSession.Verify(s => s.StartTransaction(It.IsAny<TransactionOptions>()), Times.Never);
    }

    [Fact]
    public async Task WrapExistingTransaction_CommitStillWorks()
    {
        _mockSession.Setup(s => s.IsInTransaction).Returns(true);
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new MongoTransactionManager(_mockClient.Object);
        var context = manager.WrapExistingTransaction(_mockSession.Object);

        await context.CommitAsync();

        _mockSession.Verify(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WrapExistingTransaction_DisposeDoesNotDisposeSession()
    {
        _mockSession.Setup(s => s.IsInTransaction).Returns(true);
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new MongoTransactionManager(_mockClient.Object);
        var context = manager.WrapExistingTransaction(_mockSession.Object);
        await context.CommitAsync();

        await context.DisposeAsync();

        _mockSession.Verify(s => s.Dispose(), Times.Never);
    }

    [Fact]
    public async Task WrapExistingTransaction_DisposeStillAbortsIfNeeded()
    {
        _mockSession.Setup(s => s.IsInTransaction).Returns(true);
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var manager = new MongoTransactionManager(_mockClient.Object);
        var context = manager.WrapExistingTransaction(_mockSession.Object);

        await context.DisposeAsync();

        _mockSession.Verify(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockSession.Verify(s => s.Dispose(), Times.Never);
    }
}
