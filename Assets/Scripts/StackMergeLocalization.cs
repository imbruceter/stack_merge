using System;
using System.Collections.Generic;
using System.Text;

namespace StackMerge
{
    public enum StackMergeLanguage
    {
        English = 0,
        Magyar = 1
    }

    public static class StackMergeLocalization
    {
        public static StackMergeLanguage CurrentLanguage { get; set; } = StackMergeLanguage.English;

        private static readonly Dictionary<string, string> Hungarian = new(StringComparer.Ordinal)
        {
            ["Play"] = "Játék",
            ["Algos"] = "Algók",
            ["Algorithms"] = "Algoritmusok",
            ["Upgrades"] = "Fejlesztések",
            ["Agents"] = "Ügynökök",
            ["Modifiers"] = "Módosítók",
            ["Research"] = "Kutatás",
            ["History"] = "Előzmények",
            ["Achievements"] = "Mérföldkövek",
            ["Settings"] = "Beállítások",
            ["Locked"] = "Zárolva",
            ["Unlocked"] = "Feloldva",
            ["Select"] = "Kiválaszt",
            ["Deselect"] = "Kiválasztás törlése",
            ["Tune"] = "Hangolás",
            ["Buy"] = "Vásárlás",
            ["Equip"] = "Felszerel",
            ["Unequip"] = "Levesz",
            ["Maxed"] = "Maxolva",
            ["Done"] = "Kész",
            ["Manual"] = "Manuális",
            ["Manual mode"] = "Manuális mód",
            ["Auto solving"] = "Auto megoldás",
            ["Auto restart needs token"] = "Az auto újraindításhoz token kell",
            ["The run hasn't started yet."] = "A run még nem indult el.",
            ["No moves left — run over"] = "Nincs több lépés - a run véget ért",
            ["Not enough <sprite name=\"chips\">"] = "Nincs elég <sprite name=\"chips\">",
            ["Buy an algorithm first"] = "Előbb vegyél egy algoritmust",
            ["Speed upgrade unavailable"] = "A sebesség fejlesztés nem elérhető",
            ["Stack upgrade unavailable"] = "A stack fejlesztés nem elérhető",
            ["Next preview upgrade unavailable"] = "A next preview fejlesztés nem elérhető",
            ["Income upgrade unavailable"] = "A bevétel fejlesztés nem elérhető",
            ["Solver tuning unlocked"] = "Solver hangolás feloldva",
            ["Auto solve updated"] = "Auto megoldás frissítve",
            ["Auto restart updated"] = "Auto újraindulás frissítve",
            ["Modifier Lab unlocked"] = "Módosító labor feloldva",
            ["Unlock Agents in Upgrades first"] = "Előbb oldd fel az Ügynököket a Fejlesztésekben",
            ["Unlock Modifier Lab in Upgrades first"] = "Előbb oldd fel a Módosító labort a Fejlesztésekben",
            ["Finish PPO Training first"] = "Előbb fejezd be a PPO tréninget",
            ["Unlock PPO to open Research"] = "A Kutatáshoz előbb oldd fel a PPO-t",
            ["Normal mode is still locked."] = "A normál mód még zárolva van.",
            ["PPO: Training mode"] = "PPO: tréning mód",
            ["PPO: Normal mode"] = "PPO: normál mód",
            ["Training Mode"] = "Tréning mód",
            ["Normal Mode"] = "Normál mód",
            ["Normal Mode\nLocked"] = "Normál mód\nZárolva",
            ["PPO Mode Overlay is not assigned."] = "A PPO Mode Overlay nincs bekötve.",
            ["Training: keeps learning, earns no chips.\nNormal: plays for chips like other solvers."] = "Tréning: tovább tanul, de nem szerez chipet.\nNormál: chipekért játszik, mint a többi solver.",
            ["Show FPS"] = "FPS mutatása",
            ["Hide FPS"] = "FPS elrejtése",
            ["Achievement pop-up"] = "Achievement értesítés",
            ["Achievement popup"] = "Achievement értesítés",
            ["Disable achievement pop-up"] = "Achievement értesítés tiltása",
            ["Language"] = "Nyelv",
            ["Manual controls"] = "Manuális irányítás",
            ["Tap a stack to place the current next block."] = "Koppints egy stackre az aktuális next blokk lerakásához.",
            ["Equip Miner's Pickaxe, then tap a stack block to remove it."] = "Szereld fel a Miner's Pickaxe-ot, majd koppints egy stack blokkra a törléshez.",
            ["Equip Queue Scrubber, then tap the first next block to remove it."] = "Szereld fel a Queue Scrubbert, majd koppints az első next blokkra a törléshez.",
            ["Tap a block to remove it with Miner's Pickaxe"] = "Koppints egy blokkra, hogy a Miner's Pickaxe törölje",
            ["Tap the first next block to scrub it"] = "Koppints az első next blokkra a törléshez",
            ["Miner's Pickaxe equipped"] = "Miner's Pickaxe felszerelve",
            ["Miner's Pickaxe unequipped"] = "Miner's Pickaxe levéve",
            ["Queue Scrubber equipped"] = "Queue Scrubber felszerelve",
            ["Queue Scrubber unequipped"] = "Queue Scrubber levéve",
            ["Miner's Pickaxe is locked"] = "A Miner's Pickaxe zárolva van",
            ["Queue Scrubber is locked"] = "A Queue Scrubber zárolva van",
            ["Miner's Pickaxe has no uses left"] = "Nincs több Miner's Pickaxe használat",
            ["Queue Scrubber has no uses left"] = "Nincs több Queue Scrubber használat",
            ["No block available for Miner's Pickaxe"] = "Nincs Miner's Pickaxe-szal törölhető blokk",
            ["No next block available for Queue Scrubber"] = "Nincs Queue Scrubberrel törölhető next blokk",
            ["Modifier unavailable"] = "A módosító nem elérhető",
            ["Unlocks rule-changing modules that apply to new runs."] = "Feloldja az új runokra érvényes szabálymódosító modulokat.",
            ["The solver places blocks faster with each level."] = "Növeli a solver sebességét automatikus lépéseknél.",
            ["Solver is automatically playing the game."] = "A solver automatikusan játszik.",
            ["When the run ends, a new one starts automatically."] = "Amikor a run véget ér, automatikusan új indul.",
            ["The currency for Auto restart."] = "Valuta az Auto újraindulás működéséhez.",
            ["Allows you to modify the solver parameters."] = "Lehetővé teszi a solverek finomhangolását.",
            ["Another slot so you can use 3 Agents at once."] = "Felold egy harmadik Ügynök slotot.",
            ["They give you extra bonuses when you unlock them."] = "Feloldásukkal bónuszokra lehet szert tenni.",
            ["Increases the capacity of each stack by 1 per level."] = "Szintenként 1-gyel növeli minden stack kapacitását.",
            ["Shows 1 more upcoming block per level."] = "Szintenként 1-gyel több következő blokkot mutat.",
            ["Increases the chance of spawning higher blocks."] = "Növeli a nagyobb blokkok megjelenésének esélyét.",
            ["Slightly increases how often higher blocks appear."] = "Óvatosan növeli a magasabb blokkok gyakoriságát.",
            ["Boosts the <sprite name=\"chips\"> bonus at the end of the runs."] = "Növeli a run befejezésekor járó <sprite name=\"chips\"> bónuszt.",
            ["Boosts the <sprite name=\"chips\"> earned during merges and runs."] = "Növeli a merge-ekből és runokból szerzett <sprite name=\"chips\">-eket.",
            ["Stage locked"] = "Szakasz zárolva",
            ["Needs algorithm"] = "Algoritmus szükséges",
            ["Needs Agents"] = "Ügynökök szükségesek",
            ["Stage 1 - Core automation"] = "1. szakasz - Alap automatizálás",
            ["Stage 2 - Agent acceleration"] = "2. szakasz - Ügynök gyorsítás",
            ["Stage 3 - Modifier Lab"] = "3. szakasz - Módosító labor",
            ["Stage 4 - Machine Learning"] = "4. szakasz - Gépi tanulás",
            ["Endgame - PPO training"] = "Endgame - PPO tréning",
            ["Train PPO, then prestige from the Research menu."] = "Tanítsd be a PPO-t, majd prestige-elj a Kutatás menüben.",
            ["PPO is ready to unlock in Algorithms."] = "A PPO készen áll a feloldásra az Algoritmusok menüben.",
            ["Max every Modifier to open the Machine Learning layer."] = "Maxold ki az összes Módosítót a Gépi tanulás réteg megnyitásához.",

            ["Stack capacity"] = "Stack kapacitás",
            ["Difficulty"] = "Nehézség",
            ["Speed"] = "Sebesség",
            ["Auto solving"] = "Auto megoldás",
            ["Auto restart"] = "Auto újraindítás",
            ["Mode"] = "Mód",
            ["Run score"] = "Run pontszám",
            ["Moves"] = "Lépések",
            ["Merges"] = "Merge-ek",
            ["Current next"] = "Aktuális next",
            ["Available actions"] = "Elérhető akciók",
            ["Solver"] = "Solver",
            ["Run modifiers"] = "Run módosítók",
            ["Special blocks"] = "Speciális blokkok",
            ["Solver tuning"] = "Solver hangolás",
            ["+50 tokens"] = "+50 token",
            ["Locked in Upgrades."] = "A Fejlesztésekben oldható fel.",
            ["No tuning available for this solver."] = "Ehhez a solverhez nincs hangolás.",
            ["Default"] = "Alap",
            ["RAND has no tuning. Its identity is pure randomness."] = "A RAND nem hangolható. A lényege a tiszta véletlenszerűség.",
            ["MERG stays greedy, but these sliders decide how hard it chases immediate value."] = "A MERG továbbra is mohó marad, de ezek a csúszkák döntik el, mennyire hajszolja az azonnali értéket.",
            ["BAL remains a board stabilizer. Tune how defensive or merge-friendly it should be."] = "A BAL továbbra is táblastabilizáló. Állítható, mennyire legyen védekező vagy merge-barát.",
            ["HEUR is a weighted score formula. These are the clearest direct knobs."] = "A HEUR egy súlyozott pontszámképlet. Ezek a legközvetlenebb beállításai.",
            ["LOOK uses HEUR plus a follow-up estimate. Tune its greed and its second-step trust."] = "A LOOK a HEUR-t használja egy következő lépés becsléssel. Állítható a mohósága és a második lépésbe vetett bizalma.",
            ["MOCA samples futures. Its tuning can spend a little more thinking on rollout depth or sample count."] = "A MOCA jövőbeli állapotokat mintáz. Több gondolkodás adható a rollout mélységnek vagy a mintaszámnak.",
            ["PLAN-3 searches the visible queue. Tune how much it trusts short plans over current safety."] = "A PLAN-3 a látható queue-t keresi. Állítható, hogy mennyire bízzon a rövid tervekben a biztonsággal szemben.",
            ["PLAN-5 searches deeper queue lines. Tuning lets you decide whether it should be patient or practical."] = "A PLAN-5 mélyebb queue-vonalakat keres. Hangolható, hogy türelmesebb vagy praktikusabb legyen.",
            ["MOCA+ uses smarter rollouts. Tuning affects both how much it samples and how it values rollout boards."] = "A MOCA+ okosabb rolloutokat használ. A hangolás a mintaszámra és a rollout táblák értékelésére is hat.",
            ["MCTS builds a tree. These sliders tune search behavior without replacing the tree search identity."] = "Az MCTS keresési fát épít. A keresési viselkedés állítható anélkül, hogy lecserélnék a fa alapú működését.",
            ["STALL is defensive. Tune how much it sacrifices score to keep the board alive."] = "A STALL védekező solver. Állítható, mennyi pontot áldozzon fel a tábla életben tartásáért.",
            ["COMBO looks for chain setups. Tune how patient it should be while preparing cascades."] = "A COMBO láncreakciókat keres. Itt állíthatod, mennyire legyen türelmes a kaszkádok felépítése közben.",
            ["PPO trains its own actor-critic network. These nudge the learning hyperparameters within safe bounds."] = "A PPO a saját actor-critic hálóját tanítja. Biztonságos határok között finomíthatóak a tanulási hiperparaméterek.",
            ["Merge reward"] = "Merge jutalom",
            ["Score delta"] = "Pontszám változás",
            ["High block"] = "Magas blokk",
            ["Height penalty"] = "Magasság büntetés",
            ["Empty cells"] = "Üres cellák",
            ["Smoothness"] = "Simaság",
            ["Danger penalty"] = "Veszély büntetés",
            ["Queue fit"] = "Queue illeszkedés",
            ["Follow-up trust"] = "Következő lépés bizalom",
            ["Simulation rounds"] = "Szimulációs körök",
            ["Rollout moves"] = "Rollout lépések",
            ["Planning depth"] = "Tervezési mélység",
            ["Future weight"] = "Jövő súlya",
            ["Rollout planning"] = "Rollout tervezés",
            ["Board eval"] = "Táblaértékelés",
            ["Anti-stall"] = "Anti-stall",
            ["Tree visits"] = "Fa látogatások",
            ["Exploration"] = "Felfedezés",
            ["Prior bias"] = "Prior súly",
            ["Safety cushion"] = "Biztonsági tartalék",
            ["Combo setup"] = "Kombó előkészítés",
            ["Legal moves"] = "Legális lépések",
            ["Height spread"] = "Magasság eltérés",
            ["Future depth"] = "Jövő mélység",
            ["Gamma (discount)"] = "Gamma (diszkont)",
            ["Lambda (GAE)"] = "Lambda (GAE)",
            ["Clip epsilon"] = "Clip epsilon",
            ["Raises or lowers the reward for immediate merges and cascade size."] = "Növeli vagy csökkenti az azonnali merge-ek és a kaszkádméret jutalmát.",
            ["Changes how much raw score gained by the move matters."] = "Azt állítja, mennyit számítson a lépéssel szerzett nyers pontszám.",
            ["Pushes the solver toward producing larger top blocks."] = "A nagyobb felső blokkok létrehozása felé tereli a solvert.",
            ["Positive values avoid tall uneven stacks more strongly."] = "Pozitív értéknél erősebben kerüli a magas, egyenetlen stackeket.",
            ["Rewards boards with more open cells after the move."] = "Jutalmazza azokat a táblákat, ahol a lépés után több üres cella marad.",
            ["Rewards similar stack heights and penalizes lopsided boards."] = "Jutalmazza a hasonló stackmagasságokat és bünteti az egyenetlen táblákat.",
            ["Penalizes stacks near capacity more strongly."] = "Erősebben bünteti a kapacitás közelében lévő stackeket.",
            ["Lets BAL accept more merge value before spreading out."] = "Engedi, hogy a BAL több merge-értéket fogadjon el, mielőtt szétterülne.",
            ["Changes how strongly immediate score gain affects the move score."] = "Azt állítja, milyen erősen hasson az azonnali pontnyereség a lépés pontszámára.",
            ["Rewards free stack cells after the move."] = "Jutalmazza a lépés után szabadon maradó stack cellákat.",
            ["Rewards smoother stack heights and fewer awkward bottlenecks."] = "Jutalmazza a simább stackmagasságokat és a kevesebb kényelmetlen szűk keresztmetszetet.",
            ["Rewards top blocks that match upcoming queue values."] = "Jutalmazza azokat a felső blokkokat, amelyek illeszkednek a következő queue értékekhez.",
            ["Raises or lowers the value of immediate merge chains."] = "Növeli vagy csökkenti az azonnali merge-láncok értékét.",
            ["Makes near-full stacks scarier to the heuristic."] = "Veszélyesebbnek láttatja a majdnem teli stackeket a heurisztika számára.",
            ["Changes how much the first move's score gain matters."] = "Azt állítja, mennyit számítson az első lépés pontnyeresége.",
            ["Rewards keeping more room open after the first move."] = "Jutalmazza, ha az első lépés után több hely marad nyitva.",
            ["Changes how much the simulated next move influences the choice."] = "Azt állítja, mennyire befolyásolja a választást a szimulált következő lépés.",
            ["Rewards stack tops that line up with visible upcoming blocks."] = "Jutalmazza a látható következő blokkokhoz illeszkedő stacktetőket.",
            ["Punishes positions that are close to stalling."] = "Bünteti a beragadáshoz közeli pozíciókat.",
            ["Adjusts how many futures are sampled for each legal move."] = "Azt állítja, hány jövőbeli állapotot mintázzon minden legális lépéshez.",
            ["Adjusts how many moves each future is played forward."] = "Azt állítja, hány lépést játsszon tovább minden jövőbeli állapotban.",
            ["Changes the immediate score bias inside rollout evaluation."] = "Az azonnali pontszám irányába tolja vagy gyengíti a rollout értékelést.",
            ["Rewards futures that leave more cells open."] = "Jutalmazza azokat a jövőket, amelyek több üres cellát hagynak.",
            ["Makes dangerous simulated boards less attractive."] = "Kevésbé vonzóvá teszi a veszélyes szimulált táblákat.",
            ["Rewards rollouts that keep useful top blocks for the visible queue."] = "Jutalmazza azokat a rolloutokat, amelyek hasznos felső blokkokat tartanak meg a látható queue-hoz.",
            ["Shifts how many visible queued blocks the search tries to use."] = "Azt állítja, hány látható queue blokkot próbáljon felhasználni a keresés.",
            ["Changes how strongly future planned moves affect the first move."] = "Azt állítja, mennyire hassanak a jövőbeli tervezett lépések az első lépésre.",
            ["Rewards stack tops that match upcoming values."] = "Jutalmazza a következő értékekhez illeszkedő stacktetőket.",
            ["Rewards planned lines that keep space available."] = "Jutalmazza azokat a tervezett vonalakat, amelyek helyet hagynak szabadon.",
            ["Penalizes plans that leave near-full stacks."] = "Bünteti azokat a terveket, amelyek majdnem teli stackeket hagynak.",
            ["Shifts how many queued blocks the search tries to use."] = "Azt állítja, hány queue blokkot próbáljon felhasználni a keresés.",
            ["Changes how strongly deeper planned scores affect the first move."] = "Azt állítja, mennyire hassanak a mélyebb tervek pontszámai az első lépésre.",
            ["Rewards preserving useful stack tops for upcoming blocks."] = "Jutalmazza a következő blokkokhoz hasznos stacktetők megőrzését.",
            ["Rewards plans that leave more board room."] = "Jutalmazza azokat a terveket, amelyek több helyet hagynak a táblán.",
            ["Penalizes risky deep plans more strongly."] = "Erősebben bünteti a kockázatos mély terveket.",
            ["Adjusts how many smart futures are sampled for each move."] = "Azt állítja, hány okos jövőt mintázzon minden lépéshez.",
            ["Adjusts how far smart futures are played forward."] = "Azt állítja, milyen messzire játssza tovább az okos jövőket.",
            ["Adjusts how much queue planning each rollout uses."] = "Azt állítja, mennyi queue-tervezést használjon minden rollout.",
            ["Changes how much the final simulated board shape matters."] = "Azt állítja, mennyit számítson a végső szimulált tábla formája.",
            ["Rewards futures that preserve legal moves and escape routes."] = "Jutalmazza a legális lépéseket és menekülő útvonalakat megőrző jövőket.",
            ["Adjusts how many tree iterations are spent per decision."] = "Azt állítja, hány fa-iteráció jusson egy döntésre.",
            ["Higher values try less-proven branches more often."] = "Magasabb értéknél gyakrabban próbál kevésbé bizonyított ágakat.",
            ["Changes how strongly heuristic prior scores guide the tree."] = "Azt állítja, mennyire erősen vezessék a fát a heurisztikus prior pontszámok.",
            ["Adjusts how far rollouts play from a tree node."] = "Azt állítja, milyen messzire fussanak a rolloutok egy fa node-ból.",
            ["Rewards tree lines that leave room and legal moves."] = "Jutalmazza azokat a fa-vonalakat, amelyek helyet és legális lépéseket hagynak.",
            ["Rewards lines that create potential chain merges."] = "Jutalmazza a lehetséges láncmerge-eket létrehozó vonalakat.",
            ["Rewards positions with multiple available stacks."] = "Jutalmazza a több elérhető stackkel rendelkező pozíciókat.",
            ["Rewards spare capacity after each move."] = "Jutalmazza a minden lépés után megmaradó szabad kapacitást.",
            ["Punishes near-full stacks more strongly."] = "Erősebben bünteti a majdnem teli stackeket.",
            ["Penalizes uneven stack heights."] = "Bünteti az egyenetlen stackmagasságokat.",
            ["Lets STALL take more immediate merge value."] = "Engedi, hogy a STALL több azonnali merge-értéket vegyen fel.",
            ["Rewards adjacent equal values and future cascade potential."] = "Jutalmazza a szomszédos azonos értékeket és a jövőbeli kaszkádpotenciált.",
            ["Changes how much immediate merging competes with setup."] = "Azt állítja, mennyire versenyezzen az azonnali merge az előkészítéssel.",
            ["Adjusts how many setup moves the combo estimate looks through."] = "Azt állítja, hány előkészítő lépést nézzen át a kombóbecslés.",
            ["Keeps some space open while building combos."] = "Kombók építése közben nyitva tart valamennyi helyet.",
            ["How far ahead future reward is valued. Higher plans longer-term, lower is greedier."] = "Azt állítja, milyen messzi jövőbeli jutalom számítson. Magasabb értéknél hosszabb távra tervez, alacsonyabbnál mohóbb.",
            ["Advantage estimation bias/variance trade-off. Higher = lower bias, more variance."] = "Az advantage becslés torzítás/szórás kompromisszuma. Magasabb érték = kisebb torzítás, nagyobb szórás.",
            ["How big a policy update each step may make. Smaller is more conservative and stable."] = "Azt állítja, mekkora policy-frissítés történhet egy lépésben. Kisebb értéknél konzervatívabb és stabilabb.",

            ["Completed"] = "Teljesítve",
            ["No completed runs yet. Let a run end to start collecting solver stats."] = "Még nincs befejezett run. Egy run vége után kezdődnek a solver statisztikák.",
            ["Tip: use the editor benchmark window for large balance samples without touching player progression."] = "Tipp: nagy balance mintákhoz használd az editor benchmark ablakot, a játékos mentésének módosítása nélkül.",

            ["Earn 10000 <sprite name=\"chips\"> in total"] = "Szerezz összesen 10000 <sprite name=\"chips\">-et",
            ["Earn 1 M <sprite name=\"chips\"> in total"] = "Szerezz összesen 1 M <sprite name=\"chips\">-et",
            ["Earn 1 B <sprite name=\"chips\"> in total"] = "Szerezz összesen 1 B <sprite name=\"chips\">-et",
            ["Spend 10000 <sprite name=\"chips\"> in total"] = "Költs el összesen 10000 <sprite name=\"chips\">-et",
            ["Spend 100 K <sprite name=\"chips\"> in total"] = "Költs el összesen 100 K <sprite name=\"chips\">-et",
            ["Spend 100 M <sprite name=\"chips\"> in total"] = "Költs el összesen 100 M <sprite name=\"chips\">-et",
            ["Complete 10 runs while Auto Solver is turned off"] = "Fejezz be 10 runt kikapcsolt Auto Solverrel",
            ["Complete 1000 runs with a solver"] = "Fejezz be 1000 runt egy solverrel",
            ["Move a total of 10000 times"] = "Lépj összesen 10000 alkalommal",
            ["Move a total of 100 K times"] = "Lépj összesen 100 K alkalommal",
            ["Move a total of 1 M times"] = "Lépj összesen 1 M alkalommal",
            ["Merge a total of 10000 times"] = "Merge-elj összesen 10000 alkalommal",
            ["Merge a total of 100 K times"] = "Merge-elj összesen 100 K alkalommal",
            ["Merge a total of 1 M times"] = "Merge-elj összesen 1 M alkalommal",
            ["Reach high 1024"] = "Érd el a 1024-es blokkot",
            ["Reach high 8192"] = "Érd el a 8192-es blokkot",
            ["Reach high 32768"] = "Érd el a 32768-as blokkot",
            ["Unlock Agents"] = "Oldd fel az Ügynököket",
            ["Unlock Modifiers"] = "Oldd fel a Módosítókat",
            ["Use all solvers at least once"] = "Használd az összes solvert legalább egyszer",
            ["Use all Agents at least once"] = "Használd az összes ügynököt legalább egyszer",
            ["Let Unstable Stack save your run a total of 100 times"] = "Az Unstable Stack mentsen meg összesen 100 runt",
            ["Merge with a Joker for a total of 100 times"] = "Merge-elj Jokerrel összesen 100 alkalommal",
            ["Prestige reset for the first time"] = "Prestige resetelj először",
            ["Prestige reset for a total of 5 times"] = "Prestige resetelj összesen 5 alkalommal",
            ["Buy all the researches"] = "Vedd meg az összes kutatást",

            ["Merge Broker"] = "Merge Bróker",
            ["Highwater Analyst"] = "Dagály Elemző",
            ["Score Auditor"] = "Pontszám Auditor",
            ["Overclocker"] = "Túlpörgő",
            ["Quartermaster"] = "Quartermaster",
            ["Restart Sponsor"] = "Újraindítás Szponzor",
            ["Token Prospector"] = "Token Kutató",
            ["Move Dividend"] = "Lépés Osztalék",
            ["Velocity Trader"] = "Sebesség Kereskedő",
            ["Unstable Stack"] = "Instabil Stack",
            ["Catalyst Stack"] = "Katalizátor Stack",
            ["Mirror Stack"] = "Tükör Stack",
            ["Joker"] = "Joker",
            ["Miner's Pickaxe"] = "Bányász Csákány",
            ["Queue Scrubber"] = "Queue Tisztító",
            ["Neural Accelerator"] = "Neurális Gyorsító",

            ["Boosts merge income."] = "Növeli a merge bevételt.",
            ["Rewards new highs."] = "Jutalmazza az új rekord blokkokat.",
            ["Turns score into <sprite name=\"chips\">."] = "A pontszámot <sprite name=\"chips\"> bevétellé alakítja.",
            ["Runs the solver faster."] = "Gyorsabban futtatja a solvert.",
            ["Improves baseline income."] = "Javítja az alap bevételt.",
            ["Keeps restarts funded."] = "Fenntartja az újraindítások tokenköltségét.",
            ["Turns merge volume into restart fuel."] = "A merge mennyiségét restart tokenné alakítja.",
            ["Rewards long, stable runs."] = "Jutalmazza a hosszú, stabil runokat.",
            ["Rewards fast solvers."] = "Jutalmazza a gyors solvereket.",
            ["Deletes bottom blocks when a full stack would fail."] = "Teli stack hibájánál töröl alsó blokkokat.",
            ["Converts merges into more <sprite name=\"chips\">."] = "A merge-ekből több <sprite name=\"chips\"> lesz.",
            ["Lets stack ends interact."] = "Engedi, hogy a stack két vége hasson egymásra.",
            ["Adds wild blocks to the queue."] = "Wild blokkokat ad a queue-hoz.",
            ["Lets solvers remove blocks from the board."] = "Engedi, hogy a solver blokkokat töröljön a tábláról.",
            ["Lets solvers delete bad upcoming blocks."] = "Engedi, hogy a solver rossz next blokkokat töröljön.",
            ["Speeds up expensive solvers."] = "Gyorsítja a számításigényes solvereket.",

            ["Select an algorithm to inspect it. Locked algorithms hide detailed behavior until unlocked."] = "Válassz ki egy algoritmust a részletek megtekintéséhez. A zárolt algoritmusok részletes viselkedése csak feloldás után látszik.",
            ["Buy automation, unlock new layers, then push into riskier systems when the run history proves you are ready."] = "Automatizációk és új rétegek oldhatóak fel, megalapozva a kockázatosabb rendszerek hatékonyságát.",
            ["Equipped Agents give various bonuses to progress faster."] = "A kiválasztott Ügynökök különféle bónuszokat adnak a gyorsabb haladáshoz.",
            ["Late-game rule modules. They raise the ceiling, create rescue tools, and make solver choice matter more than raw price."] = "A kiválasztott Ügynökök különféle bónuszokat adnak a gyorsabb haladáshoz.",
            ["They expand the game rules, allowing for further production. Each solver is effective in different ways."] = "Kibővítik a játékszabályokat, így további termelést is lehetővé tesz. Minden solver másban lesz hatékony.",
            ["Rule-changing modules that apply to new runs and amplify the differences between solvers."] = "Kibővítik a játékszabályokat, így további termelést is lehetővé tesz. Minden solver másban hatékony.",
            ["Modifiers apply to new runs and amplify solver differences."] = "Kibővítik a játékszabályokat, így további termelést is lehetővé tesz. Minden solver másban hatékony.",
            ["You can track how many tasks you've completed. As you progress, you'll automatically unlock them."] = "Nyomon követheted, hány feladatot teljesítettél. Ahogy haladsz, automatikusan feloldod őket.",
            ["Permanent late-game research. Prestige turns PPO Normal-mode performance into Insight, then the tree makes every future reset faster and deeper."] = "Állandó late-game kutatás. A Prestige a PPO normál mód teljesítményét Insighttá alakítja, majd a fa minden későbbi resetet gyorsabbá és mélyebbé tesz.",
            ["Permanent late-game research. The tree makes every future reset faster and deeper."] = "Állandó late-game kutatás. A fa minden későbbi resetet gyorsabbá és mélyebbé tesz.",
            ["Research locked. Finish PPO Training, then run PPO in Normal mode."] = "A kutatás zárolva van. Fejezd be a PPO tréninget, majd futtasd a PPO-t normál módban.",
            ["Track your recent runs and the performance of your used solvers."] = "Kövesd nyomon a legutóbbi runokat és a használt solverek teljesítményét.",
            ["Description"] = "Leírás",
            ["<b>Manual controls</b>"] = "<b>Manuális irányítás</b>",

            ["Free baseline solver."] = "Ingyenes alap solver.",
            ["Randomly chooses any valid stack. Weak, chaotic, but fast."] = "Véletlenszerűen választ egy érvényes stacket. Gyenge, kaotikus, de gyors.",
            ["Looks for direct merges."] = "Közvetlen merge-eket keres.",
            ["Prioritizes immediate merges and cascades before anything else."] = "Minden más előtt az azonnali merge-eket és láncokat részesíti előnyben.",
            ["Keeps stacks even."] = "Egyenletesen tartja a stackeket.",
            ["Avoids tall dangerous stacks and spreads risk across the board."] = "Kerüli a magas, veszélyes stackeket, és szétteríti a kockázatot a táblán.",
            ["Scores every legal move."] = "Minden legális lépést pontoz.",
            ["Uses handcrafted heuristics: merge value, danger, future queue fit, and free space."] = "Kézzel készített heurisztikákat használ: merge érték, veszély, jövőbeli queue illeszkedés és szabad hely.",
            ["Plans one move deeper."] = "Egy lépéssel mélyebbre tervez.",
            ["Tests each move, then estimates the best follow-up move before deciding."] = "Minden lépést tesztel, majd döntés előtt becsli a legjobb következő lépést.",
            ["Runs simulations."] = "Szimulációkat futtat.",
            ["Rolls out multiple futures and picks the best average result."] = "Több lehetséges jövőt játszik le, és a legjobb átlagos eredményt választja.",
            ["Reads the visible queue."] = "Olvassa a látható queue-t.",
            ["Queue planner. Searches lines through up to 3 visible next blocks before choosing."] = "Queue-tervező. Választás előtt legfeljebb 3 látható next blokkon át keres lépésvonalakat.",
            ["Uses the extended queue."] = "A kibővített queue-t használja.",
            ["Deep queue planner. Searches lines through up to 5 visible next blocks. Stronger once next preview upgrades are unlocked."] = "Mély queue-tervező. Legfeljebb 5 látható next blokkon át keres lépésvonalakat. A Next Preview fejlesztésekkel erősebb.",
            ["Smarter Monte Carlo rollouts."] = "Okosabb Monte Carlo rolloutok.",
            ["Enhanced Monte Carlo. Each rollout uses short queue planning and an anti-stall board score."] = "Továbbfejlesztett Monte Carlo. Minden rollout rövid queue-tervezést és anti-stall táblapontszámot használ.",
            ["Builds a search tree."] = "Keresési fát épít.",
            ["Monte Carlo Tree Search. Balances exploring new lines with exploiting lines that already score well."] = "Monte Carlo Tree Search. Egyensúlyoz az új útvonalak felfedezése és a már jól pontozó útvonalak kihasználása között.",
            ["Avoids dead boards."] = "Kerüli a halott táblákat.",
            ["Anti-stall solver. Heavily protects legal moves, semi-empty stacks, and escape routes over greedy merges."] = "Anti-stall solver. Védi a legális lépéseket, a félig üres stackeket és a menekülő útvonalakat a merge-ekkel szemben.",
            ["Sets up chain merges."] = "Láncmerge-eket készít elő.",
            ["Combo-focused solver. Reads the queue and rewards positions that can cascade over the next 2-3 turns."] = "Kombóközpontú solver. Azokat a pozíciókat jutalmazza, amelyek a következő 2-3 körben láncreakciót indíthatnak.",
            ["Endgame learner. Requires every Modifier to be fully purchased."] = "Endgame tanuló. Minden módosítót teljesen meg kell venni hozzá.",
            ["Proximal Policy Optimization is a lightweight actor-critic neural network that learns its policy from run trajectories, clipped policy updates, value estimates, and entropy-driven exploration."] = "A Proximal Policy Optimization egy könnyű actor-critic neurális háló. Run trajektóriákból, korlátozott policy frissítésekből, értékbecslésekből és entrópia-alapú felfedezésből tanul.",

            ["+75% <sprite name=\"chips\"> from merge rewards."] = "+75% <sprite name=\"chips\"> a merge jutalmakból.",
            ["+140% <sprite name=\"chips\"> from new highest-block rewards."] = "+140% <sprite name=\"chips\"> az új highest-blokk jutalmakból.",
            ["+60% <sprite name=\"chips\"> from end-of-run score bonus."] = "+60% <sprite name=\"chips\"> a run végi pontszám bónuszból.",
            ["Solver move interval is 25% shorter."] = "A solver lépésköze 25%-kal rövidebb.",
            ["+4 <sprite name=\"chips\"> on every successful placement."] = "+4 <sprite name=\"chips\"> minden sikeres lerakás után.",
            ["Auto Restart consumes no tokens while this agent is active."] = "Az Auto Restart nem fogyaszt tokent, amíg ez az ügynök aktív.",
            ["+1 token for every 8 merges while active."] = "+1 token minden 8 merge után, amíg aktív.",
            ["End-of-run <sprite name=\"chips\"> gain a bonus from total moves completed."] = "A run végi <sprite name=\"chips\"> bevétel bónuszt kap az összes megtett lépés alapján.",
            ["End-of-run <sprite name=\"chips\"> gain a throughput bonus from moves per second."] = "A run végi <sprite name=\"chips\"> bevétel áteresztési bónuszt kap a másodpercenkénti lépések alapján.",

            ["Each level gives one rescue per run. If a full stack receives a non-merge block, its bottom block is removed without reducing score."] = "Minden szint egy mentést ad runonként. Ha egy teli stack nem merge-elő blokkot kap, az alsó block törlődik pontvesztés nélkül.",
            ["Merge rewards are permanently doubled on every run after purchase."] = "Vásárlás után minden runban véglegesen megduplázza a merge jutalmakat.",
            ["Unlocks a special merge. If the top and bottom block of a stack match, they merge through the stack."] = "Felold egy speciális merge-et. Ha egy stack felső és alsó blokkja megegyezik, a stacken keresztül merge-elnek.",
            ["Unlocks occasional Joker blocks. A Joker placed onto any block merges with it."] = "Időnként Joker blokkokat old fel. A bármely blokkra lerakott Joker merge-el vele.",
            ["Each level gives one pickaxe use per run. The solver may delete any block in any stack."] = "Minden szint egy csákányhasználatot ad runonként. A solver bármely stack bármely blokkját törölheti.",
            ["Each level gives one queue skip per run. The current next block is removed and the following block moves forward."] = "Minden szint egy queue törlést ad runonként. Az aktuális next blokk törlődik, a következő pedig előrelép.",
            ["MOCA, MOCA+, and MCTS run permanently about twice as fast. Negative speed tuning on those solvers is also twice as effective."] = "A MOCA, MOCA+ és MCTS tartósan kb. kétszer gyorsabb. Ezeknél a solvereknél a negatív hangolás is kétszer hatékonyabb.",

            ["+35% Insight from every future prestige. This is the root research: every branch starts here."] = "+35% Insight minden későbbi prestige-ből. Ez a gyökérkutatás: minden ág innen indul.",
            ["Start each prestige with <sprite name=\"chips\"> already banked. It shortens the first slow minutes after a reset without skipping entire stages by itself."] = "Minden prestige elején kezdőtőkét kapsz. Lerövidíti a reset utáni lassú első perceket anélkül, hogy önmagában teljes szakaszokat átugrana.",
            ["Permanently remembers automation milestones after prestige: Auto Solve, Auto Restart tokens, then Solver Tuning."] = "Prestige után tartósan megőrzi az automatizációs mérföldköveket: Auto Solve, Auto Restart tokenek, majd Solver Tuning.",
            ["Start future prestiges with early algorithms already known: RAND, MERG, BAL, then HEUR."] = "A későbbi prestige-eket korai algoritmusokkal kezded: RAND, MERG, BAL, majd HEUR.",
            ["+18% <sprite name=\"chips\"> from every reward per level. It stacks with Chip Yield and stage multipliers."] = "+18% <sprite name=\"chips\"> minden jutalomból szintenként. Stackelődik a Chip Yielddel és a szakasz szorzókkal.",
            ["PPO still resets every prestige, but each level lowers the trained-frame requirement for Normal mode by 8%."] = "A PPO továbbra is resetelődik minden prestige-nél, de minden szint 8%-kal csökkenti a Normal módhoz szükséges betanított frame-ek számát.",
            ["Prestige keeps a pre-trained PPO snapshot. L1 remembers roughly the first 500 PPO runs; higher levels retain deeper warm starts."] = "A Prestige megtart egy előre betanított PPO snapshotot. L1 nagyjából az első 500 PPO runt őrzi meg; magasabb szintek mélyebb warm startot adnak.",
            ["Raises PPO's reward signal for creating new highest blocks. This pushes the learner toward bigger tiles instead of only safer runs."] = "Növeli a PPO jutalomjelét új highest blokkok létrehozásakor. Ez nagyobb tile-ok felé tereli a tanulót, nem csak biztonságosabb runok felé.",
            ["Improves PPO's survival shaping and danger penalties, making high-focus policies less likely to crash early."] = "Javítja a PPO túlélési shapingjét és veszélybüntetéseit, így a high-focus policyk kevésbé omlanak össze korán.",
            ["+20% prestige Insight from PPO Normal-mode performance per level. This is the late neural payoff node."] = "+20% prestige Insight a PPO Normal mód teljesítményéből szintenként. Ez a késői neurális megtérülési node.",
            ["Boosts Insight earned directly from PPO Normal-mode runs. Training mode never feeds this, and long cycles softcap so prestige stays valuable."] = "Növeli a PPO Normal mód runokból közvetlenül szerzett Insightot. A tréning mód ezt nem táplálja, a hosszú ciklusok pedig softcapet kapnak, hogy a prestige értékes maradjon.",
            ["While the game is closed, <sprite name=\"chips\"> and Passive Insight continue at a reduced rate based on your current prestige strength."] = "Amíg a játék zárva van, a <sprite name=\"chips\"> és a Passive Insight csökkentett ütemben tovább termelődnek az aktuális prestige erő alapján.",
            ["Extends how many closed-game hours can be converted into offline <sprite name=\"chips\"> and <sprite name=\"insight\">."] = "Megnöveli, hány zárt játékban töltött óra alakítható offline <sprite name=\"chips\">-ekké és <sprite name=\"insight\">-tá.",
            ["Unlock PPO to begin the prestige layer."] = "Oldd fel a PPO-t a prestige réteg megnyitásához.",
            ["Prestige for"] = "Prestige jutalom:",
            ["You can keep playing PPO in Playing Mode to increase <sprite name=\"insight\">."] = "Tovább játszhatsz PPO Playing módban, hogy növeld az <sprite name=\"insight\">-ot.",
            ["Current effect"] = "Jelenlegi hatás",
            ["Insight"] = "Insight"
        };

