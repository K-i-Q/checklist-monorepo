# Checklist (Back + Front) — Quickstart + Seed + Cenários

Guia **objetivo** para rodar localmente (API .NET + UI Angular) em monorepo, com **seed** e **roteiro de testes**.

## Stack

- **API:** .NET 9, EF Core, SQL Server (Docker), concorrência otimista com `rowversion`
- **UI:** Angular 18+, Tailwind, tema claro/escuro (persistido), mobile‑first

## Estrutura do repo

```
/ (raiz do monorepo)
├─ checklist/Checklist.Api      # Backend
└─ checklist-ui/                # Frontend
```

## Pré‑requisitos

- **Docker Desktop** ativo
- **.NET SDK 9** → `dotnet --info`
- **Node 20/22+** → `node -v` (Angular CLI opcional: `npm i -g @angular/cli`)
- **sqlcmd** (macOS: `brew install sqlcmd`)\
  _Notas Linux/Windows ao final_

---

## 1) SQL Server via Docker (macOS Apple Silicon)

```bash
docker run -e "ACCEPT_EULA=Y" \
  -e "MSSQL_SA_PASSWORD=YourStrong\!Passw0rd" \
  -p 1433:1433 --name sql2022 \
  -d --platform linux/amd64 mcr.microsoft.com/mssql/server:2022-latest
```

> No **zsh**, o `!` precisa de **escape** (`\!`) ou use `'YourStrong!Passw0rd'`.

Teste:

```bash
sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd' -Q "SELECT @@VERSION;"
```

## 2) Criar a base

```bash
sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd' -Q "IF DB_ID('ChecklistDb') IS NULL BEGIN CREATE DATABASE ChecklistDb; END;"
```

## 3) Migrar e subir a API

```bash
cd api/Checklist.Api
dotnet restore
DOTNET_ENVIRONMENT=Development dotnet ef database update
DOTNET_ENVIRONMENT=Development dotnet run
```

API em [**http://localhost:5095**](http://localhost:5095).

## 4) Seed mínimo (veículo + template + itens)

> Escolha **uma** das opções abaixo.

**Opção A — SQL direto:**

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

**Opção B — via API (curl):**

```bash
# listar veículos
curl http://localhost:5095/api/checklists/vehicles | jq
# listar templates
curl http://localhost:5095/api/checklists/templates | jq
# listar itens de um template
curl http://localhost:5095/api/checklists/templates/<TEMPLATE_ID>/items | jq
```

## 5) Rodar a UI

```bash
cd ui/
npm install
ng serve --open
```

UI em [**http://localhost:4200**](http://localhost:4200) consumindo a API em `http://localhost:5095`.

---

## Como usar (fluxo)

1. **Nova execução**: selecione Template, Veículo e (opcional) **Data de referência**.
2. **Criar execução** → **Iniciar**.
3. Marque itens (OK/NOK/N/A), **Enviar para aprovação** e finalize com **Aprovar/Reprovar**.
4. Use o botão de **Tema** para alternar claro/escuro; verifique responsividade no mobile.

---

## Cenários de teste (recomendados)

**A) Caminho feliz**

- Criar → Iniciar → marcar todos **OK** → **Enviar para aprovação** → **Aprovar**.

**B) Obrigatórios marcados como N/A**

- Deixe pelo menos 1 item **Required** como **N/A** e tente **Enviar**.\
  Esperado: erro amigável bloqueando envio.

**C) Concorrência (otimista, **``**)**

- Abra 2 abas na mesma execução.
- Na **aba 1**, altere um item. Na **aba 2**, tente alterar o mesmo item **sem recarregar**.\
  Esperado: a UI informa conflito e **recarrega** a execução.

**D) Exclusividade “1 execução ativa por veículo + data”**

- Crie execução para o mesmo **Veículo** e **Data de referência** já com uma Draft/InProgress.\
  Esperado: a API retorna 409/2601 e a UI **carrega a execução existente** automaticamente (fallback).

**E) Papéis (Executor vs Supervisor)**

- Ajuste `localStorage` (chaves `api.userId` e `api.role`) para alternar:\
  `api.role = "Executor"` para iniciar/preencher; `api.role = "Supervisor"` para aprovar.

