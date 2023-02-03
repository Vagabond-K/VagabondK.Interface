using System;
using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus.Abstractions
{
    /// <summary>
    /// Modbus Word(Holding Register, Input Register) 형식 바인딩 멤버 정의를 위한 특성
    /// </summary>
    public abstract class WordAttribute : ModbusPointAttribute
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="address">데이터 주소</param>
        protected WordAttribute(ushort address) : base(address) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        protected WordAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        protected WordAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        protected WordAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }

        /// <summary>
        /// 값 형식, null이면 바인딩 멤버의 형식을 따름.
        /// </summary>
        public Type Type { get; set; }
        /// <summary>
        /// Byte 단위 설정 길이. DateTime, byte[], string 형식에서 사용함.
        /// </summary>
        public int BytesLength { get; set; }
        /// <summary>
        /// 첫 번째 Byte 생략 여부
        /// </summary>
        public bool SkipFirstByte { get; set; }
        /// <summary>
        /// 수치형 값의 스케일(배율)
        /// </summary>
        public double Scale { get; set; } = 1;
        /// <summary>
        /// Modbus 엔디안
        /// </summary>
        public ModbusEndian Endian { get; set; } = ModbusEndian.AllBig;
        /// <summary>
        /// Bit 플래그 인덱스. Word 단위 데이터에서의 Bit 위치를 설정함. BitFlagPoint에서 사용.
        /// </summary>
        public byte BitFlagIndex { get; set; }
        /// <summary>
        /// string 형식 값의 직렬화/역직렬화를 위한 인코딩
        /// </summary>
        public string Encoding { get; set; } = System.Text.Encoding.UTF8.WebName;
        /// <summary>
        /// DateTime 값의 직렬화/역직렬화 형식
        /// </summary>
        public DateTimeFormat DateTimeFormat { get; set; } = DateTimeFormat.UnixTime;
        /// <summary>
        /// Modbus 데이터 상 DateTime 값의 DateTimeKind를 정의
        /// </summary>
        public DateTimeKind DateTimeKind { get; set; } = DateTimeKind.Utc;
        /// <summary>
        /// DateTime의 Ticks 및 Unix time의 10진수 단위 배율 
        /// </summary>
        public int TicksScalePowerOf10 { get; set; }
        /// <summary>
        /// DateTime 값의 직렬화/역직렬화를 위한 형식 문자열
        /// </summary>
        public string DateTimeFormatString { get; set; }

        /// <summary>
        /// 인터페이스 포인트 생성시 호출되는 메서드
        /// </summary>
        /// <param name="memberInfo">바인딩 할 멤버 정보</param>
        /// <param name="rootAttribute">자동 바인딩 시 지정한 최상위 인터페이스 특성</param>
        /// <param name="writable">쓰기 가능 여부</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <returns>인터페이스 포인트</returns>
        protected InterfacePoint OnCreatePoint(MemberInfo memberInfo, InterfaceAttribute rootAttribute, bool writable, bool? useMultiWriteFunction = null)
        {
            Type memberType;
            if (memberInfo is PropertyInfo property)
                memberType = property.PropertyType;
            else if (memberInfo is FieldInfo field)
                memberType = field.FieldType;
            else return null;

            var type = Type ?? memberType;
            var typeName = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? type.GenericTypeArguments[0].Name : type.Name;

            switch (typeName)
            {
                case nameof(Boolean):
                    return new BitFlagPoint(GetSlaveAddress(rootAttribute), writable, Address, BitFlagIndex, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                case nameof(SByte):
                case nameof(Byte):
                case nameof(Int16):
                case nameof(UInt16):
                case nameof(Int32):
                case nameof(UInt32):
                case nameof(Int64):
                case nameof(UInt64):
                case nameof(Single):
                case nameof(Double):
                    var numericPointType = typeof(ModbusPoint).Assembly.GetType($"{nameof(VagabondK)}.{nameof(Interface)}.{nameof(Modbus)}.{typeName}Point`1");
                    if (numericPointType == null) return null;
                    var pointType = numericPointType.MakeGenericType(memberType);
                    return (InterfacePoint)Activator.CreateInstance(pointType, GetSlaveAddress(rootAttribute), writable, Address, Scale, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                case nameof(DateTime):
                    switch (DateTimeFormat)
                    {
                        case DateTimeFormat.UnixTime:
                            return new UnixTimePoint(GetSlaveAddress(rootAttribute), writable, Address, TicksScalePowerOf10,
                                BytesLength <= 0 ? 8 : BytesLength, DateTimeKind, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                        case DateTimeFormat.DotNet:
                            return new DotNetDateTimePoint(GetSlaveAddress(rootAttribute), writable, Address, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                        case DateTimeFormat.Ticks:
                            return new TicksDateTimePoint(GetSlaveAddress(rootAttribute), writable, Address, TicksScalePowerOf10,
                                BytesLength <= 0 ? 8 : BytesLength, DateTimeKind, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                        case DateTimeFormat.String:
                            return new StringDateTimePoint(GetSlaveAddress(rootAttribute), writable, Address, DateTimeFormatString, DateTimeKind,
                                System.Text.Encoding.GetEncoding(Encoding), SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                        case DateTimeFormat.Bytes:
                            return new BytesDateTimePoint(GetSlaveAddress(rootAttribute), writable, Address, DateTimeFormatString, DateTimeKind,
                                SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                        default:
                            return null;
                    }
                case "Byte[]":
                    return new ByteArrayPoint(GetSlaveAddress(rootAttribute), writable, Address,
                        BytesLength, SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                case nameof(String):
                    return new StringPoint(GetSlaveAddress(rootAttribute), writable, Address,
                        BytesLength, System.Text.Encoding.GetEncoding(Encoding), SkipFirstByte, Endian, RequestAddress, RequestLength, useMultiWriteFunction, null);
                default:
                    return null;
            };
        }
    }
}
