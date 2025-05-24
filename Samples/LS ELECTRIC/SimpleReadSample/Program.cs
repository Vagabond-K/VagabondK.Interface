using System;
using System.IO.Ports;
using VagabondK.Interface.LSElectric;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.LSElectric.Cnet;
//using VagabondK.Protocols.LSElectric.FEnet;

class Program
{
    [LSElectricPLC(1)]
    class InterfaceObject
    {
        [PlcPoint("%MX100")]
        public bool BitValue { get; private set; }
        [PlcPoint("%MB100")]
        public byte ByteValue { get; private set; }
        [PlcPoint("%MW100")]
        public short WordValue { get; private set; }
        [PlcPoint("%MD100")]
        public int DoubleWordValue { get; private set; }
        [PlcPoint("%ML100")]
        public long LongWordValue { get; private set; }
    }

    static void Main()
    {
        var channel = new SerialPortChannel("COM5", 9600, 8, StopBits.One, Parity.None, Handshake.None) //Cnet을 위한 시리얼 포트 채널
        //var channel = new TcpChannel("127.0.0.1", 2004) //FEnet을 위한 TCP 채널
        {
            Logger = new ConsoleChannelLogger()
        };

        var @interface = new CnetInterface(new CnetClient(channel)); //Cnet 인터페이스
        //var @interface = new FEnetInterface(new FEnetClient(channel)); //FEnet 인터페이스

        var obj = new InterfaceObject();
        @interface.SetBindings(obj);
        @interface.PollingCompleted += (s, e) =>
        {
            Console.WriteLine($"%MX100: {obj.BitValue}");
            Console.WriteLine($"%MB100: {obj.ByteValue}");
            Console.WriteLine($"%MW100: {obj.WordValue}");
            Console.WriteLine($"%MD100: {obj.DoubleWordValue}");
            Console.WriteLine($"%ML100: {obj.LongWordValue}");
        };
        @interface.Start();
        Console.ReadKey();
    }
}