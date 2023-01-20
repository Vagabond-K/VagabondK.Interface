using System;
using System.ComponentModel;
using System.Windows.Input;

namespace VagabondK.Interface
{
    public class SendInterfaceValueCommand<TValue> : ICommand, INotifyPropertyChanged
    {
        internal SendInterfaceValueCommand(InterfaceHandler<TValue> handler)
        {
            this.handler = handler;
        }

        private readonly InterfaceHandler<TValue> handler;
        private bool isBusy;

        public event EventHandler CanExecuteChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool IsBusy
        {
            get => isBusy;
            set
            {
                if (isBusy != value)
                {
                    isBusy = value;
                    CanExecuteChanged?.Invoke(this, EventArgs.Empty);
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsBusy)));
                }
            }
        }

        public bool CanExecute(object parameter) => !isBusy;

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
                        await binding.SendAsyncAndUpdateProperty(value);
                    else
                        await handler.SendAsync(value);
                }
                catch { }

                IsBusy = false;
            }
        }
    }
}
