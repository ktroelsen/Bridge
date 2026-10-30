import { useCallback, useEffect, useState } from 'react';
import { fetchDeal } from '../api';
import { VULNERABILITY_NAME } from '../cards';
import type { DealInfo } from '../types';
import { DealTable } from './DealTable';

/** All four hands open, to practise counting high card points. */
export function DealViewer({ onBack }: { onBack: () => void }) {
  const [deal, setDeal] = useState<DealInfo | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [numberInput, setNumberInput] = useState('');

  const load = useCallback(async (number?: number) => {
    setError(null);
    try {
      const next = await fetchDeal(number);
      setDeal(next);
      setNumberInput(String(next.number));
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Noget gik galt.');
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <section className="panel">
      <button type="button" className="back" onClick={onBack}>
        ← Menu
      </button>
      <h2>Tæl point</h2>
      <p className="lead">
        Alle fire hænder vises åbent. Tæl selv honnørpointene (E = 4, K = 3, D = 2, Kn = 1) og se, om du får det
        samme som tallet ved hånden. Der er altid 40 point i alt.
      </p>

      <form
        className="controls"
        onSubmit={(e) => {
          e.preventDefault();
          const n = Number(numberInput);
          if (Number.isInteger(n) && n > 0) void load(n);
        }}
      >
        <button type="button" className="button" onClick={() => void load()}>
          Ny kortgivning
        </button>
        <label className="number">
          Spilnummer
          <input inputMode="numeric" value={numberInput} onChange={(e) => setNumberInput(e.target.value.replace(/\D/g, ''))} />
        </label>
        <button type="submit" className="button secondary">
          Vis
        </button>
      </form>

      {error && <p className="error">{error}</p>}

      {deal && (
        <>
          <p className="meta">
            Spil {deal.number} · {VULNERABILITY_NAME[deal.vulnerability]}
          </p>
          <DealTable deal={deal} />
        </>
      )}
    </section>
  );
}
