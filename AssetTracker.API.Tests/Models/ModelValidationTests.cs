using AssetTracker.API.Models;
using FluentAssertions;
using System.ComponentModel.DataAnnotations;

namespace AssetTracker.API.Tests.Models
{
    public class ModelValidationTests
    {
        #region User Model Tests

        [Fact]
        public void User_WithValidData_PassesValidation()
        {
            // Arrange
            var user = new User
            {
                FullName = "Test User",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                Department = "IT",
                EmployeeID = "EMP001",
                Role = "Standard User",
                AccountStatus = "Active"
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void User_WithMissingEmail_FailsValidation()
        {
            // Arrange
            var user = new User
            {
                FullName = "Test User",
                Email = "",
                PasswordHash = "hashedpassword",
                Department = "IT",
                EmployeeID = "EMP001",
                Role = "Standard User",
                AccountStatus = "Active"
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(v => v.MemberNames.Contains("Email"));
        }

        [Fact]
        public void User_WithInvalidEmail_FailsValidation()
        {
            // Arrange
            var user = new User
            {
                FullName = "Test User",
                Email = "invalid-email",
                PasswordHash = "hashedpassword",
                Department = "IT",
                EmployeeID = "EMP001",
                Role = "Standard User",
                AccountStatus = "Active"
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(v => v.MemberNames.Contains("Email"));
        }

        [Fact]
        public void User_WithMissingFullName_FailsValidation()
        {
            // Arrange
            var user = new User
            {
                FullName = "",
                Email = "test@example.com",
                PasswordHash = "hashedpassword",
                Department = "IT",
                EmployeeID = "EMP001",
                Role = "Standard User",
                AccountStatus = "Active"
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(v => v.MemberNames.Contains("FullName"));
        }

        [Fact]
        public void User_WithTooLongEmail_FailsValidation()
        {
            // Arrange
            var user = new User
            {
                FullName = "Test User",
                Email = new string('a', 250) + "@test.com",
                PasswordHash = "hashedpassword",
                Department = "IT",
                EmployeeID = "EMP001",
                Role = "Standard User",
                AccountStatus = "Active"
            };

            // Act
            var validationResults = ValidateModel(user);

            // Assert
            validationResults.Should().NotBeEmpty();
        }

        #endregion

        #region Asset Model Tests

        [Fact]
        public void Asset_WithValidData_PassesValidation()
        {
            // Arrange
            var asset = new Asset
            {
                Name = "Test Laptop",
                SerialNumber = "SN12345",
                Category = "Laptop",
                Status = "Available",
                PurchaseDate = DateTime.UtcNow,
                Value = 1000.00m
            };

            // Act
            var validationResults = ValidateModel(asset);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void Asset_WithMissingName_FailsValidation()
        {
            // Arrange
            var asset = new Asset
            {
                Name = "",
                SerialNumber = "SN12345",
                Category = "Laptop",
                Status = "Available",
                PurchaseDate = DateTime.UtcNow,
                Value = 1000.00m
            };

            // Act
            var validationResults = ValidateModel(asset);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(v => v.MemberNames.Contains("Name"));
        }

        [Fact]
        public void Asset_WithMissingSerialNumber_FailsValidation()
        {
            // Arrange
            var asset = new Asset
            {
                Name = "Test Laptop",
                SerialNumber = "",
                Category = "Laptop",
                Status = "Available",
                PurchaseDate = DateTime.UtcNow,
                Value = 1000.00m
            };

            // Act
            var validationResults = ValidateModel(asset);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(v => v.MemberNames.Contains("SerialNumber"));
        }

        #endregion

        #region ForgotPasswordDto Tests

        [Fact]
        public void ForgotPasswordDto_WithValidEmail_PassesValidation()
        {
            // Arrange
            var dto = new ForgotPasswordDto
            {
                Email = "test@example.com"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void ForgotPasswordDto_WithMissingEmail_FailsValidation()
        {
            // Arrange
            var dto = new ForgotPasswordDto
            {
                Email = ""
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(v => v.MemberNames.Contains("Email"));
        }

        [Fact]
        public void ForgotPasswordDto_WithInvalidEmail_FailsValidation()
        {
            // Arrange
            var dto = new ForgotPasswordDto
            {
                Email = "invalid-email"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(v => v.MemberNames.Contains("Email"));
        }

        #endregion

        #region ResetPasswordDto Tests

        [Fact]
        public void ResetPasswordDto_WithValidData_PassesValidation()
        {
            // Arrange
            var dto = new ResetPasswordDto
            {
                Token = "VALIDTOKEN123",
                NewPassword = "Password@123"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void ResetPasswordDto_WithMissingToken_FailsValidation()
        {
            // Arrange
            var dto = new ResetPasswordDto
            {
                Token = "",
                NewPassword = "Password@123"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(v => v.MemberNames.Contains("Token"));
        }

        [Fact]
        public void ResetPasswordDto_WithMissingPassword_FailsValidation()
        {
            // Arrange
            var dto = new ResetPasswordDto
            {
                Token = "VALIDTOKEN123",
                NewPassword = ""
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(v => v.MemberNames.Contains("NewPassword"));
        }

        [Fact]
        public void ResetPasswordDto_WithShortPassword_FailsValidation()
        {
            // Arrange
            var dto = new ResetPasswordDto
            {
                Token = "VALIDTOKEN123",
                NewPassword = "Pass1"
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().NotBeEmpty();
            validationResults.Should().Contain(v => v.ErrorMessage!.Contains("at least 6 characters"));
        }

        [Fact]
        public void ResetPasswordDto_WithLongPassword_PassesValidation()
        {
            // Arrange
            var dto = new ResetPasswordDto
            {
                Token = "VALIDTOKEN123",
                NewPassword = new string('A', 100)
            };

            // Act
            var validationResults = ValidateModel(dto);

            // Assert
            validationResults.Should().BeEmpty();
        }

        #endregion

        #region Helper Methods

        private List<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }

        #endregion
    }
}
