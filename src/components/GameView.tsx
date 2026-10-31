import { useCallback, useEffect, useRef, useState } from 'react';
import { fetchGameHint, fetchGameReview, fetchGameTable, postGame } from '../api';
import { callLabel, isNorthSouth, SEAT_NAME, VULNERABILITY_NAME } from '../cards';
import { loadGame, loadLevel, saveGame, saveLevel } from '../storage';
import type { BotLevel, Card, GameRequest, GameState, Seat } from '../types';
import { LevelPicker } from './LevelPicker';
import { AuctionTable } from './AuctionTable';
import { BiddingBox } from './BiddingBox';
import { contractText, GameResult, PlayTable, useTrickAnimation } from './CardTable';
import { PlayHand } from './PlayHand';
import { OptimalPanel } from './OptimalPanel';
import { ReviewPanel } from './ReviewPanel';
import { Rich } from './Rich';

const newGameRequest = (level: BotLevel): GameRequest => ({ level, number: null, calls: [], cards: [] });
const NO_TRICKS: never[] = [];

/** A full game against the bots: bidding with explanations and hints, then the play. */
export function GameView({ onBack }: { onBack: () => void }) {
  const [request, setRequest] = useState<GameRequest | null>(null);
  const [game, setGame] = useState<GameState | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] = useState<number | null>(null);
  const [showHint, setShowHint] = useState(false);
  const [level, setLevel] = useState<BotLevel>(loadLevel);

  const humanDeclares = game?.contract != null && isNorthSouth(game.contract.declarer);
  const humanControls = useCallback((seat: Seat) => seat === 'south' || (humanDeclares && seat === 'north'), [humanDeclares]);
  const anim = useTrickAnimation(game?.tricks ?? NO_TRICKS, humanControls);
  const { skipTo } = anim;

  const send = useCallback(
    async (next: GameRequest, animate: boolean) => {
      setBusy(true);
      setError(null);
      setShowHint(false);
      try {
        const state = await postGame(next);
        const saved = { ...next, number: state.number };
        setRequest(saved);
        saveGame(saved);
        setGame(state);
        // Select the player's latest call when the system would have bid something else.
        const last = state.auction.length - 1 - [...state.auction].reverse().findIndex((c) => c.seat === 'south');
        setSelected(last < state.auction.length && state.auction[last]?.systemCall ? last : null);
        if (!animate) skipTo(state.tricks.reduce((n, t) => n + t.plays.length, 0));
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Noget gik galt.');
      } finally {
        setBusy(false);
      }
    },
    [skipTo],
  );

  const started = useRef(false);
  useEffect(() => {
    if (started.current) return;
    started.current = true;
    const saved = loadGame();
    void send(saved && (saved.level ?? 'easy') === loadLevel() ? saved : newGameRequest(loadLevel()), false);
  }, [send]);

  const newGame = () => void send(newGameRequest(level), false);
  const replay = () => game && void send({ level, number: game.number, calls: [], cards: [] }, false);
  const changeLevel = (next: BotLevel) => {
    setLevel(next);
    saveLevel(next);
    void send(newGameRequest(next), false);
  };

  if (!game || !request) {
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

  const bidding = game.phase === 'bidding';
  const finished = (game.phase === 'done' && !anim.animating) || game.phase === 'passedOut';
  const myTurn = game.phase === 'play' && !anim.animating && !busy && game.toPlay !== null && humanControls(game.toPlay);
  const contract = game.contract;
  const south = game.hands.find((h) => h.seat === 'south')?.cards ?? [];

  const call = (c: string) => void send({ ...request, calls: [...request.calls, c] }, true);
  const play = (card: Card) => myTurn && void send({ ...request, cards: [...request.cards, card] }, true);
  const undo = () => {
    if (request.cards.length > 0) void send({ ...request, cards: request.cards.slice(0, -1) }, false);
    else if (request.calls.length > 0) void send({ ...request, calls: request.calls.slice(0, -1) }, false);
  };
  const canUndo = !busy && !finished && (request.cards.length > 0 || request.calls.length > 0);

  const chosen = selected !== null ? game.auction[selected] : null;
  const leader = contract ? (({ north: 'east', east: 'south', south: 'west', west: 'north' }) as const)[contract.declarer] : null;

  const auctionView = (
    <>
      <AuctionTable dealer={game.dealer} calls={game.auction} selected={selected} onSelect={(i) => setSelected(i === selected ? null : i)} waiting={bidding} />
      {chosen ? (
        <div className={`call-info${chosen.systemCall ? ' differs' : ''}`}>
          <p>
            <strong>
              {SEAT_NAME[chosen.seat]}: {callLabel(chosen.call)}.
            </strong>{' '}
            {chosen.explanation !== callLabel(chosen.call) && <Rich text={chosen.explanation} />}
          </p>
          {chosen.systemCall && (
            <p>
              <strong>Systemet ville have meldt {callLabel(chosen.systemCall)}:</strong> <Rich text={chosen.systemExplanation ?? ''} />
            </p>
          )}
        </div>
      ) : (
        bidding && <p className="muted small">Tryk på en melding for at se, hvad den betyder.</p>
      )}
    </>
  );

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

      <LevelPicker level={level} onChange={changeLevel} disabled={busy} />
      {busy && game.phase === 'play' && <p className="thinking muted small">Botterne tænker …</p>}

      {error && <p className="error">{error}</p>}

      {(bidding || finished) && auctionView}

      {bidding && (
        <>
          <div className="seat-label">
            <strong>Syd (dig)</strong> <span className="muted">{game.southHcp} HP</span>
          </div>
          <PlayHand cards={south} legal={[]} active={false} onPlay={() => {}} large />
          <BiddingBox legal={game.legalCalls} onCall={call} disabled={busy} />
          <div className="controls">
            <button type="button" className="button secondary" onClick={() => setShowHint(true)} disabled={!game.hint}>
              Hvad siger systemet?
            </button>
            {canUndo && (
              <button type="button" className="link" onClick={undo}>
                Fortryd
              </button>
            )}
          </div>
          {showHint && game.hint && (
            <p className="tip">
              <strong>Meld {callLabel(game.hint.call)}.</strong> <Rich text={game.hint.explanation} />
            </p>
          )}
        </>
      )}

      {contract && !finished && game.phase !== 'bidding' && (
        <>
          <PlayTable
            anim={anim}
            tricks={game.tricks}
            hands={game.hands}
            legalCards={game.legalCards}
            toPlay={game.toPlay}
            contract={contract}
            myTurn={myTurn}
            onPlay={play}
            hcpOf={(seat) => (seat === 'south' ? game.southHcp : undefined)}
            getHint={() => fetchGameHint(request)}
            intro={`${SEAT_NAME[contract.declarer]} spiller ${contractText(contract)}. ${
              isNorthSouth(contract.declarer) ? 'Du spiller både Nords og Syds kort. ' : ''
            }${leader === 'south' ? 'Du skal spille ud.' : `${SEAT_NAME[leader!]} spiller ud.`}`}
          />
          {canUndo && (
            <div className="controls">
              <button type="button" className="link" onClick={undo}>
                Fortryd sidste kort
              </button>
            </div>
          )}
          <details className="auction-details" open>
            <summary>Meldingerne</summary>
            {auctionView}
          </details>
        </>
      )}

      {game.phase === 'passedOut' && (
        <div className="notice">
          <p>Alle fire passede, så spillet bliver ikke spillet.</p>
          <div className="controls center-row">
            <button type="button" className="button" onClick={newGame}>
              Nyt spil
            </button>
            <button type="button" className="button secondary" onClick={replay}>
              Meld igen
            </button>
          </div>
        </div>
      )}

      {finished && contract && (
        <GameResult
          contract={contract}
          tricksNorthSouth={game.tricksNorthSouth}
          tricksEastWest={game.tricksEastWest}
          score={game.scoreNorthSouth ?? 0}
          fullDeal={game.fullDeal}
          onNew={newGame}
          onReplay={replay}
        >
          <ReviewPanel key={game.number} contract={contract} load={() => fetchGameReview(request)} />
          <OptimalPanel
            key={`t${game.number}`}
            contract={contract}
            vulnerability={game.vulnerability}
            scoreNorthSouth={game.scoreNorthSouth ?? 0}
            load={(strain) => fetchGameTable(request, strain)}
          />
        </GameResult>
      )}
    </section>
  );
}
