# Port Status

Last updated: 2026-04-04

## Summary

The WinUI 3 desktop port is usable and already supports real IRC connections, saved servers, a server/channel tree, tabbed sessions, settings, and a substantial amount of the original shell behavior. It is not at parity yet, but it is past the bootstrap phase and firmly in iterative fidelity/polish work.

## Ported So Far

### App And Packaging

- new packaged WinUI 3 app project
- side-by-side debug package identity to avoid wiping the original dev package
- standard WinUI startup flow via `App` and `MainWindow`
- custom title bar hosting the menu bar

### Shared Architecture

- `WinIRC.Core` shared library for reusable non-UI logic
- shared workspace/session service
- shared IRC runtime service backed by `Rymate.IrcClientCore`
- shared runtime session state for messages, participants, joined targets, and unread state

### Storage And Migration

- local JSON settings store
- local JSON server repository
- migration from sibling WinIRC package data
- migration support for both current JSON stores and older legacy stores

### Shell And Navigation

- left server/channel tree
- center `TabView` session host
- right session sidebar
- resizable left and right panes
- menu bar in title bar
- `WinIRC`, `View`, and `Settings` menus restored in WinUI form

### Dialogs And Settings

- manage servers dialog
- settings dialog
- settings sections closer to the old UWP structure
- theme, font, message style, and spacing options wired into the live chat surface

### IRC Functionality

- connect to saved servers
- join channels
- open queries
- send normal messages
- `/join`, `/query`, `/msg`, `/part`, `/whois`
- basic moderation/operator actions from the roster menu
- live roster and topic updates
- server/channel session state backed by real transport instead of placeholders

### Chat UI

- compact and modern message layouts
- grouped messages
- nick colouring
- topic bar
- users pane toggle
- unread and mention indicators
- runtime-backed last-read timestamp persistence to avoid marking old reconnect scrollback unread again

## Still To Port / Finish

### UI Fidelity

- more exact parity with old UWP shell density and spacing
- final polish for tree selection, unread visuals, and live sidebar updates
- remaining right-sidebar behavior parity
- more of the original menu actions and command surfaces

### IRC / Session Behavior

- fuller server-buffer behavior review
- more command parity and edge-case handling
- reconnect/auth polish
- better server/channel state transitions during connection changes

### Integration

- remaining protocol activation parity
- more complete notification behavior
- detached window parity review
- viewers/web content parity where still missing

### Cleanup

- remove remaining migration-era scaffolding
- reduce XAML binding warnings
- resolve `APPX2101` packaging warning if possible
- continue moving logic out of WinUI pages into shared/runtime layers where appropriate

## Known Issues

### Active / Recent

- sidebar tree still needs final polish around immediate visual updates when tab selection changes and unread state changes
- some tree and shell refresh paths have historically been sensitive to stale packaged resources or rebuild/deploy mismatch
- there are still several `WMC1506` XAML binding warnings in the WinUI project
- build currently emits a non-blocking `APPX2101` warning about `Package.debug.appxmanifest`
- view of current channel messages does not always update

### Historical Issues Already Addressed

- old package state loss risk from reusing the same package identity during debug deploy
- join-time crash from enumerating a live channel collection while it was mutating
- missing channels in the sidebar tree after reconnect
- stale topic bar text due to `OneTime` binding
- unread tracking living only in open pages instead of shared runtime state

## Current Debugging Guidance

- prefer the side-by-side WinUI debug package in Visual Studio
- do not accept a Visual Studio deploy prompt that says it will uninstall the existing package and delete app state
- if runtime behavior and UI behavior disagree, confirm which packaged output is actually being launched

## Suggested Next Steps

1. Finish the sidebar/tree sync polish.
2. Continue restoring remaining UWP menu and interaction parity.
3. Tighten reconnect/session edge cases.
4. Audit the remaining UWP-only surfaces and decide which are still worth porting exactly.
