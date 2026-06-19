using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

namespace Commerce.Functions.Forecasting;

/// <summary>
/// Durable Functions stock forecast. A timer starts the orchestration; the orchestrator fans out one
/// activity per SKU and fans the results back in. The orchestrator body is deterministic: it performs no
/// I/O and reads no clock directly, so it can be replayed safely. All real work happens in activities.
/// </summary>
public static class StockForecastOrchestration
{
    [Function(nameof(StockForecastScheduler))]
    public static async Task StockForecastScheduler(
        [TimerTrigger("0 0 2 * * *")] TimerInfo timer,
        [DurableClient] DurableTaskClient client,
        FunctionContext context)
    {
        var logger = context.GetLogger(nameof(StockForecastScheduler));
        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(StockForecastOrchestrator));
        logger.LogInformation("Started stock forecast orchestration {InstanceId}", instanceId);
    }

    [Function(nameof(StockForecastOrchestrator))]
    public static async Task<ForecastSummary> StockForecastOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var skus = await context.CallActivityAsync<IReadOnlyList<string>>(nameof(StockForecastActivities.GetActiveSkus));

        // Fan out: one activity call per SKU, executed in parallel and durably tracked.
        var forecastTasks = skus
            .Select(sku => context.CallActivityAsync<ProductForecast>(nameof(StockForecastActivities.ForecastProduct), sku))
            .ToList();

        // Fan in: wait for every activity to complete.
        var forecasts = await Task.WhenAll(forecastTasks);

        return new ForecastSummary(forecasts.Length, forecasts.Sum(f => f.RecommendedReorderQuantity));
    }
}
