using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using VagabondK.Interface.Abstractions;

namespace VagabondK.Interface
{
    public class InterfaceHandler<TValue> : InterfaceHandler
    {
        internal TValue value;
        private readonly Lazy<SendInterfaceValueCommand<TValue>> sendCommand;

        protected void SetLocalValue(in TValue value, in DateTime? timeStamp)
        {
            SetProperty(ref this.value, value, nameof(Value));
            SetTimeStamp(timeStamp);
        }

        internal override void SetReceivedOtherTypeValue<T>(in T value, in DateTime? timeStamp)
        {
            SetReceivedValue(value.To<T, TValue>(), timeStamp);
        }

        internal void SetReceivedValue(in TValue value, in DateTime? timeStamp)
        {
            if (Mode == InterfaceMode.TwoWay || Mode == InterfaceMode.ReceiveOnly)
            {
                SetLocalValue(value, timeStamp);
                OnReceived(value, timeStamp);
                Received?.Invoke(this);
            }
        }

        protected virtual void OnReceived(in TValue value, in DateTime? timeStamp) { }

        public override Task<bool> SendLocalValueAsync()
            => timeStamp != null ? Point?.OnSendAsyncRequested(value, timeStamp, null) : Task.FromResult(false);
        public override bool SendLocalValue()
            => timeStamp != null && (Point?.OnSendRequested(value, timeStamp) ?? false);

        public InterfaceHandler() : this(InterfaceMode.TwoWay) { }
        public InterfaceHandler(InterfaceMode mode) : base(mode)
        {
            sendCommand = new Lazy<SendInterfaceValueCommand<TValue>>(() => new SendInterfaceValueCommand<TValue>(this));
        }

        public TValue Value { get => value; protected set => SetProperty(ref this.value, value); }
        public event ReceivedEventHandler<TValue> Received;

        public Task<bool> SendAsync(in TValue value) => SendAsync(value, null, null);
        public Task<bool> SendAsync(in TValue value, in DateTime? timeStamp) => SendAsync(value, timeStamp, null);
        public Task<bool> SendAsync(in TValue value, in CancellationToken? cancellationToken) => SendAsync(value, null, cancellationToken);
        public async Task<bool> SendAsync(TValue value, DateTime? timeStamp, CancellationToken? cancellationToken)
        {
            if (await (Point?.OnSendAsyncRequested(value, timeStamp, cancellationToken) ?? Task.FromResult(false)))
            {
                SetLocalValue(value, timeStamp);
                return true;
            }
            return false;
        }

        public bool Send(in TValue value) => Send(value, null);
        public bool Send(in TValue value, in DateTime? timeStamp)
        {
            if (Point?.OnSendRequested(value, timeStamp) ?? false)
            {
                SetLocalValue(value, timeStamp);
                return true;
            }
            return false;
        }

        public override Task<bool> SendAsync<T>(in T value, DateTime? timeStamp, CancellationToken? cancellationToken)
            => SendAsync(value.To<T, TValue>(), timeStamp);

        public override bool Send<T>(in T value, DateTime? timeStamp)
            => Send(value.To<T, TValue>(), timeStamp);

        public virtual ICommand SendCommand => sendCommand.Value;
    }
}
