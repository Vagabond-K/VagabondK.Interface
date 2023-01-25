namespace VagabondK.Interface.Abstractions
{
    interface IInterfaceBinding
    {
        object Target { get; set; }
        string MemberName { get; set; }
        InterfaceMode Mode { get; set; }
        bool RollbackOnSendError { get; set; }
    }
}