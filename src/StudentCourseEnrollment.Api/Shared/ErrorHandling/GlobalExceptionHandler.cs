using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;

namespace StudentCourseEnrollment.Api.Shared.ErrorHandling;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        logger.LogError(
            exception,
            "Could not process a request on machine {Machine}. TraceId: {TraceId}. Path: {Path}, Method: {Method}",
            Environment.MachineName,
            traceId,
            httpContext.Request.Path,
            httpContext.Request.Method);

        await Results.Problem(
            title: "An error occurred while processing your request.",
            statusCode: StatusCodes.Status500InternalServerError,
            extensions: new Dictionary<string, object?>
            {
                {"traceId", traceId}
            }
        ).ExecuteAsync(httpContext);

        return true;
    }
}
