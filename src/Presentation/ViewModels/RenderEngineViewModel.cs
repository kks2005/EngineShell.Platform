using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Engine.Contracts;
using EngineShell.Application.Exceptions;
using EngineShell.Application.Interfaces;
using Presentation.Navigation;

namespace Presentation.ViewModels;

/// <summary>
/// Represents the ViewModel for the Render Engine functionality, handling user interactions and data binding for the associated view.
/// </summary>
public partial class RenderEngineViewModel : ViewModelBase, INavigationAware
{
    private readonly IProcessingService _processingService;
    private readonly IEngineEventBus eventBus;
    private readonly IDisposable _subscription;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string? _inputPath;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    private int _progress;

    public string ProgressText => $"{Progress}%";

    [ObservableProperty]
    private string _status = "Ready";

    public IRelayCommand LoadCommand { get; }
    public IAsyncRelayCommand ProcessCommand { get; }
    public IRelayCommand CancelCommand { get; }

    public RenderEngineViewModel(IProcessingService processingService, IEngineEventBus eventBus)
    {
        _processingService = processingService;
        this.eventBus = eventBus;
        LoadCommand = new RelayCommand(Load);
        ProcessCommand = new AsyncRelayCommand(ProcessAsync);
        CancelCommand = new RelayCommand(Cancel);

        _subscription = eventBus.Events.Subscribe(e =>
        {
            Status = e.Message;
        });
    }

    private void Load()
    {
        // TODO: Show OpenFileDialog
        InputPath = @"C:\Samples\Input.dat";
    }

    private async Task ProcessAsync()
    {
        if (string.IsNullOrWhiteSpace(InputPath))
        {
            Status = "Select an input file before processing.";
            return;
        }

        _cts = new CancellationTokenSource();

        var progress = new Progress<ProcessingProgress>(p =>
        {
            Progress = p.PercentComplete;

            if (p.PercentComplete < 100)
            {
                Status = p.Message ?? string.Empty;
            }
        });

        try
        {
            var request = new ProcessingRequest(InputPath);
            var result = await _processingService.ProcessAsync(
                request,
                progress,
                _cts.Token);

            Status = result.Success
                ? "Completed"
                : WithReference(
                    result.ErrorMessage ?? "Processing failed.",
                    result.OperationId);
        }
        catch (OperationCanceledException)
        {
            Status = "Cancelled";
        }
        catch (ProcessingOperationException exception)
        {
            Status = WithReference(
                exception.Message,
                exception.OperationId);
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
        }
    }

    private void Cancel()
    {
        _cts?.Cancel();
    }

    public string? GetNavigationWarning() =>
        ProcessCommand.IsRunning
            ? "Processing is still running. Cancel it and leave this page?"
            : null;

    public async Task OnNavigatedFromAsync(
        CancellationToken cancellationToken = default)
    {
        Cancel();

        if (ProcessCommand.ExecutionTask is { } processing)
        {
            await processing.WaitAsync(cancellationToken);
        }
    }

    public Task OnNavigatedToAsync(
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    private static string WithReference(
        string message,
        string? operationId) =>
        string.IsNullOrWhiteSpace(operationId)
            ? message
            : $"{message} Reference: {operationId}";

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                _cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // The operation completed while disposal was in progress.
            }

            _subscription.Dispose();
        }

        base.Dispose(disposing);
    }
}

