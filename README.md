# Platform Architecture Overview

ClientAgnosticEngine is an experimental .NET 9 solution demonstrating a modular, interface-driven architecture for building multiple UI clients on top of a shared application and presentation layer.

Processing engines — native, managed, or otherwise — are isolated behind contracts and adapter projects, so a client never couples to a specific engine implementation. The same application core can drive WPF, a headless console client, or other UI framework (WinUI, MAUI, Blazor Hybrid), while the engine integration underneath — P/Invoke, C++/CLI, or a test double — is swapped entirely at the composition root.

> This repository is under active development. Several clients and adapters are currently scaffolds rather than production-ready implementations.

## Architecture

```text
Clients
  WPF | WinUI 3 | .NET MAUI | Headless
                    |
              Presentation
                    |
               Application
                    |
             Engine.Contracts
                    |
       +------------+-------------+-------------+
       |            |             |             |
    P/Invoke      C++/CLI        gRPC        Simulator
```
```
Clients (WPF / WinUI / MAUI / Headless)
        ↓
Presentation
        ↓
Application
        ↓
Engine.Contracts
        ↑
Native Engine (external)
```
```
┌──────────────────────────────────────────────────────────────────────────────┐
│                                 CLIENT LAYERS                                │
│                                                                              │
│   WPF Client        WinUI Client        MAUI Client        Headless Client   │
│   - Views/XAML      - Views/XAML        - Pages/XAML       - Console Runner  │
│   - Resources       - Resources         - Resources        - DI Setup        │
│   - Bootstrap       - Bootstrap         - Bootstrap        - Workflow        │
└───────────────────────────────▲──────────────────────────────────────────────┘
                                │
                                │ depends on
                                │
┌────────────────────────────────┴─────────────────────────────────────────────┐
│                               PRESENTATION LAYER                             │
│                               (Shared ViewModels)                            │
│                                                                              │
│   ShellViewModel     HeaderViewModel  ScreensViewModel   ImportViewModel     │
│   GeneralViewModel   FooterViewModel  RenderViewModel                        │
│                                                                              │
│   Depends on: Application Services                           │
└───────────────────────────────▲──────────────────────────────────────────────┘
                                │
                                │ depends on
                                │
┌───────────────────────────────┴──────────────────────────────────────────────┐
│                               APPLICATION LAYER                               │
│                               (Use‑Case Logic)                                │
│                                                                              │
│   Interfaces:                                                                 │
│     IEngineService, INavigationService, IAppStatusService                     │
│     IAppEventService, ISettingsService, IDialogService                        │
│                                                                              │
│   Services:                                                                   │
│     EngineService, NavigationService, AppStatusService                        │
│     AppEventService, SettingsService                                          │
│                                                                              │
│   Depends on:  Engine.Contracts                                   │
└───────────────────────────────▲──────────────────────────────────────────────┘
                                │
                                │ depends on
                                │                                
┌───────────────────────────────┴──────────────────────────────────────────────┐
│                               ENGINE CONTRACTS                                │
│                               (Engine Protocol)                               │
│                                                                              │
│   IEngine, EngineRequest, EngineResult, EngineProgress                        │
│   EngineEventArgs, EngineOperation enum                                       │
│                                                                              │
│   Pure C# — no native code, no UI, no infrastructure                          │
└───────────────────────────────▲──────────────────────────────────────────────┘
                                │
                                │ implemented by
                                │
┌───────────────────────────────┴──────────────────────────────────────────────┐
│                               INFRASTRUCTURE LAYER                           │
│                           (Adapters)                                         │ 
│                                                                              │
│   Engine Adapters:                                                           │
│     PInvokeEngineAdapter                                                     │
│     CppCliEngineAdapter                                                      │
│     GrpcEngineAdapter                                                        │
│     SimulatorEngineAdapter                                                   |
└───────────────────────────────▲──────────────────────────────────────────────┘
                                │
                                │ calls into
                                │
┌───────────────────────────────┴──────────────────────────────────────────────┐
│                               NATIVE ENGINE (C++)                              │
│                                                                              │
│   Core processing, native callbacks, progress events, error events            │
│   Engine lifecycle, memory management                                         │
└──────────────────────────────────────────────────────────────────────────────┘
```

The dependency direction points inward:

- **Engine.Contracts** defines processing requests, results, progress, events, and engine interfaces.
- **Application** coordinates engine operations and application services.
- **Presentation** contains client-neutral view models built with CommunityToolkit.Mvvm.
- **Engine.Adapters** contains replaceable engine integrations.
- **Clients** contains platform-specific composition roots and views.
- **Tests** separates unit, integration, headless, and UI automation coverage.

