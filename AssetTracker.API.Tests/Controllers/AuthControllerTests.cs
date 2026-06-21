using AssetTracker.API.Controllers;
using AssetTracker.API.Data;
using AssetTracker.API.Models;
using AssetTracker.API.Tests.Helpers;
using AssetTracker.API.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;

namespace AssetTracker.API.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly IConfigurationSection _mockJwtSection;

        public AuthControllerTests()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<AuthController>>();

            // Setup Email Service mock to not throw
            _mockEmailService.Setup(x => x.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Setup JWT configuration
            var jwtSettings = new Dictionary<string, string>
            {
                {"Jwt:Secret", "ThisIsAVerySecretKeyForJWTTokenGenerationAndValidation123456"},
                {"Jwt:Issuer", "AssetTrackerAPI"},
                {"Jwt:Audience", "AssetTrackerClient"}
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(jwtSettings!)
                .Build();

            _mockJwtSection = configuration.GetSection("Jwt");
            _mockConfiguration.Setup(c => c.GetSection("Jwt")).Returns(_mockJwtSection);
        }

        #region Register Tests

        [Fact]
        public async Task Register_WithValidData_ReturnsOkResult()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new RegisterDto
            {
                FullName = "New User",
                Email = "newuser@test.com",
                EmployeeID = "EMP999",
                Department = "IT",
                Password = "Password@123"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task Register_WithWeakPassword_ReturnsBadRequest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new RegisterDto
            {
                FullName = "New User",
                Email = "newuser@test.com",
                EmployeeID = "EMP999",
                Department = "IT",
                Password = "weak"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Register_WithPasswordMissingUppercase_ReturnsBadRequest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new RegisterDto
            {
                FullName = "New User",
                Email = "newuser@test.com",
                EmployeeID = "EMP999",
                Department = "IT",
                Password = "password@123"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Register_WithPasswordMissingNumber_ReturnsBadRequest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new RegisterDto
            {
                FullName = "New User",
                Email = "newuser@test.com",
                EmployeeID = "EMP999",
                Department = "IT",
                Password = "Password@"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Register_WithPasswordMissingSpecialChar_ReturnsBadRequest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new RegisterDto
            {
                FullName = "New User",
                Email = "newuser@test.com",
                EmployeeID = "EMP999",
                Department = "IT",
                Password = "Password123"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Register_WithExistingEmail_ReturnsBadRequest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new RegisterDto
            {
                FullName = "Duplicate User",
                Email = "admin@test.com",
                EmployeeID = "EMP999",
                Department = "IT",
                Password = "Password@123"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { message = "An account with this email address already exists." });
        }

        [Fact]
        public async Task Register_WithExistingEmployeeID_ReturnsBadRequest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new RegisterDto
            {
                FullName = "Duplicate User",
                Email = "newemail@test.com",
                EmployeeID = "EMP001",
                Department = "IT",
                Password = "Password@123"
            };

            // Act
            var result = await controller.Register(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { message = "An account with this Employee ID already exists." });
        }

        [Fact]
        public async Task Register_SetsDefaultRoleAsStandardUser()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new RegisterDto
            {
                FullName = "New User",
                Email = "newuser@test.com",
                EmployeeID = "EMP999",
                Department = "IT",
                Password = "Password@123"
            };

            // Act
            await controller.Register(dto);

            // Assert
            var user = context.Users.First(u => u.Email == "newuser@test.com");
            user.Role.Should().Be("Standard User");
        }

        [Fact]
        public async Task Register_SetsAccountStatusAsPending()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new RegisterDto
            {
                FullName = "New User",
                Email = "newuser@test.com",
                EmployeeID = "EMP999",
                Department = "IT",
                Password = "Password@123"
            };

            // Act
            await controller.Register(dto);

            // Assert
            var user = context.Users.First(u => u.Email == "newuser@test.com");
            user.AccountStatus.Should().Be("Pending");
        }

        [Fact]
        public async Task Register_HashesPasswordWithBCrypt()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new RegisterDto
            {
                FullName = "New User",
                Email = "newuser@test.com",
                EmployeeID = "EMP999",
                Department = "IT",
                Password = "Password@123"
            };

            // Act
            await controller.Register(dto);

            // Assert
            var user = context.Users.First(u => u.Email == "newuser@test.com");
            user.PasswordHash.Should().NotBe("Password@123");
            BCrypt.Net.BCrypt.Verify("Password@123", user.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task Register_NormalizesEmailToLowerCase()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new RegisterDto
            {
                FullName = "New User",
                Email = "NewUser@TEST.COM",
                EmployeeID = "EMP999",
                Department = "IT",
                Password = "Password@123"
            };

            // Act
            await controller.Register(dto);

            // Assert
            var user = context.Users.First(u => u.Email == "newuser@test.com");
            user.Email.Should().Be("newuser@test.com");
        }

        [Fact]
        public async Task Register_NormalizesEmployeeIDToUpperCase()
        {
            // Arrange
            var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new RegisterDto
            {
                FullName = "New User",
                Email = "newuser@test.com",
                EmployeeID = "emp999",
                Department = "IT",
                Password = "Password@123"
            };

            // Act
            await controller.Register(dto);

            // Assert
            var user = context.Users.First(u => u.Email == "newuser@test.com");
            user.EmployeeID.Should().Be("EMP999");
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new LoginDto
            {
                Email = "admin@test.com",
                Password = "Admin@123"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new LoginDto
            {
                Email = "nonexistent@test.com",
                Password = "Password@123"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Login_WithIncorrectPassword_ReturnsUnauthorized()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new LoginDto
            {
                Email = "admin@test.com",
                Password = "WrongPassword@123"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Login_WithPendingAccountStatus_Returns403Forbidden()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new LoginDto
            {
                Email = "pending@test.com",
                Password = "Pending@123"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = result as ObjectResult;
            objectResult!.StatusCode.Should().Be(403);
        }

        [Fact]
        public async Task Login_CaseInsensitiveEmailComparison_ReturnsOk()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new LoginDto
            {
                Email = "ADMIN@TEST.COM",
                Password = "Admin@123"
            };

            // Act
            var result = await controller.Login(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        #endregion

        #region ForgotPassword Tests

        [Fact]
        public async Task ForgotPassword_WithValidEmail_ReturnsOk()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller);
            var dto = new ForgotPasswordDto
            {
                Email = "admin@test.com"
            };

            // Act
            var result = await controller.ForgotPassword(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ForgotPassword_WithNonExistentEmail_ReturnsOkForSecurity()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new ForgotPasswordDto
            {
                Email = "nonexistent@test.com"
            };

            // Act
            var result = await controller.ForgotPassword(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(new { message = "If the email exists in our records, a secure reset token has been dispatched." });
        }

        [Fact]
        public async Task ForgotPassword_GeneratesSecureToken()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller);
            var dto = new ForgotPasswordDto
            {
                Email = "admin@test.com"
            };

            // Act
            await controller.ForgotPassword(dto);

            // Assert
            var user = context.Users.First(u => u.Email == "admin@test.com");
            user.PasswordResetToken.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task ForgotPassword_TokenIsHexString64Characters()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller);
            var dto = new ForgotPasswordDto
            {
                Email = "admin@test.com"
            };

            // Act
            await controller.ForgotPassword(dto);

            // Assert
            var user = context.Users.First(u => u.Email == "admin@test.com");
            user.PasswordResetToken.Should().HaveLength(64);
            user.PasswordResetToken.Should().MatchRegex("^[0-9A-F]+$");
        }

        [Fact]
        public async Task ForgotPassword_SetsExpiryToOneHour()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller);
            var dto = new ForgotPasswordDto
            {
                Email = "admin@test.com"
            };

            // Act
            var beforeCall = DateTime.UtcNow.AddHours(1);
            await controller.ForgotPassword(dto);
            var afterCall = DateTime.UtcNow.AddHours(1);

            // Assert
            var user = context.Users.First(u => u.Email == "admin@test.com");
            user.ResetTokenExpiry.Should().NotBeNull();
            user.ResetTokenExpiry.Should().BeOnOrAfter(beforeCall);
            user.ResetTokenExpiry.Should().BeOnOrBefore(afterCall);
        }

        [Fact]
        public async Task ForgotPassword_SavesTokenToDatabase()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller);
            var dto = new ForgotPasswordDto
            {
                Email = "admin@test.com"
            };

            // Act
            await controller.ForgotPassword(dto);

            // Assert
            var user = context.Users.First(u => u.Email == "admin@test.com");
            user.PasswordResetToken.Should().NotBeNullOrEmpty();
            user.ResetTokenExpiry.Should().NotBeNull();
        }

        [Fact]
        public async Task ForgotPassword_OverwritesPreviousToken()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            ControllerTestHelper.SetupAuthenticatedUser(controller);
            var dto = new ForgotPasswordDto
            {
                Email = "admin@test.com"
            };

            // Act
            await controller.ForgotPassword(dto);
            var firstToken = context.Users.First(u => u.Email == "admin@test.com").PasswordResetToken;

            await Task.Delay(100); // Small delay to ensure different timestamp
            await controller.ForgotPassword(dto);
            var secondToken = context.Users.First(u => u.Email == "admin@test.com").PasswordResetToken;

            // Assert
            secondToken.Should().NotBe(firstToken);
        }

        #endregion

        #region ResetPassword Tests

        [Fact]
        public async Task ResetPassword_WithValidToken_ReturnsOk()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var user = context.Users.First(u => u.Email == "admin@test.com");
            user.PasswordResetToken = "VALIDTOKEN123";
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            context.SaveChanges();

            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new ResetPasswordDto
            {
                Token = "VALIDTOKEN123",
                NewPassword = "NewPassword@123"
            };

            // Act
            var result = await controller.ResetPassword(dto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task ResetPassword_WithInvalidToken_ReturnsBadRequest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new ResetPasswordDto
            {
                Token = "INVALIDTOKEN",
                NewPassword = "NewPassword@123"
            };

            // Act
            var result = await controller.ResetPassword(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task ResetPassword_WithExpiredToken_ReturnsBadRequest()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var user = context.Users.First(u => u.Email == "admin@test.com");
            user.PasswordResetToken = "EXPIREDTOKEN";
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(-1);
            context.SaveChanges();

            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new ResetPasswordDto
            {
                Token = "EXPIREDTOKEN",
                NewPassword = "NewPassword@123"
            };

            // Act
            var result = await controller.ResetPassword(dto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = result as BadRequestObjectResult;
            badRequestResult!.Value.Should().BeEquivalentTo(new { message = "Security token has expired. Please request a fresh recovery sequence." });
        }

        [Fact]
        public async Task ResetPassword_UpdatesPasswordHash()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var user = context.Users.First(u => u.Email == "admin@test.com");
            var oldPasswordHash = user.PasswordHash;
            user.PasswordResetToken = "VALIDTOKEN123";
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            context.SaveChanges();

            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new ResetPasswordDto
            {
                Token = "VALIDTOKEN123",
                NewPassword = "NewPassword@123"
            };

            // Act
            await controller.ResetPassword(dto);

            // Assert
            var updatedUser = context.Users.First(u => u.Email == "admin@test.com");
            updatedUser.PasswordHash.Should().NotBe(oldPasswordHash);
            BCrypt.Net.BCrypt.Verify("NewPassword@123", updatedUser.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task ResetPassword_ClearsTokenAfterUse()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var user = context.Users.First(u => u.Email == "admin@test.com");
            user.PasswordResetToken = "VALIDTOKEN123";
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            context.SaveChanges();

            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new ResetPasswordDto
            {
                Token = "VALIDTOKEN123",
                NewPassword = "NewPassword@123"
            };

            // Act
            await controller.ResetPassword(dto);

            // Assert
            var updatedUser = context.Users.First(u => u.Email == "admin@test.com");
            updatedUser.PasswordResetToken.Should().BeNull();
        }

        [Fact]
        public async Task ResetPassword_ClearsExpiryAfterUse()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var user = context.Users.First(u => u.Email == "admin@test.com");
            user.PasswordResetToken = "VALIDTOKEN123";
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            context.SaveChanges();

            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new ResetPasswordDto
            {
                Token = "VALIDTOKEN123",
                NewPassword = "NewPassword@123"
            };

            // Act
            await controller.ResetPassword(dto);

            // Assert
            var updatedUser = context.Users.First(u => u.Email == "admin@test.com");
            updatedUser.ResetTokenExpiry.Should().BeNull();
        }

        [Fact]
        public async Task ResetPassword_UsesBCryptForHashing()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var user = context.Users.First(u => u.Email == "admin@test.com");
            user.PasswordResetToken = "VALIDTOKEN123";
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            context.SaveChanges();

            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new ResetPasswordDto
            {
                Token = "VALIDTOKEN123",
                NewPassword = "NewPassword@123"
            };

            // Act
            await controller.ResetPassword(dto);

            // Assert
            var updatedUser = context.Users.First(u => u.Email == "admin@test.com");
            updatedUser.PasswordHash.Should().StartWith("$2");
        }

        [Fact]
        public async Task ResetPassword_TokenCannotBeReused()
        {
            // Arrange
            var context = TestDbContextFactory.CreateContextWithSeedData();
            var user = context.Users.First(u => u.Email == "admin@test.com");
            user.PasswordResetToken = "VALIDTOKEN123";
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);
            context.SaveChanges();

            var controller = new AuthController(context, _mockConfiguration.Object, _mockEmailService.Object, _mockLogger.Object);
            var dto = new ResetPasswordDto
            {
                Token = "VALIDTOKEN123",
                NewPassword = "NewPassword@123"
            };

            // Act
            await controller.ResetPassword(dto);
            var secondAttempt = await controller.ResetPassword(dto);

            // Assert
            secondAttempt.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion
    }
}

