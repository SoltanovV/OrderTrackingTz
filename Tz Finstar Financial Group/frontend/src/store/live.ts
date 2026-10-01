import { create } from 'zustand';

type Connection = 'connecting' | 'live' | 'offline' | 'delayed';
interface LiveState {
  revision: number;
  connection: Connection;
  refresh: () => void;
  setConnection: (connection: Connection) => void;
}
export const useLive = create<LiveState>((set) => ({
  revision: 0,
  connection: 'connecting',
  refresh: () => set((state) => ({ revision: state.revision + 1 })),
  setConnection: (connection) => set({ connection }),
}));
