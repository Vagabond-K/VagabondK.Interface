using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Threading;
using VagabondK.Interface.LSElectric;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.LSElectric.Cnet;
//using VagabondK.Protocols.LSElectric.FEnet;

class Program
{
    [LSElectricPLC(1)]
    class InterfaceObject : INotifyPropertyChanging
    {
        private bool bitValue;
        private byte byteValue;
        private short wordValue;
        private int doubleWordValue;
        private long longWordValue;

        public event PropertyChangingEventHandler PropertyChanging;

        private void Set<T>(ref T target, in T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(target, value))
            {
                var eventArgs = new QueryPropertyChangingEventArgs<T>(propertyName, value);
                PropertyChanging?.Invoke(this, eventArgs);
                if (!eventArgs.IsCanceled)
                    target = value;
            }
        }

        [PlcPoint("%MX100")]
        public bool BitValue { get => bitValue; set => Set(ref bitValue, value); }
        [PlcPoint("%MB100")]
        public byte ByteValue { get => byteValue; set => Set(ref byteValue, value); }
        [PlcPoint("%MW100")]
        public short WordValue { get => wordValue; set => Set(ref wordValue, value); }
        [PlcPoint("%MD100")]
        public int DoubleWordValue { get => doubleWordValue; set => Set(ref doubleWordValue, value); }
        [PlcPoint("%ML100")]
        public long LongWordValue { get => longWordValue; set => Set(ref longWordValue, value); }
    }

    class QueryPropertyChangingEventArgs<T> : PropertyChangingEventArgs
    {
        public QueryPropertyChangingEventArgs(string propertyName, T newValue) : base(propertyName)
        {
            NewValue = newValue;
        }

        public T NewValue { get; }
        public bool IsCanceled { get; set; }
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