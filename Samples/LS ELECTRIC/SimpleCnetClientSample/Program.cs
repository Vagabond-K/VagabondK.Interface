using System;
using System.Threading.Tasks;
using VagabondK.Interface.LSElectric;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.LSElectric;
using VagabondK.Protocols.LSElectric.Cnet;

namespace SimpleCnetClientSample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var localObject = new LocalObject();

            var client = new CnetClient(new TcpChannel("127.0.0.1", 1234)
            {
                Logger = new ConsoleChannelLogger()
            });

            var cnet = new CnetInterface(client, 1);

            cnet.SetBindings(localObject);

            cnet.PollingCompleted += (s, e) =>
            {
                //localObject.Bit1
            };

            cnet.Start();

            Task.Run(() =>
            {
                while (true)
                {
                    localObject.Bit1 = !localObject.Bit1;
                    System.Threading.Thread.Sleep(5000);
                }
            });
            Console.ReadKey();

            cnet.Stop();
        }
    }

    [LSElectricPLC]
    class LocalObject : NotifyPropertyChangeObject
    {
        [PlcPoint("%MX001")]
        public bool Bit1 { get => Get(false); set => Set(value); }
    }
}