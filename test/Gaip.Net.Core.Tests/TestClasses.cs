using System;
using System.Collections.Generic;
using System.Text.Json;
using Gaip.Net.Core.Contracts;

namespace Gaip.Net.Core.Tests;

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
    
    public Nested MyNested { get; set; }
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