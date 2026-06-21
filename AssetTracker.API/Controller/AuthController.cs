using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using AssetTracker.API.Data;
using AssetTracker.API.Models;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AssetTracker.API.Services;

namespace AssetTracker.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;
        private readonly IPhoneNumberService _phoneNumberService;

        // Inject both our database context engine and app configuration settings
        public AuthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailService emailService,
            ILogger<AuthController> logger,
            IPhoneNumberService? phoneNumberService = null)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _logger = logger;
            _phoneNumberService = phoneNumberService ?? new PhoneNumberService();
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Enforce strict backend password complexity rules
            bool hasUpperCase = dto.Password.Any(char.IsUpper);
            bool hasNumber = dto.Password.Any(char.IsDigit);
            bool hasSpecial = dto.Password.Any(ch => !char.IsLetterOrDigit(ch));

            if (dto.Password.Length < 8 || !hasUpperCase || !hasNumber || !hasSpecial)
            {
                return BadRequest(new { message = "Password does not meet complexity requirements (8+ characters, 1 uppercase, 1 number, and 1 special character)." });
            }

            // 2. Check if Email or EmployeeID already exists
            bool emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            bool empIdExists = await _context.Users.AnyAsync(u => u.EmployeeID.ToLower() == dto.EmployeeID.ToLower());

            if (emailExists)
            {
                return BadRequest(new { message = "An account with this email address already exists." });
            }

            if (empIdExists)
            {
                return BadRequest(new { message = "An account with this Employee ID already exists." });
            }

            // 2b. Validate and normalize the optional phone number
            if (!_phoneNumberService.IsValid(dto.PhoneNumber))
            {
                return BadRequest(new { message = "The phone number provided is not a valid South African phone number." });
            }
            string? normalizedPhoneNumber = _phoneNumberService.Normalize(dto.PhoneNumber);

            // 3. SECURELY HASH THE PASSWORD USING BCRYPT
            string securePasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // 4. Map DTO fields over to our User model
            var newUser = new User
            {
                FullName = dto.FullName,
                Email = dto.Email.ToLower(),
                EmployeeID = dto.EmployeeID.ToUpper(),
                Department = dto.Department,
                PhoneNumber = normalizedPhoneNumber, // Optional phone number
                Role = "Standard User",
                AccountStatus = "Pending",
                RegistrationDate = DateTime.UtcNow,
                PasswordHash = securePasswordHash
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "User registration submitted successfully. Account is pending administrative approval." });
        }

        // ==========================================
        // THE MISSING LOGIN METHOD IS BACK!
        // ==========================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // 1. Locate user records by Email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email address or password." });
            }

            // 2. ENFORCE STATUS RESTRICTION: Only Active accounts may log in.
            // Allowlist by design - any status other than "Active" (Pending, Inactive,
            // Rejected, or any future status) is denied by default.
            if (!user.AccountStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                var statusMessage = user.AccountStatus switch
                {
                    var s when s.Equals("Pending", StringComparison.OrdinalIgnoreCase)
                        => "Your registration request is still pending administrative approval. Please contact your manager.",
                    var s when s.Equals("Rejected", StringComparison.OrdinalIgnoreCase)
                        => "Your registration request was not approved. Please contact your manager.",
                    var s when s.Equals("Inactive", StringComparison.OrdinalIgnoreCase)
                        => "Your account has been deactivated. Please contact your administrator.",
                    _ => "Your account is not permitted to log in. Please contact your administrator."
                };

                return StatusCode(403, new { message = statusMessage });
            }

            // 3. VERIFY SECURE PASSWORD MATCH USING BCRYPT
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Invalid email address or password." });
            }

            // 4. GENERATE SIGNED JWT AUTHENTICATION TOKEN BADGE
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(1), // Token remains active for 24 hours
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(secretKey), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(tokenDescriptor);
            string tokenString = tokenHandler.WriteToken(securityToken);

            // 5. Dispatch success response token pack back to client workspace
            return Ok(new
            {
                Token = tokenString,
                Expiration = tokenDescriptor.Expires,
                User = new { user.FullName, user.Email, user.Role }
            });
        }

        // POST: api/auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            // 1. Locate the employee account by email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            // 2. Security Best Practice: To prevent email harvesting/fishing attacks, 
            // we return an Ok status even if the email doesn't exist.
            if (user == null) 
            {
                _logger.LogInformation("Forgot-password requested for non-existent account: {Email}", dto.Email);
                return Ok(new { message = "If the email exists in our records, a secure reset token has been dispatched." });
            }

            // 3. Generate a high-entropy cryptographically secure random token string
            byte[] randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            string secureToken = Convert.ToHexString(randomBytes);

            // 4. Update user record with token parameters (Valid for 1 Hour)
            user.PasswordResetToken = secureToken;
            user.ResetTokenExpiry = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync();

            // 5. Send secure reset link over configured SMTP transport.
            var configuredUiBaseUrl = _configuration["Frontend:BaseUrl"];
            var origin = Request.Headers.Origin.FirstOrDefault();
            var uiBaseUrl = !string.IsNullOrWhiteSpace(configuredUiBaseUrl)
                ? configuredUiBaseUrl
                : (!string.IsNullOrWhiteSpace(origin) ? origin : "http://localhost:4201");

            var resetLink = $"{uiBaseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(secureToken)}";

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
                _logger.LogInformation("Password reset email dispatched to {Email}", user.Email);
            }
            catch (Exception ex)
            {
                // Keep response generic to avoid account enumeration. Log delivery failures server-side.
                _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
            }

            return Ok(new { message = "If the email exists in our records, a secure reset token has been dispatched." });
        }

        // POST: api/auth/reset-password
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            // 1. Find the user holding this exact temporary token
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == dto.Token);

            if (user == null)
            {
                return BadRequest(new { message = "Invalid or compromised security token." });
            }

            // 2. Verify that the token window hasn't expired yet
            if (user.ResetTokenExpiry < DateTime.UtcNow)
            {
                return BadRequest(new { message = "Security token has expired. Please request a fresh recovery sequence." });
            }

            // 3. Overwrite password with a fresh hash string 
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword); 

            // 4. CRITICAL: Clear the token tracks so they can NEVER be reused maliciously
            user.PasswordResetToken = null;
            user.ResetTokenExpiry = null;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Your corporate account password has been successfully updated. Proceed to login." });
        }
    }
}