using System;
using System.Collections.Generic;

namespace WinIRC.Core.Models;

public sealed class WorkspaceCommandAction
{
    public WorkspaceCommandActionKind Kind { get; set; } = WorkspaceCommandActionKind.None;

    public IReadOnlyList<string> Targets { get; set; } = Array.Empty<string>();

    public string Message { get; set; } = string.Empty;
}
