# ✅ Fluxora ERP — Projeto Completo

ERP full-stack enxuto para pequenas empresas, com foco em gestão comercial e financeira.

## O que é

Fluxora conecta clientes/fornecedores, vendas/compras, contas a pagar/receber, pagamentos/recebimentos, fluxo de caixa e relatórios num único fluxo coerente — uma venda aprovada gera consequência financeira real, não é um conjunto de CRUDs isolados.

```
Clientes / Fornecedores
         ↓
   Vendas / Compras
         ↓
Contas a Receber / Pagar
         ↓
Pagamentos / Recebimentos
         ↓
     Fluxo de Caixa
         ↓
Dashboard / Relatórios
```

## Objetivo

Projeto de portfólio para demonstrar regras de negócio reais, workflows corporativos, consistência financeira, autorização por papel, idempotência, concorrência, background processing, auditoria, relatórios com dados reais, testes adversariais e um frontend que se comporta como um produto B2B de verdade.

## Stack

| Camada | Tecnologia |
|---|---|
| Backend | C# · .NET 10 · ASP.NET Core Web API · Entity Framework Core · PostgreSQL · ASP.NET Core Identity (JWT + roles) · Quartz.NET |
| Frontend | React · TypeScript · Vite · Tailwind (em progresso) |
| Qualidade | xUnit · Testcontainers (PostgreSQL) |
| Infra | Docker · GitHub Actions (CI) |

## Arquitetura

Monólito modular — sem microsserviços.

```
src/
  Fluxora.Api             # controllers, auth, composition root
  Fluxora.Application     # casos de uso (services), DTOs
  Fluxora.Domain          # agregados, invariantes, regras de negócio
  Fluxora.Infrastructure  # EF Core, PostgreSQL, Identity, auditoria

tests/
  Fluxora.UnitTests
  Fluxora.IntegrationTests

web/                       # frontend React (em progresso)
```

Regra de negócio vive no domínio (`Customer`, `Supplier`, `SalesOrder`, `PurchaseOrder`, `Receivable`, `Payable`), não em controllers.

## Status / Roadmap

- ✅ **Milestone 0 — Discovery & Architecture**
- ✅ **Milestone 1 — Foundation**: solution .NET 10, PostgreSQL, Identity (roles Admin/Manager/Sales/Finance), clientes, fornecedores, auditoria append-only, Docker
- ✅ **Milestone 2 — Sales & Purchasing**: catálogo, ciclo de vida de vendas/compras, geração inicial de contas a receber/pagar
- ✅ **Milestone 3 — Finance**: pagamentos/recebimentos com idempotência, concorrência otimista e ledger de caixa, validados por revisão adversarial e testes paralelos reais
- ✅ **Milestone 4 — Reporting & Dashboard**: 10 endpoints de relatório com agregações SQL e dashboard integrado
- ✅ **Milestone 5 — Automation & Data Exchange**: Quartz persistente, processamento de vencidos, snapshots diários e CSV de clientes
- ✅ **Milestone 6 — Production Readiness**: autorização por módulo, hardening de login/CSV, health checks, OpenAPI revisado e CI completa

**Status: projeto congelado / pronto para portfólio.** Todos os milestones concluídos, build backend e frontend limpos, suíte completa (unitários, integração PostgreSQL, frontend) e CI (backend, frontend, deployment-smoke) verdes no HEAD atual. Deploy real em produção depende de infraestrutura externa (domínio, TLS, secrets) não provisionada neste repositório — ver [`docs/deployment.md`](docs/deployment.md).

## Funcionalidades

**Milestone 1 — Foundation**
- [x] Autenticação JWT (`POST /api/auth/login`, `GET /api/auth/me`)
- [x] Roles seed: `Admin`, `Manager`, `Sales`, `Finance`
- [x] Clientes: CRUD + ativar/desativar, documento único
- [x] Fornecedores: CRUD + ativar/desativar, documento único
- [x] Auditoria append-only (`AuditEntries`, bloqueado a nível de banco contra `UPDATE`/`DELETE`)
- [x] Docker (API) + docker-compose (API + PostgreSQL)

**Milestone 2 — Sales & Purchasing**
- [x] Catálogo de produtos (`Products`, SKU único)
- [x] Vendas: `Draft → Approved` ou `Draft → Cancelled`, linhas e cancelamento travados após aprovação
- [x] Compras: `Draft → Confirmed` ou `Draft → Cancelled`, preço de custo explícito por linha
- [x] Aprovar uma venda gera uma `Receivable` (contas a receber) na mesma transação; confirmar uma compra gera uma `Payable`
- [x] Parcelamento com distribuição exata em centavos (última parcela absorve o resto de arredondamento)
- [x] Endpoints de leitura para contas a receber/pagar geradas (`GET /api/receivables`, `GET /api/payables`)

**Milestone 3 — Finance**
- [x] `POST /api/payables/{id}/installments/{id}/payments` e `POST /api/receivables/{id}/installments/{id}/receipts`
- [x] Idempotência real: header `Idempotency-Key` obrigatório, replay exato para a mesma chave+payload, `409` para chave reutilizada com payload diferente
- [x] Concorrência real: campo `Version` como concurrency token do EF Core — comparação explícita na aplicação + `DbUpdateConcurrencyException` do EF traduzido para `409`, provado com um teste de duas requisições HTTP genuinamente paralelas (`Task.WhenAll`) contra a mesma parcela
- [x] Pagamento/recebimento parcial e integral, rejeição de valor acima do saldo restante, parcela `Paid` não aceita novo lançamento
- [x] `CashMovement` (fluxo de caixa) gerado na mesma transação de cada pagamento/recebimento
- [x] Auditoria (`PaymentApplied`, `ReceiptApplied`)

