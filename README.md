# WhatsApp-to-DB

**Query your databases in plain English.**

WhatsApp-to-DB is an open-source Natural Language to SQL application built on [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel). It lets users ask questions in natural language — through a web UI **or directly from WhatsApp** — and returns results by generating and executing the appropriate SQL. Built for teams, it ships with enterprise-grade security, a fully extensible provider model, and a metadata-driven Admin UI that requires no code changes to configure.

---

## Features

### Core
- **Natural language to SQL** — powered by any LLM you configure (OpenAI, Ollama, Claude, or your own)
- **Multi-user, multi-role** — concurrent users each scoped to their own role, data access, and session context
- **Persistent chat history** — every conversation turn stored in SQLite; full context on every request
- **Fuzzy / semantic search** — vector DB integration lets the LLM match approximate or misspelled filter values

### WhatsApp Integration
- **Ask questions over WhatsApp** — users can send natural language questions directly to a WhatsApp number; the backend receives them via a WhatsApp webhook controller and processes them through the same pipeline as the web UI
- **Multiple WhatsApp profiles** — administrators can configure multiple WhatsApp profiles, each mapped to a specific database; questions sent to a profile are routed to its mapped database automatically
- **User-linked numbers** — each user account has a `WhatsAppNumber` field; incoming messages are matched to a user by their sender number
- **Role-based access over WhatsApp** — access is governed by the matched user's role; if the user is not authorised to query the mapped database, the request is denied
- **Same security pipeline** — prompt restriction and SQL monitor extensions apply identically whether a question arrives from the web UI or WhatsApp

### Security
- **Prompt restriction extension** — configure user-wise and module-wise rules that inspect (and optionally block or rewrite) prompts before they reach the LLM
- **SQL monitor extension** — rules-based inspection of every generated SQL statement before execution
- **Database user context** — sets a per-user session context on the connection before execution, activating row-level security policies in the database engine
- **Role-wise connection strings** — each role maps to a dedicated database credential, limiting accessible tables and views at the connection level

### Efficiency
- **Module-wise schema** — send only the schema subset relevant to the active module, reducing token usage and improving generation accuracy
- **Module-wise prompts** — concentrated, domain-specific system prompts per module; no need for one large catch-all prompt
- **Few-shot queries** — per-module example question → SQL pairs guide the LLM toward the expected style and structure
- **Vector-indexed columns** — administrators choose exactly which tables and columns are embedded and indexed

### Extensibility
- **Pluggable DB providers** — MS SQL Server and SQLite built in; PostgreSQL included as a sample external provider
- **Pluggable LLM providers** — OpenAI built in; Ollama (local) and Claude included as sample external providers
- **Pluggable embedding generators** — ElBruno and OpenAI built in; Jina included as a sample external provider
- **Pluggable vector DB providers** — SQLite built in; Chroma included as a sample external provider
- **Kernel Function plugins** — register custom .NET plugins to let the LLM call other databases, REST APIs, or local file servers mid-response

### Configuration & Admin UI
- **Single config root** — one path in `appsettings.json`; everything else lives in JSON files in that folder
- **Live reload** — most configuration changes (schema, prompts, security rules, roles) apply immediately without a restart
- **Metadata-driven Admin UI** — every configuration form is generated at runtime from a JSON metadata descriptor; adding a field means editing a JSON file, not writing frontend code
- **Metadata editor in the UI** — the metadata descriptors themselves are editable through the Admin UI

** Refer the doc folder in the repository for detailed documents and screenshots. **
---

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | C# / .NET 9, Microsoft Semantic Kernel |
| Chat history | SQLite |
| Default DB providers | MS SQL Server, SQLite |
| Default LLM provider | OpenAI |
| Default embedding generators | ElBruno, OpenAI |
| Default vector DB | SQLite |
| Sample external DB provider | PostgreSQL |
| Sample external LLM providers | Ollama, Claude (Anthropic) |
| Sample external embedding provider | Jina |
| Sample external vector DB provider | Chroma |
| WhatsApp integration | WhatsApp webhooks (dedicated controller in the API) |
| Frontend | Metadata-driven Admin UI (Node.js / npm) |
| Configuration | JSON files |

---

## Getting Started

### Prerequisites

| Requirement | Notes |
|---|---|
| .NET 9 SDK | Required to build and run the backend |
| nvm-windows | Download and run the installer; ensure `nvm` is on your PATH |
| Node.js 24.13.1 | Installed via nvm (see below) |
| npm 11.8.0 | Installed globally via npm (see below) |
| Git | To clone the repository |

