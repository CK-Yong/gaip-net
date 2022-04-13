using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Gaip.Net.Core.Contracts;

namespace Gaip.Net.Core;

public class WhitelistedFilterBuilder<T>
{
    private readonly FilterParser _filterParser;
    private readonly FilterVisitor<T> _visitor;
    private readonly Expression<Func<T, object>>[] _whitelistedProperties;

    private List<string> _whitelist = new();

    internal WhitelistedFilterBuilder(FilterParser filterParser, FilterVisitor<T> visitor, Expression<Func<T, object>>[] whitelistedProperties)
    {
        _filterParser = filterParser;
        _visitor = visitor;
        _whitelistedProperties = whitelistedProperties;
    }

    public WhitelistResult<T> Build()
    {
        foreach (var func in _whitelistedProperties)
        {
            if (func.Body is MemberExpression propertyAccess)
            {
                _whitelist.Add(MemberToString(propertyAccess));
            }
            else
            {
                throw new ArgumentException($"Expression {func.Body} must be a member expression");
            }
        }

        var whitelistListener = new WhitelistListener(_whitelist);
        _filterParser.AddParseListener(whitelistListener);
        
        var resultAdapter = _visitor.Visit(_filterParser.filter());
        if (resultAdapter is not IFilterAdapter<T> adapter)
        {
            throw new InvalidOperationException("Adapter is not of type IFilterAdapter<T>");
        }
            
        return new WhitelistResult<T>
        {
            IsQueryAllowed = !whitelistListener.ErrorsFound,
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

internal class WhitelistListener : FilterBaseListener
{
    private readonly List<string> _whitelist;

    public WhitelistListener(List<string> whitelist)
    {
        _whitelist = whitelist;
    }

    public bool ErrorsFound { get; private set; }

    public override void ExitComparable(FilterParser.ComparableContext context)
    {
        var accessedMember = context.GetText();

        if (context.Parent is FilterParser.ArgContext)
        {
            base.ExitComparable(context);
            return;
        }
        
        if (!_whitelist.Any(x => string.Equals(x, accessedMember, StringComparison.InvariantCultureIgnoreCase)))
        {
            ErrorsFound = true;
        }
        
        base.ExitComparable(context);
    }
}