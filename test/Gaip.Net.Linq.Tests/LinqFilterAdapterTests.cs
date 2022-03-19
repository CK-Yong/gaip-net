using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Gaip.Net.Core;
using NUnit.Framework;

namespace Gaip.Net.Linq.Tests;

[TestFixture]
public class Tests
{
    [SetUp]
    public void Setup()
    {
        
    }

    [TestCase("Id=3", new[] { 3 })]
    [TestCase("Id!=3", new[] { 1, 2, 4, 5 })]
    [TestCase("Id<4", new[] { 1, 2, 3 })]
    [TestCase("Id<=4", new[] { 1, 2, 3, 4 })]
    [TestCase("Id>2", new[] { 3, 4, 5 })]
    [TestCase("Id>=2", new[] { 2, 3, 4, 5 })]
    public void Should_convert_operators_to_expression_logic(string text, int[] expectedIds)
    {
        // Arrange
        var data = new List<TestClass>
        {
            new(){Id = 1},
            new(){Id = 2},
            new(){Id = 3},
            new(){Id = 4},
            new(){Id = 5}
        };

        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new LinqFilterAdapter<TestClass>())
            .Build();
        
        // Act
        var results = data.Where(filter).ToList();
        
        // Assert
        for (var i = 0; i < expectedIds.Length; i++)
        {
            results[i].Id.Should().Be(expectedIds[i]);
        }
    }

    [TestCase("Foo.Bar=\"baz\"")]
    [TestCase("Foo.Fizz.Buzz=\"baz\"")]
    [TestCase("Foo.Fizz.Buzz!=\"nonExistent\"")]
    [TestCase("Foo.Fizz.Integer<100")]
    [TestCase("Foo.Fizz.Integer<=100")]
    [TestCase("Foo.Fizz.Integer>98")]
    [TestCase("Foo.Fizz.Integer>=98")]
    public void Should_handle_traversal_operators(string text)
    {
        // Arrange
        var data = new List<TestClass>
        {
            new() { Id = 1, Foo = new Nested { Bar = "baz", Fizz = new Nested { Buzz = "baz", Integer = 99 } } },
            new() { Id = 2, Foo = new Nested { Bar = "fizz" } },
            new() { Id = 3, Foo = new Nested { Bar = "buzz" } }
        };
        
        // Act
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new LinqFilterAdapter<TestClass>())
            .Build();
        
        // Assert
        data.Where(filter).Single().Id.Should().Be(1);
    }

    [TestCase("Integers:42")]
    [TestCase("Array.Integer:42")]
    //todo: data.Where(x => x.Array.Any(y => y.Integer == 42))
    public void Should_handle_has_operator_for_arrays(string text)
    {
        // Arrange
        var data = new List<TestClass>
        {
            new() { Id = 1, Array = new Nested[] { new() { Integer = 42 } }, Integers = new[] { 42 } },
            new() { Id = 2, Array = new Nested[] { new() { Integer = 66 } }, Integers = new[] { 66, 123 } },
            new() { Id = 3, Array = new Nested[] { new() { Integer = 123 } }, Integers = Array.Empty<int>()}
        };

        // Act
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new LinqFilterAdapter<TestClass>())
            .Build();
        
        // Assert
        data.Where(filter).Single().Id.Should().Be(1);
    }
    
    [TestCase("Foo:Bar")]   // Bar must be non-default.
    [TestCase("Foo.Bar:*")] // Same as above.
    [TestCase("Foo.Bar:42")]
    public void Should_handle_has_operator_for_objects(string text)
    {
        
    }
    
    
}

public class TestClass
{
    public int Id { get; set; }
    public Nested Foo { get; set; }
    
    public Nested[] Array { get; set; }
    
    public int[] Integers { get; set; }
}

public class Nested
{
    public string Bar { get; set; }
    public Nested Fizz { get; set; }
    public string Buzz { get; set; }
    public int Integer { get; set; }
}
