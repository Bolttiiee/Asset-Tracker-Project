using AssetTracker.API.Data;
using AssetTracker.API.Models;
using AssetTracker.API.Services;
using AssetTracker.API.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace AssetTracker.API.Tests.Services
{
    /// <summary>
    /// Tests for the §2.4 overdue-reminder orchestration. The processor is shared by the daily
    /// background job and the manual admin trigger, so these tests lock in the core behaviour:
    /// who gets reminded, when the admin digest fires, and what gets written to the audit trail.
    /// </summary>
    public class OverdueReminderProcessorTests
    {
        private readonly Mock<IEmailService> _mockEmail;
        private readonly Mock<ILogger<OverdueReminderProcessor>> _mockLogger;

        public OverdueReminderProcessorTests()
        {
            _mockEmail = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<OverdueReminderProcessor>>();
        }

        private OverdueReminderProcessor CreateProcessor(ApplicationDbContext context) =>
            new OverdueReminderProcessor(context, _mockEmail.Object, _mockLogger.Object);

        private static Checkout AddCheckout(
            ApplicationDbContext context,
            int assetId,
            int daysUntilDue,
            string status = "Active")
        {
            var checkout = new Checkout
            {
                AssetID = assetId,
                UserID = 2,
                UserFullName = "Test User",
                UserEmail = "user@test.com",
                CheckoutTimestamp = DateTime.UtcNow.AddDays(-30),
                ExpectedReturnDate = DateTime.UtcNow.Date.AddDays(daysUntilDue),
                Status = status
            };
            context.Checkouts.Add(checkout);
            context.SaveChanges();
            return checkout;
        }

        // ── Requirement 1 & 2: reminders for overdue, active checkouts ─────────────

        [Fact]
        public async Task ProcessOverdueRemindersAsync_WithOverdueCheckout_SendsReminderEmail()
        {
            // Arrange - one checkout three days past due
            var context = TestDbContextFactory.CreateContextWithSeedData();
            AddCheckout(context, assetId: 1, daysUntilDue: -3);
            var processor = CreateProcessor(context);

            // Act
            var result = await processor.ProcessOverdueRemindersAsync();

            // Assert
            result.OverdueCount.Should().Be(1);
            result.EmailsSent.Should().Be(1);
            result.EmailsFailed.Should().Be(0);
            _mockEmail.Verify(e => e.SendOverdueReminderAsync(It.IsAny<Checkout>()), Times.Once);
        }

        [Fact]
        public async Task ProcessOverdueRemindersAsync_DueToday_IsTreatedAsOverdue()
        {
            // Arrange - boundary: due exactly today must trigger a reminder (§2.4 "on the day")
            var context = TestDbContextFactory.CreateContextWithSeedData();
            AddCheckout(context, assetId: 1, daysUntilDue: 0);
            var processor = CreateProcessor(context);

            // Act
            var result = await processor.ProcessOverdueRemindersAsync();

            // Assert
            result.OverdueCount.Should().Be(1);
            _mockEmail.Verify(e => e.SendOverdueReminderAsync(It.IsAny<Checkout>()), Times.Once);
        }

        [Fact]
        public async Task ProcessOverdueRemindersAsync_NotYetDue_DoesNotSendReminder()
        {
            // Arrange - due in the future, should be ignored
            var context = TestDbContextFactory.CreateContextWithSeedData();
            AddCheckout(context, assetId: 1, daysUntilDue: 5);
            var processor = CreateProcessor(context);

            // Act
            var result = await processor.ProcessOverdueRemindersAsync();

            // Assert
            result.OverdueCount.Should().Be(0);
            _mockEmail.Verify(e => e.SendOverdueReminderAsync(It.IsAny<Checkout>()), Times.Never);
            _mockEmail.Verify(e => e.SendAdminOverdueDigestAsync(It.IsAny<List<OverdueCheckoutInfo>>()), Times.Never);
        }

        [Fact]
        public async Task ProcessOverdueRemindersAsync_ExcludesReturnedAndCancelledCheckouts()
        {
            // Arrange - both are past due but no longer Active, so §2.4 reminders must stop
            var context = TestDbContextFactory.CreateContextWithSeedData();
            AddCheckout(context, assetId: 1, daysUntilDue: -10, status: "Returned");
            AddCheckout(context, assetId: 2, daysUntilDue: -10, status: "Cancelled");
            var processor = CreateProcessor(context);

            // Act
            var result = await processor.ProcessOverdueRemindersAsync();

            // Assert
            result.OverdueCount.Should().Be(0);
            _mockEmail.Verify(e => e.SendOverdueReminderAsync(It.IsAny<Checkout>()), Times.Never);
        }

        [Fact]
        public async Task ProcessOverdueRemindersAsync_WithMultipleOverdue_SendsReminderForEach()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            AddCheckout(context, assetId: 1, daysUntilDue: -2);
            AddCheckout(context, assetId: 2, daysUntilDue: -7);
            var processor = CreateProcessor(context);

            // Act
            var result = await processor.ProcessOverdueRemindersAsync();

            // Assert
            result.OverdueCount.Should().Be(2);
            result.EmailsSent.Should().Be(2);
            _mockEmail.Verify(e => e.SendOverdueReminderAsync(It.IsAny<Checkout>()), Times.Exactly(2));
        }

        // ── Requirement 5: audit logging of individual reminders ───────────────────

        [Fact]
        public async Task ProcessOverdueRemindersAsync_WritesEmailReminderAuditWithRecipientAndAssetDetails()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            AddCheckout(context, assetId: 1, daysUntilDue: -3);
            var processor = CreateProcessor(context);

            // Act
            await processor.ProcessOverdueRemindersAsync();

            // Assert - recipient, timestamp, and asset details all captured
            var audit = context.AuditTrails.FirstOrDefault(a => a.Action == "EMAIL_REMINDER");
            audit.Should().NotBeNull();
            audit!.AssetID.Should().Be(1);
            audit.AssetName.Should().Be("Test Laptop");
            audit.Details.Should().Contain("user@test.com"); // recipient
            audit.Timestamp.Should().BeOnOrBefore(DateTime.UtcNow);
        }

        // ── Requirement 4: admin daily digest ──────────────────────────────────────

        [Fact]
        public async Task ProcessOverdueRemindersAsync_WithOverdueCheckouts_SendsAdminDigest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            AddCheckout(context, assetId: 1, daysUntilDue: -4);
            var processor = CreateProcessor(context);

            // Act
            var result = await processor.ProcessOverdueRemindersAsync();

            // Assert
            result.AdminDigestSent.Should().BeTrue();
            _mockEmail.Verify(e => e.SendAdminOverdueDigestAsync(It.IsAny<List<OverdueCheckoutInfo>>()), Times.Once);
        }

        [Fact]
        public async Task ProcessOverdueRemindersAsync_DigestIncludesUserNameEmailAndDaysOverdue()
        {
            // Arrange - capture the digest payload the processor builds
            var context = TestDbContextFactory.CreateContextWithSeedData();
            AddCheckout(context, assetId: 1, daysUntilDue: -3);

            List<OverdueCheckoutInfo>? captured = null;
            _mockEmail
                .Setup(e => e.SendAdminOverdueDigestAsync(It.IsAny<List<OverdueCheckoutInfo>>()))
                .Callback<List<OverdueCheckoutInfo>>(list => captured = list)
                .Returns(Task.CompletedTask);

            var processor = CreateProcessor(context);

            // Act
            await processor.ProcessOverdueRemindersAsync();

            // Assert
            captured.Should().NotBeNull();
            captured!.Should().ContainSingle();
            var entry = captured[0];
            entry.UserFullName.Should().Be("Test User");
            entry.UserEmail.Should().Be("user@test.com");
            entry.DaysOverdue.Should().Be(3);
            entry.AssetName.Should().Be("Test Laptop");
            entry.SerialNumber.Should().Be("SN001");
        }

        [Fact]
        public async Task ProcessOverdueRemindersAsync_WritesAdminDigestAudit()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            AddCheckout(context, assetId: 1, daysUntilDue: -4);
            var processor = CreateProcessor(context);

            // Act
            await processor.ProcessOverdueRemindersAsync();

            // Assert
            var audit = context.AuditTrails.FirstOrDefault(a => a.Action == "ADMIN_DIGEST");
            audit.Should().NotBeNull();
            audit!.Timestamp.Should().BeOnOrBefore(DateTime.UtcNow);
        }

        [Fact]
        public async Task ProcessOverdueRemindersAsync_WithNoOverdue_DoesNotSendDigestOrWriteAudit()
        {
            // Arrange - no checkouts at all
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var processor = CreateProcessor(context);

            // Act
            var result = await processor.ProcessOverdueRemindersAsync();

            // Assert
            result.OverdueCount.Should().Be(0);
            result.AdminDigestSent.Should().BeFalse();
            _mockEmail.Verify(e => e.SendAdminOverdueDigestAsync(It.IsAny<List<OverdueCheckoutInfo>>()), Times.Never);
            context.AuditTrails.Any().Should().BeFalse();
        }

        // ── Resilience: a single failed send must not abort the run ────────────────

        [Fact]
        public async Task ProcessOverdueRemindersAsync_WhenReminderEmailThrows_CountsFailureAndContinues()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            AddCheckout(context, assetId: 1, daysUntilDue: -3);

            _mockEmail
                .Setup(e => e.SendOverdueReminderAsync(It.IsAny<Checkout>()))
                .ThrowsAsync(new InvalidOperationException("SMTP unavailable"));

            var processor = CreateProcessor(context);

            // Act
            var result = await processor.ProcessOverdueRemindersAsync();

            // Assert - failure is recorded and no EMAIL_REMINDER audit row is written for it
            result.EmailsSent.Should().Be(0);
            result.EmailsFailed.Should().Be(1);
            context.AuditTrails.Any(a => a.Action == "EMAIL_REMINDER").Should().BeFalse();
        }

        [Fact]
        public async Task ProcessOverdueRemindersAsync_WithPartialFailures_CountsAndAuditsOnlySuccesses()
        {
            // Arrange - two overdue items; the send to asset 2 fails while asset 1 succeeds.
            // Locks in the split-pass derived counting (failed = total - sent) and that only
            // successful sends write an EMAIL_REMINDER audit row, even though the sends run
            // with bounded parallelism.
            var context = TestDbContextFactory.CreateContextWithSeedData();
            AddCheckout(context, assetId: 1, daysUntilDue: -3);
            AddCheckout(context, assetId: 2, daysUntilDue: -4);

            _mockEmail
                .Setup(e => e.SendOverdueReminderAsync(It.Is<Checkout>(c => c.AssetID == 2)))
                .ThrowsAsync(new InvalidOperationException("SMTP unavailable"));
            _mockEmail
                .Setup(e => e.SendOverdueReminderAsync(It.Is<Checkout>(c => c.AssetID == 1)))
                .Returns(Task.CompletedTask);

            var processor = CreateProcessor(context);

            // Act
            var result = await processor.ProcessOverdueRemindersAsync();

            // Assert
            result.OverdueCount.Should().Be(2);
            result.EmailsSent.Should().Be(1);
            result.EmailsFailed.Should().Be(1);

            var reminderAudits = context.AuditTrails.Where(a => a.Action == "EMAIL_REMINDER").ToList();
            reminderAudits.Should().ContainSingle();
            reminderAudits[0].AssetID.Should().Be(1);
        }

        [Fact]
        public async Task ProcessOverdueRemindersAsync_RespectsConfiguredMaxParallelSends()
        {
            // Arrange - a configured degree of parallelism of 1 must still send every reminder and
            // produce identical counts/audit rows. This guards the "Email:MaxParallelSends"
            // config knob (e.g. set to 1 to fall back to sequential sending).
            var context = TestDbContextFactory.CreateContextWithSeedData();
            AddCheckout(context, assetId: 1, daysUntilDue: -3);
            AddCheckout(context, assetId: 2, daysUntilDue: -4);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Email:MaxParallelSends"] = "1"
                })
                .Build();

            var processor = new OverdueReminderProcessor(
                context, _mockEmail.Object, _mockLogger.Object, configuration);

            // Act
            var result = await processor.ProcessOverdueRemindersAsync();

            // Assert
            result.OverdueCount.Should().Be(2);
            result.EmailsSent.Should().Be(2);
            result.EmailsFailed.Should().Be(0);
            context.AuditTrails.Count(a => a.Action == "EMAIL_REMINDER").Should().Be(2);
        }
    }
}
