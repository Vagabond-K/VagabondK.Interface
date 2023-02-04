using System.Collections.Generic;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus.Abstractions
{
    /// <summary>
    /// 가변 길이 기반 인터페이스 포인트
    /// </summary>
    /// <typeparam name="TValue">값 형식</typeparam>
    public abstract class VariableLengthPoint<TValue> : MultiBytesPoint<TValue>
    {
        private int bytesLength = 2;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="bytesLength">Byte 단위 설정 길이</param>
        /// <param name="skipFirstByte">첫 번째 Byte 생략 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        protected VariableLengthPoint(byte slaveAddress, bool writable, ushort address, int bytesLength, bool skipFirstByte, ModbusEndian endian, ushort? requestAddress, ushort? requestLength, bool? useMultiWriteFunction, IEnumerable<InterfaceHandler> handlers)
            : base(slaveAddress, writable, address, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            this.bytesLength = bytesLength;
        }

        /// <summary>
        /// 값의 Byte 단위 개수
        /// </summary>
        protected override int BytesCount => BytesLength;

        /// <summary>
        /// Byte 단위 설정 길이
        /// </summary>
        public int BytesLength { get => bytesLength; set => SetProperty(ref bytesLength, value); }
    }
}
