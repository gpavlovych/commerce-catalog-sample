using Commerce.Application.Abstractions.Messaging;
using Commerce.Application.Catalog;
using Commerce.Application.Catalog.CreateProduct;
using Commerce.Application.Catalog.GetProduct;
using Commerce.Application.Catalog.SearchProducts;
using Commerce.Application.Catalog.UpdatePrice;
using Commerce.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Commerce.Api.Endpoints;

internal static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        group.MapPost("/", async (CreateProductRequest request, IDispatcher dispatcher, CancellationToken ct) =>
            {
                var result = await dispatcher.Send(new CreateProductCommand(
                    request.Sku, request.Name, request.Description, request.Price, request.Currency, request.SupplierId), ct);

                return result.IsSuccess
                    ? Results.Created($"/api/products/{result.Value}", new { id = result.Value })
                    : Problem(result.Error);
            })
            .WithName("CreateProduct")
            .Produces(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            {
                var result = await dispatcher.Query(new GetProductByIdQuery(id), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error);
            })
            .WithName("GetProductById")
            .Produces<ProductDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/price", async (Guid id, UpdatePriceRequest request, IDispatcher dispatcher, CancellationToken ct) =>
            {
                var result = await dispatcher.Send(new UpdateProductPriceCommand(id, request.Price), ct);
                return result.IsSuccess ? Results.NoContent() : Problem(result.Error);
            })
            .WithName("UpdateProductPrice")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", async (string? search, int? take, IDispatcher dispatcher, CancellationToken ct) =>
            {
                var result = await dispatcher.Query(new SearchProductsQuery(search ?? string.Empty, take ?? 20), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error);
            })
            .WithName("SearchProducts")
            .Produces<IReadOnlyList<ProductDto>>(StatusCodes.Status200OK);

        return app;
    }

    private static IResult Problem(Error error)
    {
        var status = error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "conflict" => StatusCodes.Status409Conflict,
            "validation" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(detail: error.Message, statusCode: status, title: error.Code);
    }

    internal sealed record CreateProductRequest(
        string Sku,
        string Name,
        string? Description,
        decimal Price,
        string Currency,
        Guid SupplierId);

    internal sealed record UpdatePriceRequest(decimal Price);
}
