using System;
using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus.Abstractions
{
    public abstract class ModbusBindingAttribute : InterfaceBindingAttribute
    {
        protected ModbusBindingAttribute(ushort address)
        {
            Address = address;
        }
        protected ModbusBindingAttribute(byte slaveAddress, ushort address) : this(address)
        {
            SlaveAddress = slaveAddress;
        }
        protected ModbusBindingAttribute(ushort address, ushort requestAddress, ushort requestLength) : this(address)
        {
            RequestAddress = requestAddress;
            RequestLength = requestLength;
        }
        protected ModbusBindingAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : this(slaveAddress, address, requestAddress)
        {
            RequestLength = requestLength;
        }

        public byte? SlaveAddress { get; }
        public ushort Address { get; }
        public ushort? RequestAddress { get; }
        public ushort? RequestLength { get; }

        internal byte GetSlaveAddress(InterfaceAttribute rootAttribute)
            => SlaveAddress ?? (rootAttribute as ModbusAttribute)?.SlaveAddress ?? 0;
    }
}
