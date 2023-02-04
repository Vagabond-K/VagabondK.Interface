using System;
using System.Collections.Generic;
using System.IO.Ports;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.LSElectric;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.LSElectric.Cnet;

namespace WpfBindingSample
{
    class MainViewModel
    {
        public MainViewModel()
        {
            var client = new CnetClient(new SerialPortChannel("COM5", 9600, 8, StopBits.One, Parity.None, Handshake.None));

            InterfaceObject = new InterfaceObject();

            var cnet = new CnetInterface(client, 1);
            InterfaceHandlers = cnet.SetBindings(InterfaceObject);
            cnet.Start();
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
