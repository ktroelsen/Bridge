import { cardsInSuit, SEAT_NAME, STRAIN_NAME, SUIT_NAME, SUITS } from './cards';
import type { Card, Seat, Strain, Suit } from './types';

/** Short advice for the player's turn, in the style of a teacher at the table. */
export function playAdvice(opts: {
  seat: Seat;
  hand: Card[];
  legal: Card[];
  ledSuit: Suit | null;
  trump: Suit | null;
  openingLead: boolean;
  fromDummy: boolean;
}): string {
  const { seat, hand, legal, ledSuit, trump, openingLead, fromDummy } = opts;
  const who = fromDummy ? `Du spiller fra blinden (${SEAT_NAME[seat]}). ` : '';

  if (!ledSuit) {
    if (openingLead)
      return `${who}Du skal spille ud. Et godt udspil er toppen af en sekvens (fx K fra K-D-Kn) eller 4. højeste fra din længste farve.`;
    return `${who}Du har vundet stikket og skal spille ud. Tag dine sikre stik, eller spil på en farve, der kan give flere stik.`;
  }
  if (legal.length < hand.length)
    return `${who}Du skal bekende kulør og spille ${SUIT_NAME[ledSuit].toLowerCase()}.`;
  if (trump && hand.some((c) => c.suit === trump))
    return `${who}Du har ingen ${SUIT_NAME[ledSuit].toLowerCase()}. Du kan trumfe med ${SUIT_NAME[trump].toLowerCase()} eller smide et andet kort.`;
  return `${who}Du har ingen ${SUIT_NAME[ledSuit].toLowerCase()}, så du smider et kort. Det kan ikke vinde stikket, så vælg et lille kort, du ikke får brug for.`;
}

export interface Fit {
  suit: Suit;
  mine: number;
  partner: number;
}

export function fits(hand: Card[], partner: Card[]): Fit[] {
  return SUITS.map((suit) => ({
    suit,
    mine: cardsInSuit(hand, suit).length,
    partner: cardsInSuit(partner, suit).length,
  }));
}

/** The contract a teacher would suggest (the same rules the bot uses as declarer). */
export function suggestContract(hcp: number, f: Fit[]): { strain: Strain; game: boolean; why: string } {
  const total = (s: Suit) => f.find((x) => x.suit === s)!.mine + f.find((x) => x.suit === s)!.partner;
  const major = total('spades') >= total('hearts') ? 'spades' : 'hearts';
  const minor = total('diamonds') >= total('clubs') ? 'diamonds' : 'clubs';
  if (total(major) >= 8)
    return {
      strain: major,
      game: hcp >= 25,
      why: `I har ${total(major)} ${STRAIN_NAME[major].toLowerCase()} sammen. En højfarve med 8 kort eller flere er en god trumffarve.`,
    };
  if (hcp >= 25) return { strain: 'noTrump', game: true, why: 'Ingen 8-korts fit i en højfarve, men 25+ point: prøv 3 uden trumf.' };
  if (total(minor) >= 8)
    return {
      strain: minor,
      game: hcp >= 29,
      why: `I har ${total(minor)} ${STRAIN_NAME[minor].toLowerCase()} sammen. Udgang i en lavfarve kræver 11 stik, så nøjes med delkontrakt.`,
    };
  return { strain: 'noTrump', game: false, why: 'Ingen god trumffarve og under 25 point: spil delkontrakt i uden trumf.' };
}