        public static string Translate(string value)
        {
            if (CurrentLanguage == StackMergeLanguage.English || string.IsNullOrEmpty(value))
            {
                return value;
            }

            return TranslateHungarian(value);
        }

        private static string TranslateHungarian(string value)
        {
            if (Hungarian.TryGetValue(value, out string exact))
            {
                return exact;
            }

            if (value.IndexOf('\n') >= 0)
            {
                return TranslateLines(value);
            }

            return TranslateHungarianLine(value);
        }

        private static string TranslateLines(string value)
        {
            string[] lines = value.Split('\n');
            var builder = new StringBuilder(value.Length + 16);
            for (int i = 0; i < lines.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(TranslateHungarianLine(lines[i]));
            }

            return builder.ToString();
        }

        private static string TranslateHungarianLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return line;
            }

            if (Hungarian.TryGetValue(line, out string exact))
            {
                return exact;
            }

            return ReplaceKnownPrefixes(line);
        }

        private static string ReplaceKnownPrefixes(string line)
        {
            if (line.EndsWith(" tuning", StringComparison.Ordinal))
            {
                return line[..^" tuning".Length] + " hangolás";
            }

            string translated = ReplacePrefix(line, "Stack capacity:", "Stack kapacitás:");
            translated = ReplacePrefix(translated, "Difficulty:", "Nehézség:");
            translated = ReplacePrefix(translated, "Speed:", "Sebesség:");
            translated = ReplacePrefix(translated, "Auto solving:", "Auto megoldás:");
            translated = ReplacePrefix(translated, "Auto restart:", "Auto újraindítás:");
            translated = ReplacePrefix(translated, "Mode:", "Mód:");
            translated = ReplacePrefix(translated, "Run score:", "Run pontszám:");
            translated = ReplacePrefix(translated, "Moves:", "Lépések:");
            translated = ReplacePrefix(translated, "Merges:", "Merge-ek:");
            translated = ReplacePrefix(translated, "Current next:", "Aktuális next:");
            translated = ReplacePrefix(translated, "Available actions:", "Elérhető akciók:");
            translated = ReplacePrefix(translated, "Solver:", "Solver:");
            translated = ReplacePrefix(translated, "Run modifiers:", "Run módosítók:");
            translated = ReplacePrefix(translated, "Special blocks:", "Speciális blokkok:");
            translated = ReplacePrefix(translated, "Score:", "Pontszám:");
            translated = ReplacePrefix(translated, "Highest block:", "Legmagasabb blokk:");
            translated = ReplacePrefix(translated, "Level ", "Szint ");
            translated = ReplacePrefix(translated, "Slot ", "Slot ");
            translated = ReplacePrefix(translated, "Speed L", "Sebesség L");
            translated = ReplacePrefix(translated, "Restart in ", "Újraindítás ");
            translated = ReplacePrefix(translated, "Offline gain:", "Offline bevétel:");
            translated = ReplacePrefix(translated, "Prestige complete:", "Prestige kész:");
            translated = ReplacePrefix(translated, "Prestige for ", "Prestige jutalom: ");
            translated = ReplacePrefix(translated, "Finish PPO Training first.", "Előbb fejezd be a PPO tréninget.");
            translated = ReplacePrefix(translated, "PPO Normal mode at ", "PPO Normal mód ekkor: ");
            translated = ReplacePrefix(translated, "Normal mode unlocks after ", "A Normál mód ekkor oldódik fel: ");
            translated = ReplacePrefix(translated, "Evaluating ", "Kiértékelés ");
            translated = ReplacePrefix(translated, "Warm start:", "Warm start:");
            translated = ReplacePrefix(translated, "New-high learning x", "Új high tanulás x");
            translated = ReplacePrefix(translated, "Survival shaping x", "Túlélési shaping x");
            translated = ReplacePrefix(translated, "Normal-mode prestige x", "Normal mód prestige x");
            translated = ReplacePrefix(translated, "Normal Insight x", "Normal Insight x");
            translated = ReplacePrefix(translated, "Offline efficiency ", "Offline hatékonyság ");
            translated = ReplacePrefix(translated, "Offline cap ", "Offline limit ");
            translated = ReplacePrefix(translated, "Current effect:", "Jelenlegi hatás:");
            translated = ReplacePrefix(translated, "Insight:", "Insight:");
            translated = ReplacePrefix(translated, "Requires:", "Követelmény:");
            translated = translated.Replace("Agents ", "Ügynökök ");
            translated = translated.Replace("Solvers ", "Solverek ");
            translated = translated.Replace("Runs ", "Runok ");
            translated = translated.Replace("Merges ", "Merge-ek ");
            translated = translated.Replace("Best ", "Legjobb ");
            translated = translated.Replace(" yes", " igen");
            translated = translated.Replace(" no", " nem");
            translated = translated.Replace(" (Default)", " (Alap)");
            translated = translated.Replace(" insights. You can keep playing PPO in Playing Mode to increase <sprite name=\"insight\">.", " Insight. Tovább játszhatsz PPO Playing módban, hogy növeld az <sprite name=\"insight\">-ot.");
            translated = translated.Replace(" trained frames.", " betanított frame.");
            translated = translated.Replace(" frames.", " frame.");
            translated = translated.Replace(" frames", " frame");
            translated = translated.Replace(" PPO runs retained", " PPO run megőrizve");
            translated = translated.Replace("ON", "BE");
            translated = translated.Replace("OFF", "KI");
            translated = translated.Replace("Locked", "Zárolva");
            translated = translated.Replace("Completed", "Teljesítve");
            translated = translated.Replace("earned", "bevétel");
            return translated;
        }

        private static string ReplacePrefix(string value, string english, string hungarian)
        {
            return value.StartsWith(english, StringComparison.Ordinal)
                ? hungarian + value[english.Length..]
                : value;
        }
    }
}
