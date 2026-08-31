# Fluxora ERP — Architecture Baseline

Consolidated from Milestone 0 (Discovery & Architecture). Full research/decision log lives in
the project's Notion documentation; this is the durable summary that ships with the code.

## Domain flow

```
Clientes / Fornecedores → Vendas / Compras → Contas a Receber / Pagar
  → Pagamentos / Recebimentos → Fluxo de Caixa → Dashboard / Relatórios
```

A sale approval must generate a receivable; a purchase confirmation must generate a payable.
Payments must never double-apply, even under retry or concurrent requests.

## Architecture

Modular monolith — no microservices. `src/Fluxora.{Api,Application,Domain,Infrastructure}`,
`tests/Fluxora.{UnitTests,IntegrationTests}`, `web/` reserved for the React frontend.

Business rules live in the domain (aggregates), never in controllers/services.

## Financial mutation mechanisms

- **Money**: always `decimal`, never `double`. PostgreSQL `numeric(19,2)` for totals/installments,
  `numeric(19,4)` for sub-cent unit prices. Rounding centralized, `MidpointRounding.AwayFromZero`.
  Installment splitting: base cents per installment, last installment absorbs the remainder.
- **Idempotency**: `Idempotency-Key` header required on financial mutations. Dedicated table
  (`CompanyId, Operation, Key, RequestHash, ResponseStatus, ResponseBody`), unique constraint,
  `INSERT ... ON CONFLICT DO NOTHING` flow. Financial idempotency keys never expire.
- **Concurrency**: explicit `Version` int column as an EF Core concurrency token on mutable
  financial aggregates (same pattern as the sibling HelpDesk project's `Ticket.Version`).
  Mismatch surfaces as `409 Conflict` via `DbUpdateConcurrencyException`.
- **Background processing**: Quartz.NET in-process with a persistent PostgreSQL `AdoJobStore`
  — no Redis, no separate worker service. Used for overdue-installment sweeps, recurrence
  generation, and scheduled report prep.
- **Authorization**: ASP.NET Core Identity with four roles (`Admin`, `Manager`, `Sales`,
  `Finance`) seeded on startup. Roles are the coarse layer; fine-grained permissions/policies
  and resource-based (ownership) handlers land alongside the modules that need them.
- **Audit**: dedicated append-only `AuditEntries` table, written by explicit domain/application
  events in the same transaction as the business mutation — never via a generic `SaveChanges`
  interceptor. `UPDATE`/`DELETE` are blocked at the database level (PostgreSQL rules); a
  correction is always a new compensating entry.

## Frontend architecture (Milestone 2+)

Feature-module structure (`modules/vendas`, `modules/financeiro`, ...) over a `shared/` layer
(design system, API client, auth, formatters). TanStack Query for all server state, React
Router v7 for routing + role-based gating, a single wrapped HTTP client normalizing errors
(400 → field errors, 409 → conflict/reload prompt, idempotency replay → silent success).

Visual identity: "Slate & Emerald Precision" — dense financial tables, semantic status badges,
a "Farol de Vencimentos" KPI header over Fluxora's own contas a pagar/receber, Cmd+K command
bar. Explicitly out of scope even as inspiration: real bank reconciliation (OFX), NF-e/NFS-e,
digital certificates, external CNPJ/CPF autofill, multi-CNPJ switching.

## Explicitly out of scope

NF-e/fiscal integration, real banking/Pix/card integration, payroll/HR, full CRM, accounting,
marketplace, complex multi-tenant, chatbots/generative AI, Kafka, Kubernetes, microservices.
