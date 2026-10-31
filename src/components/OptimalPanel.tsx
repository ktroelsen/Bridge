import { useState } from 'react';
import { gameLevel, isNorthSouth, SEAT_NAME, STRAIN_LABEL, STRAINS } from '../cards';
import { levelFor, score } from '../scoring';
import type { Contract, Seat, Strain, Vulnerability } from '../types';
import { contractText } from './CardTable';
import { Rich } from './Rich';

const SEATS: Seat[] = ['north', 'south', 'east', 'west'];
const INDEX: Record<Seat, number> = { north: 0, east: 1, south: 2, west: 3 };

type Table = Partial<Record<Strain, number[]>>;

function best(table: Table, ns: boolean, vulnerable: boolean) {
  let top: { level: number; strain: Strain; declarer: Seat; score: number; tricks: number } | null = null;
  for (const strain of STRAINS)
    for (const seat of SEATS.filter((s) => isNorthSouth(s) === ns)) {
      const tricks = table[strain]?.[INDEX[seat]] ?? -1;
      if (tricks < 7) continue;
      const level = levelFor(strain, tricks);
      const s = score(level, strain, vulnerable, tricks);
      if (!top || s > top.score) top = { level, strain, declarer: seat, score: s, tricks };
    }
  return top;
}

/** "Hvad kunne vindes?": the double dummy table, strain by strain, and the best contract for each side. */
export function OptimalPanel({
  load,
  contract,
  vulnerability,
  scoreNorthSouth,
}: {
  load: (strain: Strain) => Promise<{ tricks: number[] }>;
  contract: Contract;
  vulnerability: Vulnerability;
  scoreNorthSouth: number;
}) {
  const [table, setTable] = useState<Table | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const run = async () => {
    setBusy(true);
    setError(null);
    setTable({});
    try {
      for (const strain of STRAINS) {
        const row = await load(strain);
        setTable((t) => ({ ...t, [strain]: row.tricks }));
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Beregningen mislykkedes.');
    } finally {
      setBusy(false);
    }
  };

  if (!table)
    return (
      <div className="review">
        <button type="button" className="button secondary" onClick={run}>
          Hvad kunne vindes?
        </button>
      </div>
    );

  const done = !busy && STRAINS.every((s) => table[s]);
  const nsVul = vulnerability === 'both' || vulnerability === 'northSouth';
  const ewVul = vulnerability === 'both' || vulnerability === 'eastWest';
  const ns = done ? best(table, true, nsVul) : null;
  const ew = done ? best(table, false, ewVul) : null;
  const describe = (b: NonNullable<ReturnType<typeof best>>, sideName: string) =>
    `**${sideName}:** ${contractText({ level: b.level, strain: b.strain })} med ${SEAT_NAME[b.declarer]} som spilfører (${b.tricks} stik, ${b.score > 0 ? '+' : ''}${b.score}).`;

  return (
    <div className="review">
      <h3>Hvad kunne vindes?</h3>
      <p className="muted small">Stik til spilføreren med perfekt spil, for hver spilfører og trumffarve.</p>
      <table className="dd-table">
        <thead>
          <tr>
            <th />
            {STRAINS.map((s) => (
              <th key={s} className={s === 'hearts' || s === 'diamonds' ? 'red' : ''}>
                {STRAIN_LABEL[s]}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {SEATS.map((seat) => (
            <tr key={seat}>
              <th>{SEAT_NAME[seat]}</th>
              {STRAINS.map((s) => {
                const t = table[s]?.[INDEX[seat]];
                const game = t !== undefined && t >= gameLevel(s) + 6 && t < 12;
                return (
                  <td key={s} className={t === undefined ? 'pending' : t >= 12 ? 'slam' : game ? 'game' : ''}>
                    {t === undefined ? (busy ? '…' : '') : t < 0 ? '–' : t}
                  </td>
                );
              })}
            </tr>
          ))}
        </tbody>
      </table>
      {busy && <p className="muted small">Regner …</p>}
      {error && <p className="error">{error}</p>}
      {done && (
        <>
          <p>
            <Rich text={ns ? describe(ns, 'Bedst for Nord-Syd') : '**Nord-Syd** kan ikke vinde nogen kontrakt.'} />
            <br />
            <Rich text={ew ? describe(ew, 'Bedst for Øst-Vest') : '**Øst-Vest** kan ikke vinde nogen kontrakt.'} />
          </p>
          <p>
            Spillet blev {contractText(contract)} af {SEAT_NAME[contract.declarer]}, og Nord-Syd fik {scoreNorthSouth > 0 ? '+' : ''}
            {scoreNorthSouth}.
          </p>
          {STRAINS.some((s) => table[s]?.some((x) => x < 0)) && <p className="muted small">"–" betyder, at beregningen tog for lang tid.</p>}
        </>
      )}
    </div>
  );
}
