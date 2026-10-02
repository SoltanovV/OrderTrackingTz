import { useEffect, useRef, type DependencyList } from 'react';
import { useLive } from '../store/live';

export function useLiveQuery(
  load: (signal: AbortSignal) => Promise<void>,
  dependencies: DependencyList,
) {
  const revision = useLive((s) => s.revision);
  const reload = useRef<() => void>(() => {});

  useEffect(() => reload.current(), [revision]);
  useEffect(() => {
    const controller = new AbortController();
    let running = false;
    let pending = false;

    async function refresh() {
      pending = true;
      if (running) return;
      running = true;
      try {
        while (pending && !controller.signal.aborted) {
          pending = false;
          await load(controller.signal);
        }
      } finally {
        running = false;
      }
    }

    // Events queue one refresh instead of cancelling an unfinished request.
    reload.current = () => void refresh();
    reload.current();
    return () => {
      controller.abort();
      reload.current = () => {};
    };
  }, dependencies);
}
