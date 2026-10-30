import { describe, expect, it } from 'vitest';
import { playAdvice, suggestContract, type Fit } from './coach';
import type { Card } from './types';

const fit = (s: number, h: number, d: number, c: number): Fit[] => [
  { suit: 'spades', mine: s, partner: 0 },
  { suit: 'hearts', mine: h, partner: 0 },
  { suit: 'diamonds', mine: d, partner: 0 },
  { suit: 'clubs', mine: c, partner: 0 },
];

describe('suggestContract', () => {
  it('prefers a major fit and bids game with 25 points', () => {
    expect(suggestContract(26, fit(8, 2, 8, 8))).toMatchObject({ strain: 'spades', game: true });
  });
  it('plays 3UT without a major fit', () => {
    expect(suggestContract(25, fit(7, 7, 6, 6))).toMatchObject({ strain: 'noTrump', game: true });
  });
  it('plays a minor part score with a fit and few points', () => {
    expect(suggestContract(22, fit(6, 6, 9, 5))).toMatchObject({ strain: 'diamonds', game: false });
  });
});

describe('playAdvice', () => {
  const hand: Card[] = [
    { suit: 'spades', rank: 'ace' },
    { suit: 'hearts', rank: 'two' },
  ];
  it('says to follow suit when you can', () => {
    const advice = playAdvice({ seat: 'south', hand, legal: [hand[1]], ledSuit: 'hearts', trump: null, openingLead: false, fromDummy: false });
    expect(advice).toContain('bekende');
  });
  it('mentions ruffing when void with trumps', () => {
    const advice = playAdvice({ seat: 'south', hand, legal: hand, ledSuit: 'clubs', trump: 'spades', openingLead: false, fromDummy: false });
    expect(advice).toContain('trumfe');
  });
});