### 1 — Set up Node.js

```bash
nvm install 24.13.1
nvm use 24.13.1
npm install -g npm@11.8.0
```

### 2 — Configure the backend

Open `appsettings.json` and set `ConfigRootFolder` to the folder that will hold all runtime configuration files:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConfigRootFolder": "C:\\Your\\Config\\Path\\"
}
```

Create the following two files in that folder before starting the application.

**`users.json`** — defines application users. Passwords are plain text on first setup; updating via the Admin UI encrypts them.

```json
[
  {
    "Username": "admin",
    "Password": "admin",
    "Fullname": "Administrator",
    "Role": "Admin",
    "InternalUserId": "1",
    "SessionContextKey": "EmployeeID",
    "DefaultDatabase": "sqlite-chinook",
    "WhatsAppNumber": "123456"
  }
]
```

**`defaultsettings.json`** — sets the default database and LLM provider:

```json
[
  {
    "DefaultDatabase": "sqlite-chinook",
    "DefaultLlmProvider": "Ollama",
    "DefaultLlmModel": "gemma4:31b-cloud"
  }
]
```

### 3 — Build and run the backend

Navigate to the `WhatsApp-to-DB` folder:

```bash
bld.bat    # builds the solution
runw.bat   # starts the backend
```

### 4 — Run the frontend

Navigate to `WhatsApp-to-DB\DBChatUI`:

```bash
npm install    # first run only
npm run dev
```

Open `http://localhost:5173` — you should see the login screen. Log in with `admin` / `admin`.

### 5 — Configure via Admin UI

Go to `http://localhost:5173/admin`. Using the left panel, complete these steps in order:

1. **Database** — add a database connection
2. **LLM** — add an LLM provider and model
3. **Users** — review and update user accounts
4. **Schema** — define which tables or views the LLM can see (within the database config)
5. **Roles** — configure role-based connection strings
6. **Prompts** — add system-level and module-level prompts as needed
7. **WhatsApp profiles** — create one or more profiles, each mapped to a database; add the matching `WhatsAppNumber` to each user account that should have access

Once configured, return to `http://localhost:5173`, select your LLM and database in the top-right, and start asking questions.

---

## Architecture Overview

The application is built around a set of loosely coupled, DI-registered services. Swapping a provider — database, LLM, embedding generator, or vector DB — means implementing an interface in an external .NET class library, registering it in the relevant JSON config file, and restarting once to load the assembly. All subsequent configuration changes to that provider take effect immediately.

```
Admin UI  ──►  REST API  ──►  Semantic Kernel Orchestrator
                  ▲                   │
WhatsApp          │       ┌───────────┼───────────┐
Webhook  ─────────┘       ▼           ▼           ▼
                     LLM Provider  DB Provider  Vector DB
                   (OpenAI/Ollama) (MSSQL/…)  (SQLite/…)
                          │
                          ▼
                 Kernel Function Plugins
                 (custom APIs / DBs / files)
```

**Request flow:**
1. User submits a natural language question — via the web UI, or via WhatsApp (webhook → controller → same pipeline)
2. ConfigService resolves the module's schema and prompt
3. Prompt restriction extension validates the prompt
4. Module schema + prompt + few-shot examples + chat history → LLM
5. LLM returns SQL; SQL monitor extension inspects it
6. DB provider sets user context; SQL executes via role connection string
7. Results returned; conversation turn persisted to SQLite

---

## Adding an External Provider

1. Implement the appropriate interface (`IDbProvider`, `ILlmProvider`, `IEmbeddingGenerator`, or `IVectorDbProvider`) in a .NET class library
2. Build and place the DLL in an accessible path
3. Register the assembly path and fully qualified type name in the relevant JSON config file
4. Restart the application once to load the assembly
5. All further configuration changes to that provider apply immediately — no further restarts needed

---

## Tips

**Simple databases** — for small or well-structured schemas, adding schema information alone is often enough to start asking questions without any prompt at all. The [Chinook](https://github.com/lerocha/chinook-database) and Stack Overflow sample databases are good starting points.

**Enterprise databases** — for large or complex schemas, module-wise prompts, few-shot queries, and a per-module schema subset become important for accuracy and cost control.

**Use views** — if table relationships are complex or column names are cryptic, create views that hide that complexity and reference the views in the schema config instead of the raw tables. You don't need to do this for every table — just where it makes the intent clearer.
