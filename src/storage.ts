import type { BotLevel, GameRequest, MiniBridgeRequest } from './types';

// localStorage can be missing or throw (private mode, blocked storage); the app works without it.
const KEY = 'bridge.minibridge';

export function loadMiniBridge(): MiniBridgeRequest | null {
  try {
    const raw = localStorage.getItem(KEY);
    return raw ? (JSON.parse(raw) as MiniBridgeRequest) : null;
  } catch {
    return null;
  }
}

export function saveMiniBridge(request: MiniBridgeRequest | null) {
  try {
    if (request) localStorage.setItem(KEY, JSON.stringify(request));
    else localStorage.removeItem(KEY);
  } catch {
    /* not saved — the game just won't survive a reload */
  }
}

const GAME_KEY = 'bridge.game';

export function loadGame(): GameRequest | null {
  try {
    const raw = localStorage.getItem(GAME_KEY);
    return raw ? (JSON.parse(raw) as GameRequest) : null;
  } catch {
    return null;
  }
}

export function saveGame(request: GameRequest) {
  try {
    localStorage.setItem(GAME_KEY, JSON.stringify(request));
  } catch {
    /* not saved */
  }
}

const LEVEL_KEY = 'bridge.level';

export function loadLevel(): BotLevel {
  try {
    const raw = localStorage.getItem(LEVEL_KEY);
    return raw === 'medium' || raw === 'hard' ? raw : 'easy';
  } catch {
    return 'easy';
  }
}

export function saveLevel(level: BotLevel) {
  try {
    localStorage.setItem(LEVEL_KEY, level);
  } catch {
    /* not saved */
  }
}
