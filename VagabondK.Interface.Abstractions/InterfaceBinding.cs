using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Security;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VagabondK.Interface.Abstractions;

namespace VagabondK.Interface
{
    public class InterfaceBinding<TValue> : InterfaceHandler<TValue>, IInterfaceBinding
    {
        private readonly object updatePropertyLock = new object();
        private readonly object updateLock = new object();

        private object target;
        private string propertyName;
        private bool rollbackOnSendError = true;
        private Type memberType;
        private Func<TValue> getter;
        private Action<TValue> setter;
        private bool isUpdating;

        private void UpdatePropertyValue(ref TValue value, bool isReceiving = false)
        {
            lock (updateLock)
            {
                isUpdating = true;
                if (isReceiving)
                    try
                    {
                        setter?.Invoke(value);
                    }
                    catch (Exception ex)
                    {
                        RaiseErrorOccurred(ex, ErrorDirection.Receiving);
                    }
                else
                    setter?.Invoke(value);
                isUpdating = false;
            }
        }

        private void UpdateProperty()
        {
            if (target != null && !string.IsNullOrWhiteSpace(propertyName))
            {
                getter = CreateGetter(target, propertyName);
                setter = CreateSetter(target, propertyName);
                if (getter != null)
                {
                    var value = getter();
                    DateTime? timeStamp = DateTime.Now;
                    SetLocalValue(ref value, ref timeStamp);
                }
            }
            else
            {
                memberType = null;
                getter = null;
                setter = null;
            }
            propertyChangingEventArgsType = null;
            newValueGetter = null;
            cancel = null;
        }

        private static Func<TValue> CreateGetter(object target, string memberName)
        {
            var targetType = target?.GetType();
            if (targetType == null || string.IsNullOrEmpty(memberName)) return null;
            var memberInfo = GetMemberInfo(targetType, memberName);
            if (memberInfo == null) return null;

            var valueType = typeof(TValue);
            var targetConstant = Expression.Constant(target);

            if (memberInfo is PropertyInfo propertyInfo)
            {
                var getMethod = propertyInfo.GetMethod;
                if (getMethod == null) return null;
                var expression = GetConvertExpression(Expression.Call(targetConstant, getMethod), valueType);
                return Expression.Lambda<Func<TValue>>(expression).Compile();
            }
            else if (memberInfo is FieldInfo fieldInfo)
            {
                var expression = GetConvertExpression(fieldInfo.IsStatic
                    ? Expression.Field(null, fieldInfo)
                    : Expression.Field(targetConstant, fieldInfo), valueType);
                return Expression.Lambda<Func<TValue>>(expression).Compile();
            }
            return null;
        }

        private static Action<TValue> CreateSetter(object target, string memberName)
        {
            var targetType = target?.GetType();
            if (targetType == null || string.IsNullOrEmpty(memberName)) return null;
            var memberInfo = GetMemberInfo(targetType, memberName);
            if (memberInfo == null) return null;

            var valueType = typeof(TValue);
            var targetConstant = Expression.Constant(target);
            var valueParameter = Expression.Parameter(valueType);

            if (memberInfo is PropertyInfo propertyInfo)
            {
                var setMethod = propertyInfo.SetMethod;
                if (setMethod == null) return null;
                var expression = Expression.Call(targetConstant, setMethod, GetConvertExpression(valueParameter, propertyInfo.PropertyType));
                return Expression.Lambda<Action<TValue>>(expression, valueParameter).Compile();
            }
            else if (memberInfo is FieldInfo fieldInfo)
            {
                var expression = Expression.Assign(fieldInfo.IsStatic
                    ? Expression.Field(null, fieldInfo)
                    : Expression.Field(targetConstant, fieldInfo), GetConvertExpression(valueParameter, fieldInfo.FieldType));
                return Expression.Lambda<Action<TValue>>(expression, valueParameter).Compile();
            }
            return null;
        }
        private static MemberInfo GetMemberInfo(Type targetType, string memberName)
        {
            if (targetType == null) return null;

            MemberInfo memberInfo = null;
            while (memberInfo == null)
            {
                if (targetType == typeof(object)) break;
                memberInfo = targetType.GetMember(memberName,
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly).FirstOrDefault();
                if (memberInfo != null) break;
                targetType = targetType.BaseType;
            }
            return memberInfo;
        }
        private static Expression GetConvertExpression(Expression expression, Type targetType)
        {
            var sourceType = expression.Type;
            if (sourceType != targetType && !targetType.IsAssignableFrom(sourceType))
            {
                try
                {
                    return Expression.Convert(expression, targetType);
                }
                catch
                {
                    var convertToMethod = typeof(Convert).GetMethod($"To{targetType.Name}", new[] { sourceType });
                    if (convertToMethod != null)
                        return Expression.Call(convertToMethod, expression);
                }
            }
            return expression;
        }

