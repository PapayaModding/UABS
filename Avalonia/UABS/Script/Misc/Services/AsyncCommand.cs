using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UABS.Script.Misc.Services
{
    public class AsyncCommand<T> : ICommand
    {
        private readonly Func<T, Task> _execute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged;

        public AsyncCommand(Func<T, Task> execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object? parameter) => !_isExecuting;

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;

            _isExecuting = true;
            RaiseCanExecuteChanged();

            try
            {
                await _execute((T)parameter!); // cast parameter to T
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged()
            => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}