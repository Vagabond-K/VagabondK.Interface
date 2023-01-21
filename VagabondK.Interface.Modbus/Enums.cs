using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Interface.Modbus
{
    public enum DateTimeFormat
    {
        UnixTime = 0,
        DotNet = 1,
        Ticks = 2,
        String = 3,
        Bytes = 4,
    }
}
