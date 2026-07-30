# Observability and Error Handling

This document describes the WPF-first observability slice in
EngineShell.Platform. The implementation correlates one processing operation
across Application, the selected engine adapter, and the native boundary while
keeping technical details out of the UI.

## Operation flow

```text
Processing operation starts
    → create OperationId
    → open structured logging scope
    → log request and selected adapter
    → log native call and return code
    → log success, expected failure, cancellation, or exception
    → show a friendly UI outcome
    → include OperationId when investigation may be needed
```

All components in the WPF process use `ILogger<T>`. The WPF composition root
selects Serilog as the provider and writes one rolling application log:

```text
%LOCALAPPDATA%\EngineShell.Platform\Logs\WpfClient-<date>.log
```

The log rolls daily or at 10 MB and retains seven files.

## Outcome categories

The workflow distinguishes three anticipated outcomes and one last-resort
condition:

| Outcome | Managed representation | Log level | User experience |
| --- | --- | --- | --- |
| Success | Successful `ProcessingResult` | Information | Completion and output |
| Expected failure | Failed `ProcessingResult` with `ProcessingErrorCode` | Warning | Safe message and reference |
| Cancellation | `OperationCanceledException` | Information | Cancelled status |
| Unexpected failure | `ProcessingOperationException` with `InnerException` | Error | Generic message and reference |
| Unhandled process failure | WPF global exception handler | Critical | Reference and controlled shutdown |

Expected engine failures are data rather than exceptions. Unexpected technical
failures preserve their exception chain.

## Logging ownership

Logging every exception at every layer produces duplicates. Each layer has a
specific role:

```text
Engine.Native
    → returns stable status codes
    → does not expose C++ exceptions across the ABI

PInvokeEngineAdapter
    → logs native call and return details at Debug
    → maps known status codes to ProcessingResult
    → wraps known interop failures and preserves InnerException

ProcessingService
    → owns OperationId and the logging scope
    → logs the terminal operation outcome once
    → wraps unexpected failures with the user-visible reference

ViewModel
    → converts results and correlated exceptions into UI state
    → does not log the same exception again

WPF App
    → logs only otherwise-unhandled failures
```

`ProcessingService` is the operation-level logging boundary because it knows
the request, selected adapter, elapsed time, and final outcome.

## Correlation

An eight-character `OperationId` is created before application validation and
added to the logging scope. Rejected and accepted requests therefore have a
correlated diagnostic record:

```text
OperationId=6a835cd1
InputPath=scene.dat
Engine=PInvokeEngineAdapter
```

The scope flows through the asynchronous adapter call, so P/Invoke debug logs
carry the same properties. Expected failed results receive the same
`OperationId`. Unexpected exceptions are wrapped in
`ProcessingOperationException`, which exposes the reference while retaining
the original exception as `InnerException`.

## What the developer sees

```text
2026-07-29 10:27:31.084 -07:00 [ERR]
EngineShell.Application.Services.ProcessingService
Unexpected processing failure after 42 ms.
{"OperationId":"6a835cd1","InputPath":"scene.dat",
 "Engine":"PInvokeEngineAdapter"}

Engine.Adapter.PInvoke.EngineInteropException:
The Engine.Native processing call failed.
 ---> System.DllNotFoundException: Unable to load Engine.Native.dll
```

The complete managed exception and inner-exception chain is written once.

## What the user sees

Expected failure:

```text
The native engine could not process the file.
Reference: 3b170ce2
```

Unexpected failure:

```text
An unexpected processing error occurred.
Reference: 6a835cd1
```

Cancellation:

```text
Cancelled
```

The Render Engine page uses its existing status area. AI Chat adds the
correlated message to the chat as a system response. Routine operation
failures do not open modal dialogs.

## WPF exception safety net

The WPF composition root observes:

- `DispatcherUnhandledException`
- `TaskScheduler.UnobservedTaskException`
- `AppDomain.UnhandledException`

A dispatcher exception is logged as Critical, shown with a new reference, and
followed by controlled shutdown. Unobserved task and AppDomain handlers are
diagnostic safety nets; they are not substitutes for operation-level handling.

Ollama startup failures are also written to the WPF log. A missing local
installation remains recoverable and does not prevent the main application
from opening.

## Native stack limitation

The managed log can preserve the call path into `Engine_Process`, but it cannot
manufacture the internal C++ stack for a native crash. Native crash diagnosis
still requires:

- Native PDB files
- Mixed managed/native debugging
- Crash dumps
- Symbol-aware dump analysis

Expected native failures should continue to use stable status codes and safe
diagnostic messages. Logging complements native symbols and dumps; it does not
replace them.

## Current scope

- File logging is configured only by `WpfClient`.
- Processing operations are correlated; general navigation events are not.
- P/Invoke boundary diagnostics are implemented. C++/CLI uses the same safe
  result and error-code mapping, while direct C++/CLI debug logging remains a
  future consistency improvement.
- Logs are local only and are not exported to a telemetry service.
- Log level can be configured with `ENGINESHELL_LOG_LEVEL`; sensitive-data
  redaction remains a future production concern.
