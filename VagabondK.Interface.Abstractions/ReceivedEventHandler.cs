using System;
using VagabondK.Interface.Abstractions;

namespace VagabondK.Interface
{
    public delegate void ReceivedEventHandler<TValue>(InterfacePoint point, TValue value, DateTime? timeStamp);
}