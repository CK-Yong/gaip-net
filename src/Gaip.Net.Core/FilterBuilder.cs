using System;
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
        
        public WhitelistedFilterBuilder<T> UseWhitelist(params Expression<Func<T, object>>[] whitelistedProperties)
        {
            return new WhitelistedFilterBuilder<T>(_filterContext, _visitor, whitelistedProperties);
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
}