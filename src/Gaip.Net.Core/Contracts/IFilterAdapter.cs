using System.Collections.Generic;

namespace Gaip.Net.Core.Contracts
{
    public interface IFilterAdapter
    {
        IFilterAdapter And(List<object> list);
        IFilterAdapter Or(List<object> list);
        IFilterAdapter Not(object simple);
        IFilterAdapter PrefixSearch(object comparable, string strValue);
        IFilterAdapter SuffixSearch(object comparable, string strValue);
        IFilterAdapter LessThan(object comparable, object arg);
        IFilterAdapter LessThanEquals(object comparable, object arg);
        IFilterAdapter GreaterThanEquals(object comparable, object arg);
        IFilterAdapter GreaterThan(object comparable, object arg);
        IFilterAdapter NotEquals(object comparable, object arg);
        IFilterAdapter Has(object comparable, object arg);
        IFilterAdapter Equality(object comparable, object arg);
        
        /// <summary>
        /// Gets a representation of the resulting filter. May be different dependent on the adapter implementation.
        /// </summary>
        /// <typeparam name="T">The type that the resulting filter should be cast to.</typeparam>
        T GetResult<T>() where T : class;
    }
}