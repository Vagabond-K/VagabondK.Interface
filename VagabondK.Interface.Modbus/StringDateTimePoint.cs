using System;
using System.Collections.Generic;
using System.Text;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus
{
    /// <summary>
    /// Modbus Word(Holding Register, Input Register) 기반의 DateTime 형식 직렬화 인터페이스 포인트. 인코딩된 문자열을 이용함.
    /// </summary>
    public class StringDateTimePoint : MultiBytesPoint<DateTime>
    {
        private string format;
        private DateTimeKind dateTimeKind;
        private Encoding encoding = Encoding.UTF8;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="format">서식 문자열</param>
        /// <param name="dateTimeKind">현지 시간 또는 UTC(지역 표준시)를 나타내는지 아니면 현지 시간 또는 UTC로 지정되지 않는지 여부</param>
        /// <param name="encoding">인코딩, null이면 기본값으로 utf-8을 사용함</param>
        /// <param name="skipFirstByte">첫 번째 Byte 생략 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public StringDateTimePoint(byte slaveAddress = 0, bool writable = true, ushort address = 0, string format = null, DateTimeKind dateTimeKind = DateTimeKind.Utc, Encoding encoding = null, bool skipFirstByte = false, ModbusEndian endian = ModbusEndian.AllBig, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, writable, address, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            this.format = format;
            this.dateTimeKind = dateTimeKind;
            this.encoding = encoding ?? Encoding.UTF8;
        }

        /// <summary>
        /// 값의 Byte 단위 개수, Format에 입력한 문자 수와 동일함.
        /// </summary>
        protected override int BytesCount => format?.Length ?? 0;

        /// <summary>
        /// byte 배열와 DateTime 사이의 변환을 위한 형식 문자열. DataTime.ParseExact와 DataTime.ToString 메서드에서 사용하는 format과 동일함.
        /// </summary>
        public string Format { get => format; set => SetProperty(ref format, value); }

        /// <summary>
        /// Modbus 데이터 상 DateTime 값의 DateTimeKind를 정의
        /// </summary>
        public DateTimeKind DateTimeKind { get => dateTimeKind; set => SetProperty(ref dateTimeKind, value); }

        /// <summary>
        /// string 형식 값의 직렬화/역직렬화를 위한 인코딩
        /// </summary>
        public Encoding Encoding { get => encoding; set => SetProperty(ref encoding, value); }

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

            return ToBytesInRegisters(encoding.GetBytes(dateTime.ToString(format)), false);
        }

        /// <summary>
        /// 로컬 레지스터로부터 값 가져오기
        /// </summary>
        /// <returns>인터페이스 결과 값</returns>
        protected override DateTime GetValue()
        {
            var ticks = DateTime.ParseExact(encoding.GetString(GetBytesFromRegisters(false)).Trim('\0'), format, null).Ticks;

            switch (dateTimeKind)
            {
                case DateTimeKind.Local:
                    return new DateTime(ticks, dateTimeKind);
                case DateTimeKind.Utc:
                default:
                    return new DateTime(ticks, dateTimeKind).ToLocalTime();
            }
        }
    }
}
