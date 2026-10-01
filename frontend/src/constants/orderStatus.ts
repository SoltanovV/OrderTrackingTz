import type { OrderStatus } from '../types/order';

export const labels: Record<OrderStatus, string> = {
  Created: 'Создан',
  Shipped: 'Отправлен',
  Delivered: 'Доставлен',
  Cancelled: 'Отменён',
};
export const transitions: Record<OrderStatus, OrderStatus[]> = {
  Created: ['Shipped', 'Cancelled'],
  Shipped: ['Delivered', 'Cancelled'],
  Delivered: [],
  Cancelled: [],
};
