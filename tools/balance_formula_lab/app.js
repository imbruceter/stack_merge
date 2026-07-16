/*
 * Stack Merge — Balance Formula Lab
 *
 * Zárt képletes balansz-becslő: a solver-benchmark profilokból (moves/merges/score/high)
 * és a StackMergeProgression tényleges konstansaiból számolja egy run gazdaságát,
 * szimuláció nélkül. A képletek a játékkóddal lettek egyeztetve (AwardMove,
 * AwardRunCompleted, AwardSalvage, TickPassiveProduction, GetMoveInterval,
 * GetHighestBlockRewardMultiplier) — a közelítéseket lásd az index.html láblécében.
 */

// ---------------------------------------------------------------------------
// Játék-konstansok (StackMergeProgression tükör — módosításkor innen frissítsd)
// ---------------------------------------------------------------------------
const C = {
  incomeScale: 0.25,
  autoRestartSeconds: 1.2, // fix; a Restart Sponsor tokent spórol, NEM időt
  speedCosts: [6000, 12000, 25000, 55000, 110000, 250000, 550000, 1250000, 3000000, 7000000],
  moveIntervals: [0.18, 0.146, 0.118, 0.096, 0.078, 0.063, 0.051, 0.041, 0.034, 0.027, 0.022],
  computeCosts: [150000, 300000, 900000, 2500000, 6500000],
  computeReduction: 0.11,
  stackCosts: [12000, 70000, 400000, 2200000, 11000000],
  queueCosts: [40000, 400000],
  chipYieldCosts: [4000, 10000, 25000, 62000, 155000, 390000, 970000, 2400000, 6000000, 15000000],
  chipYieldBonusByLevel: [0.45, 0.35, 0.30, 0.25, 0.20, 0.18, 0.15, 0.12, 0.10, 0.10],
  difficultyCosts: [60000, 180000, 600000, 1800000, 6000000],
  scalingFrequencyCosts: [90000, 150000, 245000, 400000, 660000, 1100000, 1800000, 2950000, 4850000, 8000000],
  profitableEndingCosts: [60000, 170000, 480000, 1350000, 3800000],
  profitableEndingPerLevel: 0.15,
  passiveYieldCosts: [5000, 10500, 22000, 46000, 96000, 190000, 370000, 700000, 1300000, 2400000],
  passiveYieldPerTick: [0, 100, 200, 350, 500, 800, 1200, 1500, 2000, 3000, 5000],
  passiveTickCosts: [7000, 14000, 29000, 60000, 120000, 235000, 450000, 850000, 1550000, 2800000],
  passiveTickIntervals: [10, 9, 8, 7, 6, 5, 4, 3, 2, 1.5, 1],
  activeMultiplierCosts: [4000, 10000, 20000, 45000, 88000, 180000, 400000, 750000, 1500000, 4000000],
  activeMultiplierBonusByLevel: [0.40, 0.35, 0.30, 0.25, 0.20, 0.15, 0.10, 0.10, 0.10, 0.05],
  comboCosts: [15000, 30000, 60000, 125000, 250000, 500000, 1000000, 2000000, 4000000, 8000000],
  comboBonusPerStreakByLevel: [0.05, 0.04, 0.03, 0.025, 0.02, 0.015, 0.01, 0.0075, 0.005, 0.0025],
  comboStreakCap: 20,
  salvageCosts: [25000, 50000, 100000, 200000, 400000, 800000, 1600000, 3200000, 6400000, 12800000],
  salvageShareByLevel: [0.25, 0.20, 0.15, 0.10, 0.05, 0.05, 0.05, 0.05, 0.05, 0.05], // run-score bazison
  tokenDividendCosts: [50000, 200000, 800000, 3200000, 12800000],
  tokenDividendPerSqrtTokenPerLevel: 0.01, // +1%/szint × √token, cap nélkül
  tokenProspectorMergeTarget: 8,
  tokenPackCost: 10000, // alapár; a tényleges ár ×(1 + tartott token/100)
  tokenPackSize: 20,
};

// GetSolverPacingMultiplier tükör — a játékkóddal ellenőrizve.
const solverPacing = {
  RAND: 0.25, MERG: 0.34, BAL: 0.44, HEUR: 0.5, LOOK: 0.6, COMBO: 0.62,
  STALL: 0.64, "PLAN-3": 0.78, "PLAN-5": 0.95, MOCA: 0.92, "MOCA+": 1.05, MCTS: 0.55, PPO: 0.2,
};
const heavySolvers = new Set(["PLAN-3", "PLAN-5", "MOCA", "MOCA+", "MCTS"]);

