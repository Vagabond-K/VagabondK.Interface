using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;

namespace VagabondK.Interface.Modbus
{
    public class DiscreteInputAttribute : BitAttribute
    {
        public DiscreteInputAttribute(ushort address) : base(address) { }
        public DiscreteInputAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        public DiscreteInputAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        public DiscreteInputAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }

        protected override InterfacePoint OnCreatePoint(MemberInfo memberInfo, InterfaceAttribute rootAttribute)
            => new BitPoint(GetSlaveAddress(rootAttribute), true, Address, RequestAddress, RequestLength, null);
    }
}
