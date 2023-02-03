using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.LSElectric;

namespace VagabondK.Interface.LSElectric.Abstractions
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) PLC 인터페이스 포인트
    /// </summary>
    public abstract class PlcPoint : InterfacePoint, INotifyPropertyChanged, INotifyPropertyChanging
    {
        private DeviceVariable deviceVariable;
        internal object writeRequest;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="handlers">인터페이스 처리기</param>
        protected PlcPoint(DeviceVariable deviceVariable, IEnumerable<InterfaceHandler> handlers) : base(handlers)
        {
            this.deviceVariable = deviceVariable;
        }

        internal bool SetProperty<TProperty>(ref TProperty target, in TProperty value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<TProperty>.Default.Equals(target, value))
            {
                RaisePropertyChanging(propertyName);
                target = value;
                RaisePropertyChanged(propertyName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 임의의 속성 값 변경 이벤트 발생 메서드
        /// </summary>
        /// <param name="propertyName">변경된 속성 이름</param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        /// <summary>
        /// 임의의 속성 값 변경 직전 이벤트 발생 메서드
        /// </summary>
        /// <param name="propertyName">변경될 속성 이름</param>
        protected void RaisePropertyChanging([CallerMemberName] string propertyName = null) => PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));

        /// <summary>
        /// 디바이스 변수
        /// </summary>
        public DeviceVariable DeviceVariable { get => deviceVariable; set => SetProperty(ref deviceVariable, value); }

        /// <summary>
        /// 속성 값이 변경될 때 발생합니다.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// 속성 값이 변경되기 직전에 발생합니다.
        /// </summary>
        public event PropertyChangingEventHandler PropertyChanging;

        internal abstract void SetReceivedValue(in DeviceValue deviceValue, in DateTime timeStamp);

        internal new void RaiseErrorOccurred(Exception exception, ErrorDirection errorDirection)
            => base.RaiseErrorOccurred(exception, errorDirection);
    }

    /// <summary>
    /// LS ELECTRIC(구 LS산전) PLC 인터페이스 포인트
    /// </summary>
    /// <typeparam name="TValue">값 형식</typeparam>
    public abstract class PlcPoint<TValue> : PlcPoint, IInterfaceHandlerContainer<TValue>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="handlers">인터페이스 처리기</param>
        protected PlcPoint(DeviceVariable deviceVariable, IEnumerable<InterfaceHandler> handlers) : base(deviceVariable, handlers) { }

        private bool Send<T>(in T value)
        {
            try
            {
                return (Interface as IPlcInterface).Write(this, this is PlcPoint<T> point
                    ? point.ToDeviceValue(value)
                    : ToDeviceValue(value.To<T, TValue>()));
            }
            catch (Exception ex)
            {
                RaiseErrorOccurred(ex, ErrorDirection.Sending);
                return false;
            }
        }

        /// <summary>
        /// 비동기로 값을 전송하고자 할 때 호출되는 메서드
        /// </summary>
        /// <param name="value">전송할 값</param>
        /// <param name="timeStamp">전송할 값의 적용 일시</param>
        /// <param name="cancellationToken"></param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        protected override Task<bool> OnSendAsyncRequested<T>(in T value, in DateTime? timeStamp, in CancellationToken? cancellationToken)
        {
            var localValue = value;
            return cancellationToken != null
                ? Task.Run(() => Send(localValue), cancellationToken.Value)
                : Task.Run(() => Send(localValue));
        }
        /// <summary>
        /// 값을 동기적으로 전송하고자 할 때 호출되는 메서드
        /// </summary>
        /// <param name="value">전송할 값</param>
        /// <param name="timeStamp">전송할 값의 적용 일시</param>
        /// <returns>전송 성공 여부</returns>
        protected override bool OnSendRequested<T>(in T value, in DateTime? timeStamp) => Send(value);

        internal override void SetReceivedValue(in DeviceValue deviceValue, in DateTime timeStamp)
            => SetReceivedValue(ToPointValue(deviceValue), timeStamp);

        /// <summary>
        /// 인터페이스 포인트의 값을 PLC 디바이스 값으로 변환
        /// </summary>
        /// <param name="value">인터페이스 포인트 값</param>
        /// <returns>디바이스 값</returns>
        protected abstract DeviceValue ToDeviceValue(TValue value);

        /// <summary>
        /// PLC 디바이스 값을 인터페이스 포인트 값으로 변환
        /// </summary>
        /// <param name="deviceValue">PLC 디바이스 값</param>
        /// <returns>인터페이스 포인트 값</returns>
        protected abstract TValue ToPointValue(DeviceValue deviceValue);
    }
}
