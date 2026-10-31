import { cardLabel, cardsInSuit, gameLevel, SEAT_NAME, STRAIN_LABEL, STRAIN_NAME, SUIT_NAME, SUIT_SYMBOL } from '../cards';
import { suggestContract, fits } from '../coach';
import { cardHcp, hcp, isBalanced, leftOf, legalCards, nextSeat, pick, randomDeal, rankValue, SEATS, shape, suitLength, winningPlay, beats } from '../deck';
import type { Card, PlayedCard, Seat, Strain, Suit } from '../types';
import type { Question } from './types';

// Question generators for the drills. Each call makes a fresh question from a random deal.

const SUITS: Suit[] = ['spades', 'hearts', 'diamonds', 'clubs'];
const seatOptions = SEATS.map((s) => SEAT_NAME[s]);
const lower = (suit: Suit) => SUIT_NAME[suit].toLowerCase();

/** A trick where every player plays a random legal card from a random deal. */
function randomTrick(cards: number) {
  const deal = randomDeal();
  const leader = pick(SEATS);
  const plays: PlayedCard[] = [];
  let seat = leader;
  for (let i = 0; i < cards; i++) {
    const legal = legalCards(deal[seat], plays[0]?.card.suit ?? null);
    plays.push({ seat, card: pick(legal) });
    seat = nextSeat(seat);
  }
  return { deal, plays, next: seat };
}

const ruffed = (plays: PlayedCard[], trump: Suit | null) =>
  trump !== null && plays.some((p) => p.card.suit === trump && p.card.suit !== plays[0].card.suit);

function winnerExplanation(plays: PlayedCard[], trump: Suit | null) {
  const win = winningPlay(plays, trump);
  const led = plays[0].card.suit;
  if (trump && win.card.suit === trump && led !== trump) {
    const trumps = plays.filter((p) => p.card.suit === trump).length;
    return `${SEAT_NAME[win.seat]} vinder med ${cardLabel(win.card)}: ${lower(trump)} er trumf, og trumf slår alle andre farver${trumps > 1 ? '. Når flere trumfer, vinder den højeste trumf' : ''}.`;
  }
  const offSuit = plays.some((p) => p.card.suit !== led);
  return `${SEAT_NAME[win.seat]} vinder med ${cardLabel(win.card)}, det højeste kort i ${lower(led)}, som blev spillet ud.${offSuit ? ' Kort i andre farver kan ikke vinde stikket' + (trump ? ', medmindre de er trumf.' : '.') : ''}`;
}

/** "Who wins the trick?" With trumps, about half of the tricks have someone ruffing. */
export function trickWinner(withTrump: boolean): Question {
  const trump = withTrump ? pick(SUITS) : null;
  const wantRuff = withTrump && Math.random() < 0.55;
  let trick = randomTrick(4);
  for (let i = 0; i < 200 && ruffed(trick.plays, trump) !== wantRuff; i++) trick = randomTrick(4);
  const win = winningPlay(trick.plays, trump);
  return {
    prompt: trump ? `Trumf er ${SUIT_SYMBOL[trump]}. Hvem vinder stikket?` : 'Der spilles uden trumf. Hvem vinder stikket?',
    visual: { type: 'trick', plays: trick.plays, trump, noTrumpLabel: !trump },
    answer: { type: 'options', options: seatOptions, correct: [SEATS.indexOf(win.seat)] },
    explain: winnerExplanation(trick.plays, trump),
  };
}

/** A trick so far where it is South's turn. */
function southToPlay(accept: (hand: Card[], plays: PlayedCard[]) => boolean) {
  for (let i = 0; ; i++) {
    const before = 1 + Math.floor(Math.random() * 3);
    const deal = randomDeal();
    // Lead from the seat that makes South play after `before` cards.
    let leader: Seat = 'south';
    for (let k = 0; k < before; k++) leader = SEATS[(SEATS.indexOf(leader) + 3) % 4];
    const plays: PlayedCard[] = [];
    let seat = leader;
    for (let k = 0; k < before; k++) {
      plays.push({ seat, card: pick(legalCards(deal[seat], plays[0]?.card.suit ?? null)) });
      seat = nextSeat(seat);
    }
    if (accept(deal.south, plays) || i > 5000) return { hand: deal.south, plays };
  }
}

