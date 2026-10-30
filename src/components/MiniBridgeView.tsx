import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { postMiniBridge } from '../api';
import { fits, playAdvice, suggestContract } from '../coach';
import {
  cardLabel,
  gameLevel,
  isNorthSouth,
  isRed,
  partnerOf,
  SEAT_NAME,
  STRAIN_LABEL,
  STRAIN_NAME,
  STRAINS,
  SUIT_SYMBOL,
  VULNERABILITY_NAME,
} from '../cards';
import { loadMiniBridge, saveMiniBridge } from '../storage';
import type { Card, Contract, ContractChoice, MiniBridgeRequest, MiniBridgeState, PlayedCard, Seat, Strain } from '../types';
import { DealTable } from './DealTable';
import { HiddenHand, PlayHand } from './PlayHand';

const BOT_CARD_MS = 550;
const TRICK_PAUSE_MS = 1200;
const NEW_GAME: MiniBridgeRequest = { number: null, contract: null, cards: [] };

const contractText = (c: { level: number; strain: Strain }) => `${c.level}${STRAIN_LABEL[c.strain]}`;
const trumpOf = (c: Contract | null) => (c && c.strain !== 'noTrump' ? c.strain : null);

export function MiniBridgeView({ onBack }: { onBack: () => void }) {
  const [request, setRequest] = useState<MiniBridgeRequest | null>(null);
  const [game, setGame] = useState<MiniBridgeState | null>(null);
  // How many of the played cards are on screen; the bot's cards appear one at a time.
  const [shown, setShown] = useState(0);
  // Whether a completed trick has been cleared from the table.
  const [cleared, setCleared] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const send = useCallback(async (next: MiniBridgeRequest, animate: boolean) => {
    setBusy(true);
    setError(null);
    try {
      const state = await postMiniBridge(next);
      const saved = { ...next, number: state.number };
      setRequest(saved);
      saveMiniBridge(saved);
      setGame(state);
      if (!animate) {
        setShown(state.tricks.reduce((n, t) => n + t.plays.length, 0));
        setCleared(true);
      }
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Noget gik galt.');
    } finally {
      setBusy(false);
    }
  }, []);

  // Resume the saved game (once, also under StrictMode).
  const started = useRef(false);
  useEffect(() => {
    if (started.current) return;
    started.current = true;
    void send(loadMiniBridge() ?? NEW_GAME, false);
  }, [send]);

  const newGame = () => {
    setShown(0);
    void send(NEW_GAME, false);
  };
  const replay = () => {
    if (!game) return;
    setShown(0);
    void send({ number: game.number, contract: null, cards: [] }, false);
  };

  const plays: PlayedCard[] = useMemo(() => game?.tricks.flatMap((t) => t.plays) ?? [], [game]);
  const humanDeclares = game?.declarer != null && isNorthSouth(game.declarer);
  const humanControls = useCallback((seat: Seat) => seat === 'south' || (humanDeclares && seat === 'north'), [humanDeclares]);

  // Step the animation: pause on a completed trick, then show the next card.
  useEffect(() => {
    if (!game) return;
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
  }, [game, plays, shown, cleared, humanControls]);

  const animating = shown < plays.length || (shown > 0 && shown % 4 === 0 && !cleared);

  if (!game) {
    return (
      <section className="panel">
        <button type="button" className="back" onClick={onBack}>
          ← Menu
        </button>
        {error ? (
          <>
            <p className="error">{error}</p>
            <button type="button" className="button" onClick={newGame}>
              Nyt spil
            </button>
          </>
        ) : (
          <p className="lead">Giver kort …</p>
        )}
      </section>
    );
  }

  const hcpOf = (seat: Seat) => game.hcp.find((h) => h.seat === seat)!.hcp;
  const contract = game.contract;
  const trump = trumpOf(contract);

  // Cards still to be animated are put back into the hands on screen.
  const pending = plays.slice(shown);
  const handOf = (seat: Seat): Card[] | null => {
    const hand = game.hands.find((h) => h.seat === seat);
    if (!hand) return null;
    return [...hand.cards, ...pending.filter((p) => p.seat === seat).map((p) => p.card)];
  };
  const hiddenCount = (seat: Seat) => 13 - plays.slice(0, shown).filter((p) => p.seat === seat).length;

  const trickStart = shown % 4 === 0 ? (cleared ? shown : shown - 4) : shown - (shown % 4);
  const tablePlays = plays.slice(trickStart, shown);
  const shownTrick = trickStart / 4 < game.tricks.length ? game.tricks[trickStart / 4] : null;
  const trickComplete = tablePlays.length === 4;
  const completedTricks = game.tricks.slice(0, Math.floor(shown / 4));
  const tricksNs = completedTricks.filter((t) => t.winner && isNorthSouth(t.winner)).length;
  const tricksEw = completedTricks.length - tricksNs;

  const finished = game.phase === 'done' && !animating;
  const myTurn = game.phase === 'play' && !animating && !busy && game.toPlay !== null && humanControls(game.toPlay);

  const play = (card: Card) => {
    if (!request || !myTurn) return;
    void send({ ...request, cards: [...request.cards, card] }, true);
  };

  const role = (seat: Seat) => (seat === game.declarer ? 'Spilfører' : seat === game.dummy ? 'Blind' : null);

  const seatBox = (seat: Seat) => {
    const cards = handOf(seat);
    const active = myTurn && game.toPlay === seat;
    return (
      <div className={`seat-box ${seat}${active ? ' active' : ''}${!animating && game.toPlay === seat && game.phase === 'play' ? ' to-play' : ''}`}>
        <div className="seat-label">
          <strong>{seat === 'south' ? 'Syd (dig)' : SEAT_NAME[seat]}</strong>
          <span className="muted">{hcpOf(seat)} HP</span>
          {role(seat) && <span className="role">{role(seat)}</span>}
        </div>
        {cards ? (
          <PlayHand cards={cards} legal={game.legalCards} active={active} onPlay={play} large={seat === 'south'} />
        ) : (
          <HiddenHand count={hiddenCount(seat)} />
        )}
      </div>
    );
  };

  let advice: string | null = null;
  if (myTurn && game.toPlay) {
    const hand = handOf(game.toPlay) ?? [];
    advice = playAdvice({
      seat: game.toPlay,
      hand,
      legal: game.legalCards,
      ledSuit: tablePlays.length > 0 && !trickComplete ? tablePlays[0].card.suit : null,
      trump,
      openingLead: shown === 0,
      fromDummy: game.toPlay === game.dummy,
    });
  }

  return (
    <section className="panel game">
      <div className="game-bar">
        <button type="button" className="back" onClick={onBack}>
          ← Menu
        </button>
        <span className="muted">
          Spil {game.number} · {VULNERABILITY_NAME[game.vulnerability]}
        </span>
        <button type="button" className="link" onClick={newGame} disabled={busy}>
          Nyt spil
        </button>
      </div>

      {contract && (
        <div className="status-line">
          <span>
            Kontrakt <strong>{contractText(contract)}</strong> · {SEAT_NAME[contract.declarer]} spiller
          </span>
          <span>
            Stik NS <strong>{tricksNs}</strong> · ØV <strong>{tricksEw}</strong>
          </span>
        </div>
      )}

      {error && <p className="error">{error}</p>}

      {game.phase === 'redeal' && (
        <div className="notice">
          <p>
            Begge par har 20 point. I mini-bridge gives der så om.
          </p>
          <button type="button" className="button" onClick={newGame}>
            Nyt spil
          </button>
        </div>
      )}

      {game.phase === 'chooseContract' && request && <ContractChooser game={game} onChoose={(c) => void send({ ...request, contract: c, cards: [] }, true)} busy={busy} />}

      {(game.phase === 'play' || (game.phase === 'done' && !finished)) && (
        <>
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
                {tablePlays.length === 0 && !trump && contract && <span className="trump-hint">Uden trumf</span>}
              </div>
            </div>
            <div className="area-south">{seatBox('south')}</div>
          </div>
          <p className="coach" aria-live="polite">
            {advice ??
              (shown === 0 && contract && !isNorthSouth(contract.declarer)
                ? `${SEAT_NAME[contract.declarer]} har flest point og spiller ${contractText(contract)}. ${SEAT_NAME[partnerOf(contract.declarer)]} er blind.`
                : ' ')}
          </p>
        </>
      )}

      {finished && contract && <Result game={game} contract={contract} onNew={newGame} onReplay={replay} />}
    </section>
  );
}

