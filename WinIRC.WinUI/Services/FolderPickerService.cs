using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinIRC.Core.Interfaces;

namespace WinIRC.WinUI.Services;

public sealed class FolderPickerService : IFilePickerService
{
    public async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add("*");

        if (App.MainWindow != null)
        {
            WindowingHelpers.InitializeWithMainWindow(picker);
        }

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
