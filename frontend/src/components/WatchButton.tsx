import { Bookmark } from 'lucide-react';
import { useWatchlist } from '../store/watchlist';

export default function WatchButton({ id, expanded = false }: { id: string; expanded?: boolean }) {
  const { ids, toggle } = useWatchlist();
  const active = ids.includes(id);
  return (
    <button
      className={expanded ? 'button secondary' : `icon-button watch ${active ? 'active' : ''}`}
      onClick={() => toggle(id)}
      aria-pressed={active}
      disabled={!active && ids.length >= 100}
      title={
        !active && ids.length >= 100
          ? 'Можно отслеживать до 100 заказов'
          : active
            ? 'Не отслеживать'
            : 'Отслеживать заказ'
      }
      aria-label={active ? 'Не отслеживать' : 'Отслеживать заказ'}
    >
      <Bookmark size={18} fill={active ? 'currentColor' : 'none'} />
      {expanded && (active ? 'Отслеживается' : 'Отслеживать')}
    </button>
  );
}
