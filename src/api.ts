import type { DealInfo } from './types';

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
