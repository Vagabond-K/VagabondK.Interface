using System;
using System.Collections.Generic;
using System.Text;

namespace VagabondK.Interface.Abstractions
{
    interface IInterfaceBinding
    {
        object Target { get; set; }
        string PropertyName { get; set; }
        InterfaceMode Mode { get; set; }
        bool RollbackOnSendError { get; set; }
    }
}
