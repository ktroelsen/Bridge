import type { Card, PlayedCard, Practice, Seat, Strain, Suit } from '../types';

/** Pictures that go with a text or a question. */
export type Visual =
  | { type: 'hand'; cards: Card[]; label?: string }
  | { type: 'twoHands'; north: Card[]; south: Card[] }
  | { type: 'trick'; plays: PlayedCard[]; trump: Suit | null; noTrumpLabel?: boolean }
  | { type: 'hcpTable'; hcp: Record<Seat, number>; dealer: Seat }
  | { type: 'contract'; level: number; strain: Strain };

export type Answer =
  /** Buttons; any of the correct indices counts as right. */
  | { type: 'options'; options: string[]; correct: number[] }
  /** Click a card in the hand; any of the correct cards counts as right. */
  | { type: 'card'; hand: Card[]; correct: Card[] }
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
  | { type: 'drill'; title: string; intro: string; count: number; generate: () => Question };


export interface Lesson {
  id: string;
  title: string;
  summary: string;
  steps: Step[];
  /** Suggested mini-bridge practice after the lesson. */
  practice?: { text: string; kind: Practice };
}
