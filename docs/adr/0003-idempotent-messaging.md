# 0003. Idempotent messaging

Status: accepted

## Context

Service Bus gives at least once delivery. A consumer can see the same message more than once after a transient
failure or a lock renewal timeout, so processing must not double count. This is the class of bug that looks
fine in a demo and corrupts data under load.

## Decision

Set the Service Bus `MessageId` to the domain event id when publishing. Enable duplicate detection on the topic
so the broker drops re-sends that share a MessageId within the detection window. Beyond that, write consumers to
be safe to run more than once: the event consumer in this sample only reads the message and pushes a SignalR
notification, with no side effect that needs undoing.

## Consequences

* Duplicate publishes inside the detection window are dropped by the broker, not by ad hoc application code.
* Consumers that do have side effects should persist an idempotency key (the event id) before acting, and skip
  events they have already handled. The shape is in place for that.
* The Durable Functions orchestrator is deterministic and replay safe by construction: it performs no I/O and
  reads no clock directly, so the runtime can replay it without changing the result.
