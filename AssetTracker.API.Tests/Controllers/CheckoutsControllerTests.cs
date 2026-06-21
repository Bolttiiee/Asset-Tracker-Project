using AssetTracker.API.Controller;
using AssetTracker.API.Models;
using AssetTracker.API.Services;
using AssetTracker.API.Tests.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Text.Json;

namespace AssetTracker.API.Tests.Controllers
{
    public class CheckoutsControllerTests
    {
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IConfiguration> _mockConfiguration;

        public CheckoutsControllerTests()
        {
            _mockEmailService = new Mock<IEmailService>();
            _mockConfiguration = new Mock<IConfiguration>();
        }

        #region AssignAsset Tests

        [Fact]
        public async Task AssignAsset_WithValidData_ReturnsOk()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");
            var dto = new CheckoutDto
            {
                AssetID = 1,
                EmployeeEmail = "user@test.com",
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7)
            };

            // Act
            var result = await controller.AssignAsset(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task AssignAsset_WithZeroAssetID_ReturnsBadRequest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            var dto = new CheckoutDto
            {
                AssetID = 0,
                EmployeeEmail = "user@test.com",
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7)
            };

            // Act
            var result = await controller.AssignAsset(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task AssignAsset_WithNonExistentAsset_ReturnsNotFound()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            var dto = new CheckoutDto
            {
                AssetID = 999,
                EmployeeEmail = "user@test.com",
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7)
            };

