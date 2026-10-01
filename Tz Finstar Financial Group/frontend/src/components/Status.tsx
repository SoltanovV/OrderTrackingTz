import { labels } from '../constants/orderStatus';
import type { OrderStatus } from '../types/order';

export default function Status({ status }: { status: OrderStatus }) {
  return (
    <span className={`status ${status}`}>
      <span />
      {labels[status]}
    </span>
  );
}
