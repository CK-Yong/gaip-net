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
        var propertyExpr = ToNullSafePropertyExpression(comparable, compExpr => 
            Expression.Equal(compExpr, Expression.Constant(arg)));

        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> NotEquals(object comparable, object arg)
    {
        var propertyExpr = ToNullSafePropertyExpression(comparable, compExpr => 
            Expression.NotEqual(compExpr, Expression.Constant(arg)));
        
        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> LessThan(object comparable, object arg)
    {
        var propertyExpr = ToNullSafePropertyExpression(comparable, compExpr => 
            Expression.LessThan(compExpr, Expression.Constant(arg)));
        
        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> LessThanEquals(object comparable, object arg)
    {
        var propertyExpr = ToNullSafePropertyExpression(comparable, compExpr => 
            Expression.LessThanOrEqual(compExpr, Expression.Constant(arg)));
        
        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> GreaterThan(object comparable, object arg)
    {
        var propertyExpr = ToNullSafePropertyExpression(comparable, compExpr => 
            Expression.GreaterThan(compExpr, Expression.Constant(arg)));
        
        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> GreaterThanEquals(object comparable, object arg)
    {
        var propertyExpr = ToNullSafePropertyExpression(comparable, compExpr => 
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
        var propertyStrings = comparable.ToString().Split('.');

        return new LinqFilterAdapter<T>(BuildHasExpression(ItemExpr, propertyStrings, arg));
    }

    private static Expression BuildHasExpression(Expression buildExpression, string[] comparables, object arg)
    {
        PropertyInfo propertyInfo;
        if (buildExpression is MemberExpression memberExpression)
        {
            propertyInfo = memberExpression.Member as PropertyInfo;
        }
        else
        {
            return BuildHasExpression(Expression.Property(buildExpression, comparables[0]), comparables[1..], arg);
        }
        
        Type? elementType;
        // Determine the type of the current Type, if it is an array or IEnumerable<T>.
        var propType = propertyInfo.PropertyType;
        if (TypeIsIEnumerable(propType))
        {
            // First determine the element-type, we need it in all cases.
            if (propType.IsArray)
            {
                elementType = propType.GetElementType();
            }
            else // This is an IEnumerable, get type from generic argument.
            {
                elementType = propType?.GetInterfaces()
                    ?.SingleOrDefault(x => x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    ?.GetGenericArguments()[0];
            }

            if (comparables.Length == 0) // This is the last one. Build and return a result.
            {
                var parameter = Expression.Parameter(elementType, "comparable");
                var lambda = Expression.Lambda(Expression.Equal(parameter, Expression.Constant(arg)), parameter);
                var anyExpression = Expression.Call(typeof(Enumerable), "Any",
                    new[] { elementType }, buildExpression, lambda);
                return anyExpression;
            }
            else
            {
                // Build an Any() clause with a property accessor clause.
                var parameter = Expression.Parameter(elementType, "comparable");
                var lambda = Expression.Lambda(BuildHasExpression(Expression.Property(parameter, comparables[0]), comparables[1..], arg), parameter);
                var anyExpression = Expression.Call(typeof(Enumerable), "Any",
                    new[] { elementType }, buildExpression, lambda);
                return anyExpression;
            }
        }

        return Expression.Equal(buildExpression, Expression.Constant(arg));
    }

    private static bool PropertyIsIEnumerable(Expression expr)
    {
        var propertyType = ((expr as MemberExpression)?.Member as PropertyInfo)?.PropertyType;
        return TypeIsIEnumerable(propertyType);
    }

    private static bool TypeIsIEnumerable(Type? type)
    {
        return type != null && (type.IsArray || type.GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)));
    }

    private static Expression ToNullSafePropertyExpression(object comparables, Func<Expression, Expression>? func)
    {
        var strValues = comparables.ToString().Split('.');
        
        Expression expr = ItemExpr;

        List<Expression> nullChecks = new List<Expression>();

        for(var i = 0; i < strValues.Length; i++)
        {
            if (PropertyIsIEnumerable(expr))
            {
                // Do not check for nulls on IEnumerable properties.
                continue;
            }
            
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