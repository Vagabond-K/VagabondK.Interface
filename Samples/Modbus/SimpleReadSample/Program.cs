using System;
using VagabondK.Interface.Modbus;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Serialization;

class Program
{
    [Modbus(1)] //슬레이브 주소: 1
    class InterfaceObject
    {
        [DiscreteInput(100)]
        public bool BitValue1 { get; private set; }
        [DiscreteInput(101)]
        public bool BitValue2 { get; private set; }

        [InputRegister(200, BitFlagIndex = 0)]
        public bool BitFlagValue1 { get; private set; }
        [InputRegister(200, BitFlagIndex = 1)]
        public bool BitFlagValue2 { get; private set; }

        [InputRegister(300)] //100번지의 Word에서 Big 엔디안으로 첫 번째 byte값을 사용
        public byte ByteValue1 { get; private set; }
        [InputRegister(300, SkipFirstByte = true)] //100번지의 Word에서 Big 엔디안으로 두 번째 byte값을 사용
        public byte ByteValue2 { get; private set; }
        [InputRegister(301)]
        public short Int16Value { get; private set; }
        [InputRegister(302)]
        public int Int32Value { get; private set; }
        [InputRegister(304)]
        public long Int64Value { get; private set; }
        [InputRegister(308)]
        public float SingleValue { get; private set; }
        [InputRegister(310)]
        public double DoubleValue { get; private set; }
    }

    static void Main()
    {
        var channel = new TcpChannel("127.0.0.1", 502)
        {
            Logger = new ConsoleChannelLogger()
        };
        
        var @interface = new ModbusMasterInterface(new ModbusMaster(channel, new ModbusTcpSerializer()));

        var obj = new InterfaceObject();
        @interface.SetBindings(obj);
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
        };
        @interface.Start();
        Console.ReadKey();
    }
}