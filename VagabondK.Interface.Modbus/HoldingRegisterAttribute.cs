using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;

namespace VagabondK.Interface.Modbus
{
    public class HoldingRegisterAttribute : WordAttribute
    {
        public HoldingRegisterAttribute(ushort address) : base(address) { }
        public HoldingRegisterAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        public HoldingRegisterAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        public HoldingRegisterAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }
        public HoldingRegisterAttribute(ushort address, bool useMultiWriteFunction) : base(address) { UseMultiWriteFunction = useMultiWriteFunction; }
        public HoldingRegisterAttribute(byte slaveAddress, ushort address, bool useMultiWriteFunction) : base(slaveAddress, address) { UseMultiWriteFunction = useMultiWriteFunction; }
        public HoldingRegisterAttribute(ushort address, ushort requestAddress, ushort requestLength, bool useMultiWriteFunction) : base(address, requestAddress, requestLength) { UseMultiWriteFunction = useMultiWriteFunction; }
        public HoldingRegisterAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength, bool useMultiWriteFunction) : base(slaveAddress, address, requestAddress, requestLength) { UseMultiWriteFunction = useMultiWriteFunction; }

        public bool? UseMultiWriteFunction { get; }

        protected override InterfacePoint OnCreatePoint(MemberInfo memberInfo, InterfaceAttribute rootAttribute) => OnCreatePoint(memberInfo, rootAttribute, true, UseMultiWriteFunction);
    }
}
