using System.Threading.Tasks;
using WinIRC.Core.Interfaces;
using WinIRC.Core.Services;
using WinIRC.WinUI.Services;

namespace WinIRC.WinUI;

public sealed class AppBootstrapContext
{
    public ISettingsStore SettingsStore { get; }

    public IServerRepository ServerRepository { get; }

    public INotificationService NotificationService { get; }

    public IProtocolActivationService ProtocolActivationService { get; }

    public IExternalLauncher ExternalLauncher { get; }

    public IFilePickerService FilePickerService { get; }

    public IShareService ShareService { get; }

    public IWindowService WindowService { get; }

    public IWorkspaceSessionService WorkspaceSessions { get; }

    public IIrcRuntimeService IrcRuntime { get; }

    private AppBootstrapContext(
        ISettingsStore settingsStore,
        IServerRepository serverRepository,
        INotificationService notificationService,
        IProtocolActivationService protocolActivationService,
        IExternalLauncher externalLauncher,
        IFilePickerService filePickerService,
        IShareService shareService,
        IWindowService windowService,
        IWorkspaceSessionService workspaceSessions,
        IIrcRuntimeService ircRuntime)
    {
        SettingsStore = settingsStore;
        ServerRepository = serverRepository;
        NotificationService = notificationService;
        ProtocolActivationService = protocolActivationService;
        ExternalLauncher = externalLauncher;
        FilePickerService = filePickerService;
        ShareService = shareService;
        WindowService = windowService;
        WorkspaceSessions = workspaceSessions;
        IrcRuntime = ircRuntime;
    }

    public static async Task<AppBootstrapContext> CreateAsync()
    {
        var settingsStore = new JsonSettingsStore();
        var serverRepository = new JsonServerRepository();
        var notificationService = new DesktopNotificationService();
        var protocolActivationService = new ProtocolActivationService();
        var externalLauncher = new ExternalLauncherService();
        var filePickerService = new FolderPickerService();
        var shareService = new ShareService(notificationService);
        var windowService = new WindowService();
        var workspaceSessions = new WorkspaceSessionService();
        var ircRuntime = new IrcRuntimeService(workspaceSessions, settingsStore);

        await notificationService.InitializeAsync().ConfigureAwait(true);
        await settingsStore.InitializeAsync().ConfigureAwait(true);
        await serverRepository.InitializeAsync().ConfigureAwait(true);

        var migrationService = new PackageMigrationService(settingsStore, serverRepository);
        await migrationService.MigrateIfNeededAsync().ConfigureAwait(true);
        workspaceSessions.Reset(await serverRepository.LoadAsync().ConfigureAwait(true));

        return new AppBootstrapContext(
            settingsStore,
            serverRepository,
            notificationService,
            protocolActivationService,
            externalLauncher,
            filePickerService,
            shareService,
            windowService,
            workspaceSessions,
            ircRuntime);
    }
}
