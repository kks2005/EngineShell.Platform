namespace EngineShell.Application.AI;

/// <summary>
/// Built-in prompts shown by chat clients.
/// </summary>
public static class AIChatPrompts
{
    public const string WhatCanYouDo = "What can you do?";
    public const string ProcessSample =
        @"Process C:\Samples\Input.dat";
    public const string ExplainNativeWorkflow =
        "How does the native processing workflow work?";

    public const string CapabilitiesHelp =
        """
        I can:

        • Process a file through the configured native engine.
          Example: Process C:\Samples\Input.dat

        • Explain how the managed application, adapter, and native engine work together.

        Tool requests are validated by the application before they run.
        """;
}
