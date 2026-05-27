export const BASE_URL = 'http://localhost:3000'

export interface EmailPayload {
  messageId: number;
  from: string;
  to: string;
  cc: string;
  subject: string;
  body: string;
  chartImage?: string;
}

function authHeader(extraHeaders: Record<string, string> = {}) {
  const token = localStorage.getItem("token") || "";
  return {
    "Content-Type": "application/json",
    "Authorization": `Bearer ${token}`,
    ...extraHeaders
  };
}

// ================================================
// Existing Authentication and Admin API Methods
// ================================================
export async function login(username: string, password: string) {
  const res = await fetch(`${BASE_URL}/login`, {
    method: 'POST',
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ Username: username, Password: password })
  })
  return await res.json()
}

export async function ask(token: string, question: string) {
  const res = await fetch(`${BASE_URL}/ask`, {
    method: 'POST',
    headers: authHeader(),
    credentials: 'include',
    body: JSON.stringify({ Question: question })
  })
  return await res.text()
}

export async function getSessions() {
  const res = await fetch(`${BASE_URL}/session`, {
    method: "GET",
    credentials: 'include',
    headers: authHeader()
  });
  return await res.json();
}

export async function getMessages(sessionId: number) {
  const res = await fetch(`${BASE_URL}/message/${sessionId}`, {
    method: "GET",
    credentials: 'include',
    headers: authHeader()
  });
  return await res.json();
}

export async function getMessagesDatabases(sessionId: number, databases: string[]) {
  const res = await fetch(`${BASE_URL}/message/filter/${sessionId}`, {
    method: "POST",
    credentials: 'include',
    headers: authHeader(),
    body: JSON.stringify({ databases })
  });
  return await res.json();
}

export async function getDatabases() {
  const res = await fetch(
    `${BASE_URL}/admin/api/data/databases`,
    {
      method: "GET",
      credentials: 'include',
      headers: authHeader(),
    }
  )
  return await res.json()
}

export async function getMetadata(entity: string) {
  console.log("Getting metadata for entity", entity, `${BASE_URL}/admin/api/metadata/${entity}`)
  const res = await fetch(`${BASE_URL}/admin/api/metadata/${entity}`, {
    method: 'GET',
    credentials: 'include',
    headers: authHeader()
  });
  const ret = await res.json();
  console.log("metadata for", entity, ret)
  return ret;
}

export async function saveMetadata(entity: string, data: any) {
  const res = await fetch(`${BASE_URL}/admin/api/metadata/${entity}`, {
    method: 'POST',
    credentials: 'include',
    headers: authHeader(),
    body: JSON.stringify(data)
  });
  return await res.json();
}


export async function getData(entity: string, database?: string) {
  let url = `${BASE_URL}/admin/api/data/${entity}`;
  if (database) url += `?database=${database}`;
  const res = await fetch(url, {
    method: 'GET',
    credentials: 'include',
    headers: authHeader()
  });
  return await res.json();
}

export async function saveData(entity: string, data: any, database?: string) {
  let url = `${BASE_URL}/admin/api/data/${entity}`;
  if (database) url += `?database=${database}`;
  const res = await fetch(url, {
    method: 'POST',
    credentials: 'include',
    headers: authHeader(),
    body: JSON.stringify(data)
  });
  return await res.json();
}

export async function getActions(database: string, entity: string) {
  const url = `${BASE_URL}/admin/actions/${database}/${entity}`;
  const res = await fetch(url, {
    method: 'GET',
    credentials: 'include',
    headers: authHeader()
  });
  if (!res.ok) {
    throw new Error('Failed to fetch actions');
  }
  return await res.json();
}

export async function executeAction(actionUrl: string, method: string) {
  const url = actionUrl.startsWith('http') ? actionUrl : `${BASE_URL}/${actionUrl.replace(/^\//, '')}`;
  const res = await fetch(url, {
    method: method || 'GET',
    credentials: 'include',
    headers: authHeader()
  });
  if (!res.ok) {
    throw new Error('Action failed');
  }
  // Try to parse json, if empty or non-json return text
  const text = await res.text();
  try {
    return JSON.parse(text);
  } catch (e) {
    return text;
  }
}

// ================================================
// New Chat, Sidebar, Bookmark & Configuration APIs
// ================================================

export async function logoutSession() {
  return await fetch(`${BASE_URL}/logout`, {
    method: "POST",
    credentials: "include",
    headers: authHeader()
  });
}