function ContractChooser({ game, onChoose, busy }: { game: MiniBridgeState; onChoose: (c: ContractChoice) => void; busy: boolean }) {
  const [strain, setStrain] = useState<Strain | null>(null);
  const [isGame, setIsGame] = useState(false);
  const [showTip, setShowTip] = useState(false);

  const south = game.hands.find((h) => h.seat === 'south')!.cards;
  const north = game.hands.find((h) => h.seat === 'north')!.cards;
  const hcp = game.hcp.filter((h) => isNorthSouth(h.seat)).reduce((n, h) => n + h.hcp, 0);
  const f = fits(south, north);
  const tip = suggestContract(hcp, f);
  const declarer = game.declarer!;

  return (
    <div className="chooser">
      <p className="lead">
        {declarer === 'south'
          ? 'I har flest point, og du har flest af jer to. Du er spilfører.'
          : 'I har flest point. Nord har flest af jer to og er spilfører, men du spiller begge hænder.'}{' '}
        Nord og Syd har tilsammen <strong>{hcp} HP</strong>. Vælg trumf og om I går efter udgang.
      </p>

      <div className="chooser-hands">
        <div>
          <div className="seat-label">
            <strong>Nord</strong> <span className="muted">{game.hcp.find((h) => h.seat === 'north')!.hcp} HP</span>
          </div>
          <PlayHand cards={north} legal={[]} active={false} onPlay={() => {}} />
        </div>
        <div>
          <div className="seat-label">
            <strong>Syd (dig)</strong> <span className="muted">{game.hcp.find((h) => h.seat === 'south')!.hcp} HP</span>
          </div>
          <PlayHand cards={south} legal={[]} active={false} onPlay={() => {}} />
        </div>
      </div>

      <p className="fits">
        {f.map((x) => (
          <span key={x.suit} className={x.mine + x.partner >= 8 ? 'fit good' : 'fit'}>
            <span className={isRed(x.suit) ? 'red' : ''}>{SUIT_SYMBOL[x.suit]}</span> {x.partner}+{x.mine} = {x.mine + x.partner}
          </span>
        ))}
      </p>

      <div className="strain-buttons" role="group" aria-label="Trumf">
        {STRAINS.map((s) => (
          <button
            key={s}
            type="button"
            className={`strain${strain === s ? ' selected' : ''}${s === 'hearts' || s === 'diamonds' ? ' red' : ''}`}
            onClick={() => setStrain(s)}
            title={STRAIN_NAME[s]}
          >
            {STRAIN_LABEL[s]}
          </button>
        ))}
      </div>
      <div className="level-buttons" role="group" aria-label="Niveau">
        <button type="button" className={!isGame ? 'level selected' : 'level'} onClick={() => setIsGame(false)}>
          Delkontrakt{strain ? ` (1${STRAIN_LABEL[strain]})` : ''}
        </button>
        <button type="button" className={isGame ? 'level selected' : 'level'} onClick={() => setIsGame(true)}>
          Udgang{strain ? ` (${gameLevel(strain)}${STRAIN_LABEL[strain]})` : ''}
        </button>
      </div>
      <p className="muted small">
        Delkontrakt kræver 7 stik. Udgang giver en stor bonus, men kræver 9 stik i UT, 10 i ♥/♠ og 11 i ♣/♦.
      </p>

      <div className="controls">
        <button type="button" className="button" disabled={!strain || busy} onClick={() => strain && onChoose({ strain, game: isGame })}>
          Spil
        </button>
        <button type="button" className="button secondary" onClick={() => setShowTip(true)}>
          Hvad ville træneren vælge?
        </button>
      </div>
      {showTip && (
        <p className="tip">
          <strong>{tip.game ? `Udgang i ${STRAIN_NAME[tip.strain].toLowerCase()}` : `Delkontrakt i ${STRAIN_NAME[tip.strain].toLowerCase()}`}.</strong>{' '}
          {tip.why}
        </p>
      )}
    </div>
  );
}

function Result({ game, contract, onNew, onReplay }: { game: MiniBridgeState; contract: Contract; onNew: () => void; onReplay: () => void }) {
  const ns = isNorthSouth(contract.declarer);
  const tricks = ns ? game.tricksNorthSouth : game.tricksEastWest;
  const needed = contract.level + 6;
  const made = tricks >= needed;
  const score = game.scoreNorthSouth ?? 0;
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
      {game.fullDeal && (
        <>
          <h3>Alle hænder</h3>
          <DealTable deal={game.fullDeal} />
        </>
      )}
    </div>
  );
}
