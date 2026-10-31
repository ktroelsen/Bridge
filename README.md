# Bridge-træner

Lær bridge fra bunden og øv dig mod en bot. Appen kan installeres som PWA og kører på
`bridge.kaspert.dk`. Den samlede plan står i [PLAN.md](PLAN.md).

**Status:** Fase 0 (skelet og deploy), fase 1 (mini-bridge mod bot), fase 2 (meldinger og
fuldt spil) og første del af fase 3 (lektioner 1–5) er på plads. Man kan:

- **Spille bridge med meldinger mod botterne.** Du melder som Syd med en bot som makker.
  Hver melding kan trykkes på og forklares ("Åbning (12–21 HP, mindst 5 ♥)"), "Hvad siger
  systemet?" giver systemets melding, og melder du noget andet end systemet, ser du, hvad
  systemet ville have meldt og hvorfor. Man kan fortryde meldinger og kort.

- **Tage lektioner:** Spillets gang, Trumf, Tæl point, Mini-bridge, Kontrakter og udgang,
  Åbningsmeldinger, Svar på åbning, Genmelding, 1UT-systemet, Konkurrencemeldinger, to
  lektioner om spilføring, Modspil og Slem (med Blackwood). Spilførings- og modspilslektionerne har øvelser i at tælle
  sikre stik og tabere, udspil, anden hånd lavt og tredje hånd højt, og "Find det bedste
  kort"-opgaver med alle fire hænder åbne, løst af double dummy-løseren
  (`GET /api/quiz/play?stage=early|middle|defend`).
  Meldelektionerne har "Hvad melder du?"-øvelser, hvor serveren laver en hånd og en
  melderække, og svaret er meldesystemets egen melding med forklaring
  (`GET /api/quiz/bidding?kind=opening|response|ntResponse|rebid|ntOpener|overcall|advance`). Hver
  lektion er korte tekster og øvelser. Øvelserne laves på ny hver gang ud fra tilfældige hænder
  og stik: hvem vinder stikket, bekend kulør, vind stikket med trumf, tæl point, jævn/ujævn,
  hvem bliver spilfører, hvem spiller ud, antal stik, delkontrakt/udgang og valg af
  trumffarve. Efter nogle lektioner kan man øve i mini-bridge med en passende kortgivning
  (fx "du er spilfører" eller "I har en trumffit").
- **Gemme sit fremskridt** under sit navn (intet login). Fremskridtet gemmes også i browseren,
  så appen virker uden navn.

- **Spille mini-bridge mod botten.** Det er bridge uden meldinger. Parret med flest point
  spiller, og spilføreren vælger trumf og delkontrakt eller udgang. Du sidder Syd og spiller
  også Nord, når I har kontrakten. En trænerlinje forklarer, hvad du skal (bekende, trumfe,
  udspil), og træneren kan foreslå en kontrakt.
- **Få hjælp i kortspillet:** "Hvad er bedst?" markerer de bedste kort, og efter spillet viser
  "Gennemgå spillet", hvor mange stik spilføreren kunne have taget med perfekt spil, og hvilke
  kort der kostede stik (dine, makkers og modstandernes). "Hvad kunne vindes?" viser stik for
  hver spilfører i hver farve med perfekt spil og den bedste kontrakt for hvert par. Begge dele bruger en double
  dummy-løser (alle kort åbne) i `Bridge.Engine/Analysis/`.
- **Følge med i statistikken:** med et navn gemmes hvert færdigt spil (serveren genspiller det
  og regner selv resultatet ud). Statistiksiden viser, hvor ofte du vinder dine kontrakter og
  sætter modstandernes pr. botniveau, og dine seneste spil med "Spil igen".
- **Tælle point:** se kortgivninger med alle fire hænder åbne.

Lektioner om meldinger og fuldt spil med meldinger kommer i de næste faser.

