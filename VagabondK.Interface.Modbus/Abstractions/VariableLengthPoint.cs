using System;
using System.Collections.Generic;
using System.Text;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus.Abstractions
{
    public abstract class VariableLengthPoint<TValue> : MultiBytesPoint<TValue>
    {
        private int bytesLength = 2;

        protected VariableLengthPoint(byte slaveAddress, bool writable, ushort address, int bytesLength, bool skipFirstByte, ModbusEndian endian, ushort? requestAddress, ushort? requestLength, bool? useMultiWriteFunction, IEnumerable<InterfaceHandler> handlers)
            : base(slaveAddress, writable, address, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            this.bytesLength = bytesLength;
        }

        protected override int BytesCount => BytesLength;

        public int BytesLength { get => bytesLength; set => SetProperty(ref bytesLength, value); }
    }
}
