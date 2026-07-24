# ClientAgnostic

ClientAgnostic is an experimental .NET 9 solution for building multiple user-interface clients on top of a shared application and presentation layer. Native or remote processing engines are isolated behind contracts and adapter projects, allowing clients to select an engine integration without coupling UI code to its implementation.

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

Run all test projects in the solution:

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