/** "Which card may you play?" Half the time South can follow suit, half the time not. */
export function followSuit(): Question {
  const wantVoid = Math.random() < 0.4;
  const { hand, plays } = southToPlay((h, p) => (suitLength(h, p[0].card.suit) === 0) === wantVoid);
  const led = plays[0].card.suit;
  const legal = legalCards(hand, led);
  const canFollow = suitLength(hand, led) > 0;
  return {
    prompt: `${SEAT_NAME[plays[0].seat]} har spillet ${cardLabel(plays[0].card)} ud. Vælg et kort, du må spille.`,
    visual: { type: 'trick', plays, trump: null },
    answer: { type: 'card', hand, correct: legal },
    explain: canFollow
      ? `Du har ${lower(led)}, så du skal bekende kulør: alle dine ${lower(led)} er rigtige, og ingen andre kort.`
      : `Du har ingen ${lower(led)}, så du må spille et hvilket som helst kort. Det kan bare ikke vinde stikket uden trumf.`,
  };
}

/** "Pick a card that wins the trick" with trumps: sometimes by following high, sometimes by ruffing. */
export function winTrick(): Question {
  const trump = pick(SUITS);
  const wantRuff = Math.random() < 0.6;
  const winners = (hand: Card[], plays: PlayedCard[]) => {
    const current = winningPlay(plays, trump).card;
    return legalCards(hand, plays[0].card.suit).filter((c) => beats(c, current, trump));
  };
  const { hand, plays } = southToPlay((h, p) => {
    const led = p[0].card.suit;
    const w = winners(h, p);
    return led !== trump && w.length > 0 && (suitLength(h, led) === 0) === wantRuff;
  });
  const led = plays[0].card.suit;
  const correct = winners(hand, plays);
  const current = winningPlay(plays, trump);
  const ruff = suitLength(hand, led) === 0;
  return {
    prompt: `Trumf er ${SUIT_SYMBOL[trump]}. Vælg et kort, der vinder stikket lige nu.`,
    visual: { type: 'trick', plays, trump },
    answer: { type: 'card', hand, correct },
    explain: ruff
      ? `Du har ingen ${lower(led)}, så du må trumfe. ${correct.length > 1 ? 'Enhver trumf, der er højere end de andres trumf, vinder – og det er klogt at bruge den mindste.' : `${cardLabel(correct[0])} vinder.`}`
      : `Du har ${lower(led)} og skal bekende. Du skal slå ${cardLabel(current.card)}: ${correct.map(cardLabel).join(', ')} vinder.`,
  };
}

/** Count the high card points in a hand. */
export function countHcp(): Question {
  const hand = randomDeal().south;
  const honours = SUITS.flatMap((s) => cardsInSuit(hand, s)).filter((c) => cardHcp(c) > 0);
  return {
    prompt: 'Hvor mange honnørpoint (HP) har hånden?',
    visual: { type: 'hand', cards: hand },
    answer: { type: 'number', min: 0, max: 37, correct: hcp(hand) },
    explain: honours.length
      ? `${honours.map((c) => `${cardLabel(c)} = ${cardHcp(c)}`).join(', ')}. I alt ${hcp(hand)} HP.`
      : 'Ingen billedkort og ingen esser: 0 HP.',
  };
}

/** Balanced or not, half of each. */
export function balanced(): Question {
  const want = Math.random() < 0.5;
  let hand = randomDeal().south;
  while (isBalanced(hand) !== want) hand = randomDeal().south;
  const s = shape(hand).join('-');
  return {
    prompt: 'Er hånden jævn eller ujævn?',
    visual: { type: 'hand', cards: hand },
    answer: { type: 'options', options: ['Jævn', 'Ujævn'], correct: [want ? 0 : 1] },
    explain: want
      ? `Fordelingen er ${s}: ingen renonce, ingen single og højst én doubleton. Den er jævn.`
      : `Fordelingen er ${s}. ${shape(hand).some((l) => l <= 1) ? 'Der er en farve med 0 eller 1 kort' : 'Der er to doubletons'}, så den er ujævn.`,
  };
}

