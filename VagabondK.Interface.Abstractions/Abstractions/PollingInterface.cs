using System;
using System.Collections.Generic;
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
        private bool isRunning;
        private readonly object startStopLock = new object();

        private void RunPollingLoop()
        {
            var stopwatch = new Stopwatch();
            while (isRunning)
            {
                stopwatch.Restart();

                Poll();

                Thread.Sleep((int)Math.Max(0, pollingTimeSpan - stopwatch.ElapsedMilliseconds));
            }
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="pollingTimeSpan">값 읽기 요청 주기</param>
        /// <param name="points">인터페이스 포인트 열거</param>
        protected PollingInterface(int pollingTimeSpan, IEnumerable<TPoint> points) : base(points)
        {
            this.pollingTimeSpan = pollingTimeSpan;
        }

        /// <summary>
        /// 값 읽기 요청들을 일괄 생성할 필요가 있을 때 호출되는 메서드
        /// </summary>
        protected abstract void OnCreatePollingRequests();

        /// <summary>
        /// 값 읽기 요청 수행 메서드
        /// </summary>
        protected abstract void OnPoll();

        /// <summary>
        /// 1주기의 값 읽기 요청과 응답이 완료되었을 때 발생하는 이벤트
        /// </summary>
        public abstract event PollingCompletedEventHandler PollingCompleted;

        /// <summary>
        /// 값 읽기 요청 주기
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

        /// <summary>
        /// 실행 여부
        /// </summary>
        public bool IsRunning
        {
            get => isRunning;
            set
            {
                if (isRunning != value)
                {
                    isRunning = value;
                    RaisePropertyChanged(nameof(IsRunning));
                }
            }
        }

        /// <summary>
        /// 주기적으로 값 읽어오기를 시작합니다.
        /// </summary>
        public void Start()
        {
            lock (startStopLock)
            {
                IsRunning = true;
                thread = new Thread(new ThreadStart(RunPollingLoop))
                {
                    IsBackground = true
                };
                thread.Start();
            }
        }

        /// <summary>
        /// 주기적으로 값 읽어오기를 정지합니다.
        /// </summary>
        public void Stop()
        {
            lock (startStopLock)
            {
                IsRunning = false;
                thread.Join();
            }
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