**F) Tema/UX**

- Verifique dark mode consistente (cards, tabelas, badges), e layout mobile‑first.

---

## Solução de problemas

- ``→ escape`!` na senha ou use aspas simples.
- **Porta 1433 ocupada** → `docker rm -f sql2022` e subir novamente.
- **Erro de índice único ao criar execução** → já existe uma ativa para **Veículo+Data**; a UI deve carregar a existente. Caso esteja via API, consulte:
  - `GET /api/checklists/executions/active?vehicleId=...&date=YYYY-MM-DD`
- **Migrations antigas com **`** em **` → após atualizar, garanta que novas execuções informem data ou que a UI sugira uma.

---

## Notas rápidas por SO

- **macOS**: comandos acima já prontos.
- **Linux**: normalmente **sem** `--platform linux/amd64` no `docker run`; instale `sqlcmd` via gerenciador da distro.
- **Windows**: PowerShell aceita `"`/`'`; para variáveis de ambiente use `$env:DOTNET_ENVIRONMENT = 'Development'`.

---

# Complementos importantes

## ✅ Seed mínimo — equivalência das opções (A) SQL e (B) cURL

Ambas as opções abaixo deixam o banco **no mesmo estado útil**: 1 veículo, 1 template com 3 itens e **1 execução** com esses 3 itens. A diferença é **como** a execução é criada:

- **(A)** cria tudo **via SQL** diretamente (inclui a Execução e seus Itens).
- **(B)** cria **Veículo/Template/Itens via SQL** e a **Execução via API (cURL)** — o resultado final é idêntico (as linhas em `Executions` e `ExecutionItems` ficam equivalentes).

> IDs fixos são usados para facilitar testes e replays.

### (A) SQL direto (cria TUDO, inclusive a execução)

```bash
sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd' -d ChecklistDb -Q "
DECLARE @veh UNIQUEIDENTIFIER='D83D3241-4710-4566-A118-662B80ECC543';
DECLARE @tpl UNIQUEIDENTIFIER='53948D28-DC6F-486A-9B04-19028A229BAB';
DECLARE @d   DATE='2025-08-29';

IF NOT EXISTS (SELECT 1 FROM Vehicles WHERE Id=@veh)
  INSERT INTO Vehicles(Id,Plate,Model) VALUES(@veh,'ABC1D23','Sprinter');

IF NOT EXISTS (SELECT 1 FROM Templates WHERE Id=@tpl)
  INSERT INTO Templates(Id,Name) VALUES(@tpl,N'Saída padrão');

-- Itens do template (3 itens)
IF NOT EXISTS (SELECT 1 FROM TemplateItems WHERE Id='C6DAEB8E-20E6-4B99-8CF6-ABCA1980ED5C')
  INSERT INTO TemplateItems(Id,TemplateId,Label,[Order],Required)
  VALUES('C6DAEB8E-20E6-4B99-8CF6-ABCA1980ED5C',@tpl,N'Pneus calibrados',1,1);
IF NOT EXISTS (SELECT 1 FROM TemplateItems WHERE Id='65ADB99F-2371-446D-A828-3883A7288057')
  INSERT INTO TemplateItems(Id,TemplateId,Label,[Order],Required)
  VALUES('65ADB99F-2371-446D-A828-3883A7288057',@tpl,N'Faróis funcionando',2,1);
IF NOT EXISTS (SELECT 1 FROM TemplateItems WHERE Id='EF64ABDA-B15E-4447-BB72-0AF044757103')
  INSERT INTO TemplateItems(Id,TemplateId,Label,[Order],Required)
  VALUES('EF64ABDA-B15E-4447-BB72-0AF044757103',@tpl,N'Kit de emergência',3,0);

-- Execução e Itens (Status=Draft, itens = N/A)
DECLARE @exec UNIQUEIDENTIFIER='F4E4E895-B5F6-4F2D-BEC7-9C7E735FF896';
IF NOT EXISTS (SELECT 1 FROM Executions WHERE Id=@exec)
BEGIN
  INSERT INTO Executions(Id,TemplateId,VehicleId,Status,ReferenceDate)
  VALUES(@exec,@tpl,@veh,0,@d);

  INSERT INTO ExecutionItems(Id,ExecutionId,TemplateItemId,Status,Observation)
  SELECT NEWID(), @exec, ti.Id, 2, NULL
  FROM TemplateItems ti
  WHERE ti.TemplateId=@tpl
  ORDER BY ti.[Order];
END;

SELECT 'VehicleId' AS K, @veh AS V
UNION ALL SELECT 'TemplateId', @tpl
UNION ALL SELECT 'ExecutionId', @exec;"
```

