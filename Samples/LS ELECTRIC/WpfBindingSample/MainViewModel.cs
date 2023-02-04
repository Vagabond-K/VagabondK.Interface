using System;
using System.Collections.Generic;
using System.IO.Ports;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.LSElectric;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.LSElectric.Cnet;
//using VagabondK.Protocols.LSElectric.FEnet;

namespace WpfBindingSample
{
    class MainViewModel
    {
        public MainViewModel()
        {
            var channel = new SerialPortChannel("COM5", 9600, 8, StopBits.One, Parity.None, Handshake.None); //Cnet을 위한 시리얼 포트 채널
            var @interface = new CnetInterface(new CnetClient(channel), 1); //Cnet 인터페이스

            //var channel = new TcpChannel("127.0.0.1", 2004); //FEnet을 위한 TCP 채널
            //var @interface = new FEnetInterface(new FEnetClient(channel)); //FEnet 인터페이스

            InterfaceObject = new InterfaceObject();
            InterfaceHandlers = @interface.SetBindings(InterfaceObject);
            @interface.Start();
        }

        public InterfaceObject InterfaceObject { get; }
        public Dictionary<string, InterfaceHandler> InterfaceHandlers { get; }
    }

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
}
