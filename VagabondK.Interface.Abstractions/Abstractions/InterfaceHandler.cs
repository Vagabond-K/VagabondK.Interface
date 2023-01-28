using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace VagabondK.Interface.Abstractions
{
    /// <summary>
    /// 인터페이스 처리기
    /// </summary>
    public abstract class InterfaceHandler : INotifyPropertyChanged
    {
        internal DateTime? timeStamp;
        private InterfacePoint point;
        private InterfaceMode mode;

        internal bool SetProperty<TProperty>(ref TProperty target, in TProperty value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<TProperty>.Default.Equals(target, value))
            {
                target = value;
                RaisePropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 비동기로 로컬 값 재전송
        /// </summary>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public abstract Task<bool> SendLocalValueAsync();

        /// <summary>
        /// 로컬 값 재전송
        /// </summary>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public abstract bool SendLocalValue();

        /// <summary>
        /// 로컬 값 가져오기
        /// </summary>
        /// <typeparam name="T">가져올 값 형식</typeparam>
        /// <returns>로컬 값</returns>
        public abstract T GetValue<T>();

        /// <summary>
        /// 값을 수신했을 때 발생하는 이벤트입니다.
        /// </summary>
        public event ReceivedEventHandler Received;

        internal void OnReceived() => Received?.Invoke(this);
        internal abstract void SetReceivedOtherTypeValue<T>(in T value, in DateTime? timeStamp);

        internal void RaiseErrorOccurred(Exception exception, ErrorDirection direction)
            => ErrorOccurred?.Invoke(this, new ErrorOccurredEventArgs(exception, direction));

        /// <summary>
        /// 생성자
        /// </summary>
        protected InterfaceHandler() : this(InterfaceMode.TwoWay) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="mode">인터페이스 모드</param>
        protected InterfaceHandler(InterfaceMode mode)
        {
            Mode = mode;
        }

        /// <summary>
        /// 임의의 속성 값 변경 이벤트 발생 메서드
        /// </summary>
        /// <param name="propertyName">변경된 속성 이름</param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// 인터페이스 포인트
        /// </summary>
        public InterfacePoint Point { get => point; internal set => SetProperty(ref point, value); }

        /// <summary>
        /// 인터페이스 모드
        /// </summary>
        public InterfaceMode Mode { get => mode; set => SetProperty(ref mode, value); }

        /// <summary>
        /// 인터페이스 값의 적용 일시
        /// </summary>
        public DateTime? TimeStamp { get => timeStamp; internal set => SetProperty(ref timeStamp, value); }

        /// <summary>
        /// 인터페이스 값의 형식
        /// </summary>
        public abstract Type ValueType { get; }

        /// <summary>
        /// 인터페이스 오류 발생 이벤트
        /// </summary>
        public event ErrorOccurredEventHandler ErrorOccurred;

        /// <summary>
        /// 속성 값이 변경될 때 발생합니다.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 비동기로 값 전송
        /// </summary>
        /// <typeparam name="T">보낼 값 형식</typeparam>
        /// <param name="value">보낼 값</param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public Task<bool> SendAsync<T>(in T value) => SendAsync(value, null, null);
        /// <summary>
        /// 비동기로 값 전송
        /// </summary>
        /// <typeparam name="T">보낼 값 형식</typeparam>
        /// <param name="value">보낼 값</param>
        /// <param name="timeStamp">보낼 값의 적용 일시</param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public Task<bool> SendAsync<T>(in T value, in DateTime? timeStamp) => SendAsync(value, timeStamp, null);
        /// <summary>
        /// 비동기로 값 전송
        /// </summary>
        /// <typeparam name="T">보낼 값 형식</typeparam>
        /// <param name="value">보낼 값</param>
        /// <param name="cancellationToken">비동기 작업 취소 토큰</param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public Task<bool> SendAsync<T>(in T value, in CancellationToken? cancellationToken) => SendAsync(value, null, cancellationToken);
        /// <summary>
        /// 비동기로 값 전송
        /// </summary>
        /// <typeparam name="T">보낼 값 형식</typeparam>
        /// <param name="value">보낼 값</param>
        /// <param name="timeStamp">보낼 값의 적용 일시</param>
        /// <param name="cancellationToken">비동기 작업 취소 토큰</param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public abstract Task<bool> SendAsync<T>(in T value, in DateTime? timeStamp, in CancellationToken? cancellationToken);

        /// <summary>
        /// 값 전송
        /// </summary>
        /// <typeparam name="T">보낼 값 형식</typeparam>
        /// <param name="value">보낼 값</param>
        /// <returns>전송 성공 여부</returns>
        public bool Send<T>(in T value) => Send(value, null);
        /// <summary>
        /// 값 전송
        /// </summary>
        /// <typeparam name="T">보낼 값 형식</typeparam>
        /// <param name="value">보낼 값</param>
        /// <param name="timeStamp">보낼 값의 적용 일시</param>
        /// <returns>전송 성공 여부</returns>
        public abstract bool Send<T>(in T value, in DateTime? timeStamp);

        /// <summary>
        /// 값 전송 커맨드를 가져옵니다.
        /// </summary>
        public abstract ISendInterfaceValueCommand SendCommand { get; }
    }
}
