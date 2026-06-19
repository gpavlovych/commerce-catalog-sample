namespace Commerce.Application.Abstractions.Behaviors;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// A cross cutting step wrapped around every handler. Behaviors run outermost-first;
/// registration order in DependencyInjection decides the nesting.
/// </summary>
public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken);
}
