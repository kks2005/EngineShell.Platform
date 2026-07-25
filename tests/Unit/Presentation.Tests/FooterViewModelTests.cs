using Presentation.ViewModels;

namespace Presentation.Tests;

[TestClass]
public sealed class FooterViewModelTests
{
    [TestMethod]
    public void Properties_RaisePropertyChanged()
    {
        // Arrange
        var sut = new FooterViewModel();
        var changedProperties = new List<string?>();
        sut.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        // Act
        sut.Status = "Processing";
        sut.Progress = 25;

        // Assert
        CollectionAssert.Contains(changedProperties, nameof(FooterViewModel.Status));
        CollectionAssert.Contains(changedProperties, nameof(FooterViewModel.Progress));
    }
}
