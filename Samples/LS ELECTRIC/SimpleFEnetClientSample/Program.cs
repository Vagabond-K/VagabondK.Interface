using System;
using System.Threading.Tasks;
using VagabondK.Interface.LSElectric;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.LSElectric.FEnet;

namespace SimpleFEnetClientSample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var localObject = new LocalObject();

            var client = new FEnetClient(new TcpChannel("127.0.0.1", 2004)
            {
                Logger = new ConsoleChannelLogger()
            });

            var fenet = new FEnetInterface(client);

            var bindings = fenet.SetBindings(localObject);

            fenet.PollingCompleted += (s, e) =>
            {
                //localObject.Bit1
            };

            fenet.Start();

            Task.Run(() =>
            {
                while (true)
                {
                    localObject.Bit1 = !localObject.Bit1;
                    System.Threading.Thread.Sleep(5000);
                }
            });
            Console.ReadKey();

            fenet.Stop();
        }
    }

    [LSElectricPLC]
    class LocalObject : NotifyPropertyChangeObject
    {
        [PlcPoint("%MX001")]
        public bool Bit1 { get => Get(false); set => Set(value); }

        [PlcPoint("%MW000")]
        public short Word1 { get => Get((short)0); set => Set(value); }
    }
}
