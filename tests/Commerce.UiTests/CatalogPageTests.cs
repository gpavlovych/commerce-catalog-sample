using Microsoft.Playwright;
using Xunit;

namespace Commerce.UiTests;

/// <summary>
/// End to end UI tests with Playwright. They drive the demo console against a running instance, so they
/// cover the full path: browser to API to SQL and back. CI starts the API in SQLite demo mode first and
/// passes its URL through DEMO_BASE_URL.
/// </summary>
public sealed class CatalogPageTests : IAsyncLifetime
{
    private readonly string _baseUrl = Environment.GetEnvironmentVariable("DEMO_BASE_URL") ?? "http://localhost:5080";
    private IPlaywright _playwright = default!;
    private IBrowser _browser = default!;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
    }

    public async Task DisposeAsync()
    {
        await _browser.DisposeAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task Shows_seeded_products()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync(_baseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        await Assertions.Expect(page.Locator("[data-testid=product-row]").First)
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });

        await Assertions.Expect(page.GetByText("AL-6061-PLATE")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Creates_a_product_through_the_ui()
    {
        var page = await _browser.NewPageAsync();
        await page.GotoAsync(_baseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

        var sku = $"UI-{Guid.NewGuid():N}"[..10].ToUpperInvariant();

        await page.FillAsync("#sku", sku);
        await page.FillAsync("#name", "UI Created Widget");
        await page.FillAsync("#price", "33.33");
        await page.ClickAsync("#add");

        await Assertions.Expect(page.Locator($"tr[data-sku=\"{sku}\"]"))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 15_000 });
    }
}
