using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WinIRC.Core.Interfaces;
using WinIRC.Core.Models;

namespace WinIRC.Core.Services;

public sealed class WorkspaceSessionService : IWorkspaceSessionService
{
    private readonly Dictionary<string, List<WorkspaceTarget>> _targetsByServer = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _notificationSync = new();
    private readonly HashSet<string> _pendingNotifications = new(StringComparer.OrdinalIgnoreCase);
    private bool _notificationScheduled;

    public event Action<string>? TargetsChanged;

    public void Reset(IEnumerable<SavedServer> servers)
    {
        var next = new Dictionary<string, List<WorkspaceTarget>>(StringComparer.OrdinalIgnoreCase);

        foreach (var server in servers)
        {
            var pinnedTargets = BuildPinnedTargets(server);
            if (_targetsByServer.TryGetValue(server.Name, out var existing))
            {
                foreach (var transientTarget in existing.Where(target => !target.IsPinned))
                {
                    if (pinnedTargets.All(target => !NamesMatch(target.Name, transientTarget.Name)))
                    {
                        pinnedTargets.Add(new WorkspaceTarget
                        {
                            Name = transientTarget.Name,
                            IsPinned = false,
                            IsDirectMessage = transientTarget.IsDirectMessage
                        });
                    }
                }
            }

            next[server.Name] = pinnedTargets;
        }

        _targetsByServer.Clear();
        foreach (var pair in next)
        {
            _targetsByServer[pair.Key] = pair.Value;
            QueueTargetsChanged(pair.Key);
        }
    }

    public IReadOnlyList<WorkspaceTarget> GetTargets(string serverName)
    {
        if (_targetsByServer.TryGetValue(serverName, out var targets))
        {
            return targets;
        }

        return Array.Empty<WorkspaceTarget>();
    }

    public void AddTransientTarget(string serverName, string targetName, bool isDirectMessage)
    {
        if (TryAddTransientTarget(serverName, targetName, isDirectMessage))
        {
            QueueTargetsChanged(serverName);
        }
    }

    public void RemoveTransientTarget(string serverName, string targetName)
    {
        if (TryRemoveTransientTarget(serverName, targetName))
        {
            QueueTargetsChanged(serverName);
        }
    }

    public void AddTransientTargets(string serverName, IEnumerable<WorkspaceTarget> targets)
    {
        var changed = false;
        foreach (var target in targets)
        {
            if (string.IsNullOrWhiteSpace(target.Name))
            {
                continue;
            }

            if (TryAddTransientTarget(serverName, target.Name, target.IsDirectMessage))
            {
                changed = true;
            }
        }

        if (changed)
        {
            QueueTargetsChanged(serverName);
        }
    }

    public void RemoveTransientTargets(string serverName, IEnumerable<string> targetNames)
    {
        var changed = false;
        foreach (var targetName in targetNames)
        {
            if (TryRemoveTransientTarget(serverName, targetName))
            {
                changed = true;
            }
        }

        if (changed)
        {
            QueueTargetsChanged(serverName);
        }
    }

    public WorkspaceCommandAction InterpretCommand(SavedServer server, string currentTarget, string input)
    {
        var commandText = input.Trim();
        if (!commandText.StartsWith("/", StringComparison.Ordinal))
        {
            return new WorkspaceCommandAction();
        }

        var parts = commandText.Split(new[] { ' ' }, 2, StringSplitOptions.None)
            .Select(part => part.Trim())
            .ToArray();
        var command = parts[0].ToLowerInvariant();
        var argument = parts.Length > 1 ? parts[1] : string.Empty;

        switch (command)
        {
            case "/join":
            {
                var targets = argument
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(target => target.Trim())
                    .Where(target => target.Length > 0)
                    .ToArray();

                return targets.Length == 0
                    ? Status("Usage: /join #channel")
                    : new WorkspaceCommandAction
                    {
                        Kind = WorkspaceCommandActionKind.OpenChannel,
                        Targets = targets,
                        Message = $"Opened {targets.Length} local channel tab(s): {string.Join(", ", targets)}"
                    };
            }

            case "/query":
            case "/msg":
            {
                var nickname = argument
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(target => target.Trim())
                    .FirstOrDefault();
                return string.IsNullOrWhiteSpace(nickname)
                    ? Status("Usage: /query nickname")
                    : new WorkspaceCommandAction
                    {
                        Kind = WorkspaceCommandActionKind.OpenQuery,
                        Targets = new[] { nickname },
                        Message = $"Opened local direct message session for {nickname}."
                    };
            }

            case "/part":
            {
                return currentTarget.Length == 0
                    ? Status("Use /join from the server session to stage a channel tab.")
                    : new WorkspaceCommandAction
                    {
                        Kind = WorkspaceCommandActionKind.CloseSession
                    };
            }

            default:
                return new WorkspaceCommandAction
                {
                    Kind = WorkspaceCommandActionKind.DeferToTransport
                };
        }
    }

    private static WorkspaceCommandAction Status(string message)
    {
        return new WorkspaceCommandAction
        {
            Kind = WorkspaceCommandActionKind.ShowStatus,
            Message = message
        };
    }

    private static List<WorkspaceTarget> BuildPinnedTargets(SavedServer server)
    {
        var targets = new List<WorkspaceTarget>
        {
            new()
            {
                Name = "Server",
                IsPinned = true
            }
        };

        foreach (var channel in ParseSavedChannels(server.Channels))
        {
            targets.Add(new WorkspaceTarget
            {
                Name = channel,
                IsPinned = true,
                IsDirectMessage = !channel.StartsWith("#", StringComparison.Ordinal)
            });
        }

        return targets;
    }

    private static IEnumerable<string> ParseSavedChannels(string channels)
    {
        return (channels ?? string.Empty)
            .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(channel => channel.Trim())
            .Where(channel => channel.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static bool NamesMatch(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    private bool TryAddTransientTarget(string serverName, string targetName, bool isDirectMessage)
    {
        if (!_targetsByServer.TryGetValue(serverName, out var targets))
        {
            targets = new List<WorkspaceTarget>();
            _targetsByServer[serverName] = targets;
        }

        if (targets.Any(target => NamesMatch(target.Name, targetName)))
        {
            return false;
        }

        targets.Add(new WorkspaceTarget
        {
            Name = targetName,
            IsPinned = false,
            IsDirectMessage = isDirectMessage
        });

        return true;
    }

    private bool TryRemoveTransientTarget(string serverName, string targetName)
    {
        if (!_targetsByServer.TryGetValue(serverName, out var targets))
        {
            return false;
        }

        var target = targets.FirstOrDefault(candidate => NamesMatch(candidate.Name, targetName));
        if (target == null || target.IsPinned)
        {
            return false;
        }

        targets.Remove(target);
        return true;
    }

    private void QueueTargetsChanged(string serverName)
    {
        lock (_notificationSync)
        {
            _pendingNotifications.Add(serverName);
            if (_notificationScheduled)
            {
                return;
            }

            _notificationScheduled = true;
        }

        _ = Task.Run(async () =>
        {
            await Task.Delay(75).ConfigureAwait(false);

            string[] servers;
            lock (_notificationSync)
            {
                servers = _pendingNotifications.ToArray();
                _pendingNotifications.Clear();
                _notificationScheduled = false;
            }

            foreach (var pendingServer in servers)
            {
                TargetsChanged?.Invoke(pendingServer);
            }
        });
    }
}
