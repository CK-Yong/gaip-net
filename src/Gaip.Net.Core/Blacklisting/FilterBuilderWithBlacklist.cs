using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Antlr4.Runtime.Tree;
using Gaip.Net.Core.Contracts;

namespace Gaip.Net.Core;

public class FilterBuilderWithBlacklist<T>
{
    private readonly FilterParser _filterParser;
    private readonly FilterVisitor<T> _visitor;
    private readonly Expression<Func<T, object>>[] _blackListedProperties;
    private readonly bool _isWhitelist;

    private List<string> _blacklist = new();

    internal FilterBuilderWithBlacklist(
        FilterParser filterParser,
        FilterVisitor<T> visitor, 
        Expression<Func<T, object>>[] blackListedProperties,
        bool isWhitelist = false)
    {
        _filterParser = filterParser;
        _visitor = visitor;
        _blackListedProperties = blackListedProperties;
        _isWhitelist = isWhitelist;
    }

    public BlacklistResult<T> Build()
    {
        foreach (var func in _blackListedProperties)
        {
            if (func.Body is MemberExpression propertyAccess)
            {
                _blacklist.Add(MemberToString(propertyAccess));
            }
            else
            {
                throw new ArgumentException($"Expression {func.Body} must be a member expression");
            }
        }
        
        IParseTreeListener blacklistListener = _isWhitelist ? new WhitelistListener(_blacklist) : new BlacklistListener(_blacklist);
        _filterParser.AddParseListener(blacklistListener);
        
        var resultAdapter = _visitor.Visit(_filterParser.filter());
        if (resultAdapter is not IFilterAdapter<T> adapter)
        {
            throw new InvalidOperationException("Adapter is not of type IFilterAdapter<T>");
        }
            
        return new BlacklistResult<T>(_isWhitelist)
        {
            IsQueryAllowed = !((IHasErrors)blacklistListener).ErrorsFound,
            Value = adapter.GetResult()
        };
    }

    private string MemberToString(MemberExpression property)
    {
        var propText = property.ToString();
        var firstAccessor = propText.IndexOf('.') + 1;
        return propText[firstAccessor..];
    }
}