using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;

namespace VagabondK.Interface.Abstractions
{
    /// <summary>
    /// 통신 기반 인터페이스
    /// </summary>
    public abstract class Interface : INotifyPropertyChanged, IDisposable
    {
        private bool isRunning;
        private bool disposedValue;
        private readonly object startStopLock = new object();

        /// <summary>
        /// 임의의 속성 값 변경 이벤트 발생 메서드
        /// </summary>
        /// <param name="propertyName">변경된 속성 이름</param>
        protected void RaisePropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        internal abstract Type GetPointType();

        /// <summary>
        /// 통신 기반 인터페이스를 시작하기 위한 코드를 구현합니다.
        /// </summary>
        protected abstract void OnStart();

        /// <summary>
        /// 통신 기반 인터페이스를 종료하기 위한 코드를 구현합니다.
        /// </summary>
        protected abstract void OnStop();

        /// <summary>
        /// 속성 값이 변경될 때 발생합니다.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 현재 인터페이스의 포인트 형식을 가져옵니다.
        /// </summary>
        public Type PointType => GetPointType();

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
        /// 소멸자
        /// </summary>
        ~Interface() => Dispose(false);

        /// <summary>
        /// 통신 기반 인터페이스를 시작합니다.
        /// </summary>
        public void Start()
        {
            lock (startStopLock)
            {
                IsRunning = true;
                OnStart();
            }
        }

