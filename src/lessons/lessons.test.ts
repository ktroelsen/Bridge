import { describe, expect, it } from 'vitest';
import { sameCard } from '../cards';
import { LESSONS } from './content';
import type { Question } from './types';

function check(q: Question) {
  const a = q.answer;
  if (a.type === 'options') {
    expect(a.correct.length).toBeGreaterThan(0);
    a.correct.forEach((i) => expect(i).toBeLessThan(a.options.length));
  } else if (a.type === 'card') {
    expect(a.correct.length).toBeGreaterThan(0);
    a.correct.forEach((c) => expect(a.hand.some((h) => sameCard(h, c))).toBe(true));
    if (a.legal) a.correct.forEach((c) => expect(a.legal!.some((l) => sameCard(l, c))).toBe(true));
  } else {
    expect(a.correct).toBeGreaterThanOrEqual(a.min);
    expect(a.correct).toBeLessThanOrEqual(a.max);
  }
  expect(q.explain.length).toBeGreaterThan(10);
}

describe('lessons', () => {
  it('have unique ids that the server accepts', () => {
    const ids = LESSONS.map((l) => l.id);
    expect(new Set(ids).size).toBe(ids.length);
    ids.forEach((id) => expect(id).toMatch(/^[a-z0-9]+(-[a-z0-9]+)*$/));
  });

  it('have valid fixed questions', () => {
    LESSONS.flatMap((l) => l.steps).forEach((s) => s.type === 'question' && check(s.question));
  });

  it('have drills that always make answerable questions', () => {
    for (const lesson of LESSONS)
      for (const step of lesson.steps)
        if (step.type === 'drill') for (let i = 0; i < 60; i++) check(step.generate());
  });
});
