using System.Collections.Generic;
using System.Linq;
using System.Text;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus
{
    public class ModbusStringPoint : ModbusVariableLengthPoint<string>
    {
        private Encoding encoding = Encoding.UTF8;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="bytesLength">바이트 길이</param>
        /// <param name="encoding">인코딩, null이면 기본값으로 utf-8을 사용함</param>
        /// <param name="skipFirstByte">첫 번째 바이트를 생략하고 두 번째 바이트부터 사용할 지 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청을 위한 데이터 주소</param>
        /// <param name="requestLength">요청을 위한 데이터 개수</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public ModbusStringPoint(byte slaveAddress = 0, bool writable = true, ushort address = 0, int bytesLength = 2, Encoding encoding = null, bool skipFirstByte = false, ModbusEndian endian = ModbusEndian.AllBig, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, writable, address, bytesLength, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            this.encoding = encoding ?? Encoding.UTF8;
        }

        public Encoding Encoding { get => encoding; set => SetProperty(ref encoding, value); }

        protected override byte[] GetBytes(string value)
        {
            var bytes = encoding.GetBytes(value);
            var diff = BytesCount - bytes.Length;
            return diff < 0 ? ToBytesInRegisters(bytes.Take(BytesCount).ToArray(), false)
                : diff > 0 ? ToBytesInRegisters(bytes.Concat(Enumerable.Repeat((byte)0, diff)).ToArray(), false)
                : ToBytesInRegisters(bytes, false);
        }
        protected override string GetValue() => encoding.GetString(GetBytesFromRegisters(false)).TrimEnd('\0');
    }
}
