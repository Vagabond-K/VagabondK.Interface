using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.LSElectric.Abstractions;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.LSElectric;
using VagabondK.Protocols.LSElectric.Cnet;

namespace VagabondK.Interface.LSElectric
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 기반 클라이언트 인터페이스
    /// </summary>
    public class CnetInterface : PollingInterface<PlcPoint>, IPlcInterface
    {
        class DeviceVariableReader : DeviceVariableReader<CnetInterface, CnetClient>
        {
            private readonly Lazy<CnetReadIndividualRequest> request;

            public DeviceVariableReader(CnetInterface @interface, byte stationNumber) : base(@interface)
            {
                request = new Lazy<CnetReadIndividualRequest>(() => new CnetReadIndividualRequest(stationNumber, this.Select(group => group.Key)));
            }

            protected override IReadOnlyDictionary<DeviceVariable, DeviceValue> Read()
                => @interface.CnetClient.Request(request.Value) as CnetReadResponse;
        }

        private bool isSettingChanged = true;
        private readonly object settingChangedLock = new object();
        //private readonly List<DeviceVariableReader> deviceVariableReaders = new List<DeviceVariableReader>();
        private readonly Dictionary<byte, List<DeviceVariableReader>> readersDictionary = new Dictionary<byte, List<DeviceVariableReader>>();
        private readonly List<Exception> pollingExceptions = new List<Exception>();

        /// <summary>
        /// 생성자
        /// </summary>
        public CnetInterface() : this(new CnetClient(), null) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="cnetClient">Cnet 클라이언트</param>
        public CnetInterface(CnetClient cnetClient) : this(cnetClient, null) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="cnetClient">Cnet 클라이언트</param>
        /// <param name="pollingTimeSpan">값 읽기 요청 주기. 기본값은 500 밀리초.</param>
        public CnetInterface(CnetClient cnetClient, int pollingTimeSpan) : this(cnetClient, pollingTimeSpan, null) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="cnetClient">Cnet 클라이언트</param>
        /// <param name="points">인터페이스 포인트 열거</param>
        public CnetInterface(CnetClient cnetClient, IEnumerable<PlcPoint> points) : this(cnetClient, 500, points) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="cnetClient">Cnet 클라이언트</param>
        /// <param name="pollingTimeSpan">값 읽기 요청 주기. 기본값은 500 밀리초.</param>
        /// <param name="points">인터페이스 포인트 열거</param>
        public CnetInterface(CnetClient cnetClient, int pollingTimeSpan, IEnumerable<PlcPoint> points) : base(pollingTimeSpan)
        {
            CnetClient = cnetClient;
            AddRange(points);
        }

        /// <summary>
        /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 기반 클라이언트입니다. XGT 시리즈 제품의 Cnet I/F 모듈과 통신 가능합니다.
        /// </summary>
        public CnetClient CnetClient { get; }

        /// <summary>
        /// 1주기의 값 읽기 요청과 응답이 완료되었을 때 발생하는 이벤트
        /// </summary>
        public override event PollingCompletedEventHandler PollingCompleted;

        bool IPlcInterface.Write(PlcPoint point, DeviceValue deviceValue)
        {
            var deviceVariable = point.DeviceVariable;
            if (!(point.writeRequest is CnetWriteIndividualRequest writeRequest))
                point.writeRequest = writeRequest = new CnetWriteIndividualRequest(point.StationNumber);

            if (writeRequest.Count != 1 || !writeRequest.ContainsKey(deviceVariable))
                writeRequest.Clear();

            writeRequest.StationNumber = point.StationNumber;
            writeRequest[deviceVariable] = deviceValue;

            return CnetClient.Request(writeRequest) is CnetACKResponse;
        }

        /// <summary>
        /// 인터페이스 포인트가 추가되었을 경우 호출됨
        /// </summary>
        /// <param name="point">추가된 인터페이스 포인트</param>
        protected override void OnAdded(PlcPoint point)
        {
            if (point == null) return;
            point.PropertyChanged += OnPointPropertyChanged;
            lock (settingChangedLock) isSettingChanged = true;
        }

        /// <summary>
        /// 인터페이스 포인트가 제거되었을 경우 호출됨
        /// </summary>
        /// <param name="point">제거된 인터페이스 포인트</param>
        protected override void OnRemoved(PlcPoint point)
        {
            if (point == null) return;
            point.PropertyChanged -= OnPointPropertyChanged;
            lock (settingChangedLock) isSettingChanged = true;
        }

        private void OnPointPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            lock (settingChangedLock) isSettingChanged = true;
        }

        private void CreatePollingRequests()
        {
            readersDictionary.Clear();
            foreach (var station in this.GroupBy(point => point.StationNumber))
            {
                var deviceVariableReaders = readersDictionary[station.Key] = new List<DeviceVariableReader>();
                var groups = station.GroupBy(point => point.DeviceVariable).GroupBy(g => g.Key.DataType);
                foreach (var group in groups)
                {
                    DeviceVariableReader reader = null;
                    foreach (var points in group.OrderBy(vGroup => vGroup.Key.DeviceType).ThenBy(vGroup => vGroup.Key.Index))
                    {
                        if (reader == null || reader.Count >= 16)
                        {
                            reader = new DeviceVariableReader(this, station.Key);
                            deviceVariableReaders.Add(reader);
                        }

                        reader[points.Key] = points.ToArray();
                    }
                }
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
                    CreatePollingRequests();
                    isSettingChanged = false;
                }
            }

            bool succeed = false;
            pollingExceptions.Clear();
            foreach (var reader in readersDictionary.Values.SelectMany(item => item))
            {
                try
                {
                    reader.ReadAndUpdatePoints();
                    succeed = true;
                }
                catch (Exception ex)
                {
                    succeed = false;
                    pollingExceptions.Add(ex);
                    foreach (var point in reader.SelectMany(points => points.Value))
                        point.RaiseErrorOccurred(ex, ErrorDirection.Receiving);
                }
                if (DelayBetweenPollingRequests > 0)
                    Thread.Sleep(DelayBetweenPollingRequests);
            }

            PollingCompleted?.Invoke(this, new PollingCompletedEventArgs(succeed, pollingExceptions.Count > 0 ? new AggregateException(pollingExceptions) : null));
        }

        /// <summary>
        /// 관리되지 않는 리소스의 확보, 해제 또는 다시 설정과 관련된 애플리케이션 정의 작업을 수행합니다.
        /// </summary>
        /// <param name="disposing">Dispose 메서드 수행 중인지 여부</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            CnetClient?.Dispose();
        }

        /// <summary>
        /// 요청과 요청 사이의 지연시간, 밀리초 단위.
        /// </summary>
        public int DelayBetweenPollingRequests { get; set; }

        /// <inheritdoc/>
        protected override void OnStart()
        {
            (CnetClient.Channel as ChannelProvider)?.Start();
            base.OnStart();
        }

        /// <inheritdoc/>
        protected override void OnStop()
        {
            (CnetClient.Channel as Channel)?.Close();
            (CnetClient.Channel as ChannelProvider)?.Stop();
            base.OnStop();
        }
    }
}
