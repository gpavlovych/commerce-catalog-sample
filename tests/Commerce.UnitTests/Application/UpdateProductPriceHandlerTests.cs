using Commerce.Application.Abstractions.Ports;
using Commerce.Application.Catalog.UpdatePrice;
using Commerce.Domain.Catalog;
using Commerce.Domain.Catalog.Events;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Commerce.UnitTests.Application;

public sealed class UpdateProductPriceHandlerTests
{
    private readonly IProductRepository _products = Substitute.For<IProductRepository>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();
    private readonly IEventPublisher _events = Substitute.For<IEventPublisher>();
    private readonly IPriceNotifier _notifier = Substitute.For<IPriceNotifier>();
    private readonly IClock _clock = new FixedClock(new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero));

    private UpdateProductPriceHandler CreateHandler() => new(_products, _cache, _events, _notifier, _clock);

    private static Product ExistingProduct(decimal price) =>
        Product.Create(Guid.CreateVersion7(), "SKU-1", "Widget", null, price, "EUR", Guid.CreateVersion7(),
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)).Value;

    [Fact]
    public async Task Fails_when_product_is_missing()
    {
        _products.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Product?)null);

        var result = await CreateHandler().Handle(new UpdateProductPriceCommand(Guid.CreateVersion7(), 12m), CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("not_found");
    }

    [Fact]
    public async Task Updates_invalidates_cache_and_notifies_on_change()
    {
        var product = ExistingProduct(10m);
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await CreateHandler().Handle(new UpdateProductPriceCommand(product.Id, 15m), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _products.Received(1).UpdateAsync(product, Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _events.Received(1).PublishAsync(Arg.Any<IReadOnlyCollection<Commerce.Domain.Common.IDomainEvent>>(), Arg.Any<CancellationToken>());
        await _notifier.Received(1).PriceChangedAsync(Arg.Is<ProductPriceChanged>(e => e.NewAmount == 15m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_notify_when_price_is_unchanged()
    {
        var product = ExistingProduct(10m);
        _products.GetByIdAsync(product.Id, Arg.Any<CancellationToken>()).Returns(product);

        var result = await CreateHandler().Handle(new UpdateProductPriceCommand(product.Id, 10m), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _notifier.DidNotReceive().PriceChangedAsync(Arg.Any<ProductPriceChanged>(), Arg.Any<CancellationToken>());
        await _events.DidNotReceive().PublishAsync(Arg.Any<IReadOnlyCollection<Commerce.Domain.Common.IDomainEvent>>(), Arg.Any<CancellationToken>());
    }
}
