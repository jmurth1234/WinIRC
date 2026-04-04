using System.Collections.Generic;
using WinIRC.Core.Models;

namespace WinIRC.Core.Interfaces;

public interface IWorkspaceSessionService
{
    event System.Action<string>? TargetsChanged;

    void Reset(IEnumerable<SavedServer> servers);

    IReadOnlyList<WorkspaceTarget> GetTargets(string serverName);

    void AddTransientTarget(string serverName, string targetName, bool isDirectMessage);

    void RemoveTransientTarget(string serverName, string targetName);

    void AddTransientTargets(string serverName, IEnumerable<WorkspaceTarget> targets);

    void RemoveTransientTargets(string serverName, IEnumerable<string> targetNames);

    WorkspaceCommandAction InterpretCommand(SavedServer server, string currentTarget, string input);
}
