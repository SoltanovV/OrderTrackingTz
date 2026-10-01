export const statuses = ['Created', 'Shipped', 'Delivered', 'Cancelled'] as const;
export type OrderStatus = (typeof statuses)[number];
export interface HistoryItem {
  status: OrderStatus;
  changedAt: string;
  version: number;
}
export interface Order {
  id: string;
  orderNumber: string;
  description: string;
  status: OrderStatus;
  createdAt: string;
  updatedAt: string;
  version: number;
  history: HistoryItem[];
}
export interface OrderPage {
  items: Order[];
  total: number;
  page: number;
  pageSize: number;
}
