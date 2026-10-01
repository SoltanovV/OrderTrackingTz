import { useOrderDetails } from '../hooks/useOrderDetails';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Check, CheckCircle2, Clock3, RefreshCw, Truck } from 'lucide-react';
import { useLive } from '../store/live';
import { date } from '../utils/date';
import { labels, transitions } from '../constants/orderStatus';
import Status from '../components/Status';
import WatchButton from '../components/WatchButton';

export default function OrderDetails() {
  const { id = '' } = useParams();
  const { order, error, actionError, busy, confirmCancel, setConfirmCancel, update } =
    useOrderDetails(id);
  return (
    <>
      <Link className="back-link" to="/">
        <ArrowLeft size={16} />К списку заказов
      </Link>
      {error && (
        <div className="error" role="alert">
          {error}
          <button className="text-button" onClick={() => useLive.getState().refresh()}>
            Повторить
          </button>
        </div>
      )}
      {!order ? (
        !error && (
          <div className="empty" role="status">
            Загружаем заказ…
          </div>
        )
      ) : (
        <>
          <div className="page-heading detail-heading">
            <div>
              <h1>{order.orderNumber}</h1>
            </div>
            <WatchButton id={order.id} expanded />
          </div>
          <div className="detail-grid">
            <div className="detail-main">
              <section className="panel detail-panel">
                <div className="section-heading">
                  <h2>Информация о заказе</h2>
                  <Status status={order.status} />
                </div>
                <label className="data-label">Описание</label>
                <p className="full-description">{order.description}</p>
                <div className="meta-grid">
                  <div>
                    <span className="data-label">Дата создания</span>
                    <p>{date(order.createdAt)}</p>
                  </div>
                  <div>
                    <span className="data-label">Последнее изменение</span>
                    <p>{date(order.updatedAt)}</p>
                  </div>
                </div>
              </section>
              <section className="panel detail-panel">
                <div className="section-heading">
                  <h2>Управление статусом</h2>
                  <RefreshCw size={18} />
                </div>
                {transitions[order.status].length ? (
                  <>
                    <div className="status-actions">
                      {transitions[order.status]
                        .filter((s) => s !== 'Cancelled')
                        .map((s) => (
                          <button
                            key={s}
                            className="button"
                            disabled={busy}
                            onClick={() => update(s)}
                          >
                            {s === 'Shipped' ? <Truck size={18} /> : <Check size={18} />}
                            {busy
                              ? 'Сохранение…'
                              : s === 'Shipped'
                                ? 'Отметить отправленным'
                                : 'Отметить доставленным'}
                          </button>
                        ))}
                      <button
                        className="button danger"
                        disabled={busy}
                        onClick={() => setConfirmCancel(true)}
                      >
                        Отменить заказ
                      </button>
                    </div>
                    {confirmCancel && (
                      <div className="cancel-confirm" role="alert">
                        <p>Отменить заказ? Вернуть его в работу будет нельзя.</p>
                        <button
                          className="button danger"
                          disabled={busy}
                          onClick={() => update('Cancelled')}
                        >
                          Да, отменить
                        </button>
                        <button
                          className="text-button"
                          disabled={busy}
                          onClick={() => setConfirmCancel(false)}
                        >
                          Оставить заказ
                        </button>
                      </div>
                    )}
                  </>
                ) : (
                  <div className="completed">
                    <CheckCircle2 size={20} />
                    <p>
                      {order.status === 'Delivered'
                        ? 'Заказ доставлен. Все этапы завершены.'
                        : 'Заказ отменён. Изменение статуса недоступно.'}
                    </p>
                  </div>
                )}
                {actionError && (
                  <div className="error" role="alert">
                    {actionError}
                  </div>
                )}
              </section>
            </div>
            <section className="panel detail-panel history-panel">
              <div className="section-heading">
                <h2>История заказа</h2>
                <Clock3 size={18} />
              </div>
              <ol className="timeline">
                {order.history.map((item, index) => (
                  <li
                    key={item.version}
                    className={index === order.history.length - 1 ? 'current' : ''}
                  >
                    <span className={`timeline-marker ${item.status}`}>
                      <Check size={12} />
                    </span>
                    <strong>{labels[item.status]}</strong>
                    <time dateTime={item.changedAt}>{date(item.changedAt)}</time>
                    {index === order.history.length - 1 && (
                      <span className="current-label">Текущий статус</span>
                    )}
                  </li>
                ))}
              </ol>
            </section>
          </div>
        </>
      )}
    </>
  );
}