/** Mini-bridge: who is the declarer? */
export function declarer(): Question {
  for (;;) {
    const deal = randomDeal();
    const points = Object.fromEntries(SEATS.map((s) => [s, hcp(deal[s])])) as Record<Seat, number>;
    const ns = points.north + points.south;
    if (ns === 20) continue;
    const dealer = pick(SEATS);
    const pair: Seat[] = ns > 20 ? ['north', 'south'] : ['east', 'west'];
    const order = (s: Seat) => (SEATS.indexOf(s) - SEATS.indexOf(dealer) + 4) % 4;
    const [a, b] = pair;
    const tie = points[a] === points[b];
    const winner = points[a] > points[b] || (tie && order(a) < order(b)) ? a : b;
    const pairName = ns > 20 ? 'Nord-Syd' : 'Øst-Vest';
    return {
      prompt: `${SEAT_NAME[dealer]} er giver. Hvem bliver spilfører?`,
      visual: { type: 'hcpTable', hcp: points, dealer },
      answer: { type: 'options', options: seatOptions, correct: [SEATS.indexOf(winner)] },
      explain: `${pairName} har ${ns > 20 ? ns : 40 - ns} point og dermed flest. ${
        tie
          ? `Begge har ${points[a]}, så den, der siger sine point først (regnet fra giveren), bliver spilfører: ${SEAT_NAME[winner]}.`
          : `${SEAT_NAME[winner]} har flest af de to og bliver spilfører.`
      } ${SEAT_NAME[winner === a ? b : a]} bliver blind.`,
    };
  }
}

const STRAINS: Strain[] = ['clubs', 'diamonds', 'hearts', 'spades', 'noTrump'];

/** How many tricks does a contract need? */
export function tricksNeeded(): Question {
  const level = 1 + Math.floor(Math.random() * 7);
  const strain = pick(STRAINS);
  return {
    prompt: `Hvor mange stik skal spilføreren tage i ${level}${STRAIN_LABEL[strain]}?`,
    visual: { type: 'contract', level, strain },
    answer: { type: 'number', min: 7, max: 13, correct: level + 6 },
    explain: `Niveau + 6: ${level} + 6 = ${level + 6} stik. Trumffarven ændrer ikke på antallet.`,
  };
}

/** Part score or game? */
export function isGame(): Question {
  const strain = pick(STRAINS);
  const level = 1 + Math.floor(Math.random() * 5);
  const game = level >= gameLevel(strain);
  return {
    prompt: `Er ${level}${STRAIN_LABEL[strain]} en delkontrakt eller en udgang?`,
    visual: { type: 'contract', level, strain },
    answer: { type: 'options', options: ['Delkontrakt', 'Udgang'], correct: [game ? 1 : 0] },
    explain: `Udgang i ${STRAIN_NAME[strain].toLowerCase()} er ${gameLevel(strain)}${STRAIN_LABEL[strain]} eller højere (${gameLevel(strain) + 6} stik). ${level}${STRAIN_LABEL[strain]} er derfor ${game ? 'en udgang' : 'en delkontrakt'}.`,
  };
}

