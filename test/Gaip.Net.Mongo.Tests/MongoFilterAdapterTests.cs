using System;
using FluentAssertions;
using Gaip.Net.Core;
using MongoDB.Bson;
using NUnit.Framework;

namespace Gaip.Net.Mongo.Tests;

[TestFixture]
public class MongoFilterVisitorTests
{
    [TestCase("=", "{ foo : 123 }")]
    [TestCase("<", "{ foo : { $lt : 123 } }")]
    [TestCase("<=", "{ foo : { $lte : 123 } }")]
    [TestCase(">=", "{ foo : { $gte : 123 } }")]
    [TestCase(">", "{ foo : { $gt : 123 } }")]
    [TestCase("!=", "{ foo : { $ne : 123 } }")]
    [TestCase(":", "{ foo : { $elemMatch : { $eq: 123 } } }")]
    public void Should_parse_comparators(string op, string expected)
    {
        // Arrange
        var filter = FilterBuilder
            .FromString($"foo{op}123")
            .UseAdapter(new MongoFilterAdapter<object>());

        // Act
        var fieldDefinition = filter.Build();

        // Assert
        var value = fieldDefinition.ConvertToBsonDocument();
        value.Should().BeEquivalentTo(BsonDocument.Parse(expected));
    }

    [TestCase("\"Some String\"", "{ foo : \"Some String\" }")]
    [TestCase("\'Some String\'", "{ foo : \"Some String\" }")]
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
    public void Should_parse_data_types(string input, string expected)
    {
        // Arrange
        var filter = FilterBuilder
            .FromString($"foo={input}")
            .UseAdapter(new MongoFilterAdapter<object>());

        // Act
        var fieldDefinition = filter.Build();

        // Assert
        var value = fieldDefinition.ConvertToBsonDocument();
        value.Should().BeEquivalentTo(BsonDocument.Parse(expected));
    }

    [TestCase("-foo=\"bar\"", "{ foo : { $ne : \"bar\" }}")]
    [TestCase("NOT foo=\"bar\"", "{ foo : { $ne : \"bar\" }}")]
    [TestCase("NOT foo>25", "{ foo : { $not : { $gt: 25 }}}")]
    [TestCase("-foo<25", "{ foo : { $not : { $lt: 25 }}}")]
    [TestCase("NOT foo>=25", "{ foo : { $not : { $gte: 25}}}")]
    public void Should_handle_negation_operators(string text, string expectedQuery)
    {
        // Arrange
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new MongoFilterAdapter<object>());

        // Act
        var fieldDefinition = filter.Build();

        // Assert
        var value = fieldDefinition.ConvertToBsonDocument();
        value.Should().BeEquivalentTo(BsonDocument.Parse(expectedQuery));
    }

    [TestCase("foo=\"bar\" AND foo!=\"baz\"", "{ $and : [ { foo : \"bar\" }, { foo : { $ne: \"baz\" } } ] }")]
    [TestCase("foo=\"bar\" AND temp<=100", "{ foo: \"bar\", temp: { $lte: 100 } } ")]
    [TestCase("foo=\"bar\" AND temp<=100 AND isDeleted=false",
        "{ foo: \"bar\", temp: { $lte: 100 }, isDeleted: false } ")]
    public void Should_handle_and_operators(string text, string expectedQuery)
    {
        // Arrange
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new MongoFilterAdapter<object>());

        // Act
        var fieldDefinition = filter.Build();

        // Assert
        var value = fieldDefinition.ConvertToBsonDocument();
        value.Should().BeEquivalentTo(BsonDocument.Parse(expectedQuery));
    }

    [TestCase("foo=\"bar\" OR foo!=\"baz\"", "{ $or : [ { foo : \"bar\" }, { foo : { $ne: \"baz\" } } ] }")]
    [TestCase("foo=\"bar\" OR temp<=100", "{ $or: [{ foo: \"bar\" }, { temp: { $lte: 100 } } ] }")]
    [TestCase("foo=\"bar\" OR foo=\"baz\" OR isDeleted=false", "{ $or : [ { foo : \"bar\" }, { foo : \"baz\" }, { isDeleted: false } ] }")]
    public void Should_handle_or_operators(string text, string expectedQuery)
    {
        // Arrange
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new MongoFilterAdapter<object>());

        // Act
        var fieldDefinition = filter.Build();

        // Assert
        var value = fieldDefinition.ConvertToBsonDocument();
        value.Should().BeEquivalentTo(BsonDocument.Parse(expectedQuery));
    }

    [TestCase("foo.bar=true", "{ \"foo.bar\": true }")]
    [TestCase("foo.bar>42", "{ \"foo.bar\": { $gt: 42 }}")]
    [TestCase("foo.bar.baz=\"foo\"", "{ \"foo.bar.baz\": \"foo\" }")]
    public void Should_handle_traversal_operations(string text, string expectedQuery)
    {
        // Arrange
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new MongoFilterAdapter<object>());

        // Act
        var fieldDefinition = filter.Build();

        // Assert
        var value = fieldDefinition.ConvertToBsonDocument();
        value.Should().BeEquivalentTo(BsonDocument.Parse(expectedQuery));
    }

    [TestCase("foo=\"bar*\"", "{ foo : { $regex: \"^bar\" } }")]
    [TestCase("foo=\"*bar\"", "{ foo : { $regex: \"bar$\" } }")]
    public void Should_handle_wildCard_searches(string text, string expectedQuery)
    {
        // Arrange
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new MongoFilterAdapter<object>());

        // Act
        var fieldDefinition = filter.Build();

        // Assert
        var value = fieldDefinition.ConvertToBsonDocument();
        value.Should().BeEquivalentTo(BsonDocument.Parse(expectedQuery));
    }

    [TestCase("foo.0.bar=\"baz\"")]
    [TestCase("foo[0].bar=\"baz\"")]
    public void Should_reject_array_accessors(string text)
    {
        // Arrange
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new MongoFilterAdapter<object>());

        // Act
        Action act = () => filter.Build();

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Array accessors are not allowed. *");
    }
}