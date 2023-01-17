using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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

        internal abstract Task<bool> Send<T>(ModbusMaster master, T value);
        internal abstract bool Send<T>(ModbusSlave slave, T value);
    }

    public abstract class ModbusPoint<TValue> : ModbusPoint
    {
        private InterfaceHandler<TValue> defaultHandler;

        protected ModbusPoint(byte slaveAddress, ushort address, ushort? requestAddress, ushort? requestLength, bool? useMultiWriteFunction, IEnumerable<InterfaceHandler> handlers)
            : base(slaveAddress, address, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
        }

        protected abstract Task<bool> OnSendRequested(ModbusMaster master, TValue value);
        protected abstract bool OnSendRequested(ModbusSlave slave, TValue value);
        internal Task<bool> Send(ModbusMaster master, TValue value) => OnSendRequested(master, value);
        internal override Task<bool> Send<T>(ModbusMaster master, T value) => OnSendRequested(master, value.To<T, TValue>());
        internal bool Send(ModbusSlave slave, TValue value) => OnSendRequested(slave, value);
        internal override bool Send<T>(ModbusSlave slave, T value) => OnSendRequested(slave, value.To<T, TValue>());

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