**Lektionerne** ligger i frontenden (`src/lessons/content.ts`) som tekst, faste spørgsmål og
øvelser (`src/lessons/drills.ts`, der laver nye spørgsmål ud fra tilfældige kort). Serveren
kender kun lektionernes id'er og gemmer den bedste score pr. spiller i SQLite
(`App_Data/bridge.db`, som deploy springer over).

## Arkitektur

| Del | Teknologi | Mappe |
|---|---|---|
| Frontend | React 19 + TypeScript + Vite + Tailwind 4, PWA | `src/`, `public/` |
| API | ASP.NET Core minimal API (.NET 10) | `server/Bridge.Api/` |
| Kort, regler og (senere) bot | C#-klassebibliotek | `server/Bridge.Engine/` |
| Tests | xUnit (backend), Vitest (frontend) | `server/Bridge.Tests/`, `src/*.test.ts` |

**Spillet gemmes ikke på serveren** (samme model som Backgammon). Et mini-bridge-spil er fuldt
bestemt af *spilnummer + valgt kontrakt + dine kort*. Frontenden sender hele requesten ved
hvert kort. Serveren genspiller spillet, kontrollerer at dit kort er lovligt, lader botten
spille frem til din næste tur og returnerer kun de kort, du må se. Det igangværende spil
gemmes i browseren.

**Botten (niveau Let)** følger de klassiske begynderregler: 2. hånd lav, 3. hånd højt, vind
billigst muligt, overtag ikke makkers stik, trumf når du ikke kan bekende, udspil fra toppen
af en sekvens eller 4. højeste. Som spilfører trækker den trumf og tager sine sikre stik.
Den beslutter kun ud fra det, den må se (`PlayView`): egen hånd, blinden og spillede kort.

**Meldinger.** `Bridge.Engine/Bidding/BeginnerSystem.cs` er et naturligt begyndersystem
(5-farvers højfarve, 1UT 15–17, 2♣ stærk, Stayman, overføringer, indmeldinger,
oplysningsdobling og Blackwood) skrevet som en liste af regler. Hver regel siger, hvornår den gælder, hvilken
melding den giver, og hvad meldingen viser (`Shows`: HP-interval, længder, jævn fordeling).
Den samme liste bruges til at botten melder, til at forklare alle meldinger, til hints og
til at bygge et billede af makkers hånd. Når ingen regel passer, placerer botten kontrakten
ud fra det, makker har vist.

**Botternes kortspil** har tre niveauer, som man vælger over bordet. **Let** er
tommelfingerreglerne ovenfor. **Mellem** (fra 4. stik) og **Svær** (hele spillet) trækker
fordelinger af de skjulte kort, der passer med det, botten ved (egen hånd, blinden, spillede
kort, renoncer og det, meldingerne eller de oplyste point viste), løser hver med double
dummy-løseren og spiller det kort, der oftest vinder eller sætter kontrakten
(`Bridge.Engine/Analysis/PlayBot.cs`). Fordi spillet genspilles ved hver request, er botten
deterministisk: prøverne er seeded af stillingen, og løseren begrænses af antal stillinger,
ikke af tid. Valgene caches. I en simulering over 40 mini-bridge-spil vandt Svær som spilfører
98 % af kontrakterne mod 80 % for Let, og Svær i modspil fik Let-spilføreren ned på 68 %.
Svær bruger i gennemsnit 0,2 s pr. beslutning (højst ~1,5 s).

**Kortgivninger** er bestemt af et spilnummer. `Rng` er SplitMix64, som giver de samme tal på
alle platforme og .NET-versioner. Kortene blandes med Fisher-Yates, og giver og zone følger
den almindelige duplikat-cyklus på 16 spil. En golden test sikrer, at et spilnummer altid
giver de samme kort.

## Kør lokalt

Forudsætninger: Node.js 22+ og .NET 10 SDK.

```bash
# Terminal 1: API på http://localhost:5099
cd server/Bridge.Api && dotnet run

# Terminal 2: frontend på http://localhost:3000 (Vite proxyer /api til backend)
npm ci
npm run dev
```

Tests og checks:

```bash
npm run lint                      # tsc --noEmit
npm test                          # Vitest
dotnet test server/Bridge.slnx    # motor og API
```

## Deploy til Simply.com

Opsætningen er den samme som i Backgammon-projektet:

- `.github/workflows/ci.yml` bygger og tester på PR'er og ved push til `main`.
- `.github/workflows/deploy.yml` kører, når CI er grøn på `main` (eller manuelt). Den:
  - bygger frontenden ind i `server/Bridge.Api/wwwroot`
  - publicerer **self-contained win-x86** (Simply har ikke .NET 10 og kører 32-bit app pool)
  - synkroniserer til sitet med Web Deploy (msdeploy) og springer `App_Data` over

Engangsopsætning:

1. Opret subdomænet `bridge.kaspert.dk` hos Simply (Windows/IIS) og slå **Web Deploy** til.
2. Opret GitHub Environment `production` med secrets:
   - `SIMPLY_WEBDEPLOY_URL`
   - `SIMPLY_WEBDEPLOY_SITE`
   - `SIMPLY_WEBDEPLOY_USERNAME`
   - `SIMPLY_WEBDEPLOY_PASSWORD`
3. Merge til `main`. Så deployer pipelinen selv.

## API

- `GET /api/health` svarer `{ "status": "ok" }`.
- `GET /api/deal?number=482913` returnerer et spil med alle fire hænder, HP, giver og zone.
  Hvis `number` udelades, vælges et tilfældigt spil.
- `POST /api/minibridge` spiller mini-bridge:

  ```json
  { "number": 482913, "contract": { "strain": "spades", "game": true }, "cards": [{ "suit": "hearts", "rank": "king" }] }
  ```

  - Udelad `number` for et nyt tilfældigt spil. `practice` (`declare`, `defend`, `trumpFit`,
    `noTrump`) vælger så et spil af den slags.
  - `contract` er kun med, når Nord-Syd er spilfører (ellers vælger botten).
  - `cards` er dine kort i rækkefølge, også dem du spiller fra Nord.
  - Svaret indeholder fase, HP for alle, kontrakt, de hænder du må se, stikkene, dine lovlige
    kort og til sidst resultat og alle fire hænder.
- `POST /api/game` spiller bridge med meldinger:

  ```json
  { "number": 482913, "calls": ["1H", "P", "2H"], "cards": [] }
  ```

  - `calls` er dine meldinger (Syd) i rækkefølge: `1C`…`7N`, `P`, `X`, `XX`.
  - Svaret indeholder meldingerne med forklaring, dine lovlige meldinger, systemets forslag
    (`hint`), feedback på dine meldinger (`systemCall`), kontrakten og resten som ved
    mini-bridge.
- `POST /api/game/hint` og `POST /api/minibridge/hint` (samme body som spillet): de bedste
  kort for dig lige nu og antal stik, din side så tager med perfekt spil.
- `POST /api/game/review` og `POST /api/minibridge/review` (et færdigt spil): stik med perfekt
  spil og hver fejl med stik, sæde, kort, hvad den kostede og bedre kort.
- `POST /api/game/table?strain=spades` og `POST /api/minibridge/table?strain=...` (et færdigt
  spil): stik for spilfører Nord, Øst, Syd og Vest i én farve (-1 hvis det tog for lang tid).
  Siden henter én farve ad gangen.
- `POST /api/players/{navn}/games/mini` og `.../games/full` (et færdigt spil, samme body som
  spillet) gemmer spillet; samme spil gemmes kun én gang. `GET /api/players/{navn}/stats` giver
  statistik pr. botniveau og de seneste 30 spil.
- `POST /api/players` med `{ "name": "Kasper" }` opretter eller finder en spiller (navnet er
  nøglen, uden forskel på store og små bogstaver).
- `GET /api/players/{navn}/progress` giver den bedste score pr. lektion.
- `PUT /api/players/{navn}/progress/{lektion}` med `{ "score": 80 }` gemmer et resultat. Kun
  den bedste score beholdes.
