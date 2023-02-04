using System;
using System.Threading;
using VagabondK.Interface.Modbus;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Serialization;

class Program
{
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
        public bool SetBitValue { get => Get(false); set => Set(value); }
        [HoldingRegister(500)]
        public float SetSingleValue { get => Get(0f); set => Set(value); }
    }

    static void Main()
    {
        var channel = new TcpChannel("127.0.0.1", 502)
        {
            Logger = new ConsoleChannelLogger()
        };

        var @interface = new ModbusMasterInterface(new ModbusMaster(channel, new ModbusTcpSerializer()));

        var obj = new InterfaceObject();
        var handlers = @interface.SetBindings(obj);
        @interface.PollingCompleted += (s, e) =>
        {
            Console.WriteLine($"BitValue1: {obj.BitValue1}");
            Console.WriteLine($"BitValue2: {obj.BitValue2}");
            Console.WriteLine($"BitFlagValue1: {obj.BitFlagValue1}");
            Console.WriteLine($"BitFlagValue2: {obj.BitFlagValue2}");
            Console.WriteLine($"ByteValue1: {obj.ByteValue1}");
            Console.WriteLine($"ByteValue2: {obj.ByteValue2}");
            Console.WriteLine($"Int16Value: {obj.Int16Value}");
            Console.WriteLine($"Int32Value: {obj.Int32Value}");
            Console.WriteLine($"Int64Value: {obj.Int64Value}");
            Console.WriteLine($"SingleValue: {obj.SingleValue}");
            Console.WriteLine($"DoubleValue: {obj.DoubleValue}");

            Console.WriteLine($"SetBitValue1: {obj.SetBitValue}");
            Console.WriteLine($"SetSingleValue1: {obj.SetSingleValue}");
        };
        @interface.Start();

        while (true)
        {
            Thread.Sleep(5000);

            //속성 설정을 이용한 값 전송
            obj.SetBitValue = !obj.SetBitValue;
            obj.SetSingleValue++;

            //인터페이스 핸들러를 이용한 값 전송
            //handlers[nameof(obj.SetBitValue)].Send(!obj.SetBitValue);
            //handlers[nameof(obj.SetSingleValue)].Send(obj.SetSingleValue + 1);
        }
    }
}