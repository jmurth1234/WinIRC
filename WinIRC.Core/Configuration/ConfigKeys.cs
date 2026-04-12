namespace WinIRC.Core.Configuration;

public static class ConfigKeys
{
    public const string UserListClick = "userlistclick";
    public const string SwitchOnJoin = "switchonjoin";
    public const string UseTabs = "usetabs";
    public const string AlwaysNotify = "alwaysnotify";
    public const string AnalyticsAsked = "analyticsasked";
    public const string UploadFile = "uploadfile";
    public const string PasteImage = "pasteimage";
    public const string PasteText = "pastetext";

    public const string DefaultUsername = "defaultusername";
    public const string AutoReconnect = "autoreconnect";
    public const string IgnoreSsl = "ignoressl";

    public const string DarkTheme = "darktheme";
    public const string FontFamily = "fontfamily";
    public const string FontSize = "fontsize";
    public const string ReducedPadding = "reducedpadding";
    public const string HideStatusBar = "hidestatusbar";
    public const string IgnoreJoinLeave = "ignorejoinleave";
    public const string Blurred = "blurredback";
    public const string ShowMetadata = "showmetadata";
    public const string ModernChat = "modernchatstyle";
    public const string ShowUsersPane = "showuserspane";
    public const string ShowTopicBar = "showtopicbar";

    public const string ServersStore = "serversstore";
    public const string ServersListStore = "serversliststore";

    public const string FirstRun = "firstrun";
    public const string EnableLogs = "enablelogs";
    public const string LogsFolder = "logsfolder";
    public const string HideBackgroundTip = "hidebackgroundtip";

    public static string PerChannelSetting(string server, string channel, string key)
    {
        return $"{server}.{channel}.{key}";
    }
}
