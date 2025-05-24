using System;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.LSElectric.Abstractions;

namespace VagabondK.Interface.LSElectric
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) PLC 인터페이스 바인딩을 위한 객체를 정의하는 특성
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class LSElectricPLCAttribute : InterfaceAttribute
    {
        /// <summary>
        /// 생성자
        /// </summary>
        public LSElectricPLCAttribute() { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="stationNumber">국번</param>
        public LSElectricPLCAttribute(byte stationNumber)
        {
            StationNumber = stationNumber;
        }

        /// <summary>
        /// 국번
        /// </summary>
        public byte? StationNumber { get; }

        /// <summary>
        /// 바인딩 할 인터페이스 포인트 형식(PlcPoint 형식)
        /// </summary>
        public override Type InterfacePointType => typeof(PlcPoint);
    }
}
