namespace VagabondK.Interface.Modbus
{
    /// <summary>
    /// DateTime 값의 Modbus Word 기반 직렬화/역직렬화 형식
    /// </summary>
    public enum DateTimeFormat
    {
        /// <summary>
        /// Unix time 기반
        /// </summary>
        UnixTime = 0,
        /// <summary>
        /// .NET DateTime 형식의 ToBinary, FromBinary 메서드 기반
        /// </summary>
        DotNet = 1,
        /// <summary>
        /// Ticks 기반
        /// </summary>
        Ticks = 2,
        /// <summary>
        /// 문자열 Parsing 기반
        /// </summary>
        String = 3,
        /// <summary>
        /// 각 Byte의 수치형 값 기반
        /// </summary>
        Bytes = 4,
    }
}
