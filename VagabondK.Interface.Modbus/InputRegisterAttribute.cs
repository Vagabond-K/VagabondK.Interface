using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;

namespace VagabondK.Interface.Modbus
{
    public class InputRegisterAttribute : WordAttribute
    {
        public InputRegisterAttribute(ushort address) : base(address) { }
        public InputRegisterAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        public InputRegisterAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        public InputRegisterAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }

        protected override InterfacePoint OnCreatePoint(MemberInfo memberInfo, InterfaceAttribute rootAttribute) => OnCreatePoint(memberInfo, rootAttribute, false);
    }
}
