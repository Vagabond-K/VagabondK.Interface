using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.AccessControl;

namespace VagabondK.Interface
{
    /// <summary>
    /// 제네릭 값 변환 관련 메서드 제공 클래스
    /// </summary>
    public static class GenericValueConverter
    {
        static class Converter<TSource, TTarget>
        {
            static Converter()
            {
                var sourceType = typeof(TSource);
                var targetType = typeof(TTarget);
                var valueParameter = Expression.Parameter(sourceType);
                Expression convert = valueParameter;
                if (sourceType != targetType)
                {
                    try
                    {
                        convert = Expression.Convert(valueParameter, targetType);
                    }
                    catch
                    {
                        var convertToMethod = typeof(Convert).GetMethod($"To{targetType.Name}", new[] { sourceType });
                        if (convertToMethod != null)
                            convert = Expression.Call(convertToMethod, convert);
                    }
                }

                func = Expression.Lambda<Func<TSource, TTarget>>(convert, valueParameter).Compile();
            }
            public static Func<TSource, TTarget> func;
        }

        /// <summary>
        /// 값을 TSource 형식에서 TTarget 형식으로 변환합니다.
        /// </summary>
        /// <typeparam name="TSource">변환할 값의 형식</typeparam>
        /// <typeparam name="TTarget">변환 목표 형식</typeparam>
        /// <param name="value">변환할 값</param>
        /// <returns>변환된 값</returns>
        public static TTarget To<TSource, TTarget>(this TSource value) => Converter<TSource, TTarget>.func(value);
    }
}
