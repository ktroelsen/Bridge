import type { BiddingQuestion, DealInfo, GameRequest, GameState, MiniBridgeRequest, MiniBridgeState, PlayHint, PlayQuestion, QuizKind, Review } from './types';

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

/** Replays a full game (bidding and play) on the server; the bots act up to the player's next turn. */
export const postGame = (request: GameRequest) => post<GameState>('/api/game', request);

/** A fresh "what do you bid?" question, answered by the bidding system. */
export const fetchBiddingQuestion = (kind: QuizKind) => call<BiddingQuestion>(`/api/quiz/bidding?kind=${kind}`);

/** The best cards for the player right now (double dummy: all cards open). */
export const fetchGameHint = (request: GameRequest) => post<PlayHint>('/api/game/hint', request);
export const fetchMiniBridgeHint = (request: MiniBridgeRequest) => post<PlayHint>('/api/minibridge/hint', request);

/** The double dummy review of a finished deal. */
export const fetchGameReview = (request: GameRequest) => post<Review>('/api/game/review', request);
export const fetchMiniBridgeReview = (request: MiniBridgeRequest) => post<Review>('/api/minibridge/review', request);

/** A declarer play problem with all four hands open, solved double dummy. */
export const fetchPlayQuestion = (stage: 'early' | 'middle' | 'defend') => call<PlayQuestion>(`/api/quiz/play?stage=${stage}`);
