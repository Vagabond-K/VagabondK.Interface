using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
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

        protected bool SetProperty<TProperty>(ref TProperty target, TProperty value, [CallerMemberName] string propertyName = null)
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

        protected abstract bool OnSendRequested(ModbusMaster master, ref TValue value);
        protected abstract bool OnSendRequested(ModbusSlave slave, ref TValue value);

        internal new void SetReceivedValue(ref TValue value, ref DateTime? timeStamp)
            => base.SetReceivedValue(ref value, ref timeStamp);

        private ModbusMasterInterface masterInterface;
        private ModbusSlaveInterface slaveInterface;

        private bool SendToMaster(ref TValue value, ref DateTime? timeStamp)
        {
            var master = masterInterface?.Master;
            return master != null && OnSendRequested(master, ref value);
        }
        private bool SendToSlave(ref TValue value, ref DateTime? timeStamp)
            => slaveInterface?.OnSendRequested(this, ref value, ref timeStamp, OnSendRequested) ?? false;

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
        private delegate bool SendDelegate(ref TValue value, ref DateTime? timeStamp);

        private bool Send<T>(ref T value, ref DateTime? timeStamp)
        {
            if (this is ModbusPoint<T> point)
                return point.send?.Invoke(ref value, ref timeStamp) ?? false;
            else
            {
                var converted = value.To<T, TValue>();
                return send?.Invoke(ref converted, ref timeStamp) ?? false;
            }
        }

        protected override Task<bool> OnSendAsyncRequested<T>(T value, DateTime? timeStamp) => Task.Run(() => Send(ref value, ref timeStamp));
        protected override bool OnSendRequested<T>(T value, DateTime? timeStamp) => Send(ref value, ref timeStamp);

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
