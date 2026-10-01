import { useEffect, useState } from 'react';
import { api } from '../api/orders';
import { errorMessage } from '../utils/error';
import { useLive } from '../store/live';
import { useWatchlist } from '../store/watchlist';
import type { OrderPage } from '../types/order';

export function useOrderList(watched: boolean) {
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<OrderPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const revision = useLive((s) => s.revision);
  const ids = useWatchlist((s) => s.ids);
  const trackedKey = watched ? ids.join(',') : '';
  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError('');
    if (watched && !trackedKey) {
      setResult({ items: [], total: 0, page: 1, pageSize: 10 });
      setLoading(false);
      return;
    }
    const timer = setTimeout(async () => {
      const params = new URLSearchParams({ page: String(page), pageSize: '10' });
      if (search.trim()) params.set('search', search.trim());
      if (status) params.set('status', status);
      if (watched) trackedKey.split(',').forEach((id) => params.append('ids', id));
      try {
        const next = await api.list(params, controller.signal);
        if (!controller.signal.aborted) {
          if (page > 1 && next.items.length === 0) setPage(Math.max(1, Math.ceil(next.total / 10)));
          else setResult(next);
        }
      } catch (err) {
        if (!controller.signal.aborted) setError(errorMessage(err));
      } finally {
        if (!controller.signal.aborted) setLoading(false);
      }
    }, 200);
    return () => {
      clearTimeout(timer);
      controller.abort();
    };
  }, [search, status, page, revision, watched, trackedKey]);
  const totalPages = Math.max(1, Math.ceil((result?.total ?? 0) / 10));
  return {
    search,
    setSearch,
    status,
    setStatus,
    page,
    setPage,
    result,
    loading,
    error,
    totalPages,
  };
}
