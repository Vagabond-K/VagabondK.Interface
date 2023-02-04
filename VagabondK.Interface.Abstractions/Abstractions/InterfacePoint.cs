using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VagabondK.Interface.Abstractions
{
    /// <summary>
    /// 인터페이스 포인트
    /// </summary>
    public abstract class InterfacePoint : IInterfaceHandlerContainer
    {
        private readonly List<WeakReference<InterfaceHandler>> handlers = new List<WeakReference<InterfaceHandler>>();
        private InterfaceHandler defaultHandler = null;

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

        /// <summary>
        /// 인터페이스 처리 중에 발생한 오류를 알릴 때 사용하는 메서드입니다.
        /// </summary>
        /// <param name="exception">오류 정보</param>
        /// <param name="direction">오류가 발생했을 때의 인터페이스 처리 방향</param>
        protected void RaiseErrorOccurred(Exception exception, ErrorDirection direction)
        {
            lock (handlers)
                foreach (var handler in handlers.Select(reference => reference.TryGetTarget(out var target) ? target : null))
                    handler?.RaiseErrorOccurred(exception, direction);
        }


        /// <summary>
        /// 비동기로 로컬 값 재전송
        /// </summary>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public Task<bool> SendLocalValueAsync()
            => GetLastUpdatedHandler()?.SendLocalValueAsync() ?? Task.FromResult(false);

        /// <summary>
        /// 로컬 값 재전송
        /// </summary>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        public bool SendLocalValue()
            => GetLastUpdatedHandler()?.SendLocalValue() ?? false;

        /// <summary>
        /// 수신한 값을 로컬 환경에 설정
        /// </summary>
        /// <typeparam name="TValue">받은 값 형식</typeparam>
        /// <param name="value">받은 값</param>
        /// <param name="timeStamp">받은 값의 적용 일시</param>
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

        /// <summary>
        /// 값을 전송할 때 대기 상황이 발생하는지 여부를 가져옵니다.
        /// </summary>
        public bool IsWaitSending => Interface is IWaitSendingInterface;

        /// <summary>
        /// 비동기로 값을 전송하고자 할 때 호출되는 메서드
        /// </summary>
        /// <typeparam name="TValue">전송할 값 형식</typeparam>
        /// <param name="value">전송할 값</param>
        /// <param name="timeStamp">전송할 값의 적용 일시</param>
        /// <param name="cancellationToken">태스크 취소 토큰</param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        protected internal abstract Task<bool> OnSendAsyncRequested<TValue>(in TValue value, in DateTime? timeStamp, in CancellationToken? cancellationToken);

        /// <summary>
        /// 값을 동기적으로 전송하고자 할 때 호출되는 메서드
        /// </summary>
        /// <typeparam name="TValue">전송할 값 형식</typeparam>
        /// <param name="value">전송할 값</param>
        /// <param name="timeStamp">전송할 값의 적용 일시</param>
        /// <returns>전송 성공 여부</returns>
        protected internal abstract bool OnSendRequested<TValue>(in TValue value, in DateTime? timeStamp);

        /// <summary>
        /// 인터페이스 처리기 추가
        /// </summary>
        /// <param name="handler">추가할 인터페이스 처리기</param>
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

        /// <summary>
        /// 인터페이스 처리기 제거
        /// </summary>
        /// <param name="handler">제거할 인터페이스 처리기</param>
        /// <returns>제거 성공 여부</returns>
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

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="handlers">인터페이스 처리기 열거</param>
        public InterfacePoint(IEnumerable<InterfaceHandler> handlers)
        {
            if (handlers == null) return;
            foreach (var handler in handlers)
                Add(handler);
        }

        /// <summary>
        /// 현재 종속된 인터페이스
        /// </summary>
        public object Interface { get; internal set; }

        /// <summary>
        /// 기본 인터페이스 처리기를 가져옵니다.
        /// </summary>
        public InterfaceHandler DefaultHandler
        {
            get
            {
                if (defaultHandler == null)
                {
                    var interfaceType = GetType().GetInterfaces().FirstOrDefault(
                        type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IInterfaceHandlerContainer<>));

                    if (interfaceType != null)
                        defaultHandler = Activator.CreateInstance(typeof(InterfaceHandler<>).MakeGenericType(interfaceType.GenericTypeArguments[0])) as InterfaceHandler;
                }
                return defaultHandler;
            }
        }

        /// <summary>
        /// 인터페이스 처리기 생성
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <returns>인터페이스 처리기</returns>
        public InterfaceHandler<TValue> CreateHandler<TValue>()
        {
            var result = new InterfaceHandler<TValue>();
            Add(result);
            return result;
        }

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <returns>인터페이스 바인딩</returns>
        public InterfaceBinding<TValue> SetBinding<TValue>(object target, string memberName)
            => SetBinding<TValue>(target, memberName, InterfaceMode.TwoWay, true);

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="mode">인터페이스 모드</param>
        /// <returns>인터페이스 바인딩</returns>
        public InterfaceBinding<TValue> SetBinding<TValue>(object target, string memberName, InterfaceMode mode)
            => SetBinding<TValue>(target, memberName, mode, true);
        
        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="rollbackOnSendError">보내기 오류가 발생할 때 값 롤백 여부</param>
        /// <returns>인터페이스 바인딩</returns>
        public InterfaceBinding<TValue> SetBinding<TValue>(object target, string memberName, bool rollbackOnSendError)
            => SetBinding<TValue>(target, memberName, InterfaceMode.TwoWay, rollbackOnSendError);

        /// <summary>
        /// 인터페이스 바인딩 설정
        /// </summary>
        /// <typeparam name="TValue">값 형식</typeparam>
        /// <param name="target">바인딩 타켓 객체</param>
        /// <param name="memberName">바인딩 멤버 이름</param>
        /// <param name="mode">인터페이스 모드</param>
        /// <param name="rollbackOnSendError">보내기 오류가 발생할 때 값 롤백 여부</param>
        /// <returns>인터페이스 바인딩</returns>
        public InterfaceBinding<TValue> SetBinding<TValue>(object target, string memberName, InterfaceMode mode, bool rollbackOnSendError)
        {
            var result = new InterfaceBinding<TValue>(target, memberName, mode, rollbackOnSendError);
            Add(result);
            return result;
        }

        /// <summary>
        /// 인터페이스 처리기가 반복되는 열거자를 반환합니다.
        /// </summary>
        /// <returns>인터페이스 처리기 열거자</returns>
        public IEnumerator<InterfaceHandler> GetEnumerator()
        {
            lock (handlers)
                return handlers.Select(reference => reference.TryGetTarget(out var target) ? target : null).Where(handler => handler != null).ToArray().AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
