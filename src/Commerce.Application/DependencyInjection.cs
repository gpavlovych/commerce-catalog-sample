using Commerce.Application.Abstractions.Behaviors;
using Commerce.Application.Abstractions.Messaging;
using Commerce.Application.Catalog;
using Commerce.Application.Catalog.CreateProduct;
using Commerce.Application.Catalog.GetProduct;
using Commerce.Application.Catalog.SearchProducts;
using Commerce.Application.Catalog.UpdatePrice;
using Commerce.Domain.Common;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDispatcher, Dispatcher>();

        // Pipeline behaviors. The last one registered is the outermost at runtime,
        // so Logging wraps Validation wraps the handler.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        // Handlers. Registered against their closed interface so the dispatcher can resolve them.
        services.AddScoped<ICommandHandler<CreateProductCommand, Result<Guid>>, CreateProductHandler>();
        services.AddScoped<ICommandHandler<UpdateProductPriceCommand, Result>, UpdateProductPriceHandler>();
        services.AddScoped<IQueryHandler<GetProductByIdQuery, Result<ProductDto>>, GetProductByIdHandler>();
        services.AddScoped<IQueryHandler<SearchProductsQuery, Result<IReadOnlyList<ProductDto>>>, SearchProductsHandler>();

        // Validators.
        services.AddScoped<IValidator<CreateProductCommand>, CreateProductValidator>();
        services.AddScoped<IValidator<UpdateProductPriceCommand>, UpdateProductPriceValidator>();

        return services;
    }
}
