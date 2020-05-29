using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using WinIRC.Handlers;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace WinIRC.Ui
{
    public sealed partial class MessageBox : UserControl
    {
        public TextBox Inner => this.msgBox;
        private IrcUiHandler IrcHandler = IrcUiHandler.Instance;

        internal static readonly DependencyProperty ChannelProperty =
            DependencyProperty.Register("Channel", typeof(string), typeof(MessageBox), new PropertyMetadata(null));

        internal static readonly DependencyProperty ServerProperty =
            DependencyProperty.Register("Server", typeof(string), typeof(MessageBox), new PropertyMetadata(null));

        private bool saved;
        private int position;
        private int index;
        private int wordPos;
        private string[] words;
        private string word;
        private string[] possible;

        public string Channel
        {
            get { return (string)GetValue(ChannelProperty); }
            set { SetValue(ChannelProperty, value); }
        }

        public string Server
        {
            get { return (string)GetValue(ServerProperty); }
            set
            {
                SetValue(ServerProperty, value);
            }
        }

        public MessageBox()
        {
            this.InitializeComponent();
            this.Loaded += MessageBox_Loaded;
        }

        private void MessageBox_Loaded(object sender, RoutedEventArgs e)
        {
            var uiMode = UIViewSettings.GetForCurrentView().UserInteractionMode;

            if (uiMode == UserInteractionMode.Touch)
            {
                TabButton.Width = 48;
            }
            else
            {
                TabButton.Width = 0;
            }
        }

        public void ircMsgBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (Server == null || Channel == null || Server == "" || Channel == "")
            {
                return;
            }

            if ((e.Key == Windows.System.VirtualKey.Tab) && (msgBox.Text != ""))
            {
                e.Handled = true;

                this.TabComplete();
                return;
            }

            saved = false;
            index = 0;

            IrcHandler.IrcTextBoxHandler(msgBox, e, Server, Channel);
        }

        private void TabButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (Server == null || Channel == null || Server == "" || Channel == "")
            {
                return;
            }

            this.TabComplete();
        }

        public void TabComplete()
        {
            if (Server == null || Channel == null || Server == "" || Channel == "")
            {
                return;
            }

            if (!this.saved)
            {
                position = msgBox.SelectionStart;
                var newValue = msgBox.Text.Substring(0, this.position) + ' ' + msgBox.Text.Substring(this.position);
                words = msgBox.Text.Split(' ');

                var lcount = 0;
                for (var i = 0; i < this.words.Length; i++)
                {
                    var w = this.words[i];
                    lcount += w.Length + 1;
                    if (lcount >= this.position)
                    {
                        this.word = this.words[i];
                        this.wordPos = i;
                        break;
                    }
                }

                this.saved = true;
                this.possible = IrcHandler.GetTabCompletions(Server, Channel, msgBox.Text, this.word, position);
            }
            else
            {
                this.index++;
            }


            if (this.possible.Length > 0)
            {
                if (this.index >= this.possible.Length) this.index = 0;

                var dupe = this.words.Clone() as string[];
                var completion = this.possible[this.index];
                var prev = "";

                if (dupe.Length > 1)
                {
                    prev = dupe[this.wordPos - 1];
                }

                var res = this.Format(completion, prev, this.wordPos);
                dupe[this.wordPos] = res.word;
                if (res.prev != "") dupe[this.wordPos - 1] = res.prev;

                var newPos = String.Join(" ", dupe.Take(this.wordPos + 1)).Length;
                msgBox.Text = String.Join(" ", dupe);
                msgBox.SelectionStart = newPos;
            }
        }

        private (string word, string prev) Format(string word, string prev, int position)
        {
            if (prev == null) prev = "";

            if (prev.IndexOf("/") != -1 || word.IndexOf("/") != -1)
            {
                return (word, prev);
            }

            if (position == 0 || (position > 0 && prev.IndexOf(",") != -1))
            {
                word = word + ": ";
            }
            else if (position > 0 && prev.IndexOf(":") != -1)
            {
                word = word + ": ";
                prev = prev.Replace(":", ",");
            }
            return (word, prev);
        }

        public async void UploadFile(IInputStream str)
        {
            HttpStreamContent streamfile = new HttpStreamContent(str);
            HttpMultipartFormDataContent httpContents = new HttpMultipartFormDataContent();

            httpContents.Headers.ContentType.MediaType = "multipart/form-data";
            httpContents.Add(streamfile, "file", "upload");

            var client = new HttpClient();
            HttpResponseMessage result = await client.PostAsync(new Uri("https://0x0.st"), httpContents);
            string stringReadResult = await result.Content.ReadAsStringAsync();
            msgBox.Text += stringReadResult;
            msgBox.SelectionStart += stringReadResult.Length;
        }

        private async void msgBox_Paste(object sender, TextControlPasteEventArgs e)
        {
            // Mark the event as handled first. Otherwise, the
            // default paste action will happen, then the custom paste
            // action, and the user will see the text box content change.
            e.Handled = true;

            // Get content from the clipboard.
            var dataPackageView = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Text))
            {
                var text = await dataPackageView.GetTextAsync();

                if (text.Contains('\n'))
                {
                    var messageDialog = new Windows.UI.Popups.MessageDialog(
                        "Looks like you're attempting to post multiline text. Do you want to pastebin it?"
                    );

                    // Add commands to the message dialog.
                    messageDialog.Commands.Add(new UICommand("Pastebin", (command) =>
                    {
                        byte[] byteArray = Encoding.UTF8.GetBytes(text);
                        //byte[] byteArray = Encoding.ASCII.GetBytes(contents);
                        MemoryStream stream = new MemoryStream(byteArray);
                        this.UploadFile(stream.AsInputStream());
                    }));
                    messageDialog.Commands.Add(new UICommand("Cancel", (command) =>
                    {
                        // Cancelled. Do nothing.
                    }));

                    // Set the command that will be invoked by default.
                    messageDialog.DefaultCommandIndex = 0;

                    // Set the command to be invoked when escape is pressed.
                    messageDialog.CancelCommandIndex = 1;

                    // Show the message dialog.
                    await messageDialog.ShowAsync();
                }
                else
                {
                    msgBox.Text += text;
                    msgBox.SelectionStart += text.Length;
                }
            }

            if (dataPackageView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Bitmap))
            {
                var image = await dataPackageView.GetBitmapAsync();

                IRandomAccessStreamWithContentType stream = await image.OpenReadAsync();
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                var pixels = await decoder.GetPixelDataAsync();
                var outStream = new InMemoryRandomAccessStream();

                // Create encoder for PNG
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outStream);

                // Get pixel data from decoder and set them for encoder
                encoder.SetPixelData(decoder.BitmapPixelFormat,
                                     BitmapAlphaMode.Ignore, // Alpha is not used
                                     decoder.OrientedPixelWidth,
                                     decoder.OrientedPixelHeight,
                                     decoder.DpiX, decoder.DpiY,
                                     pixels.DetachPixelData());

                await encoder.FlushAsync(); // Write data to the stream

                this.UploadFile(outStream);
            }
        }
    }
}
