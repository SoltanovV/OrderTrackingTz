import { afterEach, describe, expect, it, vi } from 'vitest';
import { api } from './orders';
import { ApiError } from './client';

describe('API errors', () => {
  afterEach(() => vi.unstubAllGlobals());
  it('preserves a conflict so the UI can refresh a stale order', async () => {
    vi.stubGlobal(
      'fetch',
      vi
        .fn()
        .mockResolvedValue(
          new Response(JSON.stringify({ title: 'Заказ уже изменён' }), { status: 409 }),
        ),
    );
    await expect(api.create('TEST', 'description')).rejects.toMatchObject({
      status: 409,
      message: 'Заказ уже изменён',
    });
  });
  it('handles non-JSON proxy failures', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('Bad Gateway', { status: 502 })));
    await expect(api.get('missing')).rejects.toBeInstanceOf(ApiError);
  });
  it('displays server validation details', async () => {
    vi.stubGlobal(
      'fetch',
      vi
        .fn()
        .mockResolvedValue(
          new Response(JSON.stringify({ errors: { OrderNumber: ['Required'] } }), { status: 400 }),
        ),
    );
    await expect(api.create('', '')).rejects.toMatchObject({ status: 400, message: 'Required' });
  });
});