        /// <summary>
        /// 통신 기반 인터페이스를 종료합니다.
        /// </summary>
        public void Stop()
        {
            lock (startStopLock)
            {
                IsRunning = false;
                OnStop();
            }
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                disposedValue = true;
                if (disposing)
                    Stop();
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 통신 기반 인터페이스
    /// </summary>
    /// <typeparam name="TPoint">인터페이스 포인트 형식</typeparam>
    public abstract class Interface<TPoint> : Interface, ICollection<TPoint>, IEnumerable<TPoint> where TPoint : InterfacePoint
    {
        private readonly HashSet<TPoint> points = new HashSet<TPoint>();

        internal override Type GetPointType() => typeof(TPoint);

        /// <summary>
        /// 인터페이스 포인트가 추가되었을 경우 호출됨
        /// </summary>
        /// <param name="point">추가된 인터페이스 포인트</param>
        protected virtual void OnAdded(TPoint point) { }

        /// <summary>
        /// 인터페이스 포인트가 제거되었을 경우 호출됨
        /// </summary>
        /// <param name="point">제거된 인터페이스 포인트</param>
        protected virtual void OnRemoved(TPoint point) { }

        /// <summary>
        /// 인터페이스 포인트 열거를 일괄 추가합니다.
        /// </summary>
        /// <param name="points">인터페이스 포인트 열거</param>
        public void AddRange(IEnumerable<TPoint> points)
        {
            if (points == null) return;
            foreach (var point in points)
                Add(point);
        }

        /// <summary>
        /// 인터페이스 포인트를 추가합니다.
        /// </summary>
        /// <param name="point">인터페이스 포인트</param>
        public void Add(TPoint point)
        {
            if (point == null) throw new ArgumentNullException(nameof(point));
            lock (points)
            {
                if (point.Interface == this) return;

                (point.Interface as Interface<TPoint>)?.Remove(point);

                if (points.Add(point))
                {
                    point.Interface = this;
                    OnAdded(point);
                    
                    RaisePropertyChanged(nameof(Count));
                }
            }
        }

        /// <summary>
        /// 인터페이스 포인트를 제거합니다.
        /// </summary>
        /// <param name="point">인터페이스 포인트</param>
        /// <returns>제거 성공 여부</returns>
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

        /// <summary>
        /// 모든 인터페이스 포인트를 제거합니다.
        /// </summary>
        public void Clear()
        {
            lock (points)
                foreach (var point in points.ToArray())
                    Remove(point);
        }

        /// <summary>
        /// 현재 인터페이스에 종속된 인터페이스 포인트의 개수입니다.
        /// </summary>
        public int Count => points.Count;

        /// <summary>
        /// 컬렉션이 읽기 전용인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// 지정된 인터페이스 포인트가 포함되어 있는지 확인합니다.
        /// </summary>
        /// <param name="point">인터페이스 포인트</param>
        /// <returns>포함 여부</returns>
        public bool Contains(TPoint point) => points.Contains(point);

        /// <summary>
        /// 지정된 배열 인덱스에서 시작하여 현재 포함된 인터페이스 포인트들을 배열에 복사합니다.
        /// </summary>
        /// <param name="array">배열</param>
        /// <param name="arrayIndex">복사 시작 배열 인덱스</param>
        public void CopyTo(TPoint[] array, int arrayIndex) => points.CopyTo(array, arrayIndex);

        /// <summary>
        /// 인터페이스 포인트가 반복되는 열거자를 반환합니다.
        /// </summary>
        /// <returns>인터페이스 포인트 열거자</returns>
        public IEnumerator<TPoint> GetEnumerator() => points.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => points.GetEnumerator();

        /// <summary>
        /// 인터페이스 처리기 생성
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="point">인터페이스 포인트</param>
        /// <returns>인터페이스 처리기</returns>
        public InterfaceHandler<TValue> CreateHandler<TValue>(TPoint point)
        {
            if (point == null) return null;
            Add(point);
            return point.CreateHandler<TValue>();
        }

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="point">인터페이스 포인트</param>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <returns>인터페이스 바인딩</returns>
        public InterfaceBinding<TValue> SetBinding<TValue>(TPoint point, object target, string memberName)
            => SetBinding<TValue>(point, target, memberName, InterfaceMode.TwoWay, true);

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="point">인터페이스 포인트</param>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="mode">인터페이스 모드</param>
        /// <returns>인터페이스 바인딩</returns>
        public InterfaceBinding<TValue> SetBinding<TValue>(TPoint point, object target, string memberName, InterfaceMode mode)
            => SetBinding<TValue>(point, target, memberName, mode, true);

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="point">인터페이스 포인트</param>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="rollbackOnSendError">보내기 오류가 발생할 때 값 롤백 여부</param>
        /// <returns>인터페이스 바인딩</returns>
        public InterfaceBinding<TValue> SetBinding<TValue>(TPoint point, object target, string memberName, bool rollbackOnSendError)
            => SetBinding<TValue>(point, target, memberName, InterfaceMode.TwoWay, rollbackOnSendError);

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="point">인터페이스 포인트</param>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="mode">인터페이스 모드</param>
        /// <param name="rollbackOnSendError">보내기 오류가 발생할 때 값 롤백 여부</param>
        /// <returns>인터페이스 바인딩</returns>
        public InterfaceBinding<TValue> SetBinding<TValue>(TPoint point, object target, string memberName, InterfaceMode mode, bool rollbackOnSendError)
        {
            if (point == null) return null;
            Add(point);
            return point.SetBinding<TValue>(target, memberName, mode, rollbackOnSendError);
        }

        /// <summary>
        /// 인터페이스 바인딩 일괄 설정, InterfaceAttribute을 상속받은 특성을 이용하여 일괄 바인딩 설정 가능.
        /// </summary>
        /// <param name="targetRoot">최상위 바인딩 타겟 객체</param>
        /// <returns>인터페이스 처리기 사전. 키는 바인딩 경로 문자열이며 InterfaceBinding 형식의 인터페이스 처리기를 찾아볼 수 있음.</returns>
        public Dictionary<string, InterfaceHandler> SetBindings(object targetRoot) => SetBindings(targetRoot, null, null);

        /// <summary>
        /// 인터페이스 바인딩 일괄 설정, InterfaceAttribute을 상속받은 특성을 이용하여 일괄 바인딩 설정 가능.
        /// </summary>
        /// <param name="targetRoot">최상위 바인딩 타겟 객체</param>
        /// <param name="initPoint">인터페이스 포인트 초기화 동작</param>
        /// <returns>인터페이스 처리기 사전. 키는 바인딩 경로 문자열이며 InterfaceBinding 형식의 인터페이스 처리기를 찾아볼 수 있음.</returns>
        public Dictionary<string, InterfaceHandler> SetBindings(object targetRoot, Action<TPoint> initPoint) => SetBindings(targetRoot, initPoint, null);

        /// <summary>
        /// 인터페이스 바인딩 일괄 설정, InterfaceAttribute을 상속받은 특성을 이용하여 일괄 바인딩 설정 가능.
        /// </summary>
        /// <param name="targetRoot">최상위 바인딩 타겟 객체</param>
        /// <param name="initHandler">인터페이스 처리기 초기화 동작</param>
        /// <returns>인터페이스 처리기 사전. 키는 바인딩 경로 문자열이며 InterfaceBinding 형식의 인터페이스 처리기를 찾아볼 수 있음.</returns>
        public Dictionary<string, InterfaceHandler> SetBindings(object targetRoot, Action<InterfaceHandler> initHandler) => SetBindings(targetRoot, null, initHandler);

        private Dictionary<string, InterfaceHandler> SetBindings(object targetRoot, Action<TPoint> initPoint, Action<InterfaceHandler> initHandler)
        {
            if (targetRoot == null) throw new ArgumentNullException(nameof(targetRoot));

            var result = new Dictionary<string, InterfaceHandler>();

            InterfaceAttribute rootAttribute = null;
            var objects = new HashSet<object>();
            var queue = new Queue<BindingSettingQueueItem>();
            queue.Enqueue(new BindingSettingQueueItem(string.Empty, targetRoot, null));

            while (queue.Count > 0)
            {
                var item = queue.Dequeue();

                var name = item.name;
                var target = item.target;
                if (!objects.Add(target)) continue;

                var targetType = target.GetType();
                var interfaceAttribute = targetType.GetCustomAttributes(typeof(InterfaceAttribute), true).FirstOrDefault(iAttr => (iAttr as InterfaceAttribute)?.InterfacePointType == typeof(TPoint));
                if (interfaceAttribute == null) continue;

                if (rootAttribute == null)
                    rootAttribute = interfaceAttribute as InterfaceAttribute;

                foreach (var memberInfo in targetType.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    var attributes = memberInfo.GetCustomAttributes(typeof(InterfacePointAttribute), true);

                    Type memberType;
                    object memberValue = null;
                    if (memberInfo is PropertyInfo property)
                    {
                        memberType = property.PropertyType;
                        if (memberType.IsClass && property.GetMethod != null)
                            memberValue = property.GetValue(target);
                    }
                    else if (memberInfo is FieldInfo field)
                    {
                        memberType = field.FieldType;
                        if (memberType.IsClass)
                            memberValue = field.GetValue(target);
                    }
                    else continue;

                    var memberPath = string.IsNullOrEmpty(name) ? memberInfo.Name : $"{name}.{memberInfo.Name}";

                    if (memberValue is TPoint declaredPoint)
                    {
                        Add(declaredPoint);
                        var handler = declaredPoint.DefaultHandler;
                        if (handler != null)
                            result[memberPath] = handler;
                    }
                    else
                    {
                        foreach (var attribute in attributes)
                        {
                            if (attribute is InterfacePointAttribute bindingAttribute
                                && bindingAttribute.GetPoint(memberInfo, rootAttribute) is TPoint point)
                            {
                                var handler = (InterfaceHandler)Activator.CreateInstance(typeof(InterfaceBinding<>).MakeGenericType(memberType));
                                var binding = handler as IInterfaceBinding;
                                binding.Target = target;
                                binding.MemberName = memberInfo.Name;
                                binding.Mode = bindingAttribute.Mode;
                                binding.RollbackOnSendError = bindingAttribute.RollbackOnSendError;
                                point.Add(handler);
                                initPoint?.Invoke(point);
                                initHandler?.Invoke(handler);

                                Add(point);
                                result[memberPath] = handler;
                            }
                        }

                        if (memberValue != null && !item.parentTypes.Contains(memberType))
                            queue.Enqueue(new BindingSettingQueueItem(memberPath, memberValue, item.parentTypes));
                    }
                }
            }

            return result;
        }

        class BindingSettingQueueItem
        {
            public BindingSettingQueueItem(string name, object target, HashSet<Type> parentTypes)
            {
                this.name = name;
                this.target = target;
                this.parentTypes = parentTypes == null ? new HashSet<Type>() : new HashSet<Type>(parentTypes);
                this.parentTypes.Add(target.GetType());
            }
            public string name;
            public object target;
            public HashSet<Type> parentTypes;
        }
    }
}