## Solution layout

```text
src/
├── Application/
├── Engine.Contracts/
├── Presentation/
├── Engine.Adapters/
│   ├── Engine.Adapter.PInvoke/
│   ├── Engine.Adapter.CppCli/
│   ├── Engine.Adapter.Grpc/
│   └── Engine.Adapter.Simulator/
└── Clients/
    ├── WpfClient/
    ├── WinUIClient/
    ├── MauiClient/
    └── HeadlessClient/

tests/
├── Unit/
│   ├── Application.Tests/
│   └── Presentation.Tests/
├── Integration/
├── Headless/
└── UI/
    └── Wpf.UiTests.csproj
```

The solution entry point is [`ClientAgnostic.sln`](ClientAgnostic.sln).

## Current status

| Area | Status |
| --- | --- |
| Engine contracts | Core contracts and reactive engine events are present |
| P/Invoke adapter | Implements the processing contract; native calls are still being developed |
| C++/CLI adapter | Placeholder project and native source files |
| gRPC adapter | Placeholder project and protocol file |
| Simulator adapter | Placeholder |
| WPF client | Application scaffold with shared presentation references |
| WinUI client | Minimal unpackaged, self-contained WinUI 3 application for `win-x64` |
| MAUI client | Multi-target scaffold for Android, iOS, Mac Catalyst, and Windows |
| Headless client | Console scaffold |
| Tests | MSTest projects and UI-test structure are present; coverage is evolving |

## Prerequisites

- Windows 10 version 1809 or newer
- .NET 9 SDK
- Visual Studio 2022 with the relevant workloads:
  - .NET desktop development for WPF
  - Windows application development for WinUI 3
  - .NET Multi-platform App UI development for MAUI
- A C++ desktop workload will be needed when the C++/CLI adapter becomes active

You only need the workloads for the clients or adapters you intend to build.

## Build

Restore and build the complete solution:

```powershell
dotnet restore ClientAgnostic.sln
dotnet build ClientAgnostic.sln
```

Build an individual client when its platform workload is installed:

```powershell
dotnet build src/Clients/WpfClient/WpfClient.csproj
dotnet build src/Clients/WinUIClient/WinUIClient.csproj
dotnet build src/Clients/MauiClient/MauiClient.csproj
dotnet build src/Clients/HeadlessClient/HeadlessClient.csproj
```

The WinUI client is configured as an unpackaged, self-contained `win-x64` application.

## Test
The solution follows a test-pyramid strategy: keep most coverage in fast unit tests and use progressively fewer tests at the broader, slower levels.

```text
              UI              Few, slowest
           Headless
         Integration
            Unit              Many, fastest
```

| Test layer | Purpose |
| --- | --- |
| **Unit** | Tests Application and Presentation logic in isolation. These tests should be fast, deterministic, and make up most of the test suite. |
| **Integration** | Verifies that multiple components, adapters, serialization boundaries, or infrastructure concerns work together. |
| **Headless** | Exercises complete application workflows through the headless client without launching a graphical interface. |
| **UI** | Validates critical user journeys through WPF UI automation with FlaUI. These tests are the slowest and require an interactive Windows desktop session. |

Use UI tests only for high-value user journeys that cannot be covered reliably at a lower level. Business rules and view-model behavior should normally be tested in the Unit projects.

```powershell
dotnet test ClientAgnostic.sln
```

Run one test layer directly:

```powershell
dotnet test tests/Unit/Application.Tests/Application.Tests.csproj
dotnet test tests/Unit/Presentation.Tests/Presentation.Tests.csproj
dotnet test tests/UI/Wpf.UiTests.csproj
```

The WPF UI test project uses FlaUI. UI automation tests must run on an interactive Windows desktop session.

## Dependency management

Shared compiler settings are defined in [`Directory.Build.props`](Directory.Build.props). NuGet versions are managed centrally in [`Directory.Packages.props`](Directory.Packages.props), so individual project files normally contain versionless `PackageReference` entries.

Notable dependencies include:

- CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection
- Microsoft.WindowsAppSDK
- System.Reactive
- MSTest and Moq
- FlaUI for WPF UI automation

## Repository hygiene

Generated build, IDE, test-result, coverage, and publish artifacts are excluded through [`.gitignore`](.gitignore). After cloning, NuGet and build assets are recreated automatically during restore and build.

## Contributing

This solution is currently an architectural prototype. When contributing:

1. Keep UI-independent behavior out of client projects.
2. Depend on `Engine.Contracts` instead of concrete adapters.
3. Register the selected adapter in the client composition root.
4. Add tests in the matching test layer.
5. Keep package versions centralized.
