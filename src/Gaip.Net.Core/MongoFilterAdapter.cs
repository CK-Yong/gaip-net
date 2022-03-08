using System.Collections.Generic;
using System.Linq;
using Gaip.Net.Core.Contracts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Gaip.Net.Core
{
    public class MongoFilterAdapter : IFilterAdapter
    {
        private readonly FilterDefinitionBuilder<object> _filterBuilder = Builders<object>.Filter;
        private FilterDefinition<object> _filter = FilterDefinition<object>.Empty;

        public MongoFilterAdapter()
        {
        }

        private MongoFilterAdapter(FilterDefinition<object> filter)
        {
            _filter = filter;
        }

        public IFilterAdapter And(List<object> list)
        {
            var expressions = list
                .Cast<IFilterAdapter>()
                .Select(x => x.GetResult<FilterDefinition<object>>());
            return new MongoFilterAdapter(_filterBuilder.And(expressions));
        }

        public IFilterAdapter Or(List<object> list)
        {
            var expressions = list
                .Cast<IFilterAdapter>()
                .Select(x => x.GetResult<FilterDefinition<object>>());
            return new MongoFilterAdapter(_filterBuilder.Or(expressions));
        }

        public IFilterAdapter Not(object simple)
        {
            return new MongoFilterAdapter(
                _filterBuilder.Not((simple as MongoFilterAdapter)!.GetResult<FilterDefinition<object>>()));
        }

        public IFilterAdapter PrefixSearch(object comparable, string strValue)
        {
            return new MongoFilterAdapter(_filterBuilder.Regex(comparable.ToString(),
                BsonRegularExpression.Create($"^{strValue.Substring(0, strValue.Length - 1)}")));
        }

        public IFilterAdapter SuffixSearch(object comparable, string strValue)
        {
            return new MongoFilterAdapter(_filterBuilder.Regex(comparable.ToString(),
                BsonRegularExpression.Create($"{strValue.Substring(1, strValue.Length - 1)}$")));
        }

        public IFilterAdapter LessThan(object comparable, object arg)
        {
            return new MongoFilterAdapter(_filterBuilder.Lt(comparable.ToString(), arg));
        }

        public IFilterAdapter LessThanEquals(object comparable, object arg)
        {
            return new MongoFilterAdapter(_filterBuilder.Lte(comparable.ToString(), arg));
        }

        public IFilterAdapter GreaterThanEquals(object comparable, object arg)
        {
            return new MongoFilterAdapter(_filterBuilder.Gte(comparable.ToString(), arg));
        }

        public IFilterAdapter GreaterThan(object comparable, object arg)
        {
            return new MongoFilterAdapter(_filterBuilder.Gt(comparable.ToString(), arg));
        }

        public IFilterAdapter NotEquals(object comparable, object arg)
        {
            return new MongoFilterAdapter(_filterBuilder.Ne(comparable.ToString(), arg));
        }

        public IFilterAdapter Has(object comparable, object arg)
        {
            return new MongoFilterAdapter(_filterBuilder.ElemMatch<object>(comparable.ToString(), $"{{$eq: {arg}}}"));
        }

        public IFilterAdapter Equality(object comparable, object arg)
        {
            return new MongoFilterAdapter(_filterBuilder.Eq(comparable.ToString(), arg));
        }

        public T GetResult<T>() where T : class
        {
            return _filter as T;
        }
    }
}