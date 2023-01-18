namespace VagabondK.Interface.Modbus.Abstractions
{
    public abstract class ModbusBooleanAttribute : ModbusBindingAttribute
    {
        protected ModbusBooleanAttribute(ushort address) : base(address) { }
        protected ModbusBooleanAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        protected ModbusBooleanAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        protected ModbusBooleanAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }
    }
}
