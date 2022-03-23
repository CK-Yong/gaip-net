using System.Linq.Expressions;
using System.Reflection;
using Gaip.Net.Core;
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

    public IFilterAdapter<Func<T, bool>> And(List<IFilterAdapter<Func<T, bool>>> list)
    {
        return new LinqFilterAdapter<T>(
            list
                .Select(x => ((LinqFilterAdapter<T>)x)._expression)
                .Aggregate(Expression.AndAlso));
    }

    public IFilterAdapter<Func<T, bool>> Or(List<IFilterAdapter<Func<T, bool>>> list)
    {
        return new LinqFilterAdapter<T>(
            list
                .Select(x => ((LinqFilterAdapter<T>)x)._expression)
                .Aggregate(Expression.OrElse));
    }

    public IFilterAdapter<Func<T, bool>> Not(IFilterAdapter<Func<T,bool>> simple)
    {
        var binaryExpr = ((LinqFilterAdapter<T>)simple)._expression as BinaryExpression;

        Expression result;
        if (binaryExpr.Right is not ConstantExpression)
        {
            var rightNegated = Expression.Not(binaryExpr.Right);
            result = Expression.MakeBinary(binaryExpr.NodeType, binaryExpr.Left, rightNegated);
        }
        else
        {
            result = Expression.Not(binaryExpr);
        }
    
        return new LinqFilterAdapter<T>(result);
    }

    public IFilterAdapter<Func<T, bool>> Equality(object comparable, object arg)
    {
        var val = arg;
        if (arg is StringLiteralValue stringLiteral)
        {
            val = stringLiteral.Value;
        }
        
        var propertyExpr = ToNullSafePropertyExpression(comparable, compExpr => 
            Expression.Equal(compExpr, Expression.Constant(val)));

        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> NotEquals(object comparable, object arg)
    {
        var val = arg;
        if (arg is StringLiteralValue stringLiteral)
        {
            val = stringLiteral.Value;
        }
        
        var propertyExpr = ToNullSafePropertyExpression(comparable, compExpr => 
            Expression.NotEqual(compExpr, Expression.Constant(val)));
        
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
        var propertyExpr = ToNullSafePropertyExpression(comparable, compExpr =>
            Expression.AndAlso(
                Expression.NotEqual(compExpr, Expression.Default(typeof(string))),
                Expression.Call(compExpr, "StartsWith",null, Expression.Constant(strValue)))
        );
        
        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> SuffixSearch(object comparable, string strValue)
    {
        var propertyExpr = ToNullSafePropertyExpression(comparable, compExpr =>
            Expression.AndAlso(
                Expression.NotEqual(compExpr, Expression.Default(typeof(string))),
                Expression.Call(compExpr, "EndsWith",null, Expression.Constant(strValue)))
        );
        
        return new LinqFilterAdapter<T>(propertyExpr);
    }

    public IFilterAdapter<Func<T, bool>> Has(object comparable, object arg)
    {
        var val = arg;
        if (arg is StringLiteralValue stringLiteral)
        {
            val = stringLiteral.Value;
        }
        
        var propertyStrings = comparable.ToString().Split('.');

        return new LinqFilterAdapter<T>(BuildHasExpression(ItemExpr, propertyStrings, val));
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
        
        Type? elementType = null;
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
                elementType = propType.GetGenericArguments()[0];
            }

            if (comparables.Length == 0) 
            {
                // This is the last one. Build and return a result.
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

        // Only non-array and non-IEnumerable types get to this point.
        // This is not an array or IEnumerable, and this is the last property, so we can just return an equality result.
        if (comparables.Length == 0)
        {
            if (arg is SoloWildCardValue)
            {
                // A wildcard by itself indicates that the property is not default.
                return Expression.NotEqual(buildExpression, Expression.Default(propType));
            }

            if (arg is TextValue text)
            {
                // A TextValue means that a property is being checked for non-default.
                // We need to check for null, then add a non-default check.
                var propertyExpr = Expression.Property(buildExpression, text.Value);
                var info = propertyExpr.Member as PropertyInfo;
                var checkDefault = Expression.NotEqual(buildExpression, Expression.Default(propType));
                var evaluateNotDefault = Expression.NotEqual(propertyExpr, Expression.Default(info.PropertyType));
                return Expression.AndAlso(checkDefault, evaluateNotDefault);
            }
            
            // The argument is a literal value, just use equality in this case.
            return Expression.Equal(buildExpression, Expression.Constant(arg));
        }

        // We need to make sure there is a null-check added before continuing traversal.
        var nullCheck = Expression.NotEqual(buildExpression, Expression.Default(propType));
        
        // Needs to continue, but since it's not an IEnumerable, we can just use the property accessor.
        var nextExpression = BuildHasExpression(Expression.Property(buildExpression, comparables[0]), comparables[1..], arg);
        return Expression.AndAlso(nullCheck, nextExpression);
    }

    private static bool TypeIsIEnumerable(Type? type)
    {
        return type != null &&
               (type.IsArray
                // Makes sure that IEnumerable<T> itself is covered by this.
                || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                // Makes sure that derivatives are covered (e.g. IList<T>).
                || type.GetInterfaces()
                    .Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
               // Make sure strings are ignored!
               && type != typeof(string); 
    }

    private static Expression ToNullSafePropertyExpression(string[] comparables, Func<Expression, Expression>? func)
    {
        Expression expr = ItemExpr;

        List<Expression> nullChecks = new List<Expression>();

        for(var i = 0; i < comparables.Length; i++)
        {
            expr = Expression.Property(expr, comparables[i]);
            
            // Null safety, add null-checks for all properties, except for the last traversed property.
            if(i < comparables.Length - 1)
            {
                var member = (expr as MemberExpression).Member;
                var type = (member as PropertyInfo).PropertyType;
            
                if (!type.IsValueType)
                {
                    nullChecks.Add(Expression.NotEqual(expr, Expression.Default(type)));
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
    
    private static Expression ToNullSafePropertyExpression(object comparables, Func<Expression, Expression>? func)
    {
        var strValues = comparables.ToString().Split('.');

        return ToNullSafePropertyExpression(strValues, func);
    }

    public Func<T, bool> GetResult()
    {
        var lambda = Expression.Lambda<Func<T, bool>>(_expression, ItemExpr);
        return lambda.Compile();
    }
}