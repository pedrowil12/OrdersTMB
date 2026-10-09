"use client";

import { Login } from "./_components/auth/login";
import { Dashboard } from "./_components/dashboard";
import { useSession } from "./_hooks/use-session";

export default function OrdersApp() {
  const { session, restoring, saveSession, logout } = useSession();

  if (restoring) {
    return (
      <div className="screen-center">
        <div className="loader" aria-label="Carregando" />
      </div>
    );
  }

  return session
    ? <Dashboard session={session} onLogout={logout} />
    : <Login onLogin={saveSession} />;
}
