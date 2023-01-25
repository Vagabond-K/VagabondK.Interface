using System;
using System.Reflection;

namespace VagabondK.Interface.Abstractions
{
    /// <summary>
    /// 인터페이스 바인딩 멤버 정의를 위한 특성
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public abstract class InterfaceBindingAttribute : Attribute
    {
        /// <summary>
        /// 인터페이스 포인트 생성시 호출되는 메서드
        /// </summary>
        /// <param name="memberInfo">바인딩 할 멤버 정보</param>
        /// <param name="rootAttribute">자동 바인딩 시 지정한 최상위 인터페이스 특성</param>
        /// <returns>인터페이스 포인트</returns>
        protected abstract InterfacePoint OnCreatePoint(MemberInfo memberInfo, InterfaceAttribute rootAttribute);

        /// <summary>
        /// 인터페이스 모드
        /// </summary>
        public InterfaceMode Mode { get; set; } = InterfaceMode.TwoWay;

        /// <summary>
        /// 보내기 오류가 발생할 때 값 롤백 여부
        /// </summary>
        public bool RollbackOnSendError { get; set; } = true;
        internal InterfacePoint GetPoint(MemberInfo memberInfo, InterfaceAttribute rootAttribute) => OnCreatePoint(memberInfo, rootAttribute);
    }
}
