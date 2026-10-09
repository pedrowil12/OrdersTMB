import { useCallback, useEffect, useState } from "react";
import { useOrdersRealtime } from "../_hooks/use-orders-realtime";
import { api, ApiError, getErrorMessage } from "../_lib/api";
import { applyStatusEvent } from "../_lib/order-status";
import type {
  Customer,
  Order,
  OrderStatusChangedEvent,
  Product,
  Session,
  ToastState,
} from "../_lib/types";
import { CustomerForm, CustomersView } from "./customers/customers";
import { ChatWidget } from "./chat/chat-widget";
import { NewOrder } from "./orders/new-order";
import { OrdersView } from "./orders/orders-view";
import { ProductForm, ProductsView } from "./products/products";
import { ConfirmDialog, FormModal } from "./ui/dialogs";
import { Toast } from "./ui/feedback";

type Tab = "orders" | "customers" | "products";
type Modal = "order" | "customer" | "product" | null;

export function Dashboard({ session, onLogout }: { session: Session; onLogout: () => void }) {
  const isAdmin = session.user.role === "admin";
  const [tab, setTab] = useState<Tab>("orders");
  const [modal, setModal] = useState<Modal>(null);
  const [confirmLogout, setConfirmLogout] = useState(false);
  const [orders, setOrders] = useState<Order[]>([]);
  const [products, setProducts] = useState<Product[]>([]);
  const [customers, setCustomers] = useState<Customer[]>([]);
  const [selectedOrder, setSelectedOrder] = useState<Order | null>(null);
  const [selectedHistoryOrder, setSelectedHistoryOrder] = useState<Order | null>(null);
  const [loading, setLoading] = useState(true);
  const [toast, setToast] = useState<ToastState | null>(null);

  const showToast = useCallback((message: string, type: ToastState["type"] = "success") => {
    setToast({ message, type });
  }, []);

  useEffect(() => {
    if (!toast) return;
    const timeout = window.setTimeout(() => setToast(null), 4000);
    return () => window.clearTimeout(timeout);
  }, [toast]);

  const handleError = useCallback((requestError: unknown) => {
    if (requestError instanceof ApiError && requestError.status === 401) {
      onLogout();
      return;
    }
    showToast(getErrorMessage(requestError), "error");
  }, [onLogout, showToast]);

  const loadOrders = useCallback(async () => {
    try {
      const nextOrders = await api.listOrders(session.token);
      setOrders(nextOrders);

      const updateSelected = (current: Order | null) => {
        if (!current) return null;
        const summary = nextOrders.find((order) => order.id === current.id);
        return summary ? { ...summary, history: current.history } : null;
      };

      setSelectedOrder(updateSelected);
      setSelectedHistoryOrder(updateSelected);
    } catch (requestError) {
      handleError(requestError);
    }
  }, [handleError, session.token]);

  const loadAll = useCallback(async () => {
    setLoading(true);
    try {
      const [nextOrders, nextProducts, nextCustomers] = await Promise.all([
        api.listOrders(session.token),
        api.listProducts(session.token),
        isAdmin ? api.listCustomers(session.token) : Promise.resolve([]),
      ]);
      setOrders(nextOrders);
      setProducts(nextProducts);
      setCustomers(nextCustomers);
    } catch (requestError) {
      handleError(requestError);
    } finally {
      setLoading(false);
    }
  }, [handleError, isAdmin, session.token]);

  useEffect(() => {
    const initialLoad = window.setTimeout(() => { void loadAll(); }, 0);
    return () => window.clearTimeout(initialLoad);
  }, [loadAll]);

  const applyRealtimeStatus = useCallback((event: OrderStatusChangedEvent) => {
    setOrders((current) => current.map((order) =>
      order.id === event.orderId ? applyStatusEvent(order, event) : order));
    setSelectedOrder((current) =>
      current?.id === event.orderId ? applyStatusEvent(current, event) : current);
    setSelectedHistoryOrder((current) =>
      current?.id === event.orderId ? applyStatusEvent(current, event) : current);
  }, []);

  useOrdersRealtime({
    token: session.token,
    orders,
    onStatusChanged: applyRealtimeStatus,
    onReconcile: () => { void loadOrders(); },
  });

  const openOrder = useCallback((order: Order) => {
    setSelectedOrder(order);
    void api.getOrder(session.token, order.id)
      .then((details) => {
        setSelectedOrder((current) => current?.id === details.id ? details : current);
      })
      .catch(handleError);
  }, [handleError, session.token]);

  const openOrderHistory = useCallback((order: Order) => {
    setSelectedHistoryOrder(order);
    void api.getOrder(session.token, order.id)
      .then((details) => {
        setSelectedHistoryOrder((current) => current?.id === details.id ? details : current);
      })
      .catch(handleError);
  }, [handleError, session.token]);

  const navItems: Array<{ id: Tab; label: string }> = isAdmin
    ? [
        { id: "orders", label: "Pedidos" },
        { id: "customers", label: "Clientes" },
        { id: "products", label: "Produtos" },
      ]
    : [{ id: "orders", label: "Meus pedidos" }];

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="brand-row">
          <div className="brand-mark small">OT</div>
          <strong>OrdersTMB</strong>
        </div>

        <nav className="top-nav" aria-label="Menu principal">
          {navItems.map((item) => (
            <button
              key={item.id}
              className={tab === item.id ? "nav-item active" : "nav-item"}
              onClick={() => setTab(item.id)}
            >
              {item.label}
            </button>
          ))}
        </nav>

        <div className="header-user">
          <div className="avatar">{session.user.name.slice(0, 2).toUpperCase()}</div>
          <span>
            <strong>{session.user.name}</strong>
            <small>{isAdmin ? "Administrador" : "Cliente"}</small>
          </span>
          <button className="logout-button" onClick={() => setConfirmLogout(true)}>Sair</button>
        </div>
      </header>

      <main className="content">
        <header className="topbar">
          <div>
            <p className="eyebrow">Central de pedidos</p>
            <h1>{navItems.find((item) => item.id === tab)?.label}</h1>
          </div>
        </header>

        {loading ? (
          <div className="content-loader"><div className="loader" /></div>
        ) : (
          <>
            {tab === "orders" && (
              <OrdersView
                orders={orders}
                selected={selectedOrder}
                selectedHistory={selectedHistoryOrder}
                currentUserId={session.user.id}
                isAdmin={isAdmin}
                onCreate={() => setModal("order")}
                onDetails={openOrder}
                onHistory={openOrderHistory}
                onCloseDetails={() => setSelectedOrder(null)}
                onCloseHistory={() => setSelectedHistoryOrder(null)}
              />
            )}
            {tab === "customers" && isAdmin && (
              <CustomersView customers={customers} onCreate={() => setModal("customer")} />
            )}
            {tab === "products" && isAdmin && (
              <ProductsView products={products} onCreate={() => setModal("product")} />
            )}
          </>
        )}
      </main>

      {modal === "order" && (
        <FormModal title="Novo pedido" onClose={() => setModal(null)}>
          <NewOrder
            products={products}
            token={session.token}
            onCreated={async () => {
              setModal(null);
              showToast("Pedido criado e enviado para processamento.");
              await loadAll();
            }}
            onError={handleError}
          />
        </FormModal>
      )}

      {modal === "customer" && isAdmin && (
        <FormModal title="Cadastrar cliente" onClose={() => setModal(null)}>
          <CustomerForm
            token={session.token}
            onCreated={async () => {
              setModal(null);
              showToast("Cliente cadastrado com sucesso.");
              await loadAll();
            }}
            onError={handleError}
          />
        </FormModal>
      )}

      {modal === "product" && isAdmin && (
        <FormModal title="Cadastrar produto" onClose={() => setModal(null)}>
          <ProductForm
            token={session.token}
            onCreated={async () => {
              setModal(null);
              showToast("Produto cadastrado com sucesso.");
              await loadAll();
            }}
            onError={handleError}
          />
        </FormModal>
      )}

      {confirmLogout && (
        <ConfirmDialog
          title="Sair do sistema?"
          message="Sua sessão será encerrada neste dispositivo."
          confirmLabel="Sair"
          onCancel={() => setConfirmLogout(false)}
          onConfirm={onLogout}
        />
      )}

      <ChatWidget token={session.token} onError={handleError} />
      {toast && <Toast toast={toast} onClose={() => setToast(null)} />}
    </div>
  );
}
