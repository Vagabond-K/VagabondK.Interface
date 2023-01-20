﻿using System;
using VagabondK.Protocols.Modbus.Data;

namespace VagabondK.Interface.Modbus.Abstractions
{
    interface IModbusWordPoint
    {
        void SetWords(ModbusWords words);
        void SetReceivedValue(ModbusWords words, in DateTime? timeStamp);
        int WordsCount { get; }
    }
}
