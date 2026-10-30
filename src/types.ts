export type Suit = 'clubs' | 'diamonds' | 'hearts' | 'spades';
export type Rank =
  | 'two' | 'three' | 'four' | 'five' | 'six' | 'seven' | 'eight' | 'nine' | 'ten'
  | 'jack' | 'queen' | 'king' | 'ace';
export type Seat = 'north' | 'east' | 'south' | 'west';
export type Vulnerability = 'none' | 'northSouth' | 'eastWest' | 'both';

export interface Card {
  suit: Suit;
  rank: Rank;
}

export interface HandInfo {
  seat: Seat;
  hcp: number;
  balanced: boolean;
  cards: Card[];
}

export interface DealInfo {
  number: number;
  board: number;
  dealer: Seat;
  vulnerability: Vulnerability;
  hands: HandInfo[];
}
