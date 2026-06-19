using Commerce.Application.Abstractions.Behaviors;
using Microsoft.Extensions.DependencyInjection;

namespace Commerce.Application.Abstractions.Messaging;

/// <summary>
/// Resolves the one handler registered for a request, then wraps it in the registered
/// pipeline behaviors. Reflection is confined to this single class; everything else is statically typed.
/// </summary>
internal sealed class Dispatcher(IServiceProvider provider) : IDispatcher
{
    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
        => Dispatch<TResponse>(command, typeof(ICommandHandler<,>), cancellationToken);

    public Task<TResponse> Query<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
        => Dispatch<TResponse>(query, typeof(IQueryHandler<,>), cancellationToken);

    private Task<TResponse> Dispatch<TResponse>(object request, Type openHandlerType, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();

        var handlerType = openHandlerType.MakeGenericType(requestType, typeof(TResponse));
        var handler = provider.GetService(handlerType)
            ?? throw new InvalidOperationException(
                $"No handler registered for {requestType.Name}. Register it in Commerce.Application.DependencyInjection.");

        var handleMethod = handlerType.GetMethod("Handle")
            ?? throw new InvalidOperationException($"Handler {handlerType.Name} has no Handle method.");

        RequestHandlerDelegate<TResponse> pipeline = () =>
            (Task<TResponse>)handleMethod.Invoke(handler, [request, cancellationToken])!;

        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviorHandle = behaviorType.GetMethod("Handle")!;
        var behaviors = (IEnumerable<object?>)provider.GetServices(behaviorType);

        // Registration order decides nesting: the last behavior registered becomes the outermost.
        foreach (var behavior in behaviors)
        {
            if (behavior is null)
            {
                continue;
            }

            var inner = pipeline;
            var current = behavior;
            pipeline = () => (Task<TResponse>)behaviorHandle.Invoke(current, [request, inner, cancellationToken])!;
        }

        return pipeline();
    }
}