const agents = [
  ["restartSponsor", "Restart Sponsor", 100000, "eco", "Ingyen restart (token-megtakarítás)"],
  ["highwaterAnalyst", "Highwater Analyst", 250000, "eco", "+200% new-high jutalom"],
  ["quartermaster", "Quartermaster", 500000, "eco", "+10 chip minden lerakásra"],
  ["scoreAuditor", "Score Auditor", 800000, "eco", "+200% run-végi score bónusz"],
  ["overclocker", "Overclocker", 1500000, "eco", "-25% move-intervallum"],
  ["velocityTrader", "Velocity Trader", 4000000, "eco", "Run-végi bónusz a tempóból"],
  ["moveDividend", "Move Dividend", 6000000, "eco", "Run-végi bónusz a lépésszámból"],
  ["mergeBroker", "Merge Broker", 12000000, "eco", "+75% merge-bevétel"],
  ["tokenProspector", "Token Prospector", 20000000, "eco", "+1 token / 8 merge"],
];

const modifiers = [
  ["unstableStack", "Unstable Stack", [3000000, 5000000, 10000000, 20000000, 40000000], "stat", "Mentés/run — hosszabb runok"],
  ["catalystStack", "Catalyst Stack", [10000000], "eco", "Merge-bevétel ×2"],
  ["mirrorStack", "Mirror Stack", [25000000], "stat", "Extra merge-lehetőség"],
  ["joker", "Joker", [80000000], "stat", "Wild blokkok a sorban"],
  ["minersPickaxe", "Miner's Pickaxe", [5000000, 8000000, 15000000, 30000000, 50000000], "stat", "Blokk-törlés / run"],
  ["queueScrubber", "Queue Scrubber", [5000000, 8000000, 15000000, 30000000, 50000000], "stat", "Sor-átugrás / run"],
  ["neuralAccelerator", "Neural Accelerator", [2500000], "eco", "MOCA/MOCA+ ~2× gyorsabb (közelítés)"],
];

// Gazdasági (képlettel számolható) és stat-formáló (benchmark-ot igénylő) szintek.
const levelDefs = [
  ["speed", "Solver Speed", C.speedCosts, "eco", "Move-intervallum csökken"],
  ["compute", "Compute Speed", C.computeCosts, "eco", "Nehéz solverek pacing-je csökken"],
  ["chipYield", "Chip Yield", C.chipYieldCosts, "eco", "Front-loaded global chip multiplier"],
  ["profitableEnding", "Profitable Ending", C.profitableEndingCosts, "eco", "+15%/szint a run-végi bónuszra"],
  ["passiveYield", "Passive Yield", C.passiveYieldCosts, "eco", "Chips/tick"],
  ["passiveTick", "Passive Tick Rate", C.passiveTickCosts, "eco", "Sűrűbb tickek"],
  ["activeMultiplier", "Active Multiplier", C.activeMultiplierCosts, "eco", "Front-loaded passzív szorzó aktív játék közben"],
  ["combo", "Combo Engine", C.comboCosts, "eco", "Front-loaded %/streak-fok"],
  ["salvage", "Salvage Protocol", C.salvageCosts, "eco", "Front-loaded run-score share game overkor"],
  ["tokenDividend", "Token Dividend", C.tokenDividendCosts, "eco", "+1%×√token/szint bevétel, cap nélkül"],
  ["stack", "Stack Capacity", C.stackCosts, "stat", "Hosszabb runok — benchmarkból mérhető"],
  ["queue", "Next Preview", C.queueCosts, "stat", "Jobb solver-döntések — benchmarkból mérhető"],
  ["difficulty", "Difficulty Scaling", C.difficultyCosts, "stat", "Magasabb spawn-tier — benchmarkból mérhető"],
  ["scalingFrequency", "Scaling Frequency", C.scalingFrequencyCosts, "stat", "Gyakoribb magas spawn — benchmarkból mérhető"],
];

// ---------------------------------------------------------------------------
// Benchmark-profilok (a solver benchmark ablak méréseiből)
// [avgScore, avgMoves, avgMerges, bestHigh]
// ---------------------------------------------------------------------------
const profileData = {
  "10-none": {
    label: "Stack 10 · modifierek nélkül",
    // 2026-07-11 kibővített benchmark (100 run/solver, neutral tuning).
    solvers: {
      "MOCA+": [6378, 443.3, 403.3, 256], "PLAN-5": [5711, 418.1, 378.1, 256],
      "PLAN-3": [5619, 410, 370, 256], MOCA: [5514, 398.7, 358.7, 256],
      COMBO: [4817, 375.3, 335.3, 256], LOOK: [4778, 372.2, 332.2, 256],
      HEUR: [3221, 298.6, 258.6, 128], MCTS: [2838, 274, 234, 128],
      BAL: [2603, 267.4, 227.4, 128], MERG: [2455, 256.2, 216.2, 128],
      STALL: [2096, 230, 190, 128], RAND: [445, 103, 63, 32],
    },
  },
  "10-max": {
    label: "Stack 10 · maxolt modifierek",
    // 2026-07-11 kibővített benchmark, emelt keretekkel (0% move-cap/timeout, 100 run/solver).
    solvers: {
      "PLAN-3": [13745, 659.8, 609.8, 2048], "PLAN-5": [13490, 654.7, 604.7, 1024],
      "MOCA+": [11716, 575.2, 525.2, 1024], COMBO: [11544, 604.9, 554.9, 1024],
      MOCA: [10378, 520.2, 470.2, 2048], LOOK: [10222, 552.8, 502.8, 1024],
      HEUR: [7145, 465.6, 415.6, 512], MCTS: [6786, 445.7, 395.7, 512],
      MERG: [5355, 393.7, 343.7, 512], STALL: [5238, 369.1, 319.1, 512],
      BAL: [4869, 370.2, 320.2, 512], RAND: [1747, 216.9, 166.9, 128],
    },
  },
  "5-est": { label: "Stack 5 · becslés", solvers: {} },
};

