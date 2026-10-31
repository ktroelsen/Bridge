import { useEffect, useState } from 'react';
import { fetchStats } from '../api';
import { callIsRed, callLabel, SEAT_NAME } from '../cards';
import type { PlayerState } from '../player';
import type { BotLevel, GameRecord, Stats } from '../types';
import { NameForm } from './LessonList';

const LEVEL_NAME: Record<BotLevel, string> = { easy: 'Let', medium: 'Mellem', hard: 'Svær' };
const pct = (a: number, b: number) => (b === 0 ? '–' : `${Math.round((100 * a) / b)} %`);

/** Starts a deal again: stores it as the current game of its kind and opens the page. */
function playAgain(g: GameRecord, navigate: (path: string) => void) {
  try {
    localStorage.setItem('bridge.level', g.level);
    if (g.mode === 'full') localStorage.setItem('bridge.game', JSON.stringify({ level: g.level, number: g.number, calls: [], cards: [] }));
    else localStorage.setItem('bridge.minibridge', JSON.stringify({ level: g.level, number: g.number, contract: null, cards: [] }));
  } catch {
    /* the page then just starts a new deal */
  }
  navigate(g.mode === 'full' ? '/spil' : '/minibridge');
}

export function StatsView({ player, navigate }: { player: PlayerState; navigate: (path: string) => void }) {
  const [stats, setStats] = useState<Stats | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!player.name) return;
    let live = true;
    fetchStats(player.name)
      .then((s) => live && setStats(s))
      .catch((e) => live && setError(e instanceof Error ? e.message : 'Kunne ikke hente statistikken.'));
    return () => {
      live = false;
    };
  }, [player.name]);

  return (
    <>
      {!player.name && <NameForm player={player} />}
      <section className="panel">
        <button type="button" className="back" onClick={() => navigate('/')}>
          ← Menu
        </button>
        <h2>Statistik</h2>
        {!player.name && <p className="lead">Giv dit navn, så gemmes dine spil, og du kan følge med i, hvordan det går.</p>}
        {error && <p className="error">{error}</p>}
        {player.name && !stats && !error && <p className="muted">Henter …</p>}
        {stats && stats.games === 0 && <p className="lead">Du har ikke spillet nogen spil endnu. Spil mini-bridge eller bridge, så kommer de her.</p>}
        {stats && stats.games > 0 && (
          <>
            <p className="lead">
              {stats.games} spil i alt{stats.passedOut ? `, heraf ${stats.passedOut} passet ud` : ''}.
            </p>
            <table className="stats-table">
              <thead>
                <tr>
                  <th>Botter</th>
                  <th>Spil</th>
                  <th>Vundet som spilfører</th>
                  <th>Sat i modspil</th>
                  <th>Point</th>
                </tr>
              </thead>
              <tbody>
                {stats.byLevel.map((g) => (
                  <tr key={g.level}>
                    <th>{LEVEL_NAME[g.level]}</th>
                    <td>{g.games}</td>
                    <td>
                      {g.made} af {g.declared} ({pct(g.made, g.declared)})
                    </td>
                    <td>
                      {g.beaten} af {g.defended} ({pct(g.beaten, g.defended)})
                    </td>
                    <td>{g.totalScore > 0 ? `+${g.totalScore}` : g.totalScore}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <h3>Seneste spil</h3>
            <ul className="recent">
              {stats.recent.map((g) => (
                <li key={g.id}>
                  <span className="muted small">
                    {new Date(g.finishedAt).toLocaleDateString('da-DK')} · {g.mode === 'full' ? 'Bridge' : 'Mini-bridge'} · {LEVEL_NAME[g.level]}
                  </span>
                  <span>
                    {g.contract ? (
                      <>
                        <strong className={callIsRed(g.contract) ? 'red' : ''}>{callLabel(g.contract)}</strong> af {SEAT_NAME[g.declarer as keyof typeof SEAT_NAME]}:{' '}
                        {g.declarerTricks} stik, {g.made ? 'vundet' : 'gik ned'}.{' '}
                        <span className={g.scoreNorthSouth >= 0 ? 'plus' : 'minus'}>
                          {g.scoreNorthSouth > 0 ? '+' : ''}
                          {g.scoreNorthSouth}
                        </span>
                      </>
                    ) : (
                      'Passet ud.'
                    )}
                  </span>
                  <button type="button" className="link" onClick={() => playAgain(g, navigate)}>
                    Spil igen
                  </button>
                </li>
              ))}
            </ul>
          </>
        )}
      </section>
    </>
  );
}
