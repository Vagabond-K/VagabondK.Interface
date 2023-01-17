using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;

namespace VagabondK.Interface.Modbus
{
    public class ModbusDIAttribute : ModbusBooleanAttribute
    {
        public ModbusDIAttribute(ushort address) : base(address) { }
        public ModbusDIAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        public ModbusDIAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        public ModbusDIAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }

        protected override InterfacePoint OnCreatePoint(MemberInfo memberInfo)
            => new ModbusBooleanPoint(GetSlaveAddress(memberInfo.ReflectedType), false, Address, RequestAddress, RequestLength, null);
    }
}
