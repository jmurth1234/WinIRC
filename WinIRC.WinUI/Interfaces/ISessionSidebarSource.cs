using System.Collections.ObjectModel;
using System.ComponentModel;
using WinIRC.WinUI.Models;

namespace WinIRC.WinUI.Interfaces;

public interface ISessionSidebarSource : INotifyPropertyChanged
{
    string SidebarTitle { get; }

    string SidebarSubtitle { get; }

    string SidebarStatusText { get; }

    string SidebarPrimaryHeader { get; }

    ObservableCollection<ChannelFactItem> SidebarPrimaryItems { get; }

    string SidebarSecondaryHeader { get; }

    ObservableCollection<string> SidebarSecondaryItems { get; }

    bool CanActivateSidebarSecondaryItem { get; }

    void ActivateSidebarSecondaryItem(string item);

    void InsertSidebarSecondaryItem(string item);
}
