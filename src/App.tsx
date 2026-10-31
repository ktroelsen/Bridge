import { DealViewer } from './components/DealViewer';
import { GameView } from './components/GameView';
import { LessonList } from './components/LessonList';
import { LessonView } from './components/LessonView';
import { MiniBridgeView } from './components/MiniBridgeView';
import { StatsView } from './components/StatsView';
import { lessonById, LESSONS } from './lessons/content';
import { usePlayer } from './player';
import { useRoute } from './router';
import type { Practice } from './types';

const PRACTICES: Practice[] = ['any', 'declare', 'defend', 'trumpFit', 'noTrump'];

export default function App() {
  const [path, navigate] = useRoute();
  const player = usePlayer();
  const home = () => navigate('/');

  const [route, query = ''] = path.split('?');
  const parts = route.split('/').filter(Boolean);
  const practiceParam = new URLSearchParams(query).get('ov') as Practice | null;
  const practice: Practice = practiceParam && PRACTICES.includes(practiceParam) ? practiceParam : 'any';
  const lesson = parts[0] === 'lektioner' && parts[1] ? lessonById(parts[1]) : undefined;

  let page;
  if (lesson) page = <LessonView key={lesson.id} lesson={lesson} player={player} navigate={navigate} />;
  else if (parts[0] === 'lektioner') page = <LessonList player={player} navigate={navigate} />;
  else if (parts[0] === 'minibridge') page = <MiniBridgeView key={practice} onBack={home} practice={practice} player={player.name} />;
  else if (parts[0] === 'point') page = <DealViewer onBack={home} />;
  else if (parts[0] === 'spil') page = <GameView onBack={home} player={player.name} />;
  else if (parts[0] === 'statistik') page = <StatsView player={player} navigate={navigate} />;
  else page = <Home player={player} navigate={navigate} />;

  return (
    <main className="page">
      <header className="title">
        <h1>Bridge-træner</h1>
        {parts.length === 0 && <p>Lær bridge fra bunden og øv dig mod en bot.</p>}
      </header>
      {page}
    </main>
  );
}

function Home({ player, navigate }: { player: ReturnType<typeof usePlayer>; navigate: (path: string) => void }) {
  const done = LESSONS.filter((l) => player.progress[l.id] !== undefined).length;
  const next = LESSONS.find((l) => player.progress[l.id] === undefined);
  return (
    <>
      <section className="panel menu">
        {player.name && (
          <p className="greeting">
            Hej {player.name}!{' '}
            <button type="button" className="link" onClick={player.forgetName}>
              Skift spiller
            </button>
          </p>
        )}
        <button type="button" className="menu-item" onClick={() => navigate(next ? `/lektioner/${next.id}` : '/lektioner')}>
          <strong>Lær bridge</strong>
          <span>
            {done === 0
              ? 'Start her: korte lektioner med øvelser, fra kortene og stikket til kontrakter.'
              : `Du har taget ${done} af ${LESSONS.length} lektioner.${next ? ` Næste: ${next.title}.` : ' Godt gået!'}`}
          </span>
        </button>
        <button type="button" className="link left" onClick={() => navigate('/lektioner')}>
          Se alle lektioner
        </button>
        <button type="button" className="menu-item" onClick={() => navigate('/minibridge')}>
          <strong>Mini-bridge mod bot</strong>
          <span>Bridge uden meldinger. Parret med flest point spiller, og du øver stik, trumf og spilføring.</span>
        </button>
        <button type="button" className="menu-item" onClick={() => navigate('/spil')}>
          <strong>Bridge mod bot</strong>
          <span>Rigtig bridge med meldinger. Du melder med en bot som makker, kan se hvad hver melding betyder, og kan spørge systemet om råd.</span>
        </button>
        <button type="button" className="menu-item" onClick={() => navigate('/statistik')}>
          <strong>Statistik</strong>
          <span>Dine spil: hvor ofte du vinder dine kontrakter og sætter modstandernes.</span>
        </button>
        <button type="button" className="menu-item" onClick={() => navigate('/point')}>
          <strong>Tæl point</strong>
          <span>Se alle fire hænder og øv dig i at tælle honnørpoint.</span>
        </button>
      </section>
      <section className="panel">
        <h2>På vej</h2>
        <ul className="coming">
          <li>
            <strong>Egne spil</strong>
            <span>Læg spil ind fra bøger eller klubaftener (PBN) og spil dem mod botterne.</span>
          </li>
        </ul>
      </section>
    </>
  );
}