/** Which strain should North-South play? Only deals with a clear answer. */
export function trumpChoice(): Question {
  for (;;) {
    const deal = randomDeal();
    const f = fits(deal.south, deal.north);
    const total = (suit: Suit) => f.filter((x) => x.suit === suit).reduce((n, x) => n + x.mine + x.partner, 0);
    const eight = f.filter((x) => x.mine + x.partner >= 8);
    // Skip deals with two 8-card fits, or a minor fit next to nothing (the answer would be debatable).
    if (eight.length > 1) continue;
    if (eight.length === 1 && (eight[0].suit === 'clubs' || eight[0].suit === 'diamonds')) continue;
    const tip = suggestContract(25, f);
    const correct = STRAINS.indexOf(tip.strain);
    return {
      prompt: 'Nord og Syd har tilsammen 25 point. Hvad skal de spille?',
      visual: { type: 'twoHands', north: deal.north, south: deal.south },
      answer: { type: 'options', options: STRAINS.map((s) => STRAIN_LABEL[s]), correct: [correct] },
      explain:
        tip.strain === 'noTrump'
          ? `Ingen farve har 8 kort tilsammen (${SUITS.map((s) => `${SUIT_SYMBOL[s]} ${total(s)}`).join(', ')}), så uden trumf er bedst.`
          : `I har ${total(tip.strain as Suit)} ${lower(tip.strain as Suit)} sammen: en fit på 8 kort eller flere i en højfarve er den bedste trumffarve.`,
    };
  }
}

/** Opening leader: left of the declarer. */
export function openingLeader(): Question {
  const decl = pick(SEATS);
  const leader = leftOf(decl);
  return {
    prompt: `${SEAT_NAME[decl]} er spilfører. Hvem spiller ud til første stik?`,
    answer: { type: 'options', options: seatOptions, correct: [SEATS.indexOf(leader)] },
    explain: `Spilleren til venstre for spilføreren spiller ud. Til venstre for ${SEAT_NAME[decl]} sidder ${SEAT_NAME[leader]} (spillet går med uret: Nord, Øst, Syd, Vest).`,
  };
}

/** Sure winners for North-South in each suit: top cards in a row, at most as many as the longer hand has. */
export function sureWinners(north: Card[], south: Card[]) {
  return SUITS.map((suit) => {
    const ours = [...north, ...south].filter((c) => c.suit === suit);
    let tops = 0;
    for (let v = 14; v >= 2; v--) {
      const ourCard = ours.some((c) => rankValue(c.rank) === v);
      if (!ourCard) break;
      tops++;
    }
    const longer = Math.max(suitLength(north, suit), suitLength(south, suit));
    return { suit, tops, sure: Math.min(tops, longer) };
  });
}

/** "How many sure tricks?" in no trump, with North as dummy. */
export function countWinners(): Question {
  const deal = randomDeal();
  const per = sureWinners(deal.north, deal.south);
  const total = per.reduce((n, s) => n + s.sure, 0);
  const parts = per.filter((s) => s.sure > 0).map((s) => `${SUIT_SYMBOL[s.suit]} ${s.sure}`);
  const capped = per.find((s) => s.tops > s.sure);
  return {
    prompt: 'Du spiller i uden trumf. Hvor mange sikre stik har Nord og Syd tilsammen?',
    visual: { type: 'twoHands', north: deal.north, south: deal.south },
    answer: { type: 'number', min: 0, max: 13, correct: total },
    explain: `${parts.length ? parts.join(', ') : 'Ingen farve har esset'}: i alt ${total} sikre stik.${
      capped
        ? ` I ${SUIT_SYMBOL[capped.suit]} har I ${capped.tops} topkort, men den længste hånd har kun ${capped.sure} kort, så det giver ${capped.sure} stik.`
        : ''
    } Et sikkert stik er et topkort, I kan tage uden at give udspillet væk.`,
  };
}

/** Losers in one suit of declarer's hand, looking at the top three cards. */
export function suitLosers(hand: Card[], suit: Suit) {
  const cards = cardsInSuit(hand, suit);
  const looked = Math.min(cards.length, 3);
  const honours: Card['rank'][] = ['ace', 'king', 'queen'];
  let losers = 0;
  for (let i = 0; i < looked; i++) if (!cards.some((c) => c.rank === honours[i])) losers++;
  return losers;
}

