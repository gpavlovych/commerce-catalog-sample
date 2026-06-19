using Commerce.Domain.Catalog;
using Shouldly;
using Xunit;

namespace Commerce.UnitTests.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void Rounds_amount_to_two_decimal_places()
    {
        var money = new Money(10.005m, "eur");

        money.Amount.ShouldBe(10.00m);
        money.Currency.ShouldBe("EUR");
    }

    [Fact]
    public void Rejects_negative_amount()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new Money(-1m, "EUR"));
    }

    [Theory]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData(" ")]
    public void Rejects_invalid_currency(string currency)
    {
        Should.Throw<ArgumentException>(() => new Money(1m, currency));
    }

    [Fact]
    public void Equal_when_amount_and_currency_match()
    {
        new Money(5m, "EUR").ShouldBe(new Money(5m, "EUR"));
        new Money(5m, "EUR").ShouldNotBe(new Money(5m, "USD"));
    }
}
