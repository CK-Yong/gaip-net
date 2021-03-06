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

    [TestCase("Integers:42", 1)]
    [TestCase("Array.Integer:66", 2)]
    [TestCase("Array.Fizz.Bar:\"hello world\"", 3)]
    [TestCase("Array.Array.Bar:\"hello bar\"", 3)]
    [TestCase("IntegerList:3", 3)]
    public void Should_handle_has_operator_for_arrays(string text, int expectedId)
    {
        // Arrange
        var data = new List<TestClass>
        {
            new()
            {
                Id = 1, Array = new Nested[] { new() { Integer = 42, Fizz = new() } }, Integers = new[] { 42 }
            },
            new()
            {
                Id = 2, Array = new Nested[] { new() { Integer = 66, Fizz = new() } }, Integers = new[] { 66, 123 }
            },
            new()
            {
                Id = 3,
                Array = new Nested[]
                {
                    new()
                    {
                        Integer = 123, Fizz = new() { Bar = "hello world" },
                        Array = new[] { new Nested { Bar = "hello bar" } }
                    }
                },
                Integers = Array.Empty<int>(),
                IntegerList = new List<int>{1, 2, 3}
            }
        };

        // Act
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new LinqFilterAdapter<TestClass>())
            .Build();
        
        // Assert
        data.Where(filter).Single().Id.Should().Be(expectedId);
    }
    
    [TestCase("Foo:Bar")]   // Bar must be non-default.
    [TestCase("Foo.Bar:*")] // Same as above.
    [TestCase("Foo.Integer:42")]
    [TestCase("Foo.Bar:\"baz\"")]
    [TestCase("Foo.Fizz.Buzz:\"baz\"")]
    public void Should_handle_has_operator_for_objects(string text)
    {
        // Arrange
        var data = new List<TestClass>
        {
            new()
            {
                Id = 1, Foo = new Nested { Integer = 42, Bar = "baz", Fizz = new Nested { Buzz = "baz" } }
            },
            new()
            {
                Id = 2
            },
            new()
            {
                Id = 3
            }
        };

        // Act
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new LinqFilterAdapter<TestClass>())
            .Build();
        
        // Assert
        data.Where(filter).Single().Id.Should().Be(1);
    }

    [TestCase("Foo.Bar=\"baz\" AND Fizz=\"buzz\"", new []{1,2})]
    [TestCase("Fizz=\"buzz\" AND Foo.Integer>=42", new []{1})]
    [TestCase("Fizz=\"buzz\" AND Foo.Integer>=42 AND Foo.Fizz.Buzz=\"baz\"", new []{1})]
    public void Should_handle_and_operators(string text, int[] expectedIds)
    {
        // Arrange
        var data = new List<TestClass>
        {
            new()
            {
                Id = 1, Foo = new Nested { Integer = 42, Bar = "baz", Fizz = new Nested { Buzz = "baz" } }, Fizz = "buzz"
            },
            new()
            {
                Id = 2, Foo = new Nested { Bar = "baz" }, Fizz = "buzz"
            },
            new()
            {
                Id = 3
            }
        };
        
        // Act
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new LinqFilterAdapter<TestClass>())
            .Build();
        
        // Assert
        data.Where(filter).Select(x => x.Id).Should().BeEquivalentTo(expectedIds);
    }
    
    [TestCase("Foo.Bar=\"baz\" OR Fizz=\"buzz\"", new []{1,2})]
    [TestCase("Fizz=\"buzz\" OR Foo.Integer>=42", new []{1,2})]
    [TestCase("Fizz=\"buzz\" OR Foo.Integer>=42 OR Foo.Fizz.Buzz=\"baz\"", new []{1,2,3})]
    public void Should_handle_or_operators(string text, int[] expectedIds)
    {
        // Arrange
        var data = new List<TestClass>
        {
            new()
            {
                Id = 1, Foo = new Nested { Integer = 42, Bar = "baz", Fizz = new Nested { Buzz = "baz" } }
            },
            new()
            {
                Id = 2, Fizz = "buzz"
            },
            new()
            {
                Id = 3, Foo = new Nested { Fizz = new Nested { Buzz = "baz" } } 
            }
        };
        
        // Act
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new LinqFilterAdapter<TestClass>())
            .Build();
        
        // Assert
        data.Where(filter).Select(x => x.Id).Should().BeEquivalentTo(expectedIds);
    }   
    
    [TestCase("-Fizz=\"baz\"", new []{2, 3} )]
    [TestCase("NOT Fizz=\"baz\"", new []{2, 3})]
    [TestCase("NOT Foo.Integer>25", new []{3})]
    [TestCase("-Foo.Integer<25", new []{1})]
    [TestCase("NOT Foo.Integer>=42", new []{3})]
    public void Should_handle_negation_operators(string text, int[] expectedIds)
    {
        // Arrange
        var data = new List<TestClass>
        {
            new()
            {
                Id = 1, Foo = new Nested { Integer = 42, Bar = "baz", Fizz = new Nested { Buzz = "baz" } }, Fizz = "baz"
            },
            new()
            {
                Id = 2, Fizz = "buzz"
            },
            new()
            {
                Id = 3, Foo = new Nested { Bar = "not-baz", Fizz = new Nested { Buzz = "baz" } } 
            }
        };
        
        // Act
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new LinqFilterAdapter<TestClass>())
            .Build();
        
        // Assert
        data.Where(filter).Select(x => x.Id).Should().BeEquivalentTo(expectedIds);
    }    
    
    [TestCase("Fizz=\"baz*\"", new []{1} )]
    [TestCase("Foo.Bar=\"baz*\"", new []{1})]
    [TestCase("Foo.Fizz.Buzz=\"baz*\"", new []{1, 3})]
    public void Should_handle_prefix_search(string text, int[] expectedIds)
    {
        // Arrange
        var data = new List<TestClass>
        {
            new()
            {
                Id = 1, Foo = new Nested { Bar = "baz", Fizz = new Nested { Buzz = "bazinga" } }, Fizz = "bazzilian"
            },
            new()
            {
                Id = 2, Fizz = "buzz"
            },
            new()
            {
                Id = 3, Foo = new Nested { Bar = "not-baz", Fizz = new Nested { Buzz = "bazedGod" } } 
            }
        };
        
        // Act
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new LinqFilterAdapter<TestClass>())
            .Build();
        
        // Assert
        data.Where(filter).Select(x => x.Id).Should().BeEquivalentTo(expectedIds);
    }    
    
    [TestCase("Fizz=\"*.baz\"", new []{1} )]
    [TestCase("Foo.Bar=\"*baz\"", new []{1})]
    [TestCase("Foo.Fizz.Buzz=\"*baz\"", new []{1, 3})]
    public void Should_handle_suffix_search(string text, int[] expectedIds)
    {
        // Arrange
        var data = new List<TestClass>
        {
            new()
            {
                Id = 1, Foo = new Nested { Bar = "baz", Fizz = new Nested { Buzz = "verymuchbaz" } }, Fizz = "fizz.baz"
            },
            new()
            {
                Id = 2, Fizz = "buzz"
            },
            new()
            {
                Id = 3, Foo = new Nested { Bar = "randomword-baz-randomword", Fizz = new Nested { Buzz = "cheese_baz" } } 
            }
        };
        
        // Act
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new LinqFilterAdapter<TestClass>())
            .Build();
        
        // Assert
        data.Where(filter).Select(x => x.Id).Should().BeEquivalentTo(expectedIds);
    }
    
    [TestCase("foo.0.bar=\"baz\"")]
    [TestCase("foo[0].bar=\"baz\"")]
    public void Should_reject_array_accessors(string text)
    {
        // Arrange
        var filter = FilterBuilder
            .FromString(text)
            .UseAdapter(new LinqFilterAdapter<TestClass>());

        // Act
        Action act = () => filter.Build();

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Array accessors are not allowed. *");
    }
}

public class TestClass
{
    public int Id { get; set; }
    public Nested Foo { get; set; }
    
    public Nested[] Array { get; set; } = System.Array.Empty<Nested>();

    public int[] Integers { get; set; } = System.Array.Empty<int>();
    public IList<int> IntegerList { get; set; } = System.Array.Empty<int>();
    public string Fizz { get; set; }
}

public class Nested
{
    public string Bar { get; set; }
    public Nested Fizz { get; set; }
    public string Buzz { get; set; }
    public int Integer { get; set; }
    public IEnumerable<Nested> Array { get; set; } = System.Array.Empty<Nested>();
}
