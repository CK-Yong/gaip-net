using System;

namespace Gaip.Net.Core;

public class WhitelistResult<T>
{
    public bool IsQueryAllowed { get; internal init; }
    
    private readonly T _value;

    /// <summary>
    /// The value that was returned from the query. Can only be accessed if IsQueryAllowed is true.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public T Value
    {
        get => IsQueryAllowed
            ? _value
            : throw new InvalidOperationException("A non-whitelisted property was accessed in the input query");
        internal init => _value = value;
    }
}