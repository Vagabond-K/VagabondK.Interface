using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus
{
    public class ModbusUnixTimePoint : ModbusVariableLengthPoint<DateTimeOffset>
    {
        private bool isMilliseconds;
        private int scalePowerOf10;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="isMilliseconds">밀리세컨드 단위 여부</param>
        /// <param name="scalePowerOf10">10진수 기반 자릿수 스케일, 기본값은 0</param>
        /// <param name="bytesLength">바이트 길이</param>
        /// <param name="skipFirstByte">첫 번째 바이트를 생략하고 두 번째 바이트부터 사용할 지 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청을 위한 데이터 주소</param>
        /// <param name="requestLength">요청을 위한 데이터 개수</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public ModbusUnixTimePoint(byte slaveAddress = 0, bool writable = true, ushort address = 0, bool isMilliseconds = false, int scalePowerOf10 = 0, int bytesLength = 4, bool skipFirstByte = false, ModbusEndian endian = ModbusEndian.AllBig, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, writable, address, bytesLength, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            this.isMilliseconds = isMilliseconds;
            this.scalePowerOf10 = scalePowerOf10;
            BytesLength = bytesLength;
        }

        public bool IsMilliseconds { get => isMilliseconds; set => SetProperty(ref isMilliseconds, value); }
        public int ScalePowerOf10 { get => scalePowerOf10; set => SetProperty(ref scalePowerOf10, value); }

        protected override byte[] GetBytes(DateTimeOffset value)
        {
            var unixTime = IsMilliseconds
                ? value.ToUnixTimeMilliseconds()
                : value.ToUnixTimeSeconds();

            if (scalePowerOf10 > 0)
                unixTime /= (long)Math.Pow(10, Math.Abs(scalePowerOf10));
            else if (scalePowerOf10 < 0)
                unixTime *= (long)Math.Pow(10, Math.Abs(scalePowerOf10));

            var bytes = BitConverter.GetBytes(unixTime);
            var diff = BytesCount - bytes.Length;
            if (BitConverter.IsLittleEndian)
                return diff < 0 ? ToBytesInRegisters(bytes.Take(BytesCount).ToArray(), true)
                    : diff > 0 ? ToBytesInRegisters(bytes.Concat(Enumerable.Repeat((byte)0, diff)).ToArray(), true)
                    : ToBytesInRegisters(bytes, true);
            else
                return diff < 0 ? ToBytesInRegisters(bytes.Skip(-diff).ToArray(), true)
                    : diff > 0 ? ToBytesInRegisters(Enumerable.Repeat((byte)0, diff).Concat(bytes).ToArray(), true)
                    : ToBytesInRegisters(bytes, true);
        }
        protected override DateTimeOffset GetValue()
        {
            var bytes = GetBytesFromRegisters(true);

            if (BytesLength < 8)
                bytes = BitConverter.IsLittleEndian
                    ? bytes.Concat(Enumerable.Repeat((byte)0, 8 - BytesLength)).ToArray()
                    : Enumerable.Repeat((byte)0, 8 - BytesLength).Concat(bytes).ToArray();
            else if (BytesLength > 8)
                bytes = BitConverter.IsLittleEndian
                    ? bytes.Take(BytesLength).ToArray()
                    : bytes.Skip(BytesLength - 8).ToArray();

            var unixTime = BitConverter.ToInt64(bytes, 0);

            if (scalePowerOf10 > 0)
                unixTime *= (long)Math.Pow(10, Math.Abs(scalePowerOf10));
            else if (scalePowerOf10 < 0)
                unixTime /= (long)Math.Pow(10, Math.Abs(scalePowerOf10));

            return isMilliseconds
                ? DateTimeOffset.FromUnixTimeMilliseconds(unixTime)
                : DateTimeOffset.FromUnixTimeSeconds(unixTime);
        }
    }
}
