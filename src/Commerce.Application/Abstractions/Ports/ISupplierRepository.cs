namespace Commerce.Application.Abstractions.Ports;

public interface ISupplierRepository
{
    Task<bool> ExistsAsync(Guid supplierId, CancellationToken cancellationToken);
}
