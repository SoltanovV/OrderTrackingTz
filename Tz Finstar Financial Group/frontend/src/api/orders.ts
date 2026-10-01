import type { Order, OrderPage, OrderStatus } from '../types/order';
import { request } from './client';

export const api = {
  list: (params: URLSearchParams, signal?: AbortSignal) =>
    request<OrderPage>(`/orders?${params}`, { signal }),
  get: (id: string, signal?: AbortSignal) => request<Order>(`/orders/${id}`, { signal }),
  create: (orderNumber: string, description: string) =>
    request<Order>('/orders', {
      method: 'POST',
      body: JSON.stringify({ orderNumber, description }),
    }),
  changeStatus: (order: Order, status: OrderStatus) =>
    request<Order>(`/orders/${order.id}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status, version: order.version }),
    }),
};
