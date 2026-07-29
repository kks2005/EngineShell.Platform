using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EngineShell.Application.AI;
using EngineShell.Application.Interfaces;
using System.Collections.ObjectModel;

namespace Presentation.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
    private readonly IChatService _chatService;
    private CancellationTokenSource? _cancellation;

    [ObservableProperty]
    private string _input = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _progressMessage = string.Empty;

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private bool _isProgressIndeterminate;

    public ObservableCollection<ChatMessage> Messages { get; } =
    [
        new(
            "Assistant",
            "Ask me to process a file, for example: "
            + "\"Process C:\\Samples\\Input.dat\".")
    ];

    public IReadOnlyList<ChatSuggestion> Suggestions { get; } =
    [
        new("What can you do?", AIChatPrompts.WhatCanYouDo),
        new("Process a sample file", AIChatPrompts.ProcessSample),
        new("Explain the native workflow", AIChatPrompts.ExplainNativeWorkflow)
    ];

    public IAsyncRelayCommand SendCommand { get; }
    public IRelayCommand CancelCommand { get; }
    public IRelayCommand<ChatSuggestion> SelectSuggestionCommand { get; }

    public ChatViewModel(IChatService chatService)
    {
        _chatService = chatService;
        SendCommand = new AsyncRelayCommand(SendAsync, CanSend);
        CancelCommand = new RelayCommand(
            Cancel,
            () => IsBusy);
        SelectSuggestionCommand =
            new RelayCommand<ChatSuggestion>(SelectSuggestion);
    }

    partial void OnInputChanged(string value) =>
        SendCommand.NotifyCanExecuteChanged();

    partial void OnIsBusyChanged(bool value)
    {
        SendCommand.NotifyCanExecuteChanged();
        CancelCommand.NotifyCanExecuteChanged();
    }

    private bool CanSend() =>
        !IsBusy && !string.IsNullOrWhiteSpace(Input);

    private async Task SendAsync()
    {
        var userMessage = Input.Trim();
        Input = string.Empty;
        Messages.Add(new ChatMessage("You", userMessage));

        _cancellation = new CancellationTokenSource();
        IsBusy = true;

        try
        {
            var progress = new Progress<ChatProgress>(UpdateProgress);
            var result = await _chatService.SendAsync(
                userMessage,
                progress,
                _cancellation.Token);

            Messages.Add(new ChatMessage("Assistant", result.Message));
        }
        catch (OperationCanceledException)
        {
            Messages.Add(new ChatMessage("System", "Request cancelled."));
        }
        catch (HttpRequestException)
        {
            Messages.Add(new ChatMessage(
                "System",
                "Cannot reach Ollama. Make sure it is running and the "
                + "configured model is available."));
        }
        catch (Exception exception)
        {
            Messages.Add(new ChatMessage("System", exception.Message));
        }
        finally
        {
            _cancellation.Dispose();
            _cancellation = null;
            IsBusy = false;
        }
    }

    private void Cancel() => _cancellation?.Cancel();

    private void UpdateProgress(ChatProgress progress)
    {
        ProgressMessage = progress.Message;
        IsProgressIndeterminate = progress.PercentComplete is null;
        ProgressPercent = progress.PercentComplete ?? 0;
    }

    private void SelectSuggestion(ChatSuggestion? suggestion)
    {
        if (suggestion is not null)
        {
            Input = suggestion.Prompt;
        }
    }
}

public sealed record ChatMessage(
    string Speaker,
    string Text);

public sealed record ChatSuggestion(
    string Label,
    string Prompt);
