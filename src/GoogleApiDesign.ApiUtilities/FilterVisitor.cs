using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Antlr4.Runtime;
using MongoDB.Bson;
using MongoDB.Driver;

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
    
    public class MongoFilterAdapter : IFilterAdapter
    {
        private FilterDefinitionBuilder<object> _filterBuilder = Builders<object>.Filter;
        private FilterDefinition<object> _filter = FilterDefinition<object>.Empty;

        public IFilterAdapter And(List<object> list)
        {
            _filter = _filterBuilder.And(list.Cast<FilterDefinition<object>>());
            return this;
        }

        public IFilterAdapter Or(List<object> list)
        {
            _filter = _filterBuilder.Or(list.Cast<FilterDefinition<object>>());
            return this;
        }

        public IFilterAdapter Not(object simple)
        {
            _filter = _filterBuilder.Not(simple as FilterDefinition<object>);
            return this;
        }

        public IFilterAdapter PrefixSearch(object comparable, string strValue)
        {
            _filter = _filterBuilder.Regex(comparable.ToString(), BsonRegularExpression.Create($"^{strValue.Substring(0, strValue.Length - 1)}"));
            return this;
        }

        public IFilterAdapter SuffixSearch(object comparable, string strValue)
        {
            _filter = _filterBuilder.Regex(comparable.ToString(), BsonRegularExpression.Create($"{strValue.Substring(1, strValue.Length - 1)}$"));
            return this;
        }

        public IFilterAdapter LessThan(object comparable, object arg)
        {
            _filter = _filterBuilder.Lt(comparable.ToString(), arg);
            return this;
        }

        public IFilterAdapter LessthanEquals(object comparable, object arg)
        {
            _filter = _filterBuilder.Lte(comparable.ToString(), arg);
            return this;
        }

        public IFilterAdapter GreaterThanEquals(object comparable, object arg)
        {
            _filter = _filterBuilder.Gte(comparable.ToString(), arg);
            return this;
        }

        public IFilterAdapter GreaterThan(object comparable, object arg)
        {
            _filter = _filterBuilder.Gt(comparable.ToString(), arg);
            return this;
        }

        public IFilterAdapter NotEquals(object comparable, object arg)
        {
            _filter = _filterBuilder.Ne(comparable.ToString(), arg);
            return this;
        }

        public IFilterAdapter Has(object comparable, object arg)
        {
            _filter = _filterBuilder.ElemMatch<object>(comparable.ToString(), $"{{$eq: {arg}}}");
            return this;
        }

        public IFilterAdapter Equality(object comparable, object arg)
        {
            _filter = _filterBuilder.Eq(comparable.ToString(), arg);
            return this;
        }

        public T GetResult<T>() where T : class
        {
            return _filter as T;
        }
    }

    public interface IFilterAdapter
    {
        IFilterAdapter And(List<object> list);
        IFilterAdapter Or(List<object> list);
        IFilterAdapter Not(object simple);
        IFilterAdapter PrefixSearch(object comparable, string strValue);
        IFilterAdapter SuffixSearch(object comparable, string strValue);
        IFilterAdapter LessThan(object comparable, object arg);
        IFilterAdapter LessthanEquals(object comparable, object arg);
        IFilterAdapter GreaterThanEquals(object comparable, object arg);
        IFilterAdapter GreaterThan(object comparable, object arg);
        IFilterAdapter NotEquals(object comparable, object arg);
        IFilterAdapter Has(object comparable, object arg);
        IFilterAdapter Equality(object comparable, object arg);
        T GetResult<T>() where T : class;
    }

    public class FilterVisitor : FilterBaseVisitor<object>
    {
        private IFilterAdapter _adapter;
        private FilterDefinitionBuilder<object> _filterBuilder = Builders<object>.Filter;
        private FilterDefinition<object> _filter = FilterDefinition<object>.Empty;
        private IFilterAdapter _filterAdapter;

        public FilterVisitor(IFilterAdapter adapter)
        {
            _adapter = adapter;
        }
        
        public override object VisitExpression(FilterParser.ExpressionContext context)
        {
            if (context.AND().Length > 0)
            {
                var list = context.sequence()
                    .Select(VisitSequence)
                    .ToList();

                _adapter = _adapter.And(list);
                return _adapter;
            }
            return base.VisitExpression(context);
        }

        public override object VisitFactor(FilterParser.FactorContext context)
        {
            if (context.OR().Length > 0)
            {
                var list = context.term()
                    .Select(VisitTerm)
                    .ToList();

                _adapter = _adapter.Or(list);
                return _adapter;
            }

            return base.VisitFactor(context);
        }

        public override object VisitTerm(FilterParser.TermContext context)
        {
            if (context.MINUS() != null || context.NOT() != null)
            {
                var simple = VisitSimple(context.simple());

                _adapter = _adapter.Not(simple);
                return _adapter;
            }

            return base.VisitTerm(context);
        }

        public override object VisitRestriction(FilterParser.RestrictionContext context)
        {
            var comparable = VisitComparable(context.comparable());
            var comparator = context.comparator().GetText();
            var arg = context.arg();

            _adapter = comparator switch
            {
                "=" => EqualityOrSearch(comparable, arg),
                "<" => _adapter.LessThan(comparable, VisitArg(arg)),
                "<=" => _adapter.LessthanEquals(comparable, VisitArg(arg)),
                ">=" => _adapter.GreaterThanEquals(comparable, VisitArg(arg)),
                ">" => _adapter.GreaterThan(comparable, VisitArg(arg)),
                "!=" => _adapter.NotEquals(comparable, VisitArg(arg)),
                ":" => _adapter.Has(comparable, VisitArg(arg)),
                _ => throw new NotSupportedException()
            };

            return _filter;
        }

        private IFilterAdapter EqualityOrSearch(object comparable, FilterParser.ArgContext arg)
        {
            var argValue = arg.comparable().member().value().STRING();

            if (argValue != null)
            {
                var strValue = argValue.GetText().Trim('\"', '\'');

                if (strValue.EndsWith('*'))
                {
                    return _adapter.PrefixSearch(comparable, strValue);
                }

                if (strValue.StartsWith('*'))
                {
                    return _adapter.SuffixSearch(comparable, strValue);
                }
            }

            return _adapter.Equality(comparable, VisitArg(arg));
        }

        public override object VisitMember(FilterParser.MemberContext context)
        {
            if (context.field().Length > 0)
            {
                return context.GetText();
            }

            return base.VisitMember(context);
        }

        public override object VisitValue(FilterParser.ValueContext context)
        {
            if (context.INTEGER() != null)
            {
                if (int.TryParse(context.INTEGER().GetText(), out var result))
                {
                    return result;
                }

                return long.Parse(context.INTEGER().GetText());
            }

            if (context.BOOLEAN() != null)
            {
                return bool.Parse(context.BOOLEAN().GetText());
            }

            if (context.FLOAT() != null)
            {
                return double.Parse(context.FLOAT().GetText(), CultureInfo.InvariantCulture);
            }

            if (context.DURATION() != null)
            {
                var value = context.DURATION().GetText().TrimEnd('s');
                return double.Parse(value, CultureInfo.InvariantCulture) * 1000;
            }

            if (context.ASTERISK() != null)
            {
                return context.ASTERISK().GetText();
            }

            if (context.DATETIME() != null)
            {
                return DateTimeOffset.Parse(context.DATETIME().GetText(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).UtcDateTime;
            }

            if (context.STRING() != null)
            {
                return context.STRING().GetText().Trim('\"', '\'');
            }

            if (context.TEXT() != null)
            {
                return context.TEXT().GetText();
            }

            return base.VisitValue(context);
        }
    }
}