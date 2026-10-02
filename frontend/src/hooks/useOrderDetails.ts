import { useEffect, useRef, useState } from 'react';
import { api } from '../api/orders';
import { ApiError } from '../api/client';
import { errorMessage } from '../utils/error';
import { useLive } from '../store/live';
import { useLiveQuery } from './useLiveQuery';
import type { Order, OrderStatus } from '../types/order';

export function useOrderDetails(id: string) {
  const [order, setOrder] = useState<Order | null>(null);
  const [error, setError] = useState('');
  const [actionError, setActionError] = useState('');
  const [busy, setBusy] = useState(false);
  const [confirmCancel, setConfirmCancel] = useState(false);
  const actionScope = useRef<object | null>(null);
  useEffect(() => {
    actionScope.current = {};
    setOrder(null);
    setError('');
    setActionError('');
    setBusy(false);
    setConfirmCancel(false);
    return () => {
      actionScope.current = null;
    };
  }, [id]);
  useLiveQuery(
    async (signal) => {
      setError('');
      try {
        const next = await api.get(id, signal);
        if (!signal.aborted)
          setOrder((current) =>
            !current || current.id !== next.id || next.version >= current.version ? next : current,
          );
      } catch (err) {
        if (!signal.aborted) setError(errorMessage(err));
      }
    },
    [id],
  );
  async function update(status: OrderStatus) {
    const scope = actionScope.current;
    if (!order || order.id !== id || busy || !scope) return;
    setBusy(true);
    setActionError('');
    try {
      const next = await api.changeStatus(order, status);
      if (actionScope.current === scope) {
        setOrder((current) =>
          current?.id === next.id && next.version >= current.version ? next : current,
        );
        setConfirmCancel(false);
      }
      useLive.getState().refresh();
    } catch (err) {
      if (actionScope.current === scope) setActionError(errorMessage(err));
      if (err instanceof ApiError && err.status === 409) useLive.getState().refresh();
    } finally {
      if (actionScope.current === scope) setBusy(false);
    }
  }
  return { order, error, actionError, busy, confirmCancel, setConfirmCancel, update };
}