const cheapSolvers = new Set(["BAL", "MERG", "STALL", "HEUR", "RAND"]);
const solvers = ["RAND", "MERG", "BAL", "STALL", "HEUR", "LOOK", "COMBO", "MCTS", "MOCA", "MOCA+", "PLAN-3", "PLAN-5"];
for (const solver of solvers) {
  profileData["5-est"].solvers[solver] = solver === "RAND"
    ? [440, 100, 60, 32]
    : cheapSolvers.has(solver) ? [2200, 150, 110, 128] : [3800, 200, 160, 256];
}

// Bevétel-források — FIX slot-sorrend (a paletta CVD-biztonsága a sorrendtől függ).
const incomeSources = [
  ["merge", "Merge"],
  ["placement", "Lerakás"],
  ["newHigh", "New high"],
  ["combo", "Combo többlet"],
  ["runBonus", "Run bónusz"],
  ["salvage", "Salvage"],
  ["passive", "Passzív"],
  ["token", "Token-érték*"],
];

// ---------------------------------------------------------------------------
// Segédek
// ---------------------------------------------------------------------------
const $ = (id) => document.getElementById(id);
const state = {};

function num(id) { return Number($(id).value || 0); }
function clampIdx(v, arr) { return Math.min(Math.max(0, v), arr.length - 1); }
function cloneState(s) { return JSON.parse(JSON.stringify(s)); }
function pct(delta, base) { return base <= 0 ? 0 : (delta / base) * 100; }
function levelBonus(arr, level) {
  const count = Math.min(Math.max(0, level), arr.length);
  let total = 0;
  for (let i = 0; i < count; i++) total += arr[i];
  return total;
}

// GetHighestBlockRewardMultiplier tükör (játékkóddal egyeztetve).
function hmOf(high) {
  const log = Math.floor(Math.log2(Math.max(1, high)));
  if (log >= 14) return 6.0;
  if (log >= 13) return 5.5;
  if (log >= 12) return 5.0;
  if (log >= 11) return 4.4;
  if (log >= 10) return 3.8;
  if (log >= 9) return 3.0;
  if (log >= 8) return 2.4;
  if (log >= 7) return 1.9;
  if (log >= 6) return 1.5;
  if (log >= 5) return 1.2;
  return 1.0;
}

function fmt(n) {
  if (!Number.isFinite(n)) return "—";
  const abs = Math.abs(n);
  if (abs >= 1e9) return `${(n / 1e9).toFixed(2)}B`;
  if (abs >= 1e6) return `${(n / 1e6).toFixed(2)}M`;
  if (abs >= 1e3) return `${(n / 1e3).toFixed(1)}K`;
  return n.toFixed(abs >= 100 ? 0 : abs >= 10 ? 1 : 2);
}

function fmtPct(n) { return `${n >= 0 ? "+" : ""}${n.toFixed(1)}%`; }

function fmtTime(seconds) {
  if (!Number.isFinite(seconds) || seconds < 0) return "∞";
  if (seconds < 60) return `${seconds.toFixed(1)} mp`;
  const m = Math.floor(seconds / 60);
  const s = Math.round(seconds % 60);
  if (m < 60) return `${m}p ${s}mp`;
  const h = Math.floor(m / 60);
  if (h < 48) return `${h}ó ${m % 60}p`;
  return `${Math.floor(h / 24)}nap ${h % 24}ó`;
}

// ---------------------------------------------------------------------------
// A gazdasági mag
// ---------------------------------------------------------------------------
function solverInterval(s) {
  const base = C.moveIntervals[clampIdx(s.levels.speed, C.moveIntervals)];
  let pacing = solverPacing[s.solver] || 1;
  if (heavySolvers.has(s.solver)) {
    pacing *= Math.max(0.35, 1 - s.levels.compute * C.computeReduction);
  }
  if (s.agents.overclocker) pacing *= 0.75;
  // Közelítés: a Neural Accelerator a solver CPU-idejét felezi, nem az intervallumot.
  if (s.modifiers.neuralAccelerator > 0 && ["MOCA", "MOCA+", "MCTS"].includes(s.solver)) pacing *= 0.5;
  return Math.max(0.012, base * pacing);
}

