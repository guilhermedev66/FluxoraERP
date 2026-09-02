# ✅ Fluxora ERP — Projeto Completo

ERP full-stack enxuto para pequenas empresas, com foco em gestão comercial e financeira.

**Produção:** [fluxora-erp.vercel.app](https://fluxora-erp.vercel.app) (frontend, Vercel) · [fluxora-erp-api.onrender.com](https://fluxora-erp-api.onrender.com) (API, Render — health checks públicos em [`/health/live`](https://fluxora-erp-api.onrender.com/health/live) e [`/health/ready`](https://fluxora-erp-api.onrender.com/health/ready)) · PostgreSQL gerenciado no Neon.
Planos gratuitos: a API hiberna após ociosidade e o primeiro request após um período pode levar alguns segundos a mais (cold start) — ver [Limitações](#limitações--trade-offs-reais).

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
| Frontend | React · TypeScript · Vite · Tailwind CSS 4 |
| Qualidade | xUnit · Testcontainers (PostgreSQL) |
| Infra | Docker · GitHub Actions (CI) · Vercel (frontend) · Render (API) · Neon (PostgreSQL gerenciado) |

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

web/                       # frontend React
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

**Status: projeto congelado / pronto para portfólio.** Todos os milestones concluídos, build backend e frontend limpos, suíte completa (unitários, integração PostgreSQL, frontend) e CI (backend, frontend, deployment-smoke) verdes no HEAD atual. Deploy real em produção (Vercel + Render + Neon) publicado e validado ponta a ponta — ver [Produção](#produção) acima e [`docs/deployment.md`](docs/deployment.md) para o passo a passo (single-host Docker e cloud).

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

**Polish visual final (M6 Frontend)**
- [x] Tema com três opções (Light/Dark/System) — não só um toggle binário: "System" acompanha `prefers-color-scheme` ao vivo, sem flash incorreto no primeiro paint, persistido, controle acessível (`radiogroup`/`aria-checked`/tooltip nativo)
- [x] Dashboard ganhou um gráfico real (receita x despesa, 6 meses) além dos KPIs — cores vêm dos tokens `--chart-series-1/2` já reservados para isso, então acompanham Light/Dark automaticamente
- [x] Favicon próprio (ver seção de Produção)

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

Atualmente são **70 testes unitários** e **74 testes de integração**. A suíte cobre autenticação/autorização, lockout e rate limiting (incluindo disputa genuinamente concorrente pelo limite), IDOR entre agregados, CRUD, geração transacional de títulos, parcelamento exato, idempotência sequencial e realmente concorrente, conflitos de versão, ledger de caixa, snapshots consistentes de relatórios, datas de negócio, jobs repetidos (incluindo o job de vencidos correndo contra um pagamento real no mesmo registro), persistência do Quartz, importação CSV parcial/totalmente inválida/válida, exportação segura, cabeçalhos de segurança sobrevivendo ao exception handler, health checks, OpenAPI e auditoria. Os testes de integração executam a API real contra PostgreSQL via Testcontainers — nenhum mock de banco.

## Segurança

- **AuthN/AuthZ**: JWT (issuer/audience/lifetime/assinatura validados, 30s de clock skew), roles fixas (`Admin`/`Manager`/`Sales`/`Finance`), policies por módulo aplicadas em todos os controllers.
- **Login**: lockout do ASP.NET Core Identity (5 falhas / 15 min por conta) **e** um guard adicional que limita quantos alvos distintos uma mesma origem pode tentar por janela — sem isso, um atacante com um orçamento generoso de requisições por IP consegue espalhar tentativas erradas o suficiente para acionar o lockout de várias contas reais (DoS de lockout). A checagem e a reserva do alvo são atômicas sob o mesmo lock, para resistir a rajadas concorrentes contra vários alvos novos ao mesmo tempo.
- **IDOR**: IDs financeiros aninhados (parcela dentro de `Payable`/`Receivable`) são resolvidos sempre a partir do agregado pai — um par (id do título, id da parcela) incompatível retorna 404 em vez de operar sobre uma parcela não relacionada.
- **Cabeçalhos de resposta**: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `Content-Security-Policy` restritiva e header `Server` do Kestrel suprimido — aplicados via `Response.OnStarting`, portanto sobrevivem inclusive a uma resposta gerada pelo exception handler.
- **Auditoria**: `AuditEntries` é apenas-inserção — bloqueado a nível de banco (regra PostgreSQL) contra `UPDATE`/`DELETE`, e cada entrada é gravada na mesma transação da mutação que audita.
- **CORS**: origem exata da aplicação frontend (sem wildcard) via `Cors:AllowedOrigins`.

## Idempotência e concorrência

- **Idempotência**: aplicação de pagamento/recebimento exige header `Idempotency-Key`. A checagem-e-reserva da chave usa um advisory lock do PostgreSQL escopado à transação (`pg_advisory_xact_lock`), fechando a janela entre "verificar se já existe" e "gravar" mesmo sob duas requisições verdadeiramente simultâneas — não apenas sequenciais.
- **Concorrência otimista**: agregados mutáveis (`SalesOrder`, `PurchaseOrder`, parcelas) carregam um `Version` como concurrency token do EF Core. Toda mutação incrementa a versão; um conflito vira `409` de forma determinística, provado com requisições HTTP genuinamente paralelas (`Task.WhenAll`) contra o mesmo registro, não com mocks.
- **Jobs em lote**: o job diário de vencidos processa cada parcela em sua própria transação (`ExecuteUpdateAsync` condicional) — uma parcela paga concorrentemente é apenas pulada (`affected = 0`), sem reverter as demais parcelas legitimamente marcadas no mesmo lote.

## Decisões técnicas relevantes

- **Regra de negócio no domínio, não em controllers/services**: agregados (`SalesOrder`, `Receivable`, `ReceivableInstallment`, ...) expõem métodos que protegem seus próprios invariantes (`ApplyReceipt`, `MarkOverdue`, ...); a camada de aplicação orquestra, não decide.
- **Categoria da linha de compra é um snapshot imutável**, não uma referência viva ao produto — um relatório de despesas por categoria não pode mudar retroativamente porque alguém recategorizou um produto meses depois.
- **Relatórios que cruzam agregados usam uma única transação `RepeatableRead`**, para não somar receita lida num instante com parcelas lidas alguns milissegundos depois já em outro estado.
- **Sem microsserviços**: monólito modular com fronteiras de projeto (`Domain`/`Application`/`Infrastructure`/`Api`) — a complexidade de um sistema distribuído não se paga no tamanho e na fase deste projeto.

## Limitações / trade-offs reais

- **Cold start nos planos gratuitos**: a API (Render) hiberna após ociosidade e o banco (Neon) suspende o compute; o primeiro request após um tempo parado pode demorar alguns segundos a mais até "esquentar".
- **`ReverseProxy:KnownProxy` assume um único IP de proxy fixo e conhecido.** Isso funciona bem atrás de um reverse proxy próprio (nginx, etc.), mas a borda do Render não expõe um IP interno único e estável para configurar ali — na implantação atual essa opção fica em branco, então o rate limit e o guard de login por IP enxergam o IP interno do proxy do Render em vez do IP público real do cliente, o que os torna efetivamente globais (por toda a origem que passa pela borda) em vez de por-cliente nessa implantação específica. Atrás de um proxy próprio com IP fixo (ex.: deploy via Docker Compose com nginx na frente), a configuração funciona como projetada.
- **Sem self-service de redefinição de senha** — troca de senha é responsabilidade administrativa hoje.
- **Bundle do frontend acima de 500 kB minificado** (aviso do Vite) — nenhum code-splitting por rota foi feito ainda; não afeta corretude, só o tamanho do download inicial.
- **Sem multi-tenant** — é um único banco/organização por implantação, por desenho.

## Screenshots

_Espaço reservado — capturas de tela do dashboard, listagem de vendas, formulário de venda com parcelamento, tela de contas a receber e o seletor de tema (light/dark) devem entrar aqui antes de publicar em portfólio/LinkedIn._

## Licença

Projeto de portfólio pessoal — sem licença de uso comercial definida.
