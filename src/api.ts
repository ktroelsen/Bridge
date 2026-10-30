import type { DealInfo, MiniBridgeRequest, MiniBridgeState } from './types';

async function call<T>(url: string, init?: RequestInit): Promise<T> {
  const response = await fetch(url, init);
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.error ?? `Serverfejl (${response.status})`);
  }
  return response.json();
}

/** A deal with all four hands open. Without a number the server picks a random one. */
export const fetchDeal = (number?: number) =>
  call<DealInfo>(number === undefined ? '/api/deal' : `/api/deal?number=${number}`);

const post = <T>(url: string, body: unknown) =>
  call<T>(url, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) });

/** Replays a mini-bridge game on the server; the bot plays up to the player's next card. */
export const postMiniBridge = (request: MiniBridgeRequest) => post<MiniBridgeState>('/api/minibridge', request);
