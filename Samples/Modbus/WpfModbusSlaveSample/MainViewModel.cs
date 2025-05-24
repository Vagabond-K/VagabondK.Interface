using System;
using System.Collections.Generic;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Serialization;

namespace WpfModbusSlaveSample
{
    class MainViewModel
    {
        public MainViewModel()
        {
            var channel = new TcpChannelProvider(502);
            var @interface = new ModbusSlaveInterface(new ModbusSlaveService(channel, new ModbusTcpSerializer()));

            InterfaceObject = new InterfaceObject();
            InterfaceHandlers = @interface.SetBindings(InterfaceObject);
            @interface.Start();
        }

        public InterfaceObject InterfaceObject { get; }
        public Dictionary<string, InterfaceHandler> InterfaceHandlers { get; }
    }

    [Modbus(1)] //슬레이브 주소: 1
    class InterfaceObject : NotifyPropertyChangeObject
    {
        [DiscreteInput(100)]
        public bool BitValue1 { get => Get(false); set => Set(value); }
        [DiscreteInput(101)]
        public bool BitValue2 { get => Get(false); set => Set(value); }

        [InputRegister(200, BitFlagIndex = 0)]
        public bool BitFlagValue1 { get => Get(false); set => Set(value); }
        [InputRegister(200, BitFlagIndex = 1)]
        public bool BitFlagValue2 { get => Get(false); set => Set(value); }

        [InputRegister(300)] //100번지의 Word에서 Big 엔디안으로 첫 번째 byte값을 사용
        public byte ByteValue1 { get => Get<byte>(123); set => Set(value); }
        [InputRegister(300, SkipFirstByte = true)] //100번지의 Word에서 Big 엔디안으로 두 번째 byte값을 사용
        public byte ByteValue2 { get => Get<byte>(234); set => Set(value); }
        [InputRegister(301)]
        public short Int16Value { get => Get<short>(12345); set => Set(value); }
        [InputRegister(302)]
        public int Int32Value { get => Get(23456); set => Set(value); }
        [InputRegister(304)]
        public long Int64Value { get => Get(34567L); set => Set(value); }
        [InputRegister(308)]
        public float SingleValue { get => Get(12.34f); set => Set(value); }
        [InputRegister(310)]
        public double DoubleValue { get => Get(56.78); set => Set(value); }

        [Coil(400)]
        public bool SetBitValue1 { get => Get(false); set => Set(value); }
        [Coil(401)]
        public bool SetBitValue2 { get => Get(false); set => Set(value); }

        [HoldingRegister(500)]
        public float SetSingleValue1 { get => Get(0f); set => Set(value); }
        [HoldingRegister(502)]
        public float SetSingleValue2 { get => Get(0f); set => Set(value); }
    }
}
