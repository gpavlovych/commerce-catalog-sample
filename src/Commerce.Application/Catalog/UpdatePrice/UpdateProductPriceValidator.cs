using FluentValidation;

namespace Commerce.Application.Catalog.UpdatePrice;

public sealed class UpdateProductPriceValidator : AbstractValidator<UpdateProductPriceCommand>
{
    public UpdateProductPriceValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.NewPrice).GreaterThanOrEqualTo(0m);
    }
}
