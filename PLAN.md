# Bridge-træner: plan

En web-app (PWA), hvor man lærer bridge fra bunden og øver mod en bot. Frontend i React,
backend i C#, og deploy til Simply.com på samme måde som Backgammon-projektet.

## 1. Mål

1. **Spil mod bot.** Du sidder Syd. Botten spiller makker (Nord) og modstanderne (Øst/Vest).
   Hele forløbet er med: melding, udspil, spilføring og tælling.
2. **Lær pædagogisk.** Lektioner bygget op i små trin med forklaring, interaktive øvelser og
   kurerede spil. Du kan altid få et hint og en forklaring.
3. **Forstå hvorfor.** Hver melding botten afgiver kan forklares ("1♥: 12–19 HP, 5+ hjerter").
   Efter hvert spil vises en analyse af, hvad der kunne have været gjort bedre.

Ikke i første omgang: multiplayer mod andre mennesker, turneringer, avancerede konventioner.

## 2. Arkitektur (genbrugt fra Backgammon)

| Del | Teknologi | Mappe |
|---|---|---|
| Frontend | React 19 + TypeScript + Vite + Tailwind 4, PWA | `src/`, `public/` |
| API | ASP.NET Core minimal API (.NET 10) | `server/Bridge.Api/` |
| Regler, meldinger, bot, analyse | C#-klassebibliotek uden afhængigheder | `server/Bridge.Engine/` |
| Lektioner og øvelser (indhold) | JSON/Markdown-filer | `server/Bridge.Api/Content/` |
| Persistens | SQLite via EF Core i `App_Data` | `server/Bridge.Api/Data/` |
| Tests | xUnit (backend), Vitest (frontend) | `server/Bridge.Tests/`, `src/*.test.ts` |

Én .NET-app serverer både API'et og den byggede React-app fra `wwwroot`, så alt kører på
samme origin. Det er den model, Backgammon bruger.

### Tilstandsløst spil (samme idé som Backgammon)

Et spil er fuldt bestemt af **spilnummer (seed) + botniveau + dine handlinger** (meldinger og
kort). Frontenden sender hele den lille request ved hver handling, og serveren gør følgende:

1. genskaber kortgivningen ud fra seed,
2. afspiller dine handlinger og validerer dem,
3. lader botten handle, indtil det er din tur igen,
4. returnerer kun det, du må se: din hånd, blindens hånd efter udspillet, stik, lovlige
   handlinger og meldeforklaringer.

Det giver nogle fordele:

- Intet går tabt ved genstart af app-poolen på Simply.
- Spilkoder kan deles ("spil spil 482913"), og et spil kan spilles igen.
- Botten kan ikke snyde, for den ser kun sin egen hånd og blinden. Serveren håndhæver det.
- Det igangværende spil gemmes i `localStorage` og fortsætter efter reload.

SQLite bruges kun til det, der skal huskes på tværs af enheder: spillerprofil, gennemførte
lektioner, statistik og historik. Spillere oprettes med et navn, ligesom `NameDialog` i
Backgammon.

## 3. Domænemodel (Bridge.Engine)

```
Suit        Klør, Ruder, Hjerter, Spar (+ UT som "strain" i meldinger)
Rank        2..10, Knægt, Dame, Konge, Es
Card        (Suit, Rank)
Seat        Nord, Øst, Syd, Vest
Hand        13 kort. Har HP (honnørpoint), fordeling og "balanceret?"
Deal        4 hænder + giver + zonestilling (afledt af spilnummer, som ved duplikat)
Call        Pas | Bud(niveau 1-7, strain) | Dobling | Redobling
Auction     Liste af Call. Lovlighedstjek, slutter ved 3 pas, giver Contract
Contract    niveau, strain, spilfører, doblet/redoblet
Trick       4 kort, udspiller, vinder (bekend kulør, trumf)
Play        Kortspil-fasen: hvem er i spil, lovlige kort, stik vundet
Score       Duplikat-tælling: delkontrakt/udgang/slem, zone, doblinger, straf
Rng         SplitMix64, så samme seed giver samme kortgivning på alle platforme
```

Alt herinde er rene funktioner, så det er nemt at teste grundigt.

## 4. Botten

Botten består af to dele, der udvikles hver for sig.

### 4.1 Meldinger: regelmotor med forklaringer

Meldesystemet beskrives som **data** og ikke som hårdkodet `if`-logik: en liste af regler med
betingelse, melding og forklaring.

```jsonc
{
  "id": "open-1M",
  "situation": "åbning",                     // ingen har meldt endnu
  "when": { "hcp": [12, 21], "longestMajor": ">=5" },
  "call": "1{longestMajor}",
  "explain": "12–21 HP og mindst 5 kort i {suit}.",
  "priority": 50
}
```

