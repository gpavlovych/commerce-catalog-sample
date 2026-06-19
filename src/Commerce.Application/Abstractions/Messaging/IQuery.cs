namespace Commerce.Application.Abstractions.Messaging;

/// <summary>A query reads state and returns a result. It must not mutate anything.</summary>
public interface IQuery<TResponse>;
