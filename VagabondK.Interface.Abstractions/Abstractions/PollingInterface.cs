using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace VagabondK.Interface.Abstractions
{
    public abstract class PollingInterface<TPoint> : Interface<TPoint>
        where TPoint : InterfacePoint
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

        protected PollingInterface(int pollingTimeSpan, IEnumerable<TPoint> points) : base(points)
        {
            this.pollingTimeSpan = pollingTimeSpan;
        }

        protected abstract void OnCreatePollingRequests();
        protected abstract void OnPoll();


        public abstract event PollingCompletedEventHandler PollingCompleted;

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

        public void Stop()
        {
            lock (startStopLock)
            {
                IsRunning = false;
                thread.Join();
            }
        }

        public void Poll()
        {
            OnPoll();
        }

    }
}
