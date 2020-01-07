using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace WinIRC.Handlers
{
    class LuaHandler
    {
        private static LuaHandler instance;

        public static LuaHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new LuaHandler();
                }
                return instance;
            }
        }

        private StorageFolder _folder;

        internal async Task<List<String>> GetLuaFiles()
        {
            List<String> files = new List<String>();

            if (_folder == null)
            {
                await GetLuaFolder();
            }

            foreach (StorageFile file in await _folder.GetFilesAsync())
            {
                if (file.FileType == ".lua")
                {
                    files.Add(file.DisplayName);
                }
            }

            return files;
        }

        internal async Task<StorageFolder> GetLuaFolder()
        {
            if (_folder != null)
            {
                return _folder;
            }

            StorageFolder folder;

            if (StorageApplicationPermissions.FutureAccessList.Entries.Count != 0)
            {
                folder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync("PickedFolderToken");
            }
            else
            {
                var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
                folderPicker.CommitButtonText = "Choose this folder";
                folderPicker.FileTypeFilter.Add("*");

                folder = await folderPicker.PickSingleFolderAsync();

                StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
            }
            this._folder = folder;

            return folder;
        }

    }
}
