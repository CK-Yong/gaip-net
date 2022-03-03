using System;
using MongoDB.Driver;

namespace GoogleApiDesign.ApiUtilities
{
    public class MongoFilterVisitor : FilterBaseVisitor<object>
    {
        private FilterDefinitionBuilder<object> _filterBuilder = Builders<object>.Filter;
        private FilterDefinition<object> _filter = FilterDefinition<object>.Empty;

        public override object VisitRestriction(FilterParser.RestrictionContext context)
        {
            var comparable = (VisitComparable(context.comparable())).ToString();
            var comparator = context.comparator().GetText();
            var arg = context.arg();

            _filter &= comparator switch
            {
                "=" => _filterBuilder.Eq(comparable, VisitArg(arg)),
                "<" => _filterBuilder.Lt(comparable, VisitArg(arg)),
                ">=" => _filterBuilder.Gte(comparable, VisitArg(arg)),
                ">" => _filterBuilder.Gt(comparable, VisitArg(arg)),
                "!=" => _filterBuilder.Ne(comparable, VisitArg(arg)),
                _ => throw new NotSupportedException()
            };
            
            return base.VisitRestriction(context);
        }

        public override object VisitValue(FilterParser.ValueContext context)
        {
            if (context.STRING() != null)
            {
                return context.STRING().GetText();
            }

            if (context.TEXT() != null)
            {
                return context.TEXT().GetText();
            }

            if (context.INTEGER() != null)
            {
                if (int.TryParse(context.INTEGER().GetText(), out var result))
                {
                    return result;
                }

                return long.Parse(context.INTEGER().GetText());
            }

            if (context.ASTERISK() != null)
            {
                return context.ASTERISK().GetText();
            }
            
            return base.VisitValue(context);
        }

        public FilterDefinition<object> GetFilter()
        {
            return _filter;
        }
    }
    
    //todo: make this more generic by having a database adapter so we can swap out filter builders (e.g. towards SQL)
}