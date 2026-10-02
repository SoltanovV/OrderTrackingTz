import { useEffect, useState } from 'react';
import { api } from '../api/orders';
import { errorMessage } from '../utils/error';
import { useWatchlist } from '../store/watchlist';
import { useLiveQuery } from './useLiveQuery';
import type { OrderPage } from '../types/order';

export function useOrderList(watched: boolean) {
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [status, setStatus] = useState('');
  const [page, setPage] = useState(1);
  const [result, setResult] = useState<OrderPage | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const ids = useWatchlist((s) => s.ids);
  const trackedKey = watched ? ids.join(',') : '';
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(search), 200);
    return () => clearTimeout(timer);
  }, [search]);
  useLiveQuery(
    async (signal) => {
      setLoading(true);
      setError('');
      if (watched && !trackedKey) {
        setPage(1);
        setResult({ items: [], total: 0, page: 1, pageSize: 10 });
        setLoading(false);
        return;
      }
      const params = new URLSearchParams({ page: String(page), pageSize: '10' });
      if (debouncedSearch.trim()) params.set('search', debouncedSearch.trim());
      if (status) params.set('status', status);
      if (watched) trackedKey.split(',').forEach((id) => params.append('ids', id));
      try {
        const next = await api.list(params, signal);
        if (!signal.aborted) {
          if (page > 1 && next.items.length === 0) setPage(Math.max(1, Math.ceil(next.total / 10)));
          else setResult(next);
        }
      } catch (err) {
        if (!signal.aborted) setError(errorMessage(err));
      } finally {
        if (!signal.aborted) setLoading(false);
      }
    },
    [debouncedSearch, status, page, watched, trackedKey],
  );
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
