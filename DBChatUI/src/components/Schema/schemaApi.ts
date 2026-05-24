const BASE_URL = 'http://localhost:3000'

function authHeader(extraHeaders: Record<string, string> = {}) {
  const token = localStorage.getItem("token") || "";
  return {
    "Content-Type": "application/json",
    "Authorization": `Bearer ${token}`,
    ...extraHeaders
  };
}

export interface CuratedTable {
  Name: string;
  Description: string;
  Columns: string[];
}

export interface CuratedTableJoin {
  Description: string;
  JoinConditions: string[];
}

export async function getDatabaseTables(database: string): Promise<string[]> {
  const res = await fetch(`${BASE_URL}/api/schema/${database}/tables`, {
    method: 'GET',
    credentials: 'include',
    headers: authHeader()
  });
  if (!res.ok) {
    throw new Error(await res.text() || 'Failed to fetch database tables');
  }
  return await res.json();
}

export async function getTableColumns(database: string, table: string): Promise<string[]> {
  const res = await fetch(`${BASE_URL}/api/schema/${database}/columns/${table}`, {
    method: 'GET',
    credentials: 'include',
    headers: authHeader()
  });
  if (!res.ok) {
    throw new Error(await res.text() || `Failed to fetch columns for table ${table}`);
  }
  return await res.json();
}

export async function getTableForeignKeys(database: string, table: string): Promise<Record<string, Record<string, string>>> {
  const res = await fetch(`${BASE_URL}/api/schema/${database}/foreignkeys/${table}`, {
    method: 'GET',
    credentials: 'include',
    headers: authHeader()
  });
  if (!res.ok) {
    throw new Error(await res.text() || `Failed to fetch foreign keys for table ${table}`);
  }
  return await res.json();
}

export async function getCuratedTables(database: string): Promise<CuratedTable[]> {
  const res = await fetch(`${BASE_URL}/admin/api/data/tables?database=${database}`, {
    method: 'GET',
    credentials: 'include',
    headers: authHeader()
  });
  if (!res.ok) {
    throw new Error(await res.text() || 'Failed to fetch curated tables');
  }
  const data = await res.json();
  return Array.isArray(data) ? data : [];
}

export async function saveCuratedTables(database: string, data: CuratedTable[]): Promise<void> {
  const res = await fetch(`${BASE_URL}/admin/api/data/tables?database=${database}`, {
    method: 'POST',
    credentials: 'include',
    headers: authHeader(),
    body: JSON.stringify(data)
  });
  if (!res.ok) {
    throw new Error(await res.text() || 'Failed to save curated tables');
  }
}

export async function getCuratedTableJoins(database: string): Promise<CuratedTableJoin[]> {
  const res = await fetch(`${BASE_URL}/admin/api/data/tablejoins?database=${database}`, {
    method: 'GET',
    credentials: 'include',
    headers: authHeader()
  });
  if (!res.ok) {
    throw new Error(await res.text() || 'Failed to fetch curated table joins');
  }
  const data = await res.json();
  return Array.isArray(data) ? data : [];
}

export async function saveCuratedTableJoins(database: string, data: CuratedTableJoin[]): Promise<void> {
  const res = await fetch(`${BASE_URL}/admin/api/data/tablejoins?database=${database}`, {
    method: 'POST',
    credentials: 'include',
    headers: authHeader(),
    body: JSON.stringify(data)
  });
  if (!res.ok) {
    throw new Error(await res.text() || 'Failed to save curated table joins');
  }
}
