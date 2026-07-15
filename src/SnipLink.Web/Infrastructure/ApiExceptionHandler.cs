using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SnipLink.Application;

namespace SnipLink.Web.Infrastructure;

public class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = Map(exception);

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception.");

        httpContext.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = status == StatusCodes.Status500InternalServerError ? null : exception.Message
            }
        });
    }

    private static (int Status, string Title) Map(Exception exception) => exception switch
    {
        LinkNotFoundException => (StatusCodes.Status404NotFound, "Link not found"),
        OwnerTokenMismatchException => (StatusCodes.Status403Forbidden, "Forbidden"),
        AliasConflictException => (StatusCodes.Status409Conflict, "Alias already in use"),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
    };
}
