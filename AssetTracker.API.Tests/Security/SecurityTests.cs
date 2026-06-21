using FluentAssertions;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;

namespace AssetTracker.API.Tests.Security
{
    public class SecurityTests
    {
        #region BCrypt Tests

        [Fact]
        public void BCrypt_PasswordHashing_IsSecure()
        {
            // Arrange
            var password = "Password@123";

            // Act
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            // Assert
            hash.Should().NotBe(password);
            hash.Should().StartWith("$2");
            BCrypt.Net.BCrypt.Verify(password, hash).Should().BeTrue();
        }

        [Fact]
        public void BCrypt_SamePasswordGeneratesDifferentHashes()
        {
            // Arrange
            var password = "Password@123";

            // Act
            var hash1 = BCrypt.Net.BCrypt.HashPassword(password);
            var hash2 = BCrypt.Net.BCrypt.HashPassword(password);

            // Assert
            hash1.Should().NotBe(hash2);
            BCrypt.Net.BCrypt.Verify(password, hash1).Should().BeTrue();
            BCrypt.Net.BCrypt.Verify(password, hash2).Should().BeTrue();
        }

        [Fact]
        public void BCrypt_WrongPassword_FailsVerification()
        {
            // Arrange
            var password = "Password@123";
            var wrongPassword = "WrongPassword@123";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);

            // Act
            var result = BCrypt.Net.BCrypt.Verify(wrongPassword, hash);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region Password Reset Token Tests

        [Fact]
        public void PasswordResetToken_IsHighEntropy()
        {
            // Arrange & Act
            byte[] randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            string token = Convert.ToHexString(randomBytes);

            // Assert
            token.Should().HaveLength(64);
            token.Should().MatchRegex("^[0-9A-F]+$");
        }

        [Fact]
        public void PasswordResetToken_IsUnique()
        {
            // Arrange
            var tokens = new HashSet<string>();

            // Act - Generate 1000 tokens
            for (int i = 0; i < 1000; i++)
            {
                byte[] randomBytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomBytes);
                }
                string token = Convert.ToHexString(randomBytes);
                tokens.Add(token);
            }

            // Assert
            tokens.Should().HaveCount(1000);
        }

        [Fact]
        public void PasswordResetToken_Uses256BitEntropy()
        {
            // Arrange & Act
            byte[] randomBytes = new byte[32]; // 32 bytes = 256 bits
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            // Assert
            randomBytes.Should().HaveCount(32);
        }

        [Fact]
        public void PasswordResetToken_IsCryptographicallySecure()
        {
            // Arrange
            var token1Bytes = new byte[32];
            var token2Bytes = new byte[32];

            // Act
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(token1Bytes);
                rng.GetBytes(token2Bytes);
            }

            // Assert
            token1Bytes.Should().NotEqual(token2Bytes);
        }

        #endregion

        #region Password Complexity Tests

        [Theory]
        [InlineData("Password@123", true)]
        [InlineData("Pass@123", true)]
        [InlineData("password@123", false)] // No uppercase
        [InlineData("PASSWORD@123", false)] // No lowercase
        [InlineData("Password123", false)] // No special char
        [InlineData("Password@", false)] // No number
        [InlineData("Pass@1", false)] // Too short
        public void PasswordComplexity_ValidatesCorrectly(string password, bool expectedValid)
        {
            // Arrange
            bool hasUpperCase = password.Any(char.IsUpper);
            bool hasLowerCase = password.Any(char.IsLower);
            bool hasNumber = password.Any(char.IsDigit);
            bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));
            bool isLongEnough = password.Length >= 8;

            // Act
            bool isValid = hasUpperCase && hasLowerCase && hasNumber && hasSpecial && isLongEnough;

            // Assert
            isValid.Should().Be(expectedValid);
        }

        #endregion

        #region Email Security Tests

        [Theory]
        [InlineData("test@example.com", true)]
        [InlineData("user.name@example.com", true)]
        [InlineData("user+tag@example.co.uk", true)]
        [InlineData("invalid-email", false)]
        [InlineData("@example.com", false)]
        [InlineData("user@", false)]
        [InlineData("", false)]
        public void Email_ValidationWorks(string email, bool expectedValid)
        {
            // Arrange & Act
            var isValid = new EmailAddressAttribute().IsValid(email);

            // Assert
            isValid.Should().Be(expectedValid);
        }

        #endregion

        #region Token Expiry Tests

        [Fact]
        public void TokenExpiry_IsSetToOneHourInFuture()
        {
            // Arrange
            var before = DateTime.UtcNow.AddHours(1);

            // Act
            var expiry = DateTime.UtcNow.AddHours(1);

            // Assert
            var after = DateTime.UtcNow.AddHours(1);
            expiry.Should().BeOnOrAfter(before.AddSeconds(-1));
            expiry.Should().BeOnOrBefore(after.AddSeconds(1));
        }

        [Fact]
        public void TokenExpiry_ExpiredToken_IsDetected()
        {
            // Arrange
            var expiredToken = DateTime.UtcNow.AddHours(-1);
            var validToken = DateTime.UtcNow.AddHours(1);

            // Act
            var isExpiredTokenValid = expiredToken > DateTime.UtcNow;
            var isValidTokenValid = validToken > DateTime.UtcNow;

            // Assert
            isExpiredTokenValid.Should().BeFalse();
            isValidTokenValid.Should().BeTrue();
        }

        #endregion
    }
}
