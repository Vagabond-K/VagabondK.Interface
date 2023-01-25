using System;
using System.Collections.Generic;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus
{
    /// <summary>
    /// Modbus Word(Holding Register, Input Register) 기반의 DateTime 형식 직렬화 인터페이스 포인트. .NET DateTime 형식의 ToBinary, FromBinary 메서드를 이용함.
    /// </summary>
    public class DotNetDateTimePoint : MultiBytesPoint<DateTime>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="skipFirstByte">첫 번째 Byte 생략 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public DotNetDateTimePoint(byte slaveAddress = 0, bool writable = true, ushort address = 0, bool skipFirstByte = false, ModbusEndian endian = ModbusEndian.AllBig, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, writable, address, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
        }

        /// <summary>
        /// 값의 Byte 단위 개수, DateTime.ToBinary의 반환 형식이 long이므로 8 Byte임.
        /// </summary>
        protected override int BytesCount => 8;

        /// <summary>
        /// 값을 byte 배열로 직렬화
        /// </summary>
        /// <param name="value">직렬화 할 값</param>
        /// <returns>직렬화 된 byte 배열</returns>
        protected override byte[] GetBytes(in DateTime value) => ToBytesInRegisters(BitConverter.GetBytes(value.ToBinary()), true);

        /// <summary>
        /// 로컬 레지스터로부터 값 가져오기
        /// </summary>
        /// <returns>인터페이스 결과 값</returns>
        protected override DateTime GetValue() => DateTime.FromBinary(BitConverter.ToInt64(GetBytesFromRegisters(true), 0));
    }
}
