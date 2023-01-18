using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Linq;
using VagabondK.Interface.Abstractions;

namespace VagabondK.Interface
{
    public class InterfaceHandler<TValue> : InterfaceHandler
    {
        internal TValue value;

        protected void SetLocalValue(ref TValue value, ref DateTime? timeStamp)
        {
            SetProperty(ref this.value, ref value, nameof(Value));
            SetTimeStamp(ref timeStamp);
        }

        internal override void SetReceivedOtherTypeValue<T>(ref T value, ref DateTime? timeStamp)
        {
            var converted = value.To<T, TValue>();
            SetReceivedValue(ref converted, ref timeStamp);
        }

        internal void SetReceivedValue(ref TValue value, ref DateTime? timeStamp)
        {
            if (Mode == InterfaceMode.TwoWay || Mode == InterfaceMode.ReceiveOnly)
            {
                SetLocalValue(ref value, ref timeStamp);
                OnReceived(ref value, ref timeStamp);
                Received?.Invoke(this, value, timeStamp);
            }
        }

        protected virtual void OnReceived(ref TValue value, ref DateTime? timeStamp) { }

        public override Task<bool> SendLocalValueAsync()
            => timeStamp != null ? Point?.OnSendAsyncRequested(value, timeStamp) : Task.FromResult(false);
        public override bool SendLocalValue()
            => timeStamp != null && (Point?.OnSendRequested(value, timeStamp) ?? false);

        public InterfaceHandler() { }
        public InterfaceHandler(InterfaceMode mode) : base(mode)
        {
        }

        public TValue Value { get => value; protected set => SetProperty(ref this.value, ref value); }
        public event ReceivedEventHandler<TValue> Received;

        public async Task<bool> SendAsync(TValue value, DateTime? timeStamp = null)
        {
            if (await (Point?.OnSendAsyncRequested(value, timeStamp) ?? Task.FromResult(false)))
            {
                SetLocalValue(ref value, ref timeStamp);
                return true;
            }
            return false;
        }

        public bool Send(TValue value, DateTime? timeStamp = null)
        {
            if (Point?.OnSendRequested(value, timeStamp) ?? false)
            {
                SetLocalValue(ref value, ref timeStamp);
                return true;
            }
            return false;
        }

        public override Task<bool> SendAsync<T>(T value, DateTime? timeStamp)
            => SendAsync(value.To<T, TValue>(), timeStamp);

        public override bool Send<T>(T value, DateTime? timeStamp)
            => Send(value.To<T, TValue>(), timeStamp);
    }
}
