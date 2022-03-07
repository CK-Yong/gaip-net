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
            _visitor = new FilterVisitor(adapter);
            return this;
        }

        public T Build<T>() where T: class
        {
            _visitor.Visit(_filterContext);
            return _visitor.GetFilter() as T;
        }
    }
    
    public class MongoFilterAdapter : IFilterAdapter
    {
        private FilterDefinitionBuilder<object> _filterBuilder = Builders<object>.Filter;
        private FilterDefinition<object> _filter = FilterDefinition<object>.Empty;
        
        public MongoFilterAdapter()
        {
            
        }
        
        
    }

    public interface IFilterAdapter
    {
    }

    public class FilterVisitor : FilterBaseVisitor<object>
    {
        private readonly IFilterAdapter _adapter;
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
                    .Select(sequence => VisitSequence(sequence) as FilterDefinition<object>)
                    .ToList();

                _filter = _filterBuilder.And(list);
                return _filter;
            }
            return base.VisitExpression(context);
        }

        public override object VisitFactor(FilterParser.FactorContext context)
        {
            if (context.OR().Length > 0)
            {
                var list = context.term()
                    .Select(term => VisitTerm(term) as FilterDefinition<object>)
                    .ToList();

                _filter = _filterBuilder.Or(list);
                return _filter;
            }

            return base.VisitFactor(context);
        }

        public override object VisitTerm(FilterParser.TermContext context)
        {
            if (context.MINUS() != null || context.NOT() != null)
            {
                var simple = VisitSimple(context.simple());
                _filter = _filterBuilder.Not(simple as FilterDefinition<object>);
                return _filter;
            }

            return base.VisitTerm(context);
        }

        public override object VisitRestriction(FilterParser.RestrictionContext context)
        {
            var comparable = (VisitComparable(context.comparable())).ToString();
            var comparator = context.comparator().GetText();
            var arg = context.arg();

            _filter = comparator switch
            {
                "=" => EqualityOrSearch(comparable, arg),
                "<" => _filterBuilder.Lt(comparable, VisitArg(arg)),
                "<=" => _filterBuilder.Lte(comparable, VisitArg(arg)),
                ">=" => _filterBuilder.Gte(comparable, VisitArg(arg)),
                ">" => _filterBuilder.Gt(comparable, VisitArg(arg)),
                "!=" => _filterBuilder.Ne(comparable, VisitArg(arg)),
                ":" => _filterBuilder.ElemMatch<object>(comparable, $"{{$eq: {VisitArg(arg)}}}"),
                _ => throw new NotSupportedException()
            };

            return _filter;
        }

        private FilterDefinition<object> EqualityOrSearch(string? comparable, FilterParser.ArgContext arg)
        {
            var argValue = arg.comparable().member().value().STRING();

            if (argValue != null)
            {
                var strValue = argValue.GetText().Trim('\"', '\'');

                if (strValue.EndsWith('*'))
                {
                    return _filterBuilder.Regex(comparable,
                        BsonRegularExpression.Create($"^{strValue.Substring(0, strValue.Length - 1)}"));
                }

                if (strValue.StartsWith('*'))
                {
                    return _filterBuilder.Regex(comparable,
                        BsonRegularExpression.Create($"{strValue.Substring(1, strValue.Length - 1)}$"));
                }
            }
            return _filterBuilder.Eq(comparable, VisitArg(arg));
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

        public FilterDefinition<object> GetFilter()
        {
            return _filter;
        }
    }
}