function globalMultiplier(s) {
  const stage = s.layer === "modifiers" ? 24 : s.layer === "agents" ? 5 : 1;
  const chip = 1 + levelBonus(C.chipYieldBonusByLevel, s.levels.chipYield);
  const token = 1 + Math.sqrt(Math.max(0, s.tokens)) * s.levels.tokenDividend * C.tokenDividendPerSqrtTokenPerLevel;
  return stage * chip * s.researchIncome * s.datacenterMarket * token;
}

function computeEconomy(s) {
  const global = globalMultiplier(s);
  const interval = solverInterval(s);
  const playSeconds = Math.max(0.001, s.moves * interval);
  const cycleSeconds = playSeconds + C.autoRestartSeconds;

  // A Score lerakás-értékeket IS tartalmaz (Score += placedValue + mergedValue),
  // ezért az átlagos merge-tile-t a lerakási rész levonásával becsüljük.
  const mergedScore = Math.max(0, s.avgScore - s.moves * s.avgSpawnTile);
  const avgMergeTile = s.merges > 0 ? Math.max(2, mergedScore / s.merges) : 0;

  // A high-szorzó a run KÖZBENI aktuális csúcsot használja, ami a végső high alatt
  // jár a run nagy részében — a merge-ekre a csúcs és a fél-csúcs átlagát vesszük.
  const hmTop = hmOf(s.bestHigh);
  const hmMerge = (hmTop + hmOf(Math.max(1, s.bestHigh / 2))) / 2;

  const mergeAgent = s.agents.mergeBroker ? 1.75 : 1;
  const highAgent = s.agents.highwaterAnalyst ? 3.0 : 1;
  const scoreAgent = s.agents.scoreAuditor ? 3.0 : 1;
  const flatPlacement = s.agents.quartermaster ? 10 : 0;
  const catalyst = s.modifiers.catalystStack > 0 ? 2 : 1;
  const comboMultiplier = 1 + Math.min(C.comboStreakCap, s.comboStreak) * levelBonus(C.comboBonusPerStreakByLevel, s.levels.combo);

  const placement = s.moves * (2 + flatPlacement);
  const merge = s.merges * avgMergeTile * hmMerge * 0.55 * mergeAgent * catalyst;

  // New-high: tier-enként fizet (2^t × hm(2^t) × 0.85), és az "egyenlő csúcs"
  // merge-ek miatt tier-enként átlagosan >1 esemény van (newHighEvents knob).
  let newHighBase = 0;
  for (let t = 2; (1 << t) <= s.bestHigh; t++) {
    newHighBase += (1 << t) * hmOf(1 << t) * 0.85;
  }
  const newHigh = newHighBase * highAgent * s.newHighEvents;

  const movePre = placement + merge + newHigh;
  const moveIncome = movePre * comboMultiplier * C.incomeScale * global;
  const comboShare = comboMultiplier > 1 ? moveIncome * (comboMultiplier - 1) / comboMultiplier : 0;
  const shareBase = moveIncome - comboShare;
  const placementShare = movePre > 0 ? shareBase * placement / movePre : 0;
  const mergeShare = movePre > 0 ? shareBase * merge / movePre : 0;
  const newHighShare = movePre > 0 ? shareBase * newHigh / movePre : 0;

  // Run-végi bónusz (AwardRunCompleted tükör). A Velocity a JÁTÉKidőt használja.
  const scoreBonus = Math.max(1, s.avgScore) * 0.22 * hmTop * scoreAgent;
  const moveDividend = s.agents.moveDividend ? s.moves * Math.max(1, hmTop * 0.35) * 18 : 0;
  const movesPerSecond = s.moves / playSeconds;
  const velocity = s.agents.velocityTrader ? scoreBonus * Math.min(7.5, Math.max(0, movesPerSecond - 1) * 0.54) : 0;
  const runBonus = (scoreBonus + moveDividend + velocity) * (1 + s.levels.profitableEnding * C.profitableEndingPerLevel) * C.incomeScale * global;

  // Salvage a run-végi score-ból fizet (AwardSalvage tükör, score-bázisú rework után).
  const salvage = Math.max(0, s.avgScore) * levelBonus(C.salvageShareByLevel, s.levels.salvage) * C.incomeScale * global;

  // Passzív: játék közben Active Multiplierrel, a restart 1,2 mp alatt anélkül.
  const passiveBase = C.passiveYieldPerTick[clampIdx(s.levels.passiveYield, C.passiveYieldPerTick)] || 0;
  const passiveInterval = C.passiveTickIntervals[clampIdx(s.levels.passiveTick, C.passiveTickIntervals)] || 14;
  const activeMult = 1 + levelBonus(C.activeMultiplierBonusByLevel, s.levels.activeMultiplier);
  const passive = passiveBase > 0
    ? (passiveBase * activeMult / passiveInterval * playSeconds + passiveBase / passiveInterval * C.autoRestartSeconds) * global
    : 0;

  // Token-áram chip-egyenértéken: a pótlási ár a TARTOTT tokenekkel skálázik
  // (pack-ár × (1 + tartott/100)), ezért a marginális token-érték is. Külön jelölt becslés.
  const tokensPerRun = (s.agents.tokenProspector ? Math.floor(s.merges / C.tokenProspectorMergeTarget) : 0)
    + (s.agents.restartSponsor ? 1 : 0);
  const tokenChipValue = C.tokenPackCost * (1 + Math.max(0, s.tokens) / 100) / C.tokenPackSize;
  const tokenValue = s.includeTokenValue ? tokensPerRun * tokenChipValue : 0;

  const totalRun = moveIncome + runBonus + salvage + passive + tokenValue;
  return {
    global, interval, playSeconds, cycleSeconds, avgMergeTile, hmTop, hmMerge, comboMultiplier,
    placement, merge, newHigh, movePre, moveIncome,
    shares: {
      merge: mergeShare, placement: placementShare, newHigh: newHighShare, combo: comboShare,
      runBonus, salvage, passive, token: tokenValue,
    },
    runBonus, salvage, passive, tokenValue, tokensPerRun,
    totalRun,
    chipsPerSecond: totalRun / cycleSeconds,
  };
}

