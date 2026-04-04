# AGENTS.md

## Project Overview

WinIRC is an IRC client originally built as a UWP app. This repository now contains both:

- the legacy UWP application in [`C:\Users\Jess\source\repos\WinIRC\WinIRC`](C:\Users\Jess\source\repos\WinIRC\WinIRC)
- the legacy UWP application in [`WinIRC`](./WinIRC)
- the new packaged WinUI 3 desktop port in [`WinIRC.WinUI`](./WinIRC.WinUI)
- a shared logic layer in [`WinIRC.Core`](./WinIRC.Core)

The migration strategy is side-by-side replacement, not in-place conversion. The UWP app remains in the solution for reference and parity work while WinUI becomes the primary desktop target.

## Solution Layout

- [`WinIRC.sln`](./WinIRC.sln)
  The main solution containing `WinIRC`, `WinIRC.Core`, and `WinIRC.WinUI`.
- [`WinIRC`](./WinIRC)
  Legacy UWP app. Use this as the behavioral and UI reference.
- [`WinIRC.Core`](./WinIRC.Core)
  Shared cross-UI code. This is where transport/runtime/session-state logic should live where possible.
- [`WinIRC.WinUI`](./WinIRC.WinUI)
  New packaged WinUI 3 desktop app.
- [`artifacts`](./artifacts)
  Build/deploy artifacts used during the migration.

## Current Architecture

### Legacy UWP

The old app uses:

- Template10-era startup patterns
- a heavily customized `Pivot`-based tab/session host
- UWP-specific storage, activation, and UI patterns

Treat the UWP implementation as the truth for:

- interaction model
- menu structure
- shell layout
- command behavior
- visual density

### WinUI Port

The WinUI app currently uses:

- standard WinUI 3 packaged startup
- `MainWindow` hosting `ShellPage`
- `TabView` as the replacement for the old `Pivot` session host
- `ContentDialog`-based settings/server management surfaces
- JSON-backed local settings/server storage
- shared IRC runtime and workspace logic from `WinIRC.Core`

### Shared Core

`WinIRC.Core` currently owns most of the migration-critical shared state:

- saved server models and validation
- workspace/session routing
- shared IRC runtime service
- live IRC session state
- shared settings/repository interfaces

## Build And Run

### Recommended Build Command

Use Visual Studio MSBuild on Windows:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' `
  'C:\Users\Jess\source\repos\WinIRC\WinIRC.WinUI\WinIRC.WinUI.csproj' `
  /restore /t:Build `
  /p:Configuration=Debug `
  /p:Platform=x64 `
  /p:AppxPackage=false `
  /p:UapAppxPackageBuildMode=SideloadOnly
```

### Visual Studio Debugging

The WinUI debug package was intentionally split to a separate identity so it can be deployed side-by-side without wiping the existing dev package data.

Relevant manifest:

- [`WinIRC.WinUI/Package.debug.appxmanifest`](./WinIRC.WinUI/Package.debug.appxmanifest)

Important rule:

- do not redeploy over the original `WinIRC Dev` package if Visual Studio warns that state will be deleted

### Known Build Noise

The WinUI project currently builds with a recurring non-blocking warning:

- `APPX2101` about `Package.debug.appxmanifest`

There are also a number of existing XAML `WMC1506` warnings from `OneWay` bindings.

## Package Identities And App Data

### Legacy / Primary Dev Package

Original dev package state typically lives under:

- `C:\Users\Jess\AppData\Local\Packages\57221rymate1234.WinIRC.Dev_3y0bchp7kwvet`

### Side-By-Side WinUI Debug Package

Current debug package state typically lives under:

- `C:\Users\Jess\AppData\Local\Packages\57221rymate1234.WinIRC.WinUI.Debug_3y0bchp7kwvet`

### Current Storage Format

The WinUI app persists state under `LocalState\State`, including:

- `settings.json`
- `servers.json`

Migration logic also scans sibling WinIRC package folders for prior JSON and legacy stores.

## Important Design Principles

- Keep the WinUI app faithful to the old UWP app unless there is a strong reason not to.
- Prefer restoring original menus, layout, density, and behavior over inventing new shell patterns.
- Keep runtime/session logic in `WinIRC.Core`, not buried in WinUI pages.
- Keep visible UI product-facing. Port notes and migration commentary should not appear in-app.
- Prefer desktop-appropriate replacements for UWP-only APIs, but do not redesign the product during migration.

## What Has Been Ported At A High Level

- packaged WinUI 3 startup
- side-by-side debug deployment identity
- settings and saved-server JSON storage
- migration of prior app data into the WinUI package
- protocol activation plumbing
- left server/channel tree
- center tabbed session host
- right session sidebar
- connect/manage servers dialog
- settings dialog
- live IRC runtime integration via `Rymate.IrcClientCore`
- channel/session transcript rendering
- nick coloring
- compact and modern message styles
- topic bar and users pane toggles
- basic roster context actions
- unread/mention tracking backed by runtime session state

## Current High-Risk / Fragile Areas

- sidebar/tree selection and refresh behavior
- unread badge and selection sync between runtime, tabs, and tree rows
- some remaining XAML binding warnings
- stale packaged resources can still mislead debugging if the wrong output is launched

## Recommended Places To Look First

### WinUI Shell

- [`WinIRC.WinUI/ShellPage.xaml`](./WinIRC.WinUI/ShellPage.xaml)
- [`WinIRC.WinUI/ShellPage.xaml.cs`](./WinIRC.WinUI/ShellPage.xaml.cs)

### WinUI Channel Surface

- [`WinIRC.WinUI/Views/ChannelPage.xaml`](./WinIRC.WinUI/Views/ChannelPage.xaml)
- [`WinIRC.WinUI/Views/ChannelPage.xaml.cs`](./WinIRC.WinUI/Views/ChannelPage.xaml.cs)

### Shared Runtime

- [`WinIRC.Core/Services/IrcRuntimeService.cs`](./WinIRC.Core/Services/IrcRuntimeService.cs)
- [`WinIRC.Core/Models/IrcRuntimeSessionState.cs`](./WinIRC.Core/Models/IrcRuntimeSessionState.cs)
- [`WinIRC.Core/Services/WorkspaceSessionService.cs`](./WinIRC.Core/Services/WorkspaceSessionService.cs)

### Migration / Storage

- [`WinIRC.WinUI/Services/PackageMigrationService.cs`](./WinIRC.WinUI/Services/PackageMigrationService.cs)
- [`WinIRC.WinUI/Services/JsonSettingsStore.cs`](./WinIRC.WinUI/Services/JsonSettingsStore.cs)
- [`WinIRC.WinUI/Services/JsonServerRepository.cs`](./WinIRC.WinUI/Services/JsonServerRepository.cs)

## Practical Guidance For The Next Agent

- Start by checking [`PORT_STATUS.md`](./PORT_STATUS.md).
- Use the UWP app as the fidelity reference before introducing new UI patterns.
- If debugging packaged behavior, be careful to confirm which package identity and output folder are actually being used.
- If Visual Studio offers to uninstall an existing package and delete its state, stop and verify the manifest identity first.
- If a bug only appears in the packaged app, verify you are not looking at stale resources from an older deployment.
