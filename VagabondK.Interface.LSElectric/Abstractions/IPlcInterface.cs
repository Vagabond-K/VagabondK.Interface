using VagabondK.Protocols.LSElectric;

namespace VagabondK.Interface.LSElectric.Abstractions
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) PLC와 인터페이스를 위한 추상 정의
    /// </summary>
    interface IPlcInterface
    {
        /// <summary>
        /// PLC 디바이스에 값 쓰기
        /// </summary>
        /// <param name="point">PLC 인터페이스 포인트</param>
        /// <param name="deviceValue">PLC 디바이스 값</param>
        /// <returns>정상 처리 여부</returns>
        bool Write(PlcPoint point, DeviceValue deviceValue);
    }
}
