using Commerce.Application.Abstractions.Ports;
using Commerce.Application.Catalog.CreateProduct;
using Commerce.Domain.Catalog;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Commerce.UnitTests.Application;

public sealed class CreateProductHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ISupplierRepository _suppliers = Substitute.For<ISupplierRepository>();
    private readonly IEventPublisher _events = Substitute.For<IEventPublisher>();
    private readonly IClock _clock = new FixedClock(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    private readonly Guid _supplierId = Guid.CreateVersion7();

    private CreateProductHandler CreateHandler() => new(_products, _suppliers, _events, _clock);

    private CreateProductCommand ValidCommand() =>
        new("SKU-1", "Widget", null, 10m, "EUR", _supplierId);

    [Fact]
    public async Task Fails_when_supplier_does_not_exist()
    {
        _suppliers.ExistsAsync(_supplierId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("not_found");
        await _products.DidNotReceive().AddAsync(Arg.Any<Product>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fails_when_sku_already_exists()
    {
        _suppliers.ExistsAsync(_supplierId, Arg.Any<CancellationToken>()).Returns(true);
        _products.ExistsBySkuAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("conflict");
    }

    [Fact]
    public async Task Persists_and_returns_id_on_success()
    {
        _suppliers.ExistsAsync(_supplierId, Arg.Any<CancellationToken>()).Returns(true);
        _products.ExistsBySkuAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
        await _products.Received(1).AddAsync(Arg.Is<Product>(p => p.Sku.Value == "SKU-1"), Arg.Any<CancellationToken>());
    }
}
