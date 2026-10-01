import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface Watchlist {
  ids: string[];
  toggle: (id: string) => void;
}
export const useWatchlist = create<Watchlist>()(
  persist(
    (set) => ({
      ids: [],
      toggle: (id) =>
        set((state) => ({
          ids: state.ids.includes(id)
            ? state.ids.filter((item) => item !== id)
            : state.ids.length < 100
              ? [...state.ids, id]
              : state.ids,
        })),
    }),
    { name: 'order-tracking-watched-orders', version: 1 },
  ),
);
