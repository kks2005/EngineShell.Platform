# AI-Assisted Processing

This document describes the experimental AI Chat workflow in
EngineShell.Platform. The current UI is implemented in the WPF client, while
the orchestration, AI abstraction, tool dispatch, and processing workflow
remain independent of WPF.

The feature uses a locally installed Ollama server and local model. It does not
require a cloud AI API, subscription, or API key.

## Overview

The AI is an interpretation layer in front of existing application services.
It proposes a structured action but cannot invoke the processing engine
directly.

```text
User
  → WPF ChatView
  → ChatViewModel
  → IChatService / ChatService
      → IAIService / OllamaAIService
      → local Ollama at localhost:11434
      ← structured AIPlan
  → IAIToolDispatcher
  → IProcessingService
  → IProcessingEngine / PInvoke adapter
  → Engine.Native
  ← native progress and verified result
  → ChatView
```

This separation keeps AI interpretation outside the trusted execution path:

```text
Ollama proposes
    → application validates
    → application service executes
    → native engine reports
    → UI displays the verified result
```

For details about the processing boundary and native adapters, see
[Processing and native integration](processing-and-native-integration.md).

## Responsibilities

### `ChatView` and `ChatViewModel`

The presentation layer:

- Collects the user's message.
- Provides clickable, editable prompt suggestions.
- Displays deterministic capability help.
- Shows one transient progress bar.
- Supports cancellation.
- Adds only the final response to chat history.

The progress bar is indeterminate while Ollama interprets the request. It
becomes determinate when the native processing pipeline starts reporting
percentages and disappears after completion, cancellation, or failure.

### `IChatService` and `ChatService`

`ChatService` coordinates one chat turn:

1. Return application-owned help for **What can you do?**
2. Ask `IAIService` to interpret other messages.
3. Return a conversational response when no tool is requested.
4. Send a proposed tool call to `IAIToolDispatcher`.
5. Return the verified application result.

It does not contain Ollama HTTP details or call a concrete engine adapter.

### `IAIService` and `OllamaAIService`

`IAIService` is the provider-independent AI boundary. `OllamaAIService`
implements it by calling the local Ollama `/api/chat` endpoint.

The adapter supplies a JSON schema and maps Ollama's response into an
application-owned `AIPlan`. A processing request has this logical shape:

```json
{
  "action": "process_file",
  "message": "Processing the requested file.",
  "inputPath": "C:\\Samples\\Input.dat",
  "outputPath": null
}
```

Ordinary conversation uses `action: "reply"`. Only the known
`process_file` action can enter the tool execution path.

### `IAIToolDispatcher`

The dispatcher is the trust boundary between AI output and application code.
It:

- Rejects unknown tool names.
- Validates required arguments.
- Maps the proposed call to a typed `ProcessingRequest`.
- Calls `IProcessingService`.
- Translates `ProcessingProgress` into chat progress.
- Returns a completion message derived from `ProcessingResult`.

The AI never receives direct access to `IProcessingService`,
`IProcessingEngine`, the dependency-injection container, or arbitrary
application APIs.

### `OllamaLocalHost`

The WPF composition root uses `OllamaLocalHost` during startup. It:

1. Allows only loopback endpoints.
2. Checks the local `/api/tags` endpoint.
3. Runs `ollama serve` if the API is unavailable.
4. Waits briefly for the API to become ready.
5. Lets WPF report a clear installation or startup error.

The client does not stop Ollama when it exits because other local applications
may also be using the server.

## Run locally

### Install Ollama and the model

Install [Ollama for Windows](https://ollama.com/download/windows):

```powershell
irm https://ollama.com/install.ps1 | iex
```

Open a new PowerShell window and download the default model:

```powershell
ollama pull llama3.2
```

Installation and the initial model download require internet access. After the
model is stored locally, this POC can perform inference offline.

Confirm the installation and local model:

```powershell
ollama --version
ollama ls
Invoke-RestMethod http://localhost:11434/api/tags
```

### Start the WPF client

From the repository root:

```powershell
dotnet run --project src/Clients/WpfClient/WpfClient.csproj -p:Platform=x64
```

Open **AI Chat**, select a prompt suggestion, or enter:

```text
Process C:\Samples\Input.dat
```

Suggestions populate the input without sending automatically, allowing paths
and wording to be reviewed before execution.

## Configuration

The local endpoint and model can be configured before launch:

```powershell
$env:OLLAMA_URL = "http://localhost:11434/"
$env:OLLAMA_MODEL = "llama3.2"
```

`OLLAMA_URL` must resolve to a loopback address such as `localhost`,
`127.0.0.1`, or `::1`. Remote endpoints are intentionally rejected by this
POC.

## Failure and cancellation behavior

- Missing Ollama installation: WPF displays an installation message.
- Local API startup failure: WPF reports that Ollama did not become available.
- Missing configured model: the chat displays an Ollama/model availability
  error.
- Invalid or unsupported AI action: the application rejects it before
  processing.
- Cancellation: the token flows through chat, dispatch, processing, the
  adapter, and the native operation.
- Processing failure: the application result is reported instead of an
  AI-generated success claim.

## Tests

The AI slice is covered at its main boundaries:

- Application tests verify chat coordination and allowlisted dispatch.
- Presentation tests verify prompt selection and chat message behavior.
- Workflow tests verify Ollama request/response mapping and local-only host
  enforcement without requiring a live model.

The normal coverage workflow includes these test projects. See the
[Code coverage section](../README.md#code-coverage) for commands and reports.

## Current scope

- Only the WPF client opts into AI Chat.
- Ollama is the only implemented AI provider.
- `process_file` is the only executable AI tool.
- Conversation history is not persisted.
- Progress is displayed as one transient operation status, not chat messages.
- The AI view is a dedicated navigation page.

A future client could host the same `ChatViewModel` in a contextual side panel
and supply active document or image context without changing the existing AI
or processing boundaries.
