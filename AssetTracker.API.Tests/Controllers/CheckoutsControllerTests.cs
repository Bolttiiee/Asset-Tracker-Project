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

        // §2.2 Transaction Validation - reject cases

        [Fact]
        public async Task AssignAsset_WithDecommissionedAsset_ReturnsBadRequest()
        {
            // Arrange - asset 1 has been decommissioned
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var asset = context.Assets.Find(1);
            asset!.Status = "Decommissioned";
            context.SaveChanges();
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
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var payload = JsonSerializer.SerializeToElement(badRequest.Value);
            payload.GetProperty("message").GetString().Should().Contain("decommissioned");
            context.Checkouts.Any(c => c.AssetID == 1).Should().BeFalse();
        }

        [Fact]
        public async Task AssignAsset_WithPastReturnDate_ReturnsBadRequest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");
            var dto = new CheckoutDto
            {
                AssetID = 1,
                EmployeeEmail = "user@test.com",
                ExpectedReturnDate = DateTime.UtcNow.AddDays(-1) // In the past
            };

            // Act
            var result = await controller.AssignAsset(dto);

            // Assert - rejected, and no checkout persisted
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var payload = JsonSerializer.SerializeToElement(badRequest.Value);
            payload.GetProperty("message").GetString().Should().Contain("past");
            context.Checkouts.Any(c => c.AssetID == 1).Should().BeFalse();
        }

        [Fact]
        public async Task AssignAsset_WithMissingReturnDate_ReturnsBadRequest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");
            var dto = new CheckoutDto
            {
                AssetID = 1,
                EmployeeEmail = "user@test.com",
                ExpectedReturnDate = null // Missing
            };

            // Act
            var result = await controller.AssignAsset(dto);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var payload = JsonSerializer.SerializeToElement(badRequest.Value);
            payload.GetProperty("message").GetString().Should().Contain("required");
            context.Checkouts.Any(c => c.AssetID == 1).Should().BeFalse();
        }

        [Fact]
        public async Task AssignAsset_WhenUserAlreadyHasSameAsset_ReturnsBadRequest()
        {
            // Arrange - user 2 already holds an ACTIVE checkout for asset 1.
            // The asset is deliberately left "Available" so the request reaches the
            // duplicate-checkout guard (VALIDATION 7) rather than the status guard (VALIDATION 3).
            var context = TestDbContextFactory.CreateContextWithSeedData();
            context.Checkouts.Add(new Checkout
            {
                AssetID = 1,
                UserID = 2,
                UserFullName = "Test User",
                UserEmail = "user@test.com",
                CheckoutTimestamp = DateTime.UtcNow,
                ExpectedReturnDate = DateTime.UtcNow.AddDays(7),
                Status = "Active"
            });
            context.SaveChanges();

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

            // Assert - rejected specifically by the duplicate guard
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var payload = JsonSerializer.SerializeToElement(badRequest.Value);
            payload.GetProperty("message").GetString().Should().Contain("already has this asset");
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

        #region CancelCheckout Tests

        private static Checkout SeedActiveCheckout(Data.ApplicationDbContext context)
        {
            var asset = context.Assets.Find(1);
            asset!.Status = "Checked Out";
            asset.AssignedTo = "Test User";
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
            return checkout;
        }

        [Fact]
        public async Task CancelCheckout_WithActiveCheckout_ReturnsOk()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            SeedActiveCheckout(context);
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");

            // Act
            var result = await controller.CancelCheckout(1);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task CancelCheckout_UpdatesAssetStatusToAvailable()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            SeedActiveCheckout(context);
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");

            // Act
            await controller.CancelCheckout(1);

            // Assert
            var updatedAsset = context.Assets.Find(1);
            updatedAsset!.Status.Should().Be("Available");
            updatedAsset.AssignedTo.Should().BeEmpty();
        }

        [Fact]
        public async Task CancelCheckout_UpdatesCheckoutStatusToCancelled()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var checkout = SeedActiveCheckout(context);
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");

            // Act
            await controller.CancelCheckout(1);

            // Assert
            var updatedCheckout = context.Checkouts.Find(checkout.CheckoutID);
            updatedCheckout!.Status.Should().Be("Cancelled");
            updatedCheckout.ActualReturnDate.Should().NotBeNull();
        }

        [Fact]
        public async Task CancelCheckout_WritesAuditTrail()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            SeedActiveCheckout(context);
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");

            // Act
            await controller.CancelCheckout(1);

            // Assert
            var audit = context.AuditTrails.FirstOrDefault(a => a.Action == "CANCEL_CHECKOUT" && a.AssetID == 1);
            audit.Should().NotBeNull();
            audit!.PerformedByEmail.Should().Be("admin@test.com");
        }

        [Fact]
        public async Task CancelCheckout_WithNoActiveCheckout_ReturnsNotFound()
        {
            // Arrange - asset 1 has no active checkout in seed data
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new CheckoutsController(context, _mockEmailService.Object, _mockConfiguration.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller, "admin@test.com", "Admin");

            // Act
            var result = await controller.CancelCheckout(1);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }

        #endregion
    }
}
