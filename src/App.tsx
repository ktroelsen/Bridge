import { useState } from 'react';
import { DealViewer } from './components/DealViewer';
import { MiniBridgeView } from './components/MiniBridgeView';

type View = 'home' | 'minibridge' | 'points';

const COMING = [
  { title: 'Lær bridge', text: 'Lektioner fra bunden: stik, trumf, point, meldinger og spilføring.' },
  { title: 'Fuldt spil mod bot', text: 'Meld og spil med en bot som makker, og få hints undervejs.' },
];

export default function App() {
  const [view, setView] = useState<View>('home');
  const home = () => setView('home');

  return (
    <main className="page">
      <header className="title">
        <h1>Bridge-træner</h1>
        {view === 'home' && <p>Lær bridge fra bunden og øv dig mod en bot.</p>}
      </header>

      {view === 'home' && (
        <>
          <section className="panel menu">
            <button type="button" className="menu-item" onClick={() => setView('minibridge')}>
              <strong>Mini-bridge mod bot</strong>
              <span>
                Bridge uden meldinger. Parret med flest point spiller, og du lærer stik, trumf og spilføring. Det
                bedste sted at starte.
              </span>
            </button>
            <button type="button" className="menu-item" onClick={() => setView('points')}>
              <strong>Tæl point</strong>
              <span>Se alle fire hænder og øv dig i at tælle honnørpoint.</span>
            </button>
          </section>
          <section className="panel">
            <h2>På vej</h2>
            <ul className="coming">
              {COMING.map((c) => (
                <li key={c.title}>
                  <strong>{c.title}</strong>
                  <span>{c.text}</span>
                </li>
              ))}
            </ul>
          </section>
        </>
      )}

      {view === 'minibridge' && <MiniBridgeView onBack={home} />}
      {view === 'points' && <DealViewer onBack={home} />}
    </main>
  );
}
