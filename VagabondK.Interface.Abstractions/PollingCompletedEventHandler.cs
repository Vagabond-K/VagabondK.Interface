namespace VagabondK.Interface
{
    /// <summary>
    /// 1주기의 값 읽기 요청과 응답이 완료되었을 때 발생하는 이벤트의 처리기
    /// </summary>
    /// <param name="sender">이벤트 소스</param>
    /// <param name="e">1주기의 값 읽기 요청과 응답이 완료되었을 때 발생하는 이벤트의 매개변수</param>
    public delegate void PollingCompletedEventHandler(object sender, PollingCompletedEventArgs e);
}
