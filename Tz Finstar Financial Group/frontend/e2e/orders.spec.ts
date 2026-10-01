import { test, expect } from '@playwright/test';

test.beforeEach(async ({ request }) => {
  await request.get('/__test/reset');
});

test('list, search, filter and persistent watchlist', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('link', { name: 'ORD-2026-1042', exact: true })).toBeVisible();
  await page.getByLabel('Поиск заказов').fill('1041');
  await expect(page.locator('tbody tr')).toHaveCount(1);
  await page.getByRole('button', { name: 'Отслеживать заказ', exact: true }).click();
  await page.getByRole('link', { name: /Отслеживаемые/ }).click();
  await expect(page.locator('tbody tr')).toHaveCount(1);
  await page.reload();
  await expect(page.getByRole('link', { name: 'ORD-2026-1041', exact: true })).toBeVisible();
  await page.getByRole('link', { name: 'Все заказы', exact: true }).click();
  await page.getByLabel('Фильтр по статусу').selectOption('Delivered');
  await expect(page.locator('tbody tr')).toHaveCount(2);
});

test('create order, advance lifecycle and render history', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Новый заказ' }).click();
  await page.getByLabel('Номер заказа').fill('E2E-ORDER');
  await page.getByLabel('Описание', { exact: true }).fill('Новый заказ для тестирования');
  await page.getByRole('button', { name: 'Создать заказ', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'E2E-ORDER' })).toBeVisible();
  await page.getByRole('button', { name: 'Отметить отправленным' }).click();
  await expect(page.getByRole('button', { name: 'Отметить доставленным' })).toBeVisible();
  await page.getByRole('button', { name: 'Отметить доставленным' }).click();
  await expect(page.getByText('Заказ доставлен. Все этапы завершены.')).toBeVisible();
  await expect(page.locator('.timeline li')).toHaveCount(3);
  await page.screenshot({ path: 'test-results/details.png', fullPage: true });
});

test('SSE refreshes order details after an external status change', async ({ page, request }) => {
  const response = await request.get('/api/orders?search=1041');
  const order = (await response.json()).items[0];
  await page.goto(`/orders/${order.id}`);
  await expect(page.getByRole('button', { name: 'Отметить отправленным' })).toBeVisible();
  await expect(page.getByRole('status')).toHaveText('Обновляется в реальном времени');
  await request.patch(`/api/orders/${order.id}/status`, {
    data: { status: 'Shipped', version: order.version },
  });
  await expect(page.getByRole('button', { name: 'Отметить доставленным' })).toBeVisible();
  await expect(page.locator('.timeline li')).toHaveCount(2);
});

test('duplicate error keeps the creation form open and Escape closes it', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('button', { name: 'Новый заказ' }).click();
  await page.getByLabel('Номер заказа').fill('ORD-2026-1042');
  await page.getByLabel('Описание', { exact: true }).fill('Повторный заказ');
  await page.getByRole('button', { name: 'Создать заказ', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('уже существует');
  await expect(page.getByRole('dialog')).toBeVisible();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog')).not.toBeVisible();
});

test('desktop and mobile layouts are usable without page overflow', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', (error) => errors.push(error.message));
  await page.goto('/');
  await expect(page.locator('tbody tr')).toHaveCount(7);
  await page.screenshot({ path: 'test-results/desktop.png', fullPage: true });
  await page.setViewportSize({ width: 390, height: 844 });
  await page.screenshot({ path: 'test-results/mobile.png', fullPage: true });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth)).toBe(
    true,
  );
  await page.getByRole('button', { name: 'Новый заказ' }).click();
  await expect(page.getByLabel('Номер заказа')).toBeVisible();
  await page.screenshot({ path: 'test-results/mobile-dialog.png', fullPage: true });
  expect(errors).toEqual([]);
});

test('cancellation requires confirmation and becomes terminal', async ({ page }) => {
  await page.goto('/');
  await page.getByRole('link', { name: 'ORD-2026-1041', exact: true }).click();
  await page.getByRole('button', { name: 'Отменить заказ', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Да, отменить' })).toBeVisible();
  await page.getByRole('button', { name: 'Да, отменить' }).click();
  await expect(page.getByText('Заказ отменён. Изменение статуса недоступно.')).toBeVisible();
});
