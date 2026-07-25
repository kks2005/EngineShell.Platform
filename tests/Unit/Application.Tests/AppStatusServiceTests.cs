using EngineShell.Application.Services;

namespace Application.Tests;

[TestClass]
public sealed class AppStatusServiceTests
{
    [TestMethod]
    public void Constructor_UsesExpectedDefaults()
    {
        // Arrange and Act
        var sut = new AppStatusService();

        // Assert
        Assert.AreEqual("Ready", sut.Status);
        Assert.AreEqual(0d, sut.Progress);
    }

    [TestMethod]
    public void Properties_CanBeUpdated()
    {
        // Arrange
        var sut = new AppStatusService();

        // Act
        sut.Status = "Processing";
        sut.Progress = 42.5;

        // Assert
        Assert.AreEqual("Processing", sut.Status);
        Assert.AreEqual(42.5, sut.Progress);
    }
}
