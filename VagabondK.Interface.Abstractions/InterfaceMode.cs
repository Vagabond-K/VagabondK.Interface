namespace VagabondK.Interface
{
    /// <summary>
    /// 인터페이스 모드
    /// </summary>
    public enum InterfaceMode
    {
        /// <summary>
        /// 양방향 인터페이스
        /// </summary>
        TwoWay = 0,
        /// <summary>
        /// 보내기만 가능
        /// </summary>
        SendOnly = 1,
        /// <summary>
        /// 받기만 가능
        /// </summary>
        ReceiveOnly = 2,
    }
}
