using Commerce.Application.Abstractions.Ports;

namespace Commerce.UnitTests;

internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
