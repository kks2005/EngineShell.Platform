using EngineShell.Application;
using EngineShell.Application.Interfaces;
using EngineShell.Application.Models;
using EngineShell.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests;

[TestClass]
public sealed class ServiceRegistrationTests
{
    [TestMethod]
    public void AddApplication_ResolvesRealSingletonServices()
    {
        // Arrange
        var services = new ServiceCollection();
        var initialViewModel = new object();
        services.AddSingleton(
            new NavigationItem("General", "home", initialViewModel));

        // Act
        services.AddApplication();
        using var provider = services.BuildServiceProvider();
        var firstStatus = provider.GetRequiredService<IAppStatusService>();
        var secondStatus = provider.GetRequiredService<IAppStatusService>();
        var navigation = provider.GetRequiredService<INavigationService>();

        // Assert
        Assert.IsInstanceOfType<AppStatusService>(firstStatus);
        Assert.AreSame(firstStatus, secondStatus);
        Assert.IsInstanceOfType<NavigationService>(navigation);
        Assert.AreSame(initialViewModel, navigation.CurrentViewModel);
    }
}
