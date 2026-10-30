import type { Card, Rank, Seat, Suit, Vulnerability } from './types';

/** Display order: spades, hearts, diamonds, clubs (alternating colours). */
export const SUITS: Suit[] = ['spades', 'hearts', 'diamonds', 'clubs'];

export const SUIT_SYMBOL: Record<Suit, string> = { spades: '♠', hearts: '♥', diamonds: '♦', clubs: '♣' };
export const SUIT_NAME: Record<Suit, string> = { spades: 'Spar', hearts: 'Hjerter', diamonds: 'Ruder', clubs: 'Klør' };

/** Danish card notation: E(s), K(onge), D(ame), Kn(ægt). */
export const RANK_LABEL: Record<Rank, string> = {
  ace: 'E', king: 'K', queen: 'D', jack: 'Kn', ten: '10', nine: '9', eight: '8', seven: '7',
  six: '6', five: '5', four: '4', three: '3', two: '2',
};

const RANK_ORDER: Rank[] = ['two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine', 'ten', 'jack', 'queen', 'king', 'ace'];

export const SEAT_NAME: Record<Seat, string> = { north: 'Nord', east: 'Øst', south: 'Syd', west: 'Vest' };

export const VULNERABILITY_NAME: Record<Vulnerability, string> = {
  none: 'Ingen i zonen',
  northSouth: 'NS i zonen',
  eastWest: 'ØV i zonen',
  both: 'Alle i zonen',
};

export const isRed = (suit: Suit) => suit === 'hearts' || suit === 'diamonds';

/** The cards of one suit, highest first. */
export function cardsInSuit(cards: Card[], suit: Suit): Card[] {
  return cards
    .filter((c) => c.suit === suit)
    .sort((a, b) => RANK_ORDER.indexOf(b.rank) - RANK_ORDER.indexOf(a.rank));
}
