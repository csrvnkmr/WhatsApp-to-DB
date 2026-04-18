# WhatsAppToDB

A conversational AI assistant that connects WhatsApp to a SQL Server database. Users ask questions in plain language via WhatsApp, and the system queries the database and responds — accurately, securely, and in the user's own language.

Built with **C#** and **Microsoft Semantic Kernel**.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Projects](#projects)
  - [WhatsAppToDB.Abstractions](#whatsapptodb-abstractions)
  - [WhatsAppToDB](#whatsapptodb-main)
  - [VectorDBSync](#vectordbsync)
  - [DBSearchHelperPlugin](#dbsearchhelperplugin)
  - [SecurityPlugin](#securityplugin)
- [Configuration](#configuration)
  - [appsettings.json](#appsettingsjson)
  - [Schema Definition File](#schema-definition-file)
  - [VectorConfig.json](#vectorconfigjson)
- [Key Features](#key-features)
- [Getting Started](#getting-started)

---

## Architecture Overview

```
WhatsApp Message
      ↓
Context Resolution       ← IdentityPersonaPlugin (maps WhatsApp number → user identity)
      ↓
Role & Module Access     ← IdentityService (role-based module permissions)
      ↓
GetAvailableModules      ← SchemaService
      ↓
GetSchemaForModule       ← SchemaService + IModulePrompt (dynamic security constraints)
      ↓
[Optional] Fuzzy Search  ← DBSearchHelperPlugin + ChromaDB
      ↓
SQL Generation           ← Semantic Kernel (LLM, schema-constrained)
      ↓
SQL Validation           ← ISqlInterceptor (inspect, modify, or reject)
      ↓
Set SESSION_CONTEXT      ← Row-level security for SQL Server
      ↓
Execute SQL              ← DatabaseQueryPlugin
      ↓
Natural Language Response (multilingual)
      ↓
WhatsApp Reply           ← WhatsAppService
```

---

## Projects

### WhatsAppToDB.Abstractions

Common interfaces and models shared across the main application and all plugins/extensions.

| Type | Description |
|------|-------------|
| `ISqlInterceptor` | Inspect and optionally modify the generated SQL before execution |
| `IModulePrompt` | Inject module-specific constraints or security rules into the LLM prompt at runtime |
| `IIdentityService` | Resolve the current user's identity from their WhatsApp number |
| `IdentityContext` | Holds the current user's role, WhatsApp number, session context key (e.g. `SalesPersonID`) and value (e.g. `277`) |

Any external plugin or extension must reference this project to implement these interfaces.

---

### WhatsAppToDB (Main)

The main application. Runs as a web host, listening for webhooks from the WhatsApp API.

| Class | Description |
|-------|-------------|
| `Program.cs` | Bootstraps services and starts the web host |
| `DatabaseQueryPlugin` | Core Semantic Kernel plugin — executes SQL, calls `ISqlInterceptor` for validation, loads module schema, calls `IModulePrompt` for dynamic prompt injection |
| `FunctionCallLogger` | Logs every kernel function call made by the AI — useful for debugging LLM reasoning |
| `IdentityService` | Implements `IIdentityService`. Loads roles and settings per WhatsApp number from `appsettings.json` (JSON or SQL source) |
| `SchemaService` | Loads the database schema from the schema definition file configured in `appsettings.json` |
| `ServiceCollectionExtension` | `IServiceCollection` extension called by `Program.cs` to register core services and discover external plugins/extensions |
| `Settings` | Strongly-typed settings classes, populated from `appsettings.json` |
| `WhatsAppLogger` | General-purpose logger |
| `WhatsAppService` | Sends WhatsApp replies and parses incoming webhook payloads to extract the sender's number and message |
| `TestIdentityService` | Test-only identity service — useful for verifying security and context-awareness without affecting `appsettings.json` |
| `AdventureWorksTestHarness` | Integration test harness for SQL generation, function calling, security, and identity — runs independently of the webhook host |

---

### VectorDBSync

A standalone utility project for syncing data to a vector database for fuzzy search. Not part of the main request pipeline — run separately on a schedule.

Currently supports **ChromaDB**.

| Class | Description |
|-------|-------------|
| `Program` | Entry point — loads `VectorConfig.json` and starts the sync service |
| `DynamicVectorSyncService` | Handles sync (batches of 100 records) and search against the vector database |

#### VectorConfig.json

Controls what data is synced without requiring code changes.

```json
{
  "Collections": [
    {
      "CollectionName": "persons",
      "SyncSql": "SELECT BusinessEntityID AS ID, FirstName + ' ' + LastName AS Content FROM Person.Person",
      "MetadataFields": ["BusinessEntityID", "PersonType"]
    }
  ]
}
```

| Field | Description |
|-------|-------------|
| `CollectionName` | Name of the ChromaDB collection |
| `SyncSql` | SQL to fetch records. Must return `ID` and `Content` columns |
| `MetadataFields` | Optional extra columns from `SyncSql` to store alongside the content |

#### Sync Tracker Table

The sync service tracks the last sync time per collection to process only new records on subsequent runs. Create this table in your database before running VectorDBSync:

```sql
CREATE TABLE [dbo].[Vector_SyncTracker] (
    CollectionName  nvarchar(100) NOT NULL PRIMARY KEY,
    LastSyncTime    datetime      NULL
)
```

---

### DBSearchHelperPlugin

A Semantic Kernel plugin that provides fuzzy/vector search capabilities to the AI agent. Registered as a plugin in the main `WhatsAppToDB` project.

| Class | Description |
|-------|-------------|
| `AWSearchHelperPlugin` | Fuzzy name search for AdventureWorks — resolves variations like "Linda Mitchele" → "Linda Mitchell" via ChromaDB |
| `IdentityPersonaPlugin` | Resolves first-person references ("my", "I", "me") in the user's question by injecting the active user's identity context before the SQL is generated |

---

### SecurityPlugin

A sample extension project demonstrating how to implement `IModulePrompt` and `ISqlInterceptor` from `WhatsAppToDB.Abstractions`.

Use this as a reference when building your own module-level security or SQL audit logic. Register it in `appsettings.json` under Extension Settings.

---

## Configuration

### appsettings.json

```json
{
  "WhatsApp": {
    "Token": "",
    "BusinessId": "",
    "PhoneId": "",
    "VerifyToken": ""
  },
  "OpenAi": {
    "Token": "",
    "Model": "gpt-4o",
    "PromptFile": "prompt.txt"
  },
  "Database": {
    "ConnectionString": "",
    "SchemaDefinitionFile": "schema.json",
    "ChromaURL": ""
  },
  "PluginSettings": [
    {
      "AssemblyPath": "Plugins/DBSearchHelperPlugin.dll"
    }
  ],
  "ExtensionSettings": [
    {
      "AssemblyPath": "Extensions/SecurityPlugin.dll"
    }
  ],
  "RoleSettings": [
    {
      "WhatsAppNumber": "919876543210",
      "Role": "SalesPerson",
      "AllowedModules": ["Sales", "Person"],
      "ContextKey": "SalesPersonID",
      "ContextValue": "277",
      "ConnectionString": ""
    }
  ]
}
```

| Section | Description |
|---------|-------------|
| `WhatsApp` | WhatsApp Business API credentials and webhook verify token |
| `OpenAi` | API token, model name, and path to the system prompt text file |
| `Database` | SQL Server connection string, path to schema definition file, and optional ChromaDB URL |
| `PluginSettings` | Paths to external plugin DLLs that register additional Semantic Kernel functions |
| `ExtensionSettings` | Paths to extension DLLs implementing `IModulePrompt` and/or `ISqlInterceptor` |
| `RoleSettings` | Per-WhatsApp-number role, allowed modules, row-level security context, and optional override connection string |

### Schema Definition File

The schema file defines which tables and columns are available per module. The LLM is constrained to only use what is defined here.

Modules can include optional **Security Constraints** that are injected into the prompt automatically — the LLM must apply them to every query for that module.

Both the schema file and the prompt text file can be swapped out per deployment without touching application code, making the system database-agnostic.

### VectorConfig.json

See [VectorDBSync — VectorConfig.json](#vectorconfigjson) above.

---

## Key Features

**Schema-controlled SQL generation** — the LLM only sees the tables and columns you explicitly define. No hallucinated table names.

**Module-based schema loading** — schema is fetched on demand per relevant module, keeping token usage low.

**Role-based access control** — each WhatsApp number is assigned a role with a permitted list of modules. A salesperson cannot query HR data.

**Context-aware queries** — WhatsApp numbers map to a `ContextKey` and `ContextValue` so personal queries ("what are my sales this month?") resolve correctly without the user identifying themselves.

**Row-level security integration** — `SESSION_CONTEXT` is set on the SQL Server connection before every query, so native RLS policies are enforced automatically.

**Dynamic prompt injection** — `IModulePrompt` allows external plugins to inject additional constraints per module at query time (e.g. silently append `SalesPersonID = 277` to all Sales queries).

**SQL validation before execution** — `ISqlInterceptor` lets external code inspect, modify, or reject the generated SQL before it runs.

**Fuzzy search via ChromaDB** — optional vector search resolves name typos and variations before querying SQL.

**Plugin architecture** — external DLLs can register new Semantic Kernel functions. Drop in a DLL and configure the path — no changes to the core application.

**Per-user connection strings** — additional database-level isolation by connecting as a different SQL user or to a different database per WhatsApp number.

**Multilingual responses** — the agent detects the language of the user's message and responds in the same language.

**External configuration** — prompt text and schema definitions live in files outside the application binary, making it straightforward to adapt to a new database.

---

## Getting Started

1. Clone the repository and restore NuGet packages.
2. Configure `appsettings.json` with your WhatsApp API credentials, OpenAI token, and SQL Server connection string.
3. Provide a schema definition file and point `Database.SchemaDefinitionFile` to it.
4. Set up role mappings in `RoleSettings` for each WhatsApp number that should have access.
5. (Optional) Deploy ChromaDB and set `Database.ChromaURL`. Run `VectorDBSync` to populate the vector collections.
6. (Optional) Build and register external plugins or extensions in `PluginSettings` / `ExtensionSettings`.
7. Run the `WhatsAppToDB` web host and point your WhatsApp webhook to it.

To test without touching `appsettings.json`, use `AdventureWorksTestHarness` with `TestIdentityService` to simulate different user roles and validate SQL generation, security constraints, and context resolution.
