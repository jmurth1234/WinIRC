using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Media;

namespace WinIRC.Ui
{
    public class HyperlinkManager
    { 
        public Action<Uri> LinkClicked { get; internal set; }
        public Uri FirstLink { get; private set; }
        public bool InlineLink { get; private set; }

        private const String linkRegex = @"(http|ftp|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?";

        public void SetText(Paragraph obj, string text)
        {
            var tb = obj as Paragraph;

            if (tb == null)
                return;

            tb.Inlines.Clear();

            if (ContainsHyperLink(text))
                AddInlineControls(tb, SplitSpace(text));
            else
                tb.Inlines.Add(GetRunControl(text));
        }

        private void AddInlineControls(Paragraph textBlock, string[] splittedString)
        {
            for (int i = 0; i < splittedString.Length; i++)
            {
                string tmp = splittedString[i];
                if (ContainsHyperLink(tmp))
                    textBlock.Inlines.Add(GetHyperLink(tmp));
                else
                    textBlock.Inlines.Add(GetRunControl(tmp));
            }
        }

        private string ExtractCleanUrl(string section)
        {
            var matched = Regex.Match(section, linkRegex);
            var urlString = matched.Value;

            return urlString;
        }

        private Hyperlink GetHyperLink(string sectionWithUrl)
        {
            if (sectionWithUrl.ToLower().StartsWith("www."))
                sectionWithUrl = "http://" + sectionWithUrl;

            Hyperlink hyper;
            try
            {
                hyper = new Hyperlink();

                var symbol = "";
                var urlString = ExtractCleanUrl(sectionWithUrl);
                var url = new Uri(urlString);
                if ((urlString.Contains("twitter.com") && urlString.Contains("status"))
                    || isImage(urlString)
                    || urlString.Contains("youtube.com/watch")
                    || urlString.Contains("youtu.be"))
                {
                    symbol = "";
                    hyper.Click += Hyper_Click;
                    InlineLink = true;
                }
                else
                {
                    hyper.NavigateUri = url;
                }

                if (FirstLink == null)
                {
                    FirstLink = url;
                }

                hyper.Inlines.Add(GetRunControl(sectionWithUrl));

                var symbolRun = GetRunControl(symbol);
                symbolRun.FontFamily = new FontFamily("Segoe MDL2 Assets");
                symbolRun.FontSize -= 2;
                hyper.Inlines.Add(symbolRun);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                hyper = new Hyperlink();
            }
            return hyper;
        }

        public static bool isImage(string uri)
        {
            var split = uri.Split('/');
            return uri.EndsWith(".png")
                || uri.EndsWith(".jpg")
                || uri.EndsWith(".jpeg")
                || uri.EndsWith(".gif")
                || (uri.Contains("imgur") && !uri.Contains("/a/") && !(uri.Contains("/r/") && split.Length == 5));

        }

        // custom handling 
        private void Hyper_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            var content = ExtractCleanUrl((sender.Inlines[0] as Run).Text);
            var uri = new Uri(content);
            if (uri != null)
            {
                Debug.WriteLine(uri);
                LinkClicked(uri);
            }
        }

        public static bool ContainsHyperLink(string text)
        {
            return text.ToLower().Contains("http:") || text.ToLower().Contains("www.") || text.ToLower().Contains("https:");
        }

        private static Run GetRunControl(string text)
        {
            Run run = new Run();
            run.Text = text + " ";
            return run;
        }

        private static string[] SplitSpace(string val)
        {
            string[] splittedVal = val.Split(new string[] { " " }, StringSplitOptions.None);
            return splittedVal;
        }
    }
}
