using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Data;

namespace VagabondK.Interface.Modbus
{
    /// <summary>
    /// Modbus 슬레이브 기반 인터페이스
    /// </summary>
    public class ModbusSlaveInterface : Interface<ModbusPoint>, IDisposable
    {
        class AddressMap
        {
            public Dictionary<ushort, List<BitPoint>> coils = new Dictionary<ushort, List<BitPoint>>();
            public Dictionary<ushort, List<BitPoint>> discreteInput = new Dictionary<ushort, List<BitPoint>>();
            public Dictionary<ushort, List<IModbusWordPoint>> holdingRegisters = new Dictionary<ushort, List<IModbusWordPoint>>();
            public Dictionary<ushort, List<IModbusWordPoint>> inputRegisters = new Dictionary<ushort, List<IModbusWordPoint>>();
        }

        private readonly Dictionary<ushort, AddressMap> addressMaps = new Dictionary<ushort, AddressMap>();
        private readonly Dictionary<ushort, List<ModbusPoint>> slaveMaps = new Dictionary<ushort, List<ModbusPoint>>();
        private readonly object initializingValuesLock = new object();
        private bool initializingValues;
        private bool disposedValue;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="service">Modbus 슬레이브 서비스</param>
        public ModbusSlaveInterface(ModbusSlaveService service) : this(service, null) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="service">Modbus 슬레이브 서비스</param>
        /// <param name="points">인터페이스 포인트 열거</param>
        public ModbusSlaveInterface(ModbusSlaveService service, IEnumerable<ModbusPoint> points) : base(points)
        {
            Service = service;

            service.RequestedWriteCoil += OnRequestedWriteCoil;
            service.RequestedWriteHoldingRegister += OnRequestedWriteHoldingRegister;
        }

        private void SetPoint(ModbusPoint point)
        {
            lock (addressMaps)
            {
                ModbusSlave slave;
                var slaveAddress = point.SlaveAddress;

                lock (slaveMaps)
                {
                    if (!Service.TryGetModbusSlave(slaveAddress, out slave))
                        Service[slaveAddress] = slave = new ModbusSlave();
                    if (!slaveMaps.TryGetValue(slaveAddress, out var modbusPoints))
                        slaveMaps[slaveAddress] = modbusPoints = new List<ModbusPoint>();
                    modbusPoints.Add(point);
                }


                if (!addressMaps.TryGetValue(slaveAddress, out var addressMap))
                    addressMaps[slaveAddress] = addressMap = new AddressMap();

                if (point is IModbusWordPoint wordPoint)
                {
                    List<IModbusWordPoint> points;
                    if (point.Writable)
                    {
                        wordPoint.SetWords(slave.HoldingRegisters);
                        if (!addressMap.holdingRegisters.TryGetValue(point.Address, out points))
                            addressMap.holdingRegisters[point.Address] = points = new List<IModbusWordPoint>();
                    }
                    else
                    {
                        wordPoint.SetWords(slave.InputRegisters);
                        if (!addressMap.inputRegisters.TryGetValue(point.Address, out points))
                            addressMap.inputRegisters[point.Address] = points = new List<IModbusWordPoint>();
                    }
                    points.Add(wordPoint);
                }
                else if (point is BitPoint bitPoint)
                {
                    List<BitPoint> points;

                    if (point.Writable)
                    {
                        if (!addressMap.coils.TryGetValue(point.Address, out points))
                            addressMap.coils[point.Address] = points = new List<BitPoint>();
                    }
                    else
                    {
                        if (!addressMap.discreteInput.TryGetValue(point.Address, out points))
                            addressMap.discreteInput[point.Address] = points = new List<BitPoint>();
                    }
                    points.Add(bitPoint);
                }
            }
        }
        private void ClearPoint(ModbusPoint point)
        {
            lock (addressMaps)
            {
                var slaveAddress = point.SlaveAddress;

                lock (slaveMaps)
                {
                    if (slaveMaps.TryGetValue(slaveAddress, out var modbusPoints))
                    {
                        modbusPoints.Remove(point);
                        if (modbusPoints.Count == 0)
                        {
                            slaveMaps.Remove(slaveAddress);
                            Service.Remove(slaveAddress);
                        }
                    }
                }

                if (addressMaps.TryGetValue(slaveAddress, out var addressMap))
                {
                    if (point is IModbusWordPoint wordPoint)
                    {
                        var words = point.Writable ? addressMap.holdingRegisters : addressMap.inputRegisters;

                        if (words.TryGetValue(point.Address, out var points))
                        {
                            points.Remove(wordPoint);
                            if (points.Count == 0)
                                words.Remove(point.Address);
                            wordPoint.SetWords(null);
                        }
                    }
                    else if (point is BitPoint bitPoint)
                    {
                        var bits = point.Writable ? addressMap.coils : addressMap.discreteInput;
                        if (bits.TryGetValue(point.Address, out var points))
                        {
                            points.Remove(bitPoint);
                            if (points.Count == 0)
                                bits.Remove(point.Address);
                        }
                    }
                    if (addressMap.coils.Count == 0 && addressMap.holdingRegisters.Count == 0)
                        addressMaps.Remove(slaveAddress);
                }
            }
        }

