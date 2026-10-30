import { Fragment } from 'react';

/** Text with **bold** as the only markup, plus red suit symbols. */
export function Rich({ text }: { text: string }) {
  return (
    <>
      {text.split(/(\*\*[^*]+\*\*)/).map((part, i) =>
        part.startsWith('**') ? (
          <strong key={i}>
            <Suits text={part.slice(2, -2)} />
          </strong>
        ) : (
          <Fragment key={i}>
            <Suits text={part} />
          </Fragment>
        ),
      )}
    </>
  );
}

function Suits({ text }: { text: string }) {
  return (
    <>
      {text.split(/([♥♦])/).map((part, i) =>
        part === '♥' || part === '♦' ? (
          <span key={i} className="red">
            {part}
          </span>
        ) : (
          <Fragment key={i}>{part}</Fragment>
        ),
      )}
    </>
  );
}
