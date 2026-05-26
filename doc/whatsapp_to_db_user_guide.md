# InsightChat (DbChatUI) - User & Administration Guide

Welcome to the **InsightChat (DbChatUI)** application. InsightChat is a powerful, web-based database management and analytics platform. It provides a natural language interface that allows users to query databases in plain English and automatically receive answers, SQL queries, tabular datasets, interactive charts, and email sharing options. It also comes equipped with a comprehensive **Admin Configuration Dashboard** to manage LLM providers, database connection metadata, user permissions, and database schemas.

---

## Table of Contents
1. [Chat Console Features](#1-chat-console-features)
   - [Interactive Chat Area](#interactive-chat-area)
   - [Rich Response Actions](#rich-response-actions)
   - [Sidebar Navigation & Search](#sidebar-navigation-search)
2. [Administration Dashboard](#2-administration-dashboard)
   - [Global Configurations](#global-configurations)
   - [Database-Specific Settings](#database-specific-settings)
   - [Dynamic Metadata Editor](#dynamic-metadata-editor)
3. [Schema Curator Tool](#3-schema-curator-tool)
   - [Table & Column Exposure](#table-column-exposure)
   - [Relationship Discovery & Joins Builder](#relationship-discovery--joins-builder)
4. [Keyboard Shortcuts & UI Customization](#4-keyboard-shortcuts--ui-customization)

---

## 1. Chat Console Features

The main interface is the **Chat Console**, designed to make database querying intuitive. 

![InsightChat Main UI](https://images.unsplash.com/photo-1551288049-bebda4e38f71?auto=format&fit=crop&w=800&q=80) *Placeholder depicting chat & analytics dashboards.*

### Interactive Chat Area
- **Ask Anything Input**: Type database queries in plain text (e.g., *"Show me total sales by country for 2025"*).
- **History Navigation**: While focused on the text input, press the **Up Arrow** ($\uparrow$) or **Down Arrow** ($\downarrow$) on your keyboard to navigate through your previously sent questions.
- **Active Database Switcher**: Switch the database you are currently querying using the database selector dropdown in the header.
- **Database Multi-Select Filter**: Filter your workspace logs and chat history sessions by choosing one or more databases from the filter list.
- **LLM Model Switcher**: Select which Large Language Model (e.g., OpenAI, Anthropic Claude, Google Gemini) generates your SQL and summaries.

### Rich Response Actions
When the AI replies, it generates a text explanation along with several "Premium Actions" embedded at the bottom of the response bubble:

- **SQL Button**: Opens a modal displaying the exact SQL query generated and executed by the AI against your database.
- **Data Button**: Displays a preview of the raw tabular results returned from the database.
  - *Copy*: Copy the raw data directly to your clipboard.
  - *Download*: Download the data as a plain text file.
  - *Download Excel*: Download the records directly as a formatted Microsoft Excel (`.xlsx`) sheet.
- **Excel Button**: Instantly triggers a `.xlsx` spreadsheet download of the database result.
- **Chart Button**: If the response contains chart-compatible data, the application uses **Apache ECharts** to render an interactive visualization.
  - *Maximize / Restore*: Scale the chart to fill the window.
  - *Fullscreen*: Enter browser fullscreen mode for presentation.
  - *Download Image*: Save the chart as a high-resolution PNG.
  - *Open in New Tab*: Render the chart in an isolated, resizable browser window.
  - *Email*: Launch the email composer with the chart attached.
- **Email Button**: Opens a built-in email composer modal prefilled with details from the AI response. You can configure:
  - **From / To / CC**: Custom address fields.
  - **Subject / Body**: Text fields with markdown support.
  - **Embedded Visuals**: Automatically attaches the generated chart image if available.
- **Bookmark Button**: Pin specific insights. Click **Bookmark**, type a custom label, and save it.

### Sidebar Navigation & Search
- **New Chat**: Instantly wipe the current chat pane to begin a clean session.
- **Full-Text Session Search**: Search your active sessions by typing in the search bar. It matches session titles as well as specific questions asked inside those sessions.
- **Pinned Bookmarks View**: Click the **📌 Bookmarks** button to review a chronological feed of all bookmarked questions and answers. Clicking any bookmarked question immediately loads it back into a new chat.

---

## 2. Administration Dashboard

Click the **⚙️ Admin** button in the header of the chat interface to access the configuration dashboard. The admin panel uses a dynamic layout driven entirely by backend JSON metadata files, meaning form fields, validation, and requirements adapt automatically to the entity you edit.

### Global Configurations
These affect the entire application and are loaded from root configurations:
1. **Databases**: Add, update, or remove database connection metadata (SQLite, Microsoft SQL Server, PostgreSQL) and their connection strings. Sensitive fields (like passwords/credentials) are automatically encrypted.
2. **LLM Configs**: Register API keys, endpoints, and providers (OpenAI, Gemini, Custom).
3. **Users**: Manage credentials, roles, and connected WhatsApp numbers for team members.
4. **WhatsApp Profiles**: Setup profiles allowing WhatsApp bots to receive incoming queries and sync data back to the database.
5. **Default Settings / Folders**: Set fallback parameters such as the default active database and system folders.

### Database-Specific Settings
When configuring a specific database, click its name in the admin menu to manage specific behavioral parameters:
- **System Prompts**: Define custom instructions to tailor how the LLM behaves for this database.
- **Roles**: Manage access control roles and restrictions.
- **Plugins / Extensions**: Configure add-ons and external functions.
- **Table Joins & Few-Shot Queries**: Save specific relational join pathways and query examples to help train the LLM in writing correct SQL.

### Dynamic Metadata Editor
Admins can customize the config schemas themselves! Next to any section title, click **Edit Metadata** to alter field definitions (like changing a text input to a dropdown, adding a custom label, or making a field required/sensitive).

---

## 3. Schema Curator Tool

The **Schema Curator** is the core bridge between your raw database and the AI. It allows you to expose only relevant database structures to the AI and attach rich semantic descriptions.

### Table & Column Exposure
1. Select your database and click **Schema Curator** in the Admin panel.
2. **Table Visibility**: Check the box next to any table to expose it to the AI. You can write a description detailing the table's purpose.
3. **Column Visibility & Semantics**: Expand a table to view its columns.
   - Toggle individual columns on or off (e.g., hide sensitive audit fields like `password_hash`).
   - Define **AI Synonyms & Descriptions** (e.g., for a column named `cust_id`, you can add *"Customer account identifier, unique ID, client number"*). This helps the LLM recognize columns even when users use different phrasing.

### Relationship Discovery & Joins Builder
To generate multi-table queries, the AI needs to understand how tables connect.
- **Auto-discovered Relationships**: The curator inspects foreign keys in the database schema. Review suggestions in the right-hand panel and click **+ Add** to instantly promote them into active joins.
- **Custom Joins Builder**: Click **➕ Add Custom Join** to construct relationship pathways manually:
  - Define custom multi-column join relationships (e.g., `TableA.Col1 = TableB.Col1 AND TableA.Col2 = TableB.Col2`).
  - Configure join conditions comparing a column to a custom text value (e.g., `TableA.Status = 'Active'`).

---

## 4. Keyboard Shortcuts & UI Customization

- **History Lookup**: Press `Up Arrow` ($\uparrow$) in the chat field to retrieve your last question. Press `Down Arrow` ($\downarrow$) to navigate forward.
- **Cancel Forms**: Press `Escape` to close active modal popups, drop-down menus, or cancel adding items to a configuration array.
- **Themes**: Switch themes instantly in the header:
  - ☀️ **Light Mode**: High contrast, crisp layout for bright rooms.
  - 🌙 **Dark Mode**: Soft HSL color-tailored mode designed to reduce eye strain.
  - 🏢 **Corporate Mode**: A professional, neutral-toned theme tailored for business presentations.
