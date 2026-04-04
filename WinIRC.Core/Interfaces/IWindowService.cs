using System.Threading.Tasks;
using WinIRC.Core.Models;

namespace WinIRC.Core.Interfaces;

public interface IWindowService
{
    Task ShowDetachedSessionAsync(ChatSessionDescriptor descriptor);
}
