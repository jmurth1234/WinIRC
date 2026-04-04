using System.Threading.Tasks;

namespace WinIRC.Core.Interfaces;

public interface IFilePickerService
{
    Task<string?> PickFolderAsync();
}
