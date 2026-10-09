export type Role = "admin" | "customer";

export type Session = {
  token: string;
  expiresAtUtc: string;
  user: {
    id: string;
    name: string;
    email: string;
    role: Role;
  };
};

export type Product = {
  id: string;
  name: string;
  price: number;
  active: boolean;
};

export type Customer = {
  id: string;
  name: string;
  phone: string;
  email: string;
  active: boolean;
  createdAtUtc: string;
};

export type OrderItem = {
  productId: string;
  product: string;
  quantity: number;
  unitPrice: number;
  total: number;
};

export type OrderHistory = {
  status: "Pendente" | "Processando" | "Finalizado";
  previousStatus: "Pendente" | "Processando" | "Finalizado" | null;
  changedAtUtc: string;
};

export type Order = {
  id: string;
  customer: {
    id: string;
    name: string;
  };
  products: OrderItem[];
  value: number;
  status: "Pendente" | "Processando" | "Finalizado";
  createdAtUtc: string;
  history: OrderHistory[];
};

export type OrderStatusChangedEvent = {
  orderId: string;
  customerId: string;
  correlationId: string;
  eventType: "OrderStatusChanged";
  status: Order["status"];
  occurredAtUtc: string;
};

export type ToastState = {
  message: string;
  type: "success" | "error";
};

export type ChatMessage = {
  role: "user" | "assistant";
  content: string;
};
