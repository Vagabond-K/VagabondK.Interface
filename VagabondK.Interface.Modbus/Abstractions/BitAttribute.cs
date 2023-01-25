namespace VagabondK.Interface.Modbus.Abstractions
{
    /// <summary>
    /// Modbus Bit(Coil, Discrete Input) 형식 바인딩 멤버 정의를 위한 특성
    /// </summary>
    public abstract class BitAttribute : ModbusBindingAttribute
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="address">데이터 주소</param>
        protected BitAttribute(ushort address) : base(address) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        protected BitAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        protected BitAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        protected BitAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }
    }
}
