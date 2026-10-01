import { useState } from 'react';
import { Link, NavLink, Route, Routes } from 'react-router-dom';
import { Bookmark, Package } from 'lucide-react';
import { useWatchlist } from './store/watchlist';
import { useLiveEvents } from './hooks/useLiveEvents';
import ConnectionIndicator from './components/ConnectionIndicator';
import CreateDialog from './components/CreateDialog';
import OrderList from './pages/OrderList';
import OrderDetails from './pages/OrderDetails';

export default function App() {
  useLiveEvents();
  const [creating, setCreating] = useState(false);
  const trackedCount = useWatchlist((s) => s.ids.length);
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <nav>
          <NavLink to="/" end>
            <Package size={19} />
            Все заказы
          </NavLink>
          <NavLink to="/tracked">
            <Bookmark size={18} />
            Отслеживаемые<span className="nav-count">{trackedCount}</span>
          </NavLink>
        </nav>
      </aside>
      <div className="main-shell">
        <header className="topbar">
          <ConnectionIndicator />
        </header>
        <main>
          <Routes>
            <Route
              path="/"
              element={<OrderList key="all" watched={false} onCreate={() => setCreating(true)} />}
            />
            <Route
              path="/tracked"
              element={<OrderList key="tracked" watched onCreate={() => setCreating(true)} />}
            />
            <Route path="/orders/:id" element={<OrderDetails />} />
            <Route
              path="*"
              element={
                <div className="empty">
                  <h1>Страница не найдена</h1>
                  <Link className="button" to="/">
                    К заказам
                  </Link>
                </div>
              }
            />
          </Routes>
        </main>
      </div>
      <CreateDialog open={creating} onClose={() => setCreating(false)} />
    </div>
  );
}
