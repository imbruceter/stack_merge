# Stack Merge — Balance Formula Lab

Zárt képletes balansz-becslő. Nem szimulál — a solver-benchmark profilokból
(moves / merges / avg score / best high) és a `StackMergeProgression` tényleges
konstansaiból számolja egy run gazdaságát, ebből pedig minden gazdasági elem
határhasznát (ΔCPS, megtérülési idő).

## Futtatás

Nyisd meg az `index.html`-t böngészőben, vagy: `node dev_server.js` →
http://localhost:8377.

## Mit számol

- **Run-bevétel és chips/mp** — a ciklusidővel (játékidő + 1,2 mp auto-restart).
- **Bevétel-összetétel** forrásonként (merge / lerakás / new-high / combo /
  run bónusz / salvage / passzív / token-érték).
- **Következő szint értéke** minden gazdasági upgrade-re: ΔCPS, megtérülés
  (ár / ΔCPS) és ítélet. Az agentek/gazdasági modifierek ugyanígy.
- **Stat-formáló elemek** (Stack cap, Next preview, Difficulty, Scaling freq,
  Unstable/Mirror/Joker/Pickaxe/Scrubber): ezek a run-statisztikákon át hatnak —
  értéküket a két benchmark-profil A/B-összevetése mutatja, nem képlet.
- **Solver-összehasonlítás** chips/mp szerint (solverenkénti pacing-gel).

## A képletek forrása és a közelítések

A képletek a játékkóddal egyeztetve: `AwardMove`, `AwardRunCompleted`,
`AwardSalvage`, `TickPassiveProduction`, `GetMoveInterval`
(+ `GetSolverPacingMultiplier`), `GetHighestBlockRewardMultiplier`.

Ismert közelítések:

- a per-move kerekítések (ceil) elhagyva;
- a Score lerakás-értékeket is tartalmaz → az átlag merge-tile a becsült
  spawn-érték levonásával készül (állítható);
- a merge-ek high-szorzója a végső csúcs és fél-csúcs hm-jének átlaga;
- a new-high jutalom tier-szummás modell, állítható esemény/tier értékkel;
- a Neural Accelerator ×0,5 pacing-közelítés (valójában CPU-időt felez);
- a token-áram (Prospector/Sponsor) chip-egyenértéken (pack-ár/darab) számolható
  be, jelölt becslésként.

Ha a játék konstansai változnak, az `app.js` tetején lévő `C` objektumot és a
pacing/hm táblákat kell szinkronban tartani.
