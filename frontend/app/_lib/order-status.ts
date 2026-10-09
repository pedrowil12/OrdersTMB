import type { Order, OrderStatusChangedEvent } from "./types";

const statusOrder: Record<Order["status"], number> = {
  Pendente: 0,
  Processando: 1,
  Finalizado: 2,
};

export function applyStatusEvent(order: Order, event: OrderStatusChangedEvent) {
  if (statusOrder[event.status] < statusOrder[order.status]) {
    return order;
  }

  const alreadyRegistered = order.history.some((item) => item.status === event.status);
  return {
    ...order,
    status: event.status,
    history: alreadyRegistered
      ? order.history
      : [
          ...order.history,
          {
            status: event.status,
            previousStatus: order.status === event.status ? null : order.status,
            changedAtUtc: event.occurredAtUtc,
          },
        ],
  };
}
