using System;
using VagabondK.Protocols.Modbus.Data;

namespace VagabondK.Interface.Modbus.Abstractions
{
    interface IModbusRegisterPoint
    {
        void SetRegisters(ModbusRegisters registers);
        void SetReceivedValue(ModbusRegisters registers, ref DateTime? timeStamp);
        int RegistersCount { get; }
    }
}
