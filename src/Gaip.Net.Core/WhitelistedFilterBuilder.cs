using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Gaip.Net.Core.Contracts;

namespace Gaip.Net.Core;

public class WhitelistedFilterBuilder<T>
{
    private readonly FilterParser.FilterContext _filterContext;
    private readonly FilterVisitor<T> _visitor;
    private readonly Expression<Func<T, object>>[] _whitelistedProperties;

    private List<string> _whitelist = new();

    internal WhitelistedFilterBuilder(FilterParser.FilterContext filterContext, FilterVisitor<T> visitor, Expression<Func<T, object>>[] whitelistedProperties)
    {
        _filterContext = filterContext;
        _visitor = visitor;
        _whitelistedProperties = whitelistedProperties;
    }

    public WhitelistResult<T> Build()
    {
        foreach (var func in _whitelistedProperties)
        {
            if (func.Body is MemberExpression propertyAccess)
            {
                _whitelist.Add(propertyAccess.Member.Name);
            }
            else
            {
                throw new ArgumentException($"Expression {func.Body} must be a member expression");
            }
        }

        var resultAdapter = _visitor.Visit(_filterContext);

        if (resultAdapter is not IFilterAdapter<T> adapter)
        {
            throw new InvalidOperationException("Adapter is not of type IFilterAdapter<T>");
        }
            
        return new WhitelistResult<T>
        {
            IsQueryAllowed = false,
            Value = adapter.GetResult()
        };
    }
}