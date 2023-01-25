using System.Reflection;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;

namespace VagabondK.Interface.Modbus
{
    /// <summary>
    /// Modbus Input Register 형식 바인딩 멤버 정의를 위한 특성
    /// </summary>
    public class InputRegisterAttribute : WordAttribute
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="address">데이터 주소</param>
        public InputRegisterAttribute(ushort address) : base(address) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        public InputRegisterAttribute(byte slaveAddress, ushort address) : base(slaveAddress, address) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        public InputRegisterAttribute(ushort address, ushort requestAddress, ushort requestLength) : base(address, requestAddress, requestLength) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        public InputRegisterAttribute(byte slaveAddress, ushort address, ushort requestAddress, ushort requestLength) : base(slaveAddress, address, requestAddress, requestLength) { }

        /// <summary>
        /// 인터페이스 포인트 생성시 호출되는 메서드
        /// </summary>
        /// <param name="memberInfo">바인딩 할 멤버 정보</param>
        /// <param name="rootAttribute">자동 바인딩 시 지정한 최상위 인터페이스 특성</param>
        /// <returns>인터페이스 포인트</returns>
        protected override InterfacePoint OnCreatePoint(MemberInfo memberInfo, InterfaceAttribute rootAttribute) => OnCreatePoint(memberInfo, rootAttribute, false);
    }
}
