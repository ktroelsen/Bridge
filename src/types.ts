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

export type Strain = Suit | 'noTrump';
export type GamePhase = 'redeal' | 'chooseContract' | 'play' | 'done';

export interface ContractChoice {
  strain: Strain;
  game: boolean;
}

export type Practice = 'any' | 'declare' | 'defend' | 'trumpFit' | 'noTrump';

export interface MiniBridgeRequest {
  number: number | null;
  contract: ContractChoice | null;
  cards: Card[];
  /** For a new game: the kind of deal to practise. */
  practice?: Practice;
}

export interface PlayedCard {
  seat: Seat;
  card: Card;
}

export interface Trick {
  leader: Seat;
  plays: PlayedCard[];
  winner: Seat | null;
}

export interface Contract {
  level: number;
  strain: Strain;
  declarer: Seat;
}

export interface MiniBridgeState {
  number: number;
  board: number;
  dealer: Seat;
  vulnerability: Vulnerability;
  phase: GamePhase;
  hcp: { seat: Seat; hcp: number }[];
  declarer: Seat | null;
  dummy: Seat | null;
  contract: Contract | null;
  hands: { seat: Seat; cards: Card[] }[];
  tricks: Trick[];
  toPlay: Seat | null;
  legalCards: Card[];
  tricksNorthSouth: number;
  tricksEastWest: number;
  scoreNorthSouth: number | null;
  fullDeal: DealInfo | null;
}
