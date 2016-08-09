using System;
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
    public class HyperlinkedTextBlock : DependencyObject
    {

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached(
                "Text",
                typeof(string),
                typeof(HyperlinkedTextBlock),
                new PropertyMetadata("", OnInlineListPropertyChanged));

        public static string GetText(TextBlock element)
        {
            if (element != null)
                return element.GetValue(TextProperty) as string;
            return string.Empty;
        }

        public static void SetText(TextBlock element, string value)
        {
            if (element != null)
                element.SetValue(TextProperty, value);
        }

        private static void OnInlineListPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var tb = obj as Paragraph;

            if (tb == null)
                return;

            string text = e.NewValue as string;
            tb.Inlines.Clear();

            if (text.ToLower().Contains("http:") || text.ToLower().Contains("www.") || text.ToLower().Contains("https:"))
                AddInlineControls(tb, SplitSpace(text));
            else
                tb.Inlines.Add(GetRunControl(text));
        }

        private static void AddInlineControls(Paragraph textBlock, string[] splittedString)
        {
            for (int i = 0; i < splittedString.Length; i++)
            {
                string tmp = splittedString[i];
                if (tmp.ToLower().StartsWith("http:") || tmp.ToLower().StartsWith("www.") || tmp.ToLower().Contains("https:"))
                    textBlock.Inlines.Add(GetHyperLink(tmp));
                else
                    textBlock.Inlines.Add(GetRunControl(tmp));
            }
        }

        private static Hyperlink GetHyperLink(string uri)
        {
            if (uri.ToLower().StartsWith("www."))
                uri = "http://" + uri;

            Hyperlink hyper;
            try
            {
                hyper = new Hyperlink();
                hyper.NavigateUri = new Uri(uri);
                hyper.Inlines.Add(GetRunControl(uri));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                hyper = new Hyperlink();
            }
            return hyper;
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
