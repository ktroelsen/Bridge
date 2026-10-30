import { cardLabel, isRed, SEAT_NAME, STRAIN_LABEL, SUIT_NAME, SUIT_SYMBOL } from '../cards';
import type { Visual } from '../lessons/types';
import type { Seat } from '../types';
import { PlayHand } from './PlayHand';

const noop = () => {};
const LETTER: Record<Seat, string> = { north: 'N', east: 'Ø', south: 'S', west: 'V' };

export function LessonVisual({ visual }: { visual: Visual }) {
  switch (visual.type) {
    case 'hand':
      return (
        <div className="visual">
          <PlayHand cards={visual.cards} legal={[]} active={false} onPlay={noop} />
        </div>
      );
    case 'twoHands':
      return (
        <div className="visual chooser-hands">
          <div>
            <div className="seat-label">
              <strong>Nord</strong>
            </div>
            <PlayHand cards={visual.north} legal={[]} active={false} onPlay={noop} />
          </div>
          <div>
            <div className="seat-label">
              <strong>Syd</strong>
            </div>
            <PlayHand cards={visual.south} legal={[]} active={false} onPlay={noop} />
          </div>
        </div>
      );
    case 'trick':
      return (
        <div className="visual">
          <div className="trick lesson-trick" role="img" aria-label={visual.plays.map((p) => `${SEAT_NAME[p.seat]} ${cardLabel(p.card)}`).join(', ')}>
            {(['north', 'east', 'south', 'west'] as Seat[]).map((s) => (
              <span key={s} className={`seat-letter ${s}`}>
                {LETTER[s]}
                {visual.plays[0]?.seat === s ? ' · ud' : ''}
              </span>
            ))}
            {visual.plays.map((p) => (
              <span key={p.seat} className={`trick-card ${p.seat}${isRed(p.card.suit) ? ' red' : ''}`}>
                {cardLabel(p.card)}
              </span>
            ))}
          </div>
          {visual.trump && (
            <p className="muted small center">
              Trumf: <span className={isRed(visual.trump) ? 'red' : ''}>{SUIT_SYMBOL[visual.trump]}</span> {SUIT_NAME[visual.trump].toLowerCase()}
            </p>
          )}
          {visual.noTrumpLabel && <p className="muted small center">Uden trumf</p>}
        </div>
      );
    case 'hcpTable':
      return (
        <div className="visual">
          <div className="hcp-table">
            {(['north', 'west', 'east', 'south'] as Seat[]).map((s) => (
              <div key={s} className={`hcp-cell ${s}`}>
                <strong>{SEAT_NAME[s]}</strong>
                {visual.dealer === s && <span className="badge">G</span>}
                <span>{visual.hcp[s]} HP</span>
              </div>
            ))}
          </div>
        </div>
      );
    case 'contract':
      return (
        <div className="visual center">
          <span className={`contract-pill${visual.strain === 'hearts' || visual.strain === 'diamonds' ? ' red' : ''}`}>
            {visual.level}
            {STRAIN_LABEL[visual.strain]}
          </span>
        </div>
      );
  }
}
