using System;
using System.IO.Ports;
using VagabondK.Interface.LSElectric;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.LSElectric.Cnet;

class Program
{
    [LSElectricPLC]
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
        var client = new CnetClient(new SerialPortChannel("COM5", 9600, 8, StopBits.One, Parity.None, Handshake.None)
        {
            Logger = new ConsoleChannelLogger()
        });

        var obj = new InterfaceObject();

        var cnet = new CnetInterface(client, 1);
        cnet.SetBindings(obj);
        cnet.PollingCompleted += (s, e) =>
        {
            Console.WriteLine($"%MX100: {obj.BitValue}");
            Console.WriteLine($"%MB100: {obj.ByteValue}");
            Console.WriteLine($"%MW100: {obj.WordValue}");
            Console.WriteLine($"%MD100: {obj.DoubleWordValue}");
            Console.WriteLine($"%ML100: {obj.LongWordValue}");
        };
        cnet.Start();
        Console.ReadKey();
    }
}