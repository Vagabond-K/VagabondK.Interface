using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace VagabondK.Interface
{
    /// <summary>
    /// 제네릭 값 변환 관련 메서드 제공 클래스
    /// </summary>
    /// <typeparam name="TTarget">변환 목표 형식</typeparam>
    public class GenericValueConverter<TTarget>
    {
        private static readonly Dictionary<Type, Delegate> convertDelegates = new Dictionary<Type, Delegate>();

        /// <summary>
        /// 값을 변환하기 위한 메서드를 가져옵니다.
        /// </summary>
        /// <typeparam name="TSource">변환할 값의 형식</typeparam>
        /// <returns>값을 변환하기 위한 메서드</returns>
        public static Func<TSource, TTarget> GetConverter<TSource>()
        {
            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);
            if (!convertDelegates.TryGetValue(sourceType, out var @delegate) || !(@delegate is Func<TSource, TTarget> func))
            {
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

                convertDelegates[sourceType] = func;
            }
            return func;
        }

        private Delegate lastUsedConverter;

        /// <summary>
        /// 값 변환
        /// </summary>
        /// <typeparam name="TSource">변환할 값의 형식</typeparam>
        /// <param name="value">변환할 값</param>
        /// <returns>변환된 값</returns>
        public TTarget Convert<TSource>(in TSource value)
        {
            if (!(lastUsedConverter is Func<TSource, TTarget> converter))
                lastUsedConverter = converter = GetConverter<TSource>();
            return converter(value);
        }
    }
}
