using System;

namespace WinIRC
{
    public static class Config
    {
        private static Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

        // behaviour settings
        public const string UserListClick = "userlistclick";
        public const string SwitchOnJoin = "switchonjoin";
        public const string UseTabs = "usetabs";
        public const string AlwaysNotify = "alwaysnotify";

        // connection settings
        public const string DefaultUsername = "defaultusername";
        public const string AutoReconnect = "autoreconnect";
        public const string IgnoreSSL = "ignoressl";

        // display settings
        public const string DarkTheme = "darktheme";
        public const string FontFamily = "fontfamily";
        public const string FontSize = "fontsize";
        public const string ReducedPadding = "reducedpadding";
        public const string HideStatusBar = "hidestatusbar";
        public const string IgnoreJoinLeave = "ignorejoinleave";
        public const string Blurred = "blurredback";
        public const string ShowMetadata = "showmetadata";

        // handles for server storage
        public static string ServersStore = "serversstore";
        public static string ServersListStore = "serversliststore";

        public const string FirstRun = "firstrun";
        public const string EnableLogs = "enablelogs";
        public const string LogsFolder = "logsfolder";

        public static string PerChannelSetting(string server, string channel, string key)
        {
            return server + "." + channel + "." + key;
        }

        public static bool Contains(string key)
        {
            return roamingSettings.Values.ContainsKey(key);
        }

        public static void SetString(string key, string value)
        {
            roamingSettings.Values[key] = value;
        }

        public static void SetInt(string key, int value)
        {
            roamingSettings.Values[key] = value;
        }

        public static void SetBoolean(string key, bool value)
        {
            roamingSettings.Values[key] = value;
        }

        public static bool GetBoolean(string key, bool def = false)
        {
            if (Contains(key) && roamingSettings.Values[key] is bool)
            {
                return (bool) roamingSettings.Values[key];
            }
            else
            {
                return def;
            }
        }

        public static string GetString(string key, string def = "")
        {
            if (Contains(key) && roamingSettings.Values[key] is string)
            {
                return roamingSettings.Values[key] as string; 
            }
            else
            {
                return def;
            }
        }

        public static int GetInt(string key, int def = 0)
        {
            if (Contains(key) && roamingSettings.Values[key] is int)
            {
                return (int) roamingSettings.Values[key];
            }
            else
            {
                return def;
            }
        }

        public static void RemoveKey(string key)
        {
            roamingSettings.Values.Remove(key);
        }
    }
}