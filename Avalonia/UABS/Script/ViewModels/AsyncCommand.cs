using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UABS.ViewModels;

public sealed class AsyncCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private readonly Action<Exception> _exceptionHandler;
    private bool _isExecuting;

    public AsyncCommand(
        Func<Task> execute,
        Func<bool>? canExecute = null,
        Action<Exception>? exceptionHandler = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
        _exceptionHandler = exceptionHandler ?? (exception =>
            Printer.PrintException("Unhandled asynchronous command failure.", exception));
    }

    public event EventHandler? CanExecuteChanged;
    public event EventHandler<AsyncCommandFailedEventArgs>? ExecutionFailed;

    public Exception? LastException { get; private set; }

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        try
        {
            await ExecuteAsync(parameter);
        }
        catch (OperationCanceledException)
        {
            // Cancellation is an expected command outcome, not an application failure.
        }
        catch (Exception exception)
        {
            LastException = exception;
            try
            {
                _exceptionHandler(exception);
                ExecutionFailed?.Invoke(this, new AsyncCommandFailedEventArgs(exception));
            }
            catch (Exception reportingException)
            {
                Printer.PrintException("Asynchronous command failure reporting also failed.", reportingException);
                Printer.PrintException("Original asynchronous command failure.", exception);
            }
        }
    }

    public async Task ExecuteAsync(object? parameter = null)
    {
        if (!CanExecute(parameter))
            return;

        LastException = null;
        _isExecuting = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class AsyncCommandFailedEventArgs(Exception exception) : EventArgs
{
    public Exception Exception { get; } = exception;
}
