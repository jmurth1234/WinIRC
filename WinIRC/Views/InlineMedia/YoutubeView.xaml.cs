using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class YoutubeView : Page
    {
        public YoutubeView()
        {
            this.InitializeComponent();
        }

        private Uri uri;

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            this.uri = e.Parameter as Uri;

            var youtubeMatch =
                new Regex(@"youtu(?:\.be|be\.com)/(?:.*v(?:/|=)|(?:.*/)?)([a-zA-Z0-9-_]+)")
                .Match(uri.ToString());

            var videoID = youtubeMatch.Success ? youtubeMatch.Groups[1].Value : string.Empty;

            string html = @"<style>* { margin: 0; padding: 0; }</style><iframe width=""320"" height=""240"" src=""http://www.youtube.com/embed/" + videoID + @"?rel=0"" frameborder=""0"" allowfullscreen></iframe>";

            this.WebView.NavigateToString(html);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }
}
