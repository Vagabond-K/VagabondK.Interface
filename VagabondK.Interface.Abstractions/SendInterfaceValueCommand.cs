using System;
using System.ComponentModel;
using System.Windows.Input;
using VagabondK.Interface.Abstractions;

namespace VagabondK.Interface
{
    /// <summary>
    /// 값 전송을 위한 커맨드
    /// </summary>
    public interface ISendInterfaceValueCommand : ICommand
    {
        /// <summary>
        /// 인터페이스 처리기
        /// </summary>
        InterfaceHandler InterfaceHandler { get; }

        /// <summary>
        /// 값을 전송 중인지 여부를 가져옵니다.
        /// </summary>
        bool IsBusy { get; }
    }

    /// <summary>
    /// 값 전송을 위한 커맨드
    /// </summary>
    /// <typeparam name="TValue">전송할 값의 형식</typeparam>
    class SendInterfaceValueCommand<TValue> : ISendInterfaceValueCommand, INotifyPropertyChanged
    {
        internal SendInterfaceValueCommand(InterfaceHandler<TValue> handler)
        {
            this.handler = handler;
        }

        private readonly InterfaceHandler<TValue> handler;
        private bool isBusy;

        /// <summary>
        /// 명령을 실행해야 하는지 여부에 영향을 주는 변경이 발생할 때 발생합니다.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// 속성 값이 변경될 때 발생합니다.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 값을 전송 중인지 여부를 가져옵니다.
        /// </summary>
        public bool IsBusy
        {
            get => isBusy;
            private set
            {
                if (isBusy != value)
                {
                    isBusy = value;
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBusy)));
                }
            }
        }

        /// <summary>
        /// 인터페이스 처리기
        /// </summary>
        public InterfaceHandler InterfaceHandler => handler;

        /// <summary>
        /// 명령을 현재 상태에서 실행할 수 있는지를 결정하는 메서드를 정의합니다.
        /// </summary>
        /// <param name="parameter">명령에 사용된 데이터입니다. 명령에서 데이터를 전달할 필요가 없으면 이 개체를 null로 설정할 수 있습니다.</param>
        /// <returns>이 명령을 실행할 수 있으면 true이고, 그러지 않으면 false입니다.</returns>
        public bool CanExecute(object parameter) => !isBusy;

        /// <summary>
        /// 명령이 호출될 때 호출될 메서드를 정의합니다.
        /// </summary>
        /// <param name="parameter">명령에 사용된 데이터입니다. 명령에서 데이터를 전달할 필요가 없으면 이 개체를 null로 설정할 수 있습니다.</param>
        public async void Execute(object parameter)
        {
            if (!(parameter is TValue value))
            {
                try
                {
                    value = (TValue)Convert.ChangeType(parameter, typeof(TValue));
                }
                catch
                {
                    return;
                }
            }

            if (!isBusy)
            {
                IsBusy = true;

                try
                {
                    if (handler is InterfaceBinding<TValue> binding)
                        await binding.SendAsyncAndUpdateMember(value);
                    else
                        await handler.SendAsync(value);
                }
                catch { }

                IsBusy = false;
            }
        }
    }
}
