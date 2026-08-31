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

Regra de negócio vive no domínio (`Customer`, `Supplier`, agregados financeiros futuros), não em controllers.

## Status / Roadmap

- ✅ **Milestone 0 — Discovery & Architecture**
- 🚧 **Milestone 1 — Foundation**: solution .NET 10, PostgreSQL, Identity (roles Admin/Manager/Sales/Finance), clientes, fornecedores, auditoria append-only, Docker
- ⏳ **Milestone 2 — Sales & Purchasing**
- ⏳ **Milestone 3 — Finance** (idempotência, concorrência, testes adversariais)
- ⏳ **Milestone 4 — Reporting & Dashboard**
- ⏳ **Milestone 5 — Automation & Data Exchange** (background jobs, CSV)
- ⏳ **Milestone 6 — Production Readiness**

## Funcionalidades em progresso (Milestone 1)

- [x] Autenticação JWT (`POST /api/auth/login`, `GET /api/auth/me`)
- [x] Roles seed: `Admin`, `Manager`, `Sales`, `Finance`
- [x] Clientes: CRUD + ativar/desativar, documento único
- [x] Fornecedores: CRUD + ativar/desativar, documento único
- [x] Auditoria append-only (`AuditEntries`, bloqueado a nível de banco contra `UPDATE`/`DELETE`)
- [x] Testes: unitários (domínio) + integração (Testcontainers, API real)
- [x] Docker (API) + docker-compose (API + PostgreSQL)

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

## Licença

Projeto de portfólio pessoal — sem licença de uso comercial definida.
