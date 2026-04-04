namespace WinIRC.Core.Models;

public sealed class SettingValue
{
    public string Kind { get; set; } = "string";

    public string? StringValue { get; set; }

    public bool? BoolValue { get; set; }

    public int? IntValue { get; set; }

    public static SettingValue FromBoolean(bool value)
    {
        return new SettingValue
        {
            Kind = "boolean",
            BoolValue = value
        };
    }

    public static SettingValue FromInt(int value)
    {
        return new SettingValue
        {
            Kind = "int",
            IntValue = value
        };
    }

    public static SettingValue FromString(string value)
    {
        return new SettingValue
        {
            Kind = "string",
            StringValue = value
        };
    }
}
