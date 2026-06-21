using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using OfficeOpenXml;
using QuestPDF.Infrastructure;
using AssetTracker.API.Services;
using AssetTracker.API.BackgroundServices;

// Set licenses for reporting libraries
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(); // Enable controller-based routing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 1. Add database context injection using the string configuration from appsettings.json
builder.Services.AddDbContext<AssetTracker.API.Data.ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Register Phone Number Service
builder.Services.AddScoped<IPhoneNumberService, PhoneNumberService>();

// Register Overdue Reminder Processor (shared by the scheduled job and the manual admin trigger)
builder.Services.AddScoped<IOverdueReminderProcessor, OverdueReminderProcessor>();

// Register Background Service for Overdue Reminders
builder.Services.AddHostedService<OverdueReminderService>();

// Register Cross-Origin Resource Sharing (CORS) Policies
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:4201")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// 2. Register JWT Authentication Services (MOVED HERE)
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(secretKey)
    };
});

builder.Services.AddAuthorization(); // Add this line

// 3. NOW BUILD THE APPLICATION
var app = builder.Build();

// Ensure the bootstrap administrator account exists and has Admin privileges.
await AssetTracker.API.Data.DbSeeder.SeedAdminAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// 4. Middlewares run in precise order
app.UseCors("AllowAngularClient");
app.UseAuthentication(); // Checks who the user is
app.UseAuthorization();  // Checks what permissions they have

app.MapControllers(); // Map controller endpoints to routing engine

app.MapGet("/weatherforecast", () =>
{
    // ... boilerplate weather code remains down here unchanged
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Run();