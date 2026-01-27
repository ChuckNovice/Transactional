namespace Transactional.MongoDB.Tests.Integration;

using System;
using System.Threading.Tasks;
using global::MongoDB.Bson;
using global::MongoDB.Driver;
using Xunit;

[Trait("Category", "Integration")]
public class MongoIntegrationTests : IAsyncLifetime
{
    private static readonly string? ConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
    private IMongoClient? _client;
    private IMongoDatabase? _database;
    private string _collectionName = default!;

    public async Task InitializeAsync()
    {
        if (string.IsNullOrEmpty(ConnectionString))
        {
            return;
        }

        _client = new MongoClient(ConnectionString);
        _database = _client.GetDatabase("transactional_tests");
        _collectionName = $"test_{Guid.NewGuid():N}";

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_database != null)
        {
            await _database.DropCollectionAsync(_collectionName);
        }
    }

    private void SkipIfNoConnectionString()
    {
        Skip.If(string.IsNullOrEmpty(ConnectionString), "Integration tests require MONGODB_CONNECTION_STRING environment variable.");
    }

    [SkippableFact]
    public async Task CommitAsync_PersistsDataToDatabase()
    {
        SkipIfNoConnectionString();

        var collection = _database!.GetCollection<BsonDocument>(_collectionName);
        var manager = new MongoTransactionManager(_client!);

        await using (var context = await manager.BeginTransactionAsync())
        {
            await collection.InsertOneAsync(context.Session, new BsonDocument("name", "test"));
            await context.CommitAsync();
        }

        var count = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        Assert.Equal(1, count);
    }

    [SkippableFact]
    public async Task RollbackAsync_DiscardsChanges()
    {
        SkipIfNoConnectionString();

        var collection = _database!.GetCollection<BsonDocument>(_collectionName);
        var manager = new MongoTransactionManager(_client!);

        await using (var context = await manager.BeginTransactionAsync())
        {
            await collection.InsertOneAsync(context.Session, new BsonDocument("name", "test"));
            await context.RollbackAsync();
        }

        var count = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        Assert.Equal(0, count);
    }

    [SkippableFact]
    public async Task DisposeAsync_WithoutCommit_RollsBack()
    {
        SkipIfNoConnectionString();

        var collection = _database!.GetCollection<BsonDocument>(_collectionName);
        var manager = new MongoTransactionManager(_client!);

        await using (var context = await manager.BeginTransactionAsync())
        {
            await collection.InsertOneAsync(context.Session, new BsonDocument("name", "test"));
            // Not committing - should rollback on dispose
        }

        var count = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        Assert.Equal(0, count);
    }

    [SkippableFact]
    public async Task MultipleOperations_InSingleTransaction_SucceedAtomically()
    {
        SkipIfNoConnectionString();

        var collection = _database!.GetCollection<BsonDocument>(_collectionName);
        var manager = new MongoTransactionManager(_client!);

        await using (var context = await manager.BeginTransactionAsync())
        {
            await collection.InsertOneAsync(context.Session, new BsonDocument("name", "doc1"));
            await collection.InsertOneAsync(context.Session, new BsonDocument("name", "doc2"));
            await collection.InsertOneAsync(context.Session, new BsonDocument("name", "doc3"));
            await context.CommitAsync();
        }

        var count = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
        Assert.Equal(3, count);
    }

    [SkippableFact]
    public async Task Session_CanBeUsedForOperations()
    {
        SkipIfNoConnectionString();

        var collection = _database!.GetCollection<BsonDocument>(_collectionName);
        var manager = new MongoTransactionManager(_client!);

        await using (var context = await manager.BeginTransactionAsync())
        {
            Assert.NotNull(context.Session);
            Assert.True(context.Session.IsInTransaction);

            await collection.InsertOneAsync(context.Session, new BsonDocument("name", "test"));
            await context.CommitAsync();
        }
    }
}
