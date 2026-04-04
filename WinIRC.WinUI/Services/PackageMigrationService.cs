using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using WinIRC.Core;
using WinIRC.Core.Interfaces;
using WinIRC.Core.Models;

namespace WinIRC.WinUI.Services;

public sealed class PackageMigrationService
{
    private static readonly string[] CandidatePackagePrefixes =
    {
        "57221rymate1234.",
        "WinIRC"
    };

    private static readonly string[] CandidateStateFolders =
    {
        "RoamingState",
        "LocalState"
    };

    private static readonly string[] CandidateServerFiles =
    {
        ConfigKeys.ServersListStore,
        ConfigKeys.ServersStore
    };

    private static readonly string[] CandidateJsonServerPaths =
    {
        Path.Combine("LocalState", "State", "servers.json"),
        Path.Combine("RoamingState", "State", "servers.json")
    };

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ISettingsStore _settingsStore;
    private readonly IServerRepository _serverRepository;

    public PackageMigrationService(ISettingsStore settingsStore, IServerRepository serverRepository)
    {
        _settingsStore = settingsStore;
        _serverRepository = serverRepository;
    }

    public async Task MigrateIfNeededAsync()
    {
        var existingServers = await _serverRepository.LoadAsync().ConfigureAwait(false);
        if (!ShouldAttemptMigration(existingServers))
        {
            return;
        }

        var importedSettings = 0;
        var importedServers = 0;
        var importedData = false;

        try
        {
            var roamingValues = LoadCurrentPackageRoamingValues();
            if (roamingValues.Count > 0)
            {
                importedSettings = roamingValues.Count;
                await _settingsStore.ImportAsync(roamingValues).ConfigureAwait(false);
            }

            var legacyServers = LoadLegacyServersFromKnownLocations();
            if (legacyServers.Count > 0)
            {
                var mergedServers = MergeServers(existingServers, legacyServers, out importedServers);
                if (importedServers > 0)
                {
                    await _serverRepository.SaveAsync(mergedServers).ConfigureAwait(false);
                }
            }

            importedData = importedSettings > 0 || importedServers > 0;
        }
        catch
        {
            // Legacy package state is best-effort. Missing or inaccessible package
            // roots should not block the WinUI app from starting with clean local state.
        }

        await _settingsStore.SetBooleanAsync(ConfigKeys.MigrationCompleted, importedData).ConfigureAwait(false);
        await _settingsStore.SetBooleanAsync(ConfigKeys.MigrationImportedData, importedData).ConfigureAwait(false);
        await _settingsStore.SetIntAsync(ConfigKeys.MigrationImportedSettingsCount, importedSettings).ConfigureAwait(false);
        await _settingsStore.SetIntAsync(ConfigKeys.MigrationImportedServersCount, importedServers).ConfigureAwait(false);
    }

    private bool ShouldAttemptMigration(IReadOnlyList<SavedServer> existingServers)
    {
        if (!_settingsStore.GetBoolean(ConfigKeys.MigrationCompleted))
        {
            return true;
        }

        if (_settingsStore.GetBoolean(ConfigKeys.MigrationImportedData))
        {
            return false;
        }

        return existingServers.Count == 0;
    }

    private static IReadOnlyDictionary<string, SettingValue> LoadCurrentPackageRoamingValues()
    {
        var roamingValues = new Dictionary<string, SettingValue>();
        foreach (var pair in Windows.Storage.ApplicationData.Current.RoamingSettings.Values)
        {
            switch (pair.Value)
            {
                case bool boolValue:
                    roamingValues[pair.Key] = SettingValue.FromBoolean(boolValue);
                    break;
                case int intValue:
                    roamingValues[pair.Key] = SettingValue.FromInt(intValue);
                    break;
                case string stringValue:
                    roamingValues[pair.Key] = SettingValue.FromString(stringValue);
                    break;
            }
        }

        return roamingValues;
    }

    private static IReadOnlyList<SavedServer> LoadLegacyServersFromKnownLocations()
    {
        var packageRoots = GetCandidatePackageRoots();
        var dedupedServers = new List<SavedServer>();
        var seenServers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var packageRoot in packageRoots)
        {
            foreach (var stateFolder in CandidateStateFolders)
            {
                foreach (var fileName in CandidateServerFiles)
                {
                    var filePath = Path.Combine(packageRoot, stateFolder, fileName);
                    if (!File.Exists(filePath))
                    {
                        continue;
                    }

                    foreach (var server in ParseLegacyServerFile(filePath))
                    {
                        if (string.IsNullOrWhiteSpace(server.Name) && string.IsNullOrWhiteSpace(server.Hostname))
                        {
                            continue;
                        }

                        if (seenServers.Add(GetServerIdentity(server)))
                        {
                            dedupedServers.Add(server);
                        }
                    }
                }
            }

            foreach (var relativePath in CandidateJsonServerPaths)
            {
                var filePath = Path.Combine(packageRoot, relativePath);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                foreach (var server in ParseJsonServerFile(filePath))
                {
                    if (string.IsNullOrWhiteSpace(server.Name) && string.IsNullOrWhiteSpace(server.Hostname))
                    {
                        continue;
                    }

                    if (seenServers.Add(GetServerIdentity(server)))
                    {
                        dedupedServers.Add(server);
                    }
                }
            }
        }

