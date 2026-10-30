import type { Card, PlayedCard, Rank, Seat, Suit } from './types';

// Card logic for the lesson drills. The real games are played on the server; these are just
// enough rules to make up fresh hands and tricks in the browser.

export const RANKS: Rank[] = ['two', 'three', 'four', 'five', 'six', 'seven', 'eight', 'nine', 'ten', 'jack', 'queen', 'king', 'ace'];
export const SEATS: Seat[] = ['north', 'east', 'south', 'west'];
const ALL_SUITS: Suit[] = ['clubs', 'diamonds', 'hearts', 'spades'];

export const rankValue = (rank: Rank) => RANKS.indexOf(rank) + 2;

const HCP: Partial<Record<Rank, number>> = { ace: 4, king: 3, queen: 2, jack: 1 };
export const cardHcp = (card: Card) => HCP[card.rank] ?? 0;
export const hcp = (hand: Card[]) => hand.reduce((n, c) => n + cardHcp(c), 0);

export const suitLength = (hand: Card[], suit: Suit) => hand.filter((c) => c.suit === suit).length;

/** Suit lengths, longest first, e.g. [5, 3, 3, 2]. */
export const shape = (hand: Card[]) => ALL_SUITS.map((s) => suitLength(hand, s)).sort((a, b) => b - a);

/** No void, no singleton and at most one doubleton. */
export function isBalanced(hand: Card[]) {
  const lengths = shape(hand);
  return lengths.every((l) => l >= 2) && lengths.filter((l) => l === 2).length <= 1;
}

export const nextSeat = (seat: Seat): Seat => SEATS[(SEATS.indexOf(seat) + 1) % 4];
export const leftOf = nextSeat;

export function newDeck(): Card[] {
  return ALL_SUITS.flatMap((suit) => RANKS.map((rank) => ({ suit, rank })));
}

/** Four hands of 13 in seat order North, East, South, West. */
export function randomDeal(random: () => number = Math.random): Record<Seat, Card[]> {
  const deck = newDeck();
  for (let i = deck.length - 1; i > 0; i--) {
    const j = Math.floor(random() * (i + 1));
    [deck[i], deck[j]] = [deck[j], deck[i]];
  }
  return { north: deck.slice(0, 13), east: deck.slice(13, 26), south: deck.slice(26, 39), west: deck.slice(39, 52) };
}

/** The cards a player may play: the led suit if they have it, otherwise anything. */
export function legalCards(hand: Card[], ledSuit: Suit | null): Card[] {
  if (!ledSuit) return hand;
  const follow = hand.filter((c) => c.suit === ledSuit);
  return follow.length > 0 ? follow : hand;
}

export function beats(card: Card, current: Card, trump: Suit | null) {
  if (card.suit === current.suit) return rankValue(card.rank) > rankValue(current.rank);
  return card.suit === trump;
}

/** The card winning a (possibly incomplete) trick. */
export function winningPlay(plays: PlayedCard[], trump: Suit | null): PlayedCard {
  return plays.slice(1).reduce((best, p) => (beats(p.card, best.card, trump) ? p : best), plays[0]);
}

export const pick = <T>(items: T[], random: () => number = Math.random): T => items[Math.floor(random() * items.length)];

const PBN_SUITS: Suit[] = ['spades', 'hearts', 'diamonds', 'clubs'];
const PBN_RANKS = '23456789TJQKA';

/** A hand written PBN style, spades first: "AKQ2.J52.T93.842". */
export const parseHand = (pbn: string): Card[] =>
  pbn.split('.').flatMap((ranks, i) => [...ranks].map((r) => ({ suit: PBN_SUITS[i], rank: RANKS[PBN_RANKS.indexOf(r)] })));

/** One card such as "SA" or "H5" (suit letter C/D/H/S, then the rank). */
export const card = (text: string): Card => ({
  suit: ({ C: 'clubs', D: 'diamonds', H: 'hearts', S: 'spades' } as const)[text[0] as 'C' | 'D' | 'H' | 'S'],
  rank: RANKS[PBN_RANKS.indexOf(text[1])],
});

/** A trick written as "W:S5 N:SK E:SA S:S2". */
export const parseTrick = (text: string): PlayedCard[] =>
  text.split(' ').map((p) => ({ seat: ({ N: 'north', E: 'east', S: 'south', W: 'west' } as const)[p[0] as 'N' | 'E' | 'S' | 'W'], card: card(p.slice(2)) }));
