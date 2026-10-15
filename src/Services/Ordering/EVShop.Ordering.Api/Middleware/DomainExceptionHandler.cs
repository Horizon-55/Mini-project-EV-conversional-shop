using EVShop.Ordering.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace EVShop.Ordering.Api.Middleware;

// Централізований обробник доменних помилок у стандарті RFC 9457 (ProblemDetails)
// Вимога D методички (стор. 12-13)
public sealed class DomainExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext http, 
        Exception ex, 
        CancellationToken ct)
    {
        http.Response.StatusCode = ex switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            BusinessConflictException => StatusCodes.Status409Conflict, // наприклад, порушення переходів статусів або нестача на складі
            ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails =
            {
                Title = ex switch
                {
                    NotFoundException => "Ресурс не знайдено",
                    BusinessConflictException => "Конфлікт бізнес-правил",
                    ValidationException => "Помилка валідації даних",
                    _ => "Внутрішня помилка сервера"
                },
                Detail = ex.Message,
                Status = http.Response.StatusCode
            },
            Exception = ex
        });
    }
}
