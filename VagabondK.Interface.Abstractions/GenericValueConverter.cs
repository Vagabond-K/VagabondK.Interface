using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace VagabondK.Interface
{
    public static class GenericValueConverter
    {
        public static To To<From, To>(this From value) => GetConvert<From, To>()(value);
        public static Func<From, To> GetConvert<From, To>() => ValueConverter<To>.GetConvert<From>();

        class ValueConverter<TValue>
        {
            private static readonly Dictionary<Type, Delegate> convertDelegates = new Dictionary<Type, Delegate>();
            public static Func<T, TValue> GetConvert<T>()
            {
                var valueType = typeof(T);
                lock (convertDelegates)
                {
                    var targetType = typeof(TValue);
                    if (!convertDelegates.TryGetValue(valueType, out var @delegate) || !(@delegate is Func<T, TValue> func))
                    {
                        var valueParameter = Expression.Parameter(valueType);
                        Expression convert = valueParameter;
                        if (valueType != targetType && !targetType.IsAssignableFrom(valueType))
                        {
                            try
                            {
                                convert = Expression.Convert(valueParameter, targetType);
                            }
                            catch
                            {
                                var convertToMethod = typeof(Convert).GetMethod($"To{targetType.Name}", new[] { valueType });
                                if (convertToMethod != null)
                                    convert = Expression.Call(convertToMethod, convert);
                            }
                        }

                        func = Expression.Lambda<Func<T, TValue>>(convert, valueParameter).Compile();

                        convertDelegates[valueType] = func;
                    }
                    return func;
                }
            }
        }
    }
}
