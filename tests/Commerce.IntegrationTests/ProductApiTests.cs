using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Xunit;

namespace Commerce.IntegrationTests;

public sealed class ProductApiTests(CommerceApiFactory factory) : IClassFixture<CommerceApiFactory>
{
    // Matches the supplier seeded by DatabaseInitializer.
    private static readonly Guid SeededSupplierId = new("0192b4d0-0000-7000-8000-0000000000a1");

    private sealed record CreateProductRequest(string Sku, string Name, string? Description, decimal Price, string Currency, Guid SupplierId);
    private sealed record UpdatePriceRequest(decimal Price);
    private sealed record ProductResponse(Guid Id, string Sku, string Name, decimal Price, string Currency, bool IsActive);
    private sealed record CreatedResponse(Guid Id);

    [Fact]
    public async Task Create_then_get_returns_the_product()
    {
        var client = factory.CreateClient();
        var sku = $"IT-{Guid.NewGuid():N}"[..12];

        var create = await client.PostAsJsonAsync("/api/products",
            new CreateProductRequest(sku, "Integration Widget", "from test", 19.99m, "EUR", SeededSupplierId));

        create.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<CreatedResponse>();
        created.ShouldNotBeNull();

        var get = await client.GetAsync($"/api/products/{created!.Id}");
        get.StatusCode.ShouldBe(HttpStatusCode.OK);

        var product = await get.Content.ReadFromJsonAsync<ProductResponse>();
        product!.Sku.ShouldBe(sku.ToUpperInvariant());
        product.Price.ShouldBe(19.99m);
    }

    [Fact]
    public async Task Update_price_changes_the_stored_value()
    {
        var client = factory.CreateClient();
        var sku = $"IT-{Guid.NewGuid():N}"[..12];

        var created = await (await client.PostAsJsonAsync("/api/products",
            new CreateProductRequest(sku, "Repricing Widget", null, 10m, "EUR", SeededSupplierId)))
            .Content.ReadFromJsonAsync<CreatedResponse>();

        var update = await client.PutAsJsonAsync($"/api/products/{created!.Id}/price", new UpdatePriceRequest(25.50m));
        update.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var product = await (await client.GetAsync($"/api/products/{created.Id}")).Content.ReadFromJsonAsync<ProductResponse>();
        product!.Price.ShouldBe(25.50m);
    }

    [Fact]
    public async Task Rejects_invalid_input_with_400()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("", "", null, -1m, "EURO", SeededSupplierId));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Returns_404_for_unknown_supplier()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/products",
            new CreateProductRequest($"IT-{Guid.NewGuid():N}"[..12], "Orphan", null, 5m, "EUR", Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_finds_a_created_product()
    {
        var client = factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        var sku = $"SR-{marker}";

        await client.PostAsJsonAsync("/api/products",
            new CreateProductRequest(sku, $"Searchable {marker}", null, 7m, "EUR", SeededSupplierId));

        // The RediSearch index updates asynchronously; poll briefly to avoid a race.
        ProductResponse[]? matches = null;
        for (var attempt = 0; attempt < 10; attempt++)
        {
            matches = await client.GetFromJsonAsync<ProductResponse[]>($"/api/products?search={marker}");
            if (matches is { Length: > 0 })
            {
                break;
            }

            await Task.Delay(250);
        }

        matches.ShouldNotBeNull();
        matches!.ShouldContain(p => p.Sku == sku);
    }
}
