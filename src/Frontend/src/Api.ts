const USER_ID_KEY = "userId";

export function getUserId(): string {
  return localStorage.getItem(USER_ID_KEY) ?? "demo-user-001";
}

export function setUserId(id: string) {
  localStorage.setItem(USER_ID_KEY, id.trim());
}

export async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);

  headers.set("X-User-Id", getUserId());
  headers.set("Accept", "application/json");

  const res = await fetch(path, { ...init, headers });

  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(`HTTP ${res.status}: ${text || res.statusText}`);
  }

  const ct = res.headers.get("content-type") ?? "";
  if (!ct.includes("application/json")) return undefined as T;
  return (await res.json()) as T;
}
