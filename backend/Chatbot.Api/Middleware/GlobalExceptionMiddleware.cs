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
        ResourceNotFoundException =>
            StatusCodes.Status404NotFound,

        ConflictException =>
            StatusCodes.Status409Conflict,

        OpenAiServiceException =>
            StatusCodes.Status503ServiceUnavailable,

        HttpRequestException =>
            StatusCodes.Status503ServiceUnavailable,

        TaskCanceledException =>
            StatusCodes.Status504GatewayTimeout,

        _ => StatusCodes.Status500InternalServerError
    };

    var clientMessage = exception switch
    {
        ResourceNotFoundException =>
            exception.Message,

        ConflictException =>
            exception.Message,

        OpenAiServiceException =>
            "The AI service is temporarily unavailable.",

        HttpRequestException =>
            "The AI service is temporarily unavailable.",

        TaskCanceledException =>
            "The AI service took too long to respond.",

        _ =>
            "An unexpected server error occurred."
    };

    if (statusCode >= StatusCodes.Status500InternalServerError)
    {
        _logger.LogError(
            exception,
            "Request {TraceId} failed with status code {StatusCode}",
            context.TraceIdentifier,
            statusCode
        );
    }
    else
    {
        _logger.LogWarning(
            exception,
            "Request {TraceId} failed with status code {StatusCode}",
            context.TraceIdentifier,
            statusCode
        );
    }

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