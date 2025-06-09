using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Net;
using System.Text.Json;

namespace LinearOptimizationFoodApp.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log the exception with structured data
            _logger.LogError(exception,
                "Unhandled exception occurred. RequestPath: {RequestPath}, Method: {Method}, UserId: {UserId}",
                context.Request.Path,
                context.Request.Method,
                context.User?.Identity?.Name ?? "Anonymous");

            // Check if this is an API request or web request
            var isApiRequest = IsApiRequest(context);

            if (isApiRequest)
            {
                await HandleApiExceptionAsync(context, exception);
            }
            else
            {
                await HandleWebExceptionAsync(context, exception);
            }
        }

        private static bool IsApiRequest(HttpContext context)
        {
            return context.Request.Path.StartsWithSegments("/api") ||
                   context.Request.Headers["Accept"].ToString().Contains("application/json") ||
                   context.Request.ContentType?.Contains("application/json") == true;
        }

        private static async Task HandleApiExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var (statusCode, message) = GetErrorResponse(exception);
            context.Response.StatusCode = (int)statusCode;

            var response = new ApiErrorResponse
            {
                Message = message,
                StatusCode = (int)statusCode,
                Timestamp = DateTime.UtcNow,
                Path = context.Request.Path,
                Method = context.Request.Method
            };

            // Don't expose internal details in production
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                response.Details = exception.ToString();
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private static async Task HandleWebExceptionAsync(HttpContext context, Exception exception)
        {
            var (statusCode, message) = GetErrorResponse(exception);

            // Store error details in TempData for display
            if (context.RequestServices.GetService<ITempDataDictionaryFactory>() != null)
            {
                var tempDataProvider = context.RequestServices.GetRequiredService<ITempDataProvider>();
                var tempDataDictionary = new TempDataDictionary(context, tempDataProvider);

                tempDataDictionary["ErrorMessage"] = message;
                tempDataDictionary["ErrorStatusCode"] = (int)statusCode;

                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    tempDataDictionary["ErrorDetails"] = exception.ToString();
                }

                tempDataDictionary.Save();
            }

            // Redirect to error page
            context.Response.StatusCode = (int)statusCode;
            context.Response.Redirect("/Home/Error");
        }

        private static (HttpStatusCode statusCode, string message) GetErrorResponse(Exception exception)
        {
            return exception switch
            {
                ArgumentNullException => (HttpStatusCode.BadRequest, "Invalid request: Required parameter is missing."),
                ArgumentException => (HttpStatusCode.BadRequest, "Invalid request: Parameter value is invalid."),
                UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Access denied. Please check your permissions."),
                KeyNotFoundException => (HttpStatusCode.NotFound, "The requested resource was not found."),
                InvalidOperationException => (HttpStatusCode.Conflict, "The operation cannot be completed at this time."),
                TimeoutException => (HttpStatusCode.RequestTimeout, "The request timed out. Please try again."),
                NotSupportedException => (HttpStatusCode.NotImplemented, "This operation is not supported."),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
            };
        }
    }

    // API Error Response Model
    public class ApiErrorResponse
    {
        public string Message { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public DateTime Timestamp { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string? Details { get; set; } // Only populated in development
    }
}
