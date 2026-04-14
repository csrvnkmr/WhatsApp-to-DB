# WhatsApp-to-DB

A secure, cross-platform AI-to-Database gateway bridging WhatsApp (via Semantic Kernel) to SQL databases (SAP Business One, AdventureWorks).

This system utilizes Retrieval-Augmented Generation (RAG) principles to handle complex enterprise data with row-level security and high-accuracy fuzzy searching.

**🚀 Key Features**

Semantic Kernel Orchestration: Manages the flow between user intent and plugin execution.

Vector DB Integration (ChromaDB): Performed via a dedicated VectorDBSync utility to handle fuzzy searches for Customers and Products.

Pluggable Security: A decoupled Abstractions layer for mandatory row-level security (e.g., SalesPersonID filtering).

Multilingual Support: Handles queries in English, Hindi, and Gujarati.

SQL Interception: A final guardrail that validates generated T-SQL before execution.

🛠 How It Works
1. Vector DB Sync (VectorDBSync)
Before the chat begins, we must "teach" the AI about our specific data (names that are hard to spell or have multiple variations).

Process: Extracts Customer and Product names/IDs from the SQL Database.

Storage: Generates embeddings and stores them in ChromaDB.

Benefit: Enables the AI to find CustomerID: 354 even if the user types "Serius Cycle" instead of "Serious Cycle".

2. The Plugin System
The AI uses specialized tools to gather context before writing SQL:

DBSearchHelperPlugin: Queries ChromaDB. When the AI sees a name, it calls this plugin first to get the exact primary key.

DatabaseQueryPlugin: The core engine that loads the module schema and executes the final validated SQL.

3. Execution Flow
WhatsApp Message: User asks: "Total sales for Serious Cycle"

Context Injection: System injects WhatsAppNumber into Kernel.Data.

Fuzzy Search: AI calls SearchHelper -> ChromaDB returns ID 354.

Schema & Security: AI calls GetSchemaForModule. The system appends a Mandatory Filter (e.g., WHERE SalesPersonID = 277).

SQL Interception: The ISqlInterceptor verifies the WHERE clause is present.

Results: Dapper executes the query and returns JSON to the AI for a natural language response.

⚙️ Setup & Configuration
Schema Prompts: Found in the /prompts folder. These map your database views to natural language descriptions.

appsettings.json:

Provide your OpenAI and ChromaDB endpoints.

Specify the paths to your Schema and Prompt files for optimal SQL generation.

Security DLL: Implement the IModulePrompt interface in a separate DLL to enforce your specific business rules.
