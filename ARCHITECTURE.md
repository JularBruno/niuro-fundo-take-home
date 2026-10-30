# Architecture

The goal was the smallest design that meets the spec: a rule engine, one transaction for customer + application + event, the returning-customer path, and background delivery. Every abstraction below exists because something uses it.

## 1. Structure and layers

```
            ┌────────────── LoanApp.Api ──────────────┐
            │ controller, request DTOs, error handling │
            └───────┬─────────────────────────┬────────┘
                    ▼                         ▼
      LoanApp.Application  ◄────────  LoanApp.Infrastructure
      use cases, rule engine,         EF Core/Postgres, outbox processor,
      ports (interfaces)              HTTP client, config blacklist
                    │
                    ▼
            LoanApp.Domain   (no dependencies)
```

Dependencies point inward. Domain references nothing. Application references only Domain. Infrastructure implements Application's ports. Api wires everything up.

| Project | Responsibility | Key types |
|---|---|---|
| **Domain** | Invariants that must always hold | `Customer` (aggregate root), `LoanApplication`, `Ssn` and `Address` (value objects), `DomainException` |
| **Application** | Business decisions and orchestration | `LoanDecisionEngine` + `IDenyRule`, `SubmitLoanApplicationHandler`, `LoanApplicationSubmitted` + its handler, ports in `Abstractions/` |
| **Infrastructure** | Replaceable technical details | `AppDbContext` (also the unit of work), `CustomerRepository`, `OutboxEventPublisher`, `OutboxProcessor`, `HttpExternalCustomerService`, `ConfiguredSsnBlacklist` |
| **Api** | HTTP only | `LoanApplicationsController`, which maps request → `LoanRequest` → handler → response, and `DomainExceptionHandler` |

**Domain rules.**
- **`Ssn`** normalizes to 9 digits, so `123-45-6789` and `123456789` are the same customer. Its `ToString()` is masked, so an SSN never reaches a log.
- **`Address`** uppercases the state, so `ny` is treated as `NY`.
- **`Customer` owns its single `LoanApplication`.** The `LoanApplication` constructor is `internal`, and `customer.Apply(amount)` creates the application or updates it. "One customer, one application" can't be broken from code. Unique indexes on `Customers.Ssn` and `Applications.CustomerId` enforce it in the database too.

**Validation** has three layers:
1. The frontend shows friendly messages.
2. DataAnnotations on the request DTO return a 400 with errors per field.
3. The domain types are the guarantee. A `DomainException` becomes a 400.

Denials are not validation errors. They return `200 { approved: false, denialReason }`.

## 2. Rule engine

`LoanDecisionEngine` runs every registered `IDenyRule` in order. The first rule that returns a `DenialReason` decides the outcome. If none does, the loan is approved.

| Rule | Denies when | Reason |
|---|---|---|
| `UnsupportedStateRule` | `Address.State` is `NY` | `StateNotSupported` |
| `BlacklistedSsnRule` | `ISsnBlacklist` contains the SSN. The list comes from `appsettings.json` → `Blacklist:Ssns` | `SsnBlacklisted` |

**Adding a rule:**
1. Create a class implementing `IDenyRule` in `Application/Decisions/Rules`.
2. Register it in `Application/DependencyInjection.cs` with one line.
3. If it needs its own reason, add a value to `DenialReason`.

No existing rule or the engine changes. Rules are async, so a database-backed blacklist would be a new `ISsnBlacklist` implementation and nothing else.

## 3. Transaction (one unit of work)

`SubmitLoanApplicationHandler` follows these steps:

```
decide (rules)  ─ denied ─►  return reason (nothing written)
      │ approved
      ▼
load customer by SSN ─► update details  |  create + add
customer.Apply(amount)                       (create or update the single application)
events.Publish(LoanApplicationSubmitted)     (adds an OutboxMessages row, does NOT send)
unitOfWork.SaveChangesAsync()                ONE Postgres transaction: customer + application + event
```

This is the **transactional outbox**. "Publishing the event" means committing its row together with the business data, so neither can exist without the other. The returning-customer path is the same transaction, with UPDATEs instead of INSERTs.

## 4. Background event → external service

`OutboxProcessor` is a `BackgroundService`. Every 5 s it reads pending rows, oldest first, up to 20 at a time. It runs outside the HTTP request, so the user never waits on the external service.

