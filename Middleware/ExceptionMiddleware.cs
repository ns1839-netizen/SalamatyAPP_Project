using System.Text.Json;

namespace Salamaty.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);

                // ===== Handle 404 =====
                if (context.Response.StatusCode == 404 && !context.Response.HasStarted)
                {
                    await WriteResponse(
                        context,
                        404,
                        "The requested endpoint does not exist."
                    );
                }

                // ===== Handle 405 =====
                if (context.Response.StatusCode == 405 && !context.Response.HasStarted)
                {
                    await WriteResponse(
                        context,
                        405,
                        "This endpoint does not allow this HTTP method. Please use the correct method."
                    );
                }
            }
            catch (Exception ex)
            {
                // ===== Handle 500 =====
                await WriteResponse(
                    context,
                    500,
                    "Internal server error",
                    ex.Message
                );
            }
        }

        private static async Task WriteResponse(
            HttpContext context,
            int statusCode,
            string message,
            object? errors = null)
        {
            context.Response.Clear();
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;

            var response = new
            {
                success = false,
                statusCode,
                message,
                details = errors,
                traceId = context.TraceIdentifier
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response, jsonOptions)
            );
        }
    }
}
