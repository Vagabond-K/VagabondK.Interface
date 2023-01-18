using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace VagabondK.Interface.Abstractions
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
    public abstract class InterfaceBindingAttribute : Attribute
    {
        protected abstract InterfacePoint OnCreatePoint(MemberInfo memberInfo, InterfaceAttribute rootAttribute);

        public InterfaceMode Mode { get; set; } = InterfaceMode.TwoWay;
        public bool RollbackOnSendError { get; set; } = true;
        internal InterfacePoint GetPoint(MemberInfo memberInfo, InterfaceAttribute rootAttribute) => OnCreatePoint(memberInfo, rootAttribute);
    }
}
