import { parseHand, parseTrick } from '../deck';
import * as drill from './drills';
import type { Lesson } from './types';

// The learning path. Text is short on purpose: a few sentences, then the player tries it.
// **bold** is the only markup.

export const LESSONS: Lesson[] = [
  {
    id: 'spillets-gang',
    title: 'Spillets gang',
    summary: 'Kortene, makkere og stik. Det vigtigste i hele spillet.',
    steps: [
      {
        type: 'text',
        title: 'Fire spillere, to par',
        body: [
          'Bridge spilles af fire personer i **to par**. Makkerne sidder over for hinanden: **Nord og Syd** spiller sammen mod **Øst og Vest**.',
          'Her i appen sidder du altid **Syd**, og din makker er Nord.',
          'Målet er at vinde **stik**. Et stik, din makker vinder, tæller lige så meget, som hvis du selv vandt det.',
        ],
      },
      {
        type: 'text',
        title: 'Kortene',
        body: [
          'Der bruges et almindeligt spil kort med 52 kort i fire farver: **♠ spar, ♥ hjerter, ♦ ruder og ♣ klør**.',
          'I hver farve er **es (E)** højest. Så kommer **konge (K), dame (D), knægt (Kn)**, 10, 9 og så videre ned til 2.',
          'Alle kort bliver givet ud, så hver spiller har **13 kort**. Hånden sorteres efter farve, som her:',
        ],
        visual: { type: 'hand', cards: parseHand('KJ74.A92.Q853.T6') },
      },
      {
        type: 'question',
        question: {
          prompt: 'Hvilket kort er højest?',
          answer: { type: 'options', options: ['♥K', '♥E', '♥D', '♥10'], correct: [1] },
          explain: 'Es er det højeste kort i hver farve. Derefter kommer konge, dame og knægt.',
        },
      },
      {
        type: 'text',
        title: 'Et stik',
        body: [
          'Én spiller **spiller ud**: lægger et kort på bordet. Så lægger de andre ét kort hver, **med uret**.',
          'Det **højeste kort i den farve, der blev spillet ud**, vinder stikket. Vinderen spiller ud til næste stik.',
          'Her spillede Vest ♠5 ud, og Øst vinder med ♠E. Et spil har 13 stik.',
        ],
        visual: { type: 'trick', plays: parseTrick('W:S5 N:SK E:SA S:S2'), trump: null },
      },
      {
        type: 'text',
        title: 'Bekend kulør',
        body: [
          'Har du kort i den farve, der blev spillet ud, **skal** du lægge et af dem. Det hedder at **bekende kulør**.',
          'Kun hvis du ikke har nogen kort i farven, må du lægge et andet kort. Det kort kan ikke vinde stikket (det lærer du mere om i lektionen om trumf).',
        ],
      },
      {
        type: 'question',
        question: {
          prompt: 'Vest spillede ♦3 ud. Hvem vinder stikket?',
          visual: { type: 'trick', plays: parseTrick('W:D3 N:DK E:CA S:D5'), trump: null, noTrumpLabel: true },
          answer: { type: 'options', options: ['Nord', 'Øst', 'Syd', 'Vest'], correct: [0] },
          explain: 'Nord vinder med ♦K, det højeste kort i ruder. Østs ♣E er højere, men det er klør, og klør blev ikke spillet ud.',
        },
      },
      { type: 'drill', title: 'Bekend kulør', intro: 'Du sidder Syd. Vælg et kort, du må spille.', count: 5, generate: drill.followSuit },
      { type: 'drill', title: 'Hvem vinder stikket?', intro: 'Find vinderen af stikket. Der spilles uden trumf.', count: 6, generate: () => drill.trickWinner(false) },
    ],
  },
  {
    id: 'trumf',
    title: 'Trumf',
    summary: 'Trumffarven slår alle de andre farver. Og hvad uden trumf betyder.',
    steps: [
      {
        type: 'text',
        title: 'Trumf slår alt',
        body: [
          'I de fleste spil er én farve **trumf**. Et trumfkort slår alle kort i de andre farver, også et es.',
          'Men du må kun trumfe, når du **ikke kan bekende kulør**. Har du kort i den udspillede farve, skal du stadig lægge et af dem.',
          'Her er hjerter trumf. Vest spillede ♣K ud. Øst har ingen klør og trumfer med ♥2, som vinder stikket.',
        ],
        visual: { type: 'trick', plays: parseTrick('W:CK N:C4 E:H2 S:CA'), trump: 'hearts' },
      },
      {
        type: 'text',
        title: 'Flere trumfer',
        body: [
          'Hvis flere trumfer i samme stik, vinder den **højeste trumf**. Det kaldes at overtrumfe.',
          'Hvis ingen trumfer, vinder det højeste kort i den udspillede farve, præcis som før.',
        ],
      },
      {
        type: 'question',
        question: {
          prompt: 'Spar er trumf. Hvem vinder stikket?',
          visual: { type: 'trick', plays: parseTrick('W:HA N:S3 E:SJ S:H2'), trump: 'spades' },
          answer: { type: 'options', options: ['Nord', 'Øst', 'Syd', 'Vest'], correct: [1] },
          explain: 'Både Nord og Øst trumfer. Østs ♠Kn er højere end Nords ♠3, så Øst vinder. Vests ♥E kan ikke slå en trumf.',
        },
      },
      {
        type: 'question',
        question: {
          prompt: 'Hjerter er trumf, og der er spillet klør ud. Du har både klør og hjerter. Må du trumfe?',
          answer: { type: 'options', options: ['Ja, trumf slår alt', 'Nej, jeg skal bekende kulør'], correct: [1] },
          explain: 'Du skal altid bekende kulør, når du kan. Du må først trumfe, når du ikke har flere kort i den udspillede farve.',
        },
      },
      {
        type: 'text',
        title: 'Uden trumf',
        body: [
          'Man kan også spille **uden trumf**, som skrives **UT**. Så er ingen farve trumf, og det højeste kort i den udspillede farve vinder altid.',
          'Det er de regler, du øvede i første lektion.',
        ],
      },
      { type: 'drill', title: 'Hvem vinder stikket?', intro: 'Hold øje med trumffarven.', count: 8, generate: () => drill.trickWinner(true) },
      { type: 'drill', title: 'Vind stikket', intro: 'Du sidder Syd. Vælg et kort, der vinder stikket.', count: 5, generate: drill.winTrick },
    ],
    practice: { text: 'Spil mini-bridge, hvor I har en god trumffarve.', path: '/minibridge?ov=trumpFit', button: 'Øv i mini-bridge' },
  },
  {
    id: 'point',
    title: 'Tæl point',
    summary: 'Honnørpoint viser, hvor stærk en hånd er. Og hvad en jævn hånd er.',
    steps: [
      {
        type: 'text',
        title: 'Honnørpoint',
        body: [
          'For at vurdere en hånd tæller man **honnørpoint (HP)**: **es = 4, konge = 3, dame = 2, knægt = 1**.',
          'Der er 40 HP i alt, så en gennemsnitlig hånd har 10. Jo flere point, jo flere stik kan hånden typisk vinde.',
          'Hånden her har ♠E (4), ♠K (3), ♥D (2) og ♦Kn (1): i alt **10 HP**.',
        ],
        visual: { type: 'hand', cards: parseHand('AK73.Q85.J94.762') },
      },
      { type: 'drill', title: 'Tæl point', intro: 'Tæl honnørpointene og skriv tallet.', count: 6, generate: drill.countHcp },
      {
        type: 'text',
        title: 'Jævn og ujævn',
        body: [
          'En hånds **fordeling** er, hvor mange kort den har i hver farve, fx 5-3-3-2.',
          'En hånd er **jævn**, når den ikke har nogen **renonce** (0 kort i en farve) eller **single** (1 kort) og højst én **doubleton** (2 kort). De jævne fordelinger er **4-3-3-3, 4-4-3-2 og 5-3-3-2**.',
          'Jævne hænder passer godt til uden trumf. Lange og korte farver er gode, når man spiller med trumf.',
        ],
      },
      { type: 'drill', title: 'Jævn eller ujævn?', intro: 'Se på, hvor mange kort der er i hver farve.', count: 6, generate: drill.balanced },
    ],
  },
  {
    id: 'mini-bridge',
    title: 'Mini-bridge',
    summary: 'Hvem bliver spilfører, og hvad er blinden? Så er du klar til at spille.',
    steps: [
      {
        type: 'text',
        title: 'Bridge uden meldinger',
        body: [
          'I rigtig bridge **melder** man om, hvad der skal spilles. Det lærer du senere.',
          'I **mini-bridge** springer man meldingerne over. I stedet siger alle, hvor mange point de har.',
        ],
      },
      {
        type: 'text',
        title: 'Spilføreren',
        body: [
          'Det par, der har **flest point** (mere end 20), får lov at spille. Den af de to, der har flest point, bliver **spilfører**.',
          'Har de lige mange, bliver den spilfører, der sagde sine point først. Giveren starter, og så går det med uret.',
          'Har begge par 20 point, giver man kortene om.',
        ],
        visual: { type: 'hcpTable', hcp: { north: 14, east: 8, south: 9, west: 9 }, dealer: 'north' },
      },
      {
        type: 'question',
        question: {
          prompt: 'Nord er giver. Hvem bliver spilfører?',
          visual: { type: 'hcpTable', hcp: { north: 14, east: 8, south: 9, west: 9 }, dealer: 'north' },
          answer: { type: 'options', options: ['Nord', 'Øst', 'Syd', 'Vest'], correct: [0] },
          explain: 'Nord-Syd har 23 point, Øst-Vest kun 17. Nord har flest af de to og bliver spilfører.',
        },
      },
      { type: 'drill', title: 'Hvem bliver spilfører?', intro: 'Find parret med flest point, og så den stærkeste i parret.', count: 5, generate: drill.declarer },
      {
        type: 'text',
        title: 'Blinden',
        body: [
          'Spilførerens makker bliver **blind**: lægger alle sine kort med billedsiden op på bordet og spiller ikke selv. **Spilføreren spiller både sin egen og blindens hånd**.',
          'I mini-bridge lægges blinden ned med det samme. Så vælger spilføreren trumffarve, eller uden trumf, og om der skal spilles delkontrakt eller udgang. Det handler næste lektion om.',
          'Spilleren til **venstre for spilføreren** spiller ud til første stik.',
        ],
      },
      { type: 'drill', title: 'Hvem spiller ud?', intro: 'Spillet går med uret: Nord, Øst, Syd, Vest.', count: 4, generate: drill.openingLeader },
    ],
    practice: { text: 'Spil mini-bridge, hvor du er spilfører.', path: '/minibridge?ov=declare', button: 'Øv i mini-bridge' },
  },
  {
    id: 'kontrakter',
    title: 'Kontrakter og udgang',
    summary: 'Hvor mange stik en kontrakt kræver, hvornår man går efter udgang, og hvordan man finder trumffarven.',
    steps: [
      {
        type: 'text',
        title: 'En kontrakt',
        body: [
          'En **kontrakt** er et løfte om at vinde et antal stik med en bestemt trumffarve, fx **2♥**.',
          'De første 6 stik kaldes **bogen** og tæller ikke med i tallet. Antal stik, man skal vinde, er derfor **niveau + 6**: 2♥ kræver 8 stik, og 1UT kræver 7.',
        ],
        visual: { type: 'contract', level: 2, strain: 'hearts' },
      },
      { type: 'drill', title: 'Hvor mange stik?', intro: 'Niveau + 6.', count: 5, generate: drill.tricksNeeded },
      {
        type: 'text',
        title: 'Delkontrakt og udgang',
        body: [
          'En **udgang** giver en stor bonus: **3UT** (9 stik), **4♥ eller 4♠** (10 stik) og **5♣ eller 5♦** (11 stik). Lavere kontrakter er **delkontrakter**.',
          'Tommelfingerregel: Har I **25 point** tilsammen, kan I gå efter udgang. I klør eller ruder skal man bruge flere, omkring 29.',
          'Går en kontrakt ned, får modstanderne point. Så det er en afvejning mellem bonus og risiko.',
        ],
      },
      { type: 'drill', title: 'Delkontrakt eller udgang?', intro: 'Husk: 3UT, 4♥/4♠, 5♣/5♦.', count: 5, generate: drill.isGame },
      {
        type: 'text',
        title: 'Find trumffarven',
        body: [
          'Den bedste trumffarve er en farve, hvor I har **mindst 8 kort tilsammen**. Det kaldes en **fit**.',
          '**Højfarverne** ♥ og ♠ foretrækkes, fordi udgang kun kræver 10 stik dér. Har I ingen fit på 8 kort, er **uden trumf** ofte bedst.',
        ],
      },
      { type: 'drill', title: 'Hvad skal I spille?', intro: 'Tæl kortene i hver farve hos Nord og Syd tilsammen.', count: 5, generate: drill.trumpChoice },
    ],
    practice: { text: 'Spil mini-bridge, hvor du er spilfører, og vælg selv kontrakten.', path: '/minibridge?ov=declare', button: 'Øv i mini-bridge' },
  },
  {
    id: 'aabning',
    title: 'Åbningsmeldinger',
    summary: 'Meldingerne er en samtale med makker. Hvornår du åbner, og hvad du åbner med.',
    steps: [
      {
        type: 'text',
        title: 'Meldingerne er en samtale',
        body: [
          'Før kortene spilles, **melder** man. Du og makker fortæller hinanden om jeres hænder for at finde den rigtige kontrakt: hvilken trumf, og hvor mange stik I kan tage.',
          'Hver melding er et tal og en farve, fx **1♥**. Den betyder: "jeg vil tage 7 stik med hjerter som trumf". Men den fortæller også makker noget om din hånd.',
          'En melding skal være **højere end den forrige**: et højere tal, eller samme tal i en højere farve. Rækkefølgen er **♣, ♦, ♥, ♠, UT**. Vil du ikke melde, siger du **pas**.',
        ],
      },
      {
        type: 'question',
        question: {
          prompt: 'Nord har meldt 1♥. Hvad er den laveste melding, du må melde?',
          answer: { type: 'options', options: ['1♦', '1♥', '1♠', '2♣'], correct: [2] },
          explain: '1♠ er højere end 1♥, fordi spar kommer efter hjerter i rækkefølgen ♣, ♦, ♥, ♠, UT. 1♦ er lavere, og 1♥ er det samme.',
        },
      },
      {
        type: 'text',
        title: 'Hvornår åbner man?',
        body: [
          'Den første, der melder noget andet end pas, **åbner** meldingerne.',
          'Du åbner, når du har **12 HP eller mere**. Med færre point siger du pas og venter på at høre, hvad de andre melder.',
        ],
      },
      {
        type: 'text',
        title: 'Hvad åbner man med?',
        body: [
          'Se på hånden i denne rækkefølge:',
          '**1. 15–17 HP og jævn fordeling:** åbn **1UT**.',
          '**2. Fem kort eller flere i ♥ eller ♠:** åbn **1♥ eller 1♠**. Med fem af hver vælger du 1♠.',
          '**3. Ellers:** åbn i den længste af **♣ og ♦**. Med tre af hver åbner du 1♣, med fire af hver 1♦.',
          'Der er også et par sjældnere åbninger: **2UT** med 20–21 HP og jævn fordeling, **2♣** med 22 HP eller mere (siger intet om klør) og **3 i en farve** med en svag hånd (6–10 HP) og 7 kort i farven. Det sidste kaldes en **spærremelding**.',
          'Hånden her har 15 HP og jævn fordeling, så den åbner 1UT.',
        ],
        visual: { type: 'hand', cards: parseHand('AQ3.KJ4.Q873.K92') },
      },
      {
        type: 'question',
        question: {
          prompt: 'Du er giver. Hvad åbner du med?',
          visual: { type: 'hand', cards: parseHand('A3.KQ874.Q87.K92') },
          answer: { type: 'options', options: ['Pas', '1♦', '1♥', '1UT'], correct: [2] },
          explain: 'Hånden har 14 HP. Det er ikke nok til 1UT (15–17), men du har fem hjerter, så du åbner 1♥.',
        },
      },
      {
        type: 'question',
        question: {
          prompt: 'Du er giver. Hvad åbner du med?',
          visual: { type: 'hand', cards: parseHand('AQ84.KJ74.Q83.92') },
          answer: { type: 'options', options: ['1♣', '1♦', '1♥', '1♠'], correct: [1] },
          explain: 'Hånden har 12 HP, men ingen højfarve med fem kort. Så åbner du i den længste lavfarve: tre ruder mod to klør giver 1♦.',
        },
      },
      { type: 'bidding', title: 'Hvad åbner du?', intro: 'Vælg din åbning i meldeboksen. Der kommer nye hænder hver gang.', count: 10, quiz: 'opening' },
    ],
    practice: { text: 'Spil bridge med meldinger. Tryk på "Hvad siger systemet?", når du er i tvivl.', path: '/spil', button: 'Spil bridge' },
  },
  {
    id: 'svar',
    title: 'Svar på åbning',
    summary: 'Makker har åbnet. Støt makkers farve, vis din egen eller meld uden trumf.',
    steps: [
      {
        type: 'text',
        title: 'Makker har åbnet',
        body: [
          'Når makker åbner, ved du, at makker har mindst 12 HP. Har I tilsammen **25 HP**, er der som regel udgang.',
          'Tæl dine point. Med **0–5 HP** siger du pas. Med **6 HP eller mere** svarer du, så makker kan melde igen.',
        ],
      },
      {
        type: 'text',
        title: 'Støt makkers højfarve',
        body: [
          'Når makker åbner 1♥ eller 1♠, har makker mindst fem kort i farven. Har du **tre kort eller flere** i den, har I en fit på mindst 8 kort. Så **støtter** du:',
          '**6–9 HP:** meld 2 i farven (enkel støtte).',
          '**10–12 HP:** meld 3 i farven. Det er en invitation: makker melder udgang med lidt ekstra.',
          '**13–15 HP:** meld 4 i farven. Det er udgang.',
        ],
      },
      {
        type: 'question',
        question: {
          prompt: 'Nord åbner 1♥, og Øst passer. Hvad melder du?',
          visual: { type: 'hand', cards: parseHand('K84.Q73.J832.Q92') },
          answer: { type: 'options', options: ['Pas', '1UT', '2♥', '4♥'], correct: [2] },
          explain: 'Du har 8 HP og tre hjerter. Det er enkel støtte: 2♥.',
        },
      },
      {
        type: 'text',
        title: 'Vis din egen farve',
        body: [
          'Uden støtte viser du din egen farve med **mindst fire kort**. På 1-trinnet skal du have **6 HP**, på 2-trinnet **10 HP**.',
          'Når makker har åbnet 1♣ eller 1♦, er det vigtigt at vise en **højfarve med fire kort**. Har du fire af hver, melder du 1♥ først.',
          'En ny farve fra dig betyder, at **makker skal melde igen**. Så får I mere at vide om hinanden.',
        ],
      },
      {
        type: 'question',
        question: {
          prompt: 'Nord åbner 1♣, og Øst passer. Hvad melder du?',
          visual: { type: 'hand', cards: parseHand('K984.QJ73.83.Q92') },
          answer: { type: 'options', options: ['1♦', '1♥', '1♠', '1UT'], correct: [1] },
          explain: 'Du har 8 HP og fire kort i begge højfarver. Med fire af hver melder du 1♥ først. Har makker fire spar, kan makker selv melde 1♠.',
        },
      },
      {
        type: 'text',
        title: 'Svar uden trumf',
        body: [
          'Har du hverken støtte eller en farve, du kan vise, svarer du i uden trumf:',
          '**6–10 HP:** 1UT.',
          '**11–12 HP og jævn:** 2UT (invitation).',
          '**13–15 HP og jævn:** 3UT (udgang).',
          'Når makker åbner **1UT**, ved du, at makker har 15–17 HP og jævn fordeling. Uden en højfarve med fire kort: pas med 0–7 HP, 2UT med 8–9 og 3UT med 10–15. Med en højfarve er der særlige meldinger, som kommer i en senere lektion.',
        ],
      },
      {
        type: 'question',
        question: {
          prompt: 'Nord åbner 1♠, og Øst passer. Hvad melder du?',
          visual: { type: 'hand', cards: parseHand('Q8.K73.Q8732.J92') },
          answer: { type: 'options', options: ['Pas', '1UT', '2♦', '2♠'], correct: [1] },
          explain: 'Du har 8 HP, kun to spar og ingen farve, der kan meldes på 1-trinnet. 2♦ kræver 10 HP, så du svarer 1UT.',
        },
      },
      { type: 'bidding', title: 'Hvad svarer du?', intro: 'Nord har åbnet, og Øst har passet. Vælg dit svar i meldeboksen.', count: 10, quiz: 'response' },
    ],
    practice: { text: 'Spil bridge med meldinger og se, hvordan du og makker finder kontrakten.', path: '/spil', button: 'Spil bridge' },
  },
  {
    id: 'genmelding',
    title: 'Genmelding',
    summary: 'Åbnerens anden melding fortæller, hvor stærk hånden er, og hjælper med at finde fit.',
    steps: [
      {
        type: 'text',
        title: 'Åbneren melder igen',
        body: [
          'Når du har åbnet, og makker har svaret, er din næste melding en **genmelding**. Den skal især fortælle, **hvor stærk** din hånd er:',
          '**Minimum:** 12–15 HP. **Ekstra:** 16–18 HP. **Stærk:** 19–21 HP.',
          'Makker kan så lægge sine point til og se, om I skal i udgang.',
        ],
      },
      {
        type: 'text',
        title: 'Når makker har støttet dig',
        body: [
          'Har makker støttet din farve (fx 1♥ – 2♥), har I fundet en fit. Nu handler det kun om niveauet:',
          '**12–15 HP:** pas. **16–18 HP:** meld 3♥, en invitation, som makker accepterer med en god støtte. **19 HP eller mere:** meld udgang, 4♥.',
        ],
        visual: { type: 'auction', dealer: 'south', calls: ['1H', 'P', '2H', 'P'] },
      },
      {
        type: 'question',
        question: {
          prompt: 'Du åbnede 1♥, og makker støttede med 2♥. Hvad melder du nu?',
          visual: { type: 'auctionAndHand', dealer: 'south', calls: ['1H', 'P', '2H', 'P'], cards: parseHand('A3.AQ874.KQ7.K92') },
          answer: { type: 'options', options: ['Pas', '3♥', '4♥', '2UT'], correct: [1] },
          explain: 'Du har 18 HP, altså lidt ekstra. 3♥ inviterer: makker melder 4♥ med 8–9 HP og passer med 6–7.',
        },
      },
      {
        type: 'text',
        title: 'Når makker har meldt en ny farve',
        body: [
          'En ny farve fra makker betyder, at **du skal melde igen**. Vælg det første, der passer:',
          '**Fire kort i makkers farve:** støt den: 2 med minimum, 3 med ekstra, 4 med en stærk hånd.',
          '**Jævn hånd:** 1UT med 12–14 HP, 2UT med 18–19 HP.',
          '**En ny farve med fire kort:** på 1-trinnet (fx 1♣ – 1♥ – 1♠), eller en lavere farve på 2-trinnet (fx 1♥ – 1♠ – 2♦).',
          '**Seks kort i din egen farve:** meld den igen: 2 med minimum, 3 med ekstra.',
        ],
      },
      {
        type: 'question',
        question: {
          prompt: 'Du åbnede 1♥, og makker svarede 1♠. Hvad melder du nu?',
          visual: { type: 'auctionAndHand', dealer: 'south', calls: ['1H', 'P', '1S', 'P'], cards: parseHand('A3.AQ874.K87.932') },
          answer: { type: 'options', options: ['Pas', '1UT', '2♥', '2♠'], correct: [1] },
          explain: 'Du har 13 HP og jævn fordeling (5-3-3-2), ingen støtte til spar og kun fem hjerter. 1UT viser en jævn minimumshånd.',
        },
      },
      {
        type: 'question',
        question: {
          prompt: 'Du åbnede 1♥, og makker svarede 1♠. Hvad melder du nu?',
          visual: { type: 'auctionAndHand', dealer: 'south', calls: ['1H', 'P', '1S', 'P'], cards: parseHand('3.AQ874.K873.K92') },
          answer: { type: 'options', options: ['1UT', '2♦', '2♥', '2♠'], correct: [1] },
          explain: 'Hånden er ikke jævn (single spar), og du har fire ruder. 2♦ viser en ny farve, lavere end hjerter, så makker kan vælge mellem dine farver på 2-trinnet.',
        },
      },
      { type: 'bidding', title: 'Din genmelding', intro: 'Du åbnede, og makker svarede. Vælg din næste melding.', count: 10, quiz: 'rebid' },
    ],
    practice: { text: 'Spil bridge med meldinger og læg mærke til, hvad din genmelding fortæller makker.', path: '/spil', button: 'Spil bridge' },
  },
  {
    id: '1ut-systemet',
    title: '1UT-systemet',
    summary: 'Overføringer og Stayman: sådan finder I en fit i højfarve, når makker har åbnet 1UT.',
    steps: [
      {
        type: 'text',
        title: 'Find fit i højfarve',
        body: [
          'Når makker åbner **1UT**, ved du næsten alt: 15–17 HP og jævn fordeling, så mindst to kort i hver farve.',
          'Har du selv en **højfarve med fem kort**, eller **fire kort** i en højfarve, vil du gerne finde ud af, om I har 8 kort tilsammen. Til det er der to særlige meldinger.',
        ],
      },
      {
        type: 'text',
        title: 'Overføring',
        body: [
          'Med **fem kort eller flere i en højfarve** melder du farven **lige under**: **2♦ viser hjerter**, og **2♥ viser spar**. Makker skal så melde din farve: 2♥ eller 2♠.',
          'Det bruges med alle styrker, også med 0 HP. Bagefter bestemmer du:',
          '**0–7 HP:** pas. **8–9 HP:** 2UT med fem kort, 3 i farven med seks. **10 HP eller mere:** 3UT med fem kort (makker vælger), 4 i farven med seks.',
          'Fordelen er, at det er den stærke hånd (1UT-åbneren), der bliver spilfører, så modspillerne ikke kan se den.',
        ],
        visual: { type: 'auction', dealer: 'north', calls: ['1N', 'P', '2D', 'P', '2H'] },
      },
      {
        type: 'question',
        question: {
          prompt: 'Makker åbner 1UT, og Øst passer. Hvad melder du?',
          visual: { type: 'auctionAndHand', dealer: 'north', calls: ['1N', 'P'], cards: parseHand('K9843.Q73.83.J92') },
          answer: { type: 'options', options: ['Pas', '2♣', '2♥', '2♠'], correct: [2] },
          explain: 'Du har fem spar. 2♥ er en overføring til spar: makker melder 2♠. Med 6 HP passer du bagefter og spiller 2♠.',
        },
      },
      {
        type: 'text',
        title: 'Stayman',
        body: [
          'Med **fire kort i en højfarve** og **8 HP eller mere** melder du **2♣** (Stayman). Den spørger: har du fire kort i en højfarve?',
          'Makker svarer **2♥** med fire hjerter, **2♠** med fire spar (og ikke fire hjerter) og **2♦** uden en højfarve med fire kort.',
          'Har I fundet en fit: 3 i farven med 8–9 HP, 4 med 10 eller mere. Uden fit: 2UT med 8–9 HP, 3UT med 10 eller mere.',
        ],
      },
      {
        type: 'question',
        question: {
          prompt: 'Makker åbner 1UT, og Øst passer. Hvad melder du?',
          visual: { type: 'auctionAndHand', dealer: 'north', calls: ['1N', 'P'], cards: parseHand('K984.Q73.A83.J92') },
          answer: { type: 'options', options: ['2♣', '2♦', '2UT', '3UT'], correct: [0] },
          explain: 'Du har 10 HP og fire spar. 2♣ (Stayman) spørger efter en 4-korts højfarve. Svarer makker 2♠, melder du 4♠, ellers 3UT.',
        },
      },
      {
        type: 'question',
        question: {
          prompt: 'Du åbnede 1UT, og makker meldte 2♣ (Stayman). Hvad svarer du?',
          visual: { type: 'auctionAndHand', dealer: 'south', calls: ['1N', 'P', '2C', 'P'], cards: parseHand('AQ3.KJ84.Q87.K92') },
          answer: { type: 'options', options: ['2♦', '2♥', '2♠', '2UT'], correct: [1] },
          explain: 'Du har fire hjerter, så du svarer 2♥.',
        },
      },
      { type: 'bidding', title: 'Makker åbner 1UT', intro: 'Brug overføring, Stayman eller uden trumf.', count: 8, quiz: 'ntResponse' },
      { type: 'bidding', title: 'Du åbnede 1UT', intro: 'Makker har meldt Stayman eller en overføring. Hvad svarer du?', count: 5, quiz: 'ntOpener' },
    ],
    practice: { text: 'Spil bridge med meldinger. Når makker åbner 1UT, kan du bruge det, du lige har lært.', path: '/spil', button: 'Spil bridge' },
  },
  {
    id: 'konkurrence',
    title: 'Konkurrencemeldinger',
    summary: 'Når modparten åbner: indmelding, oplysningsdobling og hvordan du svarer makker.',
    steps: [
      {
        type: 'text',
        title: 'Modparten har åbnet',
        body: [
          'Også når modparten åbner, kan I tit vinde kontrakten eller presse dem op. Du har tre muligheder, når du står med en god hånd:',
          '**Indmelding:** en farve med **fem gode kort**. På 1-trinnet 8–16 HP, på 2-trinnet 11–16 HP.',
          '**1UT-indmelding:** 15–18 HP og jævn fordeling.',
          '**Oplysningsdobling (Dobl):** 12 HP eller mere, **højst to kort i deres farve** og mindst tre kort i hver af de andre. Den beder makker om at melde sin bedste farve.',
        ],
        visual: { type: 'auction', dealer: 'east', calls: ['1D'] },
      },
      {
        type: 'question',
        question: {
          prompt: 'Øst åbner 1♦. Hvad melder du?',
          visual: { type: 'auctionAndHand', dealer: 'east', calls: ['1D'], cards: parseHand('KQJ84.73.K83.Q92') },
          answer: { type: 'options', options: ['Pas', '1♠', '1UT', 'Dobl'], correct: [1] },
          explain: 'Du har 11 HP og en god spar med fem kort. Det er en indmelding på 1-trinnet: 1♠.',
        },
      },
      {
        type: 'question',
        question: {
          prompt: 'Øst åbner 1♦. Hvad melder du?',
          visual: { type: 'auctionAndHand', dealer: 'east', calls: ['1D'], cards: parseHand('KQ84.AJ73.3.Q982') },
          answer: { type: 'options', options: ['Pas', '1♥', '1♠', 'Dobl'], correct: [3] },
          explain: 'Du har 12 HP, kun én ruder og mindst tre kort i alle andre farver. Det er en oplysningsdobling: makker skal melde sin bedste farve.',
        },
      },
      {
        type: 'text',
        title: 'Når makker har doblet',
        body: [
          'Makkers oplysningsdobling beder dig melde. **Du skal melde, også med 0 HP**, medmindre modparten melder imellem.',
          'Meld din bedste farve af dem, modparten ikke har meldt. Højfarver først. **0–8 HP:** så billigt som muligt. **9–11 HP:** et trin højere (spring). **12 HP eller mere:** meld udgang.',
        ],
      },
      {
        type: 'text',
        title: 'Når makker har meldt ind',
        body: [
          'Har makker meldt ind med en farve, har makker fem kort. Med **tre kort i farven** har I en fit, og du støtter:',
          '**7–10 HP:** meld farven et trin højere. **11–12 HP:** spring (invitation). **13 HP eller mere:** udgang i højfarve.',
        ],
      },
      { type: 'bidding', title: 'Modparten har åbnet', intro: 'Øst har åbnet. Meld ind, dobl eller pas.', count: 8, quiz: 'overcall' },
      { type: 'bidding', title: 'Makker har meldt', intro: 'Vest åbnede, makker meldte ind eller doblede, og Øst passede.', count: 6, quiz: 'advance' },
    ],
    practice: { text: 'Spil bridge med meldinger. Når modstanderne åbner, kan du nu blande dig.', path: '/spil', button: 'Spil bridge' },
  },
  {
    id: 'spilfoering-1',
    title: 'Spilføring: tæl og planlæg',
    summary: 'Før du spiller første kort: tæl dine sikre stik eller dine tabere, og læg en plan.',
    steps: [
      {
        type: 'text',
        title: 'Læg en plan',
        body: [
          'Når udspillet er kommet, og blinden ligger på bordet, så **stop op, før du spiller fra blinden**. Gode spilførere bruger et øjeblik på at lægge en plan.',
          'Start med at regne ud, **hvor mange stik du skal have**: niveau + 6.',
          'I **uden trumf** tæller du dine **sikre stik** (vindere). I et **trumfspil** tæller du dine **tabere**. Så ved du, hvor mange stik der mangler, og hvad du skal arbejde for.',
        ],
      },
      {
        type: 'text',
        title: 'Sikre stik i uden trumf',
        body: [
          'Et **sikkert stik** er et topkort, du kan tage, uden at modstanderne kommer ind: esset, så kongen, hvis I også har den, og så videre.',
          'Tæl for hver farve de topkort, I har i træk fra esset, uanset om de sidder hos dig eller i blinden. Men aldrig flere, end den **længste** af de to hænder har kort i farven.',
          'Her har I ♠E K D (3 stik), ♥E K (2), ♦E K (2) og ♣E (1): i alt **8 sikre stik**. I 3UT skal du bruge 9, så du mangler ét.',
        ],
        visual: { type: 'twoHands', north: parseHand('AK2.Q3.5432.5432'), south: parseHand('Q43.AK.AK2.A9876') },
      },
      { type: 'drill', title: 'Tæl sikre stik', intro: 'Nord er blind, du er Syd. Der spilles uden trumf.', count: 5, generate: drill.countWinners },
      {
        type: 'text',
        title: 'Tabere i trumfspil',
        body: [
          'I et trumfspil er det lettere at tælle **tabere** på din egen hånd. Se på de **tre øverste kort** i hver farve: hvert manglende **es, konge og dame** er en taber.',
          'En single er en taber, medmindre det er esset. En doubleton har højst to tabere. En renonce har ingen.',
          'Bagefter ser du på blinden: kan den **stjæle** nogle af dine tabere (fordi den er kort i farven), eller kan du **smide** tabere på blindens topkort?',
        ],
        visual: { type: 'hand', cards: parseHand('AKQ43.K2.Q.J987') },
      },
      { type: 'drill', title: 'Tæl tabere', intro: 'Du er spilfører i et trumfspil. Tæl tabere på din egen hånd.', count: 5, generate: drill.countLosers },
      {
        type: 'text',
        title: 'Find det bedste kort',
        body: [
          'Nu skal du øve dig i at spille. I opgaverne kan du **se alle fire hænder**, som i en bridgeopgave i avisen. Find det kort, der giver flest stik.',
          'Svaret er regnet ud af computeren, så det er altid rigtigt, når alle spiller perfekt.',
        ],
      },
      { type: 'play', title: 'Find det bedste kort', intro: 'Alle kort er åbne. Du og blinden har udspillet.', count: 4, stage: 'early' },
    ],
    practice: { text: 'Spil mini-bridge som spilfører og brug "Gennemgå spillet" bagefter.', path: '/minibridge?ov=declare', button: 'Øv i mini-bridge' },
  },
  {
    id: 'spilfoering-2',
    title: 'Spilføring: teknikker',
    summary: 'Træk trumf, masker, etabler en lang farve og stjæl i den korte hånd.',
    steps: [
      {
        type: 'text',
        title: 'Træk trumf',
        body: [
          'I et trumfspil er det tit klogt at **trække trumf** først: spil trumf, indtil modstanderne ikke har flere. Så kan de ikke stjæle dine topkort.',
          'Tæl modstandernes trumf: har I 8 tilsammen, har de 5. Hold styr på, hvor mange der er faldet.',
          'Men vent med at trække trumf, hvis du først skal bruge blindens trumf til at stjæle med.',
        ],
      },
      {
        type: 'text',
        title: 'Stjæl i den korte hånd',
        body: [
          'Har blinden færre kort i en farve end dig, kan du **stjæle** dine tabere i den farve med blindens trumf. Hver gang giver det et ekstra stik.',
          'Det giver kun ekstra stik at stjæle i den hånd, der har **færrest trumf** (normalt blinden). Stjæler du på din egen lange trumfhånd, bruger du bare en trumf, du alligevel ville have vundet med.',
        ],
      },
      {
        type: 'text',
        title: 'Maskering',
        body: [
          'Har blinden **♠E D** og du **♠3 2**, mangler I kongen. Spiller du ♠E og så ♠D, vinder modstanderen med kongen.',
          'Spil i stedet **lille fra hånden mod ♠D** (det hedder at **maskere**). Har Vest kongen, kan han ikke gøre noget: lægger han den, tager du med esset, og ellers vinder damen. Så får du to stik i halvdelen af tilfældene.',
          'Huskereglen er: **spil mod det kort, du håber vinder**, ikke fra det.',
        ],
      },
      {
        type: 'question',
        question: {
          prompt: 'Blinden (Nord) har ♠E D, og du har ♠3 2. Du er inde på hånden. Hvordan får du bedst to spar-stik?',
          answer: {
            type: 'options',
            options: ['Spil ♠E og så ♠D', 'Spil lille spar mod blindens ♠D', 'Vent og håb, at nogen spiller spar til dig'],
            correct: [1],
          },
          explain: 'Spil mod damen. Sidder kongen hos Vest (før blinden), vinder ♠D. Spiller du esset først, falder damen kun, hvis kongen sidder alene.',
        },
      },
      {
        type: 'text',
        title: 'Etablér en lang farve',
        body: [
          'I uden trumf kommer de ekstra stik ofte fra en **lang farve**. Har I fx 8 ruder, men mangler esset, må du give et stik væk først. Når modstandernes ruder er spillet ud, er dine små ruder blevet vindere.',
          'Det gælder om at gøre det tidligt, mens du stadig har topkort i de andre farver at komme ind på.',
        ],
      },
      {
        type: 'question',
        question: {
          prompt: 'Du spiller 4♥ og har 4 tabere: én i spar og tre i klør. Blinden har kun én klør. Hvad er planen?',
          answer: {
            type: 'options',
            options: ['Træk alle trumf med det samme', 'Stjæl klør i blinden, før trumfen trækkes ud', 'Spil uden trumf i stedet'],
            correct: [1],
          },
          explain: 'Blinden er kort i klør. Spil klør og stjæl med blindens trumf, så forsvinder to tabere. Trækker du trumf først, har blinden ikke flere trumf at stjæle med.',
        },
      },
      { type: 'play', title: 'Find det bedste kort', intro: 'Længere inde i spillet. Alle kort er åbne.', count: 6, stage: 'middle' },
    ],
    practice: { text: 'Spil mini-bridge, hvor I har en trumffit, og brug teknikkerne.', path: '/minibridge?ov=trumpFit', button: 'Øv i mini-bridge' },
  },
];

/** Topics that come later (shown as locked in the list). */
export const COMING_LESSONS = [
  { title: 'Modspil', summary: 'Udspil, signaler og samarbejde med makker.' },
  { title: 'Slem', summary: 'Når I kan tage 12 eller 13 stik.' },
];

export const lessonById = (id: string) => LESSONS.find((l) => l.id === id);
