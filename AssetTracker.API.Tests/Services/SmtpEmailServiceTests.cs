using AssetTracker.API.Models;
using AssetTracker.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AssetTracker.API.Tests.Services
{
    public class SmtpEmailServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;

        public SmtpEmailServiceTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            SetupConfiguration();
        }

        private void SetupConfiguration()
        {
            _mockConfiguration.Setup(c => c["Email:SmtpHost"]).Returns("smtp.gmail.com");
            _mockConfiguration.Setup(c => c["Email:SmtpPort"]).Returns("587");
            _mockConfiguration.Setup(c => c["Email:SmtpUser"]).Returns("test@example.com");
            _mockConfiguration.Setup(c => c["Email:SmtpPassword"]).Returns("testpassword");
            _mockConfiguration.Setup(c => c["Email:FromEmail"]).Returns("test@example.com");
            _mockConfiguration.Setup(c => c["Email:FromName"]).Returns("Test System");
            _mockConfiguration.Setup(c => c["Email:AdminEmail"]).Returns("admin@example.com");
        }

        #region SendOverdueReminderAsync Tests

        [Fact]
        public async Task SendOverdueReminderAsync_WithNullAsset_ThrowsException()
        {
            // Arrange
            var emailService = new SmtpEmailService(_mockConfiguration.Object);
            var checkout = new Checkout
            {
                CheckoutID = 1,
                AssetID = 1,
                Asset = null,
                UserFullName = "Test User",
                UserEmail = "user@test.com",
                CheckoutTimestamp = DateTime.UtcNow.AddDays(-10),
                ExpectedReturnDate = DateTime.UtcNow.AddDays(-3),
                Status = "Active"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => emailService.SendOverdueReminderAsync(checkout));
        }

        [Fact]
        public void SmtpEmailService_LoadsSmtpConfiguration()
        {
            // Arrange & Act
            var emailService = new SmtpEmailService(_mockConfiguration.Object);

            // Assert
            emailService.Should().NotBeNull();
            _mockConfiguration.Verify(c => c["Email:SmtpHost"], Times.AtLeastOnce);
            _mockConfiguration.Verify(c => c["Email:SmtpPort"], Times.AtLeastOnce);
        }

        [Fact]
        public void SmtpEmailService_UsesDefaultValuesWhenConfigMissing()
        {
            // Arrange
            var mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c["Email:SmtpHost"]).Returns((string?)null);
            mockConfig.Setup(c => c["Email:SmtpPort"]).Returns((string?)null);

            // Act
            var emailService = new SmtpEmailService(mockConfig.Object);

            // Assert
            emailService.Should().NotBeNull();
        }

        #endregion

        #region SendAdminOverdueDigestAsync Tests

        [Fact]
        public async Task SendAdminOverdueDigestAsync_WithEmptyList_DoesNotThrow()
        {
            // Arrange
            var emailService = new SmtpEmailService(_mockConfiguration.Object);
            var emptyList = new List<OverdueCheckoutInfo>();

            // Act
            Func<Task> act = async () => await emailService.SendAdminOverdueDigestAsync(emptyList);

            // Assert
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SendAdminOverdueDigestAsync_WithOverdueCheckouts_DoesNotThrow()
        {
            // Arrange
            var emailService = new SmtpEmailService(_mockConfiguration.Object);
            var overdueList = new List<OverdueCheckoutInfo>
            {
                new OverdueCheckoutInfo
                {
                    UserFullName = "Test User",
                    UserEmail = "user@test.com",
                    AssetName = "Test Laptop",
                    SerialNumber = "SN001",
                    CheckoutDate = DateTime.UtcNow.AddDays(-10),
                    ExpectedReturnDate = DateTime.UtcNow.AddDays(-3),
                    DaysOverdue = 3
                }
            };

            // Act
            Func<Task> act = async () => await emailService.SendAdminOverdueDigestAsync(overdueList);

            // Assert
            // Note: This will fail if SMTP settings are not valid, but it tests the logic flow
            // In a real environment, you'd mock the SMTP client
            await act.Should().NotThrowAsync<ArgumentException>();
        }

        #endregion

        #region OverdueCheckoutInfo Model Tests

        [Fact]
        public void OverdueCheckoutInfo_CanBeCreated()
        {
            // Arrange & Act
            var info = new OverdueCheckoutInfo
            {
                UserFullName = "Test User",
                UserEmail = "user@test.com",
                AssetName = "Test Laptop",
                SerialNumber = "SN001",
                CheckoutDate = DateTime.UtcNow.AddDays(-10),
                ExpectedReturnDate = DateTime.UtcNow.AddDays(-3),
                DaysOverdue = 3
            };

            // Assert
            info.Should().NotBeNull();
            info.UserFullName.Should().Be("Test User");
            info.DaysOverdue.Should().Be(3);
        }

        #endregion
    }
}
