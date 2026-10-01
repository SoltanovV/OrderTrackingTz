import { beforeEach, describe, expect, it } from 'vitest';
import { useWatchlist } from './watchlist';

describe('watchlist', () => {
  beforeEach(() => {
    localStorage.clear();
    useWatchlist.setState({ ids: [] });
  });
  it('toggles and persists an order without duplicates', () => {
    useWatchlist.getState().toggle('one');
    expect(useWatchlist.getState().ids).toEqual(['one']);
    expect(JSON.parse(localStorage.getItem('order-tracking-watched-orders')!).state.ids).toEqual(['one']);
    useWatchlist.getState().toggle('one');
    expect(useWatchlist.getState().ids).toEqual([]);
  });
  it('matches the API limit and still allows removing at capacity', () => {
    for (let i = 0; i < 101; i++) useWatchlist.getState().toggle(String(i));
    expect(useWatchlist.getState().ids).toHaveLength(100);
    useWatchlist.getState().toggle('0');
    expect(useWatchlist.getState().ids).toHaveLength(99);
  });
});
