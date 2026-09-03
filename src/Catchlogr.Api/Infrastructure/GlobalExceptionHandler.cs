using Catchlogr.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Catchlogr.Api.Infrastructure;

/// <summary>
/// Centralized exception handler that maps application exceptions to HTTP responses.
/// Registered via <c>services.AddExceptionHandler&lt;GlobalExceptionHandler&gt;()</c>.
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        var (statusCode, message) = exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, ex.Message),
            BusinessRuleException ex => (StatusCodes.Status400BadRequest, ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = message }, ct);

        return true;
    }
}
