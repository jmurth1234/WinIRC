namespace WinIRC.Core.Models;

public sealed class WorkspaceTarget
{
    public string Name { get; set; } = string.Empty;

    public bool IsPinned { get; set; }

    public bool IsDirectMessage { get; set; }

    public string DisplayName => Name == "Server"
        ? "Server"
        : IsDirectMessage && !Name.StartsWith("@")
            ? "@" + Name
            : Name;

    public string SecondaryText => IsPinned ? "Saved target" : "Live session";
}
