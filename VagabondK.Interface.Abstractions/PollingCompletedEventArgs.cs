using System;

namespace VagabondK.Interface
{
    /// <summary>
    /// 1주기의 값 읽기 요청과 응답이 완료되었을 때 발생하는 이벤트의 매개변수
    /// </summary>
    public class PollingCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="succeed">성공 여부</param>
        /// <param name="exception">값 읽기 요청 중에 발생한 오류 정보</param>
        public PollingCompletedEventArgs(bool succeed, Exception exception)
        {
            Succeed = succeed;
            Exception = exception;
        }

        /// <summary>
        /// 성공 여부
        /// </summary>
        public bool Succeed { get; }

        /// <summary>
        /// 값 읽기 요청 중에 발생한 오류 정보
        /// </summary>
        public Exception Exception { get; }
    }
}