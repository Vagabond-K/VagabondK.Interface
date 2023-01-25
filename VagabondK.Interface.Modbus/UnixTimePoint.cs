using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus
{
    /// <summary>
    /// Modbus Word(Holding Register, Input Register) 기반의 DateTime 형식 직렬화 인터페이스 포인트. DateTimeOffset.FromUnixTimeMilliseconds 메서드와 레지스터의 수치형 데이터를 이용함
    /// </summary>
    public class UnixTimePoint : VariableLengthPoint<DateTime>
    {
        private int scalePowerOf10;
        private DateTimeKind dateTimeKind;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="scalePowerOf10">10진수 기반 자릿수 스케일, 기본값은 0</param>
        /// <param name="bytesLength">바이트 길이</param>
        /// <param name="dateTimeKind">현지 시간 또는 UTC(지역 표준시)를 나타내는지 아니면 현지 시간 또는 UTC로 지정되지 않는지 여부</param>
        /// <param name="skipFirstByte">첫 번째 Byte 생략 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public UnixTimePoint(byte slaveAddress = 0, bool writable = true, ushort address = 0, int scalePowerOf10 = 0, int bytesLength = 4, DateTimeKind dateTimeKind = DateTimeKind.Utc, bool skipFirstByte = false, ModbusEndian endian = ModbusEndian.AllBig, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, writable, address, bytesLength, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            this.scalePowerOf10 = scalePowerOf10;
            this.dateTimeKind = dateTimeKind;
            BytesLength = bytesLength;
        }

        /// <summary>
        /// Unix time의 10진수 단위 배율 
        /// </summary>
        public int ScalePowerOf10 { get => scalePowerOf10; set => SetProperty(ref scalePowerOf10, value); }

        /// <summary>
        /// Modbus 데이터 상 DateTime 값의 DateTimeKind를 정의
        /// </summary>
        public DateTimeKind DateTimeKind { get => dateTimeKind; set => SetProperty(ref dateTimeKind, value); }

        /// <summary>
        /// 값을 byte 배열로 직렬화
        /// </summary>
        /// <param name="value">직렬화 할 값</param>
        /// <returns>직렬화 된 byte 배열</returns>
        protected override byte[] GetBytes(in DateTime value)
        {
            var dateTime = value;
            if (value.Kind != DateTimeKind.Unspecified && value.Kind != dateTimeKind)
                dateTime = dateTimeKind == DateTimeKind.Local ? value.ToLocalTime() : value.ToUniversalTime();

            var unixTime = new DateTimeOffset(dateTime.Ticks, TimeSpan.Zero).ToUnixTimeMilliseconds();

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

        /// <summary>
        /// 로컬 레지스터로부터 값 가져오기
        /// </summary>
        /// <returns>인터페이스 결과 값</returns>
        protected override DateTime GetValue()
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

            var dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(unixTime);

            switch (dateTimeKind)
            {
                case DateTimeKind.Local:
                    return new DateTime(dateTimeOffset.UtcTicks, dateTimeKind);
                case DateTimeKind.Utc:
                default:
                    return dateTimeOffset.LocalDateTime;
            }
        }
    }
}
