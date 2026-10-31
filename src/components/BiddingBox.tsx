import { callIsRed, callLabel } from '../cards';

const STRAIN_LETTERS = ['C', 'D', 'H', 'S', 'N'];

/** Bids 1–7 in all strains plus pass, double and redouble. Only legal calls can be pressed. */
export function BiddingBox({ legal, onCall, disabled, maxLevel = 7 }: { legal: string[]; onCall: (call: string) => void; disabled: boolean; maxLevel?: number }) {
  const lowest = Math.min(...legal.filter((c) => /^\d/.test(c)).map((c) => Number(c[0])), 8);
  const levels = [1, 2, 3, 4, 5, 6, 7].filter((l) => l >= lowest && l <= maxLevel);
  const button = (call: string, wide = false) => (
    <button
      key={call}
      type="button"
      className={`bid${callIsRed(call) ? ' red' : ''}${wide ? ' wide' : ''}`}
      disabled={disabled || !legal.includes(call)}
      onClick={() => onCall(call)}
    >
      {callLabel(call)}
    </button>
  );
  return (
    <div className="bidding-box">
      <div className="bid-row special">
        {button('P', true)}
        {legal.includes('X') && button('X', true)}
        {legal.includes('XX') && button('XX', true)}
      </div>
      {levels.map((level) => (
        <div key={level} className="bid-row">
          {STRAIN_LETTERS.map((s) => button(`${level}${s}`))}
        </div>
      ))}
    </div>
  );
}
