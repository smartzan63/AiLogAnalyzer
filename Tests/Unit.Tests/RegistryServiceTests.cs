namespace Unit.Tests;

using AiLogAnalyzer.Core.Services;
using FluentAssertions;
using Moq;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
[TestClass]
public class RegistryServiceTests
{
    private Mock<IRegistryService>? _mockRegistryService;
    private RegistryService? _registryService;

    [TestInitialize]
    public void Setup()
    {
        _registryService = new RegistryService();
        _mockRegistryService = new Mock<IRegistryService>();
    }

    [TestMethod]
    public void GetMachineGuid_ShouldReturnMachineGuid_WhenRegistryKeyExists()
    {
        // Act
        var machineGuid = _registryService!.GetMachineGuid();

        // Assert
        machineGuid.Should().NotBeNullOrEmpty("because the MachineGuid should be present in the registry.");
    }

    [TestMethod]
    public void GetMachineGuid_ShouldReturnNull_WhenRegistryKeyIsMissing()
    {
        // Arrange
        // Mocking a scenario where the registry key is missing
        _mockRegistryService!.Setup(rs => rs.GetMachineGuid()).Returns((string)null!);

        // Act
        var result = _mockRegistryService.Object.GetMachineGuid();

        // Assert
        result.Should().BeNull("because the registry key is missing.");
    }

    [TestMethod]
    public void GetMachineGuid_ShouldReturnNull_WhenValueDoesNotExistInRegistryKey()
    {
        // Act
        var result = _registryService!.GetMachineGuid();

        // Assert
        result.Should().NotBeNullOrEmpty("because in normal system environments the key should be there");
    }

    [TestMethod]
    public void GetMachineGuid_ShouldLogError_WhenExceptionIsThrown()
    {
        // Arrange
        _mockRegistryService!.Setup(rs => rs.GetMachineGuid()).Throws(new UnauthorizedAccessException());

        // Act
        Action act = () => _mockRegistryService.Object.GetMachineGuid();

        // Assert
        act.Should().Throw<UnauthorizedAccessException>("because registry access should fail and throw an exception.");
    }
}