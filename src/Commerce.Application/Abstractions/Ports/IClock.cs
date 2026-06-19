namespace Commerce.Application.Abstractions.Ports;

/// <summary>Abstracts the system clock so time-dependent behaviour is testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
