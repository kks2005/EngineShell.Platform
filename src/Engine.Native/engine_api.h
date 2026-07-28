#pragma once
#include <stdint.h>

// Public C ABI for Engine.Native.dll
//
// All exports use extern "C" so the names are stable and unmangled.
// This header is included by:
//   - engine_core.cpp   (to implement the exports)
//   - CppCli adapter    (links directly against Engine.Native.lib)
//   - PInvoke adapter   (uses DllImport - this header is for reference only)

#ifdef __cplusplus
extern "C" {
#endif

#ifdef ENGINE_NATIVE_EXPORTS
#define ENGINE_NATIVE_API __declspec(dllexport)
#else
#define ENGINE_NATIVE_API __declspec(dllimport)
#endif

// Called as the engine makes progress. percentComplete: 0-100.
typedef void (*ProgressCallback)(int percentComplete, const char* message, void* context);

// Called once when processing finishes. success: 1=ok, 0=failed.
typedef void (*CompletionCallback)(int success, const char* outputPath, void* context);

// Native representation of a processing request.
// Demonstrates a simple DTO that can cross the C ABI boundary.
typedef struct EngineRequestDto
{
    const char* inputPath;
    const char* outputPath;
} EngineRequestDto;

// Process the request. outputPath may be null; the engine will derive one
// from inputPath.
// Returns 0 on success, -1 if cancelled, non-zero on error.
ENGINE_NATIVE_API int Engine_Process(
    const EngineRequestDto* request,
    ProgressCallback   onProgress,
    CompletionCallback onCompletion,
    void*              context);

// Request cancellation of the running operation.
ENGINE_NATIVE_API void Engine_Cancel();

// Returns a version string for diagnostics.
ENGINE_NATIVE_API const char* Engine_Version();


#ifdef __cplusplus
}
#endif
