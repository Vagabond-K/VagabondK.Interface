using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus.Abstractions
{
    public abstract class ModbusPoint : InterfacePoint, INotifyPropertyChanged, INotifyPropertyChanging
    {
        private byte slaveAddress;
        private ModbusObjectType objectType;
        private ushort address;
        private ushort? requestAddress;
        private ushort? requestLength;
        private bool? useMultiWriteFunction;

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

        internal protected bool SetProperty<TProperty>(ref TProperty target, in TProperty value, [CallerMemberName] string propertyName = null)
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

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        protected void RaisePropertyChanging([CallerMemberName] string propertyName = null) => PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));

        public byte SlaveAddress { get => slaveAddress; set => SetProperty(ref slaveAddress, value); }
        public ModbusObjectType ObjectType { get => objectType; protected set => SetProperty(ref objectType, value); }
        public ushort Address { get => address; set => SetProperty(ref address, value); }
        public ushort? RequestAddress { get => requestAddress; set => SetProperty(ref requestAddress, value); }
        public ushort? RequestLength { get => requestLength; set => SetProperty(ref requestLength, value); }
        public bool? UseMultiWriteFunction { get => useMultiWriteFunction; set => SetProperty(ref useMultiWriteFunction, value); }

        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

        public ushort ActualRequestAddress => RequestAddress ?? Address;
        public abstract ushort ActualRequestLength { get; }
        public abstract bool Writable { get; protected set; }
        protected ushort AddressIndex => (ushort)(RequestAddress == null ? 0 : RequestAddress.Value - Address);

        internal new void RaiseErrorOccurred(Exception exception, ErrorDirection errorDirection)
            => base.RaiseErrorOccurred(exception, errorDirection);

        internal abstract void Initialize();
    }

    public abstract class ModbusPoint<TValue> : ModbusPoint
    {
        private InterfaceHandler<TValue> defaultHandler;

        protected ModbusPoint(byte slaveAddress, ushort address, ushort? requestAddress, ushort? requestLength, bool? useMultiWriteFunction, IEnumerable<InterfaceHandler> handlers)
            : base(slaveAddress, address, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
        }

        protected abstract bool OnSendRequested(ModbusMaster master, in TValue value);
        protected abstract bool OnSendRequested(ModbusSlave slave, in TValue value);

        internal new void SetReceivedValue(in TValue value, in DateTime? timeStamp)
            => base.SetReceivedValue(value, timeStamp);

        private ModbusMasterInterface masterInterface;
        private ModbusSlaveInterface slaveInterface;

        private bool SendToMaster(in TValue value, in DateTime? timeStamp)
        {
            var master = masterInterface?.Master;
            return master != null && OnSendRequested(master, value);
        }
        private bool SendToSlave(in TValue value, in DateTime? timeStamp)
            => slaveInterface?.OnSendRequested(this, value, timeStamp, OnSendRequested) ?? false;

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
        private delegate bool SendDelegate(in TValue value, in DateTime? timeStamp);

        private bool Send<T>(in T value, in DateTime? timeStamp)
        {
            if (this is ModbusPoint<T> point)
                return point.send?.Invoke(value, timeStamp) ?? false;
            else
                return send?.Invoke(value.To<T, TValue>(), timeStamp) ?? false;
        }

        protected override Task<bool> OnSendAsyncRequested<T>(in T value, in DateTime? timeStamp, in CancellationToken? cancellationToken)
        {
            var localValue = value;
            var localtimeStamp = timeStamp;
            return cancellationToken != null
                ? Task.Run(() => Send(localValue, localtimeStamp), cancellationToken.Value)
                : Task.Run(() => Send(localValue, localtimeStamp));
        }
        protected override bool OnSendRequested<T>(in T value, in DateTime? timeStamp) => Send(value, timeStamp);

        public InterfaceHandler<TValue> DefaultHandler
        {
            get
            {
                if (defaultHandler == null)
                {
                    defaultHandler = new InterfaceHandler<TValue>();
                    Add(defaultHandler);
                }
                return defaultHandler;
            }
        }
    }
}
