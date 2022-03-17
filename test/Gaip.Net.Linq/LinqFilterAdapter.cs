using System.Linq.Expressions;
using System.Reflection;
using Gaip.Net.Core.Contracts;

namespace Gaip.Net.Linq;

public class LinqFilterAdapter<T> : IFilterAdapter<Func<T, bool>>
{
    private readonly Expression _expression;

    private static ParameterExpression ItemExpr = Expression.Parameter(typeof(T), "item");
    
    public LinqFilterAdapter()
    {
        
    }
    
    private LinqFilterAdapter(Expression expression)
    {
        _expression = expression;
    }

    public IFilterAdapter<Func<T, bool>> And(List<object> list)
    {
        throw new NotImplementedException();
    }

    public IFilterAdapter<Func<T, bool>> Or(List<object> list)
    {
        throw new NotImplementedException();
    }

    public IFilterAdapter<Func<T, bool>> Not(object simple)
    {
        throw new NotImplementedException();
    }

    public IFilterAdapter<Func<T, bool>> Equality(object comparable, object arg)
    {
        var propertyExpr = ToNullSafeExpression(comparable, compExpr => 
            Expression.Equal(compExpr, Expression.Constant(arg)));

        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> NotEquals(object comparable, object arg)
    {
        var propertyExpr = ToNullSafeExpression(comparable, compExpr => 
            Expression.NotEqual(compExpr, Expression.Constant(arg)));
        
        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> LessThan(object comparable, object arg)
    {
        var propertyExpr = ToNullSafeExpression(comparable, compExpr => 
            Expression.LessThan(compExpr, Expression.Constant(arg)));
        
        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> LessThanEquals(object comparable, object arg)
    {
        var propertyExpr = ToNullSafeExpression(comparable, compExpr => 
            Expression.LessThanOrEqual(compExpr, Expression.Constant(arg)));
        
        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> GreaterThan(object comparable, object arg)
    {
        var propertyExpr = ToNullSafeExpression(comparable, compExpr => 
            Expression.GreaterThan(compExpr, Expression.Constant(arg)));
        
        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> GreaterThanEquals(object comparable, object arg)
    {
        var propertyExpr = ToNullSafeExpression(comparable, compExpr => 
            Expression.GreaterThanOrEqual(compExpr, Expression.Constant(arg)));
        
        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> PrefixSearch(object comparable, string strValue)
    {
        throw new NotImplementedException();
    }


    public IFilterAdapter<Func<T, bool>> SuffixSearch(object comparable, string strValue)
    {
        throw new NotImplementedException();
    }

    public IFilterAdapter<Func<T, bool>> Has(object comparable, object arg)
    {
        throw new NotImplementedException();
    }

    private static Expression ToNullSafeExpression(object comparables, Func<Expression, Expression> func)
    {
        var strValues = comparables.ToString().Split('.');
        
        Expression expr = ItemExpr;

        List<Expression> nullChecks = new List<Expression>();

        for(var i = 0; i < strValues.Length; i++)
        {
            expr = Expression.Property(expr, strValues[i]);
            
            // Null safety, add null-checks for all properties, except for the last traversed property.
            if(i < strValues.Length - 1)
            {
                var member = (expr as MemberExpression).Member;
                var type = (member as PropertyInfo).PropertyType;
            
                if (!type.IsValueType)
                {
                    nullChecks.Add(Expression.NotEqual(expr, Expression.Constant(null, type)));
                }
            }
        }

        // Add the given comparison, now that we have the comparatorExpression.
        var comparison = func(expr);
        
        if (nullChecks.Count == 0)
        {
            return comparison;
        }

        // If null-checks are available, add them to the expression.
        var nullCheckExpr = nullChecks[0];
        if (nullChecks.Count > 1)
        {
            for (var i = 1; i < nullChecks.Count; i++)
            {
                nullCheckExpr = Expression.AndAlso(nullCheckExpr, nullChecks[i]);
            }
        }
        
        return Expression.AndAlso(nullCheckExpr, comparison);
    }

    public Func<T, bool> GetResult()
    {
        var lambda = Expression.Lambda<Func<T, bool>>(_expression, ItemExpr);
        return lambda.Compile();
    }
}