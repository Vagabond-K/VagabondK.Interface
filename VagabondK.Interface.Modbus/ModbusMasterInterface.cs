using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Data;

namespace VagabondK.Interface.Modbus
{
    public class ModbusMasterInterface : PollingInterface<ModbusPoint>, IWaitSendingInterface
    {
        class MergedReadRequest : ModbusReadRequest
        {
            public MergedReadRequest(byte slaveAddress, ModbusObjectType objectType, ushort address, ushort length, IEnumerable<ModbusPoint> points)
                : base(slaveAddress, objectType, address, length)
            {
                Points = points.ToArray();
            }
            public ModbusPoint[] Points { get; }
        }

        private const ushort maxBitLength = 2008;
        private const ushort maxWordLength = 123;
        private bool isSettingChanged = true;
        private readonly object settingChangedLock = new object();
        private readonly List<MergedReadRequest> readRequests = new List<MergedReadRequest>();
        private readonly List<Exception> pollingExceptions = new List<Exception>();
        private readonly Dictionary<byte, ModbusWords> slaveWords = new Dictionary<byte, ModbusWords>();

        public override event PollingCompletedEventHandler PollingCompleted;

        protected override void OnAdded(ModbusPoint point)
        {
            if (point == null) return;
            point.Initialize();
            point.PropertyChanged += OnPointPropertyChanged;
            lock (settingChangedLock) isSettingChanged = true;
        }

        protected override void OnRemoved(ModbusPoint point)
        {
            if (point == null) return;
            point.Initialize();
            point.PropertyChanged -= OnPointPropertyChanged;
            lock (settingChangedLock) isSettingChanged = true;
        }

        private void OnPointPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            lock (settingChangedLock) isSettingChanged = true;
        }

        protected override void OnCreatePollingRequests()
        {
            readRequests.Clear();
            if (AutoRequestMerge)
            {
                var groups = this.Where(point => point.ActualRequestLength > 0)
                    .GroupBy(point => new
                    {
                        point.SlaveAddress,
                        point.ObjectType,
                        Address = point.ActualRequestAddress,
                        Length = point.ActualRequestLength
                    }).GroupBy(g => new { g.Key.SlaveAddress, g.Key.ObjectType });

                var points = new List<ModbusPoint>();
                foreach (var group in groups)
                {
                    ushort maxLength = group.Key.ObjectType == ModbusObjectType.Coil || group.Key.ObjectType == ModbusObjectType.DiscreteInput ? maxBitLength : maxWordLength;
                    ushort? address = null;
                    ushort length = 0;
                    foreach (var request in group.OrderBy(g => g.Key.Address))
                    {
                        var requestInfo = request.Key;
                        if (address == null)
                        {
                            address = requestInfo.Address;
                            length = requestInfo.Length;
                            points.Clear();
                            points.AddRange(request);
                        }
                        else
                        {
                            var newLength = (ushort)(requestInfo.Address + requestInfo.Length - address.Value);
                            if (length > maxLength
                            || address.Value + length + RequestMergeSpan < requestInfo.Address
                            || newLength > maxLength)
                            {
                                readRequests.Add(new MergedReadRequest(requestInfo.SlaveAddress, requestInfo.ObjectType, address.Value, length, points));
                                address = requestInfo.Address;
                                length = requestInfo.Length;
                                points.Clear();
                                points.AddRange(request);
                            }
                            else
                            {
                                length = newLength;
                                points.AddRange(request);
                            }
                        }
                    }
                    readRequests.Add(new MergedReadRequest(group.Key.SlaveAddress, group.Key.ObjectType, address.Value, length, points));
                }
            }
            else
            {
                readRequests.AddRange(this.Where(point => point.ActualRequestLength > 0)
                    .GroupBy(point => new
                    {
                        point.SlaveAddress,
                        point.ObjectType,
                        Address = point.ActualRequestAddress,
                        Length = point.ActualRequestLength
                    }).OrderBy(g => g.Key.Address).Select(g => new MergedReadRequest(g.Key.SlaveAddress, g.Key.ObjectType, g.Key.Address, g.Key.Length, g)));
            }
        }

        protected override void OnPoll()
        {
            lock (settingChangedLock)
            {
                if (isSettingChanged)
                {
                    OnCreatePollingRequests();
                    isSettingChanged = false;
                }
            }

            bool succeed = false;
            pollingExceptions.Clear();
            foreach (var request in readRequests)
            {
                if (DelayBetweenRequests > 0 && readRequests[0] != request)
                    Thread.Sleep(DelayBetweenRequests);

                request.TransactionID = null;
                
                try
                {
                    var response = Master.Request(request);
                    DateTime? timeStamp = DateTime.Now;

                    if (response is ModbusReadResponse)
                    {
                        switch (request.ObjectType)
                        {
                            case ModbusObjectType.Coil:
                            case ModbusObjectType.DiscreteInput:
                                if (response is ModbusReadBitResponse bitResponse)
                                {
                                    var values = bitResponse.Values;
                                    foreach (var point in request.Points)
                                        if (point is BitPoint bitPoint)
                                        {
                                            var index = point.Address - request.Address;
                                            var value = values[index];
                                            bitPoint.SetReceivedValue(value, timeStamp);
                                        }
                                }
                                break;
                            case ModbusObjectType.HoldingRegister:
                            case ModbusObjectType.InputRegister:
                                if (response is ModbusReadWordResponse wordResponse)
                                {
                                    if (!slaveWords.TryGetValue(request.SlaveAddress, out var words))
                                    {
                                        words = new ModbusWords();
                                        slaveWords[request.SlaveAddress] = words;
                                    }

                                    words.Allocate(request.Address, (byte[])wordResponse.Bytes);
                                    foreach (var point in request.Points)
                                        if (point is IModbusWordPoint wordPoint)
                                            wordPoint.SetReceivedValue(words, timeStamp);
                                }
                                break;
                        }
                    }
                    else if (response is ModbusExceptionResponse exceptionResponse)
                    {
                        throw new ModbusException(exceptionResponse.ExceptionCode);
                    }
                    succeed = true;
                }
                catch (Exception ex)
                {
                    succeed = false;
                    pollingExceptions.Add(ex);
                    foreach (var point in request.Points)
                        point.RaiseErrorOccurred(ex, ErrorDirection.Receiving);
                }
            }

            PollingCompleted?.Invoke(this, new PollingCompletedEventArgs(succeed, pollingExceptions.Count > 0 ? new AggregateException(pollingExceptions) : null));
        }

        public ModbusMasterInterface(ModbusMaster master) : this(master, null) { }
        public ModbusMasterInterface(ModbusMaster master, int pollingTimeSpan) : this(master, pollingTimeSpan, null) { }
        public ModbusMasterInterface(ModbusMaster master, IEnumerable<ModbusPoint> points) : this(master, 500, points) { }
        public ModbusMasterInterface(ModbusMaster master, int pollingTimeSpan, IEnumerable<ModbusPoint> points) : base(pollingTimeSpan, points)
        {
            Master = master ?? throw new ArgumentNullException(nameof(master));
        }

        public ModbusMaster Master { get; }

        public bool AutoRequestMerge { get; set; } = true;
        public int RequestMergeSpan { get; set; }
        public int DelayBetweenRequests { get; set; }

        public IEnumerable<InterfaceHandler> SetBindings(object target, byte slaveAddress)
            => SetBindings(target, point => { point.SlaveAddress = slaveAddress; });
    }
}