### (B) SQL + cURL (Execução via API)

1. **Base (SQL)** — mesmo bloco de SQL acima **até antes** da seção “Execução e Itens”. (Isso cria somente Veículo/Template/Itens.)
2. **Criar execução via API**:

```bash
curl -s -X POST http://localhost:5095/api/checklists/executions \
  -H "Content-Type: application/json" \
  -d '{
        "templateId":"53948d28-dc6f-486a-9b04-19028a229bab",
        "vehicleId":"d83d3241-4710-4566-a118-662b80ecc543",
        "referenceDate":"2025-08-29"
      }'
# → retorna {"id":"<GUID>"}
```

> **Resultado**: em ambos os casos você terá 1 execução com 3 itens (status N/A) pronta para iniciar. ✅

---

## 👥 Papéis (Executor x Supervisor) — passo a passo de teste

**Pré‑requisito**: uma execução `Draft` já criada (veja o _Seed mínimo_ acima).

### A. Fluxo do _Executor_

1. **Iniciar**
   ```bash
   curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/start \
     -H "Content-Type: application/json" \
     -d '{"executorId":"11111111-1111-1111-1111-111111111111"}'
   ```
   _Espera_: `200 OK`, Status muda para **InProgress**.
2. **Atualizar itens (OK/NOK/N/A)** — respeita _rowversion_ por item:

   ```bash
   # Obtenha o JSON da execução p/ capturar rowVersion de cada item
   curl -s http://localhost:5095/api/checklists/executions/<EXEC_ID> | jq

   # PATCH de um item obrigatório para OK
   curl -i -X PATCH \
     http://localhost:5095/api/checklists/executions/<EXEC_ID>/items/C6DAEB8E-20E6-4B99-8CF6-ABCA1980ED5C \
     -H "Content-Type: application/json" \
     -H "X-User-Id: 11111111-1111-1111-1111-111111111111" \
     -H "X-User-Role: Executor" \
     -d '{"status":0, "observation":"OK", "rowVersion":"<ROWVERSION_DO_ITEM>"}'
   ```

   _Espera_: `204 No Content`.

3. **Validar regra de N/A em obrigatórios**
   - Se qualquer item obrigatório estiver `N/A`, o envio falha com `400` e mensagem.
4. **Enviar para aprovação**
   ```bash
   curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/submit \
     -H "Content-Type: application/json" \
     -H "X-User-Id: 11111111-1111-1111-1111-111111111111" \
     -H "X-User-Role: Executor" \
     -d '{"rowVersion":"<ROWVERSION_DA_EXEC>"}'
   ```
   _Espera_: `200 OK`, Status → **Submitted**.

### B. Fluxo do _Supervisor_

1. **Aprovar**
   ```bash
   curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/approve \
     -H "Content-Type: application/json" \
     -H "X-User-Id: 22222222-2222-2222-2222-222222222222" \
     -H "X-User-Role: Supervisor" \
     -d '{"decision":0, "notes":"Tudo certo", "rowVersion":"<ROWVERSION_DA_EXEC>"}'
   ```
   _Espera_: `200 OK`, Status → **Approved**, registro criado em `Approvals`.
2. **Reprovar**
   - Mude `decision` para `1` e uma anotação em `notes`.

> **Dica ① (UI)**: nos botões do front, alterne “Perfil” (Executor/Supervisor) e repita as mesmas ações. **Dica ② (Concorrência)**: abra **duas abas** como Executor; faça PATCH do mesmo item em cada aba e veja o `409 Conflict` em uma delas; a UI já se recarrega silenciosamente.

