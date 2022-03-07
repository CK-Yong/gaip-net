using Antlr4.Runtime;
using GoogleApiDesign.ApiUtilities.Contracts;

namespace GoogleApiDesign.ApiUtilities
{
    public class FilterBuilder
    {
        private readonly FilterParser.FilterContext _filterContext;
        private IFilterAdapter _adapter;
        private FilterVisitor _visitor;
        
        private FilterBuilder(string text)
        {
            var inputStream = new AntlrInputStream(text);
            var lexer = new FilterLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            _filterContext = new FilterParser(tokenStream).filter();
        }

        public static FilterBuilder FromString(string text)
        {
            return new FilterBuilder(text);
        }

        public FilterBuilder UseAdapter(IFilterAdapter adapter)
        {
            _adapter = adapter;
            _visitor = new FilterVisitor(adapter);
            return this;
        }

        public T Build<T>() where T: class
        {
            _visitor.Visit(_filterContext);
            return _adapter.GetResult<T>();
        }
    }
}