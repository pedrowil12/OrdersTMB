import { useCallback, useEffect, useState } from "react";
import type { Session } from "../_lib/types";

const SESSION_KEY = "orders-tmb-session";

export function useSession() {
  const [session, setSession] = useState<Session | null>(null);
  const [restoring, setRestoring] = useState(true);

  useEffect(() => {
    const restore = window.setTimeout(() => {
      const stored = localStorage.getItem(SESSION_KEY);
      if (stored) {
        try {
          const parsed = JSON.parse(stored) as Session;
          if (new Date(parsed.expiresAtUtc) > new Date()) {
            setSession(parsed);
          } else {
            localStorage.removeItem(SESSION_KEY);
          }
        } catch {
          localStorage.removeItem(SESSION_KEY);
        }
      }
      setRestoring(false);
    }, 0);

    return () => window.clearTimeout(restore);
  }, []);

  const saveSession = useCallback((value: Session) => {
    localStorage.setItem(SESSION_KEY, JSON.stringify(value));
    setSession(value);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(SESSION_KEY);
    setSession(null);
  }, []);

  return { session, restoring, saveSession, logout };
}
