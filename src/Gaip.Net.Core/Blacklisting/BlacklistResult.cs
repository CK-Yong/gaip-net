using System;

namespace Gaip.Net.Core;

public class BlacklistResult<T>
{
    private readonly string _errorMessage;
    public bool IsQueryAllowed { get; internal init; }

    private readonly T _value;

    internal BlacklistResult(bool isWhitelist)
    {
        _errorMessage = isWhitelist
            ? "A non-whitelisted property was accessed in the input query"
            : "A blacklisted property was accessed in the input query";
    }

    /// <summary>
    /// The value that was returned from the query. Can only be accessed if IsQueryAllowed is true.
    /// </summary>
    /// <exception cref="InvalidOperationException"></exception>
    public T Value
    {
        get => IsQueryAllowed
            ? _value
            : throw new InvalidOperationException(_errorMessage);
        internal init => _value = value;
    }
}