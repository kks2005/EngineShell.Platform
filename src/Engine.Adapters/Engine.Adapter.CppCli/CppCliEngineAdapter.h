#pragma once

namespace Engine::Adapter::CppCli
{
    /// <summary>
    /// Managed-to-native adapter for the processing engine.
    ///
    /// The application depends only on IProcessingEngine. This adapter translates
    /// that managed contract into calls to the C API exposed by Engine.Native.dll.
    /// It also converts native callbacks back into managed progress and result
    /// objects, and publishes lifecycle events through IEngineEventBus.
    /// </summary>
    public ref class CppCliEngineAdapter sealed
        : Engine::Contracts::IProcessingEngine
    {
    public:
        CppCliEngineAdapter(Engine::Contracts::IEngineEventBus^ eventBus);

        /// <summary>
        /// Runs the synchronous native engine on a worker task so callers can use
        /// the shared asynchronous IProcessingEngine contract without blocking
        /// the UI thread.
        /// </summary>
        virtual System::Threading::Tasks::Task<Engine::Contracts::ProcessingResult^>^
            ProcessAsync(
                Engine::Contracts::ProcessingRequest^ request,
                System::IProgress<Engine::Contracts::ProcessingProgress^>^ progress,
                System::Threading::CancellationToken cancellationToken);

    private:
        Engine::Contracts::IEngineEventBus^ _eventBus;
    };
}
