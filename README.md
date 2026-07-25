# EngineShell.Platform

ClientAgnosticEngine is an experimental .NET 9 solution demonstrating a modular, interface-driven architecture for building multiple UI clients on top of a shared application and presentation layer.

Processing engines — native, managed, or otherwise — are isolated behind contracts and adapter projects, so a client never couples to a specific engine implementation. The same application core can drive WPF, a headless console client, or other UI framework (WinUI, MAUI, Blazor Hybrid), while the engine integration underneath — P/Invoke, C++/CLI, or a test double — is swapped entirely at the composition root.

> This repository is under active development. Several clients and adapters are currently scaffolds rather than production-ready implementations.

## What this POC demonstrates

- Shared MVVM presentation logic across WPF and WinUI 3
- Client-specific XAML, resources, startup, and dependency injection
- Replaceable processing engines behind `IProcessingEngine`
- Reactive engine events and progress reporting
- Unit, integration, headless, and desktop UI test layers
- Shared FlaUI/UIA3 page objects for WPF and WinUI 3
- A logical automation contract that tolerates framework-specific UIA trees
- Failure screenshots and serialized access to the interactive desktop

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
┌──────────────────────────────▼────────────────────────────────┐
│ Presentation                                                  │
│ Shared Shell, Header, Footer, General, Screens, and           │
│ RenderEngine view models                                      │
└──────────────────────────────┬────────────────────────────────┘
                               │
┌──────────────────────────────▼────────────────────────────────┐
│ Application                                                   │
│ ProcessingService | NavigationService | AppStatusService      │
└──────────────────────────────┬────────────────────────────────┘
                               │
┌──────────────────────────────▼────────────────────────────────┐
│ Engine.Contracts                                              │
│ IProcessingEngine | requests | results | progress | events    │
└──────────────────────────────┬────────────────────────────────┘
                               │ implemented by
          ┌────────────────────┼────────────────────┐
          ▼                    ▼                    ▼
       P/Invoke             C++/CLI          gRPC / Simulator
```

Dependency direction points inward:

- **Engine.Contracts** defines the engine protocol without UI or infrastructure
  dependencies.
- **Application** coordinates processing, navigation, and application status.
- **Presentation** contains client-neutral view models built with
  CommunityToolkit.Mvvm.
- **Engine.Adapters** contains replaceable engine integrations.
- **Clients** contains platform-specific views and composition roots.
- **Tests** separates fast logic tests from interactive desktop automation.

## Architecture documentation

- [Processing and native integration](docs/processing-and-native-integration.md)
  explains `IProcessingService`, `IProcessingEngine`, `IEngineEventBus`,
  progress reporting, cancellation, and the planned unmanaged callback bridge.

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
    ├── UiTest.Infrastructure/
    ├── Wpf.UiTests/
    └── WinUI.UiTests/
```

The solution entry point is [`ClientAgnostic.sln`](ClientAgnostic.sln).

## Client implementation

### WPF

The WPF client provides the shared shell workflow with platform-specific views,
resources, dialog integration, and dependency injection.

### WinUI 3

The WinUI client is an unpackaged, self-contained `win-x64` application. It
includes:

- A dependency-injected `ShellView`
- Header navigation bound directly to the shared `HeaderViewModel`
- General, Screens, and Render Engine views
- Shared view-model selection through application-level data templates
- Application-level WinUI styles and accent buttons
- Light and Dark theme selection in the persistent footer

The WPF and WinUI clients intentionally share view models and logical behavior,
not framework-specific controls.

## Current status

| Area | Status |
| --- | --- |
| Engine contracts | Processing contracts, progress models, and reactive engine events implemented |
| P/Invoke adapter | Implements the processing contract with a managed progress simulation; native calls remain future work |
| C++/CLI adapter | Scaffold with native source files |
| gRPC adapter | Scaffold with a protocol file |
| Simulator adapter | Scaffold |
| WPF client | Shared shell, navigation, views, and processing workflow implemented |
| WinUI client | Shared shell workflow, native resources, themes, and views implemented |
| MAUI client | Multi-target application scaffold |
| Headless client | Console client and headless workflow coverage present |
| Tests | Unit, integration, headless, WPF UI, and WinUI UI suites implemented |

## Prerequisites

- Windows 10 version 1809 or newer
- .NET 9 SDK
- Visual Studio 2022 with the workloads needed for the projects you build:
  - .NET desktop development for WPF
  - Windows application development for WinUI 3
  - .NET Multi-platform App UI development for MAUI
  - Desktop development with C++ when the C++/CLI adapter becomes active

