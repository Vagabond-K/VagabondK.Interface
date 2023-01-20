using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;

namespace VagabondK.Interface.Modbus
{
    public class CoilAttribute : BitAttribute
    {
        public CoilAttribute(ushort address) : base(address) { }
        public CoilAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        public CoilAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        public CoilAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }
        public CoilAttribute(ushort address, bool useMultiWriteFunction) : base(address) { UseMultiWriteFunction = useMultiWriteFunction; }
        public CoilAttribute(byte slaveAddress, ushort address, bool useMultiWriteFunction) : base(slaveAddress, address) { UseMultiWriteFunction = useMultiWriteFunction; }
        public CoilAttribute(ushort address, ushort requestAddress, ushort requestLength, bool useMultiWriteFunction) : base(address, requestAddress, requestLength) { UseMultiWriteFunction = useMultiWriteFunction; }
        public CoilAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength, bool useMultiWriteFunction) : base(slaveAddress, address, requestAddress, requestLength) { UseMultiWriteFunction = useMultiWriteFunction; }

        public bool? UseMultiWriteFunction { get; }

        protected override InterfacePoint OnCreatePoint(MemberInfo memberInfo, InterfaceAttribute rootAttribute)
            => new BitPoint(GetSlaveAddress(rootAttribute), true, Address, RequestAddress, RequestLength, UseMultiWriteFunction);
    }
}
