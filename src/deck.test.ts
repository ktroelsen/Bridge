import { describe, expect, it } from 'vitest';
import { hcp, isBalanced, legalCards, randomDeal, shape, winningPlay } from './deck';
import type { Card, PlayedCard } from './types';

const c = (suit: Card['suit'], rank: Card['rank']): Card => ({ suit, rank });

describe('randomDeal', () => {
  it('deals all 52 cards, 13 each, with 40 points', () => {
    const deal = randomDeal();
    const all = Object.values(deal).flat();
    expect(new Set(all.map((x) => `${x.suit}${x.rank}`)).size).toBe(52);
    expect(Object.values(deal).every((h) => h.length === 13)).toBe(true);
    expect(Object.values(deal).reduce((n, h) => n + hcp(h), 0)).toBe(40);
  });
});

describe('trick rules', () => {
  const plays: PlayedCard[] = [
    { seat: 'west', card: c('clubs', 'king') },
    { seat: 'north', card: c('clubs', 'four') },
    { seat: 'east', card: c('hearts', 'two') },
    { seat: 'south', card: c('clubs', 'ace') },
  ];
  it('highest card of the led suit wins without trumps', () => {
    expect(winningPlay(plays, null).seat).toBe('south');
  });
  it('a trump beats the led suit', () => {
    expect(winningPlay(plays, 'hearts').seat).toBe('east');
  });
  it('must follow suit when possible', () => {
    const hand = [c('spades', 'ace'), c('hearts', 'two'), c('hearts', 'king')];
    expect(legalCards(hand, 'hearts')).toHaveLength(2);
    expect(legalCards(hand, 'clubs')).toHaveLength(3);
  });
});

describe('hand shape', () => {
  const hand = (lengths: number[]) =>
    (['spades', 'hearts', 'diamonds', 'clubs'] as const).flatMap((suit, i) =>
      (['two', 'three', 'four', 'five', 'six', 'seven', 'eight'] as const).slice(0, lengths[i]).map((rank) => c(suit, rank)),
    );
  it('knows balanced shapes', () => {
    expect(isBalanced(hand([4, 3, 3, 3]))).toBe(true);
    expect(isBalanced(hand([5, 3, 3, 2]))).toBe(true);
    expect(isBalanced(hand([4, 4, 3, 2]))).toBe(true);
    expect(isBalanced(hand([5, 4, 2, 2]))).toBe(false);
    expect(isBalanced(hand([5, 4, 3, 1]))).toBe(false);
    expect(shape(hand([3, 5, 2, 3]))).toEqual([5, 3, 3, 2]);
  });
});
