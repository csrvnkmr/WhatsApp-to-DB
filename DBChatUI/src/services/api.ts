const BASE_URL = 'http://localhost:3000'

export async function login(username: string, password: string) {
    const res = await fetch(`${BASE_URL}/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Username: username, Password: password })
    })
    return await res.json()
}

export async function ask(token: string, question: string) {
    const res = await fetch(`${BASE_URL}/ask`, {
        method: 'POST',
        headers: authHeader(),
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
        headers: authHeader()
    });

    return await res.json();
}

export async function getMessages(sessionId: number) {
    const res = await fetch(`${BASE_URL}/message/${sessionId}`, {
        method: "GET",
        headers: authHeader()
    });

    return await res.json();
}