---

## 🧪 Cenários de teste detalhados (checklist)

> Cada cenário abaixo tem **passos via UI** (recomendado) e **cURL opcional** para repetir sem interface.

### 1) Exclusividade: **1 execução ativa por Veículo + Data**

**Objetivo:** impedir Duplicatas Draft/InProgress no mesmo (Veículo, Data).

**UI**

1. Crie Execução para `Veículo A` em `2025-08-29`.
2. Sem finalizar a anterior, tente **criar outra** para o mesmo veículo/data.
   - **Esperado:** a UI não cria uma nova. Ela consulta `GET /executions/active?vehicleId=...&date=...` e **carrega** a existente.

**cURL**

```bash
# tentar criar duplicada
curl -i -X POST http://localhost:5095/api/checklists/executions \
  -H "Content-Type: application/json" \
  -d '{
        "templateId":"<TPL_ID>",
        "vehicleId":"<VEH_ID>",
        "referenceDate":"2025-08-29"
      }'
# Em caso de já existir uma ativa, consulte a atual
curl -s "http://localhost:5095/api/checklists/executions/active?vehicleId=<VEH_ID>&date=2025-08-29" | jq
```

---

### 2) Concorrência Otimista — **Item (rowversion por item)**

**Objetivo:** ao atualizar o mesmo item em 2 abas, apenas a 1ª grava; a 2ª recebe `409` e recarrega.

**UI**

1. Abra **duas abas** na mesma Execução **InProgress**.
2. Na **Aba 1**, marque um item obrigatório **OK** e aguarde o toast de sucesso.
3. Na **Aba 2**, sem recarregar, marque o **mesmo item**.
   - **Esperado:** toast de conflito e a UI **recarrega** silenciosamente a execução.

**cURL**

```bash
# capture o rowVersion do item
curl -s http://localhost:5095/api/checklists/executions/<EXEC_ID> | jq '.items[] | {templateItemId, rowVersion}'

# PATCH com rowVersion antigo (irá falhar se já atualizado)
curl -i -X PATCH \
  http://localhost:5095/api/checklists/executions/<EXEC_ID>/items/<TEMPLATE_ITEM_ID> \
  -H "Content-Type: application/json" \
  -H "X-User-Id: 11111111-1111-1111-1111-111111111111" \
  -H "X-User-Role: Executor" \
  -d '{"status":0, "observation":"OK", "rowVersion":"<ROWVERSION_ANTIGO>"}'
```

---

### 3) Regra de negócio — **Obrigatórios ≠ N/A**

**Objetivo:** impedir envio quando existir item obrigatório em **N/A**.

**UI**

1. Deixe 1 item obrigatório como **N/A**.
2. Clique **Enviar para aprovação**.
   - **Esperado:** erro amigável (400) informando que há obrigatórios N/A.

**cURL**

```bash
curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/submit \
  -H "Content-Type: application/json" \
  -H "X-User-Id: 11111111-1111-1111-1111-111111111111" \
  -H "X-User-Role: Executor" \
  -d '{"rowVersion":"<ROWVERSION_DA_EXEC>"}'
```

---

### 4) Papéis — **Executor vs Supervisor**

**Objetivo:** validar permissões e caminhos distintos.

**UI**

- No canto superior, troque o **Perfil** para `Executor` ao iniciar/preencher; `Supervisor` para aprovar/reprovar.
- A UI persiste as credenciais/role no `localStorage` (`api.userId`, `api.role`).

**cURL**

```bash
# iniciar (Executor)
curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/start \
  -H "Content-Type: application/json" \
  -d '{"executorId":"11111111-1111-1111-1111-111111111111"}'

# aprovar (Supervisor)
curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/approve \
  -H "Content-Type: application/json" \
  -H "X-User-Id: 22222222-2222-2222-2222-222222222222" \
  -H "X-User-Role: Supervisor" \
  -d '{"decision":0, "notes":"Tudo certo", "rowVersion":"<ROWVERSION_DA_EXEC>"}'
```

---

### 5) **Lock de executor** na inicialização