For each row, `LoanApplicationSubmittedHandler` (Application layer) makes the business decision: `IsNewCustomer` → `CreateAsync`, otherwise `UpdateAsync`. `HttpExternalCustomerService` only knows the HTTP contract:

| Customer | Request | Body |
|---|---|---|
| New | `POST /customers` | `{ customer: { id, ssn, firstName, lastName, companyName, address }, application: { id, requestedAmount } }` |
| Returning | `PUT /customers/{customerId}` | Same |

**Contract choices:**
- **The URL is keyed by our `customerId`, never the SSN,** so no PII appears in URLs or logs.
- **A 2xx means delivered.** Anything else, or the 10 s timeout, throws, and the outbox retries.
- **Delivery is at-least-once, so both calls are safe to repeat.** The mock treats a repeated `POST` with the same `customer.id` as a no-op. A `PUT` with the same data is naturally idempotent.
- **The SSN is in the body,** because the spec says to send "the customer" and a real partner needs to identify the person. In production, this call needs HTTPS and authentication.

**Retries and order:**
- **Success:** `ProcessedAt` is set.
- **Failure:** `Attempts++`, `LastError` is recorded, and **the batch stops**. A later UPDATE can't overtake the CREATE it depends on.
- **The next poll retries.** After 20 failed attempts, a message is parked: it stays in the table for inspection, and resetting `Attempts` to 0 retries it.

## 5. What happens when something fails

| Failure | Result |
|---|---|
| Invalid input | 400. Nothing is written |
| Rule denies | 200 with the reason. Nothing is written and nothing is sent |
| DB error during the save (customer, application or outbox insert) | The transaction rolls back: no customer, no application, no event. 500 to the client, and the form shows a "try again" banner. Integration tests cover this for both the new and the returning path |
| External service down, 5xx or timeout | The user has already been answered. The row stays pending and is retried every 5 s, in order |
| API crashes after commit, before delivery | The row is still pending and is delivered after restart |
| Crash after the external 200, before `ProcessedAt` is saved | The row is resent on the next poll. Harmless, because both calls are idempotent |
| Two first-time submissions with the same SSN at the same moment | The unique index rejects the second, which gets a 500. The data stays consistent, and resubmitting makes it a returning-customer update |

## 6. Trade-offs: what we left out and why

| Choice | Instead of | Why |
|---|---|---|
| Outbox table plus polling | A message broker (RabbitMQ/Kafka) | The outbox is what makes the event part of the DB transaction. A broker would add infrastructure and still need an outbox to be atomic |
| Retrying from the outbox | Polly or another resilience library | The outbox already persists retries, even across restarts. A second retry layer would add nothing |
| Single processor instance | `FOR UPDATE SKIP LOCKED` / leader election | Enough for one API process. Multiple instances would need row locking |
| `AppDbContext` implements `IUnitOfWork`, plus one repository per aggregate | A generic repository or one repository per entity | One aggregate means one repository. A wrapper class would only pass calls through |
| Concrete `LoanDecisionEngine` | `ILoanDecisionEngine` | Tests build it directly with fake rules. The interface would have no second implementation |
| Plain handler classes | MediatR / CQRS pipeline | There is one use case and one event |
| Blacklist in config | A database table | The smallest thing that works. Swapping it is a new `ISsnBlacklist` implementation only |
| Enum `DenialReason` | Strings or a reason class | Typo-proof, appears in the OpenAPI docs, and lets the UI map each reason to a message. The blacklist message is generic on purpose |
| Denied applications not stored | An audit table | Not required by the spec |
| No retry on the concurrent-same-SSN race | Catching the unique violation and retrying | The race is rare and the data stays consistent. The retry code would cost more than it saves |
| SSN stored in plain text | Column encryption or tokenization | Out of scope for the exercise. Production would encrypt it at rest |
| Tests use the compose Postgres, with a database per test class | Testcontainers | One dependency fewer, and real transactions either way. `LOANAPP_TEST_POSTGRES` points the tests at another server |
| Migrations applied at startup (Development only) | A separate migration step | Gives a one-command local setup. Production would run migrations in a deployment step |
| Next.js Server Action calls the API | The browser calling the API directly | No CORS setup, the API URL stays on the server, and the form works without JavaScript |
| Backend tests only | Frontend tests too | The spec asks to cover the rule engine, the returning customer and the endpoint |

Not included, because the spec says they're optional: authentication, CI, structured logging and full containerization. The README's "Known gaps" lists the same limits from a user's point of view.
