using Commerce.Domain.Common;

namespace Commerce.Domain.Catalog;

/// <summary>
/// Money is a value object: an amount plus an ISO 4217 currency. Two Money values are equal
/// when both parts match, and arithmetic across currencies is rejected rather than silently wrong.
/// </summary>
public readonly record struct Money
{
    public Money(decimal amount, string currency)
    {
        Amount = decimal.Round(Guard.AgainstNegative(amount, nameof(amount)), 2, MidpointRounding.ToEven);
        Currency = NormaliseCurrency(currency);
    }

    public decimal Amount { get; }
    public string Currency { get; }

    public static Money Of(decimal amount, string currency) => new(amount, currency);

    public Money WithAmount(decimal amount) => new(amount, Currency);

    private static string NormaliseCurrency(string currency)
    {
        var code = Guard.AgainstNullOrWhiteSpace(currency, nameof(currency)).ToUpperInvariant();
        if (code.Length != 3)
        {
            throw new ArgumentException("Currency must be a 3 letter ISO 4217 code.", nameof(currency));
        }

        return code;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
