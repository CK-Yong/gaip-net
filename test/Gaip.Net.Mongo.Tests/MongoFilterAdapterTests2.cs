using System.Collections.Generic;
using System.Threading.Tasks;
using Gaip.Net.Core.Tests;
using MongoDB.Driver;
using NUnit.Framework;

namespace Gaip.Net.Mongo.Tests;

public class MongoFilterAdapterTests2
    : GaipTestSuite<MongoFilterAdapter<TestClass>, FilterDefinition<TestClass>>
{
    private IMongoCollection<TestClass> _mongoCollection;

    public override Task SetupAsync()
    {
        var connectionString = "mongodb://localhost:27017";
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase("test");
        _mongoCollection = database.GetCollection<TestClass>("test");
        _mongoCollection.DeleteMany(FilterDefinition<TestClass>.Empty);

        return Task.CompletedTask;
    }

    public override Task InsertManyAsync(TestClass[] testClasses)
        => _mongoCollection.InsertManyAsync(testClasses);

    public override async Task<IList<TestClass>> UseFilterResultAsync(FilterDefinition<TestClass> result)
        => await (await _mongoCollection.FindAsync(result)).ToListAsync();
}