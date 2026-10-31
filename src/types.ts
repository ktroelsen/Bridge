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
export type GamePhase = 'redeal' | 'chooseContract' | 'bidding' | 'passedOut' | 'play' | 'done';

export interface ContractChoice {
  strain: Strain;
  game: boolean;
}

export type Practice = 'any' | 'declare' | 'defend' | 'trumpFit' | 'noTrump';

export type BotLevel = 'easy' | 'medium' | 'hard';

export interface MiniBridgeRequest {
  level?: BotLevel;
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
  penalty?: 'none' | 'doubled' | 'redoubled';
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

export interface CallInfo {
  seat: Seat;
  /** "1H", "3N", "P", "X", "XX" */
  call: string;
  explanation: string;
  /** For the player's own calls: what the system would have bid, when different. */
  systemCall: string | null;
  systemExplanation: string | null;
}

export interface GameRequest {
  level?: BotLevel;
  number: number | null;
  calls: string[];
  cards: Card[];
}

export interface GameState {
  number: number;
  board: number;
  dealer: Seat;
  vulnerability: Vulnerability;
  phase: GamePhase;
  southHcp: number;
  auction: CallInfo[];
  legalCalls: string[];
  hint: { call: string; explanation: string } | null;
  contract: Contract | null;
  dummy: Seat | null;
  hands: { seat: Seat; cards: Card[] }[];
  tricks: Trick[];
  toPlay: Seat | null;
  legalCards: Card[];
  tricksNorthSouth: number;
  tricksEastWest: number;
  scoreNorthSouth: number | null;
  fullDeal: DealInfo | null;
}

export type QuizKind = 'opening' | 'response' | 'ntResponse' | 'rebid' | 'ntOpener' | 'overcall' | 'advance';

export interface BiddingQuestion {
  number: number;
  dealer: Seat;
  auction: { seat: Seat; call: string; explanation: string }[];
  hand: Card[];
  hcp: number;
  legalCalls: string[];
  answer: { call: string; explanation: string };
}

export interface PlayHint {
  cards: Card[];
  /** Tricks the player's side takes in total with perfect play from here. */
  sideTricks: number;
}

export interface Mistake {
  trick: number;
  seat: Seat;
  by: 'you' | 'partner' | 'opponents';
  card: Card;
  cost: number;
  better: Card[];
}

export interface Review {
  declarerTricksPerfect: number;
  declarerTricks: number;
  complete: boolean;
  mistakes: Mistake[];
}

export interface PlayQuestion {
  number: number;
  contract: Contract;
  toPlay: Seat;
  /** Tricks played so far. */
  trick: number;
  tricksNorthSouth: number;
  tricksEastWest: number;
  hands: { seat: Seat; cards: Card[] }[];
  legalCards: Card[];
  best: Card[];
  bestTricks: number;
  values: { card: Card; tricks: number }[];
}

export interface GameRecord {
  id: number;
  mode: 'mini' | 'full';
  number: number;
  level: BotLevel;
  /** Like "4S"; empty when passed out. */
  contract: string;
  declarer: Seat | '';
  ourContract: boolean;
  declarerTricks: number;
  made: boolean;
  scoreNorthSouth: number;
  finishedAt: string;
}

export interface StatsGroup {
  level: BotLevel;
  games: number;
  declared: number;
  made: number;
  defended: number;
  beaten: number;
  totalScore: number;
}

export interface Stats {
  games: number;
  passedOut: number;
  byLevel: StatsGroup[];
  recent: GameRecord[];
}

export type ChallengeMode = 'mini' | 'full';

export interface BoardResult {
  name: string;
  contract: string | null;
  declarer: Seat | '' | null;
  declarerTricks: number | null;
  made: boolean | null;
  scoreNorthSouth: number | null;
}

export interface Challenge {
  code: string;
  mode: ChallengeMode;
  level: BotLevel;
  createdBy: string;
  createdAt: string;
  boards: { index: number; number: number; playedByViewer: boolean; results: BoardResult[] }[];
  standings: { name: string; played: number; compared: number; totalScore: number; percent: number | null }[];
}

export interface ChallengeSummary {
  code: string;
  mode: ChallengeMode;
  level: BotLevel;
  createdBy: string;
  createdAt: string;
  games: number;
  playedByMe: number;
  players: number;
}

/** A challenge deal being played: which challenge, which deal and at what level. */
export interface ChallengeGame {
  code: string;
  number: number;
  index: number;
  count: number;
  level: BotLevel;
}
