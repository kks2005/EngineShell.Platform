using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Engine.Contracts;
using EngineShell.Application.Interfaces;

namespace Presentation.ViewModels;

/// <summary>
/// Represents the ViewModel for the Render Engine functionality, handling user interactions and data binding for the associated view.
/// </summary>
public partial class RenderEngineViewModel : ViewModelBase
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
            return;

        _cts = new CancellationTokenSource();

        var progress = new Progress<ProcessingProgress>(p =>
        {
            Progress = p.PercentComplete;
            Status = p.Message ?? string.Empty;
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
                : result.ErrorMessage ?? "Failed";
        }
        catch (OperationCanceledException)
        {
            Status = "Cancelled";
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _subscription.Dispose();
            // Dispose of other managed resources if needed

        }
        base.Dispose(disposing);
    }
}

