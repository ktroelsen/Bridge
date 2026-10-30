import type { Card, Rank, Seat, Strain, Suit, Vulnerability } from './types';

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

export const STRAINS: Strain[] = ['clubs', 'diamonds', 'hearts', 'spades', 'noTrump'];
export const STRAIN_LABEL: Record<Strain, string> = { ...SUIT_SYMBOL, noTrump: 'UT' };
export const STRAIN_NAME: Record<Strain, string> = { ...SUIT_NAME, noTrump: 'Uden trumf' };

/** The lowest game level: 3UT, 4♥/4♠, 5♣/5♦. */
export const gameLevel = (strain: Strain) => (strain === 'noTrump' ? 3 : strain === 'hearts' || strain === 'spades' ? 4 : 5);

export const sameCard = (a: Card, b: Card) => a.suit === b.suit && a.rank === b.rank;

export const cardLabel = (card: Card) => `${SUIT_SYMBOL[card.suit]}${RANK_LABEL[card.rank]}`;

export const isNorthSouth = (seat: Seat) => seat === 'north' || seat === 'south';

const PARTNER: Record<Seat, Seat> = { north: 'south', south: 'north', east: 'west', west: 'east' };
export const partnerOf = (seat: Seat) => PARTNER[seat];

const STRAIN_BY_LETTER: Record<string, Strain> = { C: 'clubs', D: 'diamonds', H: 'hearts', S: 'spades', N: 'noTrump' };

/** "1H" → "1♥", "3N" → "3UT", "P" → "Pas", "X" → "Dobl", "XX" → "Redobl". */
export function callLabel(call: string): string {
  if (call === 'P') return 'Pas';
  if (call === 'X') return 'Dobl';
  if (call === 'XX') return 'Redobl';
  return `${call[0]}${STRAIN_LABEL[STRAIN_BY_LETTER[call[1]]]}`;
}

export const callIsRed = (call: string) => call[1] === 'H' || call[1] === 'D';
