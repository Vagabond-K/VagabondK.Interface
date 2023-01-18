using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Data;

namespace VagabondK.Interface.Modbus
{
    public class ModbusBooleanPoint : ModbusPoint<bool>
    {
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <param name="writable">쓰기 가능 여부, true일 경우는 Coil, false일 경우는 Discrete Input</param>
        /// <param name="address">데이터 주소</param>
        /// <param name="requestAddress">요청을 위한 데이터 주소</param>
        /// <param name="requestLength">요청을 위한 데이터 개수</param>
        /// <param name="useMultiWriteFunction">쓰기 요청 시 다중 쓰기 Function(0x0f) 사용 여부, Coil일 경우만 적용되고 Discrete Input일 경우는 무시함</param>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public ModbusBooleanPoint(byte slaveAddress = 0, bool writable = true, ushort address = 0, ushort? requestAddress = null, ushort? requestLength = null, bool? useMultiWriteFunction = null, IEnumerable<InterfaceHandler> handlers = null)
            : base(slaveAddress, address, requestAddress, requestLength, useMultiWriteFunction, handlers)
        {
            Writable = writable;
        }

        public override bool Writable
        {
            get => writable;
            protected set
            {
                var newValue = value ? ModbusObjectType.Coil : ModbusObjectType.DiscreteInput;
                if (ObjectType != newValue)
                {
                    RaisePropertyChanging();
                    writable = value;
                    RaisePropertyChanged();
                    ObjectType = newValue;
                }
            }
        }

        public override ushort ActualRequestLength => RequestLength ?? (ushort)(AddressIndex + 1);

        private bool writable;
        private ModbusWriteCoilRequest writeRequest;
        private readonly object writeRequestLock = new object();

        protected override bool OnSendRequested(ModbusMaster master, ref bool value)
        {
            lock (writeRequestLock)
            {
                if (!writable) return false;

                if (writeRequest == null || writeRequest.Function == ModbusFunction.WriteMultipleCoils != (UseMultiWriteFunction ?? false))
                    writeRequest = (UseMultiWriteFunction ?? false)
                    ? new ModbusWriteCoilRequest(SlaveAddress, Address, new[] { value })
                    : new ModbusWriteCoilRequest(SlaveAddress, Address, value);
                else
                    writeRequest.Values[0] = value;

                return master.Request(writeRequest) is ModbusOkResponse;
            }
        }

        protected override bool OnSendRequested(ModbusSlave slave, ref bool value)
        {
            try
            {
                (writable ? slave.Coils : slave.DiscreteInputs)[Address] = value;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