// ---------------------------------------------------------------------------
// Delta-sorok (következő szint / agent / eco-modifier)
// ---------------------------------------------------------------------------
function verdictOf(payback, deltaCps) {
  if (!Number.isFinite(payback) || deltaCps <= 0) return ["nulla", "Nincs mérhető hatás"];
  if (payback < 120) return ["strong", "Nagyon megéri"];
  if (payback < 1200) return ["ok", "Korrekt"];
  if (payback < 3600) return ["weak", "Gyenge"];
  return ["bad", "Nagyon gyenge"];
}

function buildDeltaRow(name, note, cost, beforeState, afterState) {
  const before = computeEconomy(beforeState);
  const after = computeEconomy(afterState);
  const deltaRun = after.totalRun - before.totalRun;
  const deltaCps = after.chipsPerSecond - before.chipsPerSecond;
  const payback = deltaCps > 0 ? cost / deltaCps : Infinity;
  const [verdictKey, verdictLabel] = verdictOf(payback, deltaCps);
  return {
    name, note, cost, deltaRun,
    deltaRunPct: pct(deltaRun, before.totalRun),
    deltaCps,
    deltaCpsPct: pct(deltaCps, before.chipsPerSecond),
    payback, verdictKey, verdictLabel,
  };
}

function nextLevelRows(s) {
  return levelDefs
    .filter(([, , , kind]) => kind === "eco")
    .map(([key, label, costs]) => {
      const level = s.levels[key];
      if (level >= costs.length) return null;
      if ((key === "passiveTick" || key === "activeMultiplier") && s.levels.passiveYield < 1) return null;
      const next = cloneState(s);
      next.levels[key] = level + 1;
      return buildDeltaRow(label, `L${level}→L${level + 1}`, costs[level], s, next);
    })
    .filter(Boolean)
    .sort((a, b) => a.payback - b.payback);
}

function toggleRows(s) {
  const rows = [];
  agents.forEach(([key, label, cost]) => {
    if (!s.agents[key]) {
      const next = cloneState(s);
      next.agents[key] = true;
      rows.push(buildDeltaRow(label, "agent", cost, s, next));
    }
  });
  modifiers.filter(([, , , kind]) => kind === "eco").forEach(([key, label, costs]) => {
    const level = s.modifiers[key];
    if (level < costs.length) {
      const next = cloneState(s);
      next.modifiers[key] = level + 1;
      rows.push(buildDeltaRow(label, `mod L${level}→L${level + 1}`, costs[level], s, next));
    }
  });
  return rows.sort((a, b) => a.payback - b.payback);
}

function solverRows(s) {
  const profile = profileData[$("stackProfile").value];
  return solvers.map((solver) => {
    const p = profile.solvers[solver] || profile.solvers.RAND;
    const next = cloneState(s);
    next.solver = solver;
    next.avgScore = p[0];
    next.moves = p[1];
    next.merges = p[2];
    next.bestHigh = p[3];
    return { solver, profile: p, economy: computeEconomy(next) };
  }).sort((a, b) => b.economy.chipsPerSecond - a.economy.chipsPerSecond);
}

