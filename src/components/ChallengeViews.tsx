import { useEffect, useState } from 'react';
import { createChallenge, fetchChallenge, fetchMyChallenges } from '../api';
import { callIsRed, callLabel, SEAT_NAME } from '../cards';
import type { PlayerState } from '../player';
import type { BotLevel, Challenge, ChallengeMode, ChallengeSummary, Seat } from '../types';
import { GameView } from './GameView';
import { NameForm } from './LessonList';
import { MiniBridgeView } from './MiniBridgeView';

const LEVEL_NAME: Record<BotLevel, string> = { easy: 'Let', medium: 'Mellem', hard: 'Svær' };
const MODE_NAME: Record<ChallengeMode, string> = { mini: 'Mini-bridge', full: 'Bridge' };
const signed = (n: number) => (n > 0 ? `+${n}` : String(n));

/** "Udfordr en ven": create a challenge, enter a code, or open one of your own. */
export function ChallengesPage({ player, navigate }: { player: PlayerState; navigate: (path: string) => void }) {
  const [mine, setMine] = useState<ChallengeSummary[] | null>(null);
  const [mode, setMode] = useState<ChallengeMode>('full');
  const [level, setLevel] = useState<BotLevel>('easy');
  const [games, setGames] = useState(8);
  const [code, setCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!player.name) return;
    fetchMyChallenges(player.name).then(setMine).catch(() => setMine([]));
  }, [player.name]);

  const create = async () => {
    if (!player.name) return;
    setBusy(true);
    setError(null);
    try {
      const c = await createChallenge(player.name, mode, level, games);
      navigate(`/udfordring/${c.code}`);
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Kunne ikke oprette udfordringen.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      {!player.name && <NameForm player={player} />}
      <section className="panel">
        <button type="button" className="back" onClick={() => navigate('/')}>
          ← Menu
        </button>
        <h2>Udfordr en ven</h2>
        <p className="lead">
          Alle i en udfordring spiller de samme spil mod botterne, hver for sig og når det passer dem. Bagefter sammenlignes
          resultaterne spil for spil, som i en bridgeturnering.
        </p>

        <form
          className="join"
          onSubmit={(e) => {
            e.preventDefault();
            if (code.trim()) navigate(`/udfordring/${code.trim().toUpperCase()}`);
          }}
        >
          <label className="number">
            Har du fået en kode?
            <input value={code} maxLength={8} onChange={(e) => setCode(e.target.value.toUpperCase())} placeholder="fx K7Q2MX" />
          </label>
          <button type="submit" className="button secondary" disabled={!code.trim()}>
            Åbn
          </button>
        </form>

        <h3>Ny udfordring</h3>
        <div className="choice-row" role="group" aria-label="Spilform">
          {(['full', 'mini'] as ChallengeMode[]).map((m) => (
            <button key={m} type="button" className={`level${mode === m ? ' selected' : ''}`} onClick={() => setMode(m)}>
              {MODE_NAME[m]}
            </button>
          ))}
        </div>
        <div className="choice-row" role="group" aria-label="Botter">
          {(['easy', 'medium', 'hard'] as BotLevel[]).map((l) => (
            <button key={l} type="button" className={`level${level === l ? ' selected' : ''}`} onClick={() => setLevel(l)}>
              {LEVEL_NAME[l]}
            </button>
          ))}
        </div>
        <div className="choice-row" role="group" aria-label="Antal spil">
          {[4, 8, 12].map((n) => (
            <button key={n} type="button" className={`level${games === n ? ' selected' : ''}`} onClick={() => setGames(n)}>
              {n} spil
            </button>
          ))}
        </div>
        <div className="controls">
          <button type="button" className="button" onClick={create} disabled={busy || !player.name}>
            Opret udfordring
          </button>
        </div>
        {!player.name && <p className="muted small">Giv dit navn ovenfor for at oprette en udfordring.</p>}
        {error && <p className="error">{error}</p>}

        {mine && mine.length > 0 && (
          <>
            <h3>Dine udfordringer</h3>
            <ul className="recent">
              {mine.map((c) => (
                <li key={c.code}>
                  <span className="muted small">
                    {new Date(c.createdAt).toLocaleDateString('da-DK')} · {MODE_NAME[c.mode]} · {LEVEL_NAME[c.level]} · oprettet af {c.createdBy}
                  </span>
                  <span>
                    <strong>{c.code}</strong>: du har spillet {c.playedByMe} af {c.games} · {c.players} {c.players === 1 ? 'spiller' : 'spillere'}
                  </span>
                  <button type="button" className="link" onClick={() => navigate(`/udfordring/${c.code}`)}>
                    Åbn
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

/** One challenge: share link, standings and the deals. */
export function ChallengeView({ code, player, navigate }: { code: string; player: PlayerState; navigate: (path: string) => void }) {
  const [challenge, setChallenge] = useState<Challenge | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  useEffect(() => {
    let live = true;
    fetchChallenge(code, player.name)
      .then((c) => live && setChallenge(c))
      .catch((e) => live && setError(e instanceof Error ? e.message : 'Kunne ikke hente udfordringen.'));
    return () => {
      live = false;
    };
  }, [code, player.name]);

  const link = `${window.location.origin}/#/udfordring/${code}`;
  const copy = async () => {
    try {
      await navigator.clipboard.writeText(link);
      setCopied(true);
    } catch {
      /* the link is shown, so it can be copied by hand */
    }
  };

  const played = challenge?.boards.filter((b) => b.playedByViewer).length ?? 0;

  return (
    <>
      {!player.name && <NameForm player={player} />}
      <section className="panel">
        <button type="button" className="back" onClick={() => navigate('/udfordring')}>
          ← Udfordringer
        </button>
        {error && <p className="error">{error}</p>}
        {!challenge && !error && <p className="muted">Henter …</p>}
        {challenge && (
          <>
            <h2>Udfordring {challenge.code}</h2>
            <p className="lead">
              {MODE_NAME[challenge.mode]} · {challenge.boards.length} spil · botter: {LEVEL_NAME[challenge.level]} · oprettet af {challenge.createdBy}
            </p>
            <div className="share">
              <span className="muted small">Del med dine venner:</span>
              <code>{link}</code>
              <button type="button" className="button secondary" onClick={copy}>
                {copied ? 'Kopieret' : 'Kopiér link'}
              </button>
            </div>

            {played > 0 && (
              <>
                <h3>Stilling</h3>
                <p className="muted small">Sammenlignet på de {played} spil, du selv har spillet. Procent er matchpoint: hvor mange du slog spil for spil.</p>
                <table className="stats-table">
                  <thead>
                    <tr>
                      <th>Spiller</th>
                      <th>Spillet</th>
                      <th>Matchpoint</th>
                      <th>Point</th>
                    </tr>
                  </thead>
                  <tbody>
                    {challenge.standings.map((s) => (
                      <tr key={s.name} className={s.name.toLowerCase() === player.name?.toLowerCase() ? 'me' : ''}>
                        <th>{s.name}</th>
                        <td>
                          {s.played} af {challenge.boards.length}
                        </td>
                        <td>{s.percent === null ? '–' : `${s.percent} %`}</td>
                        <td>{s.compared ? signed(s.totalScore) : '–'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </>
            )}

            <h3>Spillene</h3>
            <ul className="recent">
              {challenge.boards.map((b) => (
                <li key={b.number}>
                  <span>
                    <strong>Spil {b.index}</strong>{' '}
                    {b.playedByViewer ? '' : b.results.length ? `· spillet af ${b.results.map((r) => r.name).join(', ')}` : ''}
                  </span>
                  {b.playedByViewer &&
                    b.results.map((r) => (
                      <span key={r.name} className="result-line">
                        {r.name}:{' '}
                        {r.contract ? (
                          <>
                            <strong className={callIsRed(r.contract) ? 'red' : ''}>{callLabel(r.contract)}</strong> af {SEAT_NAME[r.declarer as Seat]},{' '}
                            {r.declarerTricks} stik
                          </>
                        ) : (
                          'passet ud'
                        )}{' '}
                        <span className={(r.scoreNorthSouth ?? 0) >= 0 ? 'plus' : 'minus'}>{signed(r.scoreNorthSouth ?? 0)}</span>
                      </span>
                    ))}
                  {!b.playedByViewer && player.name && (
                    <button type="button" className="button" onClick={() => navigate(`/udfordring/${challenge.code}/${b.index}`)}>
                      Spil spil {b.index}
                    </button>
                  )}
                </li>
              ))}
            </ul>
          </>
        )}
      </section>
    </>
  );
}

/** Plays one deal of a challenge in the right game view, without hints. */
export function ChallengeGamePage({ code, index, player, navigate }: { code: string; index: number; player: PlayerState; navigate: (path: string) => void }) {
  const [challenge, setChallenge] = useState<Challenge | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    fetchChallenge(code, player.name)
      .then(setChallenge)
      .catch((e) => setError(e instanceof Error ? e.message : 'Kunne ikke hente udfordringen.'));
  }, [code, player.name]);

  const back = () => navigate(`/udfordring/${code}`);
  if (error) return <p className="error">{error}</p>;
  if (!challenge) return <p className="muted">Henter …</p>;
  const board = challenge.boards[index - 1];
  if (!board) return <p className="error">Spillet findes ikke.</p>;
  const game = { code: challenge.code, number: board.number, index, count: challenge.boards.length, level: challenge.level };
  return challenge.mode === 'full' ? (
    <GameView key={board.number} onBack={back} player={player.name} challenge={game} />
  ) : (
    <MiniBridgeView key={board.number} onBack={back} player={player.name} challenge={game} />
  );
}
