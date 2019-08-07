using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Exceptions;
using Tweetinvi.Models;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using WinIRC.Net;
using WinIRC.Ui;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TwitterView : Page
    {
        private HyperlinkManager hyperlinkManager;
        private Uri uri;

        public TwitterView()
        {
            this.InitializeComponent();
            this.hyperlinkManager = new HyperlinkManager();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            this.uri = e.Parameter as Uri;

            setupPageAsync();
        }

        private async void setupPageAsync()
        {
            if (new Connection().HasInternetAccess)
            {
                var uriArray = uri.ToString().Split('/');
                // Disable the exception swallowing to allow exception to be thrown by Tweetinvi
                ExceptionHandler.SwallowWebExceptions = false;

                var tweet = await Task.Run(() =>
                {
                    try
                    {
                        ITwitterCredentials credentials;

                        credentials = Auth.SetApplicationOnlyCredentials("eK5wblbCAVkxZlMxCmp8Di1uL", "LHccPuEeF2NcaTi53PXceRFVgZ0o5idgkDv62h9mLcdAdfmJp7", true);
                        Auth.InitializeApplicationOnlyCredentials();

                        var id = long.Parse(uriArray[uriArray.Length - 1].Split('?')[0]);

                        var latestException = ExceptionHandler.GetLastException();
                        if (latestException != null)
                        {
                            Debug.WriteLine(latestException.TwitterDescription);
                            return null;
                        }

                        var t = Tweetinvi.Tweet.GetTweet(id);

                        latestException = ExceptionHandler.GetLastException();
                        if (latestException != null)
                        {
                            Debug.WriteLine(latestException.TwitterDescription);
                            return null;
                        }
                        return t;
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e);

                        var latestException = ExceptionHandler.GetLastException();
                        if (latestException != null)
                        {
                            Debug.WriteLine(latestException.TwitterDescription);
                        }
                    }
                    return null;
                });

                if (tweet != null)
                {
                    Timestamp.Text = tweet.CreatedAt.ToLocalTime().ToString();
                    hyperlinkManager.SetText(TweetParagraph, tweet.Text);
                    UsernameBox.Text = tweet.CreatedBy.Name;
                    Picture.Source = new BitmapImage(new Uri(tweet.CreatedBy.ProfileImageUrl400x400));
                }
                else
                {
                    hyperlinkManager.SetText(TweetParagraph, "Failed to load tweet, try again later");
                    UsernameBox.Text = "Error loading";
                }
            }
        }

        private void TextBlock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private void TextBlock_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor =
                new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
        }

        private void TextBlock_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            Window.Current.CoreWindow.PointerCursor =
                new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
        }
    }
}
