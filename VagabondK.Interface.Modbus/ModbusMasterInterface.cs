using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.Modbus.Abstractions;
using VagabondK.Protocols;
using VagabondK.Protocols.Modbus;
using VagabondK.Protocols.Modbus.Data;
using VagabondK.Protocols.Modbus.Serialization;

namespace VagabondK.Interface.Modbus
{
    /// <summary>
    /// Modbus 마스터 기반 인터페이스
    /// </summary>
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

        /// <summary>
        /// 1주기의 값 읽기 요청과 응답이 완료되었을 때 발생하는 이벤트
        /// </summary>
        public override event PollingCompletedEventHandler PollingCompleted;

        /// <summary>
        /// 인터페이스 포인트가 추가되었을 경우 호출됨
        /// </summary>
        /// <param name="point">추가된 인터페이스 포인트</param>
        protected override void OnAdded(ModbusPoint point)
        {
            if (point == null) return;
            point.Initialize();
            point.PropertyChanged += OnPointPropertyChanged;
            lock (settingChangedLock) isSettingChanged = true;
        }

        /// <summary>
        /// 인터페이스 포인트가 제거되었을 경우 호출됨
        /// </summary>
        /// <param name="point">제거된 인터페이스 포인트</param>
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

        /// <summary>
        /// 값 읽기 요청들을 일괄 생성할 필요가 있을 때 호출되는 메서드
        /// </summary>
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

        /// <summary>
        /// 값 읽기 요청 수행 메서드
        /// </summary>
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

            void runRequest(MergedReadRequest request)
            {
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

            if (PollingParallelRequests && Master.Serializer is ModbusTcpSerializer)
                readRequests.AsParallel().ForAll(request => runRequest(request));
            else
                foreach (var request in readRequests)
                {
                    runRequest(request);
                    if (DelayBetweenPollingRequests > 0)
                        Thread.Sleep(DelayBetweenPollingRequests);
                }

            PollingCompleted?.Invoke(this, new PollingCompletedEventArgs(succeed, pollingExceptions.Count > 0 ? new AggregateException(pollingExceptions) : null));
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="master">Modbus 마스터</param>
        public ModbusMasterInterface(ModbusMaster master) : this(master, null) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="master">Modbus 마스터</param>
        /// <param name="pollingTimeSpan">값 읽기 요청 주기. 기본값은 500 밀리초.</param>
        public ModbusMasterInterface(ModbusMaster master, int pollingTimeSpan) : this(master, pollingTimeSpan, null) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="master">Modbus 마스터</param>
        /// <param name="points">인터페이스 포인트 열거</param>
        public ModbusMasterInterface(ModbusMaster master, IEnumerable<ModbusPoint> points) : this(master, 500, points) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="master">Modbus 마스터</param>
        /// <param name="pollingTimeSpan">값 읽기 요청 주기. 기본값은 500 밀리초.</param>
        /// <param name="points">인터페이스 포인트 열거</param>
        public ModbusMasterInterface(ModbusMaster master, int pollingTimeSpan, IEnumerable<ModbusPoint> points) : base(pollingTimeSpan, points)
        {
            Master = master ?? throw new ArgumentNullException(nameof(master));
        }

        /// <summary>
        /// Modbus 마스터
        /// </summary>
        public ModbusMaster Master { get; }

        /// <summary>
        /// 자동 요청 병합 여부, true이면 근접한 데이터 주소를 하나의 요청으로 병합.
        /// </summary>
        public bool AutoRequestMerge { get; set; } = true;
        /// <summary>
        /// 요청 병합 간격, 해당 간격 이하의 인터페이스 포인트는 하나의 요청으로 병합
        /// </summary>
        public int RequestMergeSpan { get; set; }
        /// <summary>
        /// 요청과 요청 사이의 지연시간, 밀리초 단위.
        /// </summary>
        public int DelayBetweenPollingRequests { get; set; }
        /// <summary>
        /// 요청을 병렬로 수행할 지 여부. 병렬 요청은 ModbusMaster의 Serializer가 ModbusTcpSerializer일 경우에만 사용 가능.
        /// </summary>
        public bool PollingParallelRequests { get; set; }

        /// <summary>
        /// 인터페이스 바인딩 일괄 설정, InterfaceAttribute을 상속받은 특성을 이용하여 일괄 바인딩 설정 가능.
        /// </summary>
        /// <param name="targetRoot">최상위 바인딩 타겟 객체</param>
        /// <param name="slaveAddress">슬레이브 주소</param>
        /// <returns>인터페이스 처리기 사전. 키는 바인딩 경로 문자열이며 InterfaceBinding 형식의 인터페이스 처리기를 찾아볼 수 있음.</returns>
        public Dictionary<string, InterfaceHandler> SetBindings(object targetRoot, byte slaveAddress)
            => SetBindings(targetRoot, point => { point.SlaveAddress = slaveAddress; });

        /// <summary>
        /// 관리되지 않는 리소스의 확보, 해제 또는 다시 설정과 관련된 애플리케이션 정의 작업을 수행합니다.
        /// </summary>
        /// <param name="disposing">Dispose 메서드 수행 중인지 여부</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Master?.Dispose();
        }
    }
}
