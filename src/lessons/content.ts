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
    practice: { text: 'Spil mini-bridge, hvor I har en god trumffarve.', kind: 'trumpFit' },
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
    practice: { text: 'Spil mini-bridge, hvor du er spilfører.', kind: 'declare' },
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
    practice: { text: 'Spil mini-bridge, hvor du er spilfører, og vælg selv kontrakten.', kind: 'declare' },
  },
];

/** Topics that come later (shown as locked in the list). */
export const COMING_LESSONS = [
  { title: 'Åbningsmeldinger', summary: 'Hvornår og hvad man åbner med.' },
  { title: 'Svar på åbning', summary: 'Støtte, ny farve og uden trumf.' },
  { title: 'Spilføring', summary: 'Tæl vindere og tabere, træk trumf og lav en plan.' },
  { title: 'Modspil', summary: 'Udspil, signaler og samarbejde med makker.' },
];

export const lessonById = (id: string) => LESSONS.find((l) => l.id === id);
