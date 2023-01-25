namespace VagabondK.Interface
{
    /// <summary>
    /// 인터페이스 처리 중에 발생한 오류에 대한 이벤트 처리기
    /// </summary>
    /// <param name="sender">이벤트 소스</param>
    /// <param name="e">인터페이스 처리 중에 발생한 오류에 대한 이벤트 매개변수</param>
    public delegate void ErrorOccurredEventHandler(object sender, ErrorOccurredEventArgs e);
}