Den samme regelbase bruges til tre ting. Det er den vigtigste designbeslutning i projektet:

1. **Botten melder.** Den vælger den højest prioriterede regel, der passer på hånden.
2. **Forklaring af andres meldinger.** Ud fra regelbasen vises, hvad en melding lover ("Nord
   viser 6–9 HP og 3+ hjerter").
3. **Feedback til dig.** Når du melder, slår appen op, hvad systemet ville have meldt, og
   forklarer en eventuel forskel.

Botten opbygger desuden en model af hver hånd ud fra meldingerne (HP-interval og min/max
længde pr. farve). Den model bruges senere i kortspillet.

**Meldesystem:** et enkelt naturligt begyndersystem, der minder om det, Danmarks
Bridgeforbund underviser i:

- 5-farvers højfarve, 1UT = 15–17 balanceret, 2♣ stærk
- Stayman og Jacoby-overføringer over 1UT
- enkle indmeldinger og oplysningsdobling
- Blackwood (4UT) til slem

Fordi systemet er data, kan det udvides eller skiftes senere uden at røre motoren.

### 4.2 Kortspil: heuristik + double dummy

Det sker i tre trin efter niveau:

| Niveau | Teknik |
|---|---|
| **Let** | Faste tommelfingerregler: udspil fra top af sekvens, 4. højeste fra lang farve, 2. hånd lav, 3. hånd høj, bekend kulør, stik billigst muligt. |
| **Mellem** | Som Let, plus simpel planlægning: tæl vindere/tabere, træk trumf, tag sikre stik, spil efter farvesplit. |
| **Svær** | **Monte Carlo + double dummy:** generér 30–100 fordelinger af de skjulte kort, der passer med meldinger og spillede kort. Løs hver med en double dummy-solver og vælg det kort, der i gennemsnit giver flest stik. |

**Double dummy-solveren** (alle kort åbne, perfekt spil) skrives i ren C#: alfa-beta-søgning
med transpositionstabel og gruppering af ækvivalente kort. Den gør tre ting:

- Driver den stærke bot.
- Laver **analyse efter spillet** ("Med perfekt spil kunne du have vundet 10 stik. Dit 4. kort
  i stik 3 kostede et stik").
- Laver **hints** under spillet.

**Færdige pakker:** Der findes NuGet-pakken `ddsWrapper`, en .NET-wrapper om Bo Haglunds
DDS (verdens mest brugte double dummy-solver, skrevet i C++). Den er hurtig og gennemtestet,
men den kalder en native `dds.dll`. Simply kører 32-bit IIS, så det skal først bevises, at der
findes en x86-udgave af DLL'en, og at den kan loades på Simply. Planen er derfor:

1. Definér et interface `IDoubleDummySolver` i motoren.
2. I fase 4 laves en lille test-deploy med `ddsWrapper` på Simply.
3. Virker den, bruges den. Ellers skrives solveren i ren C# bag samme interface.

Uanset valget kan der sættes en tidsgrænse pr. træk eller skrues ned for antallet af
simulationer, hvis ydelsen bliver et problem.

**Meldebot:** Der findes ingen NuGet-pakke til meldinger. EPBot (motoren bag programmet BBA)
er en gratis .NET-DLL, men den er bygget til .NET Framework 4.8.1, har sine egne systemer og
giver ikke danske, pædagogiske forklaringer. Derfor skriver vi vores egen regelmotor. EPBot kan
eventuelt bruges i tests som sparringspartner.

**Siddepladser:** Når jeres side vinder kontrakten, er du altid spilfører. Hvis Nord "burde"
være spilfører, roteres bordet, så du spiller begge hænder (din og blindens). Når du sidder i
modspil, spiller botten Nord.

## 5. Det pædagogiske lag

### 5.1 Læringssti (lektioner)

Hver lektion består af en kort forklaring (tekst + illustrerede hænder), 3–10 interaktive
opgaver og til sidst 2–4 rigtige spil, der er genereret til at træne netop emnet.

| # | Modul | Indhold |
|---|---|---|
| 1 | Spillets gang | Kort, farver, stik, at følge farve, makkerpar |
| 2 | Trumf og UT | Hvad trumf gør, stik med og uden trumf |
| 3 | Mini-bridge | Spil uden meldinger: højeste HP-side bliver spilfører. Godt første spil |
| 4 | Pointtælling | HP (E=4, K=3, D=2, Kn=1), fordelingspoint, balancerede hænder |
| 5 | Kontrakter og tælling | Niveau + 6, udgang (3UT, 4♥/♠, 5♣/♦), slem, zoner |
| 6 | Åbningsmeldinger | 1 i farve, 1UT, 2♣, pas |
| 7 | Svar på åbning | Støtte, ny farve, UT-svar |
| 8 | Genmelding | Åbners anden melding, at finde fit og niveau |
| 9 | 1UT-systemet | Stayman, overføringer |
| 10 | Konkurrencemeldinger | Indmelding, oplysningsdobling |
| 11 | Spilføring I | Tæl vindere (UT), tæl tabere (trumf), lav en plan |
| 12 | Spilføring II | Trumftræk, maskering, at etablere en lang farve, at stjæle på kort hånd |
| 13 | Modspil I | Udspil, signaler (høj = opmuntrende), 2. hånd lav / 3. hånd høj |
| 14 | Slem | Kontrol, Blackwood |

Moduler låses op i rækkefølge, men alle kan åbnes frit. Fremskridt gemmes pr. spiller.

### 5.2 Øvelsestyper

- **Tæl point:** en hånd vises, du indtaster HP. Hurtig quiz.
- **Hvad melder du?** Du får en hånd og en melderække og vælger din melding. Du får feedback
  fra regelmotoren.
- **Meld hele spillet:** du og bot-makker melder en hånd færdig. Resultatet sammenlignes med
  den optimale kontrakt (beregnet med double dummy).
- **Spil kontrakten:** en fast kontrakt, hvor du er spilfører. Du får at vide, om du klarede
  den, og hvad maksimum var.
- **Find udspillet:** vælg dit udspil ud fra melderækken.
- **Frit spil med træner:** almindeligt spil med hints slået til.

### 5.3 Kortgivning med betingelser

En generator laver tilfældige spil, der opfylder betingelser, så lektionerne kan træne
præcis det, de handler om:

```jsonc
{ "south": { "hcp": [15, 17], "balanced": true }, "north": { "hcp": [8, 9] } }
```

Den bruger rejection sampling og er seeded, så det samme spil kan genskabes. Håndlavede spil
kan desuden indlæses i **PBN-formatet** (standardformatet for bridgespil), så du kan lægge
spil fra bøger eller klubaftener ind.

### 5.4 Trænerfunktioner i spillet

- **Hint-knap.** Under meldingen viser den systemets melding og hvorfor. Under kortspillet
  viser den det bedste kort ifølge double dummy og en kort begrundelse.
- **Forklar melding.** Tryk på en hvilken som helst melding i melderækken for at se, hvad den
  viser.
- **Fortryd.** Tag sidste handling tilbage. Det er nemt, fordi spillet er tilstandsløst: man
  fjerner bare sidste handling fra requesten.
- **Vis alle kort.** Åbn bordet til læring, eller efter spillet.
- **Gennemgang efter spillet.** Afspil spillet stik for stik med double dummy-tal pr. kort,
  så fejl bliver markeret.
- **Træner-niveau.** "Fuld hjælp" giver advarsler, før du laver en fejl. "Kun efter spillet"
  giver ingen afbrydelser.

## 6. API

```
POST /api/game                    Spil: { seed?, level, mode, calls[], cards[] } → synlig tilstand
POST /api/game/hint               Samme body → anbefalet handling + forklaring
POST /api/game/analysis           Afsluttet spil → double dummy-gennemgang (alle kort)
POST /api/bidding/explain         { auction[] } → hvad hver melding viser
GET  /api/lessons                 Læringssti (moduler og status)
GET  /api/lessons/{id}            Lektionsindhold og øvelser
POST /api/exercises/{id}/answer   Svar → rigtigt/forkert + forklaring
POST /api/players                 Opret spiller (navn)
GET  /api/players/{name}/progress Fremskridt og statistik
POST /api/players/{name}/games    Gem afsluttet spil (serveren genberegner resultatet)
```

## 7. Frontend

Komponenterne er:

- `Table`: bordet med fire pladser, din hånd nederst og blinden synlig efter udspillet
- `Hand`: kortene sorteret pr. farve og klikbare. Ulovlige kort er nedtonede
- `BiddingBox`: meldeboks med 1–7 × ♣♦♥♠UT, Pas, X og XX. Ulovlige meldinger er deaktiveret
- `Auction`: melderækken. Tryk for at se forklaring
- `TrickArea`: det aktuelle stik, med animation når stikket tages
- `CoachPanel`: hint, forklaringer og feedback
- `LessonView`: tekst, eksempel-hænder og øvelser
- `Review`: gennemgang efter spillet

Mobil først (PWA), med store klikflader til kortene. Farvesymbolerne har to farver
(♠♣ sort, ♥♦ rød), og der kan vælges fire farver (klør grøn, ruder orange), hvilket er en
hjælp for begyndere. Al tekst er på dansk med danske bridgetermer (spar, hjerter, ruder,
klør, UT, spilfører, blind, udspil, stik).

## 8. Deploy til Simply.com

Det er en direkte kopi af Backgammon-opsætningen:

- `.github/workflows/ci.yml`: `npm run lint`, `npm test`, `npm run build` og
  `dotnet test server/Bridge.slnx` på PR'er og push til `main`.
- `.github/workflows/deploy.yml`: kører, når CI er grøn på `main`. Den gør tre ting:
  1. bygger frontenden ind i `server/Bridge.Api/wwwroot`,
  2. `dotnet publish -r win-x86 --self-contained` (Simply har ikke .NET 10 og kører 32-bit),
  3. synkroniserer med `msdeploy` med `AppOffline` og springer `App_Data` over, så
     SQLite-databasen overlever redeploys.
- `Bridge.Api.csproj` med `<AspNetCoreHostingModel>OutOfProcess</AspNetCoreHostingModel>`.

Engangsopsætning:

1. Opret et website eller subdomæne hos Simply (`bridge.kaspert.dk`) og slå Web Deploy
   til.
2. Opret GitHub Environment `production` med `SIMPLY_WEBDEPLOY_URL`, `_SITE`, `_USERNAME`
   og `_PASSWORD`.

**Ydelse på delt hosting:** Double dummy-søgning er CPU-tung. Den kan holdes nede på flere
måder:

- en hård tidsgrænse pr. botbeslutning (fx 300 ms),
- færre simulationer på lavere niveauer,
- caching af analyser pr. seed,
- at analysen efter spillet kun kører, når brugeren beder om den.

## 9. Faser

Hver fase ender i noget, der kan deployes og bruges.

| Fase | Indhold | Resultat |
|---|---|---|
| **0. Skelet** | Repo-struktur, tom React-app, `/api/health`, CI og deploy til Simply | Pipeline virker end-to-end |
| **1. Kortmotor** | Card/Hand/Deal, seeded kortgivning, stik og lovlige kort, Let-bot til kortspil | **Mini-bridge** kan spilles mod bot |
| **2. Meldinger** | Auction og Contract, tælling, regelmotor + begyndersystem, meldeforklaringer, meldeboks | Fuldt bridgespil mod bot |
| **3. Læring v1** | Lektionsformat, moduler 1–7, øvelsestyperne "tæl point" og "hvad melder du?", betinget kortgivning, spillerprofil og fremskridt | Den pædagogiske del er i brug |
| **4. Double dummy** | DD-solver, analyse efter spillet, hints under kortspillet, optimal kontrakt | "Hvad kunne jeg have gjort bedre?" |
| **5. Stærkere bot** | Mellem- og Svær-niveau (Monte Carlo + DD), hånd-modeller fra meldinger | Udfordrende modstand |
| **6. Læring v2** | Moduler 8–14, spilførings- og modspilsøvelser, PBN-import, statistik ("du melder for lavt med 11 HP") | Komplet træner |

Faserne 0–2 giver en spilbar app. Fase 3 er det, der gør den til en læringsapp, og den bør
derfor komme før den stærke bot.

## 10. Test

- **Motoren:** enhedstests for alle regler (følge farve, stikvinder, lovlige meldinger, slut
  på melding, tælling i alle kombinationer af zone/dobling/over- og understik).
- **Kortgivning:** samme seed giver samme kort (golden tests, så spilkoder aldrig ændrer sig).
- **Meldesystem:** tabeltests med "hånd + melderække → forventet melding". Testsættet bliver
  samtidig dokumentation af systemet.
- **DD-solver:** kendte spil med kendte resultater, plus en ydelsestest med tidsgrænse.
- **API:** integrationstests med `WebApplicationFactory`, som i Backgammon.
- **Frontend:** Vitest for sortering af hånd, lovlige kort og meldeboks-logik.

## 11. Risici

| Risiko | Håndtering |
|---|---|
| Meldebotten melder dumt i usædvanlige situationer | Hold systemet lille, hav en fornuftig "fallback" (naturlig melding/pas), og log de situationer, hvor ingen regel passer, så der kan tilføjes regler |
| Double dummy er for langsom på Simply | Tidsgrænse, færre simulationer, caching. Frontenden viser "botten tænker" |
| Lektionsindholdet er det største arbejde | Start med de første 7 moduler og et fast format. Indholdet ligger i filer, så det kan skrives uden at kode |
| Snyd/lækage af skjulte kort til klienten | Serveren sender kun synlige kort. Det testes eksplicit |

## 12. Beslutninger

1. **Meldesystem:** naturligt begyndersystem (5-farvers højfarve, 1UT 15–17).
2. **Niveau:** helt fra bunden. Fase 1 (mini-bridge) og modulerne 1–5 prioriteres, og
   træneren står som standard på "fuld hjælp".
3. **Domæne:** `bridge.kaspert.dk` hos Simply.
4. **Brugere:** flere brugere, der genkendes på navnet (intet login), som i Backgammon.
