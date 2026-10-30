import { useState } from 'react';
import { COMING_LESSONS, LESSONS } from '../lessons/content';
import type { PlayerState } from '../player';

export function LessonList({ player, navigate }: { player: PlayerState; navigate: (path: string) => void }) {
  const nextLesson = LESSONS.find((l) => player.progress[l.id] === undefined);
  return (
    <>
      {player.askName && <NameForm player={player} />}
      <section className="panel">
        <button type="button" className="back" onClick={() => navigate('/')}>
          ← Menu
        </button>
        <h2>Lær bridge</h2>
        <p className="lead">
          Tag lektionerne i rækkefølge. Hver lektion er kort tekst og øvelser, og efter nogle af dem kan du øve dig i
          mini-bridge.
        </p>
        <ol className="lesson-list">
          {LESSONS.map((lesson, i) => {
            const score = player.progress[lesson.id];
            return (
              <li key={lesson.id}>
                <button type="button" className={`menu-item${lesson === nextLesson ? ' next' : ''}`} onClick={() => navigate(`/lektioner/${lesson.id}`)}>
                  <span className="lesson-head">
                    <strong>
                      {i + 1}. {lesson.title}
                    </strong>
                    {score !== undefined ? <span className="done-badge">✓ {score} %</span> : lesson === nextLesson ? <span className="next-badge">Næste</span> : null}
                  </span>
                  <span>{lesson.summary}</span>
                </button>
              </li>
            );
          })}
          {COMING_LESSONS.map((c, i) => (
            <li key={c.title}>
              <div className="menu-item locked">
                <span className="lesson-head">
                  <strong>
                    {LESSONS.length + i + 1}. {c.title}
                  </strong>
                  <span className="muted small">Kommer senere</span>
                </span>
                <span>{c.summary}</span>
              </div>
            </li>
          ))}
        </ol>
      </section>
    </>
  );
}

export function NameForm({ player }: { player: PlayerState }) {
  const [value, setValue] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  return (
    <section className="panel">
      <h2>Hvad hedder du?</h2>
      <p className="lead">Så husker appen, hvor langt du er nået, også hvis du skifter til en anden telefon eller computer. Du skal ikke have kodeord.</p>
      <form
        className="controls"
        onSubmit={async (e) => {
          e.preventDefault();
          setBusy(true);
          setError(null);
          try {
            await player.setName(value);
          } catch (err) {
            setError(err instanceof Error ? err.message : 'Noget gik galt.');
          } finally {
            setBusy(false);
          }
        }}
      >
        <label className="number">
          Navn
          <input className="name-input" maxLength={30} value={value} onChange={(e) => setValue(e.target.value)} autoComplete="given-name" />
        </label>
        <button type="submit" className="button" disabled={busy || value.trim() === ''}>
          Gem
        </button>
        <button type="button" className="link" onClick={player.skipName}>
          Fortsæt uden navn
        </button>
      </form>
      {error && <p className="error">{error}</p>}
    </section>
  );
}
