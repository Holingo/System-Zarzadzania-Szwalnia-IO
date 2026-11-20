using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Szwalnia.API.Domain;

namespace Szwalnia.API.Services;

public class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var error = exception switch
        {
            BusinessException business => (business.StatusCode, business.Message),
            DbUpdateConcurrencyException => (409, "Dane się zmieniły. Odśwież listę i ponów operację."),
            DbUpdateException { InnerException: Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 or 547 } }
                => (409, "Operacja narusza unikalność lub powiązanie danych."),
            _ => (500, "Nie udało się wykonać operacji.")
        };
        if (error.Item1 == 500)
            logger.LogError(exception, "Błąd podczas obsługi {Method} {Path}.", context.Request.Method, context.Request.Path);

        await Results.Problem(statusCode: error.Item1, title: error.Item2).ExecuteAsync(context);
        return true;
    }
}
