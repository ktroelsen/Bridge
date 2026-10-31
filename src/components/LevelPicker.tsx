import type { BotLevel } from '../types';

const LEVELS: { level: BotLevel; label: string; title: string }[] = [
  { level: 'easy', label: 'Let', title: 'Botterne følger enkle tommelfingerregler' },
  { level: 'medium', label: 'Mellem', title: 'Botterne regner på de mulige fordelinger fra 4. stik' },
  { level: 'hard', label: 'Svær', title: 'Botterne regner på de mulige fordelinger hele spillet' },
];

/** Choose how well the bots play the cards. Changing it starts a new deal. */
export function LevelPicker({ level, onChange, disabled }: { level: BotLevel; onChange: (level: BotLevel) => void; disabled?: boolean }) {
  return (
    <div className="level-picker" role="group" aria-label="Botternes niveau">
      <span className="muted small">Botter:</span>
      {LEVELS.map((l) => (
        <button
          key={l.level}
          type="button"
          className={l.level === level ? 'selected' : ''}
          title={l.title}
          disabled={disabled}
          aria-pressed={l.level === level}
          onClick={() => l.level !== level && onChange(l.level)}
        >
          {l.label}
        </button>
      ))}
    </div>
  );
}
