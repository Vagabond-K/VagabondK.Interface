using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus.Abstractions
{
    public abstract class ModbusNumericPoint<TSerialize, TValue> : ModbusMultiBytesPoint<TValue>
    {
        private double scale = 1;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="scale">스케일 값</param>
        /// <param name="skipFirstByte">첫 번째 바이트를 생략하고 두 번째 바이트부터 사용할 지 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청을 위한 데이터 주소</param>
        /// <param name="requestLength">요청을 위한 데이터 개수</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        protected ModbusNumericPoint(byte slaveAddress, bool writable, ushort address, double scale, bool skipFirstByte, ModbusEndian endian, ushort? requestAddress, ushort? requestLength, bool? useMultiWriteFunction, IEnumerable<InterfaceHandler> handlers)
            : base(slaveAddress, writable, address, skipFirstByte, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            this.scale = scale;
        }

        private static readonly Lazy<Func<TSerialize, double>> serializeToDouble = new Lazy<Func<TSerialize, double>>(() => GenericValueConverter.GetConvert<TSerialize, double>());
        private static readonly Lazy<Func<TValue, double>> valueToDouble = new Lazy<Func<TValue, double>>(() => GenericValueConverter.GetConvert<TValue, double>());
        private static readonly Lazy<Func<TSerialize, TValue>> serializeToValue = new Lazy<Func<TSerialize, TValue>>(() => GenericValueConverter.GetConvert<TSerialize, TValue>());
        private static readonly Lazy<Func<double, TValue>> doubleToValue = new Lazy<Func<double, TValue>>(() => GenericValueConverter.GetConvert<double, TValue>());
        private static readonly Lazy<Func<TValue, TSerialize>> valueToSerialize = new Lazy<Func<TValue, TSerialize>>(() => GenericValueConverter.GetConvert<TValue, TSerialize>());
        private static readonly Lazy<Func<double, TSerialize>> doubleToSerialize = new Lazy<Func<double, TSerialize>>(() => GenericValueConverter.GetConvert<double, TSerialize>());

        private static double ToDouble(TValue value) => valueToDouble.Value.Invoke(value);
        private static double ToDouble(TSerialize serialize) => serializeToDouble.Value.Invoke(serialize);
        private static TValue ToValue(TSerialize serialize) => serializeToValue.Value.Invoke(serialize);
        private static TValue ToValue(double value) => doubleToValue.Value.Invoke(value);
        private static TSerialize ToSerialize(TValue value) => valueToSerialize.Value.Invoke(value);
        private static TSerialize ToSerialize(double value) => doubleToSerialize.Value.Invoke(value);

        private TValue DoScale(TSerialize serialize) => ToValue(ToDouble(serialize) * Scale);
        private TSerialize DoReverseScale(TValue value) => ToSerialize(ToDouble(value) / Scale);
 
        protected abstract TSerialize Deserialize();
        protected abstract byte[] Serialize(TSerialize serialize);

        protected override byte[] GetBytes(TValue value)
        {
            var bytes = typeof(TValue) == typeof(TSerialize) && scale == 1
            ? (this as ModbusNumericPoint<TValue, TValue>).Serialize(value)
            : Serialize(scale == 1 ? ToSerialize(value) : DoReverseScale(value));

            return ToBytesInRegisters(bytes, bytes.Length > 1);
        }

        protected override TValue GetValue() => typeof(TValue) == typeof(TSerialize) && scale == 1
            ? (this as ModbusNumericPoint<TValue, TValue>).Deserialize()
            : scale == 1 ? ToValue(Deserialize()) : DoScale(Deserialize());

        public double Scale { get => scale; set => SetProperty(ref scale, value); }
    }
}
