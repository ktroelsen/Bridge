import { useState } from 'react';
import { isRed, RANK_LABEL, SUIT_NAME, SUIT_SYMBOL, SUITS, cardsInSuit, sameCard } from '../cards';
import type { Question } from '../lessons/types';
import type { Card } from '../types';
import { LessonVisual } from './LessonVisual';
import { Rich } from './Rich';

type Given = { option: number } | { card: Card } | { number: number };

/** One question: the visual, the answer buttons and, once answered, right/wrong with the explanation. */
export function QuestionView({ question, onAnswer, answered }: { question: Question; onAnswer: (correct: boolean) => void; answered: boolean }) {
  const [given, setGiven] = useState<Given | null>(null);
  const [numberInput, setNumberInput] = useState('');
  const a = question.answer;

  const isCorrect = (g: Given) =>
    'option' in g && a.type === 'options'
      ? a.correct.includes(g.option)
      : 'card' in g && a.type === 'card'
        ? a.correct.some((c) => sameCard(c, g.card))
        : 'number' in g && a.type === 'number'
          ? g.number === a.correct
          : false;

  const answer = (g: Given) => {
    if (answered) return;
    setGiven(g);
    onAnswer(isCorrect(g));
  };

  const right = given !== null && isCorrect(given);

  return (
    <div className="question">
      <p className="prompt">
        <Rich text={question.prompt} />
      </p>
      {question.visual && <LessonVisual visual={question.visual} />}

      {a.type === 'options' && (
        <div className="answer-options">
          {a.options.map((label, i) => {
            const state = !answered ? '' : a.correct.includes(i) ? ' correct' : given && 'option' in given && given.option === i ? ' wrong' : '';
            return (
              <button key={label} type="button" className={`answer${state}`} disabled={answered} onClick={() => answer({ option: i })}>
                <Rich text={label} />
              </button>
            );
          })}
        </div>
      )}

      {a.type === 'card' && (
        <div className="play-hand large card-answer">
          {SUITS.map((suit) => {
            const cards = cardsInSuit(a.hand, suit);
            return (
              <div key={suit} className="suit-row">
                <span className={isRed(suit) ? 'symbol red' : 'symbol'} aria-label={SUIT_NAME[suit]}>
                  {SUIT_SYMBOL[suit]}
                </span>
                {cards.length === 0 && <span className="void">—</span>}
                {cards.map((card) => {
                  const chosen = given && 'card' in given && sameCard(given.card, card);
                  const ok = a.correct.some((c) => sameCard(c, card));
                  const state = !answered ? ' playable' : ok ? ' correct' : chosen ? ' wrong' : ' dim';
                  return (
                    <button
                      key={card.rank}
                      type="button"
                      className={`card-chip${isRed(suit) ? ' red' : ''}${state}`}
                      disabled={answered}
                      onClick={() => answer({ card })}
                      aria-label={`${SUIT_NAME[suit]} ${RANK_LABEL[card.rank]}`}
                    >
                      {RANK_LABEL[card.rank]}
                    </button>
                  );
                })}
              </div>
            );
          })}
        </div>
      )}

      {a.type === 'number' &&
        (a.max - a.min <= 14 ? (
          <div className="answer-options numbers">
            {Array.from({ length: a.max - a.min + 1 }, (_, i) => a.min + i).map((n) => {
              const state = !answered ? '' : n === a.correct ? ' correct' : given && 'number' in given && given.number === n ? ' wrong' : '';
              return (
                <button key={n} type="button" className={`answer${state}`} disabled={answered} onClick={() => answer({ number: n })}>
                  {n}
                </button>
              );
            })}
          </div>
        ) : (
          <form
            className="controls"
            onSubmit={(e) => {
              e.preventDefault();
              const n = Number(numberInput);
              if (numberInput !== '' && Number.isInteger(n)) answer({ number: n });
            }}
          >
            <label className="number">
              Dit svar
              <input inputMode="numeric" autoFocus value={numberInput} disabled={answered} onChange={(e) => setNumberInput(e.target.value.replace(/\D/g, ''))} />
            </label>
            <button type="submit" className="button" disabled={answered || numberInput === ''}>
              Svar
            </button>
          </form>
        ))}

      {answered && given && (
        <div className={right ? 'feedback right' : 'feedback wrong'} role="status">
          <strong>{right ? 'Rigtigt!' : a.type === 'number' ? `Ikke helt. Svaret er ${a.correct}.` : 'Ikke helt.'}</strong> <Rich text={question.explain} />
        </div>
      )}
    </div>
  );
}
