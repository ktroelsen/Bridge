import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { playAdvice } from '../coach';
import { cardLabel, isNorthSouth, isRed, SEAT_NAME, STRAIN_LABEL, SUIT_SYMBOL } from '../cards';
import type { Card, Contract, DealInfo, PlayedCard, Seat, Strain, Trick } from '../types';
import { DealTable } from './DealTable';
import { HiddenHand, PlayHand } from './PlayHand';

const BOT_CARD_MS = 550;
const TRICK_PAUSE_MS = 1200;

export const contractText = (c: { level: number; strain: Strain; penalty?: Contract['penalty'] }) =>
  `${c.level}${STRAIN_LABEL[c.strain]}${c.penalty === 'doubled' ? ' doblet' : c.penalty === 'redoubled' ? ' redoblet' : ''}`;

/**
 * Shows the played cards one at a time (the human's at once, the bots' with a short delay) and
 * pauses on each completed trick before clearing it.
 */
export function useTrickAnimation(tricks: Trick[], humanControls: (seat: Seat) => boolean) {
  const plays: PlayedCard[] = useMemo(() => tricks.flatMap((t) => t.plays), [tricks]);
  // How many of the played cards are on screen.
  const [shown, setShown] = useState(0);
  // Whether a completed trick has been cleared from the table.
  const [cleared, setCleared] = useState(true);

  useEffect(() => {
    if (shown > 0 && shown % 4 === 0 && !cleared) {
      const t = setTimeout(() => setCleared(true), TRICK_PAUSE_MS);
      return () => clearTimeout(t);
    }
    if (shown < plays.length) {
      const delay = humanControls(plays[shown].seat) ? 0 : BOT_CARD_MS;
      const t = setTimeout(() => {
        setShown(shown + 1);
        setCleared(false);
      }, delay);
      return () => clearTimeout(t);
    }
  }, [plays, shown, cleared, humanControls]);

  /** Jump straight to a number of shown cards (a loaded game, a new deal). */
  const skipTo = useCallback((n: number) => {
    setShown(n);
    setCleared(true);
  }, []);

  const animating = shown < plays.length || (shown > 0 && shown % 4 === 0 && !cleared);
  return { plays, shown, cleared, animating, skipTo };
}

export type TrickAnimation = ReturnType<typeof useTrickAnimation>;

