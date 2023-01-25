namespace VagabondK.Interface
{
    /// <summary>
    /// 오류가 발생했을 때의 인터페이스 처리 방향
    /// </summary>
    public enum ErrorDirection
    {
        /// <summary>
        /// 방향이 정의되지 않음
        /// </summary>
        Indeterminate = 0,
        /// <summary>
        /// 값을 받는 중에 발생
        /// </summary>
        Receiving = 1,
        /// <summary>
        /// 값을 보내는 중에 발생
        /// </summary>
        Sending = 2,
    }
}
