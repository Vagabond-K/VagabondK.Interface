using System;

namespace VagabondK.Interface.Abstractions
{
    /// <summary>
    /// 인터페이스 바인딩을 위한 객체를 정의하는 특성
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public abstract class InterfaceAttribute : Attribute
    {
        /// <summary>
        /// 바인딩 할 인터페이스 포인트 형식
        /// </summary>
        public abstract Type InterfacePointType { get; }
    }
}
