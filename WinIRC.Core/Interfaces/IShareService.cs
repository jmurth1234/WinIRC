using System.Threading.Tasks;

namespace WinIRC.Core.Interfaces;

public interface IShareService
{
    Task ShareTextAsync(string title, string text);
}
