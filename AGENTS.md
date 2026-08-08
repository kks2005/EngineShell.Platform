# Agent Coding Guide

This file defines repository-wide instructions for coding agents. Keep changes
small, readable, and appropriate for an evolving architecture POC. Preserve
existing behavior unless the task explicitly changes it.

## Design intent

EngineShell.Platform demonstrates a portable .NET application core with
replaceable clients and replaceable infrastructure. Framework-specific UI,
native interop, and external-service details must remain outside the portable
application workflows.

Read `docs/architecture.md` before making architectural changes. Use
`docs/processing-and-native-integration.md` for native-boundary work,
`docs/ai-assisted-processing.md` for AI workflows, and
`docs/observability-and-error-handling.md` for failure and tracing behavior.

## Dependency rules

- Keep UI-independent behavior out of client projects.
- Application and Presentation may depend on `Engine.Contracts`; they must not
  depend on a concrete engine adapter.
- Engine adapters implement and depend on engine contracts. They must not
  depend on Application, Presentation, or a client project.
- Select concrete adapters only in executable or test composition roots.
- Keep managed/native DTO conversion, marshaling, callback lifetime management,
  and native error translation inside the adapter boundary.
- Keep native ABI types out of Application, Presentation, and client projects.
- Give non-MVVM clients their own host or interaction layer; do not make them
  depend on MVVM presentation types.
- Keep client-specific navigation, dialogs, dispatchers, lifecycle hooks,
  resources, and controls in Presentation or the corresponding client.

## Application API direction

The intended evolution is a stable, client-independent Application API through
which WPF, WinUI, MAUI, CLI, headless hosts, AI automation, and future clients
invoke use cases.

- Do not introduce this boundary through a broad rewrite. Implement and prove
  one vertical slice at a time while keeping the solution buildable.
- Keep UI commands such as `System.Windows.Input.ICommand` in the MVVM
  presentation layer.
- Application commands are plain .NET intent/request objects and must contain no
  UI-framework, control, dispatcher, dialog, or view-model types.
- View models adapt user interaction to application operations; they must not
  become application-workflow coordinators.
- Application handlers or services own validation, workflow sequencing,
  progress coordination, cancellation, tracing, and failure classification.
- AI tool calls must map to explicitly approved application operations. AI code
  must not invoke engine adapters or native entry points directly.
- Keep application commands immutable where practical. Keep behavior in
  handlers or application services rather than executable command DTOs.
- Avoid building a generic mediator or command framework beyond demonstrated
  use cases. Prefer a few typed, readable contracts.
- Do not remove or rename the existing application service surface until all
  relevant consumers have migrated and tests prove equivalent behavior.

## Results and operational behavior

- Keep results, progress, status/events, cancellation, and failures
  client-independent. Each client decides how to present them.
- Validate assumptions at the earliest layer that owns them, and do not let
  invalid state propagate silently.
- Preserve the original exception as the inner cause when translating an
  unexpected infrastructure failure.
- Avoid logging the same exception at every layer. Log once at the boundary
  that has enough context to act on it.
- Propagate operation/correlation identifiers across application, adapter, and
  client-visible failure reporting.
- Never log secrets or unnecessarily include user content and file data.

## Testing rules

- Add tests at the lowest reliable layer that proves the behavior.
- Use unit tests for application and presentation behavior, integration tests
  for real adapter/native boundaries, headless tests for complete workflows
  without UI, and FlaUI tests only for observable desktop behavior.
- Keep reusable page objects and cross-client UI-test behavior in
  `UiTest.Infrastructure`.
- Maintain logically aligned automation IDs across desktop clients.
- Do not weaken, skip, or delete a failing test merely to make a build pass.
- For regressions, add or update the smallest test that demonstrates the
  intended behavior.

## Build and verification

Use the smallest verification appropriate to the change. The canonical full
Windows build is:

```powershell
msbuild ClientAgnostic.sln /restore /t:Build /p:Configuration=Debug /p:Platform=x64
```

Managed test projects may be run individually with `dotnet test`. Relevant
commands are documented in `README.md`. The repository coverage entry point is:

```powershell
.\scripts\coverage.ps1
```

Use `-SkipUi` when desktop automation is not required and `-SkipBuild` only
after a successful compatible Debug x64 build. UI tests launch desktop
applications and should not be treated as ordinary unit tests.

## Change discipline

- Preserve unrelated user changes in a dirty working tree.
- Keep package versions in `Directory.Packages.props`.
- Follow the nullable and implicit-using defaults in `Directory.Build.props`.
- Prefer dependency injection and late composition over service location or
  hard-coded implementation selection.
- Prefer simple, explicit code over reflection, hidden conventions, or a new
  framework created only for the POC.
- Remove duplication only when the shared abstraction has a clear owner and at
  least one real reuse case.
- Update the relevant documentation when a public workflow, architectural
  boundary, setup step, or known limitation changes.
- State what was verified and disclose any verification that could not be run.
