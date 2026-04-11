namespace WinIRC.Core.Dispatching;

public interface IAppDispatcher
{
    Task InvokeAsync(Func<Task> action);
    Task InvokeAsync(Action action);
}
