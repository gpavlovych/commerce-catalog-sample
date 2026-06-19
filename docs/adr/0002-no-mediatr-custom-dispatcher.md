# 0002. No MediatR, a small custom dispatcher

Status: accepted

## Context

The application layer benefits from a command and query seam with a cross cutting pipeline for validation and
logging. MediatR is the usual choice, but its licensing changed for newer versions, and for a service this size
the behaviour we actually use is small and worth owning.

## Decision

Implement a minimal dispatcher. `IDispatcher` sends commands and queries to a single handler resolved from the
container, wrapped in registered `IPipelineBehavior` steps. Reflection is confined to the one dispatcher class;
handlers, commands, and behaviors are statically typed. Registration order decides nesting, so logging wraps
validation wraps the handler.

## Consequences

* No dependency on a third party mediator and no licensing question.
* The pattern is explicit and about forty lines, which makes the request flow easy to read and to debug.
* We own the edge cases. If we later need streaming or notifications we add them deliberately rather than
  adopting a whole library for one feature.
* The trade off is that we maintain a small amount of infrastructure code. For this size that is the cheaper
  side of the trade.
