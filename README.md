# Checklist (Back + Front) — Quickstart + Seed

Guia **objetivo** para rodar localmente (API .NET + UI Angular) em monorepo, com **seed** inicial. Abra **dois terminais na raiz** do repositório: um para a API e outro para a UI.

## Stack

- **API:** .NET 9, EF Core, SQL Server (Docker), concorrência otimista com `rowversion`
- **UI:** Angular 18+, Tailwind, tema claro/escuro (persistido)

## Estrutura do repo

```
/ (raiz)
├─ api/
│  └─ Checklist.Api/           # Backend (.NET)
└─ ui/                          # Frontend (Angular)
```

## Pré‑requisitos

- **Docker Desktop** ativo
- **.NET SDK 9** → `dotnet --info`
- **Node 20/22+** → `node -v` (Angular CLI opcional: `npm i -g @angular/cli`)
- **sqlcmd** (macOS: `brew install sqlcmd`)

> Dica: com o projeto aberto no editor, use **Terminal A** p/ API e **Terminal B** p/ UI, sempre partindo da **raiz** do repo.

---

## 1) SQL Server via Docker (macOS/Linux)

> **Use aspas simples na senha** para evitar problemas com `!` no zsh/bash.

```bash
docker run \
  -e ACCEPT_EULA=Y \
  -e MSSQL_SA_PASSWORD='YourStrong!Passw0rd' \
  -p 1433:1433 --name sql2022 \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

Teste a conexão:

```bash
sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd' -Q "SELECT @@VERSION;"
```

## 2) Criar a base

```bash
sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd' -Q "IF DB_ID('ChecklistDb') IS NULL BEGIN CREATE DATABASE ChecklistDb; END;"
```

## 3) Migrar e subir a API

**Terminal A (na raiz):**

```bash
cd api/Checklist.Api
dotnet restore
DOTNET_ENVIRONMENT=Development dotnet ef database update
DOTNET_ENVIRONMENT=Development dotnet run
```

API disponível em **[http://localhost:5095](http://localhost:5095)**.

## 4) Seed mínimo (veículo + template + itens)

> Escolha **uma** das opções abaixo.

### Opção A — SQL direto

```bash
sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd' -d ChecklistDb -Q "
DECLARE @veh UNIQUEIDENTIFIER=NEWID(), @tpl UNIQUEIDENTIFIER=NEWID();
INSERT INTO Vehicles(Id,Plate,Model) VALUES(@veh,'ABC1D23','Sprinter');
INSERT INTO Templates(Id,Name) VALUES(@tpl,N'Saída padrão');
INSERT INTO TemplateItems(Id,TemplateId,Label,[Order],Required) VALUES
(NEWID(),@tpl,N'Pneus calibrados',1,1),
(NEWID(),@tpl,N'Faróis funcionando',2,1),
(NEWID(),@tpl,N'Kit de emergência',3,0);
SELECT @veh AS VehicleId, @tpl AS TemplateId;"
```

Anote `VehicleId` e `TemplateId` (a UI também lista via API).

### Opção B — via API (curl)

```bash
# listar veículos
curl http://localhost:5095/api/checklists/vehicles | jq
# listar templates
curl http://localhost:5095/api/checklists/templates | jq
# listar itens de um template
curl http://localhost:5095/api/checklists/templates/<TEMPLATE_ID>/items | jq
```

## 5) Rodar a UI

**Terminal B (na raiz):**

```bash
cd ui
npm install
ng serve --open
```

UI em **[http://localhost:4200](http://localhost:4200)** (consome a API em `http://localhost:5095`).

---

## Como usar (fluxo)

1. **Nova execução**: selecione Template, Veículo e (opcional) **Data de referência**.
2. **Criar execução** → **Iniciar**.
3. Marque itens (OK/NOK/N/A), **Enviar para aprovação** e finalize com **Aprovar/Reprovar**.
4. Use o botão de **Tema** para alternar claro/escuro.

---

## Notas rápidas por SO

- **macOS/Linux:** comandos acima já prontos (atenção às **aspas simples** na senha `'YourStrong!Passw0rd'`).
- **Windows (PowerShell):** use `-e "MSSQL_SA_PASSWORD=YourStrong!Passw0rd"` no `docker run` e `-P "YourStrong!Passw0rd"` no `sqlcmd`.

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

**Objetivo:** editar o mesmo item em 2 abas; só a 1ª grava, a 2ª recebe `409` e recarrega.

**UI**

1. Abra **duas abas** na mesma execução **InProgress**.
2. Na Aba 1, marque um item obrigatório como **OK**.
3. Na Aba 2, sem recarregar, mude o **mesmo item**.\
   **Esperado:** toast de conflito e recarregamento automático.

**cURL**

```bash
# capture rowVersion dos itens
curl -s http://localhost:5095/api/checklists/executions/<EXEC_ID> | jq '.items[] | {templateItemId, rowVersion}'

# PATCH usando um rowVersion antigo (deve falhar)
curl -i -X PATCH \
  http://localhost:5095/api/checklists/executions/<EXEC_ID>/items/<TEMPLATE_ITEM_ID> \
  -H 'Content-Type: application/json' \
  -H 'X-User-Id: 11111111-1111-1111-1111-111111111111' \
  -H 'X-User-Role: Executor' \
  -d '{"status":0, "observation":"OK", "rowVersion":"<ROWVERSION_ANTIGO>"}'
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
