using EngineShell.Application.Services;
using Presentation.ViewModels;

namespace Presentation.Tests;

[TestClass]
public sealed class FooterViewModelTests
{
    [TestMethod]
    public void ApplicationStatusChanges_UpdateFooterProperties()
    {
        var status = new AppStatusService();
        using var sut = new FooterViewModel(status);
        var changedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) =>
            changedProperties.Add(args.PropertyName);

        status.Status = "Processing";
        status.Progress = 25;

        Assert.AreEqual("Processing", sut.Status);
        Assert.AreEqual(25d, sut.Progress);
        CollectionAssert.Contains(
            changedProperties,
            nameof(FooterViewModel.Status));
        CollectionAssert.Contains(
            changedProperties,
            nameof(FooterViewModel.Progress));
    }

    [TestMethod]
    public void Dispose_UnsubscribesFromApplicationStatus()
    {
        var status = new AppStatusService();
        var sut = new FooterViewModel(status);
        var changedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) =>
            changedProperties.Add(args.PropertyName);

        sut.Dispose();
        status.Status = "Changed after disposal";

        Assert.AreEqual(0, changedProperties.Count);
    }
}
