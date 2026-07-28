#include "engine_core.h"
#include <thread>
#include <chrono>
#include <string>

// -------------------------------------------------------------------
// Internal engine state
// -------------------------------------------------------------------

std::atomic<bool> EngineCore::cancelRequested{ false };

// -------------------------------------------------------------------
// Core processing loop
// -------------------------------------------------------------------

int EngineCore::Process(
    const EngineRequestDto* request,
    ProgressCallback   onProgress,
    CompletionCallback onCompletion,
    void*              context)
{
    if (!request || !request->inputPath || !*request->inputPath)
        return 1;

    cancelRequested = false;

    // Simulate processing in 10% increments.
    // Replace this loop body with real engine work when available.
    for (int percent = 0; percent <= 100; percent += 10)
    {
        if (cancelRequested)
            return -1;

        std::string msg = "Processing " + std::to_string(percent) + "%";

        if (onProgress)
        {
            onProgress(percent, msg.c_str(), context);
        }

        // Simulate work time (skip the final delay at 100%)
        if (percent < 100)
            std::this_thread::sleep_for(std::chrono::milliseconds(100));
    }

    // Derive an output path when the caller did not supply one
    std::string output = request->outputPath && *request->outputPath
        ? request->outputPath
        : std::string(request->inputPath) + ".processed";

    if (onCompletion)
        onCompletion(1, output.c_str(), context);

    return 0;
}

// -------------------------------------------------------------------
// Public C API  (extern "C" declared in engine_api.h)
// -------------------------------------------------------------------

int Engine_Process(
    const EngineRequestDto* request,
    ProgressCallback   onProgress,
    CompletionCallback onCompletion,
    void*              context)
{
    return EngineCore::Process(
        request,
        onProgress,
        onCompletion,
        context);
}

void Engine_Cancel()
{
    EngineCore::cancelRequested = true;
}

const char* Engine_Version()
{
    return "Engine.Native 1.0.0-poc";
}
