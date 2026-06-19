# Architecture

This service follows Clean Architecture. The goal is a domain that does not know about the database, the
message broker, or the web, and an application layer that orchestrates use cases through interfaces it owns.

## The dependency rule

Dependencies point inward only.

```
Commerce.Api  ─┐
Commerce.Functions ─┤──►  Commerce.Application  ──►  Commerce.Domain
Commerce.Infrastructure ─┘            ▲
                                      └── implements ports defined here
```

* `Commerce.Domain` has no project references and no NuGet dependencies. It holds the `Product` aggregate,
  value objects (`Money`, `Sku`), domain events, and the `Result` type. Invariants live here and nowhere else.
* `Commerce.Application` references only `Domain`. It defines the use cases (commands and queries) and the
  ports they depend on (`IProductRepository`, `ICacheService`, `IEventPublisher`, `IPriceNotifier`, `IClock`).
  It does not know which database or broker is used.
* `Commerce.Infrastructure` references `Application` and implements the ports with Dapper, Redis, RediSearch,
  and Service Bus.
* `Commerce.Api` and `Commerce.Functions` are hosts. They compose the application and infrastructure and add
  delivery concerns: HTTP endpoints and SignalR in the API, triggers and Durable orchestrations in the worker.

Because the rule is structural, it is enforced by the build: an inner project cannot reference an outer one
because the reference simply does not exist.

## Request flow

A command, for example updating a price, flows like this:

1. The endpoint maps the HTTP request to an `UpdateProductPriceCommand` and calls the dispatcher.
2. The dispatcher resolves the single handler and wraps it in the pipeline: logging on the outside,
   validation inside. Validation failures become a 400 at the API boundary.
3. The handler loads the `Product` aggregate, calls `ChangePrice`, and the aggregate decides whether anything
   actually changed. A real change records a `ProductPriceChanged` domain event.
4. The handler persists through `IProductRepository`, invalidates the cached read model, publishes the event
   through `IEventPublisher`, and notifies realtime clients through `IPriceNotifier`.
5. Infrastructure turns those calls into Dapper SQL, a Redis delete, a Service Bus message, and a SignalR push.

## Reads, cache, and search

* Single product reads use cache aside. The query handler checks `ICacheService` first and only touches the
  database on a miss, then repopulates with a short TTL.
* Search is a derived read model. When RediSearch is available the repository queries the index for matching
  ids and loads those rows from SQL, keeping full text and prefix search off the relational database. SQL Server
  remains the source of truth. In demo mode the index is a no op and search falls back to a SQL scan, so the
  same code path works with no external services.

## Why two hosts

The API is the synchronous surface: it serves the console, the REST endpoints, the OpenAPI document, and the
SignalR hub. The Functions worker is the asynchronous surface: it consumes catalog events from Service Bus and
runs the Durable Functions stock forecast on a schedule. Both reuse the exact same `Application` and
`Infrastructure` projects, so business rules and data access never fork between the two.

## Cross provider data access

The connection factory supports SQL Server (production and integration tests) and SQLite (the zero dependency
demo). The only SQL that differs by dialect is the row limit in search (`TOP` versus `LIMIT`). SQLite has no
native GUID, decimal, or datetimeoffset type, so Dapper type handlers store those values as round trippable
text. This keeps one set of queries working against both engines without leaking provider details upward.
