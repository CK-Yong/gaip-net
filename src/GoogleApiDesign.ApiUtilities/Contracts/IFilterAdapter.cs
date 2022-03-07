using System.Collections.Generic;

namespace GoogleApiDesign.ApiUtilities.Contracts
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
        T GetResult<T>() where T : class;
    }
}