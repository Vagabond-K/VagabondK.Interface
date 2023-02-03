using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Interface.Abstractions
{
    /// <summary>
    /// 인터페이스 처리기 컨테이너
    /// </summary>
    public interface IInterfaceHandlerContainer : IEnumerable<InterfaceHandler>
    {
        /// <summary>
        /// 인터페이스 처리기 생성
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <returns>인터페이스 처리기</returns>
        InterfaceHandler<TValue> CreateHandler<TValue>();

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <returns>인터페이스 바인딩</returns>
        InterfaceBinding<TValue> SetBinding<TValue>(object target, string memberName);
        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="mode">인터페이스 모드</param>
        /// <returns>인터페이스 바인딩</returns>
        InterfaceBinding<TValue> SetBinding<TValue>(object target, string memberName, InterfaceMode mode);

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="rollbackOnSendError">보내기 오류가 발생할 때 값 롤백 여부</param>
        /// <returns>인터페이스 바인딩</returns>
        InterfaceBinding<TValue> SetBinding<TValue>(object target, string memberName, bool rollbackOnSendError);

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="mode">인터페이스 모드</param>
        /// <param name="rollbackOnSendError">보내기 오류가 발생할 때 값 롤백 여부</param>
        /// <returns>인터페이스 바인딩</returns>
        InterfaceBinding<TValue> SetBinding<TValue>(object target, string memberName, InterfaceMode mode, bool rollbackOnSendError);

        /// <summary>
        /// 인터페이스 처리기 추가
        /// </summary>
        /// <param name="handler">추가할 인터페이스 처리기</param>
        void Add(InterfaceHandler handler);

        /// <summary>
        /// 인터페이스 처리기 제거
        /// </summary>
        /// <param name="handler">제거할 인터페이스 처리기</param>
        /// <returns>제거 성공 여부</returns>
        bool Remove(InterfaceHandler handler);

        /// <summary>
        /// 기본 인터페이스 처리기를 가져옵니다.
        /// </summary>
        InterfaceHandler DefaultHandler { get; }
    }

    /// <summary>
    /// 인터페이스 처리기 컨테이너
    /// </summary>
    /// <typeparam name="TValue">값 형식</typeparam>
    public interface IInterfaceHandlerContainer<TValue> : IInterfaceHandlerContainer
    {
    }
}
