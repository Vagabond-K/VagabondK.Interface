using System;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using VagabondK.Interface.Abstractions;

namespace VagabondK.Interface
{
    /// <summary>
    /// 인터페이스 바인딩
    /// </summary>
    /// <typeparam name="TValue">바인딩할 값의 형식</typeparam>
    public class InterfaceBinding<TValue> : InterfaceHandler<TValue>, IInterfaceBinding
    {
        private readonly object updateMemberLock = new object();
        private readonly object updateLock = new object();

        private object target;
        private string memberName;
        private bool rollbackOnSendError = true;
        private Type memberType;
        private Func<TValue> getter;
        private Action<TValue> setter;
        private bool isUpdating;

        private void UpdateMemberValue(in TValue value, bool isReceiving = false)
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

        private void UpdateMember()
        {
            if (target != null && !string.IsNullOrWhiteSpace(memberName))
            {
                getter = CreateGetter(target, memberName);
                setter = CreateSetter(target, memberName);
                if (getter != null)
                {
                    var value = getter();
                    DateTime? timeStamp = DateTime.Now;
                    SetLocalValue(value, timeStamp);
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
                && !isUpdating && e.PropertyName == MemberName)
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
                        if (await point.OnSendAsyncRequested(newValue, null, null))
                            UpdateMemberValue(newValue);
                    }
                    else if (!point.OnSendRequested(newValue, null))
                        cancel(e);
                }
                else if (Target is INotifyPropertyChanged notifyPropertyChanged)
                {
                    void OnPropertyChangedTemp(object senderTemp, PropertyChangedEventArgs eTemp)
                    {
                        OnPropertyChanged(senderTemp, eTemp);
                        lock (updateMemberLock)
                            notifyPropertyChanged.PropertyChanged -= OnPropertyChangedTemp;
                    }
                    lock (updateMemberLock)
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
                && memberType != null && !isUpdating && e.PropertyName == MemberName)
            {
                var value = getter();
                if (!(!point.IsWaitSending ? point.OnSendRequested(value, null) : await point.OnSendAsyncRequested(value, null, null)) && RollbackOnSendError)
                    UpdateMemberValue(this.value);
            }
        }

        /// <summary>
        /// 생성자
        /// </summary>
        public InterfaceBinding() : this(null, null, InterfaceMode.TwoWay, true) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="mode">인터페이스 모드</param>
        public InterfaceBinding(InterfaceMode mode) : this(null, null, mode, true) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="rollbackOnSendError">보내기 오류가 발생할 때 값 롤백 여부</param>
        public InterfaceBinding(bool rollbackOnSendError) : this(null, null, InterfaceMode.TwoWay, rollbackOnSendError) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="mode">인터페이스 모드</param>
        /// <param name="rollbackOnSendError">보내기 오류가 발생할 때 값 롤백 여부</param>
        public InterfaceBinding(InterfaceMode mode, bool rollbackOnSendError) : this(null, null, mode, rollbackOnSendError) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        public InterfaceBinding(object target, string memberName) : this(target, memberName, InterfaceMode.TwoWay, true) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="mode">인터페이스 모드</param>
        public InterfaceBinding(object target, string memberName, InterfaceMode mode) : this(target, memberName, mode, true) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="rollbackOnSendError">보내기 오류가 발생할 때 값 롤백 여부</param>
        public InterfaceBinding(object target, string memberName, bool rollbackOnSendError) : this(target, memberName, InterfaceMode.TwoWay, rollbackOnSendError) { }
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="mode">인터페이스 모드</param>
        /// <param name="rollbackOnSendError">보내기 오류가 발생할 때 값 롤백 여부</param>
        public InterfaceBinding(object target, string memberName, InterfaceMode mode, bool rollbackOnSendError) : base(mode)
        {
            Target = target;
            MemberName = memberName;
            RollbackOnSendError = rollbackOnSendError;
        }


        /// <summary>
        /// 값이 수신되었을 때 호출되는 메서드
        /// </summary>
        /// <param name="value">받은 값</param>
        /// <param name="timeStamp">받은 값의 적용 일시</param>
        protected override void OnReceived(in TValue value, in DateTime? timeStamp)
            => UpdateMemberValue(value, true);

        /// <summary>
        /// 바인딩 타켓 객체
        /// </summary>
        public object Target
        {
            get => target;
            set
            {
                lock (updateMemberLock)
                {
                    if (!Equals(target, value))
                    {
                        if (target is INotifyPropertyChanging oldNotifyPropertyChanging)
                            oldNotifyPropertyChanging.PropertyChanging -= OnPropertyChanging;
                        if (target is INotifyPropertyChanged oldNotifyPropertyChanged)
                            oldNotifyPropertyChanged.PropertyChanged -= OnPropertyChanged;

                        target = value;
                        UpdateMember();

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

        /// <summary>
        /// 바인딩 멤버 이름
        /// </summary>
        public string MemberName
        {
            get => memberName;
            set
            {
                lock (updateMemberLock)
                {
                    if (!Equals(memberName, value))
                    {
                        memberName = value;
                        UpdateMember();
                        RaisePropertyChanged();
                    }
                }
            }
        }

        /// <summary>
        /// 보내기 오류가 발생할 때 값 롤백 여부
        /// </summary>
        public bool RollbackOnSendError
        {
            get => rollbackOnSendError;
            set
            {
                lock (updateMemberLock)
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

        /// <summary>
        /// 비동기로 값 전송 및 멤버 값 업데이트
        /// </summary>
        /// <param name="value">보낼 값</param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public Task<bool> SendAsyncAndUpdateMember(in TValue value) => SendAsyncAndUpdateMember(value, null);
        /// <summary>
        /// 비동기로 값 전송 및 멤버 값 업데이트
        /// </summary>
        /// <param name="value">보낼 값</param>
        /// <param name="timeStamp">보낼 값의 적용 일시</param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public async Task<bool> SendAsyncAndUpdateMember(TValue value, DateTime? timeStamp)
        {
            if (await SendAsync(value, timeStamp, null))
            {
                UpdateMemberValue(value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 값 전송 및 멤버 값 업데이트
        /// </summary>
        /// <param name="value">보낼 값</param>
        /// <returns>전송 성공 여부</returns>
        public bool SendAndUpdateMember(in TValue value) => SendAndUpdateMember(value, null);
        /// <summary>
        /// 값 전송 및 멤버 값 업데이트
        /// </summary>
        /// <param name="value">보낼 값</param>
        /// <param name="timeStamp">보낼 값의 적용 일시</param>
        /// <returns>전송 성공 여부</returns>
        public bool SendAndUpdateMember(in TValue value, in DateTime? timeStamp)
        {
            if (Send(value, timeStamp))
            {
                UpdateMemberValue(value);
                return true;
            }
            return false;
        }
    }
}
