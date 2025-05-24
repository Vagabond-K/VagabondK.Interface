using System;
using System.Collections.Generic;
using System.Linq;
using VagabondK.Interface.Abstractions;
using VagabondK.Interface.LSElectric.Abstractions;
using VagabondK.Protocols.Channels;
using VagabondK.Protocols.LSElectric;
using VagabondK.Protocols.LSElectric.Cnet;
using VagabondK.Protocols.LSElectric.FEnet;

namespace VagabondK.Interface.LSElectric
{
    /// <summary>
    /// LS ELECTRIC(구 LS산전) FEnet 프로토콜 기반 클라이언트 인터페이스
    /// </summary>
    public class FEnetInterface : PollingInterface<PlcPoint>, IPlcInterface
    {
        class DeviceVariableReader : DeviceVariableReader<FEnetInterface, FEnetClient>
        {
            private readonly Lazy<FEnetReadIndividualRequest> request;

            public DeviceVariableReader(FEnetInterface @interface) : base(@interface)
            {
                request = new Lazy<FEnetReadIndividualRequest>(() => new FEnetReadIndividualRequest(Keys.First().DataType, this.Select(group => group.Key)));
            }

            protected override IReadOnlyDictionary<DeviceVariable, DeviceValue> Read()
            {
                return @interface.FEnetClient.Request(request.Value) as FEnetReadIndividualResponse;
            }
        }

        private bool isSettingChanged = true;
        private readonly object settingChangedLock = new object();
        private readonly List<DeviceVariableReader> deviceVariableReaders = new List<DeviceVariableReader>();
        private readonly List<Exception> pollingExceptions = new List<Exception>();

        /// <summary>
        /// 생성자
        /// </summary>
        public FEnetInterface() : this(new FEnetClient(), null) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="fenetClient">FEnet 클라이언트</param>
        public FEnetInterface(FEnetClient fenetClient) : this(fenetClient, null) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="fenetClient">FEnet 클라이언트</param>
        /// <param name="pollingTimeSpan">값 읽기 요청 주기. 기본값은 500 밀리초.</param>
        public FEnetInterface(FEnetClient fenetClient, int pollingTimeSpan) : this(fenetClient, pollingTimeSpan, null) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="fenetClient">FEnet 클라이언트</param>
        /// <param name="points">인터페이스 포인트 열거</param>
        public FEnetInterface(FEnetClient fenetClient, IEnumerable<PlcPoint> points) : this(fenetClient, 500, points) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="fenetClient">FEnet 클라이언트</param>
        /// <param name="pollingTimeSpan">값 읽기 요청 주기. 기본값은 500 밀리초.</param>
        /// <param name="points">인터페이스 포인트 열거</param>
        public FEnetInterface(FEnetClient fenetClient, int pollingTimeSpan, IEnumerable<PlcPoint> points) : base(pollingTimeSpan)
        {
            FEnetClient = fenetClient;
            AddRange(points);
        }

        /// <summary>
        /// LS ELECTRIC(구 LS산전) FEnet 프로토콜 기반 클라이언트입니다. XGT 시리즈 제품의 FEnet I/F 모듈과 통신 가능합니다.
        /// </summary>
        public FEnetClient FEnetClient { get; }

        /// <summary>
        /// 1주기의 값 읽기 요청과 응답이 완료되었을 때 발생하는 이벤트
        /// </summary>
        public override event PollingCompletedEventHandler PollingCompleted;

        bool IPlcInterface.Write(PlcPoint point, DeviceValue deviceValue)
        {
            var deviceVariable = point.DeviceVariable;
            if (!(point.writeRequest is FEnetWriteIndividualRequest writeRequest)
                || writeRequest.DataType != ToFEnetDataType(point.DeviceVariable.DataType))
                point.writeRequest = writeRequest = new FEnetWriteIndividualRequest(point.DeviceVariable.DataType);

            if (writeRequest.Count != 1 || !writeRequest.ContainsKey(deviceVariable))
                writeRequest.Clear();

            writeRequest[deviceVariable] = deviceValue;

            return FEnetClient.Request(writeRequest) is FEnetACKResponse;
        }

        private static FEnetDataType ToFEnetDataType(DataType dataType)
        {
            switch (dataType)
            {
                case DataType.Bit:
                    return FEnetDataType.Bit;
                case DataType.Byte:
                    return FEnetDataType.Byte;
                case DataType.Word:
                    return FEnetDataType.Word;
                case DataType.DoubleWord:
                    return FEnetDataType.DoubleWord;
                case DataType.LongWord:
                    return FEnetDataType.LongWord;
                default:
                    return FEnetDataType.Continuous;
            }
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
                    CreatePollingRequests();
                    isSettingChanged = false;
                }
            }

            bool succeed = false;
            pollingExceptions.Clear();

            void runRequest(DeviceVariableReader reader)
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
            }

            if (PollingParallelRequests)
                deviceVariableReaders.AsParallel().ForAll(reader => runRequest(reader));
            else
                foreach (var reader in deviceVariableReaders)
                    runRequest(reader);

            PollingCompleted?.Invoke(this, new PollingCompletedEventArgs(succeed, pollingExceptions.Count > 0 ? new AggregateException(pollingExceptions) : null));
        }

        /// <summary>
        /// 관리되지 않는 리소스의 확보, 해제 또는 다시 설정과 관련된 애플리케이션 정의 작업을 수행합니다.
        /// </summary>
        /// <param name="disposing">Dispose 메서드 수행 중인지 여부</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            FEnetClient?.Dispose();
        }

        /// <summary>
        /// 요청을 병렬로 수행할 지 여부.
        /// </summary>
        public bool PollingParallelRequests { get; set; }

        /// <inheritdoc/>
        protected override void OnStart()
        {
            (FEnetClient.Channel as ChannelProvider)?.Start();
            base.OnStart();
        }

        /// <inheritdoc/>
        protected override void OnStop()
        {
            (FEnetClient.Channel as Channel)?.Close();
            (FEnetClient.Channel as ChannelProvider)?.Stop();
            base.OnStop();
        }
    }
}
