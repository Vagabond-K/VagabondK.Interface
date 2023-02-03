using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.LSElectric.Abstractions;
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

            public DeviceVariableReader(CnetInterface @interface) : base(@interface)
            {
                request = new Lazy<CnetReadIndividualRequest>(() => new CnetReadIndividualRequest(@interface.StationNumber, this.Select(group => group.Key)));
            }

            protected override IReadOnlyDictionary<DeviceVariable, DeviceValue> Read()
                => @interface.CnetClient.Request(request.Value) as CnetReadResponse;
        }

        private bool isSettingChanged = true;
        private readonly object settingChangedLock = new object();
        private readonly List<DeviceVariableReader> deviceVariableReaders = new List<DeviceVariableReader>();
        private readonly List<Exception> pollingExceptions = new List<Exception>();

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="cnetClient">Cnet 클라이언트</param>
        /// <param name="stationNumber">국번</param>
        public CnetInterface(CnetClient cnetClient, byte stationNumber) : this(cnetClient, stationNumber, null) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="cnetClient">Cnet 클라이언트</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="pollingTimeSpan">값 읽기 요청 주기. 기본값은 500 밀리초.</param>
        public CnetInterface(CnetClient cnetClient, byte stationNumber, int pollingTimeSpan) : this(cnetClient, stationNumber, pollingTimeSpan, null) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="cnetClient">Cnet 클라이언트</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="points">인터페이스 포인트 열거</param>
        public CnetInterface(CnetClient cnetClient, byte stationNumber, IEnumerable<PlcPoint> points) : this(cnetClient, stationNumber, 500, points) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="cnetClient">Cnet 클라이언트</param>
        /// <param name="stationNumber">국번</param>
        /// <param name="pollingTimeSpan">값 읽기 요청 주기. 기본값은 500 밀리초.</param>
        /// <param name="points">인터페이스 포인트 열거</param>
        public CnetInterface(CnetClient cnetClient, byte stationNumber, int pollingTimeSpan, IEnumerable<PlcPoint> points) : base(pollingTimeSpan, points)
        {
            CnetClient = cnetClient;
            StationNumber = stationNumber;
        }

        /// <summary>
        /// LS ELECTRIC(구 LS산전) Cnet 프로토콜 기반 클라이언트입니다. XGT 시리즈 제품의 Cnet I/F 모듈과 통신 가능합니다.
        /// </summary>
        public CnetClient CnetClient { get; }

        /// <summary>
        /// 국번
        /// </summary>
        public byte StationNumber { get; }

        /// <summary>
        /// 1주기의 값 읽기 요청과 응답이 완료되었을 때 발생하는 이벤트
        /// </summary>
        public override event PollingCompletedEventHandler PollingCompleted;

        bool IPlcInterface.Write(PlcPoint point, DeviceValue deviceValue)
        {
            var deviceVariable = point.DeviceVariable;
            if (!(point.writeRequest is CnetWriteIndividualRequest writeRequest))
                point.writeRequest = writeRequest = new CnetWriteIndividualRequest(StationNumber);

            if (writeRequest.Count != 1 || !writeRequest.ContainsKey(deviceVariable))
                writeRequest.Clear();

            writeRequest.StationNumber = StationNumber;
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

        /// <summary>
        /// 값 읽기 요청들을 일괄 생성할 필요가 있을 때 호출되는 메서드
        /// </summary>
        protected override void OnCreatePollingRequests()
        {
            deviceVariableReaders.Clear();
            var groups = this.GroupBy(point => point.DeviceVariable).GroupBy(g => g.Key.DataType);
            foreach (var group in groups)
            {
                DeviceVariableReader reader = null;
                foreach (var points in group.OrderBy(vGroup => vGroup.Key.DeviceType).ThenBy(vGroup => vGroup.Key.Index))
                {
                    if (reader == null || reader.Count >= 16)
                    {
                        reader = new DeviceVariableReader(this);
                        deviceVariableReaders.Add(reader);
                    }

                    reader[points.Key] = points.ToArray();
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
                    OnCreatePollingRequests();
                    isSettingChanged = false;
                }
            }

            bool succeed = false;
            pollingExceptions.Clear();
            foreach (var reader in deviceVariableReaders)
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
    }
}
