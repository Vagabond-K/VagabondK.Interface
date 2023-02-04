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
    [LSElectricPLC]
    class InterfaceObject : INotifyPropertyChanged
    {
        private bool bitValue;
        private byte byteValue;
        private short wordValue;
        private int doubleWordValue;
        private long longWordValue;

        public event PropertyChangedEventHandler PropertyChanged;
        private void Set<T>(ref T target, in T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(target, value))
            {
                target = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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

    static void Main()
    {
        var client = new CnetClient(new SerialPortChannel("COM5", 9600, 8, StopBits.One, Parity.None, Handshake.None)
        //var client = new FEnetClient(new TcpChannel("127.0.0.1", 2004)
        {
            Logger = new ConsoleChannelLogger()
        });

        var obj = new InterfaceObject();

        var @interface = new CnetInterface(client, 1);
        //var @interface = new FEnetInterface(client);
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