using System;
using NUnit.Framework;
using FluentAssertions;

namespace Gaip.Net.Core.Tests;

public class FilterParserBlacklistTests
{
    [Test]
    public void Initializing_with_blacklist_should_deny_queries_to_non_whitelisted_properties()
    {
        // Act
        var filterResult = FilterBuilder.FromString("foo=\"baz\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseBlacklist(x => x.Foo)
            .Build();

        // Assert
        filterResult.IsQueryAllowed.Should().BeFalse();        
    }    
    
    [TestCase("foo=1")]
    [TestCase("bar=1")]
    [TestCase("mynested=1")]
    public void Allows_blacklisting_of_multiple_properties(string query)
    {
        // Act
        var filterResult = FilterBuilder.FromString(query)
            .UseAdapter(new TestAdapter<MyClass>())
            .UseBlacklist(x => x.Foo, x => x.Bar, x => x.MyNested)
            .Build();

        // Assert
        filterResult.IsQueryAllowed.Should().BeFalse();        
    }
    
    [Test]
    public void Building_with_blacklist_should_deny_access_to_value()
    {
        // Arrange
        var filterResult = FilterBuilder.FromString("foo=\"baz\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseBlacklist(x => x.Foo)
            .Build();

        // Act
        var act = () => filterResult.Value;
        
        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("A blacklisted property was accessed in the input query");
    }

    [TestCase("Foo")]
    [TestCase("foo")]
    [TestCase("FOO")]
    [TestCase("fOo")]
    public void Blacklist_should_evaluate_with_case_insensitivity(string property)
    {
        // Act
        var filterResult = FilterBuilder.FromString($"{property}=\"bar\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseBlacklist(x => x.Foo)
            .Build();

        // Assert
        filterResult.IsQueryAllowed.Should().BeFalse();   
    }

    [TestCase("myNested.Baz=\"baz\"")]
    [TestCase("myNested.MyNested.Baz=\"baz\"")]
    public void Blacklist_should_block_nested_properties(string query)
    {
        // Act
        var filterResult = FilterBuilder.FromString(query)
            .UseAdapter(new TestAdapter<MyClass>())
            .UseBlacklist(x => x.MyNested.Baz, x => x.MyNested.MyNested.Baz)
            .Build();

        // Assert
        filterResult.IsQueryAllowed.Should().BeFalse();        
    }

    [Test]
    public void Initializing_whitelist_with_a_method_should_result_in_an_error()
    {
        // Act
        Action act = () => FilterBuilder.FromString("bar=\"baz\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseBlacklist(x => x.ShouldNotBeCalled())
            .Build();

        // Assert
        act.Should()
            .Throw<ArgumentException>()
            .WithMessage("Expression x.ShouldNotBeCalled() must be a member expression");        
    }
}