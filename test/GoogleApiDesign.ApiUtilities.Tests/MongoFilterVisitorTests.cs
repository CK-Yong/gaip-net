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
        private FilterParser.FilterContext Setup(string text)
        {
            var inputStream = new AntlrInputStream(text);
            var lexer = new FilterLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
           return new FilterParser(tokenStream).filter();
        }

        [TestCase("=", "{ foo : 123 }")]
        [TestCase("<", "{ foo : { $lt : 123 } }")]
        [TestCase("<=", "{ foo : { $lte : 123 } }")]
        [TestCase(">=", "{ foo : { $gte : 123 } }")]
        [TestCase(">", "{ foo : { $gt : 123 } }")]
        [TestCase("!=", "{ foo : { $ne : 123 } }")]
        [TestCase(":", "{ foo : { $elemMatch : { $eq: 123 } } }")]
        public void ShouldParseComparators(string op, string expected)
        {
            // Arrange
            var parser = Setup($"foo{op}123");
            var visitor = new MongoFilterVisitor();

            // Act
            visitor.Visit(parser);

            // Assert
            var value = ConvertToString(visitor.GetFilter());
            value.Should().BeEquivalentTo(BsonDocument.Parse(expected));
        }

        [TestCase("\"Some String\"", "{ foo : \"Some String\" }")]
        // Integral numbers
        [TestCase("123", "{ foo : 123 }")]
        [TestCase("1000000000000", "{ foo : NumberLong(1000000000000) }")]
        // Floating point numbers
        [TestCase("12.345", "{ foo : 12.345 }")]
        // Booleans
        [TestCase("true", "{ foo : true }")]
        [TestCase("false", "{ foo : false }")]
        // Timestamps
        [TestCase("2012-04-21T11:30:00-04:00", "{ foo : ISODate(\"2012-04-21T15:30:00\") }")]
        [TestCase("2012-04-21T11:30:00Z", "{ foo : ISODate(\"2012-04-21T11:30:00\") }")]
        [TestCase("2012-04-21T11:30:00", "{ foo : ISODate(\"2012-04-21T11:30:00\") }")]
        // Durations
        [TestCase("1s", "{ foo : 1000 }")] // Store as millis
        [TestCase("1.234s", "{ foo : 1234 }")] // Store as millis
        public void ShouldParseDataTypes(string input, string expected)
        {
            // Arrange
            var parser = Setup($"foo={input}");
            var visitor = new MongoFilterVisitor();

            // Act
            visitor.Visit(parser);

            // Assert
            var value = ConvertToString(visitor.GetFilter());
            value.Should().BeEquivalentTo(BsonDocument.Parse(expected));
        }

        [TestCase("-foo=bar", "{ foo : { $ne : \"bar\" }}")]
        [TestCase("NOT foo=bar", "{ foo : { $ne : \"bar\" }}")]
        [TestCase("NOT foo>bar", "{ foo : { $not : { $gt: \"bar\" }}}")]
        [TestCase("-foo<bar", "{ foo : { $not : { $lt: \"bar\" }}}")]
        [TestCase("NOT foo>=bar", "{ foo : { $not : { $gte: \"bar\" }}}")]
        public void ShouldParseNegationOperations(string text, string expectedQuery)
        {
            // Arrange
            var parser = Setup(text);
            var visitor = new MongoFilterVisitor();

            // Act
            visitor.Visit(parser);

            // Assert
            var value = ConvertToString(visitor.GetFilter());
            value.Should().BeEquivalentTo(BsonDocument.Parse(expectedQuery));
        }

        [TestCase("foo=bar AND foo!=baz", "{ $and : [ { foo : \"bar\" }, { foo : { $ne: \"baz\" } } ] }")]
        [TestCase("foo=bar AND temp<=100", "{ foo: \"bar\", temp: { $lte: 100 } } ")]
        [TestCase("foo=bar AND temp<=100 AND isDeleted=false", "{ foo: \"bar\", temp: { $lte: 100 }, isDeleted: false } ")]
        public void ShouldHandleAndOperators(string text, string expectedQuery)
        {
            // Arrange
            var parser = Setup(text);
            var visitor = new MongoFilterVisitor();

            // Act
            visitor.Visit(parser);

            // Assert
            var value = ConvertToString(visitor.GetFilter());
            value.Should().BeEquivalentTo(BsonDocument.Parse(expectedQuery));
        }

        [TestCase("foo=bar OR foo!=baz", "{ $or : [ { foo : \"bar\" }, { foo : { $ne: \"baz\" } } ] }")]
        [TestCase("foo=bar OR temp<=100", "{ $or: [{ foo: \"bar\" }, { temp: { $lte: 100 } } ] }")]
        [TestCase("foo=bar OR foo=\"baz\" OR isDeleted=false", "{ $or : [ { foo : \"bar\" }, { foo : \"baz\" }, { isDeleted: false } ] }")]
        public void ShouldHandleOrOperators(string text, string expectedQuery)
        {
            // Arrange
            var parser = Setup(text);
            var visitor = new MongoFilterVisitor();

            // Act
            visitor.Visit(parser);

            // Assert
            var value = ConvertToString(visitor.GetFilter());
            value.Should().BeEquivalentTo(BsonDocument.Parse(expectedQuery));
        }

        [TestCase("foo.bar=true", "{ \"foo.bar\": true }")]
        [TestCase("foo.bar>42", "{ \"foo.bar\": { $gt: 42 }}")]
        [TestCase("foo.bar.baz=\"foo\"", "{ \"foo.bar.baz\": \"foo\" }")]
        public void ShouldHandleTraversalOperations(string text, string expectedQuery)
        {
            // Arrange
            var parser = Setup(text);
            var visitor = new MongoFilterVisitor();

            // Act
            visitor.Visit(parser);

            // Assert
            var value = ConvertToString(visitor.GetFilter());
            value.Should().BeEquivalentTo(BsonDocument.Parse(expectedQuery));
        }

        private BsonDocument ConvertToString<T>(FilterDefinition<T> filterDefinition)
        {
            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<T>();
            var bsonDocument = filterDefinition.Render(serializer, BsonSerializer.SerializerRegistry);
            return bsonDocument;
        }
    }
}