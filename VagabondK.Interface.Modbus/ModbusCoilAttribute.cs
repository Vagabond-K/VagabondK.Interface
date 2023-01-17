using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;

namespace VagabondK.Interface.Modbus
{
    public class ModbusCoilAttribute : ModbusBooleanAttribute
    {
        public ModbusCoilAttribute(ushort address) : base(address) { }
        public ModbusCoilAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        public ModbusCoilAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        public ModbusCoilAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }
        public ModbusCoilAttribute(ushort address, bool useMultiWriteFunction) : base(address) { UseMultiWriteFunction = useMultiWriteFunction; }
        public ModbusCoilAttribute(byte slaveAddress, ushort address, bool useMultiWriteFunction) : base(slaveAddress, address) { UseMultiWriteFunction = useMultiWriteFunction; }
        public ModbusCoilAttribute(ushort address, ushort requestAddress, ushort requestLength, bool useMultiWriteFunction) : base(address, requestAddress, requestLength) { UseMultiWriteFunction = useMultiWriteFunction; }
        public ModbusCoilAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength, bool useMultiWriteFunction) : base(slaveAddress, address, requestAddress, requestLength) { UseMultiWriteFunction = useMultiWriteFunction; }

        public bool? UseMultiWriteFunction { get; }

        protected override InterfacePoint OnCreatePoint(MemberInfo memberInfo)
            => new ModbusBooleanPoint(GetSlaveAddress(memberInfo.ReflectedType), true, Address, RequestAddress, RequestLength, UseMultiWriteFunction);
    }
}
