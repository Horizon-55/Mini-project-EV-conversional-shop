using EVShop.Catalog.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace EVShop.Catalog.Api.Middleware;

// Централізований обробник винятків у стандарті RFC 9457 (ProblemDetails)
// Вимога 4.4 методички (сторінка 12-13)
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails, ILogger<DomainExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            EntityNotFoundException => StatusCodes.Status404NotFound,
            ValidationException => StatusCodes.Status400BadRequest,
            ConflictException => StatusCodes.Status409Conflict,
            DomainException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = statusCode;

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Неперехоплена помилка сервера: {Message} | Path: {Path}", exception.Message, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Доменна помилка ({StatusCode}): {Message} | Path: {Path}", statusCode, exception.Message, httpContext.Request.Path);
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception
        });
    }
}
