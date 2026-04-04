using System.Collections.Generic;

namespace WinIRC.Core.Models;

public sealed class SettingsSnapshot
{
    public Dictionary<string, SettingValue> Values { get; set; } = new Dictionary<string, SettingValue>();
}
