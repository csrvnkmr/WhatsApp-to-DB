const BASE_URL = 'http://localhost:3000'

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

function authHeader() {
  const token = localStorage.getItem("token") || "";

  return {
    "Content-Type": "application/json",
    "Authorization": `Bearer ${token}`
  };
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
