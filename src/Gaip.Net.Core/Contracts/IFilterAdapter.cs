using System.Collections.Generic;

namespace Gaip.Net.Core.Contracts
{
    public interface IFilterAdapter<TResult>
    {
        IFilterAdapter<TResult> And(List<IFilterAdapter<TResult>> list);
        IFilterAdapter<TResult> Or(List<IFilterAdapter<TResult>> list);
        IFilterAdapter<TResult> Not(IFilterAdapter<TResult> simple);
        IFilterAdapter<TResult> Equality(object comparable, object arg);
        IFilterAdapter<TResult> NotEquals(object comparable, object arg);
        IFilterAdapter<TResult> LessThan(object comparable, object arg);
        IFilterAdapter<TResult> LessThanEquals(object comparable, object arg);
        IFilterAdapter<TResult> GreaterThan(object comparable, object arg);
        IFilterAdapter<TResult> GreaterThanEquals(object comparable, object arg);
        IFilterAdapter<TResult> PrefixSearch(object comparable, string strValue);
        IFilterAdapter<TResult> SuffixSearch(object comparable, string strValue);
        IFilterAdapter<TResult> Has(object comparable, object arg);

        /// <summary>
        /// Gets a representation of the resulting filter. May be different dependent on the adapter implementation.
        /// </summary>
        /// <typeparam name="T">The type that the resulting filter should be cast to.</typeparam>
        TResult GetResult();
    }
}