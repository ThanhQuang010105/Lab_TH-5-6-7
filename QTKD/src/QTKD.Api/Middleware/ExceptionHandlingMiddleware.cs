using System.Text.Json;
using QTKD.Application.DTOs;
using QTKD.Domain.Exceptions;

namespace QTKD.Api.Middleware;

/// <summary>
/// Bắt BusinessRuleException và chuyển thành HTTP response JSON chuẩn.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next   = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (BusinessRuleException ex)
        {
            _logger.LogWarning("BusinessRuleException [{Code}]: {Message}", ex.Code, ex.Message);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode  = ex.Code switch
            {
                "NOT_FOUND"   => StatusCodes.Status404NotFound,
                "WRONG_STATE" => StatusCodes.Status409Conflict,
                "CTR-01"      => StatusCodes.Status409Conflict,
                "CTR-02"      => StatusCodes.Status400BadRequest,
                "CTR-03"      => StatusCodes.Status400BadRequest,
                _             => StatusCodes.Status400BadRequest
            };

            var body = new BusinessErrorResponse(ex.Code, ex.Message, ex.Details);
            var json = JsonSerializer.Serialize(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.ContentType = "application/json";
            context.Response.StatusCode  = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("{\"code\":\"INTERNAL_ERROR\",\"message\":\"Lỗi hệ thống nội bộ.\",\"details\":[]}");
        }
    }
}

