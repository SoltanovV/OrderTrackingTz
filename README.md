# Order Tracking

## Запуск через Docker

Выполняйте команды из папки с этим README и `compose.yaml`.

```sh
docker compose -f compose.yaml up --build -d
```

Docker запустит frontend, backend, PostgreSQL и RabbitMQ. Миграции базы данных применятся автоматически.

- Приложение: [localhost:8080](http://localhost:8080).
- Проверка готовности API, БД и брокера: [localhost:5268/health](http://localhost:5268/health).

Остановка с сохранением данных:

```sh
docker compose -f compose.yaml down
```

## Назначение проектов

- **OrderTracking** — ASP.NET Core API: HTTP-запросы и уведомления через SSE.
- **OrderTracking.Domain** — заказ, статусы и правила их изменения.
- **OrderTracking.Application** — сценарии работы с заказами и интерфейсы зависимостей.
- **OrderTracking.Infrastructure** — EF Core, PostgreSQL, миграции и обмен событиями через RabbitMQ.
- **OrderTracking.Tests** — тесты backend.
- **frontend** — интерфейс на React и TypeScript отслеживание заказов через Zustand.
- **sql** — SQL-задание: таблица и функция поденных сумм платежей.
