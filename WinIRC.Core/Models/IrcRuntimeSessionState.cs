using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace WinIRC.Core.Models;

public sealed class IrcRuntimeSessionState : INotifyPropertyChanged
{
    private string _topic = string.Empty;
    private string _connectionState = "Disconnected";
    private string _nickname = string.Empty;
    private string _motd = string.Empty;
    private bool _isConnected;
    private int _unreadCount;
    private bool _hasUnreadMention;
    private DateTimeOffset? _lastReadTimestamp;
    private Action<DateTimeOffset?>? _persistLastRead;
    private int _updateDepth;
    private bool _hasDeferredChanges;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ServerName { get; set; } = string.Empty;

    public string ChannelName { get; set; } = string.Empty;

    public RangeObservableCollection<IrcRuntimeMessage> Messages { get; } = new();

    public RangeObservableCollection<string> Participants { get; } = new();

    public string Topic
    {
        get => _topic;
        internal set
        {
            if (string.Equals(_topic, value, StringComparison.Ordinal))
            {
                return;
            }

            _topic = value;
            OnPropertyChanged();
        }
    }

    public string ConnectionState
    {
        get => _connectionState;
        internal set
        {
            if (string.Equals(_connectionState, value, StringComparison.Ordinal))
            {
                return;
            }

            _connectionState = value;
            OnPropertyChanged();
        }
    }

    public bool IsConnected
    {
        get => _isConnected;
        internal set
        {
            if (_isConnected == value)
            {
                return;
            }

            _isConnected = value;
            OnPropertyChanged();
        }
    }

    public string Nickname
    {
        get => _nickname;
        internal set
        {
            if (string.Equals(_nickname, value, StringComparison.Ordinal))
            {
                return;
            }

            _nickname = value;
            OnPropertyChanged();
        }
    }

    public string Motd
    {
        get => _motd;
        internal set
        {
            if (string.Equals(_motd, value, StringComparison.Ordinal))
            {
                return;
            }

            _motd = value;
            OnPropertyChanged();
        }
    }

    public int UnreadCount
    {
        get => _unreadCount;
        private set
        {
            if (_unreadCount == value)
            {
                return;
            }

            _unreadCount = value;
            OnPropertyChanged();
        }
    }

    public bool HasUnreadMention
    {
        get => _hasUnreadMention;
        private set
        {
            if (_hasUnreadMention == value)
            {
                return;
            }

            _hasUnreadMention = value;
            OnPropertyChanged();
        }
    }

    internal void ReplaceMessages(IEnumerable<IrcRuntimeMessage> messages)
    {
        Messages.ReplaceRange(messages);
    }

    internal void AddMessage(IrcRuntimeMessage message)
    {
        Messages.Add(message);
    }

    internal void ConfigureReadTracking(DateTimeOffset? lastReadTimestamp, Action<DateTimeOffset?> persistLastRead)
    {
        _lastReadTimestamp = lastReadTimestamp;
        _persistLastRead = persistLastRead;
    }

    internal void TrackUnread(IrcRuntimeMessage message)
    {
        if (message.IsStatus || message.Kind == IrcRuntimeMessageKind.JoinPart)
        {
            return;
        }

        if (_lastReadTimestamp.HasValue && message.Timestamp <= _lastReadTimestamp.Value)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(Nickname)
            && string.Equals(message.Author, Nickname, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        UnreadCount++;
        if (message.IsMention)
        {
            HasUnreadMention = true;
        }
    }

    public void MarkRead()
    {
        _lastReadTimestamp = Messages.Count > 0
            ? Messages[Messages.Count - 1].Timestamp
            : DateTimeOffset.UtcNow;

        UnreadCount = 0;
        HasUnreadMention = false;
        _persistLastRead?.Invoke(_lastReadTimestamp);
    }

    internal void RemoveMessageAt(int index)
    {
        if (index >= 0 && index < Messages.Count)
        {
            Messages.RemoveAt(index);
        }
    }

    internal void ResetParticipants(IEnumerable<string> participants)
    {
        var nextParticipants = participants
            .Where(participant => !string.IsNullOrWhiteSpace(participant))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (Participants.SequenceEqual(nextParticipants))
        {
            return;
        }

        Participants.ReplaceRange(nextParticipants);
    }

    public RangeObservableCollection<string> JoinedTargets { get; } = new();

    internal void ResetJoinedTargets(IEnumerable<string> joinedTargets)
    {
        var nextTargets = joinedTargets
            .Where(target => !string.IsNullOrWhiteSpace(target))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (JoinedTargets.SequenceEqual(nextTargets))
        {
            return;
        }

        JoinedTargets.ReplaceRange(nextTargets);
    }

    internal void RunBatchUpdate(Action update)
    {
        _updateDepth++;
        try
        {
            update();
        }
        finally
        {
            _updateDepth--;
            if (_updateDepth == 0 && _hasDeferredChanges)
            {
                _hasDeferredChanges = false;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        if (_updateDepth > 0)
        {
            _hasDeferredChanges = true;
            return;
        }

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
