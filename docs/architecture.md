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
- **Idempotency**: `Idempotency-Key` header required on `POST .../payments` and `.../receipts`.
  `IdempotencyRecords` table (`Operation, Key, RequestHash, ResponseStatus, ResponseBody`),
  unique index on `(Operation, Key)`. Check-then-act: look up by key first — same hash replays
  the stored response, different hash is a `409`. The request hash covers the logical command
  (installment id, amount, expected version), never the transport-level header itself. Financial
  idempotency records are never expired/deleted. *Known gap:* two requests with the identical,
  brand-new key arriving at the exact same instant both pass the initial lookup before either
  commits; the unique index still prevents a duplicate row, but the loser gets a raw `DbUpdateException`
  instead of a graceful replay. This doesn't cause a double payment (see Concurrency below,
  which is what actually closes that race) — it's a UX polish item for a retry, not a
  correctness gap.
- **Concurrency**: explicit `Version` int column as an EF Core concurrency token on
  `PayableInstallment`/`ReceivableInstallment` (same pattern as the sibling HelpDesk project's
  `Ticket.Version`). The application layer compares the caller's `ExpectedVersion` before
  mutating (fast, clear error); EF Core's own optimistic-concurrency check on `SaveChanges` is
  the real race-closer for two simultaneous requests that both pass that early check — one
  `UPDATE ... WHERE Version = @expected` wins, the other affects zero rows and raises
  `DbUpdateConcurrencyException`, translated centrally in `AppDbContext` to
  `ConcurrencyConflictException` → `409`. Proven under a genuine parallel-request test
  (`PaymentApplicationTests.ApplyPayment_TwoTrulyConcurrentRequests_OnlyOneSucceeds`), not just
  asserted.
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

## Reporting (Milestone 4)

All reports are aggregation queries (`SUM`/`COUNT`/`GROUP BY`) executed in PostgreSQL via EF
Core, never computed by loading full entity graphs into memory. This required persisting
`SalesOrder.Total`/`PurchaseOrder.Total` (previously recomputed from `Lines` on every read -
correct for a single order, unusable for a `SUM` across thousands of them). `ReportingRepository`
is the only place report SQL lives; `ReportingService` just orchestrates and applies defaults
(current business month through today when both bounds are omitted; one-sided ranges remain
open-ended) and rejects inverted ranges - no business logic leaks into the API controller.
Coupled multi-query reports run under PostgreSQL `REPEATABLE READ`, while current balance uses
one conditional aggregate statement, so each response represents one coherent database snapshot.
Purchase lines snapshot the product category used by historical expense reports. The introducing
migration backfills existing lines from each product's category at migration time; category
changes made before that migration cannot be reconstructed, while later edits cannot move history.

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
