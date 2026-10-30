import { useCallback, useEffect, useState } from 'react';
import { fetchDeal } from './api';
import { VULNERABILITY_NAME } from './cards';
import { HandView } from './components/HandView';
import type { DealInfo, Seat } from './types';

const COMING = [
  { title: 'Lær bridge', text: 'Lektioner fra bunden: stik, trumf, point, meldinger og spilføring.' },
  { title: 'Mini-bridge mod bot', text: 'Spil uden meldinger, så du lærer stik og spilføring først.' },
  { title: 'Fuldt spil mod bot', text: 'Meld og spil med en bot som makker, og få hints undervejs.' },
];

export default function App() {
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

  const hand = (seat: Seat) => deal?.hands.find((h) => h.seat === seat);

  return (
    <main className="page">
      <header className="title">
        <h1>Bridge-træner</h1>
        <p>Lær bridge fra bunden og øv dig mod en bot.</p>
      </header>

      <section className="panel">
        <h2>Se en kortgivning</h2>
        <p className="lead">
          Alle fire hænder vises åbent. Tæl selv honnørpointene (E = 4, K = 3, D = 2, Kn = 1) og se, om du får
          det samme som tallet ved hånden. Der er altid 40 point i alt.
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
            <div className="table">
              {(['north', 'west', 'east', 'south'] as Seat[]).map((seat) => {
                const h = hand(seat);
                return (
                  <div key={seat} className={`slot ${seat}`}>
                    {h && <HandView hand={h} dealer={deal.dealer === seat} />}
                  </div>
                );
              })}
              <div className="slot center" aria-hidden>
                <div className="compass">
                  <span>N</span>
                  <span>V</span>
                  <span>Ø</span>
                  <span>S</span>
                </div>
              </div>
            </div>
          </>
        )}
      </section>

      <section className="panel">
        <h2>På vej</h2>
        <ul className="coming">
          {COMING.map((c) => (
            <li key={c.title}>
              <strong>{c.title}</strong>
              <span>{c.text}</span>
            </li>
          ))}
        </ul>
      </section>
    </main>
  );
}
