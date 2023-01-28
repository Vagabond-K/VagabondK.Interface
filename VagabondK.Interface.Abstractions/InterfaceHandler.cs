using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using VagabondK.Interface.Abstractions;

namespace VagabondK.Interface
{
    /// <summary>
    /// 인터페이스 처리기
    /// </summary>
    /// <typeparam name="TValue">인터페이스 값 형식</typeparam>
    public class InterfaceHandler<TValue> : InterfaceHandler
    {
        internal TValue value;
        private readonly Lazy<SendInterfaceValueCommand<TValue>> sendCommand;

        /// <summary>
        /// 로컬 값 설정
        /// </summary>
        /// <param name="value">설정할 값</param>
        /// <param name="timeStamp">설정할 값의 적용 일시</param>
        protected void SetLocalValue(in TValue value, in DateTime? timeStamp)
        {
            SetProperty(ref this.value, value, nameof(Value));
            SetProperty(ref this.timeStamp, timeStamp, nameof(TimeStamp));
        }

        internal override void SetReceivedOtherTypeValue<T>(in T value, in DateTime? timeStamp)
            => SetReceivedValue(value.To<T, TValue>(), timeStamp);

        internal void SetReceivedValue(in TValue value, in DateTime? timeStamp)
        {
            if (Mode == InterfaceMode.TwoWay || Mode == InterfaceMode.ReceiveOnly)
            {
                SetLocalValue(value, timeStamp);
                OnReceived(value, timeStamp);
                OnReceived();
                Received?.Invoke(this);
            }
        }

        /// <summary>
        /// 값이 수신되었을 때 호출되는 메서드
        /// </summary>
        /// <param name="value">받은 값</param>
        /// <param name="timeStamp">받은 값의 적용 일시</param>
        protected virtual void OnReceived(in TValue value, in DateTime? timeStamp) { }

        /// <summary>
        /// 비동기로 로컬 값 재전송
        /// </summary>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public override Task<bool> SendLocalValueAsync()
            => timeStamp != null ? Point?.OnSendAsyncRequested(value, timeStamp, null) : Task.FromResult(false);
        /// <summary>
        /// 로컬 값 재전송
        /// </summary>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public override bool SendLocalValue()
            => timeStamp != null && (Point?.OnSendRequested(value, timeStamp) ?? false);

        /// <summary>
        /// 생성자
        /// </summary>
        public InterfaceHandler() : this(InterfaceMode.TwoWay) { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="mode">인터페이스 모드</param>
        public InterfaceHandler(InterfaceMode mode) : base(mode)
        {
            sendCommand = new Lazy<SendInterfaceValueCommand<TValue>>(() => new SendInterfaceValueCommand<TValue>(this));
        }

        /// <summary>
        /// 로컬 값
        /// </summary>
        public TValue Value { get => value; protected set => SetProperty(ref this.value, value); }

        /// <summary>
        /// 인터페이스 값의 형식
        /// </summary>
        public override Type ValueType => typeof(TValue);

        /// <summary>
        /// 값을 수신했을 때 발생하는 이벤트입니다.
        /// </summary>
        public new event ReceivedEventHandler<TValue> Received;

        /// <summary>
        /// 로컬 값 가져오기
        /// </summary>
        /// <typeparam name="T">가져올 값 형식</typeparam>
        /// <returns>로컬 값</returns>
        public override T GetValue<T>() => this is InterfaceHandler<T> handler ? handler.value : value.To<TValue, T>();

        /// <summary>
        /// 비동기로 값 전송
        /// </summary>
        /// <param name="value">보낼 값</param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public Task<bool> SendAsync(in TValue value) => SendAsync(value, null, null);
        /// <summary>
        /// 비동기로 값 전송
        /// </summary>
        /// <param name="value">보낼 값</param>
        /// <param name="timeStamp">보낼 값의 적용 일시</param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public Task<bool> SendAsync(in TValue value, in DateTime? timeStamp) => SendAsync(value, timeStamp, null);
        /// <summary>
        /// 비동기로 값 전송
        /// </summary>
        /// <param name="value">보낼 값</param>
        /// <param name="cancellationToken">비동기 작업 취소 토큰</param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public Task<bool> SendAsync(in TValue value, in CancellationToken? cancellationToken) => SendAsync(value, null, cancellationToken);
        /// <summary>
        /// 비동기로 값 전송
        /// </summary>
        /// <param name="value">보낼 값</param>
        /// <param name="timeStamp">보낼 값의 적용 일시</param>
        /// <param name="cancellationToken">비동기 작업 취소 토큰</param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public async Task<bool> SendAsync(TValue value, DateTime? timeStamp, CancellationToken? cancellationToken)
        {
            if ((Mode == InterfaceMode.TwoWay || Mode == InterfaceMode.SendOnly)
                && await (Point?.OnSendAsyncRequested(value, timeStamp, cancellationToken) ?? Task.FromResult(false)))
            {
                SetLocalValue(value, timeStamp);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 값 전송
        /// </summary>
        /// <param name="value">보낼 값</param>
        /// <returns>전송 성공 여부</returns>
        public bool Send(in TValue value) => Send(value, null);
        /// <summary>
        /// 값 전송
        /// </summary>
        /// <param name="value">보낼 값</param>
        /// <param name="timeStamp">보낼 값의 적용 일시</param>
        /// <returns>전송 성공 여부</returns>
        public bool Send(in TValue value, in DateTime? timeStamp)
        {
            if ((Mode == InterfaceMode.TwoWay || Mode == InterfaceMode.SendOnly)
                && (Point?.OnSendRequested(value, timeStamp) ?? false))
            {
                SetLocalValue(value, timeStamp);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 비동기로 값 전송
        /// </summary>
        /// <typeparam name="T">보낼 값 형식</typeparam>
        /// <param name="value">보낼 값</param>
        /// <param name="timeStamp">보낼 값의 적용 일시</param>
        /// <param name="cancellationToken">비동기 작업 취소 토큰</param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public override Task<bool> SendAsync<T>(in T value, in DateTime? timeStamp, in CancellationToken? cancellationToken)
            => SendAsync(value.To<T, TValue>(), timeStamp);

        /// <summary>
        /// 값 전송
        /// </summary>
        /// <typeparam name="T">보낼 값 형식</typeparam>
        /// <param name="value">보낼 값</param>
        /// <param name="timeStamp">보낼 값의 적용 일시</param>
        /// <returns>전송 성공 여부</returns>
        public override bool Send<T>(in T value, in DateTime? timeStamp)
            => Send(value.To<T, TValue>(), timeStamp);

        /// <summary>
        /// 값 전송 커맨드를 가져옵니다.
        /// </summary>
        public override ISendInterfaceValueCommand SendCommand => sendCommand.Value;
    }
}
