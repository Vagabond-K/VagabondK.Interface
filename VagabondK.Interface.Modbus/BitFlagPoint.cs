using System;
using System.Collections.Generic;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus
{
    /// <summary>
    /// Modbus Word(Holding Register, Input Register) 데이터에서의 Bit 플래그 기반 인터페이스 포인트
    /// </summary>
    public class BitFlagPoint : WordPoint<bool>
    {
        private int bitFlagIndex;
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="bitFlagIndex">비트 위치</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public BitFlagPoint(byte slaveAddress = 0, bool writable = true, ushort address = 0, int bitFlagIndex = 0, ModbusEndian endian = ModbusEndian.AllBig, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, writable, address, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            this.bitFlagIndex = bitFlagIndex;
        }

        /// <summary>
        /// Bit 플래그 인덱스. Word 단위 데이터에서의 Bit 위치를 설정함. BitFlagPoint에서 사용.
        /// </summary>
        public int BitFlagIndex { get => bitFlagIndex; set => SetProperty(ref bitFlagIndex, value); }
        /// <summary>
        /// 실제 요청 길이
        /// </summary>
        public override ushort ActualRequestLength => RequestLength ?? (ushort)(AddressIndex + 1);
        /// <summary>
        /// 쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부 기본 값, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함
        /// </summary>
        protected override bool DefaultUseMultiWriteFunction => false;

        /// <summary>
        /// 값의 Word 단위 개수, BitFlagPoint에서는 1로 고정됨.
        /// </summary>
        protected override int WordsCount => 1;

        /// <summary>
        /// 값을 byte 배열로 직렬화
        /// </summary>
        /// <param name="value">직렬화 할 값</param>
        /// <returns>직렬화 된 byte 배열</returns>
        protected override byte[] GetBytes(in bool value)
        {
            var words = Words;
            if (words == null) return new byte[] { 0, 0 };
            try
            {
                var bitFlags = words.GetUInt16(Address, Endian.HasFlag(ModbusEndian.InnerBig));
                return Endian.Sort(BitConverter.GetBytes(value
                    ? (ushort)(bitFlags | (1 << BitFlagIndex))
                    : (ushort)(bitFlags & ~(1 << BitFlagIndex))));
            }
            catch
            {
                return new byte[] { 0, 0 };
            }
        }

        /// <summary>
        /// 로컬 레지스터로부터 값 가져오기
        /// </summary>
        /// <returns>인터페이스 결과 값</returns>
        protected override bool GetValue()
        {
            var words = Words;
            if (words == null) return false;
            try
            {
                return ((words.GetUInt16(Address, Endian.HasFlag(ModbusEndian.InnerBig)) >> BitFlagIndex) & 1) == 1;
            }
            catch
            {
                return false;
            }
        }
    }
}
