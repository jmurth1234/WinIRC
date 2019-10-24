using IrcClientCore.Handlers.BuiltIn;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace WinIRC.Views
{
    public sealed partial class ChannelListView : Page
    {
        public ChannelListView(List<ChannelListItem> obj)
        {
            this.InitializeComponent();
            this.Original = obj;
            this.Channels = new ObservableCollection<ChannelListItem>
            (
                from item in Original
                orderby item.Users descending
                select item
            );
        }

        public List<ChannelListItem> Original { get; }
        public ObservableCollection<ChannelListItem> Channels { get; set; }
        public delegate void JoinChannelDelegate(ChannelListItem item);

        public event JoinChannelDelegate JoinChannelClick;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            JoinChannelClick(ChannelsList.SelectedItem as ChannelListItem);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Channels.Clear();
            var list = new List<ChannelListItem>
            (
                from item in Original
                where item.Channel.ToLower().Contains(SearchBox.Text.ToLower())
                orderby item.Users descending
                select item
            );
            list.ForEach(Channels.Add);
        }
    }
}
