import { useLive } from '../store/live';

export default function ConnectionIndicator() {
  const connection = useLive((s) => s.connection);
  const text = {
    live: 'Обновляется в реальном времени',
    connecting: 'Подключение…',
    offline: 'Переподключение…',
    delayed: 'Уведомления задерживаются',
  }[connection];
  return (
    <div className={`connection ${connection}`} role="status">
      <span />
      {text}
    </div>
  );
}
