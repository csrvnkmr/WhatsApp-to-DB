using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Threading.Tasks;
using WhatsAppToDB;

namespace WhatsAppToDB
{
    // ==========================================================
    // TOKEN AUTH MIDDLEWARE
    // Reads:
    // Authorization: Bearer xxxxx
    // Validates token
    // Stores username in HttpContext.Items["UserName"]
    // ==========================================================
    public class TokenAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public TokenAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // ----------------------------------------------
            // Skip login endpoint
            // ----------------------------------------------
            var path = context.Request.Path.Value?.ToLower();

            if (path == "/login")
            {
                await _next(context);
                return;
            }

            // ----------------------------------------------
            // Read Authorization header
            // ----------------------------------------------
            var authHeader =
                context.Request.Headers["Authorization"]
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(authHeader) ||
                !authHeader.StartsWith("Bearer "))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Missing token");
                return;
            }

            var token =
                authHeader.Substring("Bearer ".Length).Trim();

            // ----------------------------------------------
            // Validate token
            // Replace with your real validation method
            // ----------------------------------------------
            var (isValid,session) =
                UserService.ValidateToken(token);

            if (!isValid || session == null)
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Invalid token");
                return;
            }

            // ----------------------------------------------
            // Store values for endpoint use
            // ----------------------------------------------
            context.Items["UserName"] = session.WhatsAppNumber;
            context.Items["Token"] = token;

            await _next(context);
        }
    }
}