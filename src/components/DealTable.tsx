import type { DealInfo, Seat } from '../types';
import { HandView } from './HandView';

/** All four hands in compass layout (North on top). */
export function DealTable({ deal }: { deal: DealInfo }) {
  return (
    <div className="table">
      {(['north', 'west', 'east', 'south'] as Seat[]).map((seat) => {
        const hand = deal.hands.find((h) => h.seat === seat);
        return (
          <div key={seat} className={`slot ${seat}`}>
            {hand && <HandView hand={hand} dealer={deal.dealer === seat} />}
          </div>
        );
      })}
      <div className="slot center" aria-hidden>
        <div className="compass">
          <span>N</span>
          <span>V</span>
          <span>Ø</span>
          <span>S</span>
        </div>
      </div>
    </div>
  );
}
