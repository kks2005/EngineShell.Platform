using System.Runtime.InteropServices;

namespace Engine.Adapter.PInvoke;

/// <summary>
/// P/Invoke declarations matching the public C API in engine_api.h.
/// </summary>
internal static class NativeMethods
{
    private const string LibraryName = "Engine.Native.dll";

    [StructLayout(LayoutKind.Sequential)]
    internal struct EngineRequestDto
    {
        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string InputPath;

        [MarshalAs(UnmanagedType.LPUTF8Str)]
        public string? OutputPath;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ProgressCallback(
        int percentComplete,
        nint message,
        nint context);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void CompletionCallback(
        int success,
        nint outputPath,
        nint context);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern int Engine_Process(
        ref EngineRequestDto request,
        ProgressCallback onProgress,
        CompletionCallback onCompletion,
        nint context);

    [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
    internal static extern void Engine_Cancel();

    internal static string? ToManagedString(nint value) =>
        value == nint.Zero
            ? null
            : Marshal.PtrToStringUTF8(value);
}
