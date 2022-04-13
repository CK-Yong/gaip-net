using System;

namespace Gaip.Net.Core;

public class WhitelistResult<T>
{
    public bool IsQueryAllowed { get; internal init; }
    private readonly T _value;

    public T Value
    {
        get => IsQueryAllowed
            ? _value
            : throw new InvalidOperationException("A non-whitelisted property was accessed in the input query");
        internal init => _value = value;
    }
}