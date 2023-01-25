using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace VagabondK.Interface
{
    /// <summary>
    /// 제네릭 값 변환 관련 메서드 제공 클래스
    /// </summary>
    public static class GenericValueConverter
    {
        /// <summary>
        /// 값 변환
        /// </summary>
        /// <typeparam name="From">변환할 값의 형식</typeparam>
        /// <typeparam name="To">변환 목표 형식</typeparam>
        /// <param name="value">변환할 값</param>
        /// <returns>변환된 값</returns>
        public static To To<From, To>(this From value) => GetConverter<From, To>()(value);

        /// <summary>
        /// 값을 변환하기 위한 메서드를 가져옵니다.
        /// </summary>
        /// <typeparam name="From">변환할 값의 형식</typeparam>
        /// <typeparam name="To">변환 목표 형식</typeparam>
        /// <returns>값을 변환하기 위한 메서드</returns>
        public static Func<From, To> GetConverter<From, To>() => ValueConverter<To>.GetConvert<From>();

        class ValueConverter<To>
        {
            private static readonly Dictionary<Type, Delegate> convertDelegates = new Dictionary<Type, Delegate>();
            public static Func<From, To> GetConvert<From>()
            {
                var valueType = typeof(From);
                lock (convertDelegates)
                {
                    var targetType = typeof(To);
                    if (!convertDelegates.TryGetValue(valueType, out var @delegate) || !(@delegate is Func<From, To> func))
                    {
                        var valueParameter = Expression.Parameter(valueType);
                        Expression convert = valueParameter;
                        if (valueType != targetType)
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

                        func = Expression.Lambda<Func<From, To>>(convert, valueParameter).Compile();

                        convertDelegates[valueType] = func;
                    }
                    return func;
                }
            }
        }
    }
}