**Milestone 4 — Reporting & Dashboard (backend)**
- [x] `GET /api/reports/revenue`, `/expenses`, `/net-result` — agregados mensais calculados via `SUM`/`GROUP BY` no PostgreSQL, sem carregar entidades inteiras
- [x] `GET /api/reports/overdue` e `/upcoming-due` — dados do "Farol de Vencimentos" (vencidos / vencem hoje / a vencer)
- [x] `GET /api/reports/cash-flow` (realizado, com saldo acumulado) e `/cash-flow-projected` (parcelas pendentes futuras)
- [x] `GET /api/reports/top-customers` e `/expenses-by-category`
- [x] `GET /api/reports/dashboard-summary` — KPIs consolidados numa única chamada
- [x] Todos os relatórios aceitam filtro por período (`from`/`to`)
- [x] Dashboard no frontend integrado aos endpoints reais de relatórios

**Milestone 5 — Automation & Data Exchange**
- [x] Quartz.NET 3.20 com AdoJobStore persistente no PostgreSQL, clustering e IDs estáveis de jobs/triggers
- [x] Job diário de vencimentos: `Pending → Overdue`, idempotente, com concorrência via `Version`, logging e auditoria de ator `System`
- [x] Preparação diária de snapshot do dashboard, única por data de negócio e recuperável após restart
- [x] Calendário de negócio configurável (padrão `America/Sao_Paulo`) e cron schedules configuráveis por ambiente
- [x] `POST /api/customers/import`: CSV UTF-8 com resultado detalhado (`total`, `imported`, `rejected`, `line`, `reason`), falha parcial, validações e auditoria correlacionada
- [x] `GET /api/customers/export`: exportação filtrável de dados reais com escaping CSV e autorização `Admin`/`Manager`
- [x] Policies explícitas para Financeiro, Relatórios, Automação e Data Exchange

**Milestone 6 — Production Readiness**
- [x] Policies explícitas por módulo: Vendas (`Admin`/`Manager`/`Sales`), Compras (`Admin`/`Manager`) e Financeiro (`Admin`/`Manager`/`Finance`), com testes positivos e negativos
- [x] Proteção de login com lockout após 5 falhas por 15 minutos, rate limit por endereço de origem e limite de alvos distintos por origem (mitiga lockout DoS direcionado a múltiplas contas)
- [x] Exportação CSV neutraliza fórmulas de planilha em campos não confiáveis
- [x] Importação de clientes serializa documentos concorrentes via lock transacional (import × import e import × criação avulsa não corrompem o resultado parcial)
- [x] Health checks separados: liveness em `/health/live` e readiness do PostgreSQL em `/health/ready` (`/health` preservado como alias de readiness)
- [x] OpenAPI com metadados, esquema Bearer JWT e requisitos de segurança por operação autenticada
- [x] GitHub Actions executa restore/build, testes unitários e integração PostgreSQL, lint/test/build do frontend, e um job de deployment-smoke que sobe o Compose de produção e valida liveness/readiness/login/rota autenticada real

## Como executar

### Pré-requisitos
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker (para PostgreSQL local, ou para rodar tudo via compose)

### Desenvolvimento local

```bash
cp .env.example .env   # preencha com valores locais - nunca commitar .env
docker compose up -d postgres

export ConnectionStrings__Default="Host=localhost;Database=fluxora;Username=fluxora;Password=<sua senha do .env>"
export Jwt__Key="<sua chave de pelo menos 32 caracteres>"
export Jwt__Issuer="Fluxora"
export Jwt__Audience="Fluxora"
export Database__ApplyMigrations="true"
export Business__TimeZone="America/Sao_Paulo"

dotnet run --project src/Fluxora.Api
```

A API sobe em `http://localhost:5xxx` (ver `src/Fluxora.Api/Properties/launchSettings.json`) com OpenAPI em `/openapi/v1.json` no ambiente de desenvolvimento, liveness em `/health/live` e readiness em `/health/ready`.

### Tudo via Docker

```bash
cp .env.example .env   # POSTGRES_PASSWORD e JWT_KEY são obrigatórios
docker compose up --build
```

API disponível em `http://localhost:8080`. O procedimento de host único, requisitos de TLS/segredos, migração, rollback e smoke test está em [`docs/deployment.md`](docs/deployment.md).

### Testes

```bash
dotnet test tests/Fluxora.UnitTests           # não precisa de Docker
dotnet test tests/Fluxora.IntegrationTests    # precisa de Docker (Testcontainers)
```

Atualmente são **70 testes unitários** e **65 testes de integração**. A suíte cobre autenticação/autorização, lockout e rate limiting, IDOR entre agregados, CRUD, geração transacional de títulos, parcelamento exato, idempotência sequencial e realmente concorrente, conflitos de versão, ledger de caixa, snapshots consistentes de relatórios, datas de negócio, jobs repetidos, persistência do Quartz, importação CSV parcial/totalmente inválida/válida, exportação segura, health checks, OpenAPI e auditoria. Os testes de integração executam a API real contra PostgreSQL via Testcontainers.

## Licença

Projeto de portfólio pessoal — sem licença de uso comercial definida.
