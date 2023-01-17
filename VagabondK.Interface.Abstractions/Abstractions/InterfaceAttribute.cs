using System;
using System.Collections.Generic;
using System.Reflection;

namespace VagabondK.Interface.Abstractions
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public abstract class InterfaceAttribute : Attribute
    {
        protected abstract InterfacePoint OnCreatePoint(MemberInfo memberInfo);

        public InterfaceMode Mode { get; set; } = InterfaceMode.TwoWay;
        public bool RollbackOnSendError { get; set; } = true;
        internal InterfacePoint GetPoint(MemberInfo memberInfo) => OnCreatePoint(memberInfo);
    }
}