            // Act
            var result = await controller.AssignAsset(dto);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task AssignAsset_WithCheckedOutAsset_ReturnsBadRequest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            var dto = new CheckoutDto
            {
                AssetID = 2, // This asset is already checked out in seed data
                EmployeeEmail = "user@test.com",
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7)
            };

            // Act
            var result = await controller.AssignAsset(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task AssignAsset_UpdatesAssetStatus()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");
            var dto = new CheckoutDto
            {
                AssetID = 1,
                EmployeeEmail = "user@test.com",
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7)
            };

            // Act
            await controller.AssignAsset(dto);

            // Assert
            var asset = context.Assets.Find(1);
            asset!.Status.Should().Be("Checked Out");
        }

        [Fact]
        public async Task AssignAsset_CreatesCheckoutRecord()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");
            var dto = new CheckoutDto
            {
                AssetID = 1,
                EmployeeEmail = "user@test.com",
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7)
            };

            // Act
            await controller.AssignAsset(dto);

            // Assert
            var checkout = context.Checkouts.FirstOrDefault(c => c.AssetID == 1);
            checkout.Should().NotBeNull();
            checkout!.UserFullName.Should().Be("Test User");
            checkout.UserEmail.Should().Be("user@test.com");
        }

        [Fact]
        public async Task AssignAsset_WithReturnDateToday_SendsReminderAndLogsAudit()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");
            var dto = new CheckoutDto
            {
                AssetID = 1,
                EmployeeEmail = "user@test.com",
                ExpectedReturnDate = DateTime.UtcNow.Date // Due today => already overdue
            };

            // Act
            var result = await controller.AssignAsset(dto);

            // Assert - response flags the reminder as sent
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var payload = JsonSerializer.SerializeToElement(okResult.Value);
            payload.GetProperty("reminderEmailSent").GetBoolean().Should().BeTrue();

            // Assert - the email service was invoked exactly once for this checkout
            _mockEmailService.Verify(
                x => x.SendOverdueReminderAsync(It.Is<Checkout>(c => c.AssetID == 1 && c.UserEmail == "user@test.com")),
                Times.Once);

            // Assert - an EMAIL_REMINDER row was written to the audit trail
            var reminderAudit = context.AuditTrails.FirstOrDefault(a => a.Action == "EMAIL_REMINDER" && a.AssetID == 1);
            reminderAudit.Should().NotBeNull();
            reminderAudit!.AssetName.Should().Be("Test Laptop");
        }

        [Fact]
        public async Task AssignAsset_WithFutureReturnDate_DoesNotSendReminder()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");
            var dto = new CheckoutDto
            {
                AssetID = 1,
                EmployeeEmail = "user@test.com",
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7) // Not yet due
            };

            // Act
            var result = await controller.AssignAsset(dto);

            // Assert - response flags no reminder sent
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var payload = JsonSerializer.SerializeToElement(okResult.Value);
            payload.GetProperty("reminderEmailSent").GetBoolean().Should().BeFalse();

            // Assert - no reminder email and no EMAIL_REMINDER audit row
            _mockEmailService.Verify(x => x.SendOverdueReminderAsync(It.IsAny<Checkout>()), Times.Never);
            context.AuditTrails.Any(a => a.Action == "EMAIL_REMINDER").Should().BeFalse();
        }

        #endregion

        #region ReturnAsset Tests

        [Fact]
        public async Task ReturnAsset_WithValidCheckout_ReturnsOk()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();

            // Setup: First checkout an asset
            var asset = context.Assets.Find(1);
            asset!.Status = "Checked Out";
            var checkout = new Checkout
            {
                AssetID = 1,
                UserID = 2,
                UserFullName = "Test User",
                UserEmail = "user@test.com",
                CheckoutTimestamp = DateTime.UtcNow,
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7),
                Status = "Active"
            };
            context.Checkouts.Add(checkout);
            context.SaveChanges();

            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");

            // Act - Return asset by assetId
            var result = await controller.ReturnAsset(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ReturnAsset_WithNonExistentCheckout_ReturnsNotFound()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);

            // Act - Try to return asset that doesn't exist
            var result = await controller.ReturnAsset(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task ReturnAsset_UpdatesAssetStatusToAvailable()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var asset = context.Assets.Find(1);
            asset!.Status = "Checked Out";
            var checkout = new Checkout
            {
                AssetID = 1,
                UserID = 2,
                UserFullName = "Test User",
                UserEmail = "user@test.com",
                CheckoutTimestamp = DateTime.UtcNow,
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7),
                Status = "Active"
            };
            context.Checkouts.Add(checkout);
            context.SaveChanges();

            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");

            // Act
            await controller.ReturnAsset(1);

            // Assert
            var updatedAsset = context.Assets.Find(1);
            updatedAsset!.Status.Should().Be("Available");
        }

        [Fact]
        public async Task ReturnAsset_UpdatesCheckoutStatus()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var asset = context.Assets.Find(1);
            asset!.Status = "Checked Out";
            var checkout = new Checkout
            {
                AssetID = 1,
                UserID = 2,
                UserFullName = "Test User",
                UserEmail = "user@test.com",
                CheckoutTimestamp = DateTime.UtcNow,
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7),
                Status = "Active"
            };
            context.Checkouts.Add(checkout);
            context.SaveChanges();

            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");

            // Act
            await controller.ReturnAsset(1);

            // Assert
            var updatedCheckout = context.Checkouts.Find(checkout.CheckoutID);
            updatedCheckout!.Status.Should().Be("Returned");
        }

        [Fact]
        public async Task ReturnAsset_SetsActualReturnDate()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var asset = context.Assets.Find(1);
            asset!.Status = "Checked Out";
            var checkout = new Checkout
            {
                AssetID = 1,
                UserID = 2,
                UserFullName = "Test User",
                UserEmail = "user@test.com",
                CheckoutTimestamp = DateTime.UtcNow,
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7),
                Status = "Active"
            };
            context.Checkouts.Add(checkout);
            context.SaveChanges();

            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");

            // Act
            var beforeReturn = DateTime.UtcNow;
            await controller.ReturnAsset(1);
            var afterReturn = DateTime.UtcNow;

            // Assert
            var updatedCheckout = context.Checkouts.Find(checkout.CheckoutID);
            updatedCheckout!.ActualReturnDate.Should().NotBeNull();
            updatedCheckout.ActualReturnDate.Should().BeOnOrAfter(beforeReturn);
            updatedCheckout.ActualReturnDate.Should().BeOnOrBefore(afterReturn);
        }

        #endregion
    }
}
