# 🚧 Fluxora ERP — Em desenvolvimento

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
| Backend | C# · .NET 10 · ASP.NET Core Web API · Entity Framework Core · PostgreSQL · ASP.NET Core Identity (JWT + roles) |
| Frontend | React · TypeScript · Vite · Tailwind (em progresso) |
| Qualidade | xUnit · Testcontainers (PostgreSQL) |
| Infra | Docker · GitHub Actions (CI, em progresso) |

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
- ⚠️ **Milestone 3 — Finance**: implementação concluída, revisão adversarial independente pendente antes de considerar definitivamente encerrado (pagamentos/recebimentos com idempotência, concorrência, fluxo de caixa)
- 🚧 **Milestone 4 — Reporting & Dashboard**
- ⏳ **Milestone 5 — Automation & Data Exchange** (background jobs, CSV)
- ⏳ **Milestone 6 — Production Readiness**

## Funcionalidades em progresso

**Milestone 1 — Foundation**
- [x] Autenticação JWT (`POST /api/auth/login`, `GET /api/auth/me`)
- [x] Roles seed: `Admin`, `Manager`, `Sales`, `Finance`
- [x] Clientes: CRUD + ativar/desativar, documento único
- [x] Fornecedores: CRUD + ativar/desativar, documento único
- [x] Auditoria append-only (`AuditEntries`, bloqueado a nível de banco contra `UPDATE`/`DELETE`)
- [x] Docker (API) + docker-compose (API + PostgreSQL)

**Milestone 2 — Sales & Purchasing**
- [x] Catálogo de produtos (`Products`, SKU único)
- [x] Vendas: `Draft → Approved → Cancelled`, linhas travadas após aprovação
- [x] Compras: `Draft → Confirmed → Cancelled`, preço de custo explícito por linha
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
- [ ] Relatórios de fluxo de caixa/DRE — Milestone 4

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

dotnet run --project src/Fluxora.Api
```

A API sobe em `http://localhost:5xxx` (ver `src/Fluxora.Api/Properties/launchSettings.json`) com Swagger/OpenAPI em `/openapi/v1.json` no ambiente de desenvolvimento, e health check em `/health`.

### Tudo via Docker

```bash
cp .env.example .env   # preencha com valores locais
docker compose up --build
```

API disponível em `http://localhost:8080`.

### Testes

```bash
dotnet test tests/Fluxora.UnitTests           # não precisa de Docker
dotnet test tests/Fluxora.IntegrationTests    # precisa de Docker (Testcontainers)
```

51 testes unitários (domínio: Customer/Supplier/SalesOrder/PurchaseOrder/Receivable/Payable, parcelamento, regras de pagamento/recebimento) e testes de integração cobrindo autenticação, CRUD, o fluxo "venda aprovada gera conta a receber com parcelas corretas", idempotência (replay exato, conflito de chave reutilizada) e concorrência real (duas requisições HTTP paralelas contra a mesma parcela — só uma pode ganhar) contra a API real via Testcontainers.

## Licença

Projeto de portfólio pessoal — sem licença de uso comercial definida.
