using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WinIRC.Handlers;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace WinIRC.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ScriptsView : Page
    {
        private StorageFile luaFile;
        private LuaHandler LuaFiles;

        public ScriptsView()
        {
            this.InitializeComponent();
            Code.Text = "\n\n\n";
            AddLineNumbers();

            this.Loaded += ScriptsView_Loaded;
        }

        private async void ScriptsView_Loaded(object sender, RoutedEventArgs e)
        {
            LuaFiles = LuaHandler.Instance;
            var folder = await LuaFiles.GetLuaFolder();

            if (folder == null)
            {
                Code.IsEnabled = false;
                return;
            }

            luaFile = await folder.CreateFileAsync("default.lua", Windows.Storage.CreationCollisionOption.OpenIfExists);

            Code.Text = await FileIO.ReadTextAsync(luaFile);
            ScriptsList.ItemsSource = await LuaFiles.GetLuaFiles();
        }

        private void Code_TextChanged(object sender, TextChangedEventArgs e)
        {
            AddLineNumbers();
        }

        private void AddLineNumbers()
        {
            // set LineNumberTextBox text to null & width to getWidth() function value    
            LineNumbers.Text = "";
            int numLines = Code.Text.Length - Code.Text.Replace("\r", string.Empty).Length;
            // now add each line number to LineNumberTextBox upto last line    
            for (int i = 1; i <= numLines + 1; i++)
            {
                LineNumbers.Text += i + "\n";
            }
        }

        private async void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            await FileIO.WriteTextAsync(luaFile, Code.Text);
        }

        private void ScriptsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
