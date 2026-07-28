# Processing and Native Integration

This document describes the processing boundary used by EngineShell.Platform
and the intended bridge between managed application code and an unmanaged
processing engine.

The repository implements the managed contracts, application orchestration,
progress and cancellation flow, engine lifecycle events, a native C API, and
working P/Invoke and C++/CLI adapters. A managed simulator remains available
for cross-platform clients and tests that do not require native code.

## Overview

The processing design separates three concerns:

1. Presentation requests an application operation.
2. The application layer validates and coordinates that operation.
3. An engine adapter translates the operation into a concrete engine
   implementation.

```text
RenderEngineViewModel
        │
        ▼
IProcessingService
        │
        ▼
IProcessingEngine
        │
        ├── Task<ProcessingResult>
        ├── IProgress<ProcessingProgress>
        ├── CancellationToken
        └── IEngineEventBus
```

This boundary allows the same presentation and application layers to work with
managed simulation, P/Invoke, C++/CLI, gRPC, or a test double.

## Processing pipeline

A normal processing operation follows this path:

```text
User command
    │
    ▼
RenderEngineViewModel.ProcessCommand
    │ creates request, progress receiver, and cancellation source
    ▼
IProcessingService.ProcessAsync
    │ validates application input
    ▼
IProcessingEngine.ProcessAsync
    │ performs or delegates processing
    ├── reports progress while active
    ├── publishes engine lifecycle events
    └── returns the final result
```

The operation uses separate channels because progress, final results, and
engine-wide events have different lifetimes and audiences.

## `IProcessingService`

`IProcessingService` is the application-layer API consumed by view models and
other use cases:

```csharp
public interface IProcessingService
{
    Task<ProcessingResult> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task StopAsync();
}
```

Its responsibilities are:

- Validate application-level input.
- Coordinate the selected processing engine.
- Forward operation progress and cancellation.
- Return an application-facing result.
- Shield presentation code from adapter-specific details.

The service currently validates that `InputPath` is present and delegates to
`IProcessingEngine`. Additional orchestration can be added here without
changing client view models.

Presentation code should depend on `IProcessingService`, not on
`PInvokeEngineAdapter` or another concrete adapter:

```text
Presentation → Application abstraction → Engine abstraction
```

## `IProcessingEngine`

`IProcessingEngine` is the lowest client-neutral engine abstraction:

```csharp
public interface IProcessingEngine
{
    Task<ProcessingResult> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
```

Every engine adapter exposes the same managed contract regardless of its
implementation technology.

| Value | Responsibility |
| --- | --- |
| `ProcessingRequest` | Carries the input path and optional output path |
| `ProcessingResult` | Carries final success, output path, and error information |
| `ProcessingProgress` | Carries an operation percentage and optional message |
| `IProgress<T>` | Delivers progress to the caller's managed context |
| `CancellationToken` | Carries a cancellation request into the adapter |

The asynchronous return type does not require the underlying engine to be
natively asynchronous. An adapter can translate a callback-based or
thread-based native API into a managed `Task`.

## `IEngineEventBus`

`IEngineEventBus` distributes engine events without coupling publishers to
subscribers:

```csharp
public interface IEngineEventBus
{
    IObservable<EngineEvent> Events { get; }

    void Publish(EngineEvent engineEvent);
}
```

The current `EngineEventBus` is implemented with a Reactive Extensions
`Subject<EngineEvent>`.

Supported event categories are:

- `Info`
- `Started`
- `Progress`
- `Completed`
- `Cancelled`
- `Warning`
- `Error`

An adapter publishes an event once, and any interested component may observe
it:

```text
Engine adapter
      │ publishes
      ▼
IEngineEventBus
      ├──▶ status view model
      ├──▶ diagnostic logger
      ├──▶ telemetry
      └──▶ test observer
```

The publisher does not reference any subscriber directly. Subscribers own and
dispose their subscriptions.

## Progress versus engine events

Progress and event-bus messages are related but not interchangeable.

| Mechanism | Scope | Audience | Example |
| --- | --- | --- | --- |
| `IProgress<ProcessingProgress>` | One requested operation | Its caller | `Processing... 60%` |
| `IEngineEventBus` | Engine or application lifetime | Any subscriber | Engine started, warning, diagnostic event |
| `ProcessingResult` | Terminal operation outcome | Its caller | Success and output path |

An operation should use progress for percentage updates and return its final
outcome through `ProcessingResult`. The event bus is appropriate for lifecycle,
diagnostic, warning, and error notifications that may have multiple observers.

Terminal status should come from the final result rather than a late progress
callback. This avoids dispatcher-ordering races in which a queued 100% progress
message overwrites `Completed`.

## Managed-to-unmanaged callback flow

The P/Invoke and C++/CLI adapters translate the native callback API into the
managed contracts:

