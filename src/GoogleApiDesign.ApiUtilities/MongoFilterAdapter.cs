using System.Collections.Generic;
using GoogleApiDesign.ApiUtilities.Contracts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GoogleApiDesign.ApiUtilities
{
    public class MongoFilterAdapter : IFilterAdapter
    {
        private readonly FilterDefinitionBuilder<object> _filterBuilder = Builders<object>.Filter;
        // Temporary filters, to be used for keeping track of filters and combining them (see .And() and .Or())
        private readonly List<FilterDefinition<object>> _tempFilters = new();
        
        private FilterDefinition<object> _filter = FilterDefinition<object>.Empty;

        public IFilterAdapter And(List<object> list)
        {
            _filter = _filterBuilder.And(_tempFilters);
            _tempFilters.Clear();

            return this;
        }

        public IFilterAdapter Or(List<object> list)
        {
            _filter = _filterBuilder.Or(_tempFilters);
            _tempFilters.Clear();
            
            return this;
        }

        public IFilterAdapter Not(object simple)
        {
            _filter = _filterBuilder.Not((simple as MongoFilterAdapter)!
                .GetResult<FilterDefinition<object>>());
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
            _tempFilters.Add(_filterBuilder.Lt(comparable.ToString(), arg));
            return this;
        }

        public IFilterAdapter LessThanEquals(object comparable, object arg)
        {
            _filter = _filterBuilder.Lte(comparable.ToString(), arg);
            _tempFilters.Add(_filterBuilder.Lte(comparable.ToString(), arg));
            
            return this;
        }

        public IFilterAdapter GreaterThanEquals(object comparable, object arg)
        {
            _filter = _filterBuilder.Gte(comparable.ToString(), arg);
            _tempFilters.Add(_filterBuilder.Gte(comparable.ToString(), arg));
            return this;
        }

        public IFilterAdapter GreaterThan(object comparable, object arg)
        {
            _filter = _filterBuilder.Gt(comparable.ToString(), arg);
            _tempFilters.Add(_filterBuilder.Gt(comparable.ToString(), arg));
            return this;
        }

        public IFilterAdapter NotEquals(object comparable, object arg)
        {
            _filter = _filterBuilder.Ne(comparable.ToString(), arg);
            _tempFilters.Add(_filterBuilder.Ne(comparable.ToString(), arg));
            return this;
        }

        public IFilterAdapter Has(object comparable, object arg)
        {
            _filter = _filterBuilder.ElemMatch<object>(comparable.ToString(), $"{{$eq: {arg}}}");
            _tempFilters.Add(_filterBuilder.ElemMatch<object>(comparable.ToString(), $"{{$eq: {arg}}}"));
            return this;
        }

        public IFilterAdapter Equality(object comparable, object arg)
        {
            _filter = _filterBuilder.Eq(comparable.ToString(), arg);
            _tempFilters.Add(_filterBuilder.Eq(comparable.ToString(), arg));
            return this;
        }

        public T GetResult<T>() where T : class
        {
            return _filter as T;
        }
    }
}