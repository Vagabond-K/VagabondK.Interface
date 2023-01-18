using System;
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
    public class ModbusSlaveInterface : Interface<ModbusPoint>, IModbusInterface
    {
        class AddressMap
        {
            public Dictionary<ushort, List<ModbusBooleanPoint>> coils = new Dictionary<ushort, List<ModbusBooleanPoint>>();
            public Dictionary<ushort, List<ModbusBooleanPoint>> discreteInput = new Dictionary<ushort, List<ModbusBooleanPoint>>();
            public Dictionary<ushort, List<IModbusRegisterPoint>> holdingRegisters = new Dictionary<ushort, List<IModbusRegisterPoint>>();
            public Dictionary<ushort, List<IModbusRegisterPoint>> inputRegisters = new Dictionary<ushort, List<IModbusRegisterPoint>>();
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

                if (point is IModbusRegisterPoint registerPoint)
                {
                    List<IModbusRegisterPoint> points;
                    if (point.Writable)
                    {
                        registerPoint.SetRegisters(slave.HoldingRegisters);
                        if (!addressMap.holdingRegisters.TryGetValue(point.Address, out points))
                            addressMap.holdingRegisters[point.Address] = points = new List<IModbusRegisterPoint>();
                    }
                    else
                    {
                        registerPoint.SetRegisters(slave.InputRegisters);
                        if (!addressMap.inputRegisters.TryGetValue(point.Address, out points))
                            addressMap.inputRegisters[point.Address] = points = new List<IModbusRegisterPoint>();
                    }
                    points.Add(registerPoint);
                }
                else if (point is ModbusBooleanPoint booleanPoint)
                {
                    List<ModbusBooleanPoint> points;

                    if (point.Writable)
                    {
                        if (!addressMap.coils.TryGetValue(point.Address, out points))
                            addressMap.coils[point.Address] = points = new List<ModbusBooleanPoint>();
                    }
                    else
                    {
                        if (!addressMap.discreteInput.TryGetValue(point.Address, out points))
                            addressMap.discreteInput[point.Address] = points = new List<ModbusBooleanPoint>();
                    }
                    points.Add(booleanPoint);
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
                    if (point is IModbusRegisterPoint registerPoint)
                    {
                        var registers = point.Writable ? addressMap.holdingRegisters : addressMap.inputRegisters;

                        if (registers.TryGetValue(point.Address, out var points))
                        {
                            points.Remove(registerPoint);
                            if (points.Count == 0)
                                registers.Remove(point.Address);
                            registerPoint.SetRegisters(null);
                        }
                    }
                    else if (point is ModbusBooleanPoint booleanPoint)
                    {
                        var booleans = point.Writable ? addressMap.coils : addressMap.discreteInput;
                        if (booleans.TryGetValue(point.Address, out var points))
                        {
                            points.Remove(booleanPoint);
                            if (points.Count == 0)
                                booleans.Remove(point.Address);
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
            SetPoint(point);
            point.PropertyChanging += OnPointPropertyChanging;
            point.PropertyChanged += OnPointPropertyChanged;
        }

        protected override void OnRemoved(ModbusPoint point)
        {
            if (point == null) return;
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
            ModbusBooleanPoint[] points = null;
            lock (addressMaps)
                if (addressMaps.TryGetValue(e.SlaveAddress, out var addressMap))
                    points = Enumerable.Range(e.Address, e.Values.Count).SelectMany(address =>
                        addressMap.coils.TryGetValue((ushort)address, out var refPoints) ? refPoints : Enumerable.Empty<ModbusBooleanPoint>()).ToArray();

            if (points != null)
                foreach (var point in points)
                    try
                    {
                        var value = e.Values[point.Address - e.Address];
                        SetReceivedValue(point, ref value, ref timeStamp);
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurredAt(point, ex, ErrorDirection.Receiving);
                    }
        }

        private void OnRequestedWriteHoldingRegister(object sender, RequestedWriteHoldingRegisterEventArgs e)
        {
            var timeStamp = DateTime.Now;
            IModbusRegisterPoint[] points = null;
            lock (addressMaps)
                if (addressMaps.TryGetValue(e.SlaveAddress, out var addressMap))
                    points = Enumerable.Range(e.Address, e.Registers.Count).SelectMany(address =>
                        addressMap.holdingRegisters.TryGetValue((ushort)address, out var refPoints) ? refPoints : Enumerable.Empty<IModbusRegisterPoint>()).ToArray();

            if (points != null && Service.TryGetModbusSlave(e.SlaveAddress, out var modbusSlave))
                foreach (var point in points)
                    try
                    {
                        point.SetReceivedValue(modbusSlave.HoldingRegisters, timeStamp, this);
                    }
                    catch (Exception ex)
                    {
                        ErrorOccurredAt(point as ModbusPoint, ex, ErrorDirection.Receiving);
                    }
        }

        public ModbusSlaveService Service { get; }

        protected override Task<bool> OnSendRequestedAsync<TValue>(ModbusPoint point, ref TValue value, ref DateTime? timeStamp)
            => Task.FromResult(OnSendRequested(point, ref value, ref timeStamp));
        protected override bool OnSendRequested<TValue>(ModbusPoint point, ref TValue value, ref DateTime? timeStamp)
        {
            ModbusSlave modbusSlave = null;
            var slaveAddress = point.SlaveAddress;
            var pointAddress = point.Address;
            lock (slaveMaps)
                Service.TryGetModbusSlave(slaveAddress, out modbusSlave);
            if (modbusSlave == null) return false;

            var result = (point as ModbusPoint<TValue>)?.Send(modbusSlave, value) ?? point?.Send(modbusSlave, value) ?? false;

            if (point is IModbusRegisterPoint registerPoint)
            {
                ModbusRegisters registers = null;
                IModbusRegisterPoint[] points = null;
                lock (addressMaps)
                    if (addressMaps.TryGetValue(slaveAddress, out var addressMap))
                    {
                        var count = registerPoint.RegistersCount;
                        registers = point.Writable ? modbusSlave.HoldingRegisters : modbusSlave.InputRegisters;
                        var registerMap = point.Writable ? addressMap.holdingRegisters : addressMap.inputRegisters;
                        points = Enumerable.Range(pointAddress, count).SelectMany(address =>
                            registerMap.TryGetValue((ushort)address, out var refPoints) ? refPoints.Where(refPoint => refPoint != point) : Enumerable.Empty<IModbusRegisterPoint>()).ToArray();
                    }

                if (points != null)
                    foreach (var refPoint in points)
                        refPoint.SetReceivedValue(registers, timeStamp, this);
            }
            else if (point is ModbusBooleanPoint booleanPoint)
            {
                ModbusBooleanPoint[] points = null;
                lock (addressMaps)
                    if (addressMaps.TryGetValue(slaveAddress, out var addressMap))
                    {
                        if ((point.Writable ? addressMap.coils : addressMap.discreteInput).TryGetValue(pointAddress, out var refPoints))
                            points = refPoints.Where(refPoint => refPoint != point).ToArray();
                    }
                if (points != null)
                    foreach (var refPoint in points)
                        SetReceivedValue(refPoint, ref value, ref timeStamp);
            }

            return result;
        }

        void IModbusInterface.SetReceivedValue<TValue, TValuePoint>(TValuePoint point, TValue value, DateTime? timeStamp)
            => SetReceivedValue(point, ref value, ref timeStamp);

        public IEnumerable<InterfaceHandler> SetBindings(object target, byte slaveAddress)
            => SetBindings(target, point => { point.SlaveAddress = slaveAddress; });
    }
}
