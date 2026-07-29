using EngineShell.Application.AI;
using EngineShell.Application.Interfaces;

namespace EngineShell.Application.Services;

/// <summary>
/// Coordinates AI planning and optional application tool execution.
/// </summary>
public sealed class ChatService(
    IAIService aiService,
    IAIToolDispatcher toolDispatcher) : IChatService
{
    public async Task<ChatResult> SendAsync(
        string userMessage,
        IProgress<ChatProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            throw new ArgumentException("A chat message is required.");
        }

        if (string.Equals(
                userMessage.Trim(),
                AIChatPrompts.WhatCanYouDo,
                StringComparison.OrdinalIgnoreCase))
        {
            return new ChatResult(AIChatPrompts.CapabilitiesHelp);
        }

        progress?.Report(new ChatProgress(
            "Understanding your request..."));

        var plan = await aiService.PlanAsync(
            userMessage,
            cancellationToken);

        if (plan.ToolCall is null)
        {
            return new ChatResult(plan.Message);
        }

        var toolResult = await toolDispatcher.ExecuteAsync(
            plan.ToolCall,
            progress,
            cancellationToken);

        return new ChatResult(toolResult, ToolExecuted: true);
    }
}