// ---------------------------------------------------------------------------
// UI
// ---------------------------------------------------------------------------
function init() {
  fillSelect("stackProfile", Object.entries(profileData).map(([key, p]) => [key, p.label]));
  fillSelect("modifierProfile", [["none", "Nincs modifier"], ["max", "Maxolt modifierek"], ["custom", "Egyéni"]]);
  fillSelect("solver", solvers.map((sv) => [sv, sv]));
  fillSelect("abProfileA", Object.entries(profileData).map(([key, p]) => [key, p.label]));
  fillSelect("abProfileB", Object.entries(profileData).map(([key, p]) => [key, p.label]));
  $("stackProfile").value = "10-none";
  $("modifierProfile").value = "none";
  $("solver").value = "PLAN-3";
  $("abProfileA").value = "10-none";
  $("abProfileB").value = "10-max";

  const levelInputs = $("levelInputs");
  const statLevelInputs = $("statLevelInputs");
  levelDefs.forEach(([key, label, costs, kind, hint]) => {
    const el = document.createElement("label");
    el.title = hint;
    el.innerHTML = `<span>${label}</span><input id="level_${key}" type="number" min="0" max="${costs.length}" step="1" value="0">`;
    (kind === "eco" ? levelInputs : statLevelInputs).appendChild(el);
  });

  const agentInputs = $("agentInputs");
  agents.forEach(([key, label, , , hint]) => {
    const row = document.createElement("label");
    row.className = "check";
    row.title = hint;
    row.innerHTML = `<input id="agent_${key}" type="checkbox"><span>${label}</span>`;
    agentInputs.appendChild(row);
  });

  const modifierInputs = $("modifierInputs");
  modifiers.forEach(([key, label, costs, , hint]) => {
    const row = document.createElement("label");
    row.title = hint;
    row.innerHTML = `<span>${label}</span><input id="mod_${key}" type="number" min="0" max="${costs.length}" step="1" value="0">`;
    modifierInputs.appendChild(row);
  });

  document.querySelectorAll("input,select").forEach((el) => el.addEventListener("input", render));
  $("stackProfile").addEventListener("change", loadProfile);
  $("solver").addEventListener("change", loadProfile);
  $("modifierProfile").addEventListener("change", applyModifierProfile);
  $("exportTsv").addEventListener("click", exportTsv);
  $("resetDefaults").addEventListener("click", () => location.reload());
  applyModifierProfile();
  loadProfile();
}

function fillSelect(id, rows) {
  $(id).innerHTML = rows.map(([value, label]) => `<option value="${value}">${label}</option>`).join("");
}

function loadProfile() {
  const profile = profileData[$("stackProfile").value];
  const p = profile.solvers[$("solver").value] || profile.solvers.RAND;
  $("avgScore").value = Math.round(p[0]);
  $("moves").value = Math.round(p[1]);
  $("merges").value = Math.round(p[2]);
  $("bestHigh").value = Math.round(p[3]);
  render();
}

function applyModifierProfile() {
  const value = $("modifierProfile").value;
  if (value !== "custom") {
    modifiers.forEach(([key, , costs]) => {
      $(`mod_${key}`).value = value === "max" ? costs.length : 0;
    });
  }
  render();
}

function readState(overrides = {}) {
  const levels = {};
  levelDefs.forEach(([key]) => (levels[key] = num(`level_${key}`)));
  const agentState = {};
  agents.forEach(([key]) => (agentState[key] = $(`agent_${key}`).checked));
  const modState = {};
  modifiers.forEach(([key]) => (modState[key] = num(`mod_${key}`)));
  return {
    solver: $("solver").value,
    moves: num("moves"),
    merges: num("merges"),
    avgScore: num("avgScore"),
    bestHigh: num("bestHigh"),
    layer: $("layer").value,
    researchIncome: Math.max(1, num("researchIncome")),
    datacenterMarket: Math.max(1, num("datacenterMarket")),
    tokens: num("tokens"),
    avgSpawnTile: Math.max(2, num("avgSpawnTile")),
    newHighEvents: Math.max(0, num("newHighEvents")),
    comboStreak: num("comboStreak"),
    includeTokenValue: $("includeTokenValue").checked,
    levels,
    agents: agentState,
    modifiers: modState,
    ...overrides,
  };
}

function verdictChip(row) {
  return `<span class="chip chip-${row.verdictKey}"><i></i>${row.verdictLabel}</span>`;
}

function meterCell(value, max) {
  const w = max > 0 ? Math.max(0, Math.min(100, (value / max) * 100)) : 0;
  return `<div class="meter"><div class="meter-fill" style="width:${w}%"></div></div>`;
}

