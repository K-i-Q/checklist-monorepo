# Checklist (Back + Front) — Quickstart com Make/Compose + Seed

Guia **objetivo** para rodar localmente (API .NET + UI Angular) em monorepo, agora com **Docker Compose** e **Makefile**. A **primeira subida** aplica migrations e faz **seed idempotente** (cria usuários + 1 veículo + 1 template com 3 itens).

## Stack

- **API:** .NET 9, EF Core, SQL Server (container Docker), concorrência otimista com `rowversion`
- **UI:** Angular 18+, Tailwind, tema claro/escuro (persistido)

## Estrutura do repo

```
/ (raiz)
├─ api/
│  └─ Checklist.Api/              # Backend (.NET)
├─ ui/                            # Frontend (Angular)
├─ docker-compose.yml             # orquestra SQL + API + UI
└─ Makefile                       # atalhos de up/start/logs/reset
```

## Pré‑requisitos

- **Docker Desktop** ativo
- **.NET SDK 9** → `dotnet --info`
- **Node 20/22+** → `node -v` (Angular CLI opcional: `npm i -g @angular/cli`)

> Abra o repositório no seu editor e use os comandos **a partir da raiz**.

---

## Subir com Make (recomendado)

### Primeira vez (zera, sobe e mostra logs)

```bash
make first
```

O que acontece:

- Derruba containers/volumes anteriores (zera o banco)
- `` para SQL (`sql2022`), API e UI
- Segue logs somente de **api** e **ui** (mais “limpos”)
- A API aplica **migrations** e roda **seed idempotente**

### Próximas vezes

```bash
make start   # sobe em segundo plano (sem rebuild)
make logs    # segue logs da api/ui
```

### Parar / Resetar

```bash
make stop    # para containers (mantém dados)
make reset   # para e apaga volumes (zera o banco)
```

### URLs

