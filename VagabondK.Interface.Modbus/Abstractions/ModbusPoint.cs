using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus.Abstractions
{
    /// <summary>
    /// Modbus 인터페이스 포인트
    /// </summary>
    public abstract class ModbusPoint : InterfacePoint, INotifyPropertyChanged, INotifyPropertyChanging
    {
        private byte slaveAddress;
        private ModbusObjectType objectType;
        private ushort address;
        private ushort? requestAddress;
        private ushort? requestLength;
        private bool? useMultiWriteFunction;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        /// <param name="useMultiWriteFunction">다중 쓰기 Function 사용 여부</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        protected ModbusPoint(byte slaveAddress, ushort address, ushort? requestAddress, ushort? requestLength, bool? useMultiWriteFunction, IEnumerable<InterfaceHandler> handlers)
            : base(handlers)
        {
            if (requestAddress != null && address < requestAddress.Value) throw new ArgumentOutOfRangeException(nameof(requestAddress));

            this.slaveAddress = slaveAddress;
            this.address = address;
            this.requestAddress = requestAddress;
            this.requestLength = requestLength;
            this.useMultiWriteFunction = useMultiWriteFunction;
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

        internal bool SetProperty<TProperty>(ref TProperty target, in TProperty value, Action beforeChangedEvent, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<TProperty>.Default.Equals(target, value))
            {
                RaisePropertyChanging(propertyName);
                target = value;
                beforeChangedEvent?.Invoke();
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
        /// 슬레이브 주소
        /// </summary>
        public byte SlaveAddress { get => slaveAddress; set => SetProperty(ref slaveAddress, value); }
        /// <summary>
        /// Modbus Object 형식
        /// </summary>
        public ModbusObjectType ObjectType { get => objectType; protected set => SetProperty(ref objectType, value); }
        /// <summary>
        /// 데이터 주소
        /// </summary>
        public ushort Address { get => address; set => SetProperty(ref address, value); }
        /// <summary>
        /// 요청 시작 주소
        /// </summary>
        public ushort? RequestAddress { get => requestAddress; set => SetProperty(ref requestAddress, value); }
        /// <summary>
        /// 요청 길이
        /// </summary>
        public ushort? RequestLength { get => requestLength; set => SetProperty(ref requestLength, value); }
        /// <summary>
        /// 다중 쓰기 Function 사용 여부
        /// </summary>
        public bool? UseMultiWriteFunction { get => useMultiWriteFunction; set => SetProperty(ref useMultiWriteFunction, value); }

        /// <summary>
        /// 속성 값이 변경될 때 발생합니다.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// 속성 값이 변경되기 직전에 발생합니다.
        /// </summary>
        public event PropertyChangingEventHandler PropertyChanging;

        /// <summary>
        /// 실제 요청 시작 주소
        /// </summary>
        public ushort ActualRequestAddress => RequestAddress ?? Address;
        /// <summary>
        /// 실제 요청 길이
        /// </summary>
        public abstract ushort ActualRequestLength { get; }
        /// <summary>
        /// 쓰기 가능 여부
        /// </summary>
        public abstract bool Writable { get; protected set; }
        /// <summary>
        /// 요청 주소를 기준으로 한 데이터 주소의 인덱스
        /// </summary>
        protected ushort AddressIndex => (ushort)(RequestAddress == null ? 0 : RequestAddress.Value - Address);

        internal new void RaiseErrorOccurred(Exception exception, ErrorDirection errorDirection)
            => base.RaiseErrorOccurred(exception, errorDirection);

        internal abstract void Initialize();
    }

    /// <summary>
    /// Modbus 인터페이스 포인트
    /// </summary>
    /// <typeparam name="TValue">값 형식</typeparam>
    public abstract class ModbusPoint<TValue> : ModbusPoint, IInterfaceHandlerContainer<TValue>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        /// <param name="useMultiWriteFunction">다중 쓰기 Function 사용 여부</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        protected ModbusPoint(byte slaveAddress, ushort address, ushort? requestAddress, ushort? requestLength, bool? useMultiWriteFunction, IEnumerable<InterfaceHandler> handlers)
            : base(slaveAddress, address, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
        }

        /// <summary>
        /// Modbus 마스터를 이용하여 값을 전송하고자 할 때 호출되는 메서드
        /// </summary>
        /// <param name="master">Modbus 마스터</param>
        /// <param name="value">전송할 값</param>
        /// <returns>전송 성공 여부</returns>
        protected abstract bool OnSendRequested(ModbusMaster master, in TValue value);
        /// <summary>
        /// Modbus 슬레이브를 이용하여 값을 전송하고자 할 때 호출되는 메서드
        /// </summary>
        /// <param name="slave">Modbus 슬레이브</param>
        /// <param name="value">전송할 값</param>
        /// <returns>전송 성공 여부</returns>
        protected abstract bool OnSendRequested(ModbusSlave slave, in TValue value);

        /// <summary>
        /// 수신한 값을 로컬 환경에 설정
        /// </summary>
        /// <param name="value">받은 값</param>
        internal void SetReceivedValue(in TValue value) => SetReceivedValue(value, null);

        private ModbusMasterInterface masterInterface;
        private ModbusSlaveInterface slaveInterface;

        private bool SendToMaster(in TValue value)
        {
            var master = masterInterface?.Master;
            return master != null && OnSendRequested(master, value);
        }
        private bool SendToSlave(in TValue value)
            => slaveInterface?.OnSendRequested(this, value, OnSendRequested) ?? false;

        internal override void Initialize()
        {
            if (Interface is ModbusMasterInterface master)
            {
                slaveInterface = null;
                masterInterface = master;
                send = SendToMaster;
            }
            else if (Interface is ModbusSlaveInterface slave)
            {
                masterInterface = null;
                slaveInterface = slave;
                send = SendToSlave;
            }
            else
            {
                masterInterface = null;
                slaveInterface = null;
                send = null;
            }
        }

        private SendDelegate send;
        private delegate bool SendDelegate(in TValue value);

        private bool Send<T>(in T value, in DateTime? timeStamp)
            => ((this is ModbusPoint<T> point)
            ? point.send?.Invoke(value)
            : send?.Invoke(value.To<T, TValue>())) ?? false;

        /// <summary>
        /// 비동기로 값을 전송하고자 할 때 호출되는 메서드
        /// </summary>
        /// <typeparam name="T">전송할 값 형식</typeparam>
        /// <param name="value">전송할 값</param>
        /// <param name="timeStamp">전송할 값의 적용 일시</param>
        /// <param name="cancellationToken">태스크 취소 토큰</param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        protected override Task<bool> OnSendAsyncRequested<T>(in T value, in DateTime? timeStamp, in CancellationToken? cancellationToken)
        {
            var localValue = value;
            var localtimeStamp = timeStamp;
            return cancellationToken != null
                ? Task.Run(() => Send(localValue, localtimeStamp), cancellationToken.Value)
                : Task.Run(() => Send(localValue, localtimeStamp));
        }
        /// <summary>
        /// 값을 동기적으로 전송하고자 할 때 호출되는 메서드
        /// </summary>
        /// <typeparam name="T">전송할 값 형식</typeparam>
        /// <param name="value">전송할 값</param>
        /// <param name="timeStamp">전송할 값의 적용 일시</param>
        /// <returns>전송 성공 여부</returns>
        protected override bool OnSendRequested<T>(in T value, in DateTime? timeStamp) => Send(value, timeStamp);
    }
}
