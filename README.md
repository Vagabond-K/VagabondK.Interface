# Vagabond K Interface Library [![License](https://img.shields.io/badge/license-LGPL--2.1-blue.svg)](https://licenses.nuget.org/LGPL-2.1-only)  
Modbus, LS ELECTRIC(구 LS산전)의 Cnet, FEnet 등의 프로토콜을 이용하여 객체의 속성 및 필드를 통한 인터페이스를 간단하게 구현할 수 있도록 지원하는 기능들을 구현했습니다.

- [Documentation](https://vagabond-k.github.io/docs/api/VagabondK.Interface.html)

[!["Buy me a soju"](https://vagabond-k.github.io/Images/buymeasoju131x36.png)](https://www.buymeacoffee.com/VagabondK) 

# VagabondK.Interface.Abstractions [![NuGet](https://img.shields.io/nuget/v/VagabondK.Interface.Abstractions.svg)](https://www.nuget.org/packages/VagabondK.Interface.Abstractions/)   
통신 프로토콜 기반 인터페이스 기능을 구현하기 위한 추상 형식들과, 기본적인 인터페이스 처리기와 바인딩 클래스를 제공합니다.

# VagabondK.Interface.Modbus [![NuGet](https://img.shields.io/nuget/v/VagabondK.Interface.Modbus.svg)](https://www.nuget.org/packages/VagabondK.Interface.Modbus/)   
본 라이브러리를 이용하면 Modbus 프로토콜을 이용하여 객체의 속성 및 필드를 이용한 인터페이스를 구현할 수 있습니다.   
자세한 사용법은 블로그 링크를 참고하시기 바랍니다.   
- [[블로그 링크] 객체 매핑을 이용한 Modbus 프로토콜 인터페이스 라이브러리](https://blog.naver.com/vagabond-k/223005811950)   

#### Modbus Master 기반 데이터 읽어오기 예시
```csharp
using System;
using VagabondK.Interface.Modbus;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.Logging;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Serialization;

class Program
{
    [Modbus(1)] //슬레이브 주소: 1
    class InterfaceObject
    {
        [DiscreteInput(100)]
        public bool BitValue1 { get; private set; }
        [DiscreteInput(101)]
        public bool BitValue2 { get; private set; }

        [InputRegister(200, BitFlagIndex = 0)]
        public bool BitFlagValue1 { get; private set; }
        [InputRegister(200, BitFlagIndex = 1)]
        public bool BitFlagValue2 { get; private set; }

        [InputRegister(300)] //100번지의 Word에서 Big 엔디안으로 첫 번째 byte값을 사용
        public byte ByteValue1 { get; private set; }
        [InputRegister(300, SkipFirstByte = true)] //100번지의 Word에서 Big 엔디안으로 두 번째 byte값을 사용
        public byte ByteValue2 { get; private set; }
        [InputRegister(301)]
        public short Int16Value { get; private set; }
        [InputRegister(302)]
        public int Int32Value { get; private set; }
        [InputRegister(304)]
        public long Int64Value { get; private set; }
        [InputRegister(308)]
        public float SingleValue { get; private set; }
        [InputRegister(310)]
        public double DoubleValue { get; private set; }
    }

    static void Main()
    {
        var channel = new TcpChannel("127.0.0.1", 502)
        {
            Logger = new ConsoleChannelLogger()
        };
        
        var @interface = new ModbusMasterInterface(new ModbusMaster(channel, new ModbusTcpSerializer()));

        var obj = new InterfaceObject();
        @interface.SetBindings(obj);
        @interface.PollingCompleted += (s, e) =>
        {
            Console.WriteLine($"BitValue1: {obj.BitValue1}");
            Console.WriteLine($"BitValue2: {obj.BitValue2}");
            Console.WriteLine($"BitFlagValue1: {obj.BitFlagValue1}");
            Console.WriteLine($"BitFlagValue2: {obj.BitFlagValue2}");
            Console.WriteLine($"ByteValue1: {obj.ByteValue1}");
            Console.WriteLine($"ByteValue2: {obj.ByteValue2}");
            Console.WriteLine($"Int16Value: {obj.Int16Value}");
            Console.WriteLine($"Int32Value: {obj.Int32Value}");
            Console.WriteLine($"Int64Value: {obj.Int64Value}");
            Console.WriteLine($"SingleValue: {obj.SingleValue}");
            Console.WriteLine($"DoubleValue: {obj.DoubleValue}");
        };
        @interface.Start();
        Console.ReadKey();
    }
}
```

#### 속성 변경 감지 및 Modbus Master를 통한 자동 데이터 쓰기 예시
```csharp
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
    class InterfaceObject : INotifyPropertyChanged
    {
        private bool setBitValue;
        private float setSingleValue;

        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T target, in T value, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(target, value))
            {
                target = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [Coil(400)]
        public bool SetBitValue { get => setBitValue; set => Set(ref setBitValue, value); }
        [HoldingRegister(500)]
        public float SetSingleValue { get => setSingleValue; set => Set(ref setSingleValue, value); }
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
            Console.WriteLine($"SetBitValue: {obj.SetBitValue}");
            Console.WriteLine($"SetSingleValue: {obj.SetSingleValue}");
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
```

# VagabondK.Interface.LSElectric [![NuGet](https://img.shields.io/nuget/v/VagabondK.Interface.LSElectric.svg)](https://www.nuget.org/packages/VagabondK.Interface.LSElectric/)   

본 라이브러리를 이용하면 LS ELECTRIC(구 LS산전)의 Cnet, FEnet 프로토콜을 이용하여 객체의 속성 및 필드를 이용한 PLC 인터페이스를 구현할 수 있습니다.   
자세한 사용법은 블로그 링크를 참고하시기 바랍니다.   
- [[블로그 링크] 객체 매핑을 이용한 LS산전 프로토콜 인터페이스 라이브러리](https://blog.naver.com/vagabond-k/223005956653)   

#### Cnet 프로토콜 기반 LS ELECTRIC(구 LS산전) PLC와의 인터페이스 예시
```csharp
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
```
