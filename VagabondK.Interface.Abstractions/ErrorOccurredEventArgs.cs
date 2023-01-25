using System;

namespace VagabondK.Interface
{
    /// <summary>
    /// 인터페이스 처리 중에 발생한 오류에 대한 이벤트 매개변수
    /// </summary>
    public class ErrorOccurredEventArgs : EventArgs
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="exception">오류 정보</param>
        /// <param name="direction">오류가 발생했을 때의 인터페이스 처리 방향</param>
        public ErrorOccurredEventArgs(Exception exception, ErrorDirection direction)
        {
            Exception = exception;
            Direction = direction;
        }

        /// <summary>
        /// 오류 정보
        /// </summary>
        public Exception Exception { get; }
        /// <summary>
        /// 오류가 발생했을 때의 인터페이스 처리 방향
        /// </summary>
        public ErrorDirection Direction { get; }
    }
}
