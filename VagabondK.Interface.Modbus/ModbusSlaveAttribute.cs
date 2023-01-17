using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Interface.Modbus
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class ModbusSlaveAddressAttribute : Attribute
    {
        public ModbusSlaveAddressAttribute(byte slaveAddress)
        {
            SlaveAddress = slaveAddress;
        }

        public byte SlaveAddress { get; }
    }
}
