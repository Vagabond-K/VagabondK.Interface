using System;
using VagabondK.Interface.Abstractions;

namespace VagabondK.Interface
{
    /// <summary>
    /// 값을 수신했을 때 발생하는 이벤트의 처리기
    /// </summary>
    /// <param name="handler">값을 받은 인터페이스 처리기</param>
    /// <param name="timeStamp">받은 값의 적용 일시</param>
    public delegate void ReceivedEventHandler(InterfaceHandler handler, in DateTime? timeStamp);

    /// <summary>
    /// 값을 수신했을 때 발생하는 이벤트의 처리기
    /// </summary>
    /// <typeparam name="TValue">받은 값의 형식</typeparam>
    /// <param name="handler">값을 받은 인터페이스 처리기</param>
    /// <param name="value">받은 값</param>
    /// <param name="timeStamp">받은 값의 적용 일시</param>
    public delegate void ReceivedEventHandler<TValue>(InterfaceHandler<TValue> handler, in TValue value, in DateTime? timeStamp);
}