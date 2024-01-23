using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;

namespace VagabondK.Interface.Modbus.Abstractions
{
    /// <summary>
    /// 다중 Byte 기반 인터페이스 포인트
    /// </summary>
    /// <typeparam name="TValue">값 형식</typeparam>
    public abstract class MultiBytesPoint<TValue> : WordPoint<TValue>
    {
        private bool skipFirstByte = false;
        private readonly Lazy<int> bytesCount = new Lazy<int>(() => Marshal.SizeOf(typeof(TValue)));

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Holding Register, false일 경우는 Input Register</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="skipFirstByte">첫 번째 Byte 생략 여부</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        protected MultiBytesPoint(byte slaveAddress, bool writable, ushort address, bool skipFirstByte, ModbusEndian endian, ushort? requestAddress, ushort? requestLength, bool? useMultiWriteFunction, IEnumerable<InterfaceHandler> handlers)
            : base(slaveAddress, writable, address, endian, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            this.skipFirstByte = skipFirstByte;
        }

        /// <summary>
        /// 첫 번째 Byte 생략 여부
        /// </summary>
        public bool SkipFirstByte { get => skipFirstByte; set => SetProperty(ref skipFirstByte, value); }

        /// <summary>
        /// 직렬화 Byte 개수
        /// </summary>
        protected virtual int BytesCount => bytesCount.Value;

        /// <summary>
        /// 실제 요청 길이
        /// </summary>
        public override ushort ActualRequestLength => RequestLength ?? (ushort)(AddressIndex + Math.Ceiling((double)(SkipFirstByte ? BytesCount + 1 : BytesCount) / 2));

        /// <summary>
        /// 쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부 기본 값, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함
        /// </summary>
        protected override bool DefaultUseMultiWriteFunction => Math.Ceiling((double)(SkipFirstByte ? BytesCount + 1 : BytesCount) / 2) > 1;

        /// <summary>
        /// 값의 Word 단위 개수
        /// </summary>
        protected override int WordsCount => (int)Math.Ceiling((BytesCount + (skipFirstByte ? 1 : 0)) / 2d) * 2;

        /// <summary>
        /// 로컬 레지스터로부터 byte 배열 가져오기
        /// </summary>
        /// <param name="useBitConverter">BitConverter 사용 여부, ModbusEndian에 의한 byte 배열 정렬에서 사용함.</param>
        /// <returns>byte 배열</returns>
        protected byte[] GetBytesFromRegisters(bool useBitConverter)
        {
            var count = BytesCount;
            var skip = skipFirstByte ? 1 : 0;
            var rawCount = (int)Math.Ceiling((count + skip) / 2d) * 2;
            byte[] bytes;
            try
            {
                bytes = Words?.GetRawData(Address, rawCount)?.ToArray() ?? new byte[rawCount];
            }
            catch
            {
                bytes = new byte[rawCount];
            }
            return Endian.Sort(bytes, useBitConverter).Skip(skip).Take(count).ToArray();
        }

        /// <summary>
        /// 로컬 레지스터를 기준으로 byte 배열 만들기, SkipFirstByte가 true 이거나 BytesCount가 Word 단위와 일치하지 않을 때 빈 항목을 채우기 위한 메서드
        /// </summary>
        /// <param name="bytes">byte 배열</param>
        /// <param name="useBitConverter">BitConverter 사용 여부, ModbusEndian에 의한 byte 배열 정렬에서 사용함.</param>
        /// <returns>byte 배열</returns>
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
                    oldBytes = Endian.Sort(Words?.GetRawData(Address, rawCount)?.ToArray() ?? new byte[rawCount], useBitConverter);
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
