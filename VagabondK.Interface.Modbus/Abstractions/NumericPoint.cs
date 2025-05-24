using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus.Abstractions
{
    /// <summary>
    /// 수치형 인터페이스 포인트
    /// </summary>
    /// <typeparam name="TSerialize">직렬화 형식</typeparam>
    /// <typeparam name="TValue">값 형식</typeparam>
    public abstract class NumericPoint<TSerialize, TValue> : MultiBytesPoint<TValue>
    {
        private double scale = 1;
        private readonly Lazy<int> bytesCount = new Lazy<int>(() => Marshal.SizeOf(typeof(TSerialize)));

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
        protected NumericPoint(byte slaveAddress, bool writable, ushort address, double scale, bool skipFirstByte, ModbusEndian endian, ushort? requestAddress, ushort? requestLength, bool? useMultiWriteFunction, IEnumerable<InterfaceHandler> handlers)
            : base(slaveAddress, writable, address, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            this.scale = scale;
            UpdateInternalMethods();
        }

        private Func<TValue, byte[]> funcGetBytes;
        private Func<TValue> funcGetValue;

        private void UpdateInternalMethods()
        {
            if (typeof(TValue) == typeof(TSerialize) && scale == 1)
            {
                funcGetBytes = (value) => (this as NumericPoint<TValue, TValue>).Serialize(value);
                funcGetValue = () => (this as NumericPoint<TValue, TValue>).Deserialize();
            }
            else if (scale == 1)
            {
                funcGetBytes = (value) => Serialize(value.To<TValue, TSerialize>());
                funcGetValue = () => Deserialize().To<TSerialize, TValue>();
            }
            else
            {
                funcGetBytes = (value) => Serialize(DoReverseScale(value));
                funcGetValue = () => DoScale(Deserialize()); ;
            }
        }

        private TValue DoScale(TSerialize serialize) => (serialize.To<TSerialize, double>() * Scale).To<double, TValue>();
        private TSerialize DoReverseScale(TValue value) => (value.To<TValue, double>() / Scale).To<double, TSerialize>();

        /// <summary>
        /// 로컬 레지스터를 이용하여 직렬화 형식으로 역 직렬화 수행.
        /// </summary>
        /// <returns>역 직렬화 된 값</returns>
        protected abstract TSerialize Deserialize();

        /// <summary>
        /// 직렬화 형식의 값을 byte 배열로 직렬화 수행.
        /// </summary>
        /// <param name="serialize">직렬화 할 값</param>
        /// <returns>직렬화 된 byte 배열</returns>
        protected abstract byte[] Serialize(TSerialize serialize);

        /// <summary>
        /// 직렬화 Byte 개수
        /// </summary>
        protected override int BytesCount => bytesCount.Value;

        /// <summary>
        /// 값을 byte 배열로 직렬화
        /// </summary>
        /// <param name="value">직렬화 할 값</param>
        /// <returns>직렬화 된 byte 배열</returns>
        protected override byte[] GetBytes(in TValue value)
        {
            var bytes = funcGetBytes(value);

            return ToBytesInRegisters(bytes, bytes.Length > 1);
        }

        /// <summary>
        /// 로컬 레지스터로부터 값 가져오기
        /// </summary>
        /// <returns>인터페이스 결과 값</returns>
        protected override TValue GetValue() => funcGetValue();

        /// <summary>
        /// 수치형 값의 스케일(배율)
        /// </summary>
        public double Scale { get => scale; set => SetProperty(ref scale, value, () => UpdateInternalMethods()); }
    }
}
