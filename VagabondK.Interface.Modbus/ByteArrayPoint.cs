using System.Collections.Generic;
using System.Linq;
using System.Text;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus
{
    public class ByteArrayPoint : VariableLengthPoint<byte[]>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="bytesLength">바이트 길이</param>
        /// <param name="skipFirstByte">첫 번째 바이트를 생략하고 두 번째 바이트부터 사용할 지 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청을 위한 데이터 주소</param>
        /// <param name="requestLength">요청을 위한 데이터 개수</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public ByteArrayPoint(byte slaveAddress = 0, bool writable = true, ushort address = 0, int bytesLength = 2, bool skipFirstByte = false, ModbusEndian endian = ModbusEndian.AllBig, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, writable, address, bytesLength, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
        }

        protected override byte[] GetBytes(in byte[] value)
        {
            var diff = BytesCount - value.Length;
            return diff < 0 ? ToBytesInRegisters(value.Take(BytesCount).ToArray(), false)
                : diff > 0 ? ToBytesInRegisters(value.Concat(Enumerable.Repeat((byte)0, diff)).ToArray(), false)
                : ToBytesInRegisters(value.ToArray(), false);
        }
        protected override byte[] GetValue() => GetBytesFromRegisters(false);
    }
}
