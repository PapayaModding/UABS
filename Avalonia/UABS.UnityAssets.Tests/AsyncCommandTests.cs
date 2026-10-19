using UABS.ViewModels;

namespace UABS.UnityAssets.Tests;

public sealed class AsyncCommandTests
{
    [Fact]
    public async Task ExecuteAsync_PropagatesFailureAndRestoresCanExecute()
    {
        InvalidOperationException failure = new("boom");
        AsyncCommand command = new(() => Task.FromException(failure));

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => command.ExecuteAsync());

        Assert.Same(failure, actual);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task ICommandExecute_ObservesAndReportsFailureWithoutEscapingAsyncVoid()
    {
        InvalidOperationException failure = new("boom");
        Exception? reported = null;
        TaskCompletionSource<Exception> raised = new(TaskCreationOptions.RunContinuationsAsynchronously);
        AsyncCommand command = new(
            () => Task.FromException(failure),
            exceptionHandler: exception => reported = exception);
        command.ExecutionFailed += (_, args) => raised.SetResult(args.Exception);

        command.Execute(null);
        Exception observed = await raised.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Same(failure, observed);
        Assert.Same(failure, reported);
        Assert.Same(failure, command.LastException);
        Assert.True(command.CanExecute(null));
    }

    [Fact]
    public async Task ICommandExecute_TreatsCancellationAsExpectedAndRestoresCanExecute()
    {
        bool reported = false;
        AsyncCommand command = new(
            () => Task.FromCanceled(new CancellationToken(canceled: true)),
            exceptionHandler: _ => reported = true);

        command.Execute(null);
        await Task.Yield();
        await Task.Yield();

        Assert.False(reported);
        Assert.Null(command.LastException);
        Assert.True(command.CanExecute(null));
    }
}
