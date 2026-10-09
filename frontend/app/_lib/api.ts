import type { ChatMessage, Customer, Order, Product, Session } from "./types";

export const API_URL = (process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000").replace(/\/$/, "");

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
  }
}

export function getErrorMessage(error: unknown) {
  return error instanceof Error ? error.message : "Ocorreu um erro inesperado.";
}

async function request<T>(path: string, init: RequestInit = {}, token?: string): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    const validationMessage = body?.errors
      ? Object.values(body.errors).flat().join(" ")
      : null;
    throw new ApiError(
      body?.message ?? body?.detail ?? validationMessage ?? "Não foi possível concluir a operação.",
      response.status,
    );
  }

  return response.json() as Promise<T>;
}

function emitSseChunks(event: string, onChunk: (chunk: string) => void) {
  for (const line of event.split(/\r?\n/)) {
    if (!line.startsWith("data:")) continue;

    const data = line.slice(5).trimStart();
    if (!data) continue;

    const chunk: unknown = JSON.parse(data);
    if (typeof chunk === "string") onChunk(chunk);
  }
}

async function streamOrderAnalytics(
  token: string,
  history: ChatMessage[],
  onChunk: (chunk: string) => void,
  signal?: AbortSignal,
) {
  const response = await fetch(`${API_URL}/orders/analytics`, {
    method: "POST",
    headers: {
      Accept: "text/event-stream",
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ history }),
    signal,
  });

  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new ApiError(
      body?.message ?? body?.detail ?? "Não foi possível consultar o assistente.",
      response.status,
    );
  }

  if (!response.body) {
    throw new Error("O navegador não disponibilizou a resposta do assistente.");
  }

  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();
    buffer += decoder.decode(value, { stream: !done });

    const events = buffer.split(/\r?\n\r?\n/);
    buffer = events.pop() ?? "";
    events.forEach((event) => emitSseChunks(event, onChunk));

    if (done) break;
  }

  if (buffer.trim()) emitSseChunks(buffer, onChunk);
}

export const api = {
  login: (email: string, password: string) =>
    request<Session>("/auth/login", {
      method: "POST",
      body: JSON.stringify({ email, password }),
    }),

  listOrders: (token: string) => request<Order[]>("/orders", {}, token),

  getOrder: (token: string, id: string) => request<Order>(`/orders/${id}`, {}, token),

  createOrder: (token: string, items: Array<{ productId: string; quantity: number }>) =>
    request<Order>(
      "/orders",
      { method: "POST", body: JSON.stringify({ items }) },
      token,
    ),

  listProducts: (token: string) => request<Product[]>("/products", {}, token),

  createProduct: (token: string, name: string, price: number) =>
    request<Product>(
      "/products",
      { method: "POST", body: JSON.stringify({ name, price }) },
      token,
    ),

  listCustomers: (token: string) => request<Customer[]>("/customers", {}, token),

  createCustomer: (
    token: string,
    customer: { name: string; phone: string; email: string; password: string },
  ) =>
    request<Customer>(
      "/customers",
      { method: "POST", body: JSON.stringify(customer) },
      token,
    ),

  streamOrderAnalytics,
};
