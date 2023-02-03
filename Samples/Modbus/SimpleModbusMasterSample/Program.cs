﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using VagabondK.Interface;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Serialization;

namespace SimpleModbusMasterSample
{
    internal class Program
    {
        static void Main()
        {
            var localObject = new LocalObject();

            var client = new ModbusMaster(new TcpChannel("127.0.0.1", 502)
            {
                Logger = new ConsoleChannelLogger(),
            }, new ModbusTcpSerializer())
            {
                Timeout = 3000
            };
            
            var modbus = new ModbusMasterInterface(client)
            {
                //new BitPoint(1, true, 0)
                //{
                //    new InterfaceHandler<float>()
                //}
            };

            modbus.SetBindings(localObject);



            modbus.Start();


            foreach (var point in modbus)
                foreach (var binding in point)
                {
                    binding.ErrorOccurred += Binding_ExceptionOccurred;
                    if (binding is InterfaceBinding<float> singleBinding
                        && singleBinding.MemberName == nameof(localObject.Single3))
                        singleBinding.SendAsyncAndUpdateMember(23.45f).Wait();
                }

            //localObject.Single3 = 123.567f;

            Task.Run(() =>
            {
                while (true)
                {
                    //localObject.Coil1 = !localObject.Coil1;
                    localObject.Single3 = localObject.Single3 + 1;
                    System.Threading.Thread.Sleep(1000);
                }
            });
            Console.ReadKey();
            modbus.Stop();

            //modbus.Unbind(binding1);
        }

        private static void Binding1_Received(InterfacePoint sender, bool value, DateTime? timeStamp)
        {

        }

        private static void Binding_ExceptionOccurred(object sender, ErrorOccurredEventArgs e)
        {
            Console.WriteLine($"{e.Exception}");
        }

    }

    [Modbus(0)]
    class LocalObjectBase : NotifyPropertyChangeObject
    {
        [InputRegister(200, DateTimeFormat = DateTimeFormat.Bytes, DateTimeFormatString = "yyMdHmff")]
        public DateTime DateTime1 { get => Get<DateTime>(); set => Set(value); }

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
    }

    [Modbus(1)]
    class LocalObject : LocalObjectBase
    {
        //[ModbusCoil(0)]
        //public bool Coil1 { get => Get(false); set => Set(value); }

        // [Modbus(ModbusObjectType.Coil, 0)]
        private bool Coil2 { get => Get(false); set => Set(value); }
        private bool Coil3 { get; }

        [InputRegister(100)]
        public float Single1 { get => Get(0f); set => Set(value); }
        [InputRegister(102)]
        public float Single2 { get => Get(0f); set => Set(value); }

        [HoldingRegister(400)]
        public float Single3 { get => Get(0f); set => Set(value); }

        [HoldingRegister(410)]
        public byte Byte1 { get => Get<byte>(0); set => Set(value); }
        [HoldingRegister(410, SkipFirstByte = true)]
        public byte Byte2 { get => Get<byte>(0); set => Set(value); }

        [HoldingRegister(600, BytesLength = 10)]
        public string Text1 { get => Get("test"); set => Set(value); }

        public byte Value1 { get => Get((byte)0); private set => Set(value); }
        //public LocalObject SubObject { get => Get<LocalObject>(); private set => Set(value); }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            //Debug.WriteLine(c2);
            base.OnPropertyChanged(e);
            Debug.WriteLine($"{e.PropertyName}: {GetType().GetProperty(e.PropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(this)}");
        }
    }

    [Modbus(1)]
    class LocalObject2 : INotifyPropertyChanged
    {
        public LocalObject2()
        {
            values = new PropertyValues(this);
            values.PropertyChanged += OnPropertyChanged;
        }

        private bool c2 = false;
        private readonly PropertyValues values;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add => values.PropertyChanged += value;
            remove => values.PropertyChanged -= value;
        }

        public bool Coil1 { get => values.Get(false); set => values.Set(value); }

        //[Modbus(ModbusObjectType.Coil, 0)]
        private bool Coil2 { get => values.Get(false); set => values.Set(value); }
        private bool Coil3 { get; }

        //[Modbus(ModbusObjectType.InputRegister, 100)]
        public float Single1 { get => values.Get(0f); set => values.Set(value); }
        //[Modbus(ModbusObjectType.InputRegister, 102)]
        public float Single2 { get => values.Get(0f); set => values.Set(value); }

        [HoldingRegister(400)]
        public float Single3 { get => values.Get(0f); set => values.Set(value); }

        public byte Value1 { get => values.Get((byte)0); private set => values.Set(value); }
        public string Text1 { get => values.Get<string>(); private set => values.Set(value); }
        //public LocalObject SubObject { get => Get<LocalObject>(); private set => Set(value); }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine($"{e.PropertyName}: {GetType().GetProperty(e.PropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(this)}");
        }
    }

    [Modbus(1)]
    class LocalObject3 : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public LocalObject3()
        {
            values = new PropertyValues(this);
            values.PropertyChanged += OnPropertyChanged;
        }

        private bool c2 = false;
        private readonly PropertyValues values;

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => values.PropertyChanged += value;
            remove => values.PropertyChanged -= value;
        }

        public bool Coil1 { get => values.Get(false); set => values.Set(value); }

        //[Modbus(ModbusObjectType.Coil, 0)]
        private bool Coil2 { get => values.Get(false); set => values.Set(value); }
        private bool Coil3 { get; }

        //[Modbus(ModbusObjectType.InputRegister, 100)]
        public float Single1 { get => values.Get(0f); set => values.Set(value); }
        //[Modbus(ModbusObjectType.InputRegister, 102)]
        public float Single2 { get => values.Get(0f); set => values.Set(value); }

        [HoldingRegister(400)]
        public float Single3
        {
            get => values.Get(0f); set
            {
                PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(nameof(Single3)));
                values.Set(value);
            }
        }

        public byte Value1 { get => values.Get((byte)0); private set => values.Set(value); }
        public string Text1 { get => values.Get<string>(); private set => values.Set(value); }
        //public LocalObject SubObject { get => Get<LocalObject>(); private set => Set(value); }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Debug.WriteLine($"{e.PropertyName}: {GetType().GetProperty(e.PropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).GetValue(this)}");
        }
    }
}
