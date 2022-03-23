using System.Collections.Generic;
using System.Linq;
using Gaip.Net.Core;
using Gaip.Net.Core.Contracts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Gaip.Net.Mongo
{
    /// <summary>
    /// Filter adapter for MongoDB. Using this class allows you to obtain a <see cref="FilterDefinition{TDocument}"/>. Supports Mongo C# driver attributes.
    /// </summary>
    /// <typeparam name="TDocument">The type of the document that will be queried.</typeparam>
    public sealed class MongoFilterAdapter<TDocument> : IFilterAdapter<FilterDefinition<TDocument>>
    {
        private readonly FilterDefinitionBuilder<TDocument> _filterBuilder = Builders<TDocument>.Filter;
        private readonly FilterDefinition<TDocument> _filter;

        public MongoFilterAdapter()
        {
        }
        
        private MongoFilterAdapter(FilterDefinition<TDocument> filter)
        {
            _filter = filter;
        }
        
        public IFilterAdapter<FilterDefinition<TDocument>> And(List<IFilterAdapter<FilterDefinition<TDocument>>> list)
        {
            var expressions = list
                .Select(x => x.GetResult());
            return new MongoFilterAdapter<TDocument>(_filterBuilder.And(expressions));
        }

        public IFilterAdapter<FilterDefinition<TDocument>> Or(List<IFilterAdapter<FilterDefinition<TDocument>>> list)
        {
            var expressions = list
                .Select(x => x.GetResult());
            return new MongoFilterAdapter<TDocument>(_filterBuilder.Or(expressions));
        }

        public IFilterAdapter<FilterDefinition<TDocument>> Not(IFilterAdapter<FilterDefinition<TDocument>> simple)
        {
            return new MongoFilterAdapter<TDocument>(_filterBuilder.Not(simple.GetResult()));
        }

        public IFilterAdapter<FilterDefinition<TDocument>> Equality(object comparable, object arg)
        {
            var val = arg;
            if (arg is StringLiteralValue stringLiteral)
            {
                val = stringLiteral.Value;
            }
            
            FieldDefinition<TDocument, object> field = comparable.ToString();
            return new MongoFilterAdapter<TDocument>(_filterBuilder.Eq(field, val));
        }

        public IFilterAdapter<FilterDefinition<TDocument>> NotEquals(object comparable, object arg)
        {
            var val = arg;
            if (arg is StringLiteralValue stringLiteral)
            {
                val = stringLiteral.Value;
            }
            
            FieldDefinition<TDocument, object> field = comparable.ToString();
            return new MongoFilterAdapter<TDocument>(_filterBuilder.Ne(field, val));
        }

        public IFilterAdapter<FilterDefinition<TDocument>> LessThan(object comparable, object arg)
        {
            FieldDefinition<TDocument, object> field = comparable.ToString();
            return new MongoFilterAdapter<TDocument>(_filterBuilder.Lt(field, arg));
        }

        public IFilterAdapter<FilterDefinition<TDocument>> LessThanEquals(object comparable, object arg)
        {
            FieldDefinition<TDocument, object> field = comparable.ToString();
            return new MongoFilterAdapter<TDocument>(_filterBuilder.Lte(field, arg));
        }

        public IFilterAdapter<FilterDefinition<TDocument>> GreaterThan(object comparable, object arg)
        {
            FieldDefinition<TDocument, object> field = comparable.ToString();
            return new MongoFilterAdapter<TDocument>(_filterBuilder.Gt(field, arg));
        }

        public IFilterAdapter<FilterDefinition<TDocument>> GreaterThanEquals(object comparable, object arg)
        {
            FieldDefinition<TDocument, object> field = comparable.ToString();
            return new MongoFilterAdapter<TDocument>(_filterBuilder.Gte(field, arg));
        }

        public IFilterAdapter<FilterDefinition<TDocument>> PrefixSearch(object comparable, string strValue)
        {
            FieldDefinition<TDocument, object> field = comparable.ToString();
            return new MongoFilterAdapter<TDocument>(_filterBuilder.Regex(field, BsonRegularExpression.Create($"^{strValue}")));
        }

        public IFilterAdapter<FilterDefinition<TDocument>> SuffixSearch(object comparable, string strValue)
        {
            FieldDefinition<TDocument, object> field = comparable.ToString();
            return new MongoFilterAdapter<TDocument>(_filterBuilder.Regex(field, BsonRegularExpression.Create($"{strValue}$")));
        }

        public IFilterAdapter<FilterDefinition<TDocument>> Has(object comparable, object arg)
        {
            FieldDefinition<TDocument, object> field = comparable.ToString();
            return new MongoFilterAdapter<TDocument>(_filterBuilder.ElemMatch<object>(field, $"{{$eq: {arg}}}"));
        }

        public FilterDefinition<TDocument> GetResult() 
        {
            return _filter;
        }
    }
}