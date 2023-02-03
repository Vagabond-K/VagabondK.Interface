using System;
using System.ComponentModel;
using System.Reflection;
using VagabondK.Interface.Modbus;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Serialization;

namespace SimpleModbusSlaveSample
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var channel = new TcpChannelProvider(502)
            {
                //Logger = new ConsoleChannelLogger()
            };
            var slaveService = new ModbusSlaveService(channel) { Serializer = new ModbusTcpSerializer() };
            var slave = new ModbusSlaveInterface(slaveService);
            var target = new LocalObject();

            foreach (var handler in slave.SetBindings(target).Values)
                handler.Point.SendLocalValue();

            channel.Start();

            while (true)
            {
                //target.DI1 = !target.DI1;
                //target.Byte1 += 1;
                //target.Byte2 += 2;
                target.BitFlags += 1;
                target.Text1 = $"test{DateTime.Now.Second}";
                //var array = slaveService[1].HoldingRegisters.GetRawData(410, 2).ToArray();
                //Console.WriteLine($"{array[0]}, {array[1]}");
                target.DateTime1 = DateTime.Now;

                System.Threading.Thread.Sleep(1000);
            }

            Console.ReadKey();
        }
    }

    [Modbus(1)]
    class LocalObject : NotifyPropertyChangeObject
    {
        [InputRegister(200, DateTimeFormat = DateTimeFormat.Bytes, DateTimeFormatString = "yyMdHmff")]
        public DateTime DateTime1 { get => Get<DateTime>(); set => Set(value); }

        [DiscreteInput(10)]
        public bool DI1 { get => Get(true); set => Set(value); }
        [DiscreteInput(11)]
        public bool DI2 { get => Get(true); set => Set(value); }
        [DiscreteInput(10)]
        public bool DI3 { get => Get(true); set => Set(value); }

        [Coil(10)]
        public bool Coil1 { get => Get(false); set => Set(value); }
        [Coil(11)]
        public bool Coil2 { get => Get(false); set => Set(value); }
        [Coil(12)]
        public bool Coil3 { get => Get(false); set => Set(value); }

        [InputRegister(100)]
        public float Single1 { get => Get(123.4f); set => Set(value); }
        [InputRegister(102)]
        public float Single2 { get => Get(456.7f); set => Set(value); }

        [HoldingRegister(400)]
        public float Single3 { get => Get(0f); set => Set(value); }

        [HoldingRegister(410, SkipFirstByte = true)]
        public byte Byte1 { get => Get<byte>(0); set => Set(value); }
        [HoldingRegister(410)]
        public byte Byte2 { get => Get<byte>(0); set => Set(value); }

        [HoldingRegister(411)]
        public ushort BitFlags { get => Get<ushort>(0); set => Set(value); }

        [HoldingRegister(411, BitFlagIndex = 0)]
        public bool Bit1 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 1)]
        public bool Bit2 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 2)]
        public bool Bit3 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 3)]
        public bool Bit4 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 4)]
        public bool Bit5 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 5)]
        public bool Bit6 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 6)]
        public bool Bit7 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 7)]
        public bool Bit8 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 8)]
        public bool Bit9 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 9)]
        public bool Bit10 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 10)]
        public bool Bit11 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 11)]
        public bool Bit12 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 12)]
        public bool Bit13 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 13)]
        public bool Bit14 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 14)]
        public bool Bit15 { get => Get(false); set => Set(value); }
        [HoldingRegister(411, BitFlagIndex = 15)]
        public bool Bit16 { get => Get(false); set => Set(value); }

        [HoldingRegister(600, BytesLength = 10)]
        public string Text1 { get => Get("test"); set => Set(value); }


        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            Console.WriteLine($"{e.PropertyName}: {GetType().GetProperty(e.PropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(this)}");
        }
    }
}
