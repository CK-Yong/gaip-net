using System.Collections.Generic;
using System.Linq;
using Gaip.Net.Core.Contracts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Gaip.Net.Mongo
{
    public class MongoFilterAdapter<T> : IFilterAdapter
    {
        private readonly FilterDefinitionBuilder<T> _filterBuilder = Builders<T>.Filter;
        private readonly FilterDefinition<T> _filter;

        public MongoFilterAdapter()
        {
        }
        
        private MongoFilterAdapter(FilterDefinition<T> filter)
        {
            _filter = filter;
        }
        
        public IFilterAdapter And(List<object> list)
        {
            var expressions = list
                .Cast<IFilterAdapter>()
                .Select(x => x.GetResult<FilterDefinition<T>>());
            return new MongoFilterAdapter<T>(_filterBuilder.And(expressions));
        }

        public IFilterAdapter Or(List<object> list)
        {
            var expressions = list
                .Cast<IFilterAdapter>()
                .Select(x => x.GetResult<FilterDefinition<T>>());
            return new MongoFilterAdapter<T>(_filterBuilder.Or(expressions));
        }

        public IFilterAdapter Not(object simple)
        {
            return new MongoFilterAdapter<T>(_filterBuilder.Not((simple as MongoFilterAdapter<T>)!.GetResult<FilterDefinition<T>>()));
        }

        public IFilterAdapter PrefixSearch(object comparable, string strValue)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Regex(field, BsonRegularExpression.Create($"^{strValue.Substring(0, strValue.Length - 1)}")));
        }

        public IFilterAdapter SuffixSearch(object comparable, string strValue)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Regex(field, BsonRegularExpression.Create($"{strValue.Substring(1, strValue.Length - 1)}$")));
        }

        public IFilterAdapter LessThan(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Lt(field, arg));
        }

        public IFilterAdapter LessThanEquals(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Lte(field, arg));
        }

        public IFilterAdapter GreaterThanEquals(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Gte(field, arg));
        }

        public IFilterAdapter GreaterThan(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Gt(field, arg));
        }

        public IFilterAdapter NotEquals(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Ne(field, arg));
        }

        public IFilterAdapter Has(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.ElemMatch<object>(field, $"{{$eq: {arg}}}"));
        }

        public IFilterAdapter Equality(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Eq(field, arg));
        }

        public TResult GetResult<TResult>() where TResult : class
        {
            return _filter as TResult;
        }
    }
}