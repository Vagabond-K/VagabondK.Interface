using System;
using System.Diagnostics;
using System.Threading;

namespace VagabondK.Interface.Abstractions
{
    /// <summary>
    /// 주기적으로 값 읽기 요청을 수행하는 인터페이스를 정의합니다.
    /// </summary>
    /// <typeparam name="TPoint">인터페이스 포인트 형식</typeparam>
    public abstract class PollingInterface<TPoint> : Interface<TPoint> where TPoint : InterfacePoint
    {
        private Thread thread;
        private int pollingTimeSpan = 500;
        private readonly EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        private void RunPollingLoop()
        {
            var stopwatch = new Stopwatch();
            while (IsRunning)
            {
                stopwatch.Restart();

                Poll();
                waitHandle.WaitOne((int)Math.Max(0, pollingTimeSpan - stopwatch.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="pollingTimeSpan">값 읽기 요청 주기</param>
        protected PollingInterface(int pollingTimeSpan)
        {
            this.pollingTimeSpan = pollingTimeSpan;
        }

        /// <summary>
        /// 값 읽기 요청 수행 메서드
        /// </summary>
        protected abstract void OnPoll();

        /// <summary>
        /// 1주기의 값 읽기 요청과 응답이 완료되었을 때 발생하는 이벤트
        /// </summary>
        public abstract event PollingCompletedEventHandler PollingCompleted;

        /// <summary>
        /// 값 읽기 요청 주기. 기본값은 500 밀리초.
        /// </summary>
        public int PollingTimeSpan
        {
            get => pollingTimeSpan;
            set
            {
                if (pollingTimeSpan != value)
                {
                    pollingTimeSpan = value;
                    RaisePropertyChanged(nameof(PollingTimeSpan));
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnStart()
        {
            thread = new Thread(new ThreadStart(RunPollingLoop))
            {
                IsBackground = true
            };
            thread.Start();
        }

        /// <inheritdoc/>
        protected override void OnStop()
        {
            waitHandle.Set();
            thread.Join();
        }

        /// <summary>
        /// 주기적인 실행이 아닌, 사용자가 원할 때 호출하여 값 읽어오기를 시도합니다.
        /// </summary>
        public void Poll()
        {
            OnPoll();
        }
    }
}
