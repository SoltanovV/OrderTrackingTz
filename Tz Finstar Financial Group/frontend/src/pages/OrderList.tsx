import { useOrderList } from '../hooks/useOrderList';
import { Link } from 'react-router-dom';
import {
  ArrowRight,
  Bookmark,
  ChevronLeft,
  ChevronRight,
  Inbox,
  Package,
  Plus,
  RefreshCw,
  Search,
  X,
} from 'lucide-react';
import { useLive } from '../store/live';
import { date } from '../utils/date';
import { labels } from '../constants/orderStatus';
import { statuses } from '../types/order';
import Status from '../components/Status';
import WatchButton from '../components/WatchButton';

export default function OrderList({
  watched,
  onCreate,
}: {
  watched: boolean;
  onCreate: () => void;
}) {
  const {
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
  } = useOrderList(watched);
  return (
    <>
      <div className="page-heading">
        <div>
          <h1>
            {watched ? 'Отслеживаемые заказы' : 'Все заказы'}
            <span className="count">{result?.total ?? '—'}</span>
          </h1>
        </div>
        <button className="button" onClick={onCreate}>
          <Plus size={19} />
          Новый заказ
        </button>
      </div>
      <section className="panel" aria-label="Список заказов">
        <div className="panel-toolbar">
          <div className="search">
            <Search size={19} />
            <input
              aria-label="Поиск заказов"
              maxLength={200}
              placeholder="Найти по номеру или описанию"
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
                setPage(1);
              }}
            />
            {search && (
              <button
                className="icon-button"
                aria-label="Очистить поиск"
                onClick={() => {
                  setSearch('');
                  setPage(1);
                }}
              >
                <X size={16} />
              </button>
            )}
          </div>
          <select
            aria-label="Фильтр по статусу"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
          >
            <option value="">Все статусы</option>
            {statuses.map((s) => (
              <option key={s} value={s}>
                {labels[s]}
              </option>
            ))}
          </select>
          <button
            className="icon-button"
            aria-label="Обновить список"
            title="Обновить список"
            onClick={() => useLive.getState().refresh()}
          >
            <RefreshCw size={18} className={loading ? 'spin' : ''} />
          </button>
        </div>
        {error ? (
          <div className="empty">
            <Inbox size={34} />
            <h3>Не удалось загрузить заказы</h3>
            <p role="alert">{error}</p>
            <button className="button secondary" onClick={() => useLive.getState().refresh()}>
              Повторить
            </button>
          </div>
        ) : !result && loading ? (
          <div className="empty" role="status">
            <RefreshCw className="spin" size={26} />
            <p>Загружаем заказы…</p>
          </div>
        ) : result?.items.length ? (
          <div className="table-scroll" aria-busy={loading}>
            <table>
              <thead>
                <tr>
                  <th className="bookmark-column">
                    <Bookmark size={15} aria-label="Отслеживание" />
                  </th>
                  <th>Заказ / описание</th>
                  <th>Статус</th>
                  <th>Создан</th>
                  <th>Обновлён</th>
                  <th>
                    <span className="sr-only">Подробнее</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {result.items.map((order) => (
                  <tr key={order.id}>
                    <td>
                      <WatchButton id={order.id} />
                    </td>
                    <td>
                      <Link className="order-number" to={`/orders/${order.id}`}>
                        {order.orderNumber}
                      </Link>
                      <div className="order-description" title={order.description}>
                        {order.description}
                      </div>
                    </td>
                    <td>
                      <Status status={order.status} />
                    </td>
                    <td className="table-date">{date(order.createdAt)}</td>
                    <td className="table-date">{date(order.updatedAt)}</td>
                    <td>
                      <Link
                        className="row-link"
                        to={`/orders/${order.id}`}
                        aria-label={`Открыть заказ ${order.orderNumber}`}
                      >
                        <ArrowRight size={19} />
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="empty">
            <span className="empty-icon">
              {watched ? <Bookmark size={30} /> : <Package size={32} />}
            </span>
            <h3>
              {search || status
                ? 'Ничего не найдено'
                : watched
                  ? 'Нет отслеживаемых заказов'
                  : 'Заказов пока нет'}
            </h3>
            <p>
              {search || status
                ? 'Попробуйте изменить запрос или выбрать другой статус.'
                : watched
                  ? 'Нажмите на закладку рядом с заказом в общем списке.'
                  : 'Создайте заказ и следите за изменением его статуса.'}
            </p>
            {!search &&
              !status &&
              (watched ? (
                <Link className="button secondary" to="/">
                  Перейти ко всем заказам
                  <ArrowRight size={16} />
                </Link>
              ) : (
                <button className="button secondary" onClick={onCreate}>
                  <Plus size={17} />
                  Создать заказ
                </button>
              ))}
          </div>
        )}
        <div className="table-footer">
          <span>
            {result?.total
              ? `${(page - 1) * 10 + 1}–${Math.min(page * 10, result.total)} из ${result.total} заказов`
              : '0 заказов'}
          </span>
          <div className="pagination">
            <button
              className="icon-button"
              disabled={page <= 1 || loading}
              onClick={() => setPage(page - 1)}
              aria-label="Предыдущая страница"
            >
              <ChevronLeft size={18} />
            </button>
            <span>
              {page} / {totalPages}
            </span>
            <button
              className="icon-button"
              disabled={page >= totalPages || loading}
              onClick={() => setPage(page + 1)}
              aria-label="Следующая страница"
            >
              <ChevronRight size={18} />
            </button>
          </div>
        </div>
      </section>
    </>
  );
}
