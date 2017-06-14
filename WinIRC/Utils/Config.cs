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
        public const string AutoReconnect = "autoreconnect";
        public static string IgnoreSSL = "ignoressl";

        // display settings
        public const string DarkTheme = "darktheme";
        public const string FontFamily = "fontfamily";
        public const string FontSize = "fontsize";
        public const string ReducedPadding = "reducedpadding";
        public const string HideStatusBar = "hidestatusbar";
        public const string IgnoreJoinLeave = "ignorejoinleave";

        // handles for server storage
        public static string ServersStore = "serversstore";
        public static string ServersListStore = "serversliststore";

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

        public static bool GetBoolean(string key)
        {
            if (roamingSettings.Values[key] is bool)
            {
                return (bool) roamingSettings.Values[key];
            }
            else
            {
                return false;
            }
        }

        public static string GetString(string key)
        {
            var s = roamingSettings.Values[key] as string;
            if (s != null)
            {
                return s;
            }
            else
            {
                return "";
            }
        }

        public static int GetInt(string key)
        {
            if (roamingSettings.Values[key] is int)
            {
                return (int) roamingSettings.Values[key];
            }
            else
            {
                return 0;
            }
        }

        internal static void RemoveKey(string key)
        {
            roamingSettings.Values.Remove(key);
        }
    }
}