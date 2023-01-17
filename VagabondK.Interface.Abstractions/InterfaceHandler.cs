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
        private TValue value;

        protected void SetLocalValue(TValue value, DateTime? timeStamp)
        {
            Value = value;
            TimeStamp = timeStamp ?? DateTime.Now;
        }

        internal override void SetReceivedOtherTypeValue<T>(T value, DateTime? timeStamp)
            => SetReceivedValue(value.To<T, TValue>(), timeStamp);

        internal void SetReceivedValue(TValue value, DateTime? timeStamp)
        {
            if (Mode == InterfaceMode.TwoWay || Mode == InterfaceMode.ReceiveOnly)
            {
                SetLocalValue(value, timeStamp);
                Received?.Invoke(Point, value, timeStamp);
            }
        }

        public override Task<bool> SendLocalValueAsync()
            => TimeStamp != null ? SendAsync(Value, TimeStamp) : Task.FromResult(false);
        public override bool SendLocalValue()
            => TimeStamp != null ? Send(Value, TimeStamp) : false;

        public InterfaceHandler() { }
        public InterfaceHandler(InterfaceMode mode) : base(mode)
        {
        }

        public TValue Value { get => value; protected set => SetProperty(ref this.value, value); }
        public event ReceivedEventHandler<TValue> Received;

        public Task<bool> SendAsync(TValue value) => SendAsync(value, null);
        public async Task<bool> SendAsync(TValue value, DateTime? timeStamp)
        {
            if (await ((Point?.Interface as IInterface)?.SendAsync(Point, value, timeStamp) ?? Task.FromResult(false)))
            {
                SetLocalValue(value, timeStamp);
                return true;
            }
            return false;
        }
        public bool Send(TValue value) => Send(value, null);
        public bool Send(TValue value, DateTime? timeStamp)
        {
            if ((Point?.Interface as IInterface)?.Send(Point, value, timeStamp) ?? false)
            {
                SetLocalValue(value, timeStamp);
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
