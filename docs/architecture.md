# Architecture

EngineShell.Platform separates shared application policy from UI frameworks,
engine technologies, and native implementation details. Stable contracts sit
near the center; executable clients choose concrete technologies at their
composition roots.

## Layer overview

```text
┌──────────────────────────────────────────────────────────────┐
│ WPF Client             WinUI 3 Client             MAUI Client│
│ Platform views, resources, themes, and composition roots     │
└────────────────────────────┬─────────────────────────────────┘
                             ▼
┌──────────────────────────────────────────────────────────────┐
│ Presentation                                                 │
│ Shared MVVM shell, navigation, feature, and status models    │
└────────────────────────────┬─────────────────────────────────┘
                             ▼
┌──────────────────────────────────────────────────────────────┐
│ Application                                                  │◄──── Headless / future
│ ProcessingService | ChatService | tool dispatch | navigation │      non-MVVM hosts
└────────────────────────────┬─────────────────────────────────┘
                             ▼
┌──────────────────────────────────────────────────────────────┐
│ Engine.Contracts                                             │
│ IProcessingEngine | requests | results | progress | events   │
└────────────────────────────┬─────────────────────────────────┘
                             │ runtime implementation selected
                  ┌──────────┼──────────┐
                  ▼          ▼          ▼
               P/Invoke   C++/CLI   Simulator
                  │          │
                  └────┬─────┘
                       ▼
                 Engine.Native
```

Each area has a distinct responsibility:

- **Engine.Contracts** defines the processing boundary without UI or native
  implementation details.
- **Application** coordinates navigation, status, processing, and AI-assisted
  workflows.
- **Presentation** provides shared MVVM view models for data-binding client
  frameworks using CommunityToolkit.Mvvm.
- **Engine.Adapters** maps managed contracts to a concrete engine technology.
- **AI.Adapters** maps an AI provider response into an application-owned plan.
- **Engine.Native** exposes a small C-compatible processing API.
- **Clients** own platform views, resources, and final dependency selection.
- **Tests** provide isolated composition roots and separate logic, integration,
  headless, and desktop automation concerns.

## Dependency inversion

A conventional layered implementation becomes tightly coupled when
application policy references or constructs concrete infrastructure:

```text
ProcessingService → PInvokeEngineAdapter
```

EngineShell reverses that dependency. `ProcessingService` and the shared view
models know only `IProcessingEngine` and DTOs from `Engine.Contracts`.
Concrete engine adapters implement and depend on the same contract without
referencing Application, Presentation, or a client:

```text
                 compile-time dependencies

Presentation ───────► Application ───────► Engine.Contracts
                                              ▲
                                              │ implements
                         ┌────────────────────┼────────────────────┐
                         │                    │                    │
                 P/Invoke adapter      C++/CLI adapter      Simulator adapter
```

The dependency arrows point toward the abstraction rather than from
application policy toward infrastructure. This applies the Dependency
Inversion Principle; dependency injection supplies the Inversion of Control
mechanism.

## Composition roots and late selection

For production execution, each executable client is a composition root and
references both the abstraction and its selected implementation. Tests may
provide their own isolated composition roots.

WPF currently binds P/Invoke at startup:

```csharp
services.AddApplication();
services.AddPresentation(includeAIChat: true);
services.AddSingleton<IProcessingEngine, PInvokeEngineAdapter>();
```

MAUI demonstrates platform-dependent composition by selecting P/Invoke on
Windows and the simulator on other targets. Tests can inject a mock or
controllable implementation through the same contract.

The decision is made as late as possible, when the executable is composed.
Application and Presentation do not change when another adapter is selected.
“Swappable” means replaceable at composition time; the current UI does not
hot-swap engines during an active operation.

## MVVM presentation boundary

The existing Presentation project is intentionally shared by MVVM-capable,
data-binding clients:

```text
Presentation
    ├── ViewModels
    ├── observable state
    ├── commands
    └── navigation models
```

WPF, WinUI 3, and .NET MAUI provide thin platform views over these shared
models. Presentation is UI-framework agnostic within this MVVM family, but it
is not intended to serve every possible client technology.

A web application, API, command-line tool, background worker, or service can
reuse Application, Engine.Contracts, and the selected adapters while supplying
an interaction or presentation layer appropriate to that technology:

```text
                       Application + Engine.Contracts
                                    ▲
             ┌──────────────────────┼──────────────────────┐
             │                      │                      │
     MVVM Presentation       Headless / CLI host    Future API / service
             ▲               own interaction layer    own endpoint layer
       ┌─────┼─────┐
       │     │     │
      WPF  WinUI  MAUI
```

## Headless boundaries

`HeadlessClient` is currently a minimal executable scaffold. Its project
references Application directly and does not reference the shared MVVM
Presentation project. This establishes the intended dependency boundary for a
future CLI, worker, service, or API host, but its executable workflow is not
implemented yet.

`Headless.Tests` serves a separate purpose. It composes shared view models and
application services without creating WPF, WinUI, or MAUI controls. These tests
prove that MVVM workflows can run without a graphical desktop; they do not
replace a presentation-free headless executable.

## Integration boundaries

The engine adapter is the managed/unmanaged anti-corruption boundary. It maps
requests and results, retains callback state, translates progress and
cancellation, and owns deployment of its native runtime dependency. Native
code does not depend on Application or Presentation types.

The AI tool dispatcher applies the same principle to AI input. A provider may
propose a structured action, but only the application validates and invokes an
allowlisted service.

Detailed boundary documentation:

- [Processing and native integration](processing-and-native-integration.md)
- [AI-assisted processing](ai-assisted-processing.md)
- [Observability and error handling](observability-and-error-handling.md)

## Design guidelines

1. Keep UI-independent behavior out of client projects.
2. Make Application and Presentation depend on `Engine.Contracts`, never a
   concrete engine adapter.
3. Keep engine adapters dependent on the contract rather than Application,
   Presentation, or client projects.
4. Select concrete adapters only at executable or test composition roots.
5. Keep managed/native conversion and callback lifetime handling in adapters.
6. Give non-MVVM technologies their own presentation or host layer.
7. Keep cross-client page-object behavior in `UiTest.Infrastructure`.
8. Maintain logically aligned automation IDs across clients.
9. Add tests at the lowest reliable test layer.
10. Keep package versions centralized.

## Current architectural limitations

- The presentation-free `HeadlessClient` workflow is not implemented.
- The gRPC adapter is a scaffold.
- Native cancellation supports one active operation because it uses global
  state.
- Native-adapter lifecycle events do not yet have identical scheduling.
- Runtime engine hot-swapping is not implemented or required by the POC.
