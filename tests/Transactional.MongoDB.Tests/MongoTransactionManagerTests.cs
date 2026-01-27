namespace Transactional.MongoDB.Tests
{
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
    }
}
