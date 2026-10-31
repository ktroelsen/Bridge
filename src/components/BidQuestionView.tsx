import { useEffect, useState } from 'react';
import { fetchBiddingQuestion } from '../api';
import { callLabel } from '../cards';
import type { BiddingQuestion, CallInfo, QuizKind } from '../types';
import { AuctionTable } from './AuctionTable';
import { BiddingBox } from './BiddingBox';
import { PlayHand } from './PlayHand';
import { Rich } from './Rich';

/** "What do you bid?": South's hand and the calls so far; the player answers in the bidding box. */
export function BidQuestionView({ quiz, answered, onAnswer }: { quiz: QuizKind; answered: boolean; onAnswer: (correct: boolean) => void }) {
  const [question, setQuestion] = useState<BiddingQuestion | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [given, setGiven] = useState<string | null>(null);
  const [selected, setSelected] = useState<number | null>(null);

  useEffect(() => {
    let live = true;
    fetchBiddingQuestion(quiz)
      .then((q) => live && setQuestion(q))
      .catch((e) => live && setError(e instanceof Error ? e.message : 'Kunne ikke hente spørgsmålet.'));
    return () => {
      live = false;
    };
  }, [quiz]);

  if (error) return <p className="error">{error}</p>;
  if (!question) return <p className="muted">Giver kort …</p>;

  const calls: CallInfo[] = question.auction.map((c) => ({ ...c, systemCall: null, systemExplanation: null }));
  const right = given === question.answer.call;
  const chosen = selected !== null ? calls[selected] : null;

  return (
    <div className="question">
      <p className="prompt">{calls.length === 0 ? 'Du er giver. Hvad åbner du med?' : 'Hvad melder du?'}</p>
      {calls.length > 0 && (
        <>
          <AuctionTable dealer={question.dealer} calls={calls} selected={selected} onSelect={(i) => setSelected(i === selected ? null : i)} waiting={!answered} />
          {chosen && (
            <p className="call-info">
              <strong>{callLabel(chosen.call)}:</strong> <Rich text={chosen.explanation} />
            </p>
          )}
        </>
      )}
      <div className="seat-label">
        <strong>Syd (dig)</strong> <span className="muted">{question.hcp} HP</span>
      </div>
      <PlayHand cards={question.hand} legal={[]} active={false} onPlay={() => {}} large />
      {!answered && (
        <BiddingBox
          legal={question.legalCalls}
          maxLevel={4}
          disabled={answered}
          onCall={(c) => {
            setGiven(c);
            onAnswer(c === question.answer.call);
          }}
        />
      )}
      {answered && given && (
        <div className={right ? 'feedback right' : 'feedback wrong'} role="status">
          <strong>{right ? `Rigtigt: ${callLabel(given)}.` : `Du meldte ${callLabel(given)}. Systemet melder ${callLabel(question.answer.call)}.`}</strong>{' '}
          <Rich text={question.answer.explanation} />
        </div>
      )}
    </div>
  );
}
