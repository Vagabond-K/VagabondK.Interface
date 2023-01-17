using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;

namespace VagabondK.Interface.Modbus
{
    public class ModbusIRAttribute : ModbusRegisterAttribute
    {
        public ModbusIRAttribute(ushort address) : base(address) { }
        public ModbusIRAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        public ModbusIRAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        public ModbusIRAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }

        protected override InterfacePoint OnCreatePoint(MemberInfo memberInfo) => OnCreatePoint(memberInfo, false);
    }
}
