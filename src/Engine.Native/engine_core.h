#pragma once
#include "engine_api.h"
#include <atomic>

// Internal engine implementation.
// The public C functions in engine_api.h delegate here.

namespace EngineCore
{
    // Set to true to request cancellation of the active operation.
    extern std::atomic<bool> cancelRequested;

    // Run the processing loop.
    // Returns 0 on success, -1 if cancelled.
    int Process(
        const EngineRequestDto* request,
        ProgressCallback   onProgress,
        CompletionCallback onCompletion,
        void*              context);
}
