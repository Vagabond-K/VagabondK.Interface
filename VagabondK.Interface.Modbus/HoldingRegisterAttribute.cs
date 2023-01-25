using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;

namespace VagabondK.Interface.Modbus
{
    /// <summary>
    /// Modbus Holding Register 형식 바인딩 멤버 정의를 위한 특성
    /// </summary>
    public class HoldingRegisterAttribute : WordAttribute
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="address">데이터 주소</param>
        public HoldingRegisterAttribute(ushort address) : base(address) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        public HoldingRegisterAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        public HoldingRegisterAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        public HoldingRegisterAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="address">데이터 주소</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부</param>
        public HoldingRegisterAttribute(ushort address, bool useMultiWriteFunction) : base(address) { UseMultiWriteFunction = useMultiWriteFunction; }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부</param>
        public HoldingRegisterAttribute(byte slaveAddress, ushort address, bool useMultiWriteFunction) : base(slaveAddress, address) { UseMultiWriteFunction = useMultiWriteFunction; }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부</param>
        public HoldingRegisterAttribute(ushort address, ushort requestAddress, ushort requestLength, bool useMultiWriteFunction) : base(address, requestAddress, requestLength) { UseMultiWriteFunction = useMultiWriteFunction; }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부</param>
        public HoldingRegisterAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength, bool useMultiWriteFunction) : base(slaveAddress, address, requestAddress, requestLength) { UseMultiWriteFunction = useMultiWriteFunction; }

        /// <summary>
        /// 쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부
        /// </summary>
        public bool? UseMultiWriteFunction { get; }

        /// <summary>
        /// 인터페이스 포인트 생성시 호출되는 메서드
        /// </summary>
        /// <param name="memberInfo">바인딩 할 멤버 정보</param>
        /// <param name="rootAttribute">자동 바인딩 시 지정한 최상위 인터페이스 특성</param>
        /// <returns>인터페이스 포인트</returns>
        protected override InterfacePoint OnCreatePoint(MemberInfo memberInfo, InterfaceAttribute rootAttribute) => OnCreatePoint(memberInfo, rootAttribute, true, UseMultiWriteFunction);
    }
}
