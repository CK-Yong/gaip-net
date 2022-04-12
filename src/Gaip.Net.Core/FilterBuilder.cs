using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Antlr4.Runtime;
using Gaip.Net.Core.Contracts;

namespace Gaip.Net.Core
{
    public class FilterBuilder
    {
        private readonly string _text;

        private FilterBuilder(string text)
        {
            _text = text;
        }

        public static FilterBuilder FromString(string text)
        {
            return new FilterBuilder(text);
        }
        
        public FilterBuilder<T> UseAdapter<T>(IFilterAdapter<T> adapter)
        {
            return new FilterBuilder<T>(_text, adapter);
        }
    }

    public class FilterBuilder<T>
    {
        private readonly FilterParser.FilterContext _filterContext;
        private readonly FilterVisitor<T> _visitor;

        internal FilterBuilder(string text, IFilterAdapter<T> adapter)
        {
            var inputStream = new AntlrInputStream(text);
            var lexer = new FilterLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            _filterContext = new FilterParser(tokenStream).filter();
            _visitor = new FilterVisitor<T>(adapter);
        }

        private List<string> _whitelist = new();
        public WhitelistResult<T> Whitelist = new();
        public FilterBuilder<T> UseWhitelist(params Expression<Func<T, object>>[] whitelistedProperties)
        {
            foreach (var func in whitelistedProperties)
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
            
            // todo: Use Antlr listener to evaluate accessed properties, to see if they are all whitelisted.

            return this;
        }

        public T Build()
        {
            var resultAdapter = _visitor.Visit(_filterContext);

            if (resultAdapter is not IFilterAdapter<T> adapter)
            {
                throw new InvalidOperationException("Adapter is not of type IFilterAdapter<T>");
            }

            return adapter.GetResult();
        }
    }

    public class WhitelistResult<T>
    {
        public bool IsQueryAllowed { get; set; } = false;
    }
}