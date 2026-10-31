import { describe, expect, it } from 'vitest';
import { parseHand } from '../deck';
import { RANK_LABEL } from '../cards';
import { leadFromSuit, sureWinners, suitLosers } from './drills';

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

describe('leadFromSuit', () => {
  const lead = (pbn: string, trump: 'hearts' | null = null) => RANK_LABEL[leadFromSuit(parseHand(pbn), 'spades', trump).card.rank];
  it('follows the classic lead rules', () => {
    expect(lead('KQJ74.32.432.432')).toBe('K'); // top of a sequence
    expect(lead('K9742.32.432.432')).toBe('4'); // 4th best
    expect(lead('92.K32.8432.5432')).toBe('9'); // top of a doubleton
    expect(lead('K92.J32.8432.543')).toBe('2'); // low from an honour
    expect(lead('972.J32.8432.543')).toBe('9'); // top of nothing
    expect(lead('A972.J32.843.543', 'hearts')).toBe('E'); // no underlead of an ace against a suit contract
    expect(lead('A972.J32.843.543')).toBe('2'); // but fine against no trump
  });
});
