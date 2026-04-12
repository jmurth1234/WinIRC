using Avalonia.Threading;
using WinIRC.Core.Dispatching;

namespace WinIRC.Avalonia.Services;

public sealed class AvaloniaAppDispatcher : IAppDispatcher
{
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task InvokeAsync(Func<Task> action)
    {
        await gate.WaitAsync();
        try
        {
            await Dispatcher.UIThread.InvokeAsync(action);
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
