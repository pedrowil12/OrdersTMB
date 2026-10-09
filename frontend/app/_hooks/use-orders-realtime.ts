import { useEffect, useRef, useState } from "react";
import {
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import { API_URL } from "../_lib/api";
import type { Order, OrderStatusChangedEvent } from "../_lib/types";

type RealtimeOptions = {
  token: string;
  orders: Order[];
  onStatusChanged: (event: OrderStatusChangedEvent) => void;
  onReconcile: () => void;
};

export function useOrdersRealtime({
  token,
  orders,
  onStatusChanged,
  onReconcile,
}: RealtimeOptions) {
  const [connected, setConnected] = useState(false);
  const orderIdsRef = useRef<Set<string>>(new Set());
  const statusHandlerRef = useRef(onStatusChanged);
  const reconcileRef = useRef(onReconcile);

  useEffect(() => {
    orderIdsRef.current = new Set(orders.map((order) => order.id));
  }, [orders]);

  useEffect(() => {
    statusHandlerRef.current = onStatusChanged;
  }, [onStatusChanged]);

  useEffect(() => {
    reconcileRef.current = onReconcile;
  }, [onReconcile]);

  useEffect(() => {
    let active = true;
    let retryTimer: number | undefined;
    let reconcileTimer: number | undefined;

    const connection = new HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/orders`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(LogLevel.Warning)
      .build();

    const applyStatus = (event: OrderStatusChangedEvent) => {
      if (!orderIdsRef.current.has(event.orderId)) {
        if (reconcileTimer === undefined) {
          reconcileTimer = window.setTimeout(() => {
            reconcileTimer = undefined;
            reconcileRef.current();
          }, 300);
        }
        return;
      }

      statusHandlerRef.current(event);
    };

    const scheduleRetry = () => {
      if (!active || retryTimer !== undefined) return;
      retryTimer = window.setTimeout(() => {
        retryTimer = undefined;
        void start();
      }, 5000);
    };

    const start = async () => {
      if (!active || connection.state !== HubConnectionState.Disconnected) return;

      try {
        await connection.start();
        if (active) setConnected(true);
      } catch {
        if (active) {
          setConnected(false);
          scheduleRetry();
        }
      }
    };

    connection.on("OrderStatusChanged", applyStatus);
    connection.onreconnecting(() => {
      if (active) setConnected(false);
    });
    connection.onreconnected(() => {
      if (!active) return;
      setConnected(true);
      reconcileRef.current();
    });
    connection.onclose(() => {
      if (!active) return;
      setConnected(false);
      scheduleRetry();
    });

    void start();

    return () => {
      active = false;
      if (retryTimer !== undefined) window.clearTimeout(retryTimer);
      if (reconcileTimer !== undefined) window.clearTimeout(reconcileTimer);
      connection.off("OrderStatusChanged", applyStatus);
      void connection.stop();
    };
  }, [token]);

  return connected;
}
