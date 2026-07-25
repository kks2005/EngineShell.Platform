# Processing and Native Integration

This document describes the processing boundary used by EngineShell.Platform
and the intended bridge between managed application code and an unmanaged
processing engine.

The repository currently implements the managed contracts, application
orchestration, progress flow, cancellation flow, engine event stream, and a
simulated implementation in `PInvokeEngineAdapter`. Native function invocation
and unmanaged callback registration are planned work.

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

The planned P/Invoke adapter translates a native callback API into the managed
contracts:

```text
Managed code                                      Unmanaged code
────────────                                      ──────────────
ProcessingService
    │
    ▼
PInvokeEngineAdapter
    ├── retain callback delegates
    ├── marshal request data
    ├── register callbacks ──────────────────────▶ native engine
    └── invoke native processing                         │
                                                        ├── progress callback
                                                        ├── event callback
                                                        ├── error callback
                                                        └── completion callback
                                                               │
PInvokeEngineAdapter ◀─────────────────────────────────────────┘
    ├── IProgress<ProcessingProgress>.Report(...)
    ├── IEngineEventBus.Publish(...)
    └── TaskCompletionSource.TrySetResult(...)
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
| Lifecycle or diagnostic callback | `IEngineEventBus.Publish` |
| Successful completion callback | Successful `ProcessingResult` |
| Expected engine failure | Failed `ProcessingResult` with `ErrorMessage` |
| Invalid interop state | Managed exception |
| Managed cancellation | Native cancel function or cancellation flag |
| Native cancellation acknowledgement | Cancelled managed task or cancellation result |

The exact mapping depends on the native API. It should be documented beside the
adapter when the native ABI is defined.

## Callback lifetime and memory safety

Native code may retain callback function pointers after the initial P/Invoke
call returns. The adapter must therefore make callback lifetime explicit.

Required rules:

1. Keep every callback delegate strongly referenced while native code may call
   it.
2. Match the native calling convention and parameter layout exactly.
3. Validate pointers, lengths, and string encodings before marshaling.
4. Never allow a managed exception to cross the unmanaged callback boundary.
5. Correlate callbacks with the correct active request.
6. Ignore callbacks safely after cancellation, completion, or disposal.
7. Do not release delegates, handles, or buffers until the native engine
   confirms it no longer uses them.
8. Make completion idempotent because native completion and cancellation may
   race.

Depending on the native API, lifetime management may use:

- Strong delegate fields
- `GCHandle`
- `SafeHandle`
- A request-state object
- An operation identifier
- `TaskCompletionSource<ProcessingResult>`

`SafeHandle` should be preferred for owned native handles because it provides
reliable cleanup during normal disposal and exceptional paths.

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

The event bus does not guarantee UI-thread delivery. A UI subscriber must
marshal notifications to its dispatcher if events can be published from a
native worker thread. This rule becomes important when real native callbacks
replace the current managed simulation.

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

The current `PInvokeEngineAdapter` is managed-only despite its intended adapter
role. It currently:

- Implements `IProcessingEngine`.
- Publishes `Started` and `Completed` events.
- Reports progress from 0% through 100%.
- Observes `CancellationToken`.
- Returns a successful `ProcessingResult`.

This is sufficient to exercise the architecture through unit, headless, WPF,
and WinUI tests without requiring a native binary.

It does not currently:

- Declare or invoke native entry points.
- Register unmanaged callbacks.
- Marshal native request or result structures.
- Own native handles or buffers.
- Translate native error codes.
- Call a native cancellation function.

## Planned native integration

When a native API is available, the recommended sequence is:

1. Document the native ABI, ownership rules, calling convention, and string
   encoding.
2. Add minimal `NativeMethods` declarations.
3. Define callback delegates and native data structures.
4. Introduce a per-operation state object and `TaskCompletionSource`.
5. Implement progress, event, error, and completion callback translation.
6. Implement cancellation and callback shutdown.
7. Add tests using a deterministic native test library.
8. Add stress tests for completion/cancellation races and late callbacks.
9. Verify x64 deployment, native binary discovery, and cleanup.

The public application and presentation contracts should remain stable while
this adapter evolves.
