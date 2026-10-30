# Loan Application — Fundo Full-Stack Take-Home

> 🎥 **Demo video:** https://jam.dev/c/e717bc7a-fb10-499f-9dd9-9d4ea744c021 (approved, NY denial, blacklisted SSN, returning customer, external service receiving the data)

A small loan application flow. A Next.js form sends the application to a .NET API. A rule engine approves or denies it. Approved applications are saved together with an event in one transaction, and a background worker sends that event to a mock external service over HTTP.

**Design, trade-offs and failure handling:** see [ARCHITECTURE.md](ARCHITECTURE.md).

| Part | Technology | Port |
|---|---|---|
| Backend | .NET 10, ASP.NET Core, EF Core | 5009 |
| Database | PostgreSQL 17 (Docker Compose) | 5433 |
| Frontend | Next.js 16, React 19, Tailwind 4 | 3000 |
| External service (mock) | Node.js, no dependencies | 4000 |

## Running locally

**Prerequisites:** .NET 10 SDK, Node.js 20+, Docker.

Run each command in its own terminal, from the repository root:

```bash
# 1. Database
docker compose up -d

# 2. Backend API (applies EF Core migrations on startup in Development)
cd backend && dotnet run --project src/LoanApp.Api

# 3. Mock external service
cd mock-external-service && npm start

# 4. Frontend
cd frontend && npm install && npm run dev
```

Then open **http://localhost:3000**.

| URL | What it is |
|---|---|
| http://localhost:3000 | Application form |
| http://localhost:5009/scalar/v1 | API reference and try-it UI (OpenAPI) |
| http://localhost:5009/health | Health check, including the database |
| http://localhost:4000/customers | Everything the external service has received |

The frontend calls the API at `http://localhost:5009` by default. To change that, copy `frontend/.env.example` to `frontend/.env.local` and set `API_URL`.

## Test data

| Scenario | What to enter | Result |
|---|---|---|
| **Approved** | Any state except New York, SSN `123-45-6789` | Approved page. The external service logs `+ CREATE` |
| **Denied: New York** | State **New York**, any SSN | Denied page: "We can't offer loans in New York yet" |
| **Denied: blacklisted SSN** | SSN `111-11-1111`, `222-22-2222` or `333-33-3333`, any state | Denied page with a generic message (the reason is deliberately not shown) |
| **Returning customer** | Submit the approved example again with the same SSN (with or without dashes) and a different name, state or amount | "Your application was updated", with the same reference. The external service logs `~ UPDATE` for the same customer id |
| **External service outage** | `curl -X POST localhost:4000/simulate-outage/3`, then submit (`/simulate-outage/0` cancels) | The user is still answered immediately. The worker retries every 5 s and delivers once the outage ends |
| **Validation** | Leave fields empty, use a short SSN, or enter an amount of 0 | Inline errors on each field, and the typed values are kept |

The blacklist lives in `backend/src/LoanApp.Api/appsettings.json` (`Blacklist:Ssns`).

**Inspect the database:**

```bash
docker compose exec postgres psql -U loanapp -d loanapp -c '
  SELECT c."FirstName", c."Address_State" AS state, a."RequestedAmount", a."Id" AS application_id
  FROM "Customers" c JOIN "Applications" a ON a."CustomerId" = c."Id";'

docker compose exec postgres psql -U loanapp -d loanapp -c '
  SELECT "OccurredAt", "ProcessedAt", "Attempts", "LastError" FROM "OutboxMessages" ORDER BY "OccurredAt";'
```

## Running the tests

```bash
docker compose up -d   # integration tests use the compose Postgres
cd backend && dotnet test
```

There are 28 tests:
- **Unit tests:** the rule engine, the customer aggregate (one application per customer), event dispatch (create vs update), and the HTTP contract with the external service.
- **Integration tests:** the real HTTP pipeline with real Postgres. They cover approved, denied and invalid requests, the **returning-customer** path, **transaction rollback** (for both new and returning customers), and outbox delivery, ordering and retries.

Each integration test class creates its own `loanapp_test_<guid>` database and drops it afterwards, so your dev data is never touched. To use another Postgres server, set `LOANAPP_TEST_POSTGRES`, for example `Host=...;Port=...;Username=...;Password=...`.

## API

**`POST /api/applications`**

```json
{
  "firstName": "Jane", "lastName": "Doe", "companyName": "Acme LLC",
  "address": { "street": "1 Main St", "city": "Newark", "state": "NJ", "zipCode": "07101" },
  "ssn": "123-45-6789", "requestedAmount": 25000
}
```

| Response | Body |
|---|---|
| `200` approved | `{ "approved": true, "applicationId": "…", "isReturningCustomer": false }` |
| `200` denied | `{ "approved": false, "denialReason": "StateNotSupported" \| "SsnBlacklisted" }` |
| `400` | `ValidationProblemDetails` with errors per field |

A denial is a business outcome, not an error, so it returns 200.

**External service contract** (called by the background worker):

| Customer | Request |
|---|---|
| New | `POST /customers` with body `{ customer: { id, ssn, firstName, lastName, companyName, address }, application: { id, requestedAmount } }` |
| Returning | `PUT /customers/{customerId}` with the same body |

Any response other than 2xx means a retry. The `POST` is idempotent on `customer.id`, so a resent create never duplicates the record.

## Repository layout

```
backend/
  src/LoanApp.Domain/          Customer aggregate (owns its single LoanApplication), Ssn and Address value objects
  src/LoanApp.Application/     Rule engine, submit use case, event + handler, ports (interfaces)
  src/LoanApp.Infrastructure/  EF Core/Postgres, outbox + background processor, HTTP client, blacklist from config
  src/LoanApp.Api/             Thin controller, request validation, error handling, wiring
  tests/LoanApp.Tests/         Unit + integration tests
frontend/                      Next.js form (Server Action), /approved and /denied pages
mock-external-service/         POST/PUT/GET /customers, POST /simulate-outage/:n
docker-compose.yml             PostgreSQL
```

## Known gaps

- **Single API instance assumed.** Running several would need row locking on the outbox (`FOR UPDATE SKIP LOCKED`).
- **Concurrent first submissions with the same SSN.** If two arrive at the same moment, the unique index rejects the second one (it fails, but the data stays consistent). Resubmitting makes that person a returning customer.
- **Denied applications are not stored.** The spec only asks for approved ones.
- **No authentication, HTTPS or secrets management.** Not required for the exercise. In production, the call to the external service carries an SSN, so it would need HTTPS and authentication.
- **The mock keeps its data in memory,** so it resets on restart.
- **No frontend tests.** Test effort went to the backend, which is what the spec asks to cover.