        return dedupedServers;
    }

    private static IReadOnlyList<SavedServer> ParseJsonServerFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var servers = JsonSerializer.Deserialize<List<SavedServer>>(json, JsonSerializerOptions);
            return servers ?? new List<SavedServer>();
        }
        catch
        {
            return Array.Empty<SavedServer>();
        }
    }

    private static IReadOnlyList<SavedServer> ParseLegacyServerFile(string filePath)
    {
        try
        {
            var document = XDocument.Load(filePath, LoadOptions.None);
            var serverNodes = document
                .Descendants()
                .Where(element => element.Elements().Any(IsServerFieldElement))
                .ToList();

            return serverNodes
                .Select(ParseServerElement)
                .Where(server => server != null)
                .Cast<SavedServer>()
                .ToList();
        }
        catch
        {
            return Array.Empty<SavedServer>();
        }
    }

    private static bool IsServerFieldElement(XElement element)
    {
        return TryGetElementValue(element, "Hostname") != null
            || TryGetElementValue(element, "hostname") != null
            || TryGetElementValue(element, "Channels") != null
            || TryGetElementValue(element, "channels") != null;
    }

    private static SavedServer? ParseServerElement(XElement element)
    {
        var hostname = FirstNonEmpty(
            TryGetElementValue(element, "Hostname"),
            TryGetElementValue(element, "hostname"));

        var name = FirstNonEmpty(
            TryGetElementValue(element, "Name"),
            TryGetElementValue(element, "name"),
            hostname);

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(hostname))
        {
            return null;
        }

        return new SavedServer
        {
            Name = name ?? string.Empty,
            Hostname = hostname ?? string.Empty,
            Port = ParseInt(FirstNonEmpty(
                TryGetElementValue(element, "Port"),
                TryGetElementValue(element, "port")), 6667),
            Ssl = ParseBool(FirstNonEmpty(
                TryGetElementValue(element, "Ssl"),
                TryGetElementValue(element, "ssl"))),
            UseWebSocket = ParseBool(FirstNonEmpty(
                TryGetElementValue(element, "webSocket"),
                TryGetElementValue(element, "WebSocket"))),
            Invalid = ParseBool(TryGetElementValue(element, "invalid")),
            Username = FirstNonEmpty(
                TryGetElementValue(element, "Username"),
                TryGetElementValue(element, "username")) ?? string.Empty,
            Password = FirstNonEmpty(
                TryGetElementValue(element, "Password"),
                TryGetElementValue(element, "password")) ?? string.Empty,
            NickservPassword = FirstNonEmpty(
                TryGetElementValue(element, "NickservPassword"),
                TryGetElementValue(element, "nickservPassword")) ?? string.Empty,
            Channels = FirstNonEmpty(
                TryGetElementValue(element, "Channels"),
                TryGetElementValue(element, "channels")) ?? string.Empty
        };
    }

    private static IReadOnlyList<SavedServer> MergeServers(
        IReadOnlyList<SavedServer> existingServers,
        IReadOnlyList<SavedServer> importedServers,
        out int addedServers)
    {
        var mergedServers = existingServers.ToList();
        var knownServers = new HashSet<string>(
            mergedServers.Select(GetServerIdentity),
            StringComparer.OrdinalIgnoreCase);

        addedServers = 0;
        foreach (var server in importedServers)
        {
            if (!knownServers.Add(GetServerIdentity(server)))
            {
                continue;
            }

            mergedServers.Add(server);
            addedServers++;
        }

        return mergedServers;
    }

    private static IReadOnlyList<string> GetCandidatePackageRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var currentLocalFolder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
            var currentPackageRoot = Directory.GetParent(currentLocalFolder)?.FullName;
            if (!string.IsNullOrWhiteSpace(currentPackageRoot))
            {
                roots.Add(currentPackageRoot);
            }
        }
        catch
        {
            // Accessing packaged app data is best-effort here.
        }

        var packagesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Packages");

        if (!Directory.Exists(packagesDirectory))
        {
            return roots.ToList();
        }

        foreach (var packageRoot in Directory.EnumerateDirectories(packagesDirectory))
        {
            var packageName = Path.GetFileName(packageRoot);
            if (CandidatePackagePrefixes.Any(prefix => packageName.Contains(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                roots.Add(packageRoot);
            }
        }

        return roots.ToList();
    }

    private static string GetServerIdentity(SavedServer server)
    {
        return string.Join("|", server.Name, server.Hostname, server.Port, server.Username);
    }

    private static string? TryGetElementValue(XElement element, string localName)
    {
        return element
            .Elements()
            .FirstOrDefault(child => string.Equals(child.Name.LocalName, localName, StringComparison.OrdinalIgnoreCase))
            ?.Value;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static bool ParseBool(string? value)
    {
        return bool.TryParse(value, out var result) && result;
    }

    private static int ParseInt(string? value, int defaultValue)
    {
        return int.TryParse(value, out var result) ? result : defaultValue;
    }
}
