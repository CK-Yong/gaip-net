using System.Collections.Generic;
using System.Linq;
using Gaip.Net.Core.Contracts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Gaip.Net.Mongo
{
    public class MongoFilterAdapter<T> : IFilterAdapter<FilterDefinition<T>>
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
        
        public IFilterAdapter<FilterDefinition<T>> And(List<object> list)
        {
            var expressions = list
                .Cast<IFilterAdapter<FilterDefinition<T>>>()
                .Select(x => x.GetResult());
            return new MongoFilterAdapter<T>(_filterBuilder.And(expressions));
        }

        public IFilterAdapter<FilterDefinition<T>> Or(List<object> list)
        {
            var expressions = list
                .Cast<IFilterAdapter<FilterDefinition<T>>>()
                .Select(x => x.GetResult());
            return new MongoFilterAdapter<T>(_filterBuilder.Or(expressions));
        }

        public IFilterAdapter<FilterDefinition<T>> Not(object simple)
        {
            return new MongoFilterAdapter<T>(_filterBuilder.Not((simple as MongoFilterAdapter<T>)!.GetResult()));
        }

        public IFilterAdapter<FilterDefinition<T>> PrefixSearch(object comparable, string strValue)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Regex(field, BsonRegularExpression.Create($"^{strValue.Substring(0, strValue.Length - 1)}")));
        }

        public IFilterAdapter<FilterDefinition<T>> SuffixSearch(object comparable, string strValue)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Regex(field, BsonRegularExpression.Create($"{strValue.Substring(1, strValue.Length - 1)}$")));
        }

        public IFilterAdapter<FilterDefinition<T>> LessThan(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Lt(field, arg));
        }

        public IFilterAdapter<FilterDefinition<T>> LessThanEquals(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Lte(field, arg));
        }

        public IFilterAdapter<FilterDefinition<T>> GreaterThanEquals(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Gte(field, arg));
        }

        public IFilterAdapter<FilterDefinition<T>> GreaterThan(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Gt(field, arg));
        }

        public IFilterAdapter<FilterDefinition<T>> NotEquals(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Ne(field, arg));
        }

        public IFilterAdapter<FilterDefinition<T>> Has(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.ElemMatch<object>(field, $"{{$eq: {arg}}}"));
        }

        public IFilterAdapter<FilterDefinition<T>> Equality(object comparable, object arg)
        {
            FieldDefinition<T, object> field = comparable.ToString();
            return new MongoFilterAdapter<T>(_filterBuilder.Eq(field, arg));
        }

        public FilterDefinition<T> GetResult() 
        {
            return _filter;
        }
    }
}