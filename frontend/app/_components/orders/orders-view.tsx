import { currency, dateTime } from "../../_lib/formatters";
import type { Order } from "../../_lib/types";
import { Empty, StatusBadge } from "../ui/feedback";

type OrdersViewProps = {
  orders: Order[];
  selected: Order | null;
  selectedHistory: Order | null;
  currentUserId: string;
  isAdmin: boolean;
  onCreate: () => void;
  onDetails: (order: Order) => void;
  onHistory: (order: Order) => void;
  onCloseDetails: () => void;
  onCloseHistory: () => void;
};

export function OrdersView({
  orders,
  selected,
  selectedHistory,
  currentUserId,
  isAdmin,
  onCreate,
  onDetails,
  onHistory,
  onCloseDetails,
  onCloseHistory,
}: OrdersViewProps) {
  return (
    <section className="panel">
      <div className="panel-heading">
        <div>
          <h2>Pedidos recentes</h2>
          <p>Os status são atualizados automaticamente.</p>
        </div>
        <button className="primary-button compact" onClick={onCreate}>Novo pedido</button>
      </div>

      {orders.length === 0 ? (
        <Empty text="Nenhum pedido foi criado ainda." />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Pedido</th>
                <th>Cliente</th>
                <th>Itens</th>
                <th>Valor</th>
                <th>Status</th>
                <th>Data</th>
                <th className="actions-heading">Ações</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((order) => {
                const isAdminOrder = isAdmin && order.customer.id === currentUserId;
                return (
                  <tr key={order.id}>
                    <td data-label="Pedido">
                      <strong>#{order.id.slice(0, 8).toUpperCase()}</strong>
                    </td>
                    <td data-label="Cliente">
                      <span className="customer-cell">
                        {order.customer.name}
                        {isAdminOrder && <small className="owner-label">Pedido do admin</small>}
                      </span>
                    </td>
                    <td data-label="Itens">
                      {order.products.reduce((sum, item) => sum + item.quantity, 0)}
                    </td>
                    <td data-label="Valor">{currency.format(order.value)}</td>
                    <td data-label="Status"><StatusBadge status={order.status} /></td>
                    <td data-label="Data">{dateTime.format(new Date(order.createdAtUtc))}</td>
                    <td data-label="Ações" className="actions-cell">
                      <div className="table-actions" role="group" aria-label={`Ações do pedido ${order.id}`}>
                        <button type="button" onClick={() => onDetails(order)}>Detalhes</button>
                        <button type="button" onClick={() => onHistory(order)}>Histórico</button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      {selected && (
        <OrderDetail
          order={selected}
          isAdminOrder={isAdmin && selected.customer.id === currentUserId}
          onClose={onCloseDetails}
        />
      )}

      {selectedHistory && (
        <OrderHistoryModal order={selectedHistory} onClose={onCloseHistory} />
      )}
    </section>
  );
}

function OrderDetail({
  order,
  isAdminOrder,
  onClose,
}: {
  order: Order;
  isAdminOrder: boolean;
  onClose: () => void;
}) {
  const steps = ["Pendente", "Processando", "Finalizado"] as const;
  const current = steps.indexOf(order.status);

  return (
    <div className="modal-backdrop" onMouseDown={onClose}>
      <article className="detail-card" onMouseDown={(event) => event.stopPropagation()}>
        <button className="close-button" onClick={onClose} aria-label="Fechar">×</button>
        <header className="detail-heading">
          <p className="eyebrow accent">Detalhes do pedido</p>
          <h2>#{order.id.slice(0, 8).toUpperCase()}</h2>
        </header>

        <div className="detail-meta">
          <div><span>Cliente</span><strong>{order.customer.name}</strong></div>
          <div><span>Criado em</span><strong>{dateTime.format(new Date(order.createdAtUtc))}</strong></div>
          {isAdminOrder && <span className="owner-label">Pedido do admin</span>}
        </div>

        <section className="detail-section">
          <h3>Andamento</h3>
          <div className="status-flow">
            {steps.map((step, index) => (
              <div key={step} className={index <= current ? "flow-step done" : "flow-step"}>
                <span>{index < current ? "✓" : index + 1}</span>
                <strong>{step}</strong>
              </div>
            ))}
          </div>
        </section>

        <section className="detail-section">
          <h3>Itens</h3>
          <div className="detail-items">
            {order.products.map((item) => (
              <div key={item.productId}>
                <span>
                  <strong>{item.product}</strong>
                  <small>{item.quantity} × {currency.format(item.unitPrice)}</small>
                </span>
                <strong>{currency.format(item.total)}</strong>
              </div>
            ))}
          </div>
        </section>
        <div className="detail-total">
          <span>Total</span>
          <strong>{currency.format(order.value)}</strong>
        </div>
      </article>
    </div>
  );
}

function OrderHistoryModal({ order, onClose }: { order: Order; onClose: () => void }) {
  return (
    <div className="modal-backdrop" onMouseDown={onClose}>
      <article className="detail-card history-card" onMouseDown={(event) => event.stopPropagation()}>
        <button className="close-button" onClick={onClose} aria-label="Fechar">×</button>
        <header className="detail-heading">
          <p className="eyebrow accent">Histórico do pedido</p>
          <h2>#{order.id.slice(0, 8).toUpperCase()}</h2>
        </header>

        <div className="detail-meta">
          <div><span>Cliente</span><strong>{order.customer.name}</strong></div>
          <div><span>Status atual</span><strong>{order.status}</strong></div>
        </div>

        <section className="detail-section">
          {order.history.length === 0 ? (
            <p className="history-empty">Nenhum histórico registrado para este pedido.</p>
          ) : (
            <div className="history-list">
              {order.history.map((item) => (
                <div key={`${item.status}-${item.changedAtUtc}`}>
                  <StatusBadge status={item.status} />
                  <span>
                    {item.previousStatus
                      ? `${item.previousStatus} → ${item.status}`
                      : "Estado registrado"}
                  </span>
                  <time>{dateTime.format(new Date(item.changedAtUtc))}</time>
                </div>
              ))}
            </div>
          )}
        </section>
      </article>
    </div>
  );
}
