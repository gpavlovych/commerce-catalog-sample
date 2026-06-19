using Microsoft.AspNetCore.SignalR;

namespace Commerce.Api.Realtime;

/// <summary>
/// Realtime channel for the catalog. Clients connect and receive "priceChanged" messages as prices move.
/// The hub is intentionally thin: the server pushes, clients listen.
/// </summary>
public sealed class PriceHub : Hub;
