using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus.Abstractions
{
    public abstract class ModbusMultiBytesPoint<TValue> : ModbusRegisterPoint<TValue>
    {
        private bool skipFirstByte = false;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="skipFirstByte">첫 번째 바이트를 생략하고 두 번째 바이트부터 사용할 지 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청을 위한 데이터 주소</param>
        /// <param name="requestLength">요청을 위한 데이터 개수</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        protected ModbusMultiBytesPoint(byte slaveAddress, bool writable, ushort address, bool skipFirstByte, ModbusEndian endian, ushort? requestAddress, ushort? requestLength, bool? useMultiWriteFunction, IEnumerable<InterfaceHandler> handlers)
            : base(slaveAddress, writable, address, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            this.skipFirstByte = skipFirstByte;
        }

        public bool SkipFirstByte { get => skipFirstByte; set => SetProperty(ref skipFirstByte, value); }
        protected virtual int BytesCount => Marshal.SizeOf(typeof(TValue));
        public override ushort ActualRequestLength => RequestLength ?? (ushort)(AddressIndex + Math.Ceiling((double)(SkipFirstByte ? BytesCount + 1 : BytesCount) / 2));
        protected override bool DefaultUseMultiWriteFunction => Math.Ceiling((double)(SkipFirstByte ? BytesCount + 1 : BytesCount) / 2) > 1;

        protected override int RegistersCount => (int)Math.Ceiling((BytesCount + (skipFirstByte ? 1 : 0)) / 2d) * 2;

        protected byte[] GetBytesFromRegisters(bool useBitConverter)
        {
            var count = BytesCount;
            var skip = skipFirstByte ? 1 : 0;
            var rawCount = (int)Math.Ceiling((count + skip) / 2d) * 2;
            byte[] bytes;
            try
            {
                bytes = Registers?.GetRawData(Address, rawCount)?.ToArray() ?? new byte[rawCount];
            }
            catch
            {
                bytes = new byte[rawCount];
            }
            return Endian.Sort(bytes, useBitConverter).Skip(skip).Take(count).ToArray();
        }
        protected byte[] ToBytesInRegisters(byte[] bytes, bool useBitConverter)
        {
            var count = BytesCount;
            if (bytes.Length == count && !skipFirstByte)
            {
                return Endian.Sort(bytes, useBitConverter);
            }
            else
            {
                var skip = skipFirstByte ? 1 : 0;
                var rawCount = (int)Math.Ceiling((count + skip) / 2d) * 2;
                byte[] oldBytes;
                try
                {
                    oldBytes = Endian.Sort(Registers?.GetRawData(Address, rawCount)?.ToArray() ?? new byte[rawCount], useBitConverter);
                }
                catch
                {
                    oldBytes = new byte[rawCount];
                }
                bytes.CopyTo(oldBytes, skip);
                return Endian.Sort(oldBytes, useBitConverter);
            }
        }
    }
}
