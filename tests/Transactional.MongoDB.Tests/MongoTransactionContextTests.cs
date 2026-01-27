namespace Transactional.MongoDB.Tests
{
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
    }
}
