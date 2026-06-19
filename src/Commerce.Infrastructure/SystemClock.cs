using Commerce.Application.Abstractions.Ports;

namespace Commerce.Infrastructure;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
