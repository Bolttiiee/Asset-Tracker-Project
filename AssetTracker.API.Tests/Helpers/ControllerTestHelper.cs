using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AssetTracker.API.Tests.Helpers
{
    public static class ControllerTestHelper
    {
        /// <summary>
        /// Sets up an authenticated user context with claims for controller testing
        /// </summary>
        public static void SetupAuthenticatedUser(ControllerBase controller, string email = "test@example.com", string role = "Admin")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.NameIdentifier, "1")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthentication");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = claimsPrincipal
            };

            // Add request headers for testing
            httpContext.Request.Headers["Origin"] = "http://localhost:4200";

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }
    }
}
