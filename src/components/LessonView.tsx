import { useEffect, useMemo, useState } from 'react';
import { LESSONS } from '../lessons/content';
import type { Lesson, Question, Visual } from '../lessons/types';
import type { QuizKind } from '../types';
import { BidQuestionView } from './BidQuestionView';
import { PlayQuestionView } from './PlayQuestionView';
import type { PlayerState } from '../player';
import { LessonVisual } from './LessonVisual';
import { QuestionView } from './QuestionView';
import { Rich } from './Rich';

type Screen =
  | { kind: 'text'; title: string; body: string[]; visual?: Visual }
  | { kind: 'question'; question: Question; title?: string; intro?: string; index?: number; count?: number }
  | { kind: 'bidding'; quiz: QuizKind; title: string; intro: string; index: number; count: number }
  | { kind: 'play'; stage: 'early' | 'middle'; title: string; intro: string; index: number; count: number };

/** Flattens a lesson into screens; drill questions are made fresh each time the lesson starts. */
function screensOf(lesson: Lesson): Screen[] {
  return lesson.steps.flatMap((step): Screen[] => {
    if (step.type === 'text') return [{ kind: 'text', title: step.title, body: step.body, visual: step.visual }];
    if (step.type === 'question') return [{ kind: 'question', question: step.question }];
    if (step.type === 'play')
      return Array.from({ length: step.count }, (_, i) => ({
        kind: 'play' as const,
        stage: step.stage,
        title: step.title,
        intro: step.intro,
        index: i + 1,
        count: step.count,
      }));
    if (step.type === 'bidding')
      return Array.from({ length: step.count }, (_, i) => ({
        kind: 'bidding' as const,
        quiz: step.quiz,
        title: step.title,
        intro: step.intro,
        index: i + 1,
        count: step.count,
      }));
    return Array.from({ length: step.count }, (_, i) => ({
      kind: 'question' as const,
      question: step.generate(),
      title: step.title,
      intro: step.intro,
      index: i + 1,
      count: step.count,
    }));
  });
}

export function LessonView({ lesson, player, navigate }: { lesson: Lesson; player: PlayerState; navigate: (path: string) => void }) {
  const [attempt, setAttempt] = useState(0);
  // A new attempt makes new drill questions.
  const screens = useMemo(() => screensOf(lesson), [lesson, attempt]);
  const [at, setAt] = useState(0);
  const [answered, setAnswered] = useState(false);
  const [right, setRight] = useState(0);
  const [asked, setAsked] = useState(0);

  const done = at >= screens.length;
  const score = asked === 0 ? 100 : Math.round((100 * right) / asked);
  const { recordLesson } = player;
  useEffect(() => {
    if (done) recordLesson(lesson.id, score);
  }, [done, lesson.id, score, recordLesson]);

  // All state is reset in the same render, so the finished state of the last attempt is never recorded twice.
  const restart = () => {
    setAttempt((n) => n + 1);
    setAt(0);
    setAnswered(false);
    setRight(0);
    setAsked(0);
  };

  const answer = (ok: boolean) => {
    setAnswered(true);
    setAsked((n) => n + 1);
    if (ok) setRight((n) => n + 1);
  };

  const next = () => {
    setAt((n) => n + 1);
    setAnswered(false);
    window.scrollTo(0, 0);
  };

  const index = LESSONS.indexOf(lesson);
  const following = LESSONS[index + 1];
  const screen = screens[at];

  return (
    <section className="panel lesson">
      <div className="game-bar">
        <button type="button" className="back" onClick={() => navigate('/lektioner')}>
          ← Lektioner
        </button>
        <span className="muted small">
          Lektion {index + 1}: {lesson.title}
        </span>
      </div>
      <div className="progress" aria-hidden>
        <div style={{ width: `${(100 * Math.min(at, screens.length)) / screens.length}%` }} />
      </div>

      {!done && screen.kind === 'text' && (
        <div className="lesson-text">
          <h2>{screen.title}</h2>
          {screen.body.map((p, i) => (
            <p key={i}>
              <Rich text={p} />
            </p>
          ))}
          {screen.visual && <LessonVisual visual={screen.visual} />}
          <div className="controls">
            <button type="button" className="button" onClick={next}>
              Næste
            </button>
          </div>
        </div>
      )}

      {!done && screen.kind === 'play' && (
        <div>
          <h2>
            {screen.title} <span className="muted small">{screen.index} af {screen.count}</span>
          </h2>
          {screen.index === 1 && <p className="lead">{screen.intro}</p>}
          <PlayQuestionView key={at} stage={screen.stage} answered={answered} onAnswer={answer} />
          {answered && (
            <div className="controls">
              <button type="button" className="button" onClick={next} autoFocus>
                Næste
              </button>
            </div>
          )}
        </div>
      )}

      {!done && screen.kind === 'bidding' && (
        <div>
          <h2>
            {screen.title} <span className="muted small">{screen.index} af {screen.count}</span>
          </h2>
          {screen.index === 1 && <p className="lead">{screen.intro}</p>}
          <BidQuestionView key={at} quiz={screen.quiz} answered={answered} onAnswer={answer} />
          {answered && (
            <div className="controls">
              <button type="button" className="button" onClick={next} autoFocus>
                Næste
              </button>
            </div>
          )}
        </div>
      )}

      {!done && screen.kind === 'question' && (
        <div>
          {screen.title && (
            <h2>
              {screen.title} <span className="muted small">{screen.index} af {screen.count}</span>
            </h2>
          )}
          {screen.intro && screen.index === 1 && <p className="lead">{screen.intro}</p>}
          <QuestionView
            key={at}
            question={screen.question}
            answered={answered}
            onAnswer={answer}
          />
          {answered && (
            <div className="controls">
              <button type="button" className="button" onClick={next} autoFocus>
                Næste
              </button>
            </div>
          )}
        </div>
      )}

      {done && (
        <div className="lesson-done">
          <h2>Lektionen er færdig</h2>
          <p className="big-score">{score} %</p>
          <p>
            Du svarede rigtigt på {right} af {asked} spørgsmål i første forsøg.{' '}
            {score >= 80 ? 'Flot!' : 'Tag gerne lektionen igen. Der kommer nye spørgsmål hver gang.'}
          </p>
          {lesson.practice && (
            <div className="tip">
              <p>{lesson.practice.text}</p>
              <button type="button" className="button" onClick={() => navigate(lesson.practice!.path)}>
                {lesson.practice.button}
              </button>
            </div>
          )}
          <div className="controls">
            {following && (
              <button type="button" className="button" onClick={() => navigate(`/lektioner/${following.id}`)}>
                Næste lektion: {following.title}
              </button>
            )}
            <button type="button" className="button secondary" onClick={restart}>
              Tag lektionen igen
            </button>
          </div>
        </div>
      )}
    </section>
  );
}
