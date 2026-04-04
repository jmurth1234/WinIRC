using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using WinIRC.Core.Interfaces;
using WinIRC.Core.Models;
using WinIRC.WinUI.Views;

namespace WinIRC.WinUI.Services;

public sealed class WindowService : IWindowService
{
    private readonly List<Window> _openWindows = new List<Window>();

    public Task ShowDetachedSessionAsync(ChatSessionDescriptor descriptor)
    {
        var detachedWindow = new DetachedSessionWindow(descriptor.CloneDetached());
        detachedWindow.Closed += DetachedWindow_Closed;
        _openWindows.Add(detachedWindow);
        detachedWindow.Activate();
        return Task.CompletedTask;
    }

    private void DetachedWindow_Closed(object sender, WindowEventArgs args)
    {
        if (sender is Window window)
        {
            _openWindows.Remove(window);
        }
    }
}
