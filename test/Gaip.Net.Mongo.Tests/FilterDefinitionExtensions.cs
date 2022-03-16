using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Gaip.Net.Mongo.Tests;

public static class FilterDefinitionExtensions
{
    internal static BsonDocument ConvertToBsonDocument<T>(this FilterDefinition<T> filterDefinition)
    {
        var serializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
        var bsonDocument = filterDefinition.Render(serializer, BsonSerializer.SerializerRegistry);
        return bsonDocument;
    }
}