**Objetivo:** uma execução iniciada por um executor não pode ser retomada por outro.

**UI**

1. Como Executor **A**, clique **Iniciar**.
2. Mude o Perfil para Executor **B** e tente **Iniciar** a mesma execução.
   - **Esperado:** **409** (já iniciado por outro executor) e a UI mantém o estado.

**cURL**

```bash
# já iniciada por A; tentar iniciar com B retorna 409
curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/start \
  -H "Content-Type: application/json" \
  -d '{"executorId":"BBBBBBBB-BBBB-BBBB-BBBB-BBBBBBBBBBBB"}'
```

---

### 6) Concorrência — **Submit com rowversion desatualizado**

**Objetivo:** ao tentar enviar com `rowVersion` antigo, retornar **409** e recarregar.

**UI**

- Em 2 abas, na **Aba 1** finalize (Submit). Na **Aba 2**, tente enviar.
  - **Esperado:** conflito; a UI recarrega.

**cURL**

```bash
curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/submit \
  -H "Content-Type: application/json" \
  -H "X-User-Id: 11111111-1111-1111-1111-111111111111" \
  -H "X-User-Role: Executor" \
  -d '{"rowVersion":"<ROWVERSION_ANTIGA>"}'
```

---

### 7) Concorrência — **Approve com rowversion desatualizado**

**Objetivo:** ao aprovar/reprovar com `rowVersion` antigo, retornar **409**.

**UI**

- Em 2 abas como Supervisor; na **Aba 1** aprove; na **Aba 2** tente aprovar/reprovar.

**cURL**

```bash
curl -i -X POST http://localhost:5095/api/checklists/executions/<EXEC_ID>/approve \
  -H "Content-Type: application/json" \
  -H "X-User-Id: 22222222-2222-2222-2222-222222222222" \
  -H "X-User-Role: Supervisor" \
  -d '{"decision":1, "notes":"Exemplo", "rowVersion":"<ROWVERSION_ANTIGA>"}'
```

---

### 8) Tema + Responsividade (UX)

**Objetivo:** validar consistência do Dark/Light e layout mobile‑first.

- Alterne o **Tema** (botão no cabeçalho) e confira cartões/tabelas/badges.
- No DevTools, teste em iPhone/Pixel (larguras ≤ 768px) — a UI troca a **tabela** por **cards**.

---

## 🔎 APIs úteis durante os testes

```
GET  /api/checklists/vehicles
GET  /api/checklists/templates
GET  /api/checklists/templates/{templateId}/items

POST /api/checklists/executions                       { templateId, vehicleId, referenceDate }
GET  /api/checklists/executions/active?vehicleId=&date=
GET  /api/checklists/executions/{id}
POST /api/checklists/executions/{id}/start            { executorId }
PATCH/api/checklists/executions/{id}/items/{templateItemId} { status, observation, rowVersion }
POST /api/checklists/executions/{id}/submit           { rowVersion }
POST /api/checklists/executions/{id}/approve          { decision, notes, rowVersion }
```

> **Cabeçalhos de papel**: use `X-User-Role: Executor` para `PATCH/submit` e `X-User-Role: Supervisor` para `approve` (acompanhe com `X-User-Id`).

---

## ✅ Critérios de aceitação (resumo)

- Exclusividade `(VehicleId, ReferenceDate)` enquanto `Status ∈ {Draft, InProgress}`.
- Regras de required (não enviar com N/A obrigatório).
- Concorrência otimista: `409` gera **recarregamento** automático no front.
- Trilhas de aprovação: registro em `Approvals` ao decidir.
- UX: dark/light consistente; mobile‐first funcional.

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
  docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=YourStrong\!Passw0rd" \
  -p 1433:1433 --name sql2022 -d --platform linux/amd64 \
  mcr.microsoft.com/mssql/server:2022-latest
```

---

## 📌 Observações finais

- Em erros de duplicidade (índice único), a UI tenta **carregar** a execução existente automaticamente.
- Exemplos usam datas e GUIDs fixos para facilitar replays; ajuste conforme necessário.
- Para auditoria, acompanhe logs do Kestrel e mensagens no toast da UI.
