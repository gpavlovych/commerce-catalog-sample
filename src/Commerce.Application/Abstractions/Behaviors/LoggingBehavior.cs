using System.Diagnostics;
using Commerce.Domain.Common;
using Microsoft.Extensions.Logging;

namespace Commerce.Application.Abstractions.Behaviors;

/// <summary>Logs the name, duration, and outcome of every request, including failures and exceptions.</summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Handling {Request}", name);

        try
        {
            var response = await next();
            stopwatch.Stop();

            if (response is Result { IsFailure: true } result)
            {
                logger.LogWarning("{Request} returned failure {Code} in {Elapsed} ms: {Message}",
                    name, result.Error.Code, stopwatch.ElapsedMilliseconds, result.Error.Message);
            }
            else
            {
                logger.LogInformation("{Request} handled in {Elapsed} ms", name, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(ex, "{Request} threw after {Elapsed} ms", name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}