function renderDeltaTable(id, rows) {
  const maxCps = Math.max(...rows.map((r) => Math.max(0, r.deltaCps)), 1e-9);
  const body = rows.map((r) => `
    <tr>
      <td class="t-name">${r.name} <span class="t-note">${r.note}</span></td>
      <td class="t-num">${fmt(r.cost)}</td>
      <td class="t-num ${r.deltaCps > 0 ? "good" : r.deltaCps < 0 ? "bad" : "mute"}">${r.deltaCps > 0 ? "+" : ""}${fmt(r.deltaCps)}</td>
      <td class="t-num ${r.deltaCpsPct > 0 ? "good" : "mute"}">${fmtPct(r.deltaCpsPct)}</td>
      <td class="t-meter">${meterCell(r.deltaCps, maxCps)}</td>
      <td class="t-num">${fmtTime(r.payback)}</td>
      <td>${verdictChip(r)}</td>
    </tr>`).join("");
  $(id).innerHTML = `<thead><tr>
      <th>Elem</th><th class="t-num">Ár</th><th class="t-num">ΔCPS</th><th class="t-num">ΔCPS %</th>
      <th>Hatás</th><th class="t-num">Megtérülés</th><th>Ítélet</th>
    </tr></thead><tbody>${body}</tbody>`;
}

function renderIncomeBar(economy) {
  const total = economy.totalRun;
  const segments = incomeSources
    .map(([key, label], index) => ({ key, label, index, value: economy.shares[key] || 0 }))
    .filter((seg) => seg.value > 0.0001);
  $("incomeBar").innerHTML = segments.map((seg) => {
    const share = total > 0 ? (seg.value / total) * 100 : 0;
    const labelHtml = share >= 9 ? `<span>${seg.label} ${share.toFixed(0)}%</span>` : "";
    return `<div class="seg seg-${seg.index + 1}" style="flex-grow:${Math.max(share, 0.4)}" title="${seg.label}: ${fmt(seg.value)} chips (${share.toFixed(1)}%)">${labelHtml}</div>`;
  }).join("");
  $("incomeLegend").innerHTML = segments.map((seg) => {
    const share = total > 0 ? (seg.value / total) * 100 : 0;
    return `<span class="legend-item"><i class="sw sw-${seg.index + 1}"></i>${seg.label} <b>${share.toFixed(1)}%</b> <em>${fmt(seg.value)}</em></span>`;
  }).join("");
}

function renderSolverTable(s) {
  const rows = solverRows(s);
  const maxCps = Math.max(...rows.map((r) => r.economy.chipsPerSecond), 1e-9);
  const best = rows[0];
  const body = rows.map((r) => {
    const rel = pct(r.economy.chipsPerSecond - best.economy.chipsPerSecond, best.economy.chipsPerSecond);
    return `
    <tr class="${r.solver === s.solver ? "row-active" : ""}">
      <td class="t-name">${r.solver}</td>
      <td class="t-num">${fmt(r.economy.chipsPerSecond)}</td>
      <td class="t-meter">${meterCell(r.economy.chipsPerSecond, maxCps)}</td>
      <td class="t-num ${rel < -0.05 ? "mute" : "good"}">${r.solver === best.solver ? "legjobb" : fmtPct(rel)}</td>
      <td class="t-num">${fmt(r.economy.totalRun)}</td>
      <td class="t-num">${fmtTime(r.economy.cycleSeconds)}</td>
      <td class="t-num mute">${fmt(r.profile[1])}</td>
      <td class="t-num mute">${fmt(r.profile[2])}</td>
      <td class="t-num mute">${fmt(r.profile[3])}</td>
    </tr>`;
  }).join("");
  $("solverTable").innerHTML = `<thead><tr>
      <th>Solver</th><th class="t-num">Chips/mp</th><th>Hatás</th><th class="t-num">vs legjobb</th>
      <th class="t-num">Run bevétel</th><th class="t-num">Ciklusidő</th>
      <th class="t-num">Moves</th><th class="t-num">Merges</th><th class="t-num">High</th>
    </tr></thead><tbody>${body}</tbody>`;
}

function renderStatShaping(s) {
  const items = [];
  levelDefs.filter(([, , , kind]) => kind === "stat").forEach(([key, label, costs, , hint]) => {
    const level = s.levels[key];
    items.push(`<li><b>${label}</b> <span class="t-note">L${level}/${costs.length} · köv. ár ${level < costs.length ? fmt(costs[level]) : "max"}</span><br><span class="mute">${hint}</span></li>`);
  });
  modifiers.filter(([, , , kind]) => kind === "stat").forEach(([key, label, costs, , hint]) => {
    const level = s.modifiers[key];
    items.push(`<li><b>${label}</b> <span class="t-note">L${level}/${costs.length} · köv. ár ${level < costs.length ? fmt(costs[level]) : "max"}</span><br><span class="mute">${hint}</span></li>`);
  });
  $("statShapingList").innerHTML = items.join("");

  // A/B benchmark-összehasonlítás: ugyanaz a gazdasági állapot, két profil run-statjaival.
  const solver = s.solver;
  const build = (profileKey) => {
    const p = profileData[profileKey].solvers[solver] || profileData[profileKey].solvers.RAND;
    const next = cloneState(s);
    next.avgScore = p[0];
    next.moves = p[1];
    next.merges = p[2];
    next.bestHigh = p[3];
    return computeEconomy(next);
  };
  const a = build($("abProfileA").value);
  const b = build($("abProfileB").value);
  const delta = pct(b.chipsPerSecond - a.chipsPerSecond, a.chipsPerSecond);
  $("abResult").innerHTML = `
    <div class="ab-row"><span>A · ${profileData[$("abProfileA").value].label}</span><b>${fmt(a.chipsPerSecond)} chips/mp</b></div>
    <div class="ab-row"><span>B · ${profileData[$("abProfileB").value].label}</span><b>${fmt(b.chipsPerSecond)} chips/mp</b></div>
    <div class="ab-row ab-delta"><span>B a A-hoz képest</span><b class="${delta >= 0 ? "good" : "bad"}">${fmtPct(delta)}</b></div>`;
}

