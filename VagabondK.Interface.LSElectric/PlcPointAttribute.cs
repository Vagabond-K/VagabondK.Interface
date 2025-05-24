using System;
using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.LSElectric;

namespace VagabondK.Interface.LSElectric
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) PLC 인터페이스 기반 바인딩 멤버 정의를 위한 특성
    /// </summary>
    public class PlcPointAttribute : InterfacePointAttribute
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수 문자열 맨 처음 %를 표기하고 디바이스 종류, 데이터 형식, 주소 순으로 입력함. (예: %MW100)</param>
        public PlcPointAttribute(string deviceVariable)
        {
            try
            {
                DeviceVariable = deviceVariable;
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ex.Message, nameof(deviceVariable), ex);
            }
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수 문자열 맨 처음 %를 표기하고 디바이스 종류, 데이터 형식, 주소 순으로 입력함. (예: %MW100)</param>
        /// <param name="stationNumber">국번</param>
        public PlcPointAttribute(string deviceVariable, byte stationNumber) : this(deviceVariable)
        {
            StationNumber = stationNumber;
        }

        /// <summary>
        /// 디바이스 변수
        /// </summary>
        public DeviceVariable DeviceVariable { get; }

        /// <summary>
        /// 국번
        /// </summary>
        public byte? StationNumber { get; }

        /// <summary>
        /// 인터페이스 포인트 생성시 호출되는 메서드
        /// </summary>
        /// <param name="memberInfo">바인딩 할 멤버 정보</param>
        /// <param name="rootAttribute">자동 바인딩 시 지정한 최상위 인터페이스 특성</param>
        /// <returns>인터페이스 포인트</returns>
        protected override InterfacePoint OnCreatePoint(MemberInfo memberInfo, InterfaceAttribute rootAttribute)
        {
            Type memberType;
            if (memberInfo is PropertyInfo property)
                memberType = property.PropertyType;
            else if (memberInfo is FieldInfo field)
                memberType = field.FieldType;
            else return null;

            var typeName = memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(Nullable<>) ? memberType.GenericTypeArguments[0].Name : memberType.Name;
            var stationNumber = StationNumber ?? (rootAttribute as LSElectricPLCAttribute)?.StationNumber ?? 0;

            switch (typeName)
            {
                case nameof(Boolean):
                    return new BooleanPoint(DeviceVariable) { StationNumber = stationNumber };
                case nameof(SByte):
                    return new SBytePoint(DeviceVariable) { StationNumber = stationNumber };
                case nameof(Byte):
                    return new BytePoint(DeviceVariable) { StationNumber = stationNumber };
                case nameof(Int16):
                    return new Int16Point(DeviceVariable) { StationNumber = stationNumber };
                case nameof(UInt16):
                    return new UInt16Point(DeviceVariable) { StationNumber = stationNumber };
                case nameof(Int32):
                    return new Int32Point(DeviceVariable) { StationNumber = stationNumber };
                case nameof(UInt32):
                    return new UInt32Point(DeviceVariable) { StationNumber = stationNumber };
                case nameof(Int64):
                    return new Int64Point(DeviceVariable) { StationNumber = stationNumber };
                case nameof(UInt64):
                    return new UInt64Point(DeviceVariable) { StationNumber = stationNumber };
                case nameof(Single):
                    return new SinglePoint(DeviceVariable) { StationNumber = stationNumber };
                case nameof(Double):
                    return new DoublePoint(DeviceVariable) { StationNumber = stationNumber };
                default:
                    return null;
            };

        }
    }
}
