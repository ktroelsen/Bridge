import type { Card, PlayedCard, QuizKind, Seat, Strain, Suit } from '../types';

/** Pictures that go with a text or a question. */
export type Visual =
  | { type: 'hand'; cards: Card[]; label?: string }
  | { type: 'twoHands'; north: Card[]; south: Card[] }
  | { type: 'trick'; plays: PlayedCard[]; trump: Suit | null; noTrumpLabel?: boolean }
  | { type: 'hcpTable'; hcp: Record<Seat, number>; dealer: Seat }
  | { type: 'contract'; level: number; strain: Strain }
  /** An auction so far, e.g. dealer South and ["1H", "P", "1S", "P"]; "?" marks South's turn. */
  | { type: 'auction'; dealer: Seat; calls: string[] }
  | { type: 'auctionAndHand'; dealer: Seat; calls: string[]; cards: Card[] };

export type Answer =
  /** Buttons; any of the correct indices counts as right. */
  | { type: 'options'; options: string[]; correct: number[] }
  /** Click a card in the hand; any of the correct cards counts as right. Only `legal` cards (default: all) can be clicked. */
  | { type: 'card'; hand: Card[]; correct: Card[]; legal?: Card[] }
  /** A number (buttons for small ranges, otherwise an input field). */
  | { type: 'number'; min: number; max: number; correct: number };

export interface Question {
  prompt: string;
  visual?: Visual;
  answer: Answer;
  /** Shown after answering, right or wrong: why the right answer is right. */
  explain: string;
}

export type Step =
  | { type: 'text'; title: string; body: string[]; visual?: Visual }
  | { type: 'question'; question: Question }
  | { type: 'drill'; title: string; intro: string; count: number; generate: () => Question }
  /** "What do you bid?" questions from the server, answered by the bidding system. */
  | { type: 'bidding'; title: string; intro: string; count: number; quiz: QuizKind }
  /** "Find the best card": declarer play problems from the server, all hands open. */
  | { type: 'play'; title: string; intro: string; count: number; stage: 'early' | 'middle' | 'defend' };


export interface Lesson {
  id: string;
  title: string;
  summary: string;
  steps: Step[];
  /** Suggested practice after the lesson: a text and the page to go to (e.g. "/minibridge?ov=declare"). */
  practice?: { text: string; path: string; button: string };
}
