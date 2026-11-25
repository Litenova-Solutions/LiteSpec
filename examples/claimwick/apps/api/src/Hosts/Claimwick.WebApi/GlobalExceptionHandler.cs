using Claimwick.Kernel.Domain;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Claimwick.WebApi;

/// <summary>The one failure map of the host: every business refusal answers its recorded status, and anything else answers 500.</summary>
/// <follows>litespec/backend/dotnet/api#map-failures-to-statuses-in-the-global-exception-handler</follows>
internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(IHostEnvironment environment, ILogger<GlobalExceptionHandler> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
        {
            return false;
        }

        if (exception is DomainException refusal)
        {
            httpContext.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await httpContext.Response.WriteAsJsonAsync(
                new { code = refusal.Code, detail = refusal.Message },
                cancellationToken);
            return true;
        }

        _logger.LogError(exception, "The request failed with an unexpected error.");
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(
            _environment.IsDevelopment()
                ? new { code = "internal_error", detail = $"{exception.GetType().Name}: {exception.Message}" }
                : new { code = "internal_error", detail = "The request failed with an unexpected error." },
            cancellationToken);
        return true;
    }
}