function render() {
  const s = readState();
  const economy = computeEconomy(s);

  $("kpiRunIncome").textContent = fmt(economy.totalRun);
  $("kpiCps").textContent = fmt(economy.chipsPerSecond);
  $("kpiCycle").textContent = fmtTime(economy.cycleSeconds);
  $("kpiGlobal").textContent = `${economy.global.toFixed(2)}×`;
  $("kpiTokens").textContent = economy.tokensPerRun > 0 ? `+${economy.tokensPerRun}/run` : "0";

  renderIncomeBar(economy);
  const upgrades = nextLevelRows(s);
  const toggles = toggleRows(s);
  renderDeltaTable("upgradeTable", upgrades);
  renderDeltaTable("toggleTable", toggles);
  renderSolverTable(s);
  renderStatShaping(s);

  $("formulaBreakdown").textContent = [
    `Merge-bevétel bázis:   ${fmt(economy.merge)}   (átlag merge-tile ${economy.avgMergeTile.toFixed(1)}, hm_merge ${economy.hmMerge.toFixed(2)}×)`,
    `Lerakás bázis:         ${fmt(economy.placement)}`,
    `New-high bázis:        ${fmt(economy.newHigh)}   (tier-összeg × ${s.newHighEvents} esemény/tier)`,
    `Combo szorzó:          ${economy.comboMultiplier.toFixed(3)}×   (streak ${s.comboStreak}, cap ${C.comboStreakCap})`,
    `Move-bevétel (global után): ${fmt(economy.moveIncome)}`,
    `Run bónusz:            ${fmt(economy.runBonus)}`,
    `Salvage:               ${fmt(economy.salvage)}`,
    `Passzív / run:         ${fmt(economy.passive)}`,
    `Token-érték / run:     ${fmt(economy.tokenValue)}   (${economy.tokensPerRun} token × ${C.tokenPackCost / C.tokenPackSize} chip)`,
    `Global szorzó:         ${economy.global.toFixed(3)}×   |  intervallum ${economy.interval.toFixed(3)} mp  |  játékidő ${fmtTime(economy.playSeconds)} + restart ${C.autoRestartSeconds} mp`,
  ].join("\n");

  state.lastTsv = buildTsv(s, economy, upgrades, toggles, solverRows(s));
}

function buildTsv(s, economy, upgrades, toggles, solverCompare) {
  const lines = [];
  lines.push(["Section", "Name", "Note", "Cost", "DeltaCps", "DeltaCpsPct", "PaybackSeconds", "Verdict"].join("\t"));
  upgrades.forEach((r) => lines.push(["Upgrade", r.name, r.note, r.cost, r.deltaCps, r.deltaCpsPct, r.payback, r.verdictLabel].join("\t")));
  toggles.forEach((r) => lines.push(["Toggle", r.name, r.note, r.cost, r.deltaCps, r.deltaCpsPct, r.payback, r.verdictLabel].join("\t")));
  lines.push("");
  lines.push(["Solver", "ChipsPerSecond", "RunIncome", "CycleSeconds"].join("\t"));
  solverCompare.forEach((r) => lines.push([r.solver, r.economy.chipsPerSecond, r.economy.totalRun, r.economy.cycleSeconds].join("\t")));
  lines.push("");
  incomeSources.forEach(([key, label]) => lines.push(["Income", label, economy.shares[key] || 0].join("\t")));
  lines.push("");
  lines.push(["Scenario", "Solver", s.solver, "Moves", s.moves, "Merges", s.merges, "AvgScore", s.avgScore, "BestHigh", s.bestHigh, "Global", economy.global].join("\t"));
  return lines.join("\n");
}

function exportTsv() {
  const blob = new Blob([state.lastTsv || ""], { type: "text/tab-separated-values;charset=utf-8" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = "stack_merge_balance_formula.tsv";
  a.click();
  URL.revokeObjectURL(url);
}

init();
