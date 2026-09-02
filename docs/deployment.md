# Deploy

Dois caminhos suportados: implantação em nuvem (Vercel + Render + Neon — é o que roda em produção hoje) e host único com Docker Compose (self-hosted, com domínio/TLS por sua conta).

## Nuvem (produção atual): Vercel + Render + Neon

- **Frontend (Vercel)**: projeto Vite detectado automaticamente (`npm run build`, saída em `web/dist`). Variável de build `VITE_API_BASE_URL` aponta para a URL pública da API (`https://.../api`) — ela é embutida no bundle em build time, então uma mudança de URL da API exige um novo build/deploy do frontend.
- **API (Render, Web Service com runtime Docker)**: usa o `Dockerfile` existente em `src/Fluxora.Api/Dockerfile` com o **contexto de build na raiz do repositório** (o Dockerfile copia `src/Fluxora.Domain`, `src/Fluxora.Application`, etc. a partir da raiz). Health check path: `/health/ready`. Variáveis de ambiente equivalentes às do Compose (ver `.env.example`), com `ConnectionStrings__Default` apontando para o Neon e `Cors__AllowedOrigins__0` apontando para a URL exata do Vercel.
- **Banco (Neon)**: um projeto/branch com um database dedicado; a connection string pooled do Neon (`postgresql://user:pass@host/db?sslmode=require&channel_binding=require`) precisa ser convertida para o formato ADO.NET do Npgsql: `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require;Trust Server Certificate=true;Channel Binding=Require`.
- **Migrações**: `Database__ApplyMigrations=true` no primeiro deploy aplica o schema automaticamente na subida (mesma lógica do Compose); com uma única instância de API isso é seguro.
- **Bootstrap do Admin**: `Bootstrap__AdminEmail`/`Bootstrap__AdminPassword` na primeira subida criam o usuário Admin inicial (a seed é idempotente — não recria nem falha em deploys seguintes). Considere girar essa senha após a primeira confirmação de login.
- **Planos gratuitos**: a instância da API hiberna após ociosidade (cold start no primeiro request) e o compute do Neon também suspende — aceitável para portfólio, não para uma carga de produção real com SLA.

## Host único (self-hosted): Docker Compose

Este é o caminho self-hosted para uma implantação simples da API e do PostgreSQL com Docker Compose. O frontend continua sendo um artefato separado (`npm run build`) e deve ser servido por CDN ou servidor web.

### Limites deliberados

- O Compose publica API e PostgreSQL apenas em `127.0.0.1` por padrão. Um reverse proxy externo deve fornecer domínio e TLS.
- Segredos continuam externos ao repositório. A implantação real exige `POSTGRES_PASSWORD`, `JWT_KEY` e, na primeira inicialização, credenciais temporárias de bootstrap do Admin.
- Backups, monitoramento externo, registro de imagens e certificados pertencem ao ambiente de hospedagem; não há credenciais ou contas para configurá-los neste repositório.
- `Database__ApplyMigrations=true` é apropriado somente para uma instância de API. Em múltiplas réplicas, execute a migração com uma única instância e inicie as demais com a opção desabilitada.

### Preparação

1. Copie `.env.example` para `.env`.
2. Gere senhas fortes e uma chave JWT exclusiva, por exemplo com `openssl rand -base64 48`.
3. Defina `ALLOWED_HOSTS` com o hostname público e `CORS_ALLOWED_ORIGIN` com a origem exata do frontend.
4. Se houver reverse proxy, defina `REVERSE_PROXY_KNOWN_PROXY` com o IP de origem que o Kestrel realmente enxerga para esse proxy. Somente esse endereço poderá fornecer `X-Forwarded-For`/`X-Forwarded-Proto`; isso preserva o rate limit por cliente sem confiar em headers forjados.
5. Mantenha `POSTGRES_BIND_ADDRESS=127.0.0.1`. Não publique o banco na rede sem um requisito e uma camada de rede dedicada.
6. Para uma release reproduzível, defina `API_IMAGE` e `POSTGRES_IMAGE` com tags imutáveis do ambiente.
7. Planeje backup automático do volume `fluxora_postgres_data` e teste uma restauração antes de aceitar dados reais.

Nunca compartilhe a saída de `docker compose config`: ela contém os segredos interpolados.

### Subida e migração inicial

```bash
docker compose build --pull api
docker compose up -d
docker compose ps
```

Na primeira subida, preencha `BOOTSTRAP_ADMIN_EMAIL` e `BOOTSTRAP_ADMIN_PASSWORD`. Depois que o login for confirmado, remova ambos do `.env` e recrie o container da API:

```bash
docker compose up -d --force-recreate api
```

### Smoke test

Somente endpoints públicos:

```bash
FLUXORA_BASE_URL=http://localhost:8080 ./scripts/smoke-test.sh
```

Fluxo autenticado completo (requer `curl`, `jq` e uma conta com acesso a Relatórios):

```bash
FLUXORA_BASE_URL=https://erp.example.com \
FLUXORA_SMOKE_EMAIL=smoke@example.com \
FLUXORA_SMOKE_PASSWORD='<senha>' \
./scripts/smoke-test.sh
```

O script valida liveness, readiness real do PostgreSQL, login, identidade autenticada e o resumo do dashboard. Ele não imprime senha nem token.

### Operação e rollback

- `/health/live` confirma que o processo HTTP responde; `/health/ready` inclui PostgreSQL e o scheduler Quartz (jobs de vencidos e snapshot travados não passam), e também alimenta o `HEALTHCHECK` da imagem. Ambos retornam JSON com o status de cada check individual.
- Os containers reiniciam com `unless-stopped`, aguardam até um minuto para encerramento limpo e rotacionam logs locais em três arquivos de 10 MB.
- Para investigar uma falha de inicialização, use `docker compose logs --tail=200 api postgres` sem publicar o conteúdo caso possa conter dados operacionais.
- Para rollback com imagens publicadas, restaure `API_IMAGE` para a tag anterior e execute `docker compose up -d --no-build api`. Reverter somente a imagem não reverte o schema; qualquer migração aplicada exige um plano de rollback revisado separadamente.
