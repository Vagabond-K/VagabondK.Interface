using VagabondK.Interface.Abstractions;

namespace VagabondK.Interface.Modbus.Abstractions
{
    /// <summary>
    /// Modbus 인터페이스 기반 바인딩 멤버 정의를 위한 특성
    /// </summary>
    public abstract class ModbusPointAttribute : InterfacePointAttribute
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="address">데이터 주소</param>
        protected ModbusPointAttribute(ushort address)
        {
            Address = address;
        }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        protected ModbusPointAttribute(byte slaveAddress, ushort address) : this(address)
        {
            SlaveAddress = slaveAddress;
        }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        protected ModbusPointAttribute(ushort address, ushort requestAddress, ushort requestLength) : this(address)
        {
            RequestAddress = requestAddress;
            RequestLength = requestLength;
        }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        protected ModbusPointAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : this(slaveAddress, address, requestAddress)
        {
            RequestLength = requestLength;
        }

        /// <summary>
        /// 슬레이브 주소
        /// </summary>
        public byte? SlaveAddress { get; }
        /// <summary>
        /// 데이터 주소
        /// </summary>
        public ushort Address { get; }
        /// <summary>
        /// 요청 시작 주소
        /// </summary>
        public ushort? RequestAddress { get; }
        /// <summary>
        /// 요청 길이
        /// </summary>
        public ushort? RequestLength { get; }

        /// <summary>
        /// 슬레이브 주소 가져오기, 최상위 바인딩 객체의 특성에서 대표 슬레이브 주소를 가져올 수 있습니다.
        /// </summary>
        /// <param name="rootAttribute">최상위 인터페이스 특성</param>
        /// <returns>슬레이브 주소</returns>
        protected byte GetSlaveAddress(InterfaceAttribute rootAttribute)
            => SlaveAddress ?? (rootAttribute as ModbusAttribute)?.SlaveAddress ?? 0;
    }
}