```text
Managed code                                      Unmanaged code
────────────                                      ──────────────
ProcessingService
    │
    ▼
PInvokeEngineAdapter
    ├── retain callback delegates
    ├── map ProcessingRequest to EngineRequestDto
    ├── register callbacks ──────────────────────▶ native engine
    └── invoke native processing                         │
                                                        ├── progress callback
                                                        └── completion callback
                                                               │
PInvokeEngineAdapter ◀─────────────────────────────────────────┘
    ├── IProgress<ProcessingProgress>.Report(...)
    ├── IEngineEventBus.Publish(...)
    └── create ProcessingResult
            │
            ▼
Task<ProcessingResult>
```

The adapter is an anti-corruption boundary: native calling conventions,
pointers, error codes, callback correlation, and resource ownership do not
escape into the application or presentation layers.

## Callback mapping

| Native behavior | Managed representation |
| --- | --- |
| Progress callback | `IProgress<ProcessingProgress>.Report` |
| Successful completion callback | Successful `ProcessingResult` |
| Expected engine failure | Failed `ProcessingResult` with `ErrorMessage` |
| Invalid interop state | Managed exception |
| Managed cancellation | Native cancel function or cancellation flag |
| Native cancellation acknowledgement | Cancelled managed task or cancellation result |

Lifecycle events are managed application concerns and are published by the
adapters rather than by the native engine.

## Callback lifetime and memory safety

`Engine_Process` is synchronous in the current POC. Callback pointers and
borrowed request strings remain valid only for the duration of that call.
The adapters must still make their lifetime explicit.

Required rules:

1. Keep every callback delegate strongly referenced while native code may call
   it.
2. Match the native calling convention and parameter layout exactly.
3. Validate pointers, lengths, and string encodings before marshaling.
4. Never allow a managed exception to cross the unmanaged callback boundary.
5. Copy callback strings before returning to native code.
6. Keep callback delegates alive until `Engine_Process` returns.
7. Do not retain borrowed native pointers in managed state.

The P/Invoke adapter keeps delegates strongly referenced for the synchronous
call. The C++/CLI adapter uses a `gcroot` callback context to route callbacks
to the correct managed operation.

## Threading and synchronization

Native callbacks may arrive on arbitrary native worker threads.

The adapter should:

- Keep callback work short.
- Copy or marshal native data before the native buffer becomes invalid.
- Avoid accessing WPF or WinUI controls.
- Translate data into managed contract types.
- Report progress or publish events after translation.
- Use thread-safe completion and cancellation state.

`Progress<T>` normally captures the synchronization context on which it is
created. In the current clients, the view model creates it on the UI thread, so
its callback can safely update observable properties.

The adapters run synchronous native work on a worker task. Progress is reported
through the caller-provided `IProgress<T>`, while lifecycle events are
published before and after the awaited worker task so UI subscribers are not
updated directly from the native worker thread.

## Cancellation

Cancellation starts in the presentation layer and travels toward the engine:

```text
Cancel command
    │
    ▼
CancellationTokenSource.Cancel()
    │
    ▼
IProcessingService
    │
    ▼
IProcessingEngine adapter
    │
    ├── call native cancel API, or
    └── set a native cancellation flag
```

The adapter must define when cancellation is complete. Requesting native
cancellation is not necessarily the same as the native operation having
stopped.

Cleanup must wait until native code can no longer invoke callbacks or access
managed-owned buffers. The managed task should complete as cancelled only when
that state is safe and unambiguous.

`IProcessingService.StopAsync` exists in the application contract but is not
implemented by the current `ProcessingService`. Its intended relationship to
per-operation cancellation should be finalized with the native engine
lifecycle.

## Error translation

Interop failures should be translated consistently:

- Expected processing failures become an unsuccessful `ProcessingResult`.
- Cancellation becomes `OperationCanceledException` or a clearly documented
  cancellation result.
- Invalid arguments are rejected before entering native code.
- ABI violations, invalid pointers, or impossible native states become managed
  exceptions.
- Diagnostic details may also be published as `Warning` or `Error` engine
  events.

Errors should not be reported only through the event bus. The caller awaiting
`ProcessAsync` must always receive an unambiguous terminal outcome.

## Current POC implementation

The current native path includes:

- `Engine.Native.dll` with a small C ABI and `EngineRequestDto`.
- Progress and completion callbacks.
- Native cancellation through `Engine_Cancel`.
- `PInvokeEngineAdapter` with UTF-8 marshalling and delegate callbacks.
- `CppCliEngineAdapter` with explicit managed/native conversion and callback
  context lifetime.
- Adapter-owned deployment of `Engine.Native.dll`.
- Focused x64 integration tests for both adapters.

Future native algorithms can evolve behind this boundary without changing the
public application or presentation contracts.
