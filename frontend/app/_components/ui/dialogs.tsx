import type { ReactNode } from "react";

export function FormModal({
  title,
  children,
  onClose,
}: {
  title: string;
  children: ReactNode;
  onClose: () => void;
}) {
  return (
    <div className="modal-backdrop" onMouseDown={onClose}>
      <article className="form-modal" onMouseDown={(event) => event.stopPropagation()}>
        <button className="close-button" onClick={onClose} aria-label="Fechar">×</button>
        <p className="eyebrow accent">OrdersTMB</p>
        <h2>{title}</h2>
        {children}
      </article>
    </div>
  );
}

export function ConfirmDialog({
  title,
  message,
  confirmLabel,
  disabled = false,
  onCancel,
  onConfirm,
}: {
  title: string;
  message: string;
  confirmLabel: string;
  disabled?: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  return (
    <div className="confirm-backdrop" onMouseDown={disabled ? undefined : onCancel}>
      <article
        className="confirm-card"
        onMouseDown={(event) => event.stopPropagation()}
        role="alertdialog"
        aria-modal="true"
        aria-labelledby="confirm-title"
      >
        <h2 id="confirm-title">{title}</h2>
        <p>{message}</p>
        <div className="dialog-actions">
          <button className="secondary-button" onClick={onCancel} disabled={disabled}>Cancelar</button>
          <button className="primary-button" onClick={onConfirm} disabled={disabled}>{confirmLabel}</button>
        </div>
      </article>
    </div>
  );
}
