using Chatbot.Api.Exceptions;
using Chatbot.Api.Models.Responses;

namespace Chatbot.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
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
        catch (OperationCanceledException)
            when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Request {TraceId} was cancelled by the client",
                context.TraceIdentifier
            );
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var statusCode = exception switch
        {
            OpenAiServiceException => StatusCodes.Status503ServiceUnavailable,

            HttpRequestException => StatusCodes.Status503ServiceUnavailable,

            TaskCanceledException => StatusCodes.Status504GatewayTimeout,

            InvalidOperationException =>
                StatusCodes.Status500InternalServerError,

            _ => StatusCodes.Status500InternalServerError
        };

        var clientMessage = statusCode switch
        {
            StatusCodes.Status503ServiceUnavailable =>
                "The AI service is temporarily unavailable.",

            StatusCodes.Status504GatewayTimeout =>
                "The AI service took too long to respond.",

            _ => "An unexpected server error occurred."
        };

        _logger.LogError(
            exception,
            "Request {TraceId} failed with status code {StatusCode}",
            context.TraceIdentifier,
            statusCode
        );

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            Error = clientMessage,
            TraceId = context.TraceIdentifier
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }
}