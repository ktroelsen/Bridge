import { useEffect, useState } from 'react';
import { fetchPlayQuestion } from '../api';
import { cardLabel, cardsInSuit, isRed, RANK_LABEL, sameCard, SEAT_NAME, SUIT_NAME, SUIT_SYMBOL, SUITS } from '../cards';
import type { Card, PlayQuestion, Seat } from '../types';
import { contractText } from './CardTable';
import { PlayHand } from './PlayHand';

/** One hand as suit lines (read only). */
function OpenHand({ seat, cards, label }: { seat: Seat; cards: Card[]; label?: string }) {
  return (
    <section className="hand" aria-label={`${SEAT_NAME[seat]}s kort`}>
      <header className="hand-header">
        <span className="seat">{SEAT_NAME[seat]}</span>
        {label && <span className="role">{label}</span>}
      </header>
      {SUITS.map((suit) => {
        const inSuit = cardsInSuit(cards, suit);
        return (
          <div key={suit} className="suit-line">
            <span className={isRed(suit) ? 'symbol red' : 'symbol'} aria-label={SUIT_NAME[suit]}>
              {SUIT_SYMBOL[suit]}
            </span>
            <span className="ranks">{inSuit.length ? inSuit.map((c) => RANK_LABEL[c.rank]).join(' ') : '—'}</span>
          </div>
        );
      })}
    </section>
  );
}

const tricks = (n: number) => (n === 1 ? '1 stik' : `${n} stik`);

/** "Find the best card": all four hands open; the player picks a card from the hand on lead. */
export function PlayQuestionView({ stage, answered, onAnswer }: { stage: 'early' | 'middle'; answered: boolean; onAnswer: (correct: boolean) => void }) {
  const [q, setQ] = useState<PlayQuestion | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [given, setGiven] = useState<Card | null>(null);

  useEffect(() => {
    let live = true;
    fetchPlayQuestion(stage)
      .then((x) => live && setQ(x))
      .catch((e) => live && setError(e instanceof Error ? e.message : 'Kunne ikke hente opgaven.'));
    return () => {
      live = false;
    };
  }, [stage]);

  if (error) return <p className="error">{error}</p>;
  if (!q) return <p className="muted">Finder en opgave …</p>;

  const needed = q.contract.level + 6;
  const handOf = (seat: Seat) => q.hands.find((h) => h.seat === seat)?.cards ?? [];
  const dummy: Seat = q.contract.declarer === 'south' ? 'north' : 'south';
  const role = (seat: Seat) => (seat === q.contract.declarer ? 'Spilfører' : seat === dummy ? 'Blind' : undefined);
  const right = given !== null && q.best.some((b) => sameCard(b, given));
  const givenTricks = given ? q.values.find((v) => sameCard(v.card, given))?.tricks : undefined;
  const lead = q.toPlay;

  return (
    <div className="question">
      <p className="prompt">
        Kontrakt {contractText(q.contract)} ({tricks(needed)}). {q.trick - 1} stik er spillet: I har taget {q.tricksNorthSouth}, modstanderne{' '}
        {q.tricksEastWest}. {lead === q.contract.declarer ? 'Spilføreren' : 'Blinden'} ({SEAT_NAME[lead]}) skal spille ud, og du spiller begge
        hænder. Hvilket kort giver flest stik?
      </p>
      <div className="table">
        {(['north', 'west', 'east', 'south'] as Seat[]).map((seat) => (
          <div key={seat} className={`slot ${seat}`}>
            {seat !== lead && <OpenHand seat={seat} cards={handOf(seat)} label={role(seat)} />}
            {seat === lead && (
              <div className={`seat-box ${answered ? '' : 'active'}`}>
                <div className="seat-label">
                  <strong>{SEAT_NAME[seat]}</strong>
                  {role(seat) && <span className="role">{role(seat)}</span>}
                </div>
                <PlayHand
                  cards={handOf(seat)}
                  legal={q.legalCards}
                  active={!answered}
                  best={answered ? q.best : undefined}
                  onPlay={(card) => {
                    setGiven(card);
                    onAnswer(q.best.some((b) => sameCard(b, card)));
                  }}
                />
              </div>
            )}
          </div>
        ))}
        <div className="slot center" aria-hidden>
          <div className="compass">
            <span>N</span>
            <span>V</span>
            <span>Ø</span>
            <span>S</span>
          </div>
        </div>
      </div>
      {answered && given && (
        <div className={right ? 'feedback right' : 'feedback wrong'} role="status">
          <strong>{right ? `Rigtigt: ${cardLabel(given)}.` : `${cardLabel(given)} giver ${tricks(givenTricks ?? 0)}.`}</strong>{' '}
          {right ? '' : `Bedst er ${q.best.slice(0, 3).map(cardLabel).join(' eller ')}${q.best.length > 3 ? ' m.fl.' : ''}. `}
          Med det bedste kort tager I {tricks(q.bestTricks)} i alt, når alle spiller perfekt
          {q.bestTricks >= needed ? ', og kontrakten vindes.' : ` (kontrakten kræver ${needed}).`}
        </div>
      )}
    </div>
  );
}
