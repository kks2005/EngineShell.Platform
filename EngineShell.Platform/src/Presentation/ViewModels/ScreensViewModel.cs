using CommunityToolkit.Mvvm.Input;
using Engine.Contracts;
using EngineShell.Application.Interfaces;

namespace Presentation.ViewModels;
public sealed partial class ScreensViewModel(IAppStatusService status, IProcessingService engineService) : ViewModelBase
{
    private readonly IAppStatusService _status = status;
    private readonly IProcessingService _engineService = engineService;

    [RelayCommand]
    public async Task LoadScreensAsync()
    {
        _status.Status = "Loading screens...";

        var request = new ProcessingRequest("path/to/screens/input", "path/to/screens/output");

        var result = await _engineService.ProcessAsync(request, cancellationToken: CancellationToken.None);

        _status.Status = $"Loaded {result.Success} screens";
    }
}