import { describe, expect, it } from 'vitest';
import { levelFor, score } from './scoring';

describe('score', () => {
  it('matches the server for known results', () => {
    expect(score(1, 'clubs', false, 7)).toBe(70);
    expect(score(2, 'hearts', false, 8)).toBe(110);
    expect(score(1, 'noTrump', false, 8)).toBe(120);
    expect(score(3, 'noTrump', true, 9)).toBe(600);
    expect(score(4, 'spades', true, 11)).toBe(650);
    expect(score(6, 'spades', false, 12)).toBe(980);
    expect(score(7, 'noTrump', true, 13)).toBe(2220);
    expect(score(3, 'noTrump', true, 7)).toBe(-200);
  });
  it('bids game, or slam with 12 tricks', () => {
    expect(levelFor('spades', 11)).toBe(4);
    expect(levelFor('clubs', 11)).toBe(5);
    expect(levelFor('noTrump', 8)).toBe(2);
    expect(levelFor('hearts', 12)).toBe(6);
  });
});
