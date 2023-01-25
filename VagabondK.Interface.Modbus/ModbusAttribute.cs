using System;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;

namespace VagabondK.Interface.Modbus
{
    /// <summary>
    /// Modbus 인터페이스 바인딩을 위한 객체를 정의하는 특성
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class ModbusAttribute : InterfaceAttribute
    {
        /// <summary>
        /// 생성자
        /// </summary>
        public ModbusAttribute() { }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        public ModbusAttribute(byte slaveAddress)
        {
            SlaveAddress = slaveAddress;
        }

        /// <summary>
        /// 슬레이브 주소
        /// </summary>
        public byte? SlaveAddress { get; }

        /// <summary>
        /// 바인딩 할 인터페이스 포인트 형식(ModbusPoint 형식)
        /// </summary>
        public override Type InterfacePointType => typeof(ModbusPoint);
    }
}
