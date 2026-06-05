import { BASE_URL } from "./api";

function authHeader(extraHeaders: Record<string, string> = {}) {
  const token = localStorage.getItem("token") || "";
  return {
    "Content-Type": "application/json",
    "Authorization": `Bearer ${token}`,
    ...extraHeaders
  };
}

export interface UserInstruction {
  Id?: any;
  id?: any;
  InstructionText?: string;
  instructionText?: string;
  Database?: string;
  database?: string;
}

export async function getUserInstructions(database: string): Promise<UserInstruction[]> {
  const res = await fetch(`${BASE_URL}/api/user-instructions/database/${encodeURIComponent(database)}`, {
    method: "GET",
    credentials: "include",
    headers: authHeader()
  });
  if (!res.ok) {
    throw new Error(`Failed to fetch instructions: ${res.statusText}`);
  }
  return await res.json();
}

export async function deleteUserInstruction(id: any): Promise<void> {
  const res = await fetch(`${BASE_URL}/api/user-instructions/${id}`, {
    method: "DELETE",
    credentials: "include",
    headers: authHeader()
  });
  if (!res.ok) {
    throw new Error(`Failed to delete instruction: ${res.statusText}`);
  }
}

export async function updateUserInstruction(payload: { Id: any; InstructionText: string; Database: string }): Promise<any> {
  const res = await fetch(`${BASE_URL}/api/user-instructions/`, {
    method: "PUT",
    credentials: "include",
    headers: authHeader(),
    body: JSON.stringify({
      Id: payload.Id,
      InstructionText: payload.InstructionText,
      Database: payload.Database
    })
  });
  if (!res.ok) {
    throw new Error(`Failed to update instruction: ${res.statusText}`);
  }
  return await res.json();
}

export async function addUserInstruction(payload: { InstructionText: string; Database: string }): Promise<any> {
  const res = await fetch(`${BASE_URL}/api/user-instructions/`, {
    method: "POST",
    credentials: "include",
    headers: authHeader(),
    body: JSON.stringify({
      InstructionText: payload.InstructionText,
      Database: payload.Database
    })
  });
  if (!res.ok) {
    throw new Error(`Failed to add instruction: ${res.statusText}`);
  }
  return await res.json();
}

export async function clearUserInstructions(database: string): Promise<any> {
  const res = await fetch(`${BASE_URL}/api/user-instructions/clear?Database=${encodeURIComponent(database)}`, {
    method: "GET",
    credentials: "include",
    headers: authHeader()
  });
  if (!res.ok) {
    throw new Error(`Failed to clear instructions: ${res.statusText}`);
  }
  return await res.json();
}
