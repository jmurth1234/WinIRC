using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
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

        private Hyperlink GetHyperLink(string uri)
        {
            if (uri.ToLower().StartsWith("www."))
                uri = "http://" + uri;

            Hyperlink hyper;
            try
            {
                hyper = new Hyperlink();

                var symbol = "";

                var url = new Uri(uri);

                if ((uri.Contains("twitter.com") && uri.Contains("status"))
                    || isImage(uri)
                    || uri.Contains("youtube.com/watch")
                    || uri.Contains("youtu.be"))
                {
                    symbol = "";
                    hyper.Click += Hyper_Click;
                }
                else
                {
                    hyper.NavigateUri = url;
                }

                hyper.Inlines.Add(GetRunControl(uri));

                var symbolRun = GetRunControl(symbol);
                symbolRun.FontFamily = new FontFamily("Segoe MDL2 Assets");
                symbolRun.FontSize--;
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
            return uri.EndsWith(".png")
                || uri.EndsWith(".jpg")
                || uri.EndsWith(".jpeg")
                || uri.EndsWith(".gif");

        }

        // custom handling 
        private void Hyper_Click(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            var uri = new Uri((sender.Inlines[0] as Run).Text);
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
