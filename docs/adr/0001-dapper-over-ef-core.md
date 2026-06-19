# 0001. Dapper over EF Core

Status: accepted

## Context

The service is read heavy and latency sensitive, and the team wants the SQL it runs to be the SQL it wrote.
EF Core is productive, but it adds a translation layer between the code and the query plan, and that layer is
exactly where surprising production behaviour tends to come from: an unexpected join, a client side evaluation,
a chatty load.

## Decision

Use Dapper over Azure SQL. Queries are hand written in one place (`SqlScripts.cs`) with explicit column lists
and parameters. The aggregate keeps private setters and no parameterless constructor, so rows are read into an
explicit row record and rebuilt through a `Rehydrate` factory rather than letting an ORM reflect into private
state.

## Consequences

* The query plan is predictable because the query is literal. Performance work happens in SQL, where it belongs.
* Mapping is a few lines per repository instead of a model and a change tracker.
* There is no migrations engine. This sample creates its schema with an idempotent initializer; a real system
  would add a migration tool (for example DbUp or Flyway) without changing the data access approach.
* Persistence ignorance is preserved: the domain has no persistence attributes and no awareness of Dapper.