        private Type propertyChangingEventArgsType;
        private Func<PropertyChangingEventArgs, TValue> newValueGetter;
        private Action<PropertyChangingEventArgs> cancel;

        private async void OnPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            var point = Point;

            if (point != null && (Mode == InterfaceMode.TwoWay || Mode == InterfaceMode.SendOnly)
                && !isUpdating && e.PropertyName == PropertyName)
            {
                var eventArgstype = e?.GetType();
                if (eventArgstype != null && propertyChangingEventArgsType != eventArgstype)
                {
                    var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                    var newValueGetMethod =
                        eventArgstype.GetProperty("NewValue", bindingFlags)?.GetMethod
                        ?? eventArgstype.GetProperty("Value", bindingFlags)?.GetMethod;
                    var cancelSetMethod =
                        eventArgstype.GetProperty("IsCanceled", bindingFlags)?.SetMethod
                        ?? eventArgstype.GetProperty("Canceled", bindingFlags)?.SetMethod
                        ?? eventArgstype.GetProperty("Cancel", bindingFlags)?.SetMethod
                        ?? eventArgstype.GetProperty("IsCancel", bindingFlags)?.SetMethod;

                    if (newValueGetMethod != null && cancelSetMethod != null && cancelSetMethod.GetParameters()?.FirstOrDefault()?.ParameterType == typeof(bool))
                    {
                        var eventArgsParameter = Expression.Parameter(typeof(PropertyChangingEventArgs));
                        var changingEventArgs = Expression.Convert(eventArgsParameter, eventArgstype);
                        var callGetMethod = GetConvertExpression(Expression.Call(changingEventArgs, newValueGetMethod), typeof(TValue));
                        this.newValueGetter = Expression.Lambda<Func<PropertyChangingEventArgs, TValue>>(callGetMethod, eventArgsParameter).Compile();

                        var callSetMethod = Expression.Call(changingEventArgs, cancelSetMethod, Expression.Constant(true));
                        this.cancel = Expression.Lambda<Action<PropertyChangingEventArgs>>(callSetMethod, eventArgsParameter).Compile();
                    }
                    propertyChangingEventArgsType = eventArgstype;
                }

                var newValueGetter = this.newValueGetter;
                var cancel = this.cancel;
                
                if (newValueGetter != null && cancel != null)
                {
                    var newValue = newValueGetter(e);
                    if (point.IsWaitSending)
                    {
                        cancel(e);
                        if (await point.OnSendAsyncRequested(newValue))
                            UpdatePropertyValue(ref newValue);
                    }
                    else if (!point.OnSendRequested(newValue))
                        cancel(e);
                }
                else if (Target is INotifyPropertyChanged notifyPropertyChanged)
                {
                    void OnPropertyChangedTemp(object senderTemp, PropertyChangedEventArgs eTemp)
                    {
                        OnPropertyChanged(senderTemp, eTemp);
                        lock (updatePropertyLock)
                            notifyPropertyChanged.PropertyChanged -= OnPropertyChangedTemp;
                    }
                    lock (updatePropertyLock)
                        notifyPropertyChanged.PropertyChanged += OnPropertyChangedTemp;
                }
            }
        }

        private async void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var getter = this.getter;
            var point = Point;

