﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Data;

namespace VagabondK.Interface.Modbus
{
    public class ModbusSlaveInterface : Interface<ModbusPoint>
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

        public ModbusSlaveInterface(ModbusSlaveService service) : this(service, null) { }
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

        protected override void OnAdded(ModbusPoint point)
        {
            if (point == null) return;
            point.Initialize();
            SetPoint(point);
            point.PropertyChanging += OnPointPropertyChanging;
            point.PropertyChanged += OnPointPropertyChanged;
        }

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

                if (points != null)
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
                if (points != null)
                    foreach (var refPoint in points)
                        (refPoint as ModbusPoint<TValue>)?.SetReceivedValue(value, timeStamp);
            }

            return result;
        }

        public IEnumerable<InterfaceHandler> SetBindings(object target, byte slaveAddress)
            => SetBindings(target, point => { point.SlaveAddress = slaveAddress; });
    }
}
