using System;
using System.IO.Ports;
using System.Threading;
using VagabondK.Interface.LSElectric;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.LSElectric.Cnet;
//using VagabondK.Protocols.LSElectric.FEnet;

class Program
{
    [LSElectricPLC(1)]
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
        var channel = new SerialPortChannel("COM5", 9600, 8, StopBits.One, Parity.None, Handshake.None) //Cnet을 위한 시리얼 포트 채널
        //var channel = new TcpChannel("127.0.0.1", 2004) //FEnet을 위한 TCP 채널
        {
            Logger = new ConsoleChannelLogger()
        };

        var @interface = new CnetInterface(new CnetClient(channel)); //Cnet 인터페이스
        //var @interface = new FEnetInterface(new FEnetClient(channel)); //FEnet 인터페이스

        var obj = new InterfaceObject();
        var handlers = @interface.SetBindings(obj);
        @interface.PollingCompleted += (s, e) =>
        {
            Console.WriteLine($"%MX100: {obj.BitValue}");
            Console.WriteLine($"%MB100: {obj.ByteValue}");
            Console.WriteLine($"%MW100: {obj.WordValue}");
            Console.WriteLine($"%MD100: {obj.DoubleWordValue}");
            Console.WriteLine($"%ML100: {obj.LongWordValue}");
        };
        @interface.Start();

        while (true)
        {
            Thread.Sleep(5000);

            //속성 설정을 이용한 값 전송
            obj.BitValue = !obj.BitValue;
            obj.ByteValue++;
            obj.WordValue++;
            obj.DoubleWordValue++;
            obj.LongWordValue++;

            //수동으로 인터페이스 핸들러를 이용하여 값 전송
            //handlers[nameof(obj.BitValue)].Send(!obj.BitValue);
            //handlers[nameof(obj.ByteValue)].Send(obj.ByteValue + 1);
            //handlers[nameof(obj.WordValue)].Send(obj.WordValue + 1);
            //handlers[nameof(obj.DoubleWordValue)].Send(obj.DoubleWordValue + 1);
            //handlers[nameof(obj.LongWordValue)].Send(obj.LongWordValue + 1);
        }
    }
}