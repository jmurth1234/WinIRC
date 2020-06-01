using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace WinIRC.Ui
{
    class PromptDialog
    {
        private ContentDialog dialog;
        private TextBox input;

        public String Result
        {
            get => input.Text;
        }

        public PromptDialog(String title, String text, String placeholder, String confirmButton = "OK", String def = "")
        {
            dialog = new ContentDialog()
            {
                Title = title,
                RequestedTheme = ElementTheme.Dark,
            };

            // Setup Content
            var panel = new StackPanel();

            panel.Children.Add(new TextBlock
            {
                Text = text,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness
                {
                    Bottom = 8,
                },
            });

            input = new TextBox
            {
                PlaceholderText = placeholder,
                Text = def
            };

            panel.Children.Add(input);
            dialog.Content = panel;

            // Add Buttons
            dialog.PrimaryButtonText = confirmButton;
	        dialog.SecondaryButtonText = "Cancel";
        }

        public async Task<ContentDialogResult> Show()
        {
            return await dialog.ShowAsync();
        }
    }
}
