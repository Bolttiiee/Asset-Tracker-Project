using Microsoft.EntityFrameworkCore;
using AssetTracker.API.Data;
using AssetTracker.API.Models;

namespace AssetTracker.API.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            return context;
        }

        public static ApplicationDbContext CreateContextWithSeedData()
        {
            var context = CreateInMemoryContext();

            // Seed test users
            context.Users.AddRange(
                new User
                {
                    UserID = 1,
                    FullName = "Test Admin",
                    Email = "admin@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Department = "IT",
                    EmployeeID = "EMP001",
                    Role = "Admin",
                    AccountStatus = "Active",
                    RegistrationDate = DateTime.UtcNow
                },
                new User
                {
                    UserID = 2,
                    FullName = "Test User",
                    Email = "user@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                    Department = "Sales",
                    EmployeeID = "EMP002",
                    Role = "Standard User",
                    AccountStatus = "Active",
                    RegistrationDate = DateTime.UtcNow
                },
                new User
                {
                    UserID = 3,
                    FullName = "Pending User",
                    Email = "pending@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pending@123"),
                    Department = "HR",
                    EmployeeID = "EMP003",
                    Role = "Standard User",
                    AccountStatus = "Pending",
                    RegistrationDate = DateTime.UtcNow
                },
                new User
                {
                    UserID = 4,
                    FullName = "Inactive User",
                    Email = "inactive@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Inactive@123"),
                    Department = "Finance",
                    EmployeeID = "EMP004",
                    Role = "Standard User",
                    AccountStatus = "Inactive",
                    RegistrationDate = DateTime.UtcNow
                },
                new User
                {
                    UserID = 5,
                    FullName = "Rejected User",
                    Email = "rejected@test.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Rejected@123"),
                    Department = "Operations",
                    EmployeeID = "EMP005",
                    Role = "Standard User",
                    AccountStatus = "Rejected",
                    RegistrationDate = DateTime.UtcNow
                }
            );

            // Seed test assets
            context.Assets.AddRange(
                new Asset
                {
                    AssetID = 1,
                    Name = "Test Laptop",
                    SerialNumber = "SN001",
                    Category = "Laptop",
                    Status = "Available",
                    PurchaseDate = DateTime.UtcNow.AddYears(-1),
                    Value = 1200.00m
                },
                new Asset
                {
                    AssetID = 2,
                    Name = "Test Monitor",
                    SerialNumber = "SN002",
                    Category = "Monitor",
                    Status = "Checked Out",
                    PurchaseDate = DateTime.UtcNow.AddYears(-1),
                    Value = 300.00m
                }
            );

            context.SaveChanges();
            return context;
        }
    }
}
