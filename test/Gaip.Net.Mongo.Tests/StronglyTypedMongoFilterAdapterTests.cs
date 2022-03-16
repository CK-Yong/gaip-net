using FluentAssertions;
using Gaip.Net.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NUnit.Framework;

namespace Gaip.Net.Mongo.Tests;

[TestFixture]
public class StronglyTypedMongoFilterAdapterTests
{
    [Test]
    public void Should_consider_bsonId_attributes()
    {
        // Arrange
        var filter = FilterBuilder
            .FromString("MyId=abc123")
            .UseAdapter(new MongoFilterAdapter<TestDocument>());

        // Act
        var query = filter.Build();
        
        // Assert
        var value = query.ConvertToBsonDocument();
        value.Should().BeEquivalentTo(BsonDocument.Parse("{ \"_id\" : \"abc123\" }"));
    }

    [Test]
    public void Should_consider_bsonElement_attributes()
    {
        // Arrange
        var filter = FilterBuilder
            .FromString($"MyElement=100")
            .UseAdapter(new MongoFilterAdapter<TestDocument>());

        // Act
        var query = filter.Build();

        // Assert
        var value = query.ConvertToBsonDocument();
        value.Should().BeEquivalentTo(BsonDocument.Parse("{ \"element_with_other_name\" : 100 }"));
    }
}

internal class TestDocument
{
    [BsonId]
    public string MyId { get; set; }
    
    [BsonElement("element_with_other_name")]
    public int MyElement { get; set; }
}