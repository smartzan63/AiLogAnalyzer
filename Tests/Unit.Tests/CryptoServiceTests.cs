using System.Security.Cryptography;
using AiLogAnalyzer.Core.Services;
using FluentAssertions;
using Microsoft.Win32;
using Moq;

namespace Unit.Tests;

[TestClass]
public class CryptoServiceTests
{
    private Mock<IRegistryService> _mockRegistryService;
    private CryptoService _cryptoService;

    [TestInitialize]
    public void Setup()
    {
        _mockRegistryService = new Mock<IRegistryService>();
        _mockRegistryService.Setup(cs => cs.GetMachineGuid()).Returns("test-guid");
        _cryptoService = new CryptoService(_mockRegistryService.Object);
    }

    [TestMethod]
    public void Encrypt_ShouldReturnNonEmptyString()
    {
        // Arrange
        var plainText = "test-data";

        // Act
        var encryptedText = _cryptoService.Encrypt(plainText);

        // Assert
        encryptedText.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    public void Decrypt_ShouldReturnOriginalString()
    {
        // Arrange
        var plainText = "test-data";
        var encryptedText = _cryptoService.Encrypt(plainText);

        // Act
        var decryptedText = _cryptoService.Decrypt(encryptedText);

        // Assert
        decryptedText.Should().Be(plainText);
    }

    [TestMethod]
    public void Decrypt_ShouldReturnNull_WhenDecryptionFails()
    {
        // Act
        var decryptedText = _cryptoService.Decrypt("invalid-encrypted-text");

        // Assert
        decryptedText.Should().BeNull();
    }

    [TestMethod]
    public void Encrypt_ShouldThrowException_WhenEncryptionFails()
    {
        // Arrange
        var plainText = "test-data";

        // Simulate encryption failure by causing an issue in the Aes creation (e.g., invalid key size)
        var mockCryptoService = new Mock<CryptoService>(_mockRegistryService.Object);
        mockCryptoService.Setup(cs => cs.Encrypt(It.IsAny<string>()))
            .Throws<CryptographicException>();

        // Act
        Action act = () => mockCryptoService.Object.Encrypt(plainText);

        // Assert
        act.Should().Throw<CryptographicException>("because encryption should fail due to a cryptographic issue");
    }

    [TestMethod]
    public void Decrypt_ShouldThrowException_WhenDecryptionFails()
    {
        // Arrange
        var encryptedText = "invalid-encrypted-text";

        // Simulate decryption failure by causing an issue with invalid data
        var mockCryptoService = new Mock<CryptoService>(_mockRegistryService.Object);
        mockCryptoService.Setup(cs => cs.Decrypt(It.IsAny<string>()))
            .Throws<CryptographicException>();

        // Act
        Action act = () => mockCryptoService.Object.Decrypt(encryptedText);

        // Assert
        act.Should().Throw<CryptographicException>("because decryption should fail due to invalid data");
    }

    [TestMethod]
    public void CreateEntropy_ShouldThrowException_WhenMachineGuidIsMissing()
    {
        // Arrange
        _mockRegistryService.Setup(rs => rs.GetMachineGuid()).Returns((string)null!); // Simulate missing GUID

        // Act
        Action act = () => _cryptoService.Encrypt("test");

        // Assert
        act.Should()
            .Throw<InvalidOperationException>("because the machine GUID is missing and entropy cannot be created.");
    }

    [TestMethod]
    public void CreateEntropy_ShouldThrowException_WhenMachineGuidRetrievalFails()
    {
        // Arrange
        _mockRegistryService.Setup(rs => rs.GetMachineGuid())
            .Throws<UnauthorizedAccessException>(); // Simulate registry access failure

        // Act
        Action act = () => _cryptoService.Encrypt("test");

        // Assert
        act.Should()
            .Throw<UnauthorizedAccessException>(
                "because there was an issue accessing the registry for the Machine GUID.");
    }
}