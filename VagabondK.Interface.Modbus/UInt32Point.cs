using System;
using System.Collections.Generic;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus
{
    public class UInt32Point<TValue> : NumericPoint<uint, TValue>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="scale">스케일 값</param>
        /// <param name="skipFirstByte">첫 번째 바이트를 생략하고 두 번째 바이트부터 사용할 지 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청을 위한 데이터 주소</param>
        /// <param name="requestLength">요청을 위한 데이터 개수</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public UInt32Point(byte slaveAddress = 0, bool writable = true, ushort address = 0, double scale = 1, bool skipFirstByte = false, ModbusEndian endian = ModbusEndian.AllBig, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, writable, address, scale, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers) { }

        protected override uint Deserialize() => BitConverter.ToUInt32(GetBytesFromRegisters(true), 0);
        protected override byte[] Serialize(uint serialize) => BitConverter.GetBytes(serialize);
    }

    public class ModbusUInt32Point : UInt32Point<uint>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="skipFirstByte">첫 번째 바이트를 생략하고 두 번째 바이트부터 사용할 지 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청을 위한 데이터 주소</param>
        /// <param name="requestLength">요청을 위한 데이터 개수</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public ModbusUInt32Point(byte slaveAddress = 0, bool writable = true, ushort address = 0, bool skipFirstByte = false, ModbusEndian endian = ModbusEndian.AllBig, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, writable, address, 1, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers) { }
    }
}
