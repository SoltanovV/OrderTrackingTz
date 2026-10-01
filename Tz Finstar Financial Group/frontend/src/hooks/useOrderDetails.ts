import { useEffect, useState } from 'react';
import { api } from '../api/orders';
import { ApiError } from '../api/client';
import { errorMessage } from '../utils/error';
import { useLive } from '../store/live';
import type { Order, OrderStatus } from '../types/order';

export function useOrderDetails(id: string) {
  const [order, setOrder] = useState<Order | null>(null);
  const [error, setError] = useState('');
  const [actionError, setActionError] = useState('');
  const [busy, setBusy] = useState(false);
  const [confirmCancel, setConfirmCancel] = useState(false);
  const revision = useLive((s) => s.revision);
  useEffect(() => {
    setOrder(null);
    setActionError('');
    setConfirmCancel(false);
  }, [id]);
  useEffect(() => {
    const controller = new AbortController();
    setError('');
    api
      .get(id, controller.signal)
      .then((next) => {
        if (!controller.signal.aborted)
          setOrder((current) =>
            !current || current.id !== next.id || next.version >= current.version ? next : current,
          );
      })
      .catch((err) => {
        if (!controller.signal.aborted) setError(errorMessage(err));
      });
    return () => controller.abort();
  }, [id, revision]);
  async function update(status: OrderStatus) {
    if (!order || busy) return;
    setBusy(true);
    setActionError('');
    try {
      const next = await api.changeStatus(order, status);
      setOrder((current) => (!current || next.version >= current.version ? next : current));
      setConfirmCancel(false);
      useLive.getState().refresh();
    } catch (err) {
      setActionError(errorMessage(err));
      if (err instanceof ApiError && err.status === 409) useLive.getState().refresh();
    } finally {
      setBusy(false);
    }
  }
  return { order, error, actionError, busy, confirmCancel, setConfirmCancel, update };
}
