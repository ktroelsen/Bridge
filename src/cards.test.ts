import { describe, expect, it } from 'vitest';
import { cardsInSuit, RANK_LABEL } from './cards';
import type { Card } from './types';

describe('cardsInSuit', () => {
  it('keeps one suit and sorts it high to low', () => {
    const cards: Card[] = [
      { suit: 'hearts', rank: 'two' },
      { suit: 'spades', rank: 'ace' },
      { suit: 'hearts', rank: 'king' },
      { suit: 'hearts', rank: 'ten' },
    ];
    expect(cardsInSuit(cards, 'hearts').map((c) => RANK_LABEL[c.rank])).toEqual(['K', '10', '2']);
    expect(cardsInSuit(cards, 'clubs')).toEqual([]);
  });
});
