// UI contract fixture only. Production always uses ASP.NET Core + PostgreSQL + RabbitMQ.
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import path from 'node:path';
import { randomUUID } from 'node:crypto';

const root = path.resolve('dist');
let orders = [];
const streams = new Set();
const json = (res, code, value) => {
  res.writeHead(code, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(value));
};
const emit = (order) =>
  streams.forEach((res) =>
    res.write(
      `event: order-changed\ndata: ${JSON.stringify({ eventId: randomUUID(), order })}\n\n`,
    ),
  );
const make = (number, description, status = 'Created', offset = 0) => {
  const createdAt = new Date(Date.UTC(2026, 8, 30, 8, 30 - offset)).toISOString();
  const steps =
    status === 'Delivered'
      ? ['Created', 'Shipped', 'Delivered']
      : status === 'Created'
        ? ['Created']
        : ['Created', status];
  return {
    id: randomUUID(),
    orderNumber: number,
    description,
    status,
    createdAt,
    updatedAt: createdAt,
    version: steps.length,
    history: steps.map((status, i) => ({ status, changedAt: createdAt, version: i + 1 })),
  };
};
const seed = () => {
  orders = [
    make('ORD-2026-1042', 'Ноутбук Lenovo ThinkPad и комплект аксессуаров', 'Shipped'),
    make('ORD-2026-1041', 'Мониторы Dell UltraSharp, 3 шт.', 'Created', 1),
    make('ORD-2026-1040', 'Офисная мебель для переговорной', 'Delivered', 2),
    make('ORD-2026-1039', 'Канцелярские принадлежности', 'Shipped', 3),
    make('ORD-2026-1038', 'Сетевое оборудование Cisco', 'Created', 4),
    make('ORD-2026-1037', 'Комплект беспроводных клавиатур', 'Cancelled', 5),
    make('ORD-2026-1036', 'Бумага для принтера, 20 упаковок', 'Delivered', 6),
  ];
};
seed();
createServer(async (req, res) => {
  try {
    const url = new URL(req.url, 'http://localhost');
    if (url.pathname === '/__test/reset') {
      seed();
      return json(res, 200, orders);
    }
    if (url.pathname === '/api/events') {
      res.writeHead(200, {
        'Content-Type': 'text/event-stream',
        'Cache-Control': 'no-cache',
        Connection: 'keep-alive',
      });
      res.write('retry: 1000\nevent: ready\ndata: {"brokerConnected":true}\n\n');
      streams.add(res);
      req.on('close', () => streams.delete(res));
      return;
    }
    if (url.pathname.startsWith('/api/orders')) {
      let raw = '';
      for await (const chunk of req) raw += chunk;
      const body = raw ? JSON.parse(raw) : {};
      if (req.method === 'POST') {
        const number = body.orderNumber.trim().toUpperCase();
        if (orders.some((order) => order.orderNumber === number))
          return json(res, 409, { title: 'Заказ с таким номером уже существует.' });
        const order = make(number, body.description.trim());
        orders.unshift(order);
        emit(order);
        return json(res, 201, order);
      }
      const id = url.pathname.split('/')[3];
      if (id) {
        const order = orders.find((item) => item.id === id);
        if (!order) return json(res, 404, { title: 'Заказ не найден.' });
        if (req.method === 'PATCH') {
          if (order.version !== body.version)
            return json(res, 409, { title: 'Заказ уже изменён. Обновите данные.' });
          order.status = body.status;
          order.version++;
          order.updatedAt = new Date().toISOString();
          order.history.push({
            status: order.status,
            version: order.version,
            changedAt: order.updatedAt,
          });
          emit(order);
        }
        return json(res, 200, order);
      }
      const search = (url.searchParams.get('search') || '').toLowerCase();
      const status = url.searchParams.get('status');
      const ids = url.searchParams.getAll('ids');
      const matched = orders.filter(
        (o) =>
          `${o.orderNumber} ${o.description}`.toLowerCase().includes(search) &&
          (!status || o.status === status) &&
          (!ids.length || ids.includes(o.id)),
      );
      const page = Number(url.searchParams.get('page') || 1);
      const pageSize = Number(url.searchParams.get('pageSize') || 10);
      return json(res, 200, {
        items: matched.slice((page - 1) * pageSize, page * pageSize),
        total: matched.length,
        page,
        pageSize,
      });
    }
    const file = path.resolve(root, '.' + decodeURIComponent(url.pathname));
    if (file !== root && !file.startsWith(root + path.sep)) return json(res, 403, {});
    const extension = path.extname(file);
    const asset = extension ? file : path.join(root, 'index.html');
    const content = await readFile(asset);
    const types = {
      '.js': 'text/javascript',
      '.css': 'text/css',
      '.svg': 'image/svg+xml',
      '.html': 'text/html',
    };
    res.writeHead(200, {
      'Content-Type': types[path.extname(asset)] || 'application/octet-stream',
    });
    res.end(content);
  } catch {
    json(res, 500, { title: 'Fixture error' });
  }
}).listen(4174, '127.0.0.1');
