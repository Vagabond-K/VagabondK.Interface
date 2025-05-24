using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Data;

namespace VagabondK.Interface.Modbus.Abstractions
{
    /// <summary>
    /// Modbus Word(Holding Register, Input Register) 형식 인터페이스 포인트
    /// </summary>
    /// <typeparam name="TValue">값 형식</typeparam>
    public abstract class WordPoint<TValue> : ModbusPoint<TValue>, IModbusWordPoint
    {
        private bool writable;

        private ModbusEndian endian = ModbusEndian.AllBig;
        private readonly object writeRequestLock = new object();

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="endian">Modbus 엔디안</param>
        /// <param name="requestAddress">요청 시작 주소</param>
        /// <param name="requestLength">요청 길이</param>
        /// <param name="useMultiWriteFunction">다중 쓰기 Function 사용 여부</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public WordPoint(byte slaveAddress, bool writable, ushort address, ModbusEndian endian, ushort? requestAddress, ushort? requestLength, bool? useMultiWriteFunction, IEnumerable<InterfaceHandler> handlers)
            : base(slaveAddress, address, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            Writable = writable;
            this.endian = endian;
        }

        /// <summary>
        /// 쓰기 가능 여부
        /// </summary>
        public override bool Writable
        {
            get => writable;
            protected set
            {
                var newValue = value ? ModbusObjectType.HoldingRegister : ModbusObjectType.InputRegister;
                if (ObjectType != newValue)
                {
                    RaisePropertyChanging();
                    writable = value;
                    RaisePropertyChanged();
                    ObjectType = newValue;
                }
            }
        }

        /// <summary>
        /// Modbus 엔디안
        /// </summary>
        public ModbusEndian Endian { get => endian; set => SetProperty(ref endian, value); }

        /// <summary>
        /// 로컬 레지스터, Word 단위 데이터세트.
        /// </summary>
        protected ModbusWords Words { get; private set; }

        /// <summary>
        /// Holding Register 쓰기 요청
        /// </summary>
        protected ModbusWriteHoldingRegisterRequest WriteRequest { get; set; }

        /// <summary>
        /// 쓰기 요청 시 다중 쓰기 Function(0x10) 사용 여부 기본 값, Holding Register일 경우만 적용되고 Input Register일 경우는 무시함
        /// </summary>
        protected abstract bool DefaultUseMultiWriteFunction { get; }

        /// <summary>
        /// 값의 Word 단위 개수
        /// </summary>
        protected abstract int WordsCount { get; }
        int IModbusWordPoint.WordsCount => WordsCount;

        /// <summary>
        /// 로컬 레지스터로부터 값 가져오기
        /// </summary>
        /// <returns>인터페이스 결과 값</returns>
        protected abstract TValue GetValue();

        /// <summary>
        /// 값을 byte 배열로 직렬화
        /// </summary>
        /// <param name="value">직렬화 할 값</param>
        /// <returns>직렬화 된 byte 배열</returns>
        protected abstract byte[] GetBytes(in TValue value);

        /// <summary>
        /// Modbus 마스터를 이용하여 값을 전송하고자 할 때 호출되는 메서드
        /// </summary>
        /// <param name="master">Modbus 마스터</param>
        /// <param name="value">전송할 값</param>
        /// <returns>전송 성공 여부</returns>
        protected override bool OnSendRequested(ModbusMaster master, in TValue value)
        {
            lock (writeRequestLock)
            {
                var bytes = GetBytes(value);

                if (!writable) return false;

                var writeRequest = WriteRequest;

                if (writeRequest == null || writeRequest.Function == ModbusFunction.WriteMultipleHoldingRegisters != (UseMultiWriteFunction ?? DefaultUseMultiWriteFunction))
                    WriteRequest = writeRequest = (UseMultiWriteFunction ?? DefaultUseMultiWriteFunction)
                    ? new ModbusWriteHoldingRegisterRequest(SlaveAddress, Address, Enumerable.Repeat((byte)0, bytes.Length))
                    : new ModbusWriteHoldingRegisterRequest(SlaveAddress, Address, 0);

                if (writeRequest.Function == ModbusFunction.WriteMultipleHoldingRegisters)
                {
                    for (int i = 0; i < bytes.Length; i++)
                        writeRequest.Bytes[i] = bytes[i];
                    return master.Request(writeRequest) is ModbusOkResponse;
                }
                else
                {
                    bool result = false;
                    int requestCount = bytes.Length / 2;
                    for (int i = 0; i < requestCount; i++)
                    {
                        writeRequest.Bytes[0] = bytes[i * 2];
                        writeRequest.Bytes[1] = bytes[i * 2 + 1];
                        writeRequest.Address = (ushort)(Address + i);
                        result = master.Request(writeRequest) is ModbusOkResponse;
                    }
                    return result;
                }
            }
        }

        /// <summary>
        /// Modbus 슬레이브를 이용하여 값을 전송하고자 할 때 호출되는 메서드
        /// </summary>
        /// <param name="slave">Modbus 슬레이브</param>
        /// <param name="value">전송할 값</param>
        /// <returns>전송 성공 여부</returns>
        protected override bool OnSendRequested(ModbusSlave slave, in TValue value)
        {
            try
            {
                (writable ? slave.HoldingRegisters : slave.InputRegisters).SetRawData(Address, GetBytes(value));
                return true;
            }
            catch
            {
                return false;
            }
        }

        void IModbusWordPoint.SetWords(ModbusWords words)
        {
            Words = words;
        }

        void IModbusWordPoint.SetReceivedValue(ModbusWords words)
        {
            Words = words;
            var value = GetValue();
            SetReceivedValue(value, null);
        }

    }
}
