namespace VagabondK.Interface.Modbus.Abstractions
{
    public abstract class BitAttribute : ModbusBindingAttribute
    {
        protected BitAttribute(ushort address) : base(address) { }
        protected BitAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        protected BitAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        protected BitAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }
    }
}
