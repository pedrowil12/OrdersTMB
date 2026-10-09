import { useEffect, useState, type FormEvent } from "react";
import { api, getErrorMessage } from "../../_lib/api";
import type { Session, ToastState } from "../../_lib/types";
import { Toast } from "../ui/feedback";

export function Login({ onLogin }: { onLogin: (session: Session) => void }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [toast, setToast] = useState<ToastState | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!toast) return;
    const timeout = window.setTimeout(() => setToast(null), 4000);
    return () => window.clearTimeout(timeout);
  }, [toast]);

  async function submit(event: FormEvent) {
    event.preventDefault();
    setLoading(true);
    try {
      onLogin(await api.login(email, password));
    } catch (requestError) {
      setToast({ message: getErrorMessage(requestError), type: "error" });
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="login-shell">
      <form className="login-card" onSubmit={submit}>
        <div className="login-title">
          <div className="brand-mark">OT</div>
          <h1>OrdersTMB - Gestão de Pedidos</h1>
        </div>

        <label>
          E-mail
          <input
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            placeholder="voce@empresa.com"
            autoComplete="email"
            required
          />
        </label>
        <label>
          Senha
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            placeholder="••••••••"
            autoComplete="current-password"
            required
          />
        </label>

        <button className="primary-button" disabled={loading}>
          {loading ? "Entrando..." : "Entrar"}
        </button>
      </form>
      {toast && <Toast toast={toast} onClose={() => setToast(null)} />}
    </main>
  );
}
