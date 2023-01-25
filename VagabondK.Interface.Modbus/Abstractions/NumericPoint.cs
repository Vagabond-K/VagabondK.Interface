using System;
using System.Collections.Generic;
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
        }

        private static readonly Lazy<Func<TSerialize, double>> serializeToDouble = new Lazy<Func<TSerialize, double>>(() => GenericValueConverter.GetConverter<TSerialize, double>());
        private static readonly Lazy<Func<TValue, double>> valueToDouble = new Lazy<Func<TValue, double>>(() => GenericValueConverter.GetConverter<TValue, double>());
        private static readonly Lazy<Func<TSerialize, TValue>> serializeToValue = new Lazy<Func<TSerialize, TValue>>(() => GenericValueConverter.GetConverter<TSerialize, TValue>());
        private static readonly Lazy<Func<double, TValue>> doubleToValue = new Lazy<Func<double, TValue>>(() => GenericValueConverter.GetConverter<double, TValue>());
        private static readonly Lazy<Func<TValue, TSerialize>> valueToSerialize = new Lazy<Func<TValue, TSerialize>>(() => GenericValueConverter.GetConverter<TValue, TSerialize>());
        private static readonly Lazy<Func<double, TSerialize>> doubleToSerialize = new Lazy<Func<double, TSerialize>>(() => GenericValueConverter.GetConverter<double, TSerialize>());

        private static double ToDouble(TValue value) => valueToDouble.Value.Invoke(value);
        private static double ToDouble(TSerialize serialize) => serializeToDouble.Value.Invoke(serialize);
        private static TValue ToValue(TSerialize serialize) => serializeToValue.Value.Invoke(serialize);
        private static TValue ToValue(double value) => doubleToValue.Value.Invoke(value);
        private static TSerialize ToSerialize(TValue value) => valueToSerialize.Value.Invoke(value);
        private static TSerialize ToSerialize(double value) => doubleToSerialize.Value.Invoke(value);

        private TValue DoScale(TSerialize serialize) => ToValue(ToDouble(serialize) * Scale);
        private TSerialize DoReverseScale(TValue value) => ToSerialize(ToDouble(value) / Scale);

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
        /// 값을 byte 배열로 직렬화
        /// </summary>
        /// <param name="value">직렬화 할 값</param>
        /// <returns>직렬화 된 byte 배열</returns>
        protected override byte[] GetBytes(in TValue value)
        {
            var bytes = typeof(TValue) == typeof(TSerialize) && scale == 1
            ? (this as NumericPoint<TValue, TValue>).Serialize(value)
            : Serialize(scale == 1 ? ToSerialize(value) : DoReverseScale(value));

            return ToBytesInRegisters(bytes, bytes.Length > 1);
        }

        /// <summary>
        /// 로컬 레지스터로부터 값 가져오기
        /// </summary>
        /// <returns>인터페이스 결과 값</returns>
        protected override TValue GetValue() => typeof(TValue) == typeof(TSerialize) && scale == 1
            ? (this as NumericPoint<TValue, TValue>).Deserialize()
            : scale == 1 ? ToValue(Deserialize()) : DoScale(Deserialize());

        /// <summary>
        /// 수치형 값의 스케일(배율)
        /// </summary>
        public double Scale { get => scale; set => SetProperty(ref scale, value); }
    }
}
