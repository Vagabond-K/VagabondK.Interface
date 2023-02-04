using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using VagabondK.Interface.Modbus;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Serialization;

class Program
{
    [Modbus(1)] //슬레이브 주소: 1
    class InterfaceObject : INotifyPropertyChanging
    {
        private bool setBitValue1;
        private float setSingleValue1;

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

        [Coil(400)]
        public bool SetBitValue { get => setBitValue1; set => Set(ref setBitValue1, value); }
        [HoldingRegister(500)]
        public float SetSingleValue { get => setSingleValue1; set => Set(ref setSingleValue1, value); }
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
        var channel = new TcpChannel("127.0.0.1", 502)
        {
            Logger = new ConsoleChannelLogger()
        };

        var @interface = new ModbusMasterInterface(new ModbusMaster(channel, new ModbusTcpSerializer()));

        var obj = new InterfaceObject();
        var handlers = @interface.SetBindings(obj);
        @interface.PollingCompleted += (s, e) =>
        {
            Console.WriteLine($"SetBitValue1: {obj.SetBitValue}");
            Console.WriteLine($"SetSingleValue1: {obj.SetSingleValue}");
        };
        @interface.Start();

        while (true)
        {
            Thread.Sleep(5000);

            //속성 설정을 이용한 값 전송
            obj.SetBitValue = !obj.SetBitValue;
            obj.SetSingleValue++;

            //수동으로 인터페이스 핸들러를 이용하여 값 전송
            //handlers[nameof(obj.SetBitValue)].Send(!obj.SetBitValue);
            //handlers[nameof(obj.SetSingleValue)].Send(obj.SetSingleValue + 1);
        }
    }
}