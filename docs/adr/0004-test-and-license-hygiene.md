# 0004. Test and license hygiene

Status: accepted

## Context

Test libraries carry licenses too, and some popular ones have moved to commercial terms or added install time
behaviour that teams do not always want. Choosing them deliberately avoids a surprise later.

## Decision

* xUnit for the test framework.
* Shouldly for assertions, rather than FluentAssertions whose version 8 moved to a commercial license for
  commercial use.
* NSubstitute for test doubles, rather than Moq, to avoid the SponsorLink episode.
* Testcontainers for integration tests, so they run against real SQL Server and real Redis Stack instead of
  in memory fakes.
* Playwright for UI tests against the running console.
* FluentValidation (Apache 2.0) for input validation.

## Consequences

* Every test dependency is permissively licensed and free for commercial use.
* Integration tests exercise the real Dapper queries, the RediSearch index, and the cache aside paths, so they
  catch provider specific issues that mocks would hide.
* The cost is that integration and UI tests need Docker and a browser, which CI provides.
