import { describe, expect, it } from 'vitest';
import { parseHand } from '../deck';
import { sureWinners, suitLosers } from './drills';

describe('sureWinners', () => {
  it('counts top cards in a row, capped by the longer hand', () => {
    const north = parseHand('AK2.Q3.5432.5432');
    const south = parseHand('Q43.AK.AK2.A9876');
    const per = Object.fromEntries(sureWinners(north, south).map((s) => [s.suit, s.sure]));
    expect(per.spades).toBe(3); // A K Q
    expect(per.hearts).toBe(2); // A K Q, but only two cards in the longer hand
    expect(per.diamonds).toBe(2); // A K
    expect(per.clubs).toBe(1); // A
  });
});

describe('suitLosers', () => {
  it('looks at the top three cards', () => {
    const hand = parseHand('AKQ43.K2.Q.J987');
    expect(suitLosers(hand, 'spades')).toBe(0);
    expect(suitLosers(hand, 'hearts')).toBe(1); // missing the ace
    expect(suitLosers(hand, 'diamonds')).toBe(1); // a singleton queen
    expect(suitLosers(hand, 'clubs')).toBe(3); // no A, K or Q
  });
});