export async function logout() {
  await fetch(`${BASE_URL}/logout`, {
    method: "POST",
    credentials: "include",
    headers: authHeader()
  });
  localStorage.removeItem("token");
  localStorage.removeItem("username");
  location.reload();
}

export async function searchChats(searchText: string) {
  const res = await fetch(
    `${BASE_URL}/search?text=${encodeURIComponent(searchText)}`,
    {
      credentials: "include",
      headers: authHeader()
    }
  );
  return await res.json();
}

export async function getBookmarks() {
  const res = await fetch(`${BASE_URL}/bookmarks`, {
    credentials: "include",
    headers: authHeader()
  });
  return await res.json();
}

export async function addBookmark(messageId: number, text: string) {
  return await fetch(
    `${BASE_URL}/addbookmark/${messageId}?text=${encodeURIComponent(text)}`,
    {
      credentials: "include",
      headers: authHeader()
    }
  );
}

export async function removeBookmark(messageId: number) {
  return await fetch(`${BASE_URL}/removebookmark/${messageId}`, {
    credentials: "include",
    headers: authHeader()
  });
}

export async function askQuestion(question: string, sessionId: number | null) {
  const res = await fetch(`${BASE_URL}/ask`, {
    method: "POST",
    credentials: "include",
    headers: authHeader(),
    body: JSON.stringify({
      SessionId: sessionId,
      Question: question
    })
  });
  return await res.text();
}

export async function getMessageSql(messageId: number) {
  const res = await fetch(`${BASE_URL}/messagesql/${messageId}`, {
    credentials: "include",
    headers: authHeader()
  });
  return await res.json();
}

export async function getMessageData(messageId: number) {
  const res = await fetch(`${BASE_URL}/messagedata/${messageId}`, {
    credentials: "include",
    headers: authHeader()
  });
  return await res.json();
}

export async function sendEmailResult(payload: EmailPayload) {
  const res = await fetch(`${BASE_URL}/emailresult`, {
    method: "POST",
    credentials: "include",
    headers: authHeader(),
    body: JSON.stringify({
      messageId: payload.messageId,
      from: payload.from,
      to: payload.to,
      cc: payload.cc,
      subject: payload.subject,
      body: payload.body,
      chartImage: payload.chartImage
    })
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    throw new Error(errorData.message || "Unable to send email.");
  }
  return await res.json();
}

export async function getDatabasesList() {
  const res = await fetch(`${BASE_URL}/databases`, {
    credentials: "include",
    headers: authHeader()
  });
  return await res.json();
}

export async function selectActiveDatabase(dbName: string) {
  const res = await fetch(`${BASE_URL}/databases/select`, {
    method: "POST",
    credentials: "include",
    headers: authHeader(),
    body: JSON.stringify(dbName)
  });
  return res;
}

export async function getLlmsList() {
  const res = await fetch(`${BASE_URL}/llms`, {
    credentials: "include",
    headers: authHeader()
  });
  return await res.json();
}

export async function selectActiveLlm(provider: string, model: string) {
  const res = await fetch(`${BASE_URL}/llms/select`, {
    method: "POST",
    credentials: "include",
    headers: authHeader(),
    body: JSON.stringify({ provider, model })
  });
  return res;
}

export async function filterSessions(databases: string[]) {
  const res = await fetch(`${BASE_URL}/sessions/filter`, {
    method: "POST",
    credentials: "include",
    headers: authHeader(),
    body: JSON.stringify({ databases })
  });
  return await res.json();
}

export async function exportExcelFile(messageId: number) {
  const token = localStorage.getItem("token") || "";
  const res = await fetch(`${BASE_URL}/exportdata/${messageId}`, {
    credentials: "include",
    headers: {
      "Authorization": `Bearer ${token}`
    }
  });
  if (!res.ok) {
    throw new Error("Unable to export Excel");
  }
  return await res.blob();
}

export async function compileMetadataString(targetEngine: string, parameters: Record<string, string>) {
  const res = await fetch(`${BASE_URL}/admin/api/compile-metadata-string`, {
    method: 'POST',
    credentials: 'include',
    headers: authHeader(),
    body: JSON.stringify({ targetEngine, parameters })
  });
  if (!res.ok) {
    const errorData = await res.json().catch(() => ({}));
    throw new Error(errorData.message || "Failed to compile connection string.");
  }
  return await res.json();
}