- **API:** [http://localhost:5095](http://localhost:5095)
- **UI:** [http://localhost:4200](http://localhost:4200)

> A UI já aponta para `http://api:5095` dentro da rede do compose e expõe em `http://localhost:4200`.

---

## Subir manualmente (sem Make)

Se preferir, rode direto com Compose:

```bash
docker compose up -d --build
# (opcional) ver logs enxutos
docker compose logs -f api ui
```

Ou modo “dev solto” (sem compose):

- **Terminal A**
  ```bash
  cd api/Checklist.Api
  dotnet run
  ```
- **Terminal B**
  ```bash
  cd ui
  npm install
  ng serve --open
  ```

> Nesse modo, suba um SQL Server por fora (Docker/instalado) e ajuste a connection string.

---

## Configuração / Connection String

A API resolve a connection string **nesta ordem**:

1. `ConnectionStrings:SqlServer` no `appsettings.*.json`
2. `ConnectionStrings__SqlServer` (variável de ambiente)

No Compose já vem configurado para falar com o container ``. Você pode sobrescrever via **env** sem tocar em código.

---

## Seed idempotente (o que é criado)

Na **primeira subida**, a API aplica migrations e garante os registros (só cria se não existirem):

- **Veículo:** Placa `ABC1D23` — Modelo `Sprinter`
- **Template:** `Saída padrão` com 3 itens: _Pneus calibrados_ (obrig.), _Faróis funcionando_ (obrig.), _Kit de emergência_ (opcional)
- **Usuários:**
  - `Executor 1` — `11111111-1111-1111-1111-111111111111`
  - `Executor 2` — `33333333-3333-3333-3333-333333333333`
  - `Supervisor` — `22222222-2222-2222-2222-222222222222`

Se você derrubar com `make reset`, ao subir novamente o seed roda de novo e **continua idempotente**.

---

## Logs mais “limpos”

Na inicialização você verá mensagens resumidas, por exemplo:

- `DB ▸ aplicando migrações…`
- `DB ▸ verificando seed mínimo…`
- `DB ✓ seed aplicado (vehicle/template/itens)`

Logs verbosos do SQL Server continuam no container do banco; os da aplicação foram reduzidos.

---

## Como usar (fluxo básico)

1. Na UI, escolha **Perfil** (Executor 1/2 ou Supervisor) no cabeçalho.
2. **Nova execução**: selecione Template, Veículo e (opcional) **Data de referência**.
3. **Criar execução** → **Iniciar** (bloqueia a execução para o executor atual).
4. Marque itens (OK/NOK/N/A) e **Enviar para aprovação**.
5. Como **Supervisor**, **Aprovar** ou **Reprovar**.
6. Use o botão de **Tema** para alternar claro/escuro; confira responsividade no mobile.

---

## Cenários de teste (recomendados)

> Cada cenário tem passos via **UI** (recomendado) e **cURL opcional**. Na UI, use o seletor **Perfil** (canto superior) para alternar entre **Executor 1**, **Executor 2** e **Supervisor**. Nos exemplos de `curl`, **use aspas simples** no corpo (`-d '...'`).

### 1) Exclusividade — **1 execução ativa por Veículo + Data**

**Objetivo:** impedir duplicatas `Draft/InProgress` para o mesmo `(VehicleId, ReferenceDate)`.

**UI**

1. Crie uma execução para `Veículo A` em `2025-08-29`.
2. Sem finalizar, tente **criar outra** para o mesmo veículo/data.\
   **Esperado:** a API retorna `409` e a UI **carrega a execução existente** (fallback automático).

**cURL**

```bash
# tentar criar duplicada
curl -i -X POST http://localhost:5095/api/checklists/executions \
  -H 'Content-Type: application/json' \
  -d '{
        "templateId":"<TPL_ID>",
        "vehicleId":"<VEH_ID>",
        "referenceDate":"2025-08-29"
      }'

# consultar a ativa
curl -s 'http://localhost:5095/api/checklists/executions/active?vehicleId=<VEH_ID>&date=2025-08-29' | jq
```

---

### 2) Concorrência Otimista — **Item (rowversion por item)**

**Objetivo:** editar o mesmo item em 2 abas; só a 1ª grava, a 2ª recebe `409` e recarrega **quando tentar gravar algo diferente do que já está salvo**.

**UI**

1. Abra **duas abas** na mesma execução **InProgress**.
2. Na **Aba 1**, marque um item obrigatório como **OK** (ou altere a observação).
3. Na **Aba 2**, **sem recarregar**, tente mudar o **mesmo item**.
   - **Esperado:** toast de **conflito** e **recarregamento** automático **se houver mudança real** com `rowVersion` antigo.

> **Nota (idempotência x conflito):**
>
> - Se a 2ª aba enviar **exatamente os mesmos valores** que já estão no banco (ex.: status **OK → OK** novamente e **sem alterar a observação**), a API detecta **no‑op** e responde `204 No Content` (sem `409`). **Nada é alterado** e o `rowVersion` **não** é incrementado — isto é **intencional** para evitar conflitos desnecessários.
> - Para **forçar o **``, a 2ª aba deve enviar **alguma mudança real** (ex.: **OK ↔ NOK** **ou** alterar a **observação**) ainda com o `rowVersion`antigo. A UI trata o`409` e recarrega a execução.

**cURL**

```bash
# capture rowVersion dos itens
curl -s http://localhost:5095/api/checklists/executions/<EXEC_ID> | jq '.items[] | {templateItemId, rowVersion}'

# PATCH com rowVersion antigo e **mudança real** (deve falhar com 409)
curl -i -X PATCH \
  http://localhost:5095/api/checklists/executions/<EXEC_ID>/items/<TEMPLATE_ITEM_ID> \
  -H 'Content-Type: application/json' \
  -H 'X-User-Id: 11111111-1111-1111-1111-111111111111' \
  -H 'X-User-Role: Executor' \
  -d '{"status":1, "observation":"Exemplo de alteração", "rowVersion":"<ROWVERSION_ANTIGO>"}'
```

---

### 3) Regra de negócio — **Obrigatórios ≠ N/A**

**Objetivo:** não permitir **Submit** se houver item obrigatório como **N/A**.

**UI**

1. Deixe 1 item obrigatório como **N/A**.
2. Clique **Enviar para aprovação**.\
   **Esperado:** `400` + mensagem amigável.

**cURL**

```bash
curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/submit \
  -H 'Content-Type: application/json' \
  -H 'X-User-Id: 11111111-1111-1111-1111-111111111111' \
  -H 'X-User-Role: Executor' \
  -d '{"rowVersion":"<ROWVERSION_DA_EXEC>"}'
```

---

### 4) Papéis — **Executor vs Supervisor**

**Objetivo:** validar permissões distintas.

**UI**

- No seletor **Perfil**, escolha **Executor 1** ou **Executor 2** para iniciar/preencher; escolha **Supervisor** para aprovar/reprovar.
- **Executores** não veem botões _Aprovar/Reprovar_.

**cURL**

```bash
# iniciar (Executor)
curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/start \
  -H 'Content-Type: application/json' \
  -d '{"executorId":"11111111-1111-1111-1111-111111111111"}'

# aprovar (Supervisor)
curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/approve \
  -H 'Content-Type: application/json' \
  -H 'X-User-Id: 22222222-2222-2222-2222-222222222222' \
  -H 'X-User-Role: Supervisor' \
  -d '{"decision":0, "notes":"Tudo certo", "rowVersion":"<ROWVERSION_DA_EXEC>"}'
```

---

### 5) **Lock do executor** ao iniciar

**Objetivo:** execução iniciada por um executor não pode ser retomada por outro.

**UI**

1. Como **Executor 1**, clique **Iniciar**.
2. Troque o Perfil para **Executor 2** e tente **Iniciar** a mesma execução.\
   **Esperado:** `409` com mensagem _Já iniciado por outro executor._

**cURL**

```bash
curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/start \
  -H 'Content-Type: application/json' \
  -d '{"executorId":"33333333-3333-3333-3333-333333333333"}'
```

---

### 6) Concorrência — **Submit** com rowversion desatualizado

**UI**: finalize na Aba 1; tente enviar na Aba 2.\
**Esperado:** `409` + recarregar.

**cURL**

```bash
curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/submit \
  -H 'Content-Type: application/json' \
  -H 'X-User-Id: 11111111-1111-1111-1111-111111111111' \
  -H 'X-User-Role: Executor' \
  -d '{"rowVersion":"<ROWVERSION_ANTIGA>"}'
```

---

### 7) Concorrência — **Approve** com rowversion desatualizado

**UI**: como Supervisor, aprove/reprove na Aba 1; repita na Aba 2.\
**Esperado:** `409`.

**cURL**

```bash
curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/approve \
  -H 'Content-Type: application/json' \
  -H 'X-User-Id: 22222222-2222-2222-2222-222222222222' \
  -H 'X-User-Role: Supervisor' \
  -d '{"decision":1, "notes":"Exemplo", "rowVersion":"<ROWVERSION_ANTIGA>"}'
```

---

### 8) Tema + Responsividade (UX)

**Objetivo:** garantir dark/light e layout mobile‑first.

- Alterne o **Tema** no cabeçalho.
- No DevTools, teste em larguras ≤ 768px (lista vira **cards** no mobile).

---

## 🔎 APIs úteis durante os testes

```
GET  /api/checklists/vehicles
GET  /api/checklists/templates
GET  /api/checklists/templates/{templateId}/items

POST /api/checklists/executions                            { templateId, vehicleId, referenceDate }
GET  /api/checklists/executions/active?vehicleId=&date=
GET  /api/checklists/executions/{id}
POST /api/checklists/executions/{id}/start                 { executorId }
PATCH /api/checklists/executions/{id}/items/{templateItemId} { status, observation, rowVersion }
POST /api/checklists/executions/{id}/submit                { rowVersion }
POST /api/checklists/executions/{id}/approve               { decision, notes, rowVersion }
GET  /api/checklists/users
```

> **Cabeçalhos de papel (quando fizer cURL):** inclua `X-User-Id` e `X-User-Role` (Executor/Supervisor). Na UI isso é automático pelo seletor de Perfil.

---

## ✅ Critérios de aceitação (resumo)

- Exclusividade `(VehicleId, ReferenceDate)` enquanto `Status ∈ {Draft, InProgress}`.
- Regras de obrigatoriedade (não enviar com N/A obrigatório).
- Concorrência otimista: `409` dispara **recarregar** no front.
- Aprovação registra trilha em `Approvals`.
- UX consistente em dark/light e mobile‑first.

---

## 🧹 Reset / limpeza rápida

**Zerar execuções do dia para um veículo (SQL):**

```sql
DELETE FROM Approvals WHERE ExecutionId IN (
  SELECT Id FROM Executions WHERE VehicleId = '<VEH_ID>' AND ReferenceDate = '2025-08-29'
);
DELETE FROM ExecutionItems WHERE ExecutionId IN (
  SELECT Id FROM Executions WHERE VehicleId = '<VEH_ID>' AND ReferenceDate = '2025-08-29'
);
DELETE FROM Executions WHERE VehicleId = '<VEH_ID>' AND ReferenceDate = '2025-08-29';
```

**Recriar container do SQL Server:**

```bash
docker rm -f sql2022 && \
  docker run -e 'ACCEPT_EULA=Y' -e 'MSSQL_SA_PASSWORD=YourStrong!Passw0rd' \
  -p 1433:1433 --name sql2022 -d --platform linux/amd64 \
  mcr.microsoft.com/mssql/server:2022-latest
```

---

## 📌 Observações finais

- Em duplicidade (índice único), a UI tenta **carregar** a execução existente automaticamente.
- Exemplos usam GUIDs/datas fixas para facilitar replays; ajuste conforme necessário.
- Para auditoria, acompanhe logs do Kestrel e toasts da UI.