        /// <summary>
        /// 인터페이스 포인트가 추가되었을 경우 호출됨
        /// </summary>
        /// <param name="point">추가된 인터페이스 포인트</param>
        protected override void OnAdded(ModbusPoint point)
        {
            if (point == null) return;
            point.Initialize();
            SetPoint(point);
            point.PropertyChanging += OnPointPropertyChanging;
            point.PropertyChanged += OnPointPropertyChanged;
        }

        /// <summary>
        /// 인터페이스 포인트가 제거되었을 경우 호출됨
        /// </summary>
        /// <param name="point">제거된 인터페이스 포인트</param>
        protected override void OnRemoved(ModbusPoint point)
        {
            if (point == null) return;
            point.Initialize();
            ClearPoint(point);
            point.PropertyChanging -= OnPointPropertyChanging;
            point.PropertyChanged -= OnPointPropertyChanged;
        }

        private void OnPointPropertyChanging(object sender, System.ComponentModel.PropertyChangingEventArgs e)
        {
            if (sender is ModbusPoint point)
                switch (e.PropertyName)
                {
                    case nameof(ModbusPoint.SlaveAddress):
                    case nameof(ModbusPoint.Address):
                        ClearPoint(point);
                        break;
                }
        }
        private void OnPointPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is ModbusPoint point)
                switch (e.PropertyName)
                {
                    case nameof(ModbusPoint.SlaveAddress):
                    case nameof(ModbusPoint.Address):
                        SetPoint(point);
                        break;
                }
        }

        private void OnRequestedWriteCoil(object sender, RequestedWriteCoilEventArgs e)
        {
            DateTime? timeStamp = DateTime.Now;
            BitPoint[] points = null;
            lock (addressMaps)
                if (addressMaps.TryGetValue(e.SlaveAddress, out var addressMap))
                    points = Enumerable.Range(e.Address, e.Values.Count).SelectMany(address =>
                        addressMap.coils.TryGetValue((ushort)address, out var refPoints) ? refPoints : Enumerable.Empty<BitPoint>()).ToArray();

            if (points != null)
                foreach (var point in points)
                {
                    var value = e.Values[point.Address - e.Address];
                    point.SetReceivedValue(value, timeStamp);
                }
        }

        private void OnRequestedWriteHoldingRegister(object sender, RequestedWriteHoldingRegisterEventArgs e)
        {
            DateTime? timeStamp = DateTime.Now;
            IModbusWordPoint[] points = null;
            lock (addressMaps)
                if (addressMaps.TryGetValue(e.SlaveAddress, out var addressMap))
                    points = Enumerable.Range(e.Address, e.Words.Count).SelectMany(address =>
                        addressMap.holdingRegisters.TryGetValue((ushort)address, out var refPoints) ? refPoints : Enumerable.Empty<IModbusWordPoint>()).ToArray();

            if (points != null && Service.TryGetModbusSlave(e.SlaveAddress, out var modbusSlave))
                foreach (var point in points)
                    point.SetReceivedValue(modbusSlave.HoldingRegisters, timeStamp);
        }

        /// <summary>
        /// Modbus 슬레이브 서비스
        /// </summary>
        public ModbusSlaveService Service { get; }

        internal delegate bool SendToSlaveDelegate<TValue>(ModbusSlave slave, in TValue value);

        internal bool OnSendRequested<TValue>(ModbusPoint<TValue> point, in TValue value, in DateTime? timeStamp, SendToSlaveDelegate<TValue> send)
        {
            ModbusSlave modbusSlave = null;
            var slaveAddress = point.SlaveAddress;
            var pointAddress = point.Address;
            lock (slaveMaps)
                Service.TryGetModbusSlave(slaveAddress, out modbusSlave);
            if (modbusSlave == null) return false;

            var result = send?.Invoke(modbusSlave, value) ?? false;

            if (point is IModbusWordPoint wordPoint)
            {
                ModbusWords words = null;
                IModbusWordPoint[] points = null;
                lock (addressMaps)
                    if (addressMaps.TryGetValue(slaveAddress, out var addressMap))
                    {
                        var count = wordPoint.WordsCount;
                        words = point.Writable ? modbusSlave.HoldingRegisters : modbusSlave.InputRegisters;
                        var wordMap = point.Writable ? addressMap.holdingRegisters : addressMap.inputRegisters;
                        points = Enumerable.Range(pointAddress, count).SelectMany(address =>
                            wordMap.TryGetValue((ushort)address, out var refPoints) ? refPoints.Where(refPoint => refPoint != point) : Enumerable.Empty<IModbusWordPoint>()).ToArray();
                    }

                lock (initializingValuesLock)
                    if (points != null && !initializingValues)
                        foreach (var refPoint in points)
                            refPoint.SetReceivedValue(words, timeStamp);
            }
            else if (point is BitPoint bitPoint)
            {
                BitPoint[] points = null;
                lock (addressMaps)
                    if (addressMaps.TryGetValue(slaveAddress, out var addressMap))
                    {
                        if ((point.Writable ? addressMap.coils : addressMap.discreteInput).TryGetValue(pointAddress, out var refPoints))
                            points = refPoints.Where(refPoint => refPoint != bitPoint).ToArray();
                    }
                lock (initializingValuesLock)
                    if (points != null && !initializingValues)
                        foreach (var refPoint in points)
                            (refPoint as ModbusPoint<TValue>)?.SetReceivedValue(value, timeStamp);
            }

            return result;
        }

        /// <summary>
        /// 인터페이스 바인딩 일괄 설정, InterfaceAttribute을 상속받은 특성을 이용하여 일괄 바인딩 설정 가능.
        /// </summary>
        /// <param name="targetRoot">최상위 바인딩 타겟 객체</param>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <returns>인터페이스 처리기 사전. 키는 바인딩 경로 문자열이며 InterfaceBinding 형식의 인터페이스 처리기를 찾아볼 수 있음.</returns>
        public Dictionary<string, InterfaceHandler> SetBindings(object targetRoot, byte slaveAddress)
            => SetBindings(targetRoot, point => { point.SlaveAddress = slaveAddress; });

        /// <summary>
        /// Modbus 슬레이브들의 메모리를 인터페이스 포인트 값들로 초기화
        /// </summary>
        public void InitializeSlaveValues()
        {
            lock (initializingValuesLock)
            {
                initializingValues = true;
                foreach (var point in this)
                    point.SendLocalValue();
                initializingValues = false;
            }
        }

        /// <summary>
        /// 관리되지 않는 리소스의 확보, 해제 또는 다시 설정과 관련된 애플리케이션 정의 작업을 수행합니다.
        /// </summary>
        /// <param name="disposing">Dispose 메서드 수행 중인지 여부</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Service?.Dispose();
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// 관리되지 않는 리소스의 확보, 해제 또는 다시 설정과 관련된 애플리케이션 정의 작업을 수행합니다.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
