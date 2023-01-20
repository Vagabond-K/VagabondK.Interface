using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace VagabondK.Interface.Abstractions
{
    public abstract class InterfacePoint : IEnumerable<InterfaceHandler>
    {
        private readonly List<WeakReference<InterfaceHandler>> handlers = new List<WeakReference<InterfaceHandler>>();

        private InterfaceHandler GetLastUpdatedHandler()
        {
            lock (handlers)
                return handlers.Select(handler => handler.TryGetTarget(out var target) ? target : null)
                    .Where(handler => handler != null && handler.TimeStamp != null)
                    .OrderByDescending(handler => handler.TimeStamp.Value)
                    .FirstOrDefault();
        }

        private void VacuumHandlers()
        {
            lock (handlers)
                foreach (var removed in handlers.Where(reference => !reference.TryGetTarget(out var target)).ToArray())
                    handlers.Remove(removed);
        }

        protected void RaiseErrorOccurred(Exception exception, ErrorDirection direction)
        {
            lock (handlers)
                foreach (var handler in handlers.Select(reference => reference.TryGetTarget(out var target) ? target : null))
                    handler?.RaiseErrorOccurred(exception, direction);
        }

        public Task<bool> SendLocalValueAsync()
            => GetLastUpdatedHandler()?.SendLocalValueAsync() ?? Task.FromResult(false);
        public bool SendLocalValue()
            => GetLastUpdatedHandler()?.SendLocalValue() ?? false;

        protected void SetReceivedValue<TValue>(in TValue value, in DateTime? timeStamp)
        {
            lock (handlers)
                foreach (var handler in handlers.Select(reference => reference.TryGetTarget(out var target) ? target : null))
                    try
                    {
                        if (handler is InterfaceHandler<TValue> interfaceHandler) interfaceHandler.SetReceivedValue(value, timeStamp);
                        else handler.SetReceivedOtherTypeValue(value, timeStamp);
                    }
                    catch (Exception ex)
                    {
                        handler.RaiseErrorOccurred(ex, ErrorDirection.Receiving);
                    }
        }

        public bool IsWaitSending => Interface is IWaitSendingInterface;

        protected internal abstract Task<bool> OnSendAsyncRequested<TValue>(in TValue value, in DateTime? timeStamp, in CancellationToken? cancellationToken);
        protected internal abstract bool OnSendRequested<TValue>(in TValue value, in DateTime? timeStamp);

        public void Add(InterfaceHandler handler)
        {
            VacuumHandlers();
            lock (handlers)
                if (handler != null)
                {
                    handler.Point?.Remove(handler);
                    handlers.Add(new WeakReference<InterfaceHandler>(handler));
                    handler.Point = this;
                }
        }

        public bool Remove(InterfaceHandler handler)
        {
            VacuumHandlers();
            lock (handlers)
            {
                bool result = false;
                foreach (var removed in handlers.Where(reference => reference.TryGetTarget(out var target) && Equals(target, handler)).ToArray())
                    if (handlers.Remove(removed))
                    {
                        result = true;
                        handler.Point = null;
                    }
                return result;
            }
        }

        public InterfacePoint(IEnumerable<InterfaceHandler> handlers)
        {
            if (handlers == null) return;
            foreach (var handler in handlers)
                Add(handler);
        }

        public object Interface { get; internal set; }

        public InterfaceHandler<TValue> CreateHandler<TValue>()
        {
            var result = new InterfaceHandler<TValue>();
            Add(result);
            return result;
        }

        public InterfaceBinding<TValue> SetBinding<TValue>(object target, string propertyName)
            => SetBinding<TValue>(target, propertyName, InterfaceMode.TwoWay, true);
        public InterfaceBinding<TValue> SetBinding<TValue>(object target, string propertyName, InterfaceMode mode)
            => SetBinding<TValue>(target, propertyName, mode, true);
        public InterfaceBinding<TValue> SetBinding<TValue>(object target, string propertyName, bool rollbackOnSendError)
            => SetBinding<TValue>(target, propertyName, InterfaceMode.TwoWay, rollbackOnSendError);
        public InterfaceBinding<TValue> SetBinding<TValue>(object target, string propertyName, InterfaceMode mode, bool rollbackOnSendError)
        {
            var result = new InterfaceBinding<TValue>(target, propertyName, mode, rollbackOnSendError);
            Add(result);
            return result;
        }

        public IEnumerator<InterfaceHandler> GetEnumerator()
        {
            lock (handlers)
                return handlers.Select(reference => reference.TryGetTarget(out var target) ? target : null).Where(handler => handler != null).ToArray().AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
