# Bridge-træner

Lær bridge fra bunden og øv dig mod en bot. Appen kan installeres som PWA og kører på
`bridge.kaspert.dk`. Den samlede plan står i [PLAN.md](PLAN.md).

**Status:** Fase 0 (skelet og deploy) og fase 1 (mini-bridge mod bot) er på plads. Man kan:

- **Spille mini-bridge mod botten.** Det er bridge uden meldinger. Parret med flest point
  spiller, og spilføreren vælger trumf og delkontrakt eller udgang. Du sidder Syd og spiller
  også Nord, når I har kontrakten. En trænerlinje forklarer, hvad du skal (bekende, trumfe,
  udspil), og træneren kan foreslå en kontrakt.
- **Tælle point:** se kortgivninger med alle fire hænder åbne.

Lektioner og fuldt spil med meldinger kommer i de næste faser.

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

  - Udelad `number` for et nyt tilfældigt spil.
  - `contract` er kun med, når Nord-Syd er spilfører (ellers vælger botten).
  - `cards` er dine kort i rækkefølge, også dem du spiller fra Nord.
  - Svaret indeholder fase, HP for alle, kontrakt, de hænder du må se, stikkene, dine lovlige
    kort og til sidst resultat og alle fire hænder.
