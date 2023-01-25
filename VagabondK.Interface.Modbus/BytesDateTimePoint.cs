using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus
{
    /// <summary>
    /// Modbus Word(Holding Register, Input Register) 기반의 DateTime 형식 직렬화 인터페이스 포인트. 레지스터의 수치형 값을 이용하여 DateTime 값을 추출하거나 직렬화 함.
    /// </summary>
    public class BytesDateTimePoint : MultiBytesPoint<DateTime>
    {
        private readonly object formatLock = new object();
        private string format;
        private DateTimeKind dateTimeKind;
        private int yearByteCount = 0;
        private int monthByteCount = 0;
        private int dayByteCount = 0;
        private int hourByteCount = 0;
        private int minuteByteCount = 0;
        private int secondByteCount = 0;
        private int millisecondByteCount = 0;
        private byte[] yearBytes;
        private byte[] monthBytes;
        private byte[] dayBytes;
        private byte[] hourBytes;
        private byte[] minuteBytes;
        private byte[] secondBytes;
        private byte[] millisecondBytes;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="format">서식 문자열</param>
        /// <param name="dateTimeKind">현지 시간 또는 UTC(지역 표준시)를 나타내는지 아니면 현지 시간 또는 UTC로 지정되지 않는지 여부</param>
        /// <param name="skipFirstByte">첫 번째 Byte 생략 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public BytesDateTimePoint(byte slaveAddress = 0, bool writable = true, ushort address = 0, string format = null, DateTimeKind dateTimeKind = DateTimeKind.Utc, bool skipFirstByte = false, ModbusEndian endian = ModbusEndian.AllBig, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, writable, address, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            this.format = format;
            this.dateTimeKind = dateTimeKind;
            UpdateByteCounts();
        }

        /// <summary>
        /// 값의 Byte 단위 개수, Format에 입력한 문자 수와 동일함.
        /// </summary>
        protected override int BytesCount => format?.Length ?? 0;

        /// <summary>
        /// byte 배열와 DateTime 사이의 변환을 위한 형식 문자열. y: 년, M: 월, d: 일, H: 시, m: 분, s: 초, f: 밀리초. yyMdHmsff일 경우 총 9 Byte를 사용하며 년과 밀리초만 2 Byte로 직렬화 하고 나머지는 1 Byte를 사용함.
        /// </summary>
        public string Format
        {
            get => format;
            set
            {
                if (SetProperty(ref format, value))
                    UpdateByteCounts();
            }
        }

        /// <summary>
        /// Modbus 데이터 상 DateTime 값의 DateTimeKind를 정의
        /// </summary>
        public DateTimeKind DateTimeKind { get => dateTimeKind; set => SetProperty(ref dateTimeKind, value); }

        private void UpdateByteCounts()
        {
            lock (formatLock)
            {
                yearByteCount = 0;
                monthByteCount = 0;
                dayByteCount = 0;
                hourByteCount = 0;
                minuteByteCount = 0;
                secondByteCount = 0;
                millisecondByteCount = 0;

                foreach (char c in format)
                {
                    switch (c)
                    {
                        case 'y': yearByteCount++; break;
                        case 'M': monthByteCount++; break;
                        case 'd': dayByteCount++; break;
                        case 'H': hourByteCount++; break;
                        case 'm': minuteByteCount++; break;
                        case 's': secondByteCount++; break;
                        case 'f': millisecondByteCount++; break;
                    }
                }

                yearBytes = new byte[Math.Max(4, yearByteCount)];
                monthBytes = new byte[Math.Max(4, monthByteCount)];
                dayBytes = new byte[Math.Max(4, dayByteCount)];
                hourBytes = new byte[Math.Max(4, hourByteCount)];
                minuteBytes = new byte[Math.Max(4, minuteByteCount)];
                secondBytes = new byte[Math.Max(4, secondByteCount)];
                millisecondBytes = new byte[Math.Max(4, millisecondByteCount)];
            }
        }

        private byte[] GetInt32Bytes(int count, int value)
        {
            if (count <= 0) return Array.Empty<byte>();

            var result = BitConverter.IsLittleEndian ? BitConverter.GetBytes(value).Reverse().ToArray() : BitConverter.GetBytes(value);
            if (result.Length != count)
                result = result.Length > count
                    ? result.Skip(result.Length - count).ToArray()
                    : Enumerable.Repeat((byte)0, count - result.Length).Concat(result).ToArray();
            return result;
        }

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

            byte[] buffer;

            lock (formatLock)
            {
                buffer = new byte[format.Length];

                var yearBytes = GetInt32Bytes(yearByteCount, dateTime.Year);
                var monthBytes = GetInt32Bytes(monthByteCount, dateTime.Month);
                var dayBytes = GetInt32Bytes(dayByteCount, dateTime.Day);
                var hourBytes = GetInt32Bytes(hourByteCount, dateTime.Hour);
                var minute = hourByteCount == 0 ? dateTime.Minute + dateTime.Hour * 60 : dateTime.Minute;
                var minuteBytes = GetInt32Bytes(minuteByteCount, minute);
                var second = minuteByteCount == 0 ? dateTime.Second + minute * 60 : dateTime.Second;
                var secondBytes = GetInt32Bytes(secondByteCount, second);
                var millisecond = secondByteCount == 0 ? dateTime.Millisecond + second * 1000 : dateTime.Millisecond;
                var millisecondBytes = GetInt32Bytes(millisecondByteCount, millisecond);

                int yearByteIndex = 0;
                int monthByteIndex = 0;
                int dayByteIndex = 0;
                int hourByteIndex = 0;
                int minuteByteIndex = 0;
                int secondByteIndex = 0;
                int millisecondByteIndex = 0;

                for (int i = 0; i < format.Length; i++)
                {
                    var c = format[i];
                    switch (c)
                    {
                        case 'y': buffer[i] = yearBytes[yearByteIndex++]; break;
                        case 'M': buffer[i] = monthBytes[monthByteIndex++]; break;
                        case 'd': buffer[i] = dayBytes[dayByteIndex++]; break;
                        case 'H': buffer[i] = hourBytes[hourByteIndex++]; break;
                        case 'm': buffer[i] = minuteBytes[minuteByteIndex++]; break;
                        case 's': buffer[i] = secondBytes[secondByteIndex++]; break;
                        case 'f': buffer[i] = millisecondBytes[millisecondByteIndex++]; break;
                    }
                }
            }

            return ToBytesInRegisters(buffer, false);
        }

        /// <summary>
        /// 로컬 레지스터로부터 값 가져오기
        /// </summary>
        /// <returns>인터페이스 결과 값</returns>
        protected override DateTime GetValue()
        {
            lock (formatLock)
            {
                var buffer = GetBytesFromRegisters(false);
                int yearByteIndex = BitConverter.IsLittleEndian ? yearByteCount : Math.Max(0, 4 - yearByteCount);
                int monthByteIndex = BitConverter.IsLittleEndian ? monthByteCount : Math.Max(0, 4 - monthByteCount);
                int dayByteIndex = BitConverter.IsLittleEndian ? dayByteCount : Math.Max(0, 4 - dayByteCount);
                int hourByteIndex = BitConverter.IsLittleEndian ? hourByteCount : Math.Max(0, 4 - hourByteCount);
                int minuteByteIndex = BitConverter.IsLittleEndian ? minuteByteCount : Math.Max(0, 4 - minuteByteCount);
                int secondByteIndex = BitConverter.IsLittleEndian ? secondByteCount : Math.Max(0, 4 - secondByteCount);
                int millisecondByteIndex = BitConverter.IsLittleEndian ? millisecondByteCount : Math.Max(0, 4 - millisecondByteCount);

                for (int i = 0; i < format.Length; i++)
                {
                    var c = format[i];
                    switch (c)
                    {
                        case 'y': yearBytes[BitConverter.IsLittleEndian ? --yearByteIndex : yearByteIndex++] = buffer[i]; break;
                        case 'M': monthBytes[BitConverter.IsLittleEndian ? --monthByteIndex : monthByteIndex++] = buffer[i]; break;
                        case 'd': dayBytes[BitConverter.IsLittleEndian ? --dayByteIndex : dayByteIndex++] = buffer[i]; break;
                        case 'H': hourBytes[BitConverter.IsLittleEndian ? --hourByteIndex : hourByteIndex++] = buffer[i]; break;
                        case 'm': minuteBytes[BitConverter.IsLittleEndian ? --minuteByteIndex : minuteByteIndex++] = buffer[i]; break;
                        case 's': secondBytes[BitConverter.IsLittleEndian ? --secondByteIndex : secondByteIndex++] = buffer[i]; break;
                        case 'f': millisecondBytes[BitConverter.IsLittleEndian ? --millisecondByteIndex : millisecondByteIndex++] = buffer[i]; break;
                    }
                }

                var millisecond = millisecondByteCount > 0 ? BitConverter.ToInt32(millisecondBytes, 0) : 0;
                var second = secondByteCount > 0 ? BitConverter.ToInt32(secondBytes, 0) : millisecond / 1000;
                var minute = minuteByteCount > 0 ? BitConverter.ToInt32(minuteBytes, 0) : second / 60;
                var hour = hourByteCount > 0 ? BitConverter.ToInt32(hourBytes, 0) : minute / 60;
                var day = hourByteCount > 0 ? BitConverter.ToInt32(dayBytes, 0) : 1;
                var month = monthByteCount > 0 ? BitConverter.ToInt32(monthBytes, 0) : 1;
                var year = hourByteCount > 0 ? BitConverter.ToInt32(yearBytes, 0) : 1;

                millisecond %= 1000;
                second %= 60;
                minute %= 60;

                var ticks = new DateTime(year, month, day, hour, minute, second, millisecond).Ticks;

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
}
