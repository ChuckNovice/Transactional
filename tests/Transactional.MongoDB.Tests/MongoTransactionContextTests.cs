namespace Transactional.MongoDB.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using global::MongoDB.Driver;
using Moq;
using Xunit;

public class MongoTransactionContextTests
{
    private readonly Mock<IClientSessionHandle> _mockSession;

    public MongoTransactionContextTests()
    {
        _mockSession = new Mock<IClientSessionHandle>();
    }

    [Fact]
    public void Constructor_NullSession_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MongoTransactionContext(null!));
    }

    [Fact]
    public void Constructor_StartsTransaction()
    {
        _ = new MongoTransactionContext(_mockSession.Object);

        _mockSession.Verify(s => s.StartTransaction(null), Times.Once);
    }

    [Fact]
    public void Constructor_WithOptions_StartsTransactionWithOptions()
    {
        var options = new TransactionOptions();

        _ = new MongoTransactionContext(_mockSession.Object, options);

        _mockSession.Verify(s => s.StartTransaction(options), Times.Once);
    }

    [Fact]
    public void Session_ReturnsInjectedSession()
    {
        var context = new MongoTransactionContext(_mockSession.Object);

        Assert.Same(_mockSession.Object, context.Session);
    }

    [Fact]
    public async Task CommitAsync_CallsCommitTransactionAsync()
    {
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);

        await context.CommitAsync();

        _mockSession.Verify(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_PassesCancellationToken()
    {
        var cts = new CancellationTokenSource();
        _mockSession
            .Setup(s => s.CommitTransactionAsync(cts.Token))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);

        await context.CommitAsync(cts.Token);

        _mockSession.Verify(s => s.CommitTransactionAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_IsIdempotent()
    {
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);

        await context.CommitAsync();
        await context.CommitAsync();

        _mockSession.Verify(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CommitAsync_AfterRollbackAsync_ThrowsInvalidOperationException()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        await context.RollbackAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.CommitAsync());
    }

    [Fact]
    public async Task RollbackAsync_CallsAbortTransactionAsync()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);

        await context.RollbackAsync();

        _mockSession.Verify(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_PassesCancellationToken()
    {
        var cts = new CancellationTokenSource();
        _mockSession
            .Setup(s => s.AbortTransactionAsync(cts.Token))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);

        await context.RollbackAsync(cts.Token);

        _mockSession.Verify(s => s.AbortTransactionAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_IsIdempotent()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);

        await context.RollbackAsync();
        await context.RollbackAsync();

        _mockSession.Verify(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RollbackAsync_AfterCommitAsync_ThrowsInvalidOperationException()
    {
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        await context.CommitAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.RollbackAsync());
    }

    [Fact]
    public async Task DisposeAsync_WhenNotCommittedOrRolledBack_AbortsTransaction()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);

        await context.DisposeAsync();

        _mockSession.Verify(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_DisposesSession()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);

        await context.DisposeAsync();

        _mockSession.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_AfterCommit_DoesNotAbort()
    {
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        await context.CommitAsync();

        await context.DisposeAsync();

        _mockSession.Verify(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DisposeAsync_AfterRollback_DoesNotAbortAgain()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        await context.RollbackAsync();

        await context.DisposeAsync();

        _mockSession.Verify(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_IsIdempotent()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);

        await context.DisposeAsync();
        await context.DisposeAsync();

        _mockSession.Verify(s => s.Dispose(), Times.Once);
    }

    // State property tests

    [Fact]
    public void IsCommitted_Initially_IsFalse()
    {
        var context = new MongoTransactionContext(_mockSession.Object);

        Assert.False(context.IsCommitted);
    }

    [Fact]
    public async Task IsCommitted_AfterCommit_IsTrue()
    {
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        await context.CommitAsync();

        Assert.True(context.IsCommitted);
    }

    [Fact]
    public void IsRolledBack_Initially_IsFalse()
    {
        var context = new MongoTransactionContext(_mockSession.Object);

        Assert.False(context.IsRolledBack);
    }

    [Fact]
    public async Task IsRolledBack_AfterRollback_IsTrue()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        await context.RollbackAsync();

        Assert.True(context.IsRolledBack);
    }

    // OnCommitted callback tests

    [Fact]
    public void OnCommitted_NullCallback_ThrowsArgumentNullException()
    {
        var context = new MongoTransactionContext(_mockSession.Object);

        Assert.Throws<ArgumentNullException>(() => context.OnCommitted(null!));
    }

    [Fact]
    public async Task OnCommitted_AfterCommit_ThrowsInvalidOperationException()
    {
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        await context.CommitAsync();

        Assert.Throws<InvalidOperationException>(() => context.OnCommitted(_ => Task.CompletedTask));
    }

    [Fact]
    public async Task OnCommitted_AfterRollback_ThrowsInvalidOperationException()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        await context.RollbackAsync();

        Assert.Throws<InvalidOperationException>(() => context.OnCommitted(_ => Task.CompletedTask));
    }

    [Fact]
    public async Task CommitAsync_InvokesOnCommittedCallbacks()
    {
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        var callbackInvoked = false;
        context.OnCommitted(_ =>
        {
            callbackInvoked = true;
            return Task.CompletedTask;
        });

        await context.CommitAsync();

        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task CommitAsync_InvokesMultipleCallbacksInOrder()
    {
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        var order = new System.Collections.Generic.List<int>();
        context.OnCommitted(_ => { order.Add(1); return Task.CompletedTask; });
        context.OnCommitted(_ => { order.Add(2); return Task.CompletedTask; });
        context.OnCommitted(_ => { order.Add(3); return Task.CompletedTask; });

        await context.CommitAsync();

        Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    [Fact]
    public async Task CommitAsync_PassesCancellationTokenToCallbacks()
    {
        var cts = new CancellationTokenSource();
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        CancellationToken receivedToken = default;
        context.OnCommitted(ct =>
        {
            receivedToken = ct;
            return Task.CompletedTask;
        });

        await context.CommitAsync(cts.Token);

        Assert.Equal(cts.Token, receivedToken);
    }

    [Fact]
    public async Task CommitAsync_CallbackThrows_TransactionStillCommitted()
    {
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        context.OnCommitted(_ => throw new InvalidOperationException("Callback failed"));

        var ex = await Assert.ThrowsAsync<AggregateException>(() => context.CommitAsync());

        Assert.True(context.IsCommitted);
        Assert.Single(ex.InnerExceptions);
    }

    [Fact]
    public async Task CommitAsync_MultipleCallbacksThrow_AllExceptionsCollected()
    {
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        context.OnCommitted(_ => throw new InvalidOperationException("First"));
        context.OnCommitted(_ => throw new InvalidOperationException("Second"));

        var ex = await Assert.ThrowsAsync<AggregateException>(() => context.CommitAsync());

        Assert.Equal(2, ex.InnerExceptions.Count);
    }

    [Fact]
    public async Task CommitAsync_CallbackThrows_AllCallbacksStillInvoked()
    {
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        var secondCallbackInvoked = false;
        context.OnCommitted(_ => throw new InvalidOperationException("First"));
        context.OnCommitted(_ => { secondCallbackInvoked = true; return Task.CompletedTask; });

        await Assert.ThrowsAsync<AggregateException>(() => context.CommitAsync());

        Assert.True(secondCallbackInvoked);
    }

    // OnRolledBack callback tests

    [Fact]
    public void OnRolledBack_NullCallback_ThrowsArgumentNullException()
    {
        var context = new MongoTransactionContext(_mockSession.Object);

        Assert.Throws<ArgumentNullException>(() => context.OnRolledBack(null!));
    }

    [Fact]
    public async Task OnRolledBack_AfterRollback_ThrowsInvalidOperationException()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        await context.RollbackAsync();

        Assert.Throws<InvalidOperationException>(() => context.OnRolledBack(_ => Task.CompletedTask));
    }

    [Fact]
    public async Task OnRolledBack_AfterCommit_ThrowsInvalidOperationException()
    {
        _mockSession
            .Setup(s => s.CommitTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        await context.CommitAsync();

        Assert.Throws<InvalidOperationException>(() => context.OnRolledBack(_ => Task.CompletedTask));
    }

    [Fact]
    public async Task RollbackAsync_InvokesOnRolledBackCallbacks()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        var callbackInvoked = false;
        context.OnRolledBack(_ =>
        {
            callbackInvoked = true;
            return Task.CompletedTask;
        });

        await context.RollbackAsync();

        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task RollbackAsync_InvokesMultipleCallbacksInOrder()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        var order = new System.Collections.Generic.List<int>();
        context.OnRolledBack(_ => { order.Add(1); return Task.CompletedTask; });
        context.OnRolledBack(_ => { order.Add(2); return Task.CompletedTask; });
        context.OnRolledBack(_ => { order.Add(3); return Task.CompletedTask; });

        await context.RollbackAsync();

        Assert.Equal(new[] { 1, 2, 3 }, order);
    }

    [Fact]
    public async Task RollbackAsync_CallbackThrows_TransactionStillRolledBack()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        context.OnRolledBack(_ => throw new InvalidOperationException("Callback failed"));

        var ex = await Assert.ThrowsAsync<AggregateException>(() => context.RollbackAsync());

        Assert.True(context.IsRolledBack);
        Assert.Single(ex.InnerExceptions);
    }

    // Implicit rollback does not invoke callbacks

    [Fact]
    public async Task DisposeAsync_ImplicitRollback_DoesNotInvokeCallbacks()
    {
        _mockSession
            .Setup(s => s.AbortTransactionAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var context = new MongoTransactionContext(_mockSession.Object);
        var callbackInvoked = false;
        context.OnRolledBack(_ =>
        {
            callbackInvoked = true;
            return Task.CompletedTask;
        });

        await context.DisposeAsync();

        Assert.False(callbackInvoked);
    }
}
