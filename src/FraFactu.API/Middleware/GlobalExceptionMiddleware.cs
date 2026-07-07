using FraFactu.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Net;
using System.Text.Json;

namespace FraFactu.API.Middleware;

/// <summary>
/// Middleware para manejo global de excepciones
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
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
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var response = context.Response;
        response.ContentType = "application/json";

        object problemDetails;

        if (exception is FacturacionException facturacionEx)
        {
            if (facturacionEx is ValidationException validationEx)
            {
                problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Error de validación",
                    status = facturacionEx.StatusCode,
                    detail = facturacionEx.Message,
                    errorCode = facturacionEx.ErrorCode,
                    errors = validationEx.Errors
                };
            }
            else
            {
                problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Error de aplicación",
                    status = facturacionEx.StatusCode,
                    detail = facturacionEx.Message,
                    errorCode = facturacionEx.ErrorCode
                };
            }

            response.StatusCode = facturacionEx.StatusCode;
        }
        else if (exception is DbUpdateConcurrencyException)
        {
            response.StatusCode = 409;
            problemDetails = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                title = "Conflicto de concurrencia",
                status = 409,
                detail = "La operación no pudo completarse porque otro usuario modificó los datos simultáneamente. Por favor intente nuevamente.",
                errorCode = "CONCURRENCY_CONFLICT"
            };
        }
        else if (exception is DbUpdateException dbEx)
        {
            var pgEx = dbEx.InnerException as PostgresException;
            var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

            if (pgEx?.SqlState == PostgresErrorCodes.UniqueViolation)
            {
                response.StatusCode = 409;
                problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    title = "Registro duplicado",
                    status = 409,
                    detail = _env.IsDevelopment() ? innerMessage : "Ya existe un registro con esos datos.",
                    errorCode = "DUPLICATE_ENTRY"
                };
            }
            else if (pgEx?.SqlState == PostgresErrorCodes.CheckViolation)
            {
                response.StatusCode = 409;
                problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    title = "Stock insuficiente",
                    status = 409,
                    detail = "La operación resultaría en stock negativo. Verifique la disponibilidad e intente nuevamente.",
                    errorCode = "STOCK_CONSTRAINT_VIOLATION"
                };
            }
            else
            {
                response.StatusCode = 400;
                problemDetails = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Error de base de datos",
                    status = 400,
                    detail = _env.IsDevelopment() ? innerMessage : "Error al guardar los datos.",
                    errorCode = "DATABASE_ERROR"
                };
            }
        }
        else
        {
            problemDetails = new
            {
                type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title = "Error interno del servidor",
                status = 500,
                detail = _env.IsDevelopment()
                    ? exception.Message
                    : "Ocurrió un error inesperado. Por favor contacte al administrador.",
                errorCode = "INTERNAL_SERVER_ERROR",
                stackTrace = _env.IsDevelopment() ? exception.StackTrace : null
            };

            response.StatusCode = 500;
        }

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        await response.WriteAsync(JsonSerializer.Serialize(problemDetails, options));
    }
}
