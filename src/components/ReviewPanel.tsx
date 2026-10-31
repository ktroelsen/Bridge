import { useState } from 'react';
import { cardLabel, SEAT_NAME } from '../cards';
import type { Contract, Mistake, Review } from '../types';
import { Rich } from './Rich';

const list = (cards: { suit: Mistake['card']['suit']; rank: Mistake['card']['rank'] }[]) =>
  cards.length <= 3 ? cards.map(cardLabel).join(' eller ') : `${cards.slice(0, 3).map(cardLabel).join(', ')} m.fl.`;

const tricks = (n: number) => (n === 1 ? '1 stik' : `${n} stik`);

function line(m: Mistake, dummy: string) {
  const fromDummy = m.by === 'you' && m.seat === dummy ? ' fra blinden' : '';
  if (m.by === 'you')
    return `**Stik ${m.trick}:** Du spillede ${cardLabel(m.card)}${fromDummy}. Med ${list(m.better)} kunne I have fået ${tricks(m.cost)} mere.`;
  if (m.by === 'partner')
    return `**Stik ${m.trick}:** ${SEAT_NAME[m.seat]} (makker) spillede ${cardLabel(m.card)}. ${list(m.better)} ville have givet ${tricks(m.cost)} mere.`;
  return `**Stik ${m.trick}:** ${SEAT_NAME[m.seat]} spillede ${cardLabel(m.card)} og gav jer ${tricks(m.cost)}.`;
}

/** "Gennemgå spillet": the double dummy review of a finished deal. */
export function ReviewPanel({ contract, load }: { contract: Contract; load: () => Promise<Review> }) {
  const [review, setReview] = useState<Review | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const dummy = ({ north: 'south', south: 'north', east: 'west', west: 'east' } as const)[contract.declarer];

  if (!review)
    return (
      <div className="review">
        <button
          type="button"
          className="button secondary"
          disabled={busy}
          onClick={async () => {
            setBusy(true);
            setError(null);
            try {
              setReview(await load());
            } catch (e) {
              setError(e instanceof Error ? e.message : 'Analysen mislykkedes.');
            } finally {
              setBusy(false);
            }
          }}
        >
          {busy ? 'Analyserer …' : 'Gennemgå spillet'}
        </button>
        {error && <p className="error">{error}</p>}
      </div>
    );

  const mine = review.mistakes.filter((m) => m.by === 'you');
  const others = review.mistakes.filter((m) => m.by !== 'you');
  const declarer = SEAT_NAME[contract.declarer];

  return (
    <div className="review">
      <h3>Gennemgang</h3>
      <p>
        Med perfekt spil fra alle fire kunne {declarer} tage <strong>{review.declarerTricksPerfect} stik</strong>. {declarer} fik{' '}
        {review.declarerTricks}.
      </p>
      {mine.length === 0 ? (
        <p className="feedback right">Du gav ingen stik væk. Flot spillet!</p>
      ) : (
        <ul className="mistakes">
          {mine.map((m) => (
            <li key={`${m.trick}-${m.seat}`}>
              <Rich text={line(m, dummy)} />
            </li>
          ))}
        </ul>
      )}
      {others.length > 0 && (
        <>
          <p className="muted small">De andre ved bordet:</p>
          <ul className="mistakes others">
            {others.map((m) => (
              <li key={`${m.trick}-${m.seat}`}>
                <Rich text={line(m, dummy)} />
              </li>
            ))}
          </ul>
        </>
      )}
      {!review.complete && <p className="muted small">Analysen nåede ikke hele spillet.</p>}
      <p className="muted small">
        Analysen regner med, at alle kan se alle kort ("double dummy"). Den er derfor strengere end virkeligheden: nogle
        "fejl" kunne man ikke have undgået uden at kende de andres kort.
      </p>
    </div>
  );
}
