#include "CppCliEngineAdapter.h"
#include "engine_api.h"
#include <msclr/marshal_cppstd.h>
#include <vcclr.h>

namespace Engine::Adapter::CppCli
{
    ref class ProcessingOperation;

    // Native callbacks receive only a void* context. gcroot keeps the managed
    // ProcessingOperation reachable while Engine_Process is running and lets
    // each callback return to the correct managed operation.
    struct NativeCallbackContext
    {
        explicit NativeCallbackContext(ProcessingOperation^ value)
            : operation(value)
        {
        }

        gcroot<ProcessingOperation^> operation;
    };

    void __cdecl OnNativeProgress(
        int percent,
        const char* message,
        void* context);

    void __cdecl OnNativeCompletion(
        int success,
        const char* outputPath,
        void* context);

    /// <summary>
    /// Holds all state for one call to Engine_Process.
    ///
    /// Keeping this state in a per-call object makes callback routing explicit
    /// and keeps the public adapter focused on implementing IProcessingEngine.
    /// </summary>
    ref class ProcessingOperation sealed
    {
    public:
        ProcessingOperation(
            Engine::Contracts::ProcessingRequest^ request,
            System::IProgress<Engine::Contracts::ProcessingProgress^>^ progress,
            System::Threading::CancellationToken cancellationToken,
            Engine::Contracts::IEngineEventBus^ eventBus)
            : _request(request),
              _progress(progress),
              _cancellationToken(cancellationToken),
              _eventBus(eventBus),
              _completedSuccessfully(false)
        {
        }

        Engine::Contracts::ProcessingResult^ Run()
        {
            // Lifecycle events are managed application concerns. The native
            // engine reports processing data but does not know about the event bus.
            Publish(
                Engine::Contracts::EngineEventType::Started,
                "Processing started.");

            try
            {
                // Convert managed strings to native storage. These std::string
                // instances remain alive for the entire synchronous native call.
                std::string inputPath =
                    msclr::interop::marshal_as<std::string>(
                        _request->InputPath);
                std::string outputPath = _request->OutputPath == nullptr
                    ? std::string()
                    : msclr::interop::marshal_as<std::string>(
                        _request->OutputPath);

                // Map the managed ProcessingRequest to the small C-compatible
                // request DTO used at the DLL boundary. The DTO borrows these
                // string pointers; the native engine must not retain them.
                EngineRequestDto nativeRequest{};
                nativeRequest.inputPath = inputPath.c_str();
                nativeRequest.outputPath =
                    outputPath.empty() ? nullptr : outputPath.c_str();

                // context is stack allocated safely because Engine_Process is
                // synchronous and all callbacks finish before this method returns.
                NativeCallbackContext context(this);
                int result = Engine_Process(
                    &nativeRequest,
                    OnNativeProgress,
                    OnNativeCompletion,
                    &context);

                if (result == -1)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    throw gcnew System::OperationCanceledException();
                }

                if (result != 0 || !_completedSuccessfully)
                {
                    System::String^ message = System::String::Format(
                        "Native processing failed with code {0}.",
                        result);
                    Publish(
                        Engine::Contracts::EngineEventType::Error,
                        message);
                    return gcnew Engine::Contracts::ProcessingResult(
                        false,
                        nullptr,
                        message);
                }

                Publish(
                    Engine::Contracts::EngineEventType::Completed,
                    "Processing completed.");

                return gcnew Engine::Contracts::ProcessingResult(
                    true,
                    _completedOutputPath,
                    nullptr);
            }
            catch (System::OperationCanceledException^)
            {
                Report(0, "Cancelled");
                Publish(
                    Engine::Contracts::EngineEventType::Cancelled,
                    "Processing cancelled.");
                throw;
            }
        }

        void HandleNativeProgress(int percent, const char* message)
        {
            // CancellationToken is a managed concept. Translate it into the
            // native engine's cancellation function at the callback boundary.
            if (_cancellationToken.IsCancellationRequested)
            {
                Engine_Cancel();
                return;
            }

            Report(percent, gcnew System::String(message));
        }

        void HandleNativeCompletion(int success, const char* outputPath)
        {
            // Copy callback strings immediately. Native callback memory is not
            // assumed to remain valid after the callback returns.
            _completedSuccessfully = success == 1;
            _completedOutputPath = outputPath == nullptr
                ? nullptr
                : gcnew System::String(outputPath);
        }

    private:
        Engine::Contracts::ProcessingRequest^ _request;
        System::IProgress<Engine::Contracts::ProcessingProgress^>^ _progress;
        System::Threading::CancellationToken _cancellationToken;
        Engine::Contracts::IEngineEventBus^ _eventBus;
        bool _completedSuccessfully;
        System::String^ _completedOutputPath;

        void Report(int percent, System::String^ message)
        {
            if (_progress != nullptr)
            {
                _progress->Report(
                    gcnew Engine::Contracts::ProcessingProgress(
                        percent,
                        message));
            }
        }

        void Publish(
            Engine::Contracts::EngineEventType type,
            System::String^ message)
        {
            _eventBus->Publish(
                gcnew Engine::Contracts::EngineEvent(
                    type,
                    message,
                    System::DateTimeOffset::Now));
        }
    };

    void __cdecl OnNativeProgress(
        int percent,
        const char* message,
        void* context)
    {
        // Recover the managed operation associated with this native call.
        auto callbackContext =
            static_cast<NativeCallbackContext*>(context);
        ProcessingOperation^ operation = callbackContext->operation;
        operation->HandleNativeProgress(percent, message);
    }

    void __cdecl OnNativeCompletion(
        int success,
        const char* outputPath,
        void* context)
    {
        auto callbackContext =
            static_cast<NativeCallbackContext*>(context);
        ProcessingOperation^ operation = callbackContext->operation;
        operation->HandleNativeCompletion(success, outputPath);
    }

    CppCliEngineAdapter::CppCliEngineAdapter(
        Engine::Contracts::IEngineEventBus^ eventBus)
        : _eventBus(eventBus)
    {
        if (eventBus == nullptr)
            throw gcnew System::ArgumentNullException("eventBus");
    }

    System::Threading::Tasks::Task<Engine::Contracts::ProcessingResult^>^
        CppCliEngineAdapter::ProcessAsync(
            Engine::Contracts::ProcessingRequest^ request,
            System::IProgress<Engine::Contracts::ProcessingProgress^>^ progress,
            System::Threading::CancellationToken cancellationToken)
    {
        if (request == nullptr)
            throw gcnew System::ArgumentNullException("request");

        auto operation = gcnew ProcessingOperation(
            request,
            progress,
            cancellationToken,
            _eventBus);

        // Engine_Process is synchronous, so execute it on the thread pool while
        // preserving the asynchronous contract used by the application layer.
        return System::Threading::Tasks::Task::Run<
            Engine::Contracts::ProcessingResult^>(
                gcnew System::Func<Engine::Contracts::ProcessingResult^>(
                    operation,
                    &ProcessingOperation::Run),
                cancellationToken);
    }
}
