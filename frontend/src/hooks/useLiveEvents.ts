import { useEffect } from 'react';
import { useLive } from '../store/live';

export function useLiveEvents() {
  useEffect(() => {
    const source = new EventSource('/api/events');
    let refreshTimer: ReturnType<typeof setTimeout> | undefined;
    const refresh = () => {
      if (refreshTimer !== undefined) return;
      refreshTimer = setTimeout(() => {
        refreshTimer = undefined;
        useLive.getState().refresh();
      }, 150);
    };
    const connection = (event: MessageEvent) => {
      const previous = useLive.getState().connection;
      try {
        const next = JSON.parse(event.data).brokerConnected ? 'live' : 'delayed';
        useLive.getState().setConnection(next);
        if (previous !== next || event.type === 'ready') refresh();
      } catch {
        useLive.getState().setConnection('offline');
      }
    };
    source.addEventListener('ready', connection);
    source.addEventListener('heartbeat', connection);
    // Fetch the authoritative state: duplicate or out-of-order events cannot roll back a status.
    source.addEventListener('order-changed', refresh);
    source.onerror = () => useLive.getState().setConnection('offline');
    const onVisible = () => {
      if (document.visibilityState === 'visible') refresh();
    };
    document.addEventListener('visibilitychange', onVisible);
    return () => {
      source.close();
      clearTimeout(refreshTimer);
      document.removeEventListener('visibilitychange', onVisible);
    };
  }, []);
}
