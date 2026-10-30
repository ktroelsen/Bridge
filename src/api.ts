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

export interface LessonProgress {
  lessonId: string;
  bestScore: number;
  completedAt: string;
}

const playerUrl = (name: string) => `/api/players/${encodeURIComponent(name)}`;

export const registerPlayer = (name: string) => post<{ name: string }>('/api/players', { name });

export const fetchProgress = (name: string) => call<LessonProgress[]>(`${playerUrl(name)}/progress`);

export const saveLessonResult = (name: string, lessonId: string, score: number) =>
  call<LessonProgress>(`${playerUrl(name)}/progress/${lessonId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ score }),
  });
