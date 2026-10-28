# Loan Application — Fundo Full-Stack Take-Home

> 🎥 **Demo video:** _TODO — public link goes here_

A small loan application flow: a Next.js form, a .NET backend with a rule engine that approves or denies, transactional persistence, and a background event that syncs the data to a mock external service.

> **Status:** work in progress. This README describes the planned design; run commands and test data will be filled in as each part lands.

## Stack

| Part | Technology |
|---|---|
| Backend | .NET 10 (C#), ASP.NET Core, EF Core |
| Database | PostgreSQL 17 (Docker Compose) |
| Frontend | Next.js |
| External service | Small Node app (mock) |

## Repository layout

```
backend/
  src/
    Domain/          # Customer, Application entities; rule contracts
    Application/     # Use case (submit application), rule engine, ports (interfaces)
    Infrastructure/  # EF Core DbContext, outbox, HTTP client to external service
    Api/             # Thin controller, DI wiring, background worker host
  tests/             # Rule engine, returning-customer path, endpoint
frontend/            # Next.js form, approved page, denied page
mock-external-service/  # Receives the payload, logs it, returns 200
```

## Planned design

### Rule engine
Each deny rule is its own class implementing `IDenyRule`. The engine runs every registered rule; if any matches, the application is denied with that rule's reason. Adding a rule means adding a class and registering it — existing rules are never touched.

Initial rules:
- State is `NY` → deny.
- SSN is on the blacklist → deny.

### Persistence and transaction (transactional outbox)
On approval, inside **one database transaction**:
1. Insert or update the **Customer** (looked up by SSN).
2. Insert or update the **Application** (`id`, `requestedAmount`, `customerId`).
3. Insert an **outbox row** (the event).

If any step fails, everything rolls back — no half-saved customer, no orphan application, no event. Writing the event to the same database is what makes "publishing the event" part of the unit of work.

### Returning customer
Same SSN means one customer and one application: the existing records are updated with the latest submission, and the event updates the external service instead of creating a new record.

### Background event → external service
A hosted background worker reads pending outbox rows and calls the external service, outside the HTTP request that answers the form. It marks a row as processed on a 200 and retries on failure.

**Contract:** `PUT /customers/{ssn}` with the customer and application payload. `PUT` is an idempotent upsert, so one endpoint covers both create (new customer) and update (returning customer), and a retry never creates a duplicate.

## Running locally

Prerequisites: .NET 10 SDK, Node.js 20+, Docker.

```bash
# 1. Database (Postgres on localhost:5433)
docker compose up -d

# 2. Backend (applies migrations on startup in Development)
cd backend
dotnet tool restore
dotnet run --project src/LoanApp.Api
# health check: curl http://localhost:5009/health

# 3. Frontend
cd ../frontend
npm install
npm run dev   # http://localhost:3000
```

_TODO — mock external service._

## Running the tests

_TODO_

## Test data

_TODO — blacklisted SSNs, approved example, NY denial, returning-customer steps._

## Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) (_TODO_) for layers, the rule engine, the event flow, failure handling, and trade-offs.
