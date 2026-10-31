import { gameLevel } from './cards';
import type { Strain } from './types';

// Duplicate scoring for undoubled contracts (mirrors Bridge.Engine/Scoring.cs).

export function trickValue(strain: Strain, level: number) {
  if (strain === 'clubs' || strain === 'diamonds') return 20 * level;
  if (strain === 'hearts' || strain === 'spades') return 30 * level;
  return 40 + 30 * (level - 1);
}

/** The score for the declaring side (negative when the contract goes down). */
export function score(level: number, strain: Strain, vulnerable: boolean, tricks: number) {
  const over = tricks - (level + 6);
  if (over < 0) return over * (vulnerable ? 100 : 50);
  const value = trickValue(strain, level);
  let total = value + (value >= 100 ? (vulnerable ? 500 : 300) : 50);
  if (level === 6) total += vulnerable ? 750 : 500;
  if (level === 7) total += vulnerable ? 1500 : 1000;
  return total + over * (strain === 'clubs' || strain === 'diamonds' ? 20 : 30);
}

/** The contract to bid with these tricks: game, slam with 12+, otherwise the tricks you take. */
export function levelFor(strain: Strain, tricks: number) {
  return tricks >= 12 ? tricks - 6 : Math.min(tricks - 6, gameLevel(strain));
}
