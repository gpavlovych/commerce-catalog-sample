namespace Commerce.Functions.Forecasting;

public sealed record ProductForecast(string Sku, int RecommendedReorderQuantity);

public sealed record ForecastSummary(int ProductsAnalysed, int TotalRecommendedUnits);
