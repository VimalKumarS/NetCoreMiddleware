using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace WebApplication_LearnMiddleware
{
    public abstract class CacheBase : ICache
    {
        public TResult InvokeCached<TResult>(
                Expression<Func<TResult>> expression,
                CachePolicy policy)
        {
            string cacheKey;
            MethodInfo method;
            object instance;
            object[] arguments;
            // extract the key as well as the items needed to invoke 
            // the expression from the arguments
            ParseExpression(expression, out cacheKey, out method, out instance, out arguments);

            // if the value is in the cache, return it
            TResult cachedValue;
            if (this.TryFind(cacheKey, policy, out cachedValue))
            {
                return cachedValue;
            }

            // compute the value
            var computedValue = (TResult)method.Invoke(instance, arguments);
            // check if if it's valid for caching
            ThrowIfNotCacheable(computedValue);
            // add it to the cache
            this.Add(cacheKey, computedValue, policy);
            return computedValue;
        }

        private static void ThrowIfNotCacheable(object value)
        {
            if (value != null
                && !(value is IConvertible)
                && !(value is ICacheable))
            {
                throw new InvalidOperationException("value of type " + value.GetType() + " is safe to cache");
            }
        }
        private static void ParseExpression(
                LambdaExpression expression,
                out string cacheKey,
                out MethodInfo method,
                out object instance,
                out object[] arguments)
        {
            // if the expression is of the form () => [instance.]SomeFunction(...), the body is a MethodCallExpression
            var methodCall = (MethodCallExpression)expression.Body;

            method = methodCall.Method;
            instance = methodCall.Object != null ? GetValue(methodCall.Object) : null;
            arguments = new object[methodCall.Arguments.Count];

            // build up a key for caching based on the method and it's parameters
            var keyBuilder = new CacheKeyBuilder();
            keyBuilder.By(method.DeclaringType)
                        .By(method.MetadataToken);
                       // .By(method.GetGenericArguments())
                        //.By(instance);
            for (var i = 0; i < methodCall.Arguments.Count; ++i)
            {
                arguments[i] = GetValue(methodCall.Arguments[i]);
                keyBuilder.By(arguments[i]);
            }
            cacheKey = keyBuilder.ToString();
        }

        protected abstract bool TryFind<TResult>(
                string key,
                CachePolicy policy,
                out TResult value);
        protected abstract void Add<TResult>(
                string key,
                TResult value,
                CachePolicy policy);

        private static object GetValue(Expression expression)
        {
            switch (expression.NodeType)
            {
                // we special-case constant and member access because these handle 
                // the majority of common cases. For example, passing a local 
                // variable as an argument translates to a field reference on the 
                // closure object in expression land
                case ExpressionType.Constant:
                    return ((ConstantExpression)expression).Value;
                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)expression;
                    var instance = memberExpression.Expression != null
                                    ? GetValue(memberExpression.Expression)
                                    : null;
                    var field = memberExpression.Member as FieldInfo;
                    return field != null
                        ? field.GetValue(instance)
                        : ((PropertyInfo)memberExpression.Member)
                                            .GetValue(instance);
                default:
                    // this should always work if the expression CAN be evaluated 
                    // (it can't if it references unbound parameters, for example)
                    // however, compilation is slow so the cases above 
                    // provide valuable performance
                    var lambda = Expression.Lambda<Func<object>>(
                                    Expression.Convert(expression, typeof(object))
                                );
                    return lambda.Compile()();
            }
        }
    }
    public interface ICacheable { }
}
