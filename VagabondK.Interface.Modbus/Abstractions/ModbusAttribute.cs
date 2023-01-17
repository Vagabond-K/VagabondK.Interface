using System;
using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus.Abstractions
{
    public abstract class ModbusAttribute : InterfaceAttribute
    {
        protected ModbusAttribute(ushort address)
        {
            Address = address;
        }
        protected ModbusAttribute(byte slaveAddress, ushort address) : this(address)
        {
            SlaveAddress = slaveAddress;
        }
        protected ModbusAttribute(ushort address, ushort requestAddress, ushort requestLength) : this(address)
        {
            RequestAddress = requestAddress;
            RequestLength = requestLength;
        }
        protected ModbusAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : this(slaveAddress, address, requestAddress)
        {
            RequestLength = requestLength;
        }

        public byte? SlaveAddress { get; }
        public ushort Address { get; }
        public ushort? RequestAddress { get; }
        public ushort? RequestLength { get; }

        internal byte GetSlaveAddress(Type targetType)
            => SlaveAddress ?? targetType.GetCustomAttribute<ModbusSlaveAddressAttribute>()?.SlaveAddress ?? 0;
    }
}
