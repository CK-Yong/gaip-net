using System;
using NUnit.Framework;
using FluentAssertions;

namespace Gaip.Net.Core.Tests;

public class FilterParserWhitelistTests
{
    [Test]
    public void Initializing_with_whitelist_should_deny_queries_to_non_whitelisted_properties()
    {
        // Act
        var filterResult = FilterBuilder.FromString("bar=\"baz\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseWhitelist(x => x.Foo)
            .Build();

        // Assert
        filterResult.IsQueryAllowed.Should().BeFalse();        
    }    
    
    [Test]
    public void Allows_whitelisting_of_multiple_properties()
    {
        // Act
        var filterResult = FilterBuilder.FromString("foo=1 OR bar=\"baz\" OR myNested:\"abc\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseWhitelist(x => x.Foo, x => x.Bar, x => x.MyNested)
            .Build();

        // Assert
        filterResult.IsQueryAllowed.Should().BeTrue();        
    }
    
    [Test]
    public void Building_with_whitelist_should_deny_access_to_value()
    {
        // Arrange
        var filterResult = FilterBuilder.FromString("bar=\"baz\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseWhitelist(x => x.Foo)
            .Build();

        // Act
        var act = () => filterResult.Value;
        
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
        var filterResult = FilterBuilder.FromString($"{property}=\"bar\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseWhitelist(x => x.Foo)
            .Build();

        // Assert
        filterResult.IsQueryAllowed.Should().BeTrue();   
    }

    [Test]
    public void Whitelist_should_allow_nested_properties()
    {
        // Act
        var filterResult = FilterBuilder.FromString("myNested.Baz=\"baz\" OR myNested.MyNested.Baz=\"baz\"")
            .UseAdapter(new TestAdapter<MyClass>())
            .UseWhitelist(x => x.MyNested.Baz, x => x.MyNested.MyNested.Baz)
            .Build();

        // Assert
        filterResult.IsQueryAllowed.Should().BeTrue();        
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