/** "How many losers?" in declarer's hand, trump contract. */
export function countLosers(): Question {
  let hand = randomDeal().south;
  // A hand worth playing in a suit: a five-card suit or longer.
  while (shape(hand)[0] < 5) hand = randomDeal().south;
  const per = SUITS.map((s) => ({ suit: s, losers: suitLosers(hand, s), length: suitLength(hand, s) }));
  const total = per.reduce((n, s) => n + s.losers, 0);
  return {
    prompt: 'Du er spilfører i et trumfspil. Hvor mange tabere har din hånd?',
    visual: { type: 'hand', cards: hand },
    answer: { type: 'number', min: 0, max: 13, correct: total },
    explain: `${per.map((s) => `${SUIT_SYMBOL[s.suit]} ${s.losers}`).join(', ')}: i alt ${total} tabere. Se på de tre øverste kort i hver farve: hvert manglende es, konge og dame er en taber (en single taber kun, hvis den ikke er esset; en doubleton højst to).`,
  };
}

/** The conventional card to lead from a suit, with the reason. */
export function leadFromSuit(hand: Card[], suit: Suit, trump: Suit | null): { card: Card; why: string } {
  const cards = cardsInSuit(hand, suit);
  const v = (c: Card) => rankValue(c.rank);
  if (cards.length >= 2 && v(cards[0]) >= 10 && v(cards[0]) - v(cards[1]) === 1)
    return { card: cards[0], why: 'Med to kort i træk øverst (fx K-D) spiller du det øverste ud. Så ved makker, at du har kortet lige under.' };
  if (trump && cards[0].rank === 'ace')
    return { card: cards[0], why: 'Mod en trumfkontrakt spiller man ikke under et es: spilføreren kan have kongen og vinde, og esset kan blive stjålet senere. Spil esset.' };
  if (cards.length <= 2) return { card: cards[0], why: 'Fra to kort spiller du det højeste ud.' };
  if (cards.length >= 4) return { card: cards[3], why: 'Fra fire kort eller flere uden kort i træk spiller du det 4. højeste ud.' };
  if (v(cards[0]) >= 11) return { card: cards[2], why: 'Fra tre kort med en honnør spiller du det laveste ud og gemmer honnøren.' };
  return { card: cards[0], why: 'Fra tre små kort spiller du det højeste ud. Så ved makker, at du ikke har noget godt i farven.' };
}

/** "Which card do you lead?" in a given suit. */
export function leadCard(): Question {
  const trump = Math.random() < 0.5 ? pick(SUITS) : null;
  for (;;) {
    const hand = randomDeal().south;
    const suits = SUITS.filter((s) => s !== trump && suitLength(hand, s) >= 2);
    if (suits.length === 0) continue;
    const suit = pick(suits);
    const { card, why } = leadFromSuit(hand, suit, trump);
    const contract = trump ? `4${SUIT_SYMBOL[trump]}` : '3UT';
    return {
      prompt: `Modstanderne spiller ${contract}, og du skal spille ud. Du har valgt at spille ${lower(suit)} ud. Hvilket kort?`,
      answer: { type: 'card', hand, correct: [card] },
      explain: `${cardLabel(card)}. ${why}`,
    };
  }
}

/** The cards in a hand that are worth the same as the highest one (no card in between left with others). */
function topSequence(hand: Card[], suit: Suit, gone: Card[]) {
  const mine = cardsInSuit(hand, suit);
  const result = [mine[0]];
  for (let v = rankValue(mine[0].rank) - 1; v >= 2; v--) {
    const card = mine.find((c) => rankValue(c.rank) === v);
    if (card) result.push(card);
    else if (!gone.some((c) => c.suit === suit && rankValue(c.rank) === v)) break;
  }
  return result;
}

/** Third hand high: partner leads low, dummy plays low, South plays high (the lowest of touching cards). */
export function thirdHand(): Question {
  for (;;) {
    const deal = randomDeal();
    const suit = pick(SUITS);
    const north = cardsInSuit(deal.north, suit);
    const east = cardsInSuit(deal.east, suit);
    const south = cardsInSuit(deal.south, suit);
    if (north.length < 3 || east.length < 1 || south.length < 2) continue;
    const lead = north[north.length - 1];
    const dummy = east[east.length - 1];
    if (rankValue(lead.rank) > 9 || rankValue(dummy.rank) > 9) continue;
    const high = south[0];
    if (rankValue(high.rank) < 10 || rankValue(high.rank) < rankValue(dummy.rank)) continue;
    const seq = topSequence(deal.south, suit, [lead, dummy]);
    const card = seq[seq.length - 1];
    return {
      prompt: 'Vest spiller 3UT. Makker (Nord) spiller ud, og blinden (Øst) lægger lavt. Hvilket kort lægger du?',
      visual: { type: 'trick', plays: [{ seat: 'north', card: lead }, { seat: 'east', card: dummy }], trump: null },
      answer: { type: 'card', hand: deal.south, correct: [card], legal: south },
      explain:
        seq.length > 1
          ? `${cardLabel(card)}. Tredje hånd spiller højt, så spilføreren må bruge et højt kort for at vinde. Med kort i træk (${seq.map(cardLabel).join(', ')}) lægger du det laveste af dem.`
          : `${cardLabel(card)}. Tredje hånd spiller højt, så spilføreren må bruge et højt kort for at vinde stikket.`,
    };
  }
}

/** Second hand low: dummy leads a low card, South plays low. */
export function secondHand(): Question {
  for (;;) {
    const deal = randomDeal();
    const suit = pick(SUITS);
    const east = cardsInSuit(deal.east, suit);
    const south = cardsInSuit(deal.south, suit);
    if (east.length < 2 || south.length < 2) continue;
    const lead = east[east.length - 1];
    if (rankValue(lead.rank) > 9 || rankValue(south[0].rank) < 11) continue;
    const low = south[south.length - 1];
    return {
      prompt: 'Vest spiller 3UT. Blinden (Øst) spiller et lille kort ud. Du sidder lige efter. Hvilket kort lægger du?',
      visual: { type: 'trick', plays: [{ seat: 'east', card: lead }], trump: null },
      answer: { type: 'card', hand: deal.south, correct: [low], legal: south },
      explain: `${cardLabel(low)}. Anden hånd spiller lavt: makker sidder sidst og kan vinde stikket billigt, og dine høje kort skal bruges til at slå spilførerens honnører.`,
    };
  }
}

/** "Partner asks with 4UT (Blackwood): what do you answer?" */
export function blackwoodAnswer(): Question {
  const hand = randomDeal().south;
  const aces = hand.filter((c) => c.rank === 'ace').length;
  const options = ['5♣', '5♦', '5♥', '5♠'];
  const names = ['0 eller 4 esser', '1 es', '2 esser', '3 esser'];
  return {
    prompt: 'I har fundet en fit i spar, og makker melder 4UT (Blackwood). Hvad svarer du?',
    visual: { type: 'hand', cards: hand },
    answer: { type: 'options', options, correct: [aces % 4] },
    explain: `Du har ${aces === 1 ? '1 es' : `${aces} esser`}, så du svarer ${options[aces % 4]} (${names[aces % 4]}).`,
  };
}

/** "How high?" from the points together: part score, game, small or grand slam. */
export function slamLevel(): Question {
  const total = pick([20, 22, 24, 26, 28, 30, 31, 33, 34, 35, 37, 38]);
  const options = ['Delkontrakt', 'Udgang', 'Lilleslem', 'Storeslem'];
  const correct = total >= 37 ? 3 : total >= 33 ? 2 : total >= 25 ? 1 : 0;
  return {
    prompt: `I har en god fit og ${total} HP tilsammen. Hvor højt skal I melde?`,
    answer: { type: 'options', options, correct: [correct] },
    explain: `Tommelfingerregel: 25 HP til udgang, 33 til lilleslem (12 stik) og 37 til storeslem (13 stik). Med ${total} HP er det ${options[correct].toLowerCase()}.`,
  };
}
