using System;
using System.Collections.Generic;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus
{
    /// <summary>
    /// Modbus Word(Holding Register, Input Register) 기반의 double 형식 직렬화 인터페이스 포인트
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class DoublePoint<TValue> : NumericPoint<double, TValue>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="scale">수치형 값의 스케일(배율)</param>
        /// <param name="skipFirstByte">첫 번째 Byte 생략 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public DoublePoint(byte slaveAddress = 0, bool writable = true, ushort address = 0, double scale = 1, bool skipFirstByte = false, ModbusEndian endian = ModbusEndian.AllBig, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, writable, address, scale, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers) { }

        /// <summary>
        /// 로컬 레지스터를 이용하여 직렬화 형식으로 역 직렬화 수행.
        /// </summary>
        /// <returns>역 직렬화 된 값</returns>
        protected override double Deserialize() => BitConverter.ToDouble(GetBytesFromRegisters(true), 0);
        /// <summary>
        /// 직렬화 형식의 값을 byte 배열로 직렬화 수행.
        /// </summary>
        /// <param name="serialize">직렬화 할 값</param>
        /// <returns>직렬화 된 byte 배열</returns>
        protected override byte[] Serialize(double serialize) => BitConverter.GetBytes(serialize);
    }

    /// <summary>
    /// Modbus Word(Holding Register, Input Register) 기반의 double 형식 직렬화 인터페이스 포인트
    /// </summary>
    public class DoublePoint : DoublePoint<double>
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
        public DoublePoint(byte slaveAddress = 0, bool writable = true, ushort address = 0, bool skipFirstByte = false, ModbusEndian endian = ModbusEndian.AllBig, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, writable, address, 1, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers) { }
    }
}
