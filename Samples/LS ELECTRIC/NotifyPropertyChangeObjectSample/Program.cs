using System;
using System.IO.Ports;
using System.Threading;
using VagabondK.Interface.LSElectric;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.LSElectric.Cnet;

class Program
{
    [LSElectricPLC]
    class InterfaceObject : NotifyPropertyChangeObject
    {
        [PlcPoint("%MX100")]
        public bool BitValue { get => Get(false); set => Set(value); }

        [PlcPoint("%MB100")]
        public byte ByteValue { get => Get<byte>(0); set => Set(value); }

        [PlcPoint("%MW100")]
        public short WordValue { get => Get<short>(0); set => Set(value); }

        [PlcPoint("%MD100")]
        public int DoubleWordValue { get => Get(0); set => Set(value); }

        [PlcPoint("%ML100")]
        public long LongWordValue { get => Get<long>(0); set => Set(value); }
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

        while (true)
        {
            Thread.Sleep(5000);
            obj.BitValue = !obj.BitValue;
            obj.ByteValue++;
            obj.WordValue++;
            obj.DoubleWordValue++;
            obj.LongWordValue++;
        }
    }
}