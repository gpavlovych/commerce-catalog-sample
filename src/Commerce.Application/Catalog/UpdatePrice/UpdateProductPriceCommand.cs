using Commerce.Application.Abstractions.Messaging;
using Commerce.Domain.Common;

namespace Commerce.Application.Catalog.UpdatePrice;

public sealed record UpdateProductPriceCommand(Guid ProductId, decimal NewPrice) : ICommand<Result>;
