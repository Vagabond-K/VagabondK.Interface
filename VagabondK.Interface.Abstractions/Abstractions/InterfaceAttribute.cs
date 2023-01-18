using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Interface.Abstractions
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
    public abstract class InterfaceAttribute : Attribute
    {
        public abstract Type InterfacePointType { get; }
    }
}
