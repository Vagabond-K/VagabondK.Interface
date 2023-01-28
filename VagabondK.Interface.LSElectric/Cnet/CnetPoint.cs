using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using VagabondK.Interface.Abstractions;
using VagabondK.Protocols.LSElectric;
using VagabondK.Protocols.LSElectric.Cnet;

namespace VagabondK.Interface.LSElectric.Cnet
{
    public abstract class CnetPoint : InterfacePoint
    {
        protected CnetPoint(IEnumerable<InterfaceHandler> handlers) : base(handlers)
        {
        }

        public DeviceVariable DeviceVariable { get; set; }
        internal abstract void SetReceivedValue(in DeviceValue deviceValue, in DateTime timeStamp);
    }

    public class CnetPoint<TValue> : CnetPoint, IInterfaceHandlerContainer<TValue>
    {
        private static readonly Func<TValue, DeviceValue> toDeviceValue;
        private static readonly Func<DeviceValue, TValue> toPointValue;
        private CnetWriteIndividualRequest writeRequest = new CnetWriteIndividualRequest(1);

        static CnetPoint()
        {
            try
            {
                var pointValueParameter = Expression.Parameter(typeof(TValue));
                var convertToDeviceValue = Expression.Convert(pointValueParameter, typeof(DeviceValue));
                toDeviceValue = Expression.Lambda<Func<TValue, DeviceValue>>(convertToDeviceValue, pointValueParameter).Compile();

                var deviceValueParameter = Expression.Parameter(typeof(DeviceValue));
                var convertToPointValue = Expression.Convert(deviceValueParameter, typeof(TValue));
                toPointValue = Expression.Lambda<Func<DeviceValue, TValue>>(convertToPointValue, deviceValueParameter).Compile();
            }
            catch (Exception ex)
            {
                throw new ArgumentOutOfRangeException(nameof(TValue), ex);
            }
        }

        protected CnetPoint(IEnumerable<InterfaceHandler> handlers) : base(handlers)
        {
        }

        private bool Send<T>(in T value, in DateTime? timeStamp)
        {
            var deviceValue = this is CnetPoint<T> point
                ? CnetPoint<T>.toDeviceValue(value)
                : toDeviceValue(value.To<T, TValue>());

            writeRequest[DeviceVariable] = deviceValue;

            (Interface as CnetInterface).CnetClient.Request(writeRequest);

            return false;
        }

        /// <summary>
        /// 비동기로 값을 전송하고자 할 때 호출되는 메서드
        /// </summary>
        /// <param name="value">전송할 값</param>
        /// <param name="timeStamp">전송할 값의 적용 일시</param>
        /// <param name="cancellationToken"></param>
        /// <returns>전송 성공 여부 반환 태스크</returns>
        protected override Task<bool> OnSendAsyncRequested<T>(in T value, in DateTime? timeStamp, in CancellationToken? cancellationToken)
        {
            var localValue = value;
            var localtimeStamp = timeStamp;
            return cancellationToken != null
                ? Task.Run(() => Send(localValue, localtimeStamp), cancellationToken.Value)
                : Task.Run(() => Send(localValue, localtimeStamp));
        }
        /// <summary>
        /// 값을 동기적으로 전송하고자 할 때 호출되는 메서드
        /// </summary>
        /// <param name="value">전송할 값</param>
        /// <param name="timeStamp">전송할 값의 적용 일시</param>
        /// <returns>전송 성공 여부</returns>
        protected override bool OnSendRequested<T>(in T value, in DateTime? timeStamp) => Send(value, timeStamp);

        internal override void SetReceivedValue(in DeviceValue deviceValue, in DateTime timeStamp)
            => SetReceivedValue(toPointValue(deviceValue), timeStamp);
    }


}
