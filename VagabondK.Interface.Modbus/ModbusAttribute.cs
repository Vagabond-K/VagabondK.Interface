using System;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;

namespace VagabondK.Interface.Modbus
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class ModbusAttribute : InterfaceAttribute
    {
        public ModbusAttribute() { }
        public ModbusAttribute(byte slaveAddress)
        {
            SlaveAddress = slaveAddress;
        }

        public byte? SlaveAddress { get; }

        public override Type InterfacePointType => typeof(ModbusPoint);
    }
}
