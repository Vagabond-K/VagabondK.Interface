using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Data;

namespace VagabondK.Interface.Modbus.Abstractions
{
    public abstract class WordPoint<TValue> : ModbusPoint<TValue>, IModbusWordPoint
    {
        private bool writable;

        private ModbusEndian endian = ModbusEndian.AllBig;
        private readonly object writeRequestLock = new object();

        public WordPoint(byte slaveAddress, bool writable, ushort address, ModbusEndian endian, ushort? requestAddress, ushort? requestLength, bool? useMultiWriteFunction, IEnumerable<InterfaceHandler> handlers)
            : base(slaveAddress, address, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            Writable = writable;
            this.endian = endian;
        }

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

        public ModbusEndian Endian { get => endian; set => SetProperty(ref endian, value); }

        protected ModbusWords Words { get; private set; }
        protected ModbusWriteHoldingRegisterRequest WriteRequest { get; set; }
        protected abstract bool DefaultUseMultiWriteFunction { get; }
        protected abstract int WordsCount { get; }
        int IModbusWordPoint.WordsCount => WordsCount;

        protected abstract TValue GetValue();

        protected abstract byte[] GetBytes(in TValue value);

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

        void IModbusWordPoint.SetReceivedValue(ModbusWords words, in DateTime? timeStamp)
        {
            Words = words;
            var value = GetValue();
            SetReceivedValue(value, timeStamp);
        }

    }
}
