namespace Commerce.Application.Abstractions.Messaging;

/// <summary>
/// Sends commands and queries to their single handler, through the configured pipeline.
/// This is the seam MediatR would normally fill. See docs/adr/0002-no-mediatr-custom-dispatcher.md.
/// </summary>
public interface IDispatcher
{
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
    Task<TResponse> Query<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
