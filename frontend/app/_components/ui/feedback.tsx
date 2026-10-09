import type { Order, ToastState } from "../../_lib/types";

export function StatusBadge({ status }: { status: Order["status"] }) {
  return (
    <span className={`status ${status.toLowerCase()}`}>
      <i />
      {status}
    </span>
  );
}

export function Toast({ toast, onClose }: { toast: ToastState; onClose: () => void }) {
  return (
    <div className={`toast ${toast.type}`} role={toast.type === "error" ? "alert" : "status"}>
      <span>{toast.message}</span>
      <button onClick={onClose} aria-label="Fechar mensagem">×</button>
    </div>
  );
}

export function Empty({ text }: { text: string }) {
  return (
    <div className="empty">
      <span>○</span>
      <p>{text}</p>
    </div>
  );
}