/** The card table during the play: four seats, the trick in the middle and the coach line. */
export function PlayTable({
  anim,
  tricks,
  hands,
  legalCards,
  toPlay,
  contract,
  myTurn,
  onPlay,
  hcpOf,
  intro,
}: {
  anim: TrickAnimation;
  tricks: Trick[];
  hands: { seat: Seat; cards: Card[] }[];
  legalCards: Card[];
  toPlay: Seat | null;
  contract: Contract;
  myTurn: boolean;
  onPlay: (card: Card) => void;
  hcpOf?: (seat: Seat) => number | undefined;
  intro?: string;
}) {
  const { plays, shown, cleared, animating } = anim;
  const trump = contract.strain !== 'noTrump' ? contract.strain : null;
  const dummy: Seat = ({ north: 'south', south: 'north', east: 'west', west: 'east' } as const)[contract.declarer];

  // Cards still to be animated are put back into the hands on screen.
  const pending = plays.slice(shown);
  const handOf = (seat: Seat): Card[] | null => {
    const hand = hands.find((h) => h.seat === seat);
    if (!hand) return null;
    return [...hand.cards, ...pending.filter((p) => p.seat === seat).map((p) => p.card)];
  };
  const hiddenCount = (seat: Seat) => 13 - plays.slice(0, shown).filter((p) => p.seat === seat).length;

  const trickStart = shown % 4 === 0 ? (cleared ? shown : shown - 4) : shown - (shown % 4);
  const tablePlays = plays.slice(trickStart, shown);
  const shownTrick = trickStart / 4 < tricks.length ? tricks[trickStart / 4] : null;
  const trickComplete = tablePlays.length === 4;
  const completed = tricks.slice(0, Math.floor(shown / 4));
  const tricksNs = completed.filter((t) => t.winner && isNorthSouth(t.winner)).length;

  const role = (seat: Seat) => (seat === contract.declarer ? 'Spilfører' : seat === dummy ? 'Blind' : null);

  const seatBox = (seat: Seat) => {
    const cards = handOf(seat);
    const active = myTurn && toPlay === seat;
    const hcp = hcpOf?.(seat);
    return (
      <div className={`seat-box ${seat}${active ? ' active' : ''}${!animating && toPlay === seat ? ' to-play' : ''}`}>
        <div className="seat-label">
          <strong>{seat === 'south' ? 'Syd (dig)' : SEAT_NAME[seat]}</strong>
          {hcp !== undefined && <span className="muted">{hcp} HP</span>}
          {role(seat) && <span className="role">{role(seat)}</span>}
        </div>
        {cards ? (
          <PlayHand cards={cards} legal={legalCards} active={active} onPlay={onPlay} large={seat === 'south'} />
        ) : (
          <HiddenHand count={hiddenCount(seat)} />
        )}
      </div>
    );
  };

  let advice: string | null = null;
  if (myTurn && toPlay) {
    advice = playAdvice({
      seat: toPlay,
      hand: handOf(toPlay) ?? [],
      legal: legalCards,
      ledSuit: tablePlays.length > 0 && !trickComplete ? tablePlays[0].card.suit : null,
      trump,
      openingLead: shown === 0,
      fromDummy: toPlay === dummy,
    });
  }

  return (
    <>
      <div className="status-line">
        <span>
          Kontrakt <strong>{contractText(contract)}</strong> · {SEAT_NAME[contract.declarer]} spiller
        </span>
        <span>
          Stik NS <strong>{tricksNs}</strong> · ØV <strong>{completed.length - tricksNs}</strong>
        </span>
      </div>
      <div className="play-table">
        <div className="area-north">{seatBox('north')}</div>
        <div className="area-west">{seatBox('west')}</div>
        <div className="area-east">{seatBox('east')}</div>
        <div className="area-center">
          <div className="trick">
            {tablePlays.map((p) => (
              <span
                key={`${p.card.suit}-${p.card.rank}`}
                className={`trick-card ${p.seat}${isRed(p.card.suit) ? ' red' : ''}${trickComplete && shownTrick?.winner === p.seat ? ' winner' : ''}`}
              >
                {cardLabel(p.card)}
              </span>
            ))}
            {tablePlays.length === 0 && trump && <span className="trump-hint">Trumf: {SUIT_SYMBOL[trump]}</span>}
            {tablePlays.length === 0 && !trump && <span className="trump-hint">Uden trumf</span>}
          </div>
        </div>
        <div className="area-south">{seatBox('south')}</div>
      </div>
      <p className="coach" aria-live="polite">
        {advice ?? (shown === 0 && intro ? intro : ' ')}
      </p>
    </>
  );
}

/** The result of a played deal, with all four hands. */
export function GameResult({
  contract,
  tricksNorthSouth,
  tricksEastWest,
  score,
  fullDeal,
  onNew,
  onReplay,
  children,
}: {
  contract: Contract;
  tricksNorthSouth: number;
  tricksEastWest: number;
  score: number;
  fullDeal: DealInfo | null;
  onNew: () => void;
  onReplay: () => void;
  children?: ReactNode;
}) {
  const ns = isNorthSouth(contract.declarer);
  const tricks = ns ? tricksNorthSouth : tricksEastWest;
  const needed = contract.level + 6;
  const made = tricks >= needed;
  const ours = ns === made;

  return (
    <div className="result">
      <h3>{made ? 'Kontrakten blev vundet' : `Kontrakten gik ${needed - tricks} ned`}</h3>
      <p>
        {SEAT_NAME[contract.declarer]} spillede {contractText(contract)} og fik {tricks} stik (skulle bruge {needed}).{' '}
        {ours ? 'Godt spillet!' : ns ? 'Prøv spillet igen og se, om du kan finde et stik mere.' : 'Modspillerne fik deres stik.'}
      </p>
      <p className={score >= 0 ? 'score plus' : 'score minus'}>
        Resultat for Nord-Syd: {score > 0 ? '+' : ''}
        {score}
      </p>
      <div className="controls">
        <button type="button" className="button" onClick={onNew}>
          Nyt spil
        </button>
        <button type="button" className="button secondary" onClick={onReplay}>
          Spil igen
        </button>
      </div>
      {children}
      {fullDeal && (
        <>
          <h3>Alle hænder</h3>
          <DealTable deal={fullDeal} />
        </>
      )}
    </div>
  );
}
