using System;
using Antlr4.Runtime;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using NUnit.Framework;

namespace GoogleApiDesign.ApiUtilities.Tests
{
    public class MongoFilterVisitorTests
    {
        public FilterParser.FilterContext Setup(string text)
        {
            var inputStream = new AntlrInputStream(text);
            var lexer = new FilterLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
           return new FilterParser(tokenStream).filter();
        }

        [TestCase("=", "{ id : 123 }")]
        [TestCase("<", "{ id : { $lt : 123 } }")]
        [TestCase(">=", "{ id : { $gte : 123 } }")]
        [TestCase(">", "{ id : { $gt : 123 } }")]
        [TestCase("!=", "{ id : { $ne : 123 } }")]
        [TestCase(":", "{ id : { $elemMatch : { $eq: 123 } } }")] 
        public void ShouldParseComparators(string op, string expected)
        {
            // Arrange
            var parser = Setup($"id{op}123");
            var visitor = new MongoFilterVisitor();

            // Act
            visitor.Visit(parser);

            // Assert
            var value = ConvertToString(visitor.GetFilter());
            value.Should().BeEquivalentTo(BsonDocument.Parse(expected));
        }

        private BsonDocument ConvertToString<T>(FilterDefinition<T> filterDefinition)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var bsonDocument = filterDefinition.Render(serializer, BsonSerializer.SerializerRegistry);
            return bsonDocument;
        }
    }
}