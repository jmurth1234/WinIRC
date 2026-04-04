# WinIRC

WinIRC is an IRC client for Windows. This repository currently contains both the original UWP application and an in-progress WinUI 3 desktop port.

## Current Project State

The repository is in a migration phase:

- [`WinIRC`](./WinIRC) is the legacy UWP app and remains the reference implementation.
- [`WinIRC.WinUI`](./WinIRC.WinUI) is the new packaged WinUI 3 desktop app.
- [`WinIRC.Core`](./WinIRC.Core) contains shared models, workspace logic, and the live IRC runtime.

The WinUI app is already capable of:

- loading and saving servers from JSON storage
- migrating app data from prior WinIRC package state
- connecting to real IRC/ZNC servers
- joining channels and opening queries
- rendering live channel transcripts
- showing channel users, topics, and session state
- tracking unread messages and mentions
- using a left server/channel tree with a center tabbed session host

It is not fully at parity yet, but it is beyond the bootstrap stage and is the main focus of active development.

## Repository Layout

- [`WinIRC.sln`](./WinIRC.sln)  
  Main solution containing the UWP app, WinUI app, and shared core library.
- [`WinIRC`](./WinIRC)  
  Legacy UWP application.
- [`WinIRC.Core`](./WinIRC.Core)  
  Shared non-UI logic.
- [`WinIRC.WinUI`](./WinIRC.WinUI)  
  New packaged WinUI 3 desktop app.

## Building From Source

### Recommended Environment

- Windows 11 or a recent Windows 10 build
- Visual Studio 2022 / 2026-era preview tooling with WinUI 3 and Windows App SDK support
- .NET 10 SDK

### Recommended Build Command

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' `
  'C:\Users\Jess\source\repos\WinIRC\WinIRC.WinUI\WinIRC.WinUI.csproj' `
  /restore /t:Build `
  /p:Configuration=Debug `
  /p:Platform=x64 `
  /p:AppxPackage=false `
  /p:UapAppxPackageBuildMode=SideloadOnly
```

You can also build from Visual Studio by opening [`WinIRC.sln`](./WinIRC.sln).

## Debugging Notes

The WinUI debug package is intentionally set up to run side-by-side with the original dev package so local app state is less likely to be lost during deploy/debug.

If Visual Studio shows a dialog saying the existing app will be uninstalled and its state deleted, stop and verify the manifest identity before continuing.

## Features

### Already Present In The Project

- direct IRC and websocket-backed connections
- join and part channels
- `/help`, `/query`, `/whois`, and other IRC commands
- basic moderation commands
- saved servers and configuration
- responsive desktop UI

### WinUI Port Highlights

- packaged WinUI 3 desktop shell
- title-bar `MenuBar`
- resizable left and right panes
- server/channel tree
- session tabs
- live chat transcript with compact and modern message styles
- nick colouring
- unread and mention indicators

## Project Tracking

For current migration status and known issues, see:

- [`PORT_STATUS.md`](./PORT_STATUS.md)
- [`AGENTS.md`](./AGENTS.md)

## Contributing

Contributions are welcome.

The main expectation is to preserve the product's original behavior and UI where practical while moving implementation details over to WinUI 3 and shared core code.
