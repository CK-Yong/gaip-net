using System;
using AutoFixture;
using FluentAssertions;
using Gaip.Net.Core;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Gaip.Net.Mongo.Tests;

[TestFixture]
public class MongoFilterAdapterComponentTests
{
    private IMongoCollection<TestDocument> _mongoCollection;
    private Fixture _fixture;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        var url = MongoUrl.Create("mongodb://localhost:27017");
        var settings = MongoClientSettings.FromUrl(url);
        settings.ClusterConfigurator = cb => {
            cb.Subscribe<CommandStartedEvent>(e => {
                if (e.CommandName == "find")
                {
                    Console.WriteLine(e.Command.ToJson());
                }
            });
        };
        
        _mongoCollection = new MongoClient(settings)
            .GetDatabase("test")
            .GetCollection<TestDocument>("test");

        _fixture = new Fixture();
    }

    [Test]
    public void Should_consider_bsonId_attributes()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        _mongoCollection.InsertOne(new TestDocument { MyId = id });

        var filter = FilterBuilder
            .FromString($"MyId=\"{id}\"")
            .UseAdapter(new MongoFilterAdapter<TestDocument>())
            .Build();
        
        // Act
        var result = _mongoCollection.Find(filter).SingleOrDefault();
        
        // Assert
        result.Should().NotBeNull();
        result.MyId.Should().Be(id);
    }

    [Test]
    public void Should_consider_bsonElement_attributes()
    {
        // Arrange
        var testDocument = _fixture.Create<TestDocument>();
        
        _mongoCollection.InsertOne(testDocument);

        var filter = FilterBuilder
            .FromString($"MyElement={testDocument.MyElement}")
            .UseAdapter(new MongoFilterAdapter<TestDocument>())
            .Build();
        
        // Act
        var result = _mongoCollection.Find(filter).SingleOrDefault();
        
        // Assert
        result.Should().NotBeNull();
        result.MyId.Should().Be(testDocument.MyId);
    }
}

internal class TestDocument
{
    [BsonId]
    public string MyId { get; set; }
    
    [BsonElement("element_with_other_name")]
    public int MyElement { get; set; }
}