using AssetTracker.API.Models;
using Microsoft.EntityFrameworkCore;

namespace AssetTracker.API.Data
{
    // Ensures the bootstrap administrator account always exists with Admin privileges.
    // Runs on application startup and is safe to run repeatedly (idempotent).
    public static class DbSeeder
    {
        // The designated administrator. Stored lower-case to match how registration
        // persists emails (dto.Email.ToLower()).
        private const string AdminEmail = "adaboltman@gmail.com";

        public static async Task SeedAdminAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(DbSeeder));

            var existing = await context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == AdminEmail);

            if (existing != null)
            {
                // Promote an existing account. The password is intentionally left untouched.
                bool changed = false;

                if (existing.Role != "Admin")
                {
                    existing.Role = "Admin";
                    changed = true;
                }

                // Admins must be Active to log in (Login blocks Pending/Inactive accounts).
                if (existing.AccountStatus != "Active")
                {
                    existing.AccountStatus = "Active";
                    changed = true;
                }

                if (changed)
                {
                    await context.SaveChangesAsync();
                    logger.LogInformation(
                        "Promoted existing account {Email} to Admin / Active.", AdminEmail);
                }

                return;
            }

            // Fresh database: create the bootstrap admin with a default password.
            const string defaultPassword = "Admin@123";
            var admin = new User
            {
                FullName = "System Administrator",
                Email = AdminEmail,
                EmployeeID = "ADMIN001",
                Department = "IT",
                Role = "Admin",
                AccountStatus = "Active",
                RegistrationDate = DateTime.UtcNow,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(defaultPassword)
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();

            logger.LogWarning(
                "Created bootstrap admin {Email} with the default password '{Password}'. " +
                "Change this password after first login.", AdminEmail, defaultPassword);
        }
    }
}
