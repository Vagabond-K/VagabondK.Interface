using System;
using System.Collections.Generic;
using System.Text;
using VagabondK.Interface.Abstractions;

namespace VagabondK.Interface
{
    /// <summary>
    /// 인터페이스 처리기 컨테이너 확장 메서드 모음
    /// </summary>
    public static class InterfaceHandlerContainerExtensions
    {
        /// <summary>
        /// 인터페이스 처리기 생성
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="container">인터페이스 처리기 컨테이너</param>
        /// <returns>인터페이스 처리기</returns>
        public static InterfaceHandler<TValue> CreateHandler<TValue>(this IInterfaceHandlerContainer<TValue> container)
            => container.CreateHandler<TValue>();

        /// <summary>
        /// 기본 인터페이스 처리기를 가져옵니다.
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="container">인터페이스 처리기 컨테이너</param>
        /// <returns>기본 인터페이스 처리기</returns>
        public static InterfaceHandler<TValue> GetDefaultHandler<TValue>(this IInterfaceHandlerContainer<TValue> container)
            => container?.DefaultHandler as InterfaceHandler<TValue>;

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="container">인터페이스 처리기 컨테이너</param>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <returns>인터페이스 바인딩</returns>
        public static InterfaceBinding<TValue> SetBinding<TValue>(this IInterfaceHandlerContainer<TValue> container, object target, string memberName)
            => container.SetBinding<TValue>(target, memberName);

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="container">인터페이스 처리기 컨테이너</param>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="mode">인터페이스 모드</param>
        /// <returns>인터페이스 바인딩</returns>
        public static InterfaceBinding<TValue> SetBinding<TValue>(this IInterfaceHandlerContainer<TValue> container, object target, string memberName, InterfaceMode mode)
            => container.SetBinding(target, memberName, mode);

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="container">인터페이스 처리기 컨테이너</param>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="rollbackOnSendError">보내기 오류가 발생할 때 값 롤백 여부</param>
        /// <returns>인터페이스 바인딩</returns>
        public static InterfaceBinding<TValue> SetBinding<TValue>(this IInterfaceHandlerContainer<TValue> container, object target, string memberName, bool rollbackOnSendError)
            => container.SetBinding(target, memberName, rollbackOnSendError);

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="container">인터페이스 처리기 컨테이너</param>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="mode">인터페이스 모드</param>
        /// <param name="rollbackOnSendError">보내기 오류가 발생할 때 값 롤백 여부</param>
        /// <returns>인터페이스 바인딩</returns>
        public static InterfaceBinding<TValue> SetBinding<TValue>(this IInterfaceHandlerContainer<TValue> container, object target, string memberName, InterfaceMode mode, bool rollbackOnSendError)
            => container.SetBinding(target, memberName, mode, rollbackOnSendError);
    }
}
