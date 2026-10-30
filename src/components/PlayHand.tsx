import { cardsInSuit, isRed, RANK_LABEL, SUIT_NAME, SUIT_SYMBOL, SUITS, sameCard } from '../cards';
import type { Card } from '../types';

/**
 * A hand as four suit rows, where each card is a button. Only legal cards can be clicked when
 * it is this hand's turn; the others are dimmed.
 */
export function PlayHand({
  cards,
  legal,
  active,
  onPlay,
  large = false,
}: {
  cards: Card[];
  legal: Card[];
  active: boolean;
  onPlay: (card: Card) => void;
  large?: boolean;
}) {
  return (
    <div className={large ? 'play-hand large' : 'play-hand'}>
      {SUITS.map((suit) => {
        const inSuit = cardsInSuit(cards, suit);
        return (
          <div key={suit} className="suit-row">
            <span className={isRed(suit) ? 'symbol red' : 'symbol'} aria-label={SUIT_NAME[suit]}>
              {SUIT_SYMBOL[suit]}
            </span>
            {inSuit.length === 0 && <span className="void">—</span>}
            {inSuit.map((card) => {
              const playable = active && legal.some((l) => sameCard(l, card));
              return (
                <button
                  key={card.rank}
                  type="button"
                  className={`card-chip${isRed(suit) ? ' red' : ''}${playable ? ' playable' : ''}${active && !playable ? ' dim' : ''}`}
                  disabled={!playable}
                  onClick={() => onPlay(card)}
                  aria-label={`${SUIT_NAME[suit]} ${RANK_LABEL[card.rank]}`}
                >
                  {RANK_LABEL[card.rank]}
                </button>
              );
            })}
          </div>
        );
      })}
    </div>
  );
}

/** A hidden hand: just the number of cards left. */
export function HiddenHand({ count }: { count: number }) {
  return (
    <div className="hidden-hand" aria-label={`${count} skjulte kort`}>
      {Array.from({ length: count }, (_, i) => (
        <span key={i} className="card-back" />
      ))}
    </div>
  );
}
