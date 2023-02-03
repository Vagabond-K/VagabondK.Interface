﻿using System.Collections.Generic;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.LSElectric.Abstractions;
using VagabondK.Protocols.LSElectric;

namespace VagabondK.Interface.LSElectric
{
    /// <summary>
    /// short 형식 인터페이스 포인트
    /// </summary>
    public class Int16Point : PlcPoint<short>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="deviceVariable">디바이스 변수</param>
        /// <param name="handlers">인터페이스 처리기</param>
        public Int16Point(DeviceVariable deviceVariable, IEnumerable<InterfaceHandler> handlers = null)
            : base(deviceVariable, handlers)
        {
        }

        /// <summary>
        /// 인터페이스 포인트의 값을 PLC 디바이스 값으로 변환
        /// </summary>
        /// <param name="value">인터페이스 포인트 값</param>
        /// <returns>디바이스 값</returns>
        protected override DeviceValue ToDeviceValue(short value) => new DeviceValue(value);

        /// <summary>
        /// PLC 디바이스 값을 인터페이스 포인트 값으로 변환
        /// </summary>
        /// <param name="deviceValue">PLC 디바이스 값</param>
        /// <returns>인터페이스 포인트 값</returns>
        protected override short ToPointValue(DeviceValue deviceValue) => deviceValue;
    }
}