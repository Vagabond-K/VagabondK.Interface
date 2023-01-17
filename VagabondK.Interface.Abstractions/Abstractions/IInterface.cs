using System;
using System.Threading.Tasks;

namespace VagabondK.Interface.Abstractions
{
    interface IInterface
    {
        Task<bool> SendAsync<TValue>(InterfacePoint point, TValue value, DateTime? timeStamp);
        bool Send<TValue>(InterfacePoint point, TValue value, DateTime? timeStamp);
        bool Remove(InterfacePoint point);
    }
}
