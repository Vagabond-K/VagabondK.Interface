using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace VagabondK.Interface.Abstractions
{
    public abstract class Interface<TPoint> : ICollection<TPoint>, IEnumerable<TPoint>, INotifyPropertyChanged, IInterface where TPoint : InterfacePoint
    {
        private readonly HashSet<TPoint> points = new HashSet<TPoint>();

        protected Interface(IEnumerable<TPoint> points) => AddRange(points);

        protected virtual void OnAdded(TPoint point) { }
        protected virtual void OnRemoved(TPoint point) { }
        protected abstract Task<bool> OnSendRequestedAsync<TValue>(TPoint point, TValue value, DateTime? timeStamp);
        protected abstract bool OnSendRequested<TValue>(TPoint point, TValue value, DateTime? timeStamp);

        protected void SetReceivedValue<TValue>(TPoint point, TValue value, DateTime? timeStamp)
            => point?.SetReceivedValue(value, timeStamp);

        protected void ErrorOccurredAt(TPoint point, Exception exception, ErrorDirection direction)
            => point?.RaiseErrorOccurred(exception, direction);

        protected void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public event PropertyChangedEventHandler PropertyChanged;

        public void AddRange(IEnumerable<TPoint> points)
        {
            if (points == null) return;
            foreach (var point in points)
                Add(point);
        }

        public void Add(TPoint point)
        {
            if (point == null) throw new ArgumentNullException(nameof(point));
            lock (points)
            {
                if (point.Interface == this) return;
                (point.Interface as IInterface)?.Remove(point);

                if (points.Add(point))
                {
                    point.Interface = this;
                    OnAdded(point);
                    
                    RaisePropertyChanged(nameof(Count));
                }
            }
        }

        public bool Remove(TPoint point)
        {
            if (point == null) return false;
            lock (points)
            {
                if (points.Remove(point))
                {
                    point.Interface = null;
                    OnRemoved(point);
                    RaisePropertyChanged(nameof(Count));
                    return true;
                }
                return false;
            }
        }

        public void Clear() => points.Clear();

        async Task<bool> IInterface.SendAsync<TValue>(InterfacePoint point, TValue value, DateTime? timeStamp)
        {
            try
            {
                var result = await OnSendRequestedAsync((TPoint)point, value, timeStamp);
                return result;
            }
            catch (Exception ex)
            {
                point.RaiseErrorOccurred(ex, ErrorDirection.Sending);
            }
            return false;
        }

        bool IInterface.Send<TValue>(InterfacePoint point, TValue value, DateTime? timeStamp)
        {
            try
            {
                var result = OnSendRequested((TPoint)point, value, timeStamp);
                return result;
            }
            catch (Exception ex)
            {
                point.RaiseErrorOccurred(ex, ErrorDirection.Sending);
            }
            return false;
        }

        bool IInterface.Remove(InterfacePoint point) => Remove((TPoint)point);

        public int Count => points.Count;

        public bool IsReadOnly => false;

        public bool Contains(TPoint item) => points.Contains(item);
        public void CopyTo(TPoint[] array, int arrayIndex) => points.CopyTo(array, arrayIndex);
        public IEnumerator<TPoint> GetEnumerator() => points.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => points.GetEnumerator();


        public InterfaceHandler<TValue> CreateHandler<TValue>(TPoint point)
        {
            if (point == null) return null;
            Add(point);
            return point.CreateHandler<TValue>();
        }

        public InterfaceBinding<TValue> SetBinding<TValue>(TPoint point, object target, string propertyName)
            => SetBinding<TValue>(point, target, propertyName, InterfaceMode.TwoWay, true);
        public InterfaceBinding<TValue> SetBinding<TValue>(TPoint point, object target, string propertyName, InterfaceMode mode)
            => SetBinding<TValue>(point, target, propertyName, mode, true);
        public InterfaceBinding<TValue> SetBinding<TValue>(TPoint point, object target, string propertyName, bool rollbackOnSendError)
            => SetBinding<TValue>(point, target, propertyName, InterfaceMode.TwoWay, rollbackOnSendError);
        public InterfaceBinding<TValue> SetBinding<TValue>(TPoint point, object target, string propertyName, InterfaceMode mode, bool rollbackOnSendError)
        {
            if (point == null) return null;
            Add(point);
            return point.SetBinding<TValue>(target, propertyName, mode, rollbackOnSendError);
        }

        public IEnumerable<InterfaceHandler> SetBindings(object target) => SetBindings(target, null);
        public IEnumerable<InterfaceHandler> SetBindings(object targetRoot, Action<InterfaceHandler> initHandler)
        {
            if (targetRoot == null) return Enumerable.Empty<InterfaceHandler>();

            var result = new List<InterfaceHandler>();

            var objects = new HashSet<object>();
            var queue = new Queue();
            queue.Enqueue(targetRoot);

            while (queue.Count > 0)
            {
                var target = queue.Dequeue();
                if (!objects.Add(target)) continue;

                var targetType = target.GetType();
                foreach (var memberInfo in targetType.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var attributes = memberInfo.GetCustomAttributes(typeof(InterfaceAttribute), true);

                    Type memberType;
                    object subObject = null;
                    if (memberInfo is PropertyInfo property)
                    {
                        memberType = property.PropertyType;
                        if (memberType.IsClass && property.GetMethod != null)
                            subObject = property.GetValue(target);
                    }
                    else if (memberInfo is FieldInfo field)
                    {
                        memberType = field.FieldType;
                        if (memberType.IsClass)
                            subObject = field.GetValue(target);
                    }
                    else continue;

                    foreach (var attribute in attributes)
                    {
                        if (attribute is InterfaceAttribute interfaceAttribute
                            && interfaceAttribute.GetPoint(memberInfo) is TPoint point)
                        {
                            var handler = (InterfaceHandler)Activator.CreateInstance(typeof(InterfaceBinding<>).MakeGenericType(memberType));
                            var binding = handler as IInterfaceBinding;
                            binding.Target = target;
                            binding.PropertyName = memberInfo.Name;
                            binding.Mode = interfaceAttribute.Mode;
                            binding.RollbackOnSendError = interfaceAttribute.RollbackOnSendError;
                            point.Add(handler);
                            initHandler?.Invoke(handler);

                            Add(point);
                            result.Add(handler);
                        }
                    }

                    if (subObject != null)
                        queue.Enqueue(subObject);
                }
            }

            return result;
        }
    }
}