## Quick start

Build the implemented Windows clients:

```powershell
dotnet build src/Clients/WpfClient/WpfClient.csproj
dotnet build src/Clients/WinUIClient/WinUIClient.csproj
```

Build the complete solution when all required platform workloads are installed:

```powershell
dotnet restore ClientAgnostic.sln
dotnet build ClientAgnostic.sln
```

Other clients can be built independently:

```powershell
dotnet build src/Clients/MauiClient/MauiClient.csproj
dotnet build src/Clients/HeadlessClient/HeadlessClient.csproj
```

## Testing

The solution follows a test-pyramid strategy:

```text
              UI              Few, slowest, interactive
           Headless
         Integration
            Unit              Many, fastest
```

| Test layer | Purpose |
| --- | --- |
| **Unit** | Tests Application and Presentation logic in isolation |
| **Integration** | Verifies service registration and multi-component workflows |
| **Headless** | Exercises complete workflows without launching a graphical client |
| **UI** | Validates critical WPF and WinUI 3 journeys through FlaUI/UIA3 |

Run non-interactive tests:

```powershell
dotnet test tests/Unit/Application.Tests/Application.Tests.csproj
dotnet test tests/Unit/Presentation.Tests/Presentation.Tests.csproj
dotnet test tests/Integration/Integration.Tests.csproj
dotnet test tests/Headless/Headless.Tests.csproj
```

### Desktop UI automation

UI tests require an unlocked, interactive Windows desktop:

```powershell
dotnet test tests/UI/Wpf.UiTests/Wpf.UiTests.csproj
dotnet test tests/UI/WinUI.UiTests/WinUI.UiTests.csproj
```

The two client suites use the same logical page objects and automation IDs:

```text
UiTest.Infrastructure
├── generic test lifecycle and application fixture contract
├── desktop mutex
├── waits and failure screenshots
├── automation IDs and contract verification
└── shared shell, component, and page objects
```

Each client keeps only its executable launcher and client-specific tests. A
named Windows mutex prevents WPF and WinUI test processes from manipulating the
desktop concurrently.

Current UI coverage includes:

- Application startup
- Shared automation-contract verification
- Header navigation across shared views
- Load-and-process workflow with terminal status and 100% progress
- WinUI Light/Dark theme selection

At the latest verification, all 4 WPF UI tests and all 5 WinUI UI tests passed.

## Code coverage

Collect and merge Application unit, Presentation unit, Integration, Headless,
and WPF UI coverage:

```powershell
.\scripts\coverage.ps1
```

The script restores the repository-local coverage tools, runs each test layer,
and writes three reports:

```text
artifacts/
├── coverage/
│   ├── application-unit/
│   ├── presentation-unit/
│   ├── integration/
│   ├── headless/
│   └── ui/
├── coverage-report/
│   └── index.html
├── ui-coverage-report/
│   └── index.html
└── overall-coverage-report/
    └── index.html
```

`coverage-report` is the fast non-UI baseline, `ui-coverage-report` contains
code exercised through the WPF process, and `overall-coverage-report` merges
every test layer without double-counting production lines.

WPF UI coverage requires an interactive Windows desktop. Generate only the
non-UI report in a non-interactive environment:

```powershell
.\scripts\coverage.ps1 -SkipUi
```

WinUI client-process coverage is not yet included.

## Dependency management

Shared compiler settings are defined in
[`Directory.Build.props`](Directory.Build.props). NuGet versions are managed
centrally in [`Directory.Packages.props`](Directory.Packages.props), so project
files normally contain versionless `PackageReference` entries.

Notable dependencies include:

- CommunityToolkit.Mvvm
- Microsoft.Extensions.DependencyInjection
- Microsoft.WindowsAppSDK
- System.Reactive
- MSTest and Moq
- FlaUI.Core and FlaUI.UIA3

## Contributing

This repository is an architectural POC. When contributing:

1. Keep UI-independent behavior out of client projects.
2. Depend on `Engine.Contracts`, not concrete adapters.
3. Select concrete adapters in each client composition root.
4. Keep WPF and WinUI automation IDs logically aligned.
5. Put cross-client page-object behavior in `UiTest.Infrastructure`.
6. Add tests at the lowest reliable test layer.
7. Keep package versions centralized.

## Notes

Built with assistance from AI coding tools for scaffolding and documentation;
architecture and design decisions are my own.
