using System;
using System.Collections.Generic;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus
{
    public class BitFlagPoint : WordPoint<bool>
    {
        private int bitIndex;
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="bitIndex">비트 위치</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청을 위한 데이터 주소</param>
        /// <param name="requestLength">요청을 위한 데이터 개수</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public BitFlagPoint(byte slaveAddress = 0, bool writable = true, ushort address = 0, int bitIndex = 0, ModbusEndian endian = ModbusEndian.AllBig, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, writable, address, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            this.bitIndex = bitIndex;
        }

        public int BitIndex { get => bitIndex; set => SetProperty(ref bitIndex, value); }
        public override ushort ActualRequestLength => RequestLength ?? (ushort)(AddressIndex + 1);
        protected override bool DefaultUseMultiWriteFunction => false;

        protected override int WordsCount => 1;

        protected override byte[] GetBytes(in bool value)
        {
            var words = Words;
            if (words == null) return new byte[] { 0, 0 };
            try
            {
                var bitFlags = words.GetUInt16(Address, Endian.HasFlag(ModbusEndian.InnerBig));
                return Endian.Sort(BitConverter.GetBytes(value
                    ? (ushort)(bitFlags | (1 << BitIndex))
                    : (ushort)(bitFlags & ~(1 << BitIndex))));
            }
            catch
            {
                return new byte[] { 0, 0 };
            }
        }

        protected override bool GetValue()
        {
            var words = Words;
            if (words == null) return false;
            try
            {
                return ((words.GetUInt16(Address, Endian.HasFlag(ModbusEndian.InnerBig)) >> BitIndex) & 1) == 1;
            }
            catch
            {
                return false;
            }
        }
    }
}