            if (getter != null && point != null
                && (Mode == InterfaceMode.TwoWay || Mode == InterfaceMode.SendOnly)
                && memberType != null && !isUpdating && e.PropertyName == PropertyName)
            {
                var value = getter();
                if (!(!point.IsWaitSending ? point.OnSendRequested(value) : await point.OnSendAsyncRequested(value)) && RollbackOnSendError)
                    UpdatePropertyValue(ref this.value);
            }
        }

        public InterfaceBinding() : this(null, null, InterfaceMode.TwoWay, true) { }
        public InterfaceBinding(InterfaceMode mode) : this(null, null, mode, true) { }
        public InterfaceBinding(bool rollbackOnSendError) : this(null, null, InterfaceMode.TwoWay, rollbackOnSendError) { }
        public InterfaceBinding(InterfaceMode mode, bool rollbackOnSendError) : this(null, null, mode, rollbackOnSendError) { }
        public InterfaceBinding(object target, string propertyName) : this(target, propertyName, InterfaceMode.TwoWay, true) { }
        public InterfaceBinding(object target, string propertyName, InterfaceMode mode) : this(target, propertyName, mode, true) { }
        public InterfaceBinding(object target, string propertyName, bool rollbackOnSendError) : this(target, propertyName, InterfaceMode.TwoWay, rollbackOnSendError) { }
        public InterfaceBinding(object target, string propertyName, InterfaceMode mode, bool rollbackOnSendError) : base(mode)
        {
            Target = target;
            PropertyName = propertyName;
            RollbackOnSendError = rollbackOnSendError;
        }

        protected override void OnReceived(ref TValue value, ref DateTime? timeStamp)
            => UpdatePropertyValue(ref value, true);

        public object Target
        {
            get => target;
            set
            {
                lock (updatePropertyLock)
                {
                    if (!Equals(target, value))
                    {
                        if (target is INotifyPropertyChanging oldNotifyPropertyChanging)
                            oldNotifyPropertyChanging.PropertyChanging -= OnPropertyChanging;
                        if (target is INotifyPropertyChanged oldNotifyPropertyChanged)
                            oldNotifyPropertyChanged.PropertyChanged -= OnPropertyChanged;

                        target = value;
                        UpdateProperty();

                        if (rollbackOnSendError)
                        {
                            if (target is INotifyPropertyChanging newNotifyPropertyChanging)
                                newNotifyPropertyChanging.PropertyChanging += OnPropertyChanging;
                            else if (target is INotifyPropertyChanged newNotifyPropertyChanged)
                                newNotifyPropertyChanged.PropertyChanged += OnPropertyChanged;
                        }
                        else if (target is INotifyPropertyChanged newNotifyPropertyChanged)
                            newNotifyPropertyChanged.PropertyChanged += OnPropertyChanged;
                        RaisePropertyChanged();
                    }
                }
            }
        }
        public string PropertyName
        {
            get => propertyName;
            set
            {
                lock (updatePropertyLock)
                {
                    if (!Equals(propertyName, value))
                    {
                        propertyName = value;
                        UpdateProperty();
                        RaisePropertyChanged();
                    }
                }
            }
        }

        public bool RollbackOnSendError
        {
            get => rollbackOnSendError;
            set
            {
                lock (updatePropertyLock)
                {
                    if (rollbackOnSendError != value)
                    {
                        rollbackOnSendError = value;
                        if (target != null)
                        {
                            if (target is INotifyPropertyChanging oldNotifyPropertyChanging)
                                oldNotifyPropertyChanging.PropertyChanging -= OnPropertyChanging;
                            if (target is INotifyPropertyChanged oldNotifyPropertyChanged)
                                oldNotifyPropertyChanged.PropertyChanged -= OnPropertyChanged;

                            if (rollbackOnSendError)
                            {
                                if (target is INotifyPropertyChanging newNotifyPropertyChanging)
                                    newNotifyPropertyChanging.PropertyChanging += OnPropertyChanging;
                                else if (target is INotifyPropertyChanged newNotifyPropertyChanged)
                                    newNotifyPropertyChanged.PropertyChanged += OnPropertyChanged;
                            }
                            else if (target is INotifyPropertyChanged newNotifyPropertyChanged)
                                newNotifyPropertyChanged.PropertyChanged += OnPropertyChanged;
                        }

                        RaisePropertyChanged();
                    }
                }
            }
        }

        public Task<bool> SendAsyncAndUpdateProperty(TValue value) => SendAsyncAndUpdateProperty(value, null);
        public async Task<bool> SendAsyncAndUpdateProperty(TValue value, DateTime? timeStamp)
        {
            if (await Point?.OnSendAsyncRequested(value, timeStamp))
            {
                UpdatePropertyValue(ref value);
                return true;
            }
            return false;
        }

        public bool SendAndUpdateProperty(TValue value) => SendAndUpdateProperty(value, null);
        public bool SendAndUpdateProperty(TValue value, DateTime? timeStamp)
        {
            if (Point?.OnSendRequested(value, timeStamp) ?? false)
            {
                UpdatePropertyValue(ref value);
                return true;
            }
            return false;
        }

    }
}
