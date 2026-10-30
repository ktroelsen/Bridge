import { callIsRed, callLabel, SEAT_NAME } from '../cards';
import type { CallInfo, Seat } from '../types';

const COLUMNS: Seat[] = ['west', 'north', 'east', 'south'];

/** The bidding as a table with West, North, East, South columns. Every call can be tapped to see what it means. */
export function AuctionTable({
  dealer,
  calls,
  selected,
  onSelect,
  waiting,
}: {
  dealer: Seat;
  calls: CallInfo[];
  selected: number | null;
  onSelect: (index: number) => void;
  /** Show a "?" where the next call goes. */
  waiting: boolean;
}) {
  const offset = COLUMNS.indexOf(dealer);
  const cells: ({ index: number; call: CallInfo } | null | 'next')[] = [...Array(offset).fill(null), ...calls.map((call, index) => ({ index, call }))];
  if (waiting) cells.push('next');
  const rows: (typeof cells)[] = [];
  for (let i = 0; i < cells.length; i += 4) rows.push(cells.slice(i, i + 4));

  return (
    <table className="auction">
      <thead>
        <tr>
          {COLUMNS.map((s) => (
            <th key={s} className={s === 'south' ? 'me' : ''}>
              {SEAT_NAME[s]}
              {s === dealer ? ' (G)' : ''}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {rows.map((row, r) => (
          <tr key={r}>
            {Array.from({ length: 4 }, (_, c) => {
              const cell = row[c];
              if (cell === 'next')
                return (
                  <td key={c}>
                    <span className="call next">?</span>
                  </td>
                );
              if (!cell) return <td key={c} />;
              const { index, call } = cell;
              return (
                <td key={c}>
                  <button
                    type="button"
                    className={`call${callIsRed(call.call) ? ' red' : ''}${selected === index ? ' selected' : ''}${call.systemCall ? ' differs' : ''}`}
                    onClick={() => onSelect(index)}
                    aria-label={`${SEAT_NAME[call.seat]}: ${callLabel(call.call)}`}
                  >
                    {callLabel(call.call)}
                  </button>
                </td>
              );
            })}
          </tr>
        ))}
      </tbody>
    </table>
  );
}
