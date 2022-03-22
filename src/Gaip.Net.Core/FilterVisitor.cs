using System;
using System.Globalization;
using System.Linq;
using Gaip.Net.Core.Contracts;

namespace Gaip.Net.Core
{
    public class FilterVisitor<T> : FilterBaseVisitor<object>
    {
        private readonly IFilterAdapter<T> _adapter;

        public FilterVisitor(IFilterAdapter<T> adapter)
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

               return _adapter.And(list);
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

                return _adapter.Or(list);
            }

            return base.VisitFactor(context);
        }

        public override object VisitTerm(FilterParser.TermContext context)
        {
            if (context.MINUS() != null || context.NOT() != null)
            {
                var simple = VisitSimple(context.simple());

               return _adapter.Not(simple);
            }

            return base.VisitTerm(context);
        }

        public override object VisitRestriction(FilterParser.RestrictionContext context)
        {
            var comparable = VisitComparable(context.comparable());
            var comparator = context.comparator().GetText();
            var arg = context.arg();

            return comparator switch
            {
                "=" => EqualityOrSearch(comparable, arg),
                "<" => _adapter.LessThan(comparable, VisitArg(arg)),
                "<=" => _adapter.LessThanEquals(comparable, VisitArg(arg)),
                ">=" => _adapter.GreaterThanEquals(comparable, VisitArg(arg)),
                ">" => _adapter.GreaterThan(comparable, VisitArg(arg)),
                "!=" => _adapter.NotEquals(comparable, VisitArg(arg)),
                ":" => _adapter.Has(comparable, VisitArg(arg)),
                _ => throw new NotSupportedException()
            };
        }

        private IFilterAdapter<T> EqualityOrSearch(object comparable, FilterParser.ArgContext arg)
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
                return new SoloWildCardValue();
            }

            if (context.DATETIME() != null)
            {
                return DateTimeOffset.Parse(context.DATETIME().GetText(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).UtcDateTime;
            }

            if (context.STRING() != null)
            {
                return new StringLiteralValue(context.STRING().GetText() );
            }

            if (context.TEXT() != null)
            {
                return new TextValue(context.TEXT().GetText());
            }

            return base.VisitValue(context);
        }

        protected override object AggregateResult(object aggregate, object nextResult)
        {
            return aggregate ?? nextResult;
        }
    }

    // Represents a value that is a wildcard (literal asterisk as a value)
    public sealed class SoloWildCardValue
    {
    }

    /// <summary>
    /// Represents a value that is a string literal. This is used when the value was surrounded by quotes. 
    /// </summary>
    public sealed class StringLiteralValue
    {
        public StringLiteralValue(string value)
        {
            Value = value.Trim('\"', '\'');
        }

        public string Value { get; }
        
        public override string ToString()
        {
            return Value;
        }
    }

    /// <summary>
    /// Represents a raw text value. This is used when the value was not surrounded by quotes. 
    /// </summary>
    public sealed class TextValue
    {
        internal TextValue(string value)
        {
            Value = value;
        }

        public string Value { get; }
        
        public override string ToString()
        {
            return Value;
        }
    }
}