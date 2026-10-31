import { useCallback, useEffect, useRef, useState } from 'react';
import { fetchMiniBridgeHint, fetchMiniBridgeReview, fetchMiniBridgeTable, postMiniBridge } from '../api';
import { fits, suggestContract } from '../coach';
import { gameLevel, isNorthSouth, isRed, partnerOf, SEAT_NAME, STRAIN_LABEL, STRAIN_NAME, STRAINS, SUIT_SYMBOL, VULNERABILITY_NAME } from '../cards';
import { loadLevel, loadMiniBridge, saveLevel, saveMiniBridge } from '../storage';
import type { BotLevel, Card, ContractChoice, MiniBridgeRequest, MiniBridgeState, Practice, Seat, Strain } from '../types';
import { LevelPicker } from './LevelPicker';
import { contractText, GameResult, PlayTable, useTrickAnimation } from './CardTable';
import { PlayHand } from './PlayHand';
import { OptimalPanel } from './OptimalPanel';
import { ReviewPanel } from './ReviewPanel';

const newGameRequest = (practice: Practice, level: BotLevel): MiniBridgeRequest => ({ level, number: null, contract: null, cards: [], practice });

const PRACTICE_LABEL: Record<Practice, string | null> = {
  any: null,
  declare: 'Øvelse: du er spilfører',
  defend: 'Øvelse: du spiller modspil',
  trumpFit: 'Øvelse: I har en trumffit',
  noTrump: 'Øvelse: ingen fit, prøv uden trumf',
};

export function MiniBridgeView({ onBack, practice = 'any' }: { onBack: () => void; practice?: Practice }) {
  const [request, setRequest] = useState<MiniBridgeRequest | null>(null);
  const [game, setGame] = useState<MiniBridgeState | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [level, setLevel] = useState<BotLevel>(loadLevel);

  const humanDeclares = game?.declarer != null && isNorthSouth(game.declarer);
  const humanControls = useCallback((seat: Seat) => seat === 'south' || (humanDeclares && seat === 'north'), [humanDeclares]);
  const anim = useTrickAnimation(game?.tricks ?? NO_TRICKS, humanControls);
  const { skipTo } = anim;

  const send = useCallback(
    async (next: MiniBridgeRequest, animate: boolean) => {
      setBusy(true);
      setError(null);
      try {
        const state = await postMiniBridge(next);
        const saved = { ...next, number: state.number };
        setRequest(saved);
        saveMiniBridge(saved);
        setGame(state);
        if (!animate) skipTo(state.tricks.reduce((n, t) => n + t.plays.length, 0));
      } catch (e) {
        setError(e instanceof Error ? e.message : 'Noget gik galt.');
      } finally {
        setBusy(false);
      }
    },
    [skipTo],
  );

  // Resume the saved game (once, also under StrictMode). Practice from a lesson always starts a new deal.
  const started = useRef(false);
  useEffect(() => {
    if (started.current) return;
    started.current = true;
    const saved = loadMiniBridge();
    const resume = practice === 'any' && saved && (saved.level ?? 'easy') === loadLevel();
    void send(resume ? saved : newGameRequest(practice, loadLevel()), false);
  }, [send, practice]);

  const newGame = () => void send(newGameRequest(practice, level), false);
  const replay = () => game && void send({ level, number: game.number, contract: null, cards: [] }, false);
  const changeLevel = (next: BotLevel) => {
    setLevel(next);
    saveLevel(next);
    void send(newGameRequest(practice, next), false);
  };

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

  const contract = game.contract;
  const finished = game.phase === 'done' && !anim.animating;
  const myTurn = game.phase === 'play' && !anim.animating && !busy && game.toPlay !== null && humanControls(game.toPlay);
  const play = (card: Card) => {
    if (!request || !myTurn) return;
    void send({ ...request, cards: [...request.cards, card] }, true);
  };

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

      {PRACTICE_LABEL[practice] && <p className="practice-label">{PRACTICE_LABEL[practice]}</p>}

      {error && <p className="error">{error}</p>}

      {game.phase === 'redeal' && (
        <div className="notice">
          <p>Begge par har 20 point. I mini-bridge gives der så om.</p>
          <button type="button" className="button" onClick={newGame}>
            Nyt spil
          </button>
        </div>
      )}

      {game.phase === 'chooseContract' && request && (
        <ContractChooser game={game} onChoose={(c) => void send({ ...request, contract: c, cards: [] }, true)} busy={busy} />
      )}

      {contract && !finished && (game.phase === 'play' || game.phase === 'done') && (
        <PlayTable
          anim={anim}
          tricks={game.tricks}
          hands={game.hands}
          legalCards={game.legalCards}
          toPlay={game.toPlay}
          contract={contract}
          myTurn={myTurn}
          onPlay={play}
          hcpOf={(seat) => game.hcp.find((h) => h.seat === seat)?.hcp}
          getHint={request ? () => fetchMiniBridgeHint(request) : undefined}
          intro={
            !isNorthSouth(contract.declarer)
              ? `${SEAT_NAME[contract.declarer]} har flest point og spiller ${contractText(contract)}. ${SEAT_NAME[partnerOf(contract.declarer)]} er blind.`
              : undefined
          }
        />
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
          {request && <ReviewPanel key={game.number} contract={contract} load={() => fetchMiniBridgeReview(request)} />}
          {request && (
            <OptimalPanel
              key={`t${game.number}`}
              contract={contract}
              vulnerability={game.vulnerability}
              scoreNorthSouth={game.scoreNorthSouth ?? 0}
              load={(strain) => fetchMiniBridgeTable(request, strain)}
            />
          )}
        </GameResult>
      )}
    </section>
  );
}

const NO_TRICKS: never[] = [];

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
