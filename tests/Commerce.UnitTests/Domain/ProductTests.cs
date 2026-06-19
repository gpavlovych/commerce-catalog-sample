using Commerce.Domain.Catalog;
using Commerce.Domain.Catalog.Events;
using Shouldly;
using Xunit;

namespace Commerce.UnitTests.Domain;

public sealed class ProductTests
{
    private static readonly Guid SupplierId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private static Product NewProduct(decimal price = 10m) =>
        Product.Create(Guid.CreateVersion7(), "SKU-1", "Widget", null, price, "EUR", SupplierId, Now).Value;

    [Fact]
    public void Create_succeeds_with_valid_input()
    {
        var result = Product.Create(Guid.CreateVersion7(), "sku-1", "Widget", "desc", 10m, "eur", SupplierId, Now);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Sku.Value.ShouldBe("SKU-1");
        result.Value.IsActive.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_fails_without_a_name(string name)
    {
        var result = Product.Create(Guid.CreateVersion7(), "sku-1", name, null, 10m, "EUR", SupplierId, Now);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("validation");
    }

    [Fact]
    public void Create_fails_without_a_supplier()
    {
        var result = Product.Create(Guid.CreateVersion7(), "sku-1", "Widget", null, 10m, "EUR", Guid.Empty, Now);

        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void ChangePrice_raises_event_when_amount_changes()
    {
        var product = NewProduct(10m);
        var later = Now.AddMinutes(5);

        var result = product.ChangePrice(12.50m, later);

        result.IsSuccess.ShouldBeTrue();
        product.Price.Amount.ShouldBe(12.50m);
        product.UpdatedAt.ShouldBe(later);

        var raised = product.DomainEvents.OfType<ProductPriceChanged>().ShouldHaveSingleItem();
        raised.OldAmount.ShouldBe(10m);
        raised.NewAmount.ShouldBe(12.50m);
    }

    [Fact]
    public void ChangePrice_is_a_no_op_when_amount_is_unchanged()
    {
        var product = NewProduct(10m);

        var result = product.ChangePrice(10m, Now.AddMinutes(5));

        result.IsSuccess.ShouldBeTrue();
        product.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ChangePrice_fails_for_negative_amount()
    {
        var product = NewProduct(10m);

        var result = product.ChangePrice(-5m, Now);

        result.IsFailure.ShouldBeTrue();
        product.DomainEvents.ShouldBeEmpty();
    }
}
