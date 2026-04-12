namespace WinIRC.Core.Dispatching;

public sealed class ImmediateDispatcher : IAppDispatcher
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task InvokeAsync(Func<Task> action)
    {
        await gate.WaitAsync();
        try
        {
            await action();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task InvokeAsync(Action action)
    {
        await InvokeAsync(() =>
        {
            action();
            return Task.CompletedTask;
        });
    }
}
