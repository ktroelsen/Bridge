import { cardsInSuit, isRed, RANK_LABEL, SEAT_NAME, SUIT_NAME, SUIT_SYMBOL, SUITS } from '../cards';
import type { HandInfo } from '../types';

/** One hand as four suit lines (the way hands are printed in bridge books). */
export function HandView({ hand, dealer }: { hand: HandInfo; dealer: boolean }) {
  return (
    <section className="hand" aria-label={`${SEAT_NAME[hand.seat]}s hånd`}>
      <header className="hand-header">
        <span className="seat">{SEAT_NAME[hand.seat]}</span>
        {dealer && <span className="badge" title="Giver">G</span>}
        <span className="hcp">{hand.hcp} HP</span>
      </header>
      {SUITS.map((suit) => {
        const cards = cardsInSuit(hand.cards, suit);
        return (
          <div key={suit} className="suit-line">
            <span className={isRed(suit) ? 'symbol red' : 'symbol'} aria-label={SUIT_NAME[suit]}>
              {SUIT_SYMBOL[suit]}
            </span>
            <span className="ranks">{cards.length ? cards.map((c) => RANK_LABEL[c.rank]).join(' ') : '—'}</span>
          </div>
        );
      })}
    </section>
  );
}
