using System;
using System.Collections.Generic;
using Gaip.Net.Core.Contracts;
using NUnit.Framework;
using System.Text.Json;
using FluentAssertions;

namespace Gaip.Net.Core.Tests;

public class FilterParserWhitelistTests
{
    [Test]
    public void Initializing_with_whitelist_should_deny_queries_to_non_whitelisted_properties()
    {
        // Act
        var filterBuilder = FilterBuilder.FromString("bar=\"baz\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseWhitelist(x => x.Foo)
            .Build();

        // Assert
        filterBuilder.IsQueryAllowed.Should().BeFalse();        
    }
    
    [Test]
    public void Building_with_whitelist_should_deny_access_to_value()
    {
        // Arrange
        var filterBuilder = FilterBuilder.FromString("bar=\"baz\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseWhitelist(x => x.Foo)
            .Build();

        // Act
        var act = () => filterBuilder.Value;
        
        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("A non-whitelisted property was accessed in the input query");
    }

    [TestCase("Foo")]
    [TestCase("foo")]
    [TestCase("FOO")]
    [TestCase("fOo")]
    public void Whitelist_should_evaluate_with_case_insensitivity(string property)
    {
        // Act
        var filterBuilder = FilterBuilder.FromString($"{property}=\"bar\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseWhitelist(x => x.Foo)
            .Build();

        // Assert
        filterBuilder.IsQueryAllowed.Should().BeTrue();   
    }

    [Test]
    public void Whitelist_should_allow_nested_properties()
    {
        // Act
        var filterBuilder = FilterBuilder.FromString("myNested.Baz=\"baz\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseWhitelist(x => x.MyNested.Baz)
            .Build();

        // Assert
        filterBuilder.IsQueryAllowed.Should().BeTrue();        
    }

    [Test]
    public void Initializing_whitelist_with_a_method_should_result_in_an_error()
    {
        // Act
        Action act = () => FilterBuilder.FromString("bar=\"baz\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseWhitelist(x => x.ShouldNotBeCalled())
            .Build();

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Expression x.ShouldNotBeCalled() must be a member expression");        
    }
}

internal class MyClass
{
    public string Foo { get; set; }
    
    public string Bar { get; set; }
    
    public Nested MyNested { get; set; }

    public string ShouldNotBeCalled()
    {
        return string.Empty;
    }
}

internal class Nested
{
    public string Baz { get; set; }
}

public class TestAdapter<T> : IFilterAdapter<T>
{
    public IFilterAdapter<T> And(List<IFilterAdapter<T>> list)
    {
        Console.WriteLine("And: " + JsonSerializer.Serialize(list));
        return this;
    }

    public IFilterAdapter<T> Or(List<IFilterAdapter<T>> list)
    {
        Console.WriteLine("Or: " + JsonSerializer.Serialize(list));
        return this;
    }

    public IFilterAdapter<T> Not(IFilterAdapter<T> simple)
    {
        Console.WriteLine("Not: " + JsonSerializer.Serialize(simple));
        return this;
    }

    public IFilterAdapter<T> Equality(object comparable, object arg)
    {
        Console.WriteLine("Equality: " + JsonSerializer.Serialize(comparable) + " " + JsonSerializer.Serialize(arg));
        return this;
    }

    public IFilterAdapter<T> NotEquals(object comparable, object arg)
    {
        Console.WriteLine("NotEquals: " + JsonSerializer.Serialize(comparable) + " " + JsonSerializer.Serialize(arg));
        return this;
    }

    public IFilterAdapter<T> LessThan(object comparable, object arg)
    {
        Console.WriteLine("LessThan: " + JsonSerializer.Serialize(comparable) + " " + JsonSerializer.Serialize(arg));
        return this;
    }

    public IFilterAdapter<T> LessThanEquals(object comparable, object arg)
    {
        Console.WriteLine("LessThanEquals: " + JsonSerializer.Serialize(comparable) + " " + JsonSerializer.Serialize(arg));
        return this;
    }

    public IFilterAdapter<T> GreaterThan(object comparable, object arg)
    {
        Console.WriteLine("GreaterThan: " + JsonSerializer.Serialize(comparable) + " " + JsonSerializer.Serialize(arg));
        return this;
    }

    public IFilterAdapter<T> GreaterThanEquals(object comparable, object arg)
    {
        Console.WriteLine("GreaterThanEquals: " + JsonSerializer.Serialize(comparable) + " " + JsonSerializer.Serialize(arg));
        return this;
    }

    public IFilterAdapter<T> PrefixSearch(object comparable, string strValue)
    {
        Console.WriteLine("PrefixSearch: " + JsonSerializer.Serialize(comparable) + " " + JsonSerializer.Serialize(strValue));
        return this;
    }

    public IFilterAdapter<T> SuffixSearch(object comparable, string strValue)
    {
        Console.WriteLine("SuffixSearch: " + JsonSerializer.Serialize(comparable) + " " + JsonSerializer.Serialize(strValue));
        return this;
    }

    public IFilterAdapter<T> Has(object comparable, object arg)
    {
        Console.WriteLine("Has: " + JsonSerializer.Serialize(comparable) + " " + JsonSerializer.Serialize(arg));
        return this;
    }

    public T? GetResult()
    {
        return default(T);
    }
}