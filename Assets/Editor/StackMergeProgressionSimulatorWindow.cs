using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace StackMerge
{
    /// <summary>
    /// Headless progression "bot" + balance lab. The core simulation plays the real game economy
    /// run after run (real awards, purchase methods, virtual real-time clock: move intervals,
    /// restart delays/tokens, PPO training pacing + eval pauses, passive production, datacenter).
    ///
    /// Two modes:
    /// 1. "Run simulation" — one integrated playthrough with per-cycle phase timelines and the
    ///    per-cycle income mix (which % of chips came from merges / run bonus / salvage / ...).
    /// 2. "Run balance audit" — ablation study: a fixed-seed baseline plus one variant per shop /
    ///    meta item in which the auto-buyer NEVER buys that item (common-random-numbers, so the
    ///    difference is the item's contribution, not noise). Variants run in parallel worker
    ///    threads (the whole economy stack is pure C#). The report ranks every item by how much
    ///    slower the playthrough gets without it (Δ%), plus ROI = seconds saved per chips spent —
    ///    weak/overpriced items and load-bearing outliers become immediately visible.
    /// </summary>
    public sealed class StackMergeProgressionSimulatorWindow : EditorWindow
    {
        // Mirrors StackMergeGameBootstrap.AutoRestartDelay — keep in sync.
        private const double AutoRestartDelaySeconds = 1.2;
        // Keep at least this many restart tokens banked before spending chips on other purchases.
        private const int TokenReserve = 25;
        private const string BaselineKey = "(baseline)";

        private static readonly HashSet<string> NoBannedKeys = new();

        private static readonly string[] IncomeSourceNames =
        {
            "placement", "merge", "new high", "combo", "run bonus", "salvage", "passive", "offline"
        };

        // Solvers preferred for grinding income — fastest strong solvers first. The two slowest
        // (MCTS, MOCA+) are skipped so the simulation stays fast; a grinding player uses a quick
        // solver anyway. PPO is excluded (training-only / endgame).
        // Only the available (post-cull) solvers; removed ones can't be unlocked so they never win here anyway.
        private static readonly SolverId[] IncomeSolverPreference =
        {
            SolverId.Combo, SolverId.Plan3, SolverId.Look,
            SolverId.Moca, SolverId.Heur, SolverId.Balance, SolverId.Rand
        };

        // Which agents to keep equipped when there are more hired agents than slots (2, or 3 with
        // the Extra Slot). Ordered by chips-per-real-second impact.
        private static readonly AgentId[] AgentPriority =
        {
            AgentId.Overclocker, AgentId.MergeBroker, AgentId.VelocityTrader,
            AgentId.HighwaterAnalyst, AgentId.ScoreAuditor, AgentId.Quartermaster,
            AgentId.MoveDividend, AgentId.TokenProspector, AgentId.RestartSponsor
        };

        private int maxRuns = 8000;
        private int seed = 12345;
        private int moveCap = 1000;
        private bool stopAtPpo = true;
        private bool simulateResearch = true;
        private int targetPrestiges = 6;
        private int ppoNormalRunsPerPrestige = 90;
        private int ppoTrainingFramesPerStep = 90000;
        private int ppoTrainingMovesPerRun = 150;
        private SolverId manualProxy = SolverId.Rand;
        private float manualSecondsPerMove = 1.0f;
        private float manualRestartSeconds = 5.0f;
        private int wallClockLimitSeconds = 600;
        private int auditSeeds = 1;
        private bool auditMeta;
        private int metaAuditPrestiges = 6;
        private float auditVariantRunCapMultiplier = 2f;
        // The Unity editor's Boehm GC is stop-the-world: allocation-heavy sims across many threads
        // serialize on it, so more threads ≠ faster. 4 is a good default; experiment upward.
        private int auditThreads = 4;
        private int fastImpactWindowRuns = 120;
        private int fastImpactPrestigeHorizon = 3;
        private bool auditSolvers = true;
        private bool auditUnlocks = true;
        private bool auditUpgrades = true;
        private bool auditAgents = true;
        private bool auditModifiers = true;
        private bool auditTokenPack = true;
        private bool auditRunReplayCache = true;
        private bool auditRepresentativeRunCache;
        private string auditKeyFilter = string.Empty;
        private Vector2 scroll;
        private string summary = "Configure and press Run.";
        private string lastPath = string.Empty;
        private volatile bool auditCancelRequested;

        [MenuItem("Tools/Stack Merge/Progression Simulator")]
        public static void Open()
        {
            GetWindow<StackMergeProgressionSimulatorWindow>("Progression Sim").minSize = new Vector2(520f, 620f);
        }

        public static void RunBalanceAuditBatch()
        {
            var runner = CreateInstance<StackMergeProgressionSimulatorWindow>();
            try
            {
                runner.ApplyBatchArguments(Environment.GetCommandLineArgs());
                runner.RunBalanceAudit();

                string dir = Path.Combine(Application.dataPath, "..", "BenchmarkResults");
                Directory.CreateDirectory(dir);
                string path = Path.GetFullPath(Path.Combine(dir, $"balance_audit_summary_{DateTime.Now:yyyyMMdd_HHmmss}.txt"));
                File.WriteAllText(path, runner.summary);
                Debug.Log($"Balance audit summary written to: {path}");
                if (!string.IsNullOrEmpty(runner.lastPath))
                {
                    Debug.Log($"Balance audit TSV written to: {runner.lastPath}");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                throw;
            }
            finally
            {
                DestroyImmediate(runner);
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        public static void RunFastImpactBatch()
        {
            var runner = CreateInstance<StackMergeProgressionSimulatorWindow>();
            try
            {
                runner.ApplyBatchArguments(Environment.GetCommandLineArgs());
                runner.RunFastImpactLab();

                string dir = Path.Combine(Application.dataPath, "..", "BenchmarkResults");
                Directory.CreateDirectory(dir);
                string path = Path.GetFullPath(Path.Combine(dir, $"fast_impact_summary_{DateTime.Now:yyyyMMdd_HHmmss}.txt"));
                File.WriteAllText(path, runner.summary);
                Debug.Log($"Fast impact summary written to: {path}");
                if (!string.IsNullOrEmpty(runner.lastPath))
                {
                    Debug.Log($"Fast impact TSV written to: {runner.lastPath}");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }

                throw;
            }
            finally
            {
                DestroyImmediate(runner);
            }

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private void ApplyBatchArguments(string[] args)
        {
            maxRuns = GetBatchInt(args, "maxRuns", "BALANCE_AUDIT_MAX_RUNS", maxRuns);
            seed = GetBatchInt(args, "seed", "BALANCE_AUDIT_SEED", seed);
            moveCap = GetBatchInt(args, "moveCap", "BALANCE_AUDIT_MOVE_CAP", moveCap);
            wallClockLimitSeconds = GetBatchInt(args, "wallLimit", "BALANCE_AUDIT_WALL_LIMIT", wallClockLimitSeconds);
            auditSeeds = Mathf.Clamp(GetBatchInt(args, "auditSeeds", "BALANCE_AUDIT_SEEDS", auditSeeds), 1, 20);
            auditThreads = Mathf.Clamp(GetBatchInt(args, "auditThreads", "BALANCE_AUDIT_THREADS", auditThreads), 1, 32);
            fastImpactWindowRuns = Mathf.Clamp(GetBatchInt(args, "fastImpactWindow", "FAST_IMPACT_WINDOW_RUNS", fastImpactWindowRuns), 20, 5000);
            fastImpactPrestigeHorizon = Mathf.Clamp(GetBatchInt(args, "fastImpactPrestiges", "FAST_IMPACT_PRESTIGES", fastImpactPrestigeHorizon), 1, 100);
            auditVariantRunCapMultiplier = Mathf.Clamp(GetBatchFloat(args, "variantCapX", "BALANCE_AUDIT_VARIANT_CAP_X", auditVariantRunCapMultiplier), 1.1f, 50f);
            auditMeta = GetBatchBool(args, "auditMeta", "BALANCE_AUDIT_META", auditMeta);
            metaAuditPrestiges = Mathf.Clamp(GetBatchInt(args, "metaPrestiges", "BALANCE_AUDIT_META_PRESTIGES", metaAuditPrestiges), 1, 1000);
            auditRunReplayCache = GetBatchBool(args, "runReplayCache", "BALANCE_AUDIT_RUN_REPLAY_CACHE", auditRunReplayCache);
            auditRepresentativeRunCache = GetBatchBool(args, "experimentalEstimateCache", "BALANCE_AUDIT_EXPERIMENTAL_CACHE", auditRepresentativeRunCache);
            auditSolvers = GetBatchBool(args, "auditSolvers", "BALANCE_AUDIT_SOLVERS", auditSolvers);
            auditUnlocks = GetBatchBool(args, "auditUnlocks", "BALANCE_AUDIT_UNLOCKS", auditUnlocks);
            auditUpgrades = GetBatchBool(args, "auditUpgrades", "BALANCE_AUDIT_UPGRADES", auditUpgrades);
            auditAgents = GetBatchBool(args, "auditAgents", "BALANCE_AUDIT_AGENTS", auditAgents);
            auditModifiers = GetBatchBool(args, "auditModifiers", "BALANCE_AUDIT_MODIFIERS", auditModifiers);
            auditTokenPack = GetBatchBool(args, "auditTokenPack", "BALANCE_AUDIT_TOKEN_PACK", auditTokenPack);
            auditKeyFilter = GetBatchString(args, "auditFilter", "BALANCE_AUDIT_FILTER", auditKeyFilter);
        }

        private static string GetBatchString(string[] args, string name, string environmentName, string fallback)
        {
            string prefixA = $"-{name}=";
            string prefixB = $"--{name}=";
            foreach (string arg in args ?? Array.Empty<string>())
            {
                if (arg.StartsWith(prefixA, StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring(prefixA.Length);
                }

                if (arg.StartsWith(prefixB, StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring(prefixB.Length);
                }
            }

            string environment = Environment.GetEnvironmentVariable(environmentName);
            return string.IsNullOrEmpty(environment) ? fallback : environment;
        }

        private static int GetBatchInt(string[] args, string name, string environmentName, int fallback)
        {
            return int.TryParse(GetBatchString(args, name, environmentName, string.Empty), out int value) ? value : fallback;
        }

        private static float GetBatchFloat(string[] args, string name, string environmentName, float fallback)
        {
            return float.TryParse(GetBatchString(args, name, environmentName, string.Empty), out float value) ? value : fallback;
        }

        private static bool GetBatchBool(string[] args, string name, string environmentName, bool fallback)
        {
            string raw = GetBatchString(args, name, environmentName, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            return raw.Equals("1", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("true", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || raw.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Progression Simulator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Plays the real economy run-by-run with an auto-buyer (cheapest affordable first). Tracks a virtual " +
                "real-time clock and reports per-cycle phases + income mix. The Balance Audit runs one extra " +
                "simulation per item with that item banned (same seeds) and ranks every upgrade/agent/modifier/" +
                "solver/research/datacenter element by its measured contribution.",
                MessageType.Info);

            maxRuns = Mathf.Clamp(EditorGUILayout.IntField("Max runs", maxRuns), 10, 1000000);
            seed = EditorGUILayout.IntField("Seed", seed);
            moveCap = Mathf.Clamp(EditorGUILayout.IntField("Move cap / run", moveCap), 20, 5000);
            stopAtPpo = EditorGUILayout.Toggle("Stop when PPO unlocks", stopAtPpo);
            simulateResearch = EditorGUILayout.Toggle(new GUIContent("Simulate prestige / research", "Continues after PPO through training, Normal-mode runs and prestige cycles, auto-buying research nodes."), simulateResearch);
            using (new EditorGUI.DisabledScope(!simulateResearch || stopAtPpo))
            {
                targetPrestiges = Mathf.Clamp(EditorGUILayout.IntField("Target prestiges", targetPrestiges), 1, 1000);
                ppoTrainingFramesPerStep = Mathf.Clamp(EditorGUILayout.IntField("PPO training frames / step", ppoTrainingFramesPerStep), 1000, 2000000);
                ppoTrainingMovesPerRun = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("PPO training moves / run", "Estimated average run length while training — used to count the per-run 'Evaluating…' pauses."), ppoTrainingMovesPerRun), 20, 2000);
                ppoNormalRunsPerPrestige = Mathf.Clamp(EditorGUILayout.IntField("PPO normal runs / prestige", ppoNormalRunsPerPrestige), 1, 10000);
            }
            manualProxy = (SolverId)EditorGUILayout.EnumPopup(new GUIContent("Manual play proxy", "Solver used to model the human player before Auto Solve is bought."), manualProxy);
            manualSecondsPerMove = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Manual sec / move", "How long a human takes per move before Auto Solve."), manualSecondsPerMove), 0.1f, 30f);
            manualRestartSeconds = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Manual restart sec", "Time to restart by hand — also used when Auto Restart has no token."), manualRestartSeconds), 0.5f, 120f);
            wallClockLimitSeconds = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Editor time limit (s)", "Safety cap per simulation (per audit variant too)."), wallClockLimitSeconds), 30, 14400);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Balance audit", EditorStyles.boldLabel);
            auditSeeds = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Audit seeds", "Runs every variant with this many seeds and averages the metrics. 1 = fastest, single common-random-numbers comparison."), auditSeeds), 1, 5);
            auditThreads = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Audit threads", "Parallel variant simulations. The editor's GC serializes allocation-heavy threads, so past ~4 the gains flatten — experiment."), auditThreads), 1, 16);
            fastImpactWindowRuns = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Fast impact window", "Runs before/after a purchase used for the local impact estimate. Larger = smoother but less local."), fastImpactWindowRuns), 20, 1000);
            fastImpactPrestigeHorizon = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Fast impact prestiges", "Single-baseline horizon for the fast impact lab. Use >1 to include Research and Datacenter items."), fastImpactPrestigeHorizon), 1, 30);
            auditVariantRunCapMultiplier = Mathf.Clamp(EditorGUILayout.FloatField(new GUIContent("Variant run cap x", "Variants stop after this multiplier of the baseline run count. 2x is the normal REQUIRED threshold; raise it for targeted diagnosis."), auditVariantRunCapMultiplier), 1.1f, 20f);
            auditRunReplayCache = EditorGUILayout.Toggle(new GUIContent("Fast run replay cache", "Reuses deterministic run outcomes for identical seed + gameplay state. Keeps economy rewards exact by replaying counters and re-applying global income multipliers."), auditRunReplayCache);
            auditRepresentativeRunCache = EditorGUILayout.Toggle(new GUIContent("Experimental estimate cache", "Unsafe for final balance numbers: reuses one representative run result per gameplay state. Keep this off for exact audits."), auditRepresentativeRunCache);
            auditKeyFilter = EditorGUILayout.TextField(new GUIContent("Audit key filter", "Optional case-insensitive filter. Example: Upgrade.Speed, PassiveYield, TokenPack. Empty = audit every enabled key."), auditKeyFilter ?? string.Empty);
            using (new EditorGUILayout.HorizontalScope())
            {
                auditSolvers = GUILayout.Toggle(auditSolvers, "Solvers");
                auditUnlocks = GUILayout.Toggle(auditUnlocks, "Unlocks");
                auditUpgrades = GUILayout.Toggle(auditUpgrades, "Upgrades");
                auditAgents = GUILayout.Toggle(auditAgents, "Agents");
                auditModifiers = GUILayout.Toggle(auditModifiers, "Modifiers");
                auditTokenPack = GUILayout.Toggle(auditTokenPack, "Tokens");
            }

            auditMeta = EditorGUILayout.Toggle(new GUIContent("Audit meta layer", "Also ablate research nodes and datacenter items over a multi-prestige horizon (much slower)."), auditMeta);
            using (new EditorGUI.DisabledScope(!auditMeta))
            {
                metaAuditPrestiges = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Meta horizon (prestiges)", "How many prestiges the meta-scope variants simulate."), metaAuditPrestiges), 2, 30);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run simulation", GUILayout.Height(30f)))
                {
                    Run();
                }

                if (GUILayout.Button("Run balance audit", GUILayout.Height(30f)))
                {
                    RunBalanceAudit();
                }

                if (GUILayout.Button("Run fast impact lab", GUILayout.Height(30f)))
                {
                    RunFastImpactLab();
                }

                if (!string.IsNullOrEmpty(lastPath) && GUILayout.Button("Reveal log", GUILayout.Height(30f), GUILayout.Width(100f)))
                {
                    EditorUtility.RevealInFinder(lastPath);
                }
            }

            if (!string.IsNullOrEmpty(lastPath))
            {
                EditorGUILayout.LabelField("Full TSV:", lastPath);
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(summary, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        // ---------------------------------------------------------------------------------------
        // Normal mode
        // ---------------------------------------------------------------------------------------

        private void Run()
        {
            // A Play-mode "unlock Datacenter in Editor" test toggle must never leak into sims.
            StackMergeProgression.DebugUnlockDatacenter = false;
            auditCancelRequested = false;
            var config = new SimCoreConfig
            {
                Seed = seed,
                MaxRuns = maxRuns,
                StopAtPpo = stopAtPpo,
                SimulateResearch = simulateResearch,
                TargetPrestiges = targetPrestiges,
                WallClockLimitSeconds = wallClockLimitSeconds,
                BannedKeys = null,
                CollectDetails = true
            };

            SimOutcome outcome = RunSimulationCore(config);
            string prefix = outcome.HitSafetyLimit
                ? $"Stopped after {outcome.Runs} runs ({wallClockLimitSeconds}s safety limit). Raise 'Editor time limit' or reduce Max runs.\n\n"
                : string.Empty;
            summary = prefix + BuildSummary(outcome.Progression, outcome.Sim, outcome.Runs);
            lastPath = WriteFile(outcome);
            Repaint();
        }

        /// <summary>
        /// The whole simulation loop, thread-safe (no Unity-object access): builds its own
        /// progression/solvers/rng and returns everything the reporters need. `BannedKeys` items are
        /// invisible to every auto-buyer — the ablation audit's lever.
        /// </summary>
        private SimOutcome RunSimulationCore(SimCoreConfig config)
        {
            StackMergeProgression progression = config.StartSnapshot?.Progression ?? new StackMergeProgression(new StackMergeProgressionData());
            IStackMergeSolver[] solvers = StackMergeSolverFactory.CreateAll();
            SimRandom rng = config.StartSnapshot?.Random ?? new SimRandom(config.Seed);
            SimState sim = config.StartSnapshot?.Sim ?? new SimState();
            HashSet<string> banned = config.BannedKeys ?? NoBannedKeys;
            var outcome = new SimOutcome { Sim = sim, Progression = progression };
            StringBuilder details = config.CollectDetails ? new StringBuilder() : null;
            details?.AppendLine("Run\tSimTime\tChips\tInsight\tCycleInsight\tPrestiges\tGrossIncome\tCumEarned\tSolver\tScore\tMoves\tHigh\tBought");

            int run = config.StartSnapshot?.CompletedRuns ?? 0;
            bool ppoUnlocked = config.StartSnapshot?.PpoUnlocked ?? progression.IsSolverUnlocked(SolverId.MachineLearning);
            int sampleEvery = Mathf.Max(1, config.MaxRuns / 400);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (run < config.MaxRuns
                   && !(config.StopAtPpo && ppoUnlocked)
                   && !(config.SimulateResearch && !config.StopAtPpo && config.TargetPrestiges > 0 && progression.PrestigeCount >= config.TargetPrestiges))
            {
                run++;
                double clockBefore = sim.Clock;
                bool researchLayer = config.SimulateResearch && !config.StopAtPpo && progression.IsSolverUnlocked(SolverId.MachineLearning);
                long gross = 0;
                string activity;
                RunResult result = default;
                SolverId solverId = manualProxy;

                if (researchLayer && !progression.MachineLearningPlayingModeUnlocked)
                {
                    // PPO training phase. Exclusive like in the real game: no chip income, passive
                    // production suspended.
                    activity = AdvancePpoTraining(progression, sim, run);
                    solverId = SolverId.MachineLearning;
                }
                else if (researchLayer)
                {
                    activity = AdvancePpoNormalAndPrestige(progression, sim, run, rng, config.TargetPrestiges, banned, config.ForkSnapshots, config.ForkSnapshotPrefix);
                    solverId = SolverId.MachineLearning;
                }
                else
                {
                    // Regular income run with the fastest strong unlocked solver (or the manual proxy).
                    bool auto = progression.AutoSolveEnabled && progression.HasPurchasedSolver;
                    solverId = auto ? BestIncomeSolver(progression) : manualProxy;
                    if (auto)
                    {
                        // Keep the game's "selected solver" in sync with what we actually play, so
                        // IsMachineLearningTrainingActive can't wrongly suppress income once PPO is owned.
                        progression.SelectOrUnlockSolver(solverId);
                    }

                    result = PlayRun(
                        progression,
                        solvers,
                        solverId,
                        rng,
                        config.RunCache,
                        config.RunCacheStats,
                        config.RepresentativeRunCache,
                        config.RepresentativeRunCacheStats,
                        config.UseRepresentativeRunCache);
                    double moveInterval = auto ? progression.GetMoveInterval(solverId) : manualSecondsPerMove;
                    double runSeconds = result.Moves * moveInterval;
                    sim.Clock += runSeconds;
                    progression.TickPassiveProduction((float)runSeconds, true);

                    long salvage = progression.AwardSalvage(result.Score);
                    long runBonus = progression.AwardRunCompleted(result.Score, solverId, result.Moves, result.Merges, result.High, !auto, (float)runSeconds);
                    gross = result.MoveIncome + salvage + runBonus;

                    double restartSeconds = ConsumeRestartDelay(progression, auto);
                    sim.Clock += restartSeconds;
                    progression.TickPassiveProduction((float)restartSeconds, false);
                    activity = string.Empty;
                }

                // Datacenter: pick an allocation for the current phase, then advance its production
                // by exactly the simulated seconds this iteration consumed.
                if (progression.DatacenterUnlocked)
                {
                    ApplyDatacenterPolicy(progression);
                    progression.TickDatacenter((float)(sim.Clock - clockBefore));
                }

                string bought = AutoBuy(progression, sim, run, rng, banned, config.ForkSnapshots, config.ForkSnapshotPrefix);
                string datacenterBought = AutoBuyDatacenter(progression, sim, run, rng, banned, config.ForkSnapshots, config.ForkSnapshotPrefix);
                if (!string.IsNullOrEmpty(datacenterBought))
                {
                    bought = string.IsNullOrEmpty(bought) ? datacenterBought : $"{bought}, {datacenterBought}";
                }

                if (!string.IsNullOrEmpty(activity))
                {
                    bought = string.IsNullOrEmpty(bought) ? activity : $"{bought}, {activity}";
                }

                ApplyAgentLoadout(progression);
                RecordPhases(progression, sim, run);
                ppoUnlocked = progression.IsSolverUnlocked(SolverId.MachineLearning);

                if (details != null && (run % sampleEvery == 0 || bought.Length > 0 || run <= 12))
                {
                    long earned = progression.TotalChipsEarned;
                    details.AppendLine($"{run}\t{FormatTime(sim.Clock)}\t{progression.Chips}\t{progression.ResearchInsight}\t{progression.ResearchInsightEarnedThisPrestige}\t{progression.PrestigeCount}\t{gross}\t{earned}\t{solverId}\t{result.Score}\t{result.Moves}\t{result.High}\t{bought}");
                }

                if (config.CollectRunHistory)
                {
                    outcome.RunSamples.Add(new RunSample(
                        run,
                        clockBefore,
                        sim.Clock,
                        progression.Chips,
                        progression.TotalChipsEarned,
                        gross,
                        result.Score,
                        result.Moves,
                        result.Merges,
                        result.High,
                        solverId,
                        progression.PrestigeCount,
                        bought));
                }

                if (auditCancelRequested)
                {
                    outcome.Cancelled = true;
                    break;
                }

                if (stopwatch.Elapsed.TotalSeconds > config.WallClockLimitSeconds)
                {
                    outcome.HitSafetyLimit = true;
                    break;
                }
            }

            CloseOpenCycle(sim, progression, run);
            outcome.Runs = run;
            outcome.PpoUnlockedAtEnd = ppoUnlocked;
            outcome.ReachedHorizon = DidReachHorizon(config, progression, ppoUnlocked);
            outcome.HitRunLimit = HasExplicitHorizon(config)
                && !outcome.ReachedHorizon
                && !outcome.HitSafetyLimit
                && !outcome.Cancelled
                && run >= config.MaxRuns;
            outcome.Details = details;
            return outcome;
        }

        private static bool HasExplicitHorizon(SimCoreConfig config)
        {
            return config.StopAtPpo
                || (config.SimulateResearch && !config.StopAtPpo && config.TargetPrestiges > 0);
        }

        private static bool DidReachHorizon(SimCoreConfig config, StackMergeProgression progression, bool ppoUnlocked)
        {
            if (config.StopAtPpo)
            {
                return ppoUnlocked;
            }

            if (config.SimulateResearch && !config.StopAtPpo && config.TargetPrestiges > 0)
            {
                return progression.PrestigeCount >= config.TargetPrestiges;
            }

            return true;
        }

        private RunResult PlayRun(
            StackMergeProgression progression,
            IStackMergeSolver[] solvers,
            SolverId solverId,
            SimRandom rng,
            ConcurrentDictionary<string, CachedRunResult> runCache,
            RunReplayCacheStats cacheStats,
            ConcurrentDictionary<string, CachedRunResult> representativeRunCache,
            RunReplayCacheStats representativeCacheStats,
            bool useRepresentativeCache)
        {
            int stateSeed = rng.Next();
            int contextSeed = rng.Next();
            bool replaySafe = CanUseRunReplayCache(progression);
            string cacheKey = runCache != null && replaySafe
                ? BuildRunCacheKey(progression, solverId, stateSeed, contextSeed)
                : null;
            string representativeKey = representativeRunCache != null && replaySafe
                ? BuildRepresentativeRunCacheKey(progression, solverId)
                : null;
            if (cacheKey != null && runCache.TryGetValue(cacheKey, out CachedRunResult cached))
            {
                cacheStats?.RecordHit();
                return ReplayCachedRun(progression, cached);
            }

            if (cacheKey != null)
            {
                cacheStats?.RecordMiss();
            }

            if (useRepresentativeCache && representativeKey != null && representativeRunCache.TryGetValue(representativeKey, out CachedRunResult representative))
            {
                representativeCacheStats?.RecordHit();
                return ReplayCachedRun(progression, representative);
            }

            if (useRepresentativeCache && representativeKey != null)
            {
                representativeCacheStats?.RecordMiss();
            }

            StackMergeRunModifiers modifiers = progression.BuildRunModifiers();
            var state = new StackMergeGameState(
                stackCapacity: progression.StackCapacity,
                queueLength: progression.QueueLength,
                difficultyLevel: progression.DifficultyLevel,
                scalingFrequencyLevel: progression.ScalingFrequencyLevel,
                modifiers: modifiers,
                seed: stateSeed);

            IStackMergeSolver solver = solvers[Mathf.Clamp((int)solverId, 0, solvers.Length - 1)];
            var context = new SolverContext(
                new System.Random(contextSeed),
                progression.MonteCarloSimulationCount,
                progression.MonteCarloRolloutDepth,
                lightweightMode: true,
                tuning: progression.SolverTuningUnlocked ? progression.GetSolverTuning(solverId) : SolverTuningSettings.Neutral(solverId),
                highTierSpeedTuningAccelerator: progression.NeuralAcceleratorActive,
                machineLearningAgent: progression.MachineLearningAgent,
                machineLearningTrainingMode: progression.IsMachineLearningTrainingActive);

            bool training = progression.IsMachineLearningTrainingActive;
            long moveIncome = 0;
            bool captureRun = cacheKey != null || representativeKey != null;
            long[] ledgerBefore = captureRun ? progression.CopyIncomeLedger() : null;
            var moveIncomeBeforeGlobal = captureRun ? new List<long>(128) : null;
            int moves = 0;
            int unstableSaves = 0;
            int jokerMerges = 0;
            while (!state.IsGameOver && moves < moveCap)
            {
                SolverDecision decision = solver.ChooseMove(state, context);
                if (!decision.HasMove || !SolverScoring.CanApplyDecision(state, decision))
                {
                    break;
                }

                MoveResult moveResult = SolverScoring.ApplyDecision(state, decision);
                if (!moveResult.Accepted)
                {
                    break;
                }

                if (moveIncomeBeforeGlobal != null)
                {
                    moveIncomeBeforeGlobal.Add(progression.CalculateReplayMoveIncomeBeforeGlobal(moveResult));
                }

                moveIncome += progression.AwardMove(moveResult, training);
                if (moveResult.UnstableSaveUsed)
                {
                    unstableSaves++;
                }

                jokerMerges += Math.Max(0, moveResult.JokerMergeCount);
                if (solverId == SolverId.MachineLearning)
                {
                    progression.ObserveMachineLearningMove(moveResult, state, training);
                }

                moves++;
            }

            // Salvage Protocol input: block value stranded on the board at game over.
            long strandedValue = 0;
            foreach (IReadOnlyList<int> stack in state.Stacks)
            {
                foreach (int blockValue in stack)
                {
                    if (blockValue != StackMergeGameState.JokerBlockValue)
                    {
                        strandedValue += blockValue;
                    }
                }
            }

            if (cacheKey != null)
            {
                long[] ledgerShare = BuildLedgerDelta(ledgerBefore, progression.CopyIncomeLedger());
                var captured = new CachedRunResult(
                    state.Score,
                    state.BlocksDropped,
                    state.TotalMerges,
                    state.HighestMergedBlock,
                    strandedValue,
                    moveIncomeBeforeGlobal?.ToArray() ?? Array.Empty<long>(),
                    ledgerShare,
                    unstableSaves,
                    jokerMerges);
                runCache.TryAdd(cacheKey, captured);
                if (representativeKey != null)
                {
                    representativeRunCache.TryAdd(representativeKey, captured);
                }
            }
            else if (representativeKey != null)
            {
                long[] ledgerShare = BuildLedgerDelta(ledgerBefore, progression.CopyIncomeLedger());
                var captured = new CachedRunResult(
                    state.Score,
                    state.BlocksDropped,
                    state.TotalMerges,
                    state.HighestMergedBlock,
                    strandedValue,
                    moveIncomeBeforeGlobal?.ToArray() ?? Array.Empty<long>(),
                    ledgerShare,
                    unstableSaves,
                    jokerMerges);
                representativeRunCache.TryAdd(representativeKey, captured);
            }

            return new RunResult(state.Score, state.BlocksDropped, state.TotalMerges, state.HighestMergedBlock, moveIncome, strandedValue);
        }

        private static RunResult ReplayCachedRun(StackMergeProgression progression, CachedRunResult cached)
        {
            long replayIncome = progression.AwardReplayRun(
                cached.Moves,
                cached.Merges,
                cached.High,
                cached.MoveIncomeBeforeGlobal,
                cached.MoveLedgerShare,
                cached.UnstableSaves,
                cached.JokerMerges);
            return new RunResult(cached.Score, cached.Moves, cached.Merges, cached.High, replayIncome, cached.StrandedBoardValue);
        }

        private static long[] BuildLedgerDelta(long[] before, long[] after)
        {
            int length = Math.Max(before?.Length ?? 0, after?.Length ?? 0);
            var delta = new long[length];
            for (int i = 0; i < length; i++)
            {
                long previous = before != null && i < before.Length ? before[i] : 0;
                long current = after != null && i < after.Length ? after[i] : 0;
                delta[i] = Math.Max(0, current - previous);
            }

            return delta;
        }

        private static bool CanUseRunReplayCache(StackMergeProgression progression)
        {
            if (progression == null)
            {
                return false;
            }

            // Token Dividend is a global income multiplier based on the current token count. If
            // Token Prospector is also active, the token count can rise inside the run, so the exact
            // per-move multiplier path must be simulated instead of collapsed into one replay gain.
            return progression.TokenDividendLevel <= 0 || !progression.IsAgentActive(AgentId.TokenProspector);
        }

        private static string BuildRunCacheKey(StackMergeProgression progression, SolverId solverId, int stateSeed, int contextSeed)
        {
            var builder = new StringBuilder(192);
            builder.Append((int)solverId).Append('|')
                .Append(stateSeed).Append('|')
                .Append(contextSeed).Append('|')
                .Append(progression.StackCapacity).Append('|')
                .Append(progression.QueueLength).Append('|')
                .Append(progression.DifficultyLevel).Append('|')
                .Append(progression.ScalingFrequencyLevel).Append('|')
                .Append(progression.ComboEngineLevel).Append('|');

            StackMergeRunModifiers modifiers = progression.BuildRunModifiers();
            builder.Append(modifiers.UnstableSaves).Append(',')
                .Append(modifiers.MirrorStack ? 1 : 0).Append(',')
                .Append(modifiers.JokerBlocks ? 1 : 0).Append(',')
                .Append(modifiers.PickaxeUses).Append(',')
                .Append(modifiers.QueueSkips).Append('|');

            foreach (ModifierDefinition modifier in StackMergeProgression.Modifiers)
            {
                builder.Append(progression.GetModifierLevel(modifier.Id)).Append(',');
            }

            builder.Append('|');
            for (int slot = 0; slot < progression.MaxAgentSlots; slot++)
            {
                builder.Append(progression.GetActiveAgentIdAtSlot(slot)).Append(',');
            }

            builder.Append('|')
                .Append(progression.GetResearchLevel(ResearchId.AgentSynergy)).Append('|');

            SolverTuningSettings tuning = progression.SolverTuningUnlocked
                ? progression.GetSolverTuning(solverId)
                : SolverTuningSettings.Neutral(solverId);
            for (int i = 0; i < SolverTuningSettings.MaxSlots; i++)
            {
                builder.Append(tuning.GetSlotValue(i)).Append(',');
            }

            return builder.ToString();
        }

        private static string BuildRepresentativeRunCacheKey(StackMergeProgression progression, SolverId solverId)
        {
            var builder = new StringBuilder(160);
            builder.Append((int)solverId).Append('|')
                .Append(progression.StackCapacity).Append('|')
                .Append(progression.QueueLength).Append('|')
                .Append(progression.DifficultyLevel).Append('|')
                .Append(progression.ScalingFrequencyLevel).Append('|')
                .Append(progression.ComboEngineLevel).Append('|');

            StackMergeRunModifiers modifiers = progression.BuildRunModifiers();
            builder.Append(modifiers.UnstableSaves).Append(',')
                .Append(modifiers.MirrorStack ? 1 : 0).Append(',')
                .Append(modifiers.JokerBlocks ? 1 : 0).Append(',')
                .Append(modifiers.PickaxeUses).Append(',')
                .Append(modifiers.QueueSkips).Append('|');

            foreach (ModifierDefinition modifier in StackMergeProgression.Modifiers)
            {
                builder.Append(progression.GetModifierLevel(modifier.Id)).Append(',');
            }

            builder.Append('|');
            for (int slot = 0; slot < progression.MaxAgentSlots; slot++)
            {
                builder.Append(progression.GetActiveAgentIdAtSlot(slot)).Append(',');
            }

            builder.Append('|')
                .Append(progression.GetResearchLevel(ResearchId.AgentSynergy)).Append('|');

            SolverTuningSettings tuning = progression.SolverTuningUnlocked
                ? progression.GetSolverTuning(solverId)
                : SolverTuningSettings.Neutral(solverId);
            for (int i = 0; i < SolverTuningSettings.MaxSlots; i++)
            {
                builder.Append(tuning.GetSlotValue(i)).Append(',');
            }

            return builder.ToString();
        }

        private static string MakeForkSnapshotPrefix(AuditScope scope, int seedIndex)
        {
            return $"{scope}:{seedIndex}:";
        }

        private static string MakeForkSnapshotKey(AuditScope scope, int seedIndex, string key)
        {
            return MakeForkSnapshotKey(MakeForkSnapshotPrefix(scope, seedIndex), key);
        }

        private static string MakeForkSnapshotKey(string prefix, string key)
        {
            return $"{prefix}{key}";
        }

        private static SimulationForkSnapshot CreateForkSnapshot(StackMergeProgression progression, SimState sim, SimRandom rng, int completedRuns)
        {
            return new SimulationForkSnapshot(
                progression.CloneForSimulation(),
                sim.Clone(),
                rng.Clone(),
                completedRuns,
                progression.IsSolverUnlocked(SolverId.MachineLearning));
        }

        private static void CaptureForkSnapshot(
            ConcurrentDictionary<string, SimulationForkSnapshot> forkSnapshots,
            string forkSnapshotPrefix,
            string key,
            StackMergeProgression progression,
            SimState sim,
            SimRandom rng,
            int completedRuns)
        {
            if (forkSnapshots == null
                || progression == null
                || sim == null
                || rng == null
                || string.IsNullOrEmpty(forkSnapshotPrefix)
                || string.IsNullOrEmpty(key))
            {
                return;
            }

            string snapshotKey = MakeForkSnapshotKey(forkSnapshotPrefix, key);
            if (forkSnapshots.ContainsKey(snapshotKey))
            {
                return;
            }

            forkSnapshots.TryAdd(snapshotKey, CreateForkSnapshot(progression, sim, rng, completedRuns));
        }

        /// <summary>
        /// Advances the clock across one game-over → next-run gap. Auto Restart takes 1.2s and eats
        /// a token exactly like the game; without a token the player restarts by hand.
        /// </summary>
        private double ConsumeRestartDelay(StackMergeProgression progression, bool autoPlay)
        {
            if (autoPlay && progression.AutoRestartUnlocked && progression.AutoRestartEnabled && progression.TryConsumeAutoRestartToken())
            {
                return AutoRestartDelaySeconds;
            }

            return manualRestartSeconds;
        }

        private static SolverId BestIncomeSolver(StackMergeProgression progression)
        {
            foreach (SolverId candidate in IncomeSolverPreference)
            {
                if (progression.IsSolverUnlocked(candidate))
                {
                    return candidate;
                }
            }

            return SolverId.Rand;
        }

        /// <summary>
        /// PPO training phase step: injects a chunk of training frames and charges the clock the
        /// real training-mode move interval per frame plus the per-run "Evaluating…" pauses.
        /// </summary>
        private string AdvancePpoTraining(StackMergeProgression progression, SimState sim, int run)
        {
            progression.SelectOrUnlockSolver(SolverId.MachineLearning);
            progression.SetMachineLearningTrainingMode(true);

            long missingFrames = progression.MachineLearningPlayingModeFrameRequirement - progression.MachineLearningFrames;
            int frames = (int)Math.Min(Math.Max(0, missingFrames), ppoTrainingFramesPerStep);
            var events = new List<string>();
            if (frames > 0)
            {
                double interval = progression.GetMoveInterval(SolverId.MachineLearning);
                double trainingRuns = frames / (double)Mathf.Max(20, ppoTrainingMovesPerRun);
                double evalSeconds = trainingRuns * progression.MachineLearningEvaluationSeconds;
                progression.AddMachineLearningSimulationProgress(frames, 0, 0, 0, 1, 0);
                sim.Clock += frames * interval + evalSeconds;
                events.Add($"PPO train +{frames:N0}f ({FormatTime(frames * interval + evalSeconds)}, eval {FormatTime(evalSeconds)})");
            }

            if (progression.MachineLearningPlayingModeUnlocked)
            {
                progression.SetMachineLearningTrainingMode(false);
                sim.Purchases.Add(new PurchaseRecord(run, sim.Clock, "PPO Training Complete", 0, "frames", progression.MachineLearningFrames, progression.TotalChipsEarned));
                events.Add("PPO Training Complete");
            }

            return string.Join(", ", events);
        }

        /// <summary>
        /// PPO Normal-mode phase step: a batch of Normal runs (estimated result, real per-move time),
        /// then prestige + research buys once Insight is on the table.
        /// </summary>
        private string AdvancePpoNormalAndPrestige(
            StackMergeProgression progression,
            SimState sim,
            int run,
            SimRandom rng,
            int prestigeTarget,
            HashSet<string> banned,
            ConcurrentDictionary<string, SimulationForkSnapshot> forkSnapshots,
            string forkSnapshotPrefix)
        {
            progression.SelectOrUnlockSolver(SolverId.MachineLearning);
            progression.SetMachineLearningTrainingMode(false);

            var events = new List<string>();
            (long score, int high, int moves, int merges) = EstimatePpoNormalRun(progression, rng);
            long insightBefore = progression.ResearchInsight;
            progression.AddMachineLearningSimulationProgress(0, ppoNormalRunsPerPrestige, score, high, moves, merges);
            long passiveInsight = progression.ResearchInsight - insightBefore;
            if (passiveInsight > 0)
            {
                events.Add($"Passive Insight +{passiveInsight}");
            }

            double interval = progression.GetMoveInterval(SolverId.MachineLearning);
            double playSeconds = (double)ppoNormalRunsPerPrestige * moves * interval;
            double restartSeconds = 0;
            for (int i = 0; i < ppoNormalRunsPerPrestige; i++)
            {
                restartSeconds += ConsumeRestartDelay(progression, true);
            }

            sim.Clock += playSeconds + restartSeconds;
            progression.TickPassiveProduction((float)playSeconds, true);
            progression.TickPassiveProduction((float)restartSeconds, false);
            events.Add($"PPO Normal x{ppoNormalRunsPerPrestige} ({FormatTime(playSeconds + restartSeconds)})");

            long preview = progression.PreviewPrestigeInsightGain();
            if (preview > 0 && progression.PrestigeCount < prestigeTarget)
            {
                long gained = progression.ExecutePrestige();
                sim.Purchases.Add(new PurchaseRecord(run, sim.Clock, $"Prestige +{gained} Insight", 0, "Insight", progression.ResearchInsight, progression.TotalChipsEarned));
                RecordPhase(sim, $"Prestige #{progression.PrestigeCount}", run);
                events.Add($"Prestige +{gained}");
                StartNewCycle(sim, progression, run, gained, progression.PrestigeCount);

                string research = AutoBuyResearch(progression, sim, run, rng, banned, forkSnapshots, forkSnapshotPrefix);
                if (!string.IsNullOrEmpty(research))
                {
                    events.Add(research);
                }
            }

            return string.Join(", ", events);
        }

        private (long Score, int High, int Moves, int Merges) EstimatePpoNormalRun(StackMergeProgression progression, SimRandom rng)
        {
            int prestige = Math.Max(0, progression.PrestigeCount);
            // Grounded in the measured PPO benchmark: a Normal-mode session peaks around tile
            // 2048-8192, climbing SLOWLY with PPO research, the prestige count, and how many Normal
            // runs are played. Capped modestly (the small net plateaus).
            double research =
                progression.GetResearchLevel(ResearchId.PpoHighFocus) * 0.25
                + progression.GetResearchLevel(ResearchId.PpoMemory) * 0.18
                + progression.GetResearchLevel(ResearchId.PpoStability) * 0.10;
            double volumeBoost = Math.Log10(1.0 + ppoNormalRunsPerPrestige) * 0.9;
            double prestigeBoost = Math.Log10(2.0 + prestige) * 0.5;
            int highExponent = Mathf.Clamp(10 + (int)Math.Floor(research + volumeBoost + prestigeBoost + rng.NextDouble() * 1.2), 9, 16);
            int high = 1 << highExponent;
            long score = Math.Max(2000, (long)Math.Round(high * (2.4 + rng.NextDouble() * 1.2)));
            int moves = Mathf.Clamp(150 + highExponent * 30 + (int)Math.Round(rng.NextDouble() * 60.0), 120, 1200);
            int merges = Mathf.Clamp(moves - 50, 80, moves);
            return (score, high, moves, merges);
        }

        // ---------------------------------------------------------------------------------------
        // Auto-buyers (all honor the audit's banned-key set)
        // ---------------------------------------------------------------------------------------

        private string AutoBuy(
            StackMergeProgression progression,
            SimState sim,
            int run,
            SimRandom rng,
            HashSet<string> banned,
            ConcurrentDictionary<string, SimulationForkSnapshot> forkSnapshots,
            string forkSnapshotPrefix)
        {
            var boughtThisRun = new List<string>();

            // Token maintenance first: Auto Restart is worthless without tokens, and the pack is a
            // repeatable purchase the cheapest-first loop must not see (it would buy packs forever).
            if (progression.AutoRestartUnlocked && !progression.AutoRestartIsTokenFree && !banned.Contains("TokenPack"))
            {
                int packGuard = 0;
                while (progression.Tokens < TokenReserve
                       && progression.Chips >= progression.GetTokenPackCost() * 4
                       && packGuard++ < 10)
                {
                    long costPaid = progression.GetTokenPackCost();
                    ImpactSnapshot before = CaptureImpactSnapshot(progression);
                    CaptureForkSnapshot(forkSnapshots, forkSnapshotPrefix, "TokenPack", progression, sim, rng, run);
                    if (!progression.BuyTokenPack())
                    {
                        break;
                    }

                    ImpactSnapshot after = CaptureImpactSnapshot(progression);
                    sim.RecordPurchase(run, sim.Clock, "TokenPack", "Token Pack", costPaid, false, progression.Chips, progression.TotalChipsEarned, before, after);
                    boughtThisRun.Add("Token Pack");
                }
            }

            int guard = 0;
            while (guard++ < 300)
            {
                List<Candidate> candidates = GetCandidates(progression, banned);
                Candidate? cheapest = null;
                foreach (Candidate candidate in candidates)
                {
                    if (candidate.Cost <= progression.Chips && (cheapest == null || candidate.Cost < cheapest.Value.Cost))
                    {
                        cheapest = candidate;
                    }
                }

                if (cheapest == null)
                {
                    break;
                }

                long costPaid = cheapest.Value.Cost;
                ImpactSnapshot before = CaptureImpactSnapshot(progression);
                CaptureForkSnapshot(forkSnapshots, forkSnapshotPrefix, cheapest.Value.Key, progression, sim, rng, run);
                if (!cheapest.Value.Buy())
                {
                    break;
                }

                ImpactSnapshot after = CaptureImpactSnapshot(progression);
                sim.RecordPurchase(run, sim.Clock, cheapest.Value.Key, cheapest.Value.Label, costPaid, false, progression.Chips, progression.TotalChipsEarned, before, after);
                boughtThisRun.Add(cheapest.Value.Label);
            }

            return string.Join(", ", boughtThisRun);
        }

        /// <summary>Keeps the best hired agents equipped (a real player swaps to the strongest loadout).</summary>
        private static void ApplyAgentLoadout(StackMergeProgression progression)
        {
            if (!progression.AgentsMenuUnlocked)
            {
                return;
            }

            var desired = new List<AgentId>();
            foreach (AgentId agentId in AgentPriority)
            {
                if (progression.IsAgentUnlocked(agentId) && desired.Count < progression.ActiveAgentSlots)
                {
                    desired.Add(agentId);
                }
            }

            foreach (AgentDefinition definition in StackMergeProgression.Agents)
            {
                if (progression.IsAgentActive(definition.Id) && !desired.Contains(definition.Id))
                {
                    progression.UnequipAgent(definition.Id);
                }
            }

            foreach (AgentId agentId in desired)
            {
                if (!progression.IsAgentActive(agentId))
                {
                    progression.EquipAgent(agentId);
                }
            }
        }

        /// <summary>
        /// Allocation strategy of the simulated player: while PPO Training still needs frames,
        /// route most compute at it; otherwise split between Insight and chip income.
        /// </summary>
        private static void ApplyDatacenterPolicy(StackMergeProgression p)
        {
            if (p.DatacenterTrainingHasTarget)
            {
                p.SetDatacenterAllocation(DatacenterAllocationId.TrainingCluster, 0f);
                p.SetDatacenterAllocation(DatacenterAllocationId.AnalysisNode, 0.15f);
                p.SetDatacenterAllocation(DatacenterAllocationId.MarketBots, 0.15f);
                p.SetDatacenterAllocation(DatacenterAllocationId.TrainingCluster, 0.70f);
            }
            else
            {
                p.SetDatacenterAllocation(DatacenterAllocationId.TrainingCluster, 0f);
                p.SetDatacenterAllocation(DatacenterAllocationId.AnalysisNode, 0.50f);
                p.SetDatacenterAllocation(DatacenterAllocationId.MarketBots, 0.50f);
            }
        }

        /// <summary>
        /// Invests in the datacenter continuously: buys the cheapest rack/facility whenever it costs
        /// at most a third of the current chips, leaving the rest for cycle progression.
        /// </summary>
        private string AutoBuyDatacenter(
            StackMergeProgression p,
            SimState sim,
            int run,
            SimRandom rng,
            HashSet<string> banned,
            ConcurrentDictionary<string, SimulationForkSnapshot> forkSnapshots,
            string forkSnapshotPrefix)
        {
            if (!p.DatacenterUnlocked)
            {
                return string.Empty;
            }

            var boughtThisRun = new List<string>();
            int guard = 0;
            while (guard++ < 40)
            {
                string bestKey = null;
                string bestLabel = null;
                long bestCost = 0;
                Func<bool> bestBuy = null;
                foreach (DatacenterRackDefinition rack in StackMergeProgression.DatacenterRacks)
                {
                    DatacenterRackId rackId = rack.Id;
                    string key = $"Rack.{rack.DisplayName}";
                    if (banned.Contains(key))
                    {
                        continue;
                    }

                    long cost = p.GetDatacenterRackCost(rackId);
                    if (bestBuy == null || cost < bestCost)
                    {
                        bestKey = key;
                        bestLabel = $"Rack {rack.DisplayName} #{p.GetDatacenterRackCount(rackId) + 1}";
                        bestCost = cost;
                        bestBuy = () => p.BuyDatacenterRack(rackId);
                    }
                }

                foreach (DatacenterFacilityDefinition facility in StackMergeProgression.DatacenterFacilities)
                {
                    DatacenterFacilityId facilityId = facility.Id;
                    string key = $"Facility.{facility.DisplayName}";
                    if (banned.Contains(key) || p.IsDatacenterFacilityMaxed(facilityId))
                    {
                        continue;
                    }

                    long cost = p.GetDatacenterFacilityCost(facilityId);
                    if (bestBuy == null || cost < bestCost)
                    {
                        bestKey = key;
                        bestLabel = $"Facility {facility.DisplayName} L{p.GetDatacenterFacilityLevel(facilityId) + 1}";
                        bestCost = cost;
                        bestBuy = () => p.BuyDatacenterFacility(facilityId);
                    }
                }

                if (bestBuy == null || p.Chips < bestCost * 3)
                {
                    break;
                }

                ImpactSnapshot before = CaptureImpactSnapshot(p);
                CaptureForkSnapshot(forkSnapshots, forkSnapshotPrefix, bestKey, p, sim, rng, run);
                if (!bestBuy())
                {
                    break;
                }

                ImpactSnapshot after = CaptureImpactSnapshot(p);
                sim.RecordPurchase(run, sim.Clock, bestKey, bestLabel, bestCost, false, p.Chips, p.TotalChipsEarned, before, after);
                boughtThisRun.Add(bestLabel);
            }

            return string.Join(", ", boughtThisRun);
        }

        private string AutoBuyResearch(
            StackMergeProgression progression,
            SimState sim,
            int run,
            SimRandom rng,
            HashSet<string> banned,
            ConcurrentDictionary<string, SimulationForkSnapshot> forkSnapshots,
            string forkSnapshotPrefix)
        {
            var boughtThisRun = new List<string>();
            int guard = 0;
            while (guard++ < 200)
            {
                List<Candidate> candidates = GetResearchCandidates(progression, banned);
                Candidate? cheapest = null;
                foreach (Candidate candidate in candidates)
                {
                    if (candidate.Cost <= progression.ResearchInsight && (cheapest == null || candidate.Cost < cheapest.Value.Cost))
                    {
                        cheapest = candidate;
                    }
                }

                if (cheapest == null)
                {
                    break;
                }

                long costPaid = cheapest.Value.Cost;
                ImpactSnapshot before = CaptureImpactSnapshot(progression);
                CaptureForkSnapshot(forkSnapshots, forkSnapshotPrefix, cheapest.Value.Key, progression, sim, rng, run);
                if (!cheapest.Value.Buy())
                {
                    break;
                }

                ImpactSnapshot after = CaptureImpactSnapshot(progression);
                sim.RecordPurchase(run, sim.Clock, cheapest.Value.Key, cheapest.Value.Label, costPaid, true, progression.ResearchInsight, progression.TotalChipsEarned, before, after);
                boughtThisRun.Add(cheapest.Value.Label);
            }

            return string.Join(", ", boughtThisRun);
        }

        private static List<Candidate> GetResearchCandidates(StackMergeProgression p, HashSet<string> banned)
        {
            var list = new List<Candidate>();
            if (p.PrestigeCount <= 0)
            {
                return list;
            }

            foreach (ResearchDefinition definition in StackMergeProgression.Research)
            {
                ResearchId researchId = definition.Id;
                string key = $"Research.{definition.DisplayName}";
                if (banned.Contains(key) || p.IsResearchMaxed(researchId))
                {
                    continue;
                }

                string reason = p.GetResearchUnavailableReason(researchId);
                if (!string.IsNullOrEmpty(reason) && reason != "Not enough Insight.")
                {
                    continue;
                }

                int nextLevel = p.GetResearchLevel(researchId) + 1;
                list.Add(new Candidate(key, $"Research {definition.DisplayName} L{nextLevel}", p.GetResearchCost(researchId), () => p.BuyResearch(researchId)));
            }

            return list;
        }

        private static List<Candidate> GetCandidates(StackMergeProgression p, HashSet<string> banned)
        {
            var list = new List<Candidate>();

            void Add(string key, string label, long cost, Func<bool> buy)
            {
                if (!banned.Contains(key))
                {
                    list.Add(new Candidate(key, label, cost, buy));
                }
            }

            // Solver unlocks (non-PPO).
            foreach (SolverDefinition definition in StackMergeSolverCatalog.Definitions)
            {
                SolverId id = definition.Id;
                if (!definition.Available || id == SolverId.MachineLearning || p.IsSolverUnlocked(id))
                {
                    continue;
                }

                Add($"Solver.{definition.DisplayName}", $"Solver {definition.DisplayName}", p.GetSolverUnlockCost(id), () => p.SelectOrUnlockSolver(id));
            }

            // PPO — only once every modifier is maxed.
            if (p.CanUnlockMachineLearning && !p.IsSolverUnlocked(SolverId.MachineLearning))
            {
                Add("Solver.PPO", "Solver PPO", p.GetSolverUnlockCost(SolverId.MachineLearning), () => p.SelectOrUnlockSolver(SolverId.MachineLearning));
            }

            if (!p.AutoSolveUnlocked && p.HasPurchasedSolver)
            {
                Add("Unlock.AutoSolve", "Auto Solve", p.GetAutoSolveCost(), p.ToggleOrBuyAutoSolve);
            }

            if (!p.AutoRestartUnlocked)
            {
                Add("Unlock.AutoRestart", "Auto Restart", p.GetAutoRestartCost(), p.ToggleOrBuyAutoRestart);
            }

            if (!p.SolverTuningUnlocked)
            {
                Add("Unlock.SolverTuning", "Solver Tuning", p.GetSolverTuningUnlockCost(), p.BuySolverTuningUnlock);
            }

            if (!p.AgentsMenuUnlocked)
            {
                Add("Unlock.AgentsMenu", "Agents Menu", p.GetAgentsMenuUnlockCost(), p.BuyAgentsMenuUnlock);
            }

            if (p.AgentsMenuUnlocked && !p.ExtraAgentSlotUnlocked)
            {
                Add("Unlock.ExtraAgentSlot", "Extra Agent Slot", p.GetExtraAgentSlotUpgradeCost(), p.BuyExtraAgentSlotUpgrade);
            }

            if (p.CanUnlockModifiersMenu && !p.ModifiersMenuUnlocked)
            {
                Add("Unlock.ModifiersMenu", "Modifiers Menu", p.GetModifiersMenuUnlockCost(), p.BuyModifiersMenuUnlock);
            }

            if (!p.IsMaxSpeed)
            {
                Add("Upgrade.Speed", $"Speed L{p.SpeedLevel + 1}", p.GetSpeedUpgradeCost(), p.BuySpeedUpgrade);
            }

            if (!p.IsMaxComputeSpeed)
            {
                Add("Upgrade.ComputeSpeed", $"Compute Speed L{p.ComputeSpeedLevel + 1}", p.GetComputeSpeedUpgradeCost(), p.BuyComputeSpeedUpgrade);
            }

            if (!p.IsMaxStackCapacity)
            {
                Add("Upgrade.StackCapacity", $"Stack Cap L{p.StackCapacityLevel + 1}", p.GetStackCapacityUpgradeCost(), p.BuyStackCapacityUpgrade);
            }

            if (!p.IsMaxQueuePreview)
            {
                Add("Upgrade.QueuePreview", "Queue Preview", p.GetQueuePreviewUpgradeCost(), p.BuyQueuePreviewUpgrade);
            }

            if (!p.IsMaxIncome)
            {
                Add("Upgrade.ChipYield", "Chip Yield", p.GetIncomeUpgradeCost(), p.BuyIncomeUpgrade);
            }

            if (!p.IsMaxDifficulty)
            {
                Add("Upgrade.Difficulty", $"Difficulty L{p.DifficultyLevel + 1}", p.GetDifficultyUpgradeCost(), p.BuyDifficultyUpgrade);
            }

            if (!p.IsMaxScalingFrequency && p.ScalingFrequencyPurchasable)
            {
                Add("Upgrade.ScalingFrequency", $"Scaling Frequency L{p.ScalingFrequencyLevel + 1}", p.GetScalingFrequencyUpgradeCost(), p.BuyScalingFrequencyUpgrade);
            }

            if (!p.IsMaxProfitableEnding)
            {
                Add("Upgrade.ProfitableEnding", $"Profitable Ending L{p.ProfitableEndingLevel + 1}", p.GetProfitableEndingUpgradeCost(), p.BuyProfitableEndingUpgrade);
            }

            if (!p.IsMaxPassiveYield)
            {
                Add("Upgrade.PassiveYield", $"Passive Yield L{p.PassiveYieldLevel + 1}", p.GetPassiveYieldUpgradeCost(), p.BuyPassiveYieldUpgrade);
            }

            if (!p.IsMaxPassiveTickRate && p.PassiveSupportUpgradesUnlocked)
            {
                Add("Upgrade.PassiveTickRate", $"Passive Tick Rate L{p.PassiveTickRateLevel + 1}", p.GetPassiveTickRateUpgradeCost(), p.BuyPassiveTickRateUpgrade);
            }

            if (!p.IsMaxActiveMultiplier && p.PassiveSupportUpgradesUnlocked)
            {
                Add("Upgrade.ActiveMultiplier", $"Active Multiplier L{p.ActiveMultiplierLevel + 1}", p.GetActiveMultiplierUpgradeCost(), p.BuyActiveMultiplierUpgrade);
            }

            if (!p.IsMaxComboEngine)
            {
                Add("Upgrade.ComboEngine", $"Combo Engine L{p.ComboEngineLevel + 1}", p.GetComboEngineUpgradeCost(), p.BuyComboEngineUpgrade);
            }

            if (!p.IsMaxSalvageProtocol)
            {
                Add("Upgrade.SalvageProtocol", $"Salvage Protocol L{p.SalvageProtocolLevel + 1}", p.GetSalvageProtocolUpgradeCost(), p.BuySalvageProtocolUpgrade);
            }

            if (!p.IsMaxTokenDividend)
            {
                Add("Upgrade.TokenDividend", $"Token Dividend L{p.TokenDividendLevel + 1}", p.GetTokenDividendUpgradeCost(), p.BuyTokenDividendUpgrade);
            }

            if (p.AgentsMenuUnlocked)
            {
                foreach (AgentDefinition agent in StackMergeProgression.Agents)
                {
                    if (!p.IsAgentUnlocked(agent.Id))
                    {
                        AgentId agentId = agent.Id;
                        Add($"Agent.{agent.DisplayName}", $"Agent {agent.DisplayName}", p.GetAgentCost(agentId), () => p.BuyOrToggleAgent(agentId));
                    }
                }
            }

            if (p.ModifiersMenuUnlocked)
            {
                foreach (ModifierDefinition modifier in StackMergeProgression.Modifiers)
                {
                    if (!p.IsModifierMaxed(modifier.Id))
                    {
                        ModifierId modifierId = modifier.Id;
                        Add($"Modifier.{modifier.DisplayName}", $"Modifier {modifier.DisplayName} L{p.GetModifierLevel(modifierId) + 1}", p.GetModifierUpgradeCost(modifierId), () => p.BuyModifierUpgrade(modifierId));
                    }
                }
            }

            return list;
        }

        // ---------------------------------------------------------------------------------------
        // Balance audit (ablation)
        // ---------------------------------------------------------------------------------------

        private void RunBalanceAudit()
        {
            StackMergeProgression.DebugUnlockDatacenter = false;
            auditCancelRequested = false;

            int[] seeds = Enumerable.Range(0, auditSeeds).Select(i => seed + i * 7919).ToArray();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var jobs = new List<AuditJob>();
            var skippedShop = new List<string>();
            var skippedMeta = new List<string>();
            ConcurrentDictionary<string, CachedRunResult> runCache = auditRunReplayCache ? new ConcurrentDictionary<string, CachedRunResult>() : null;
            var runCacheStats = auditRunReplayCache ? new RunReplayCacheStats() : null;
            ConcurrentDictionary<string, CachedRunResult> representativeRunCache = auditRepresentativeRunCache ? new ConcurrentDictionary<string, CachedRunResult>() : null;
            var representativeRunCacheStats = auditRepresentativeRunCache ? new RunReplayCacheStats() : null;
            var forkSnapshots = new ConcurrentDictionary<string, SimulationForkSnapshot>();
            bool batchMode = Application.isBatchMode;

            try
            {
                // --- Phase 1: baselines run FIRST, sequentially and uncontended. If the baseline
                // can't even reach its horizon, every variant comparison would be garbage — abort
                // with a clear message instead of burning 20 minutes.
                AuditJob shopBaseline = RunAuditBaseline(AuditScope.Shop, seeds, stopwatch, runCache, runCacheStats, representativeRunCache, representativeRunCacheStats, forkSnapshots);
                if (shopBaseline == null)
                {
                    return; // cancelled — summary already set
                }

                jobs.Add(shopBaseline);
                if (!shopBaseline.AllFinished(1))
                {
                    SimOutcome worst = shopBaseline.Outcomes.FirstOrDefault(o => o != null);
                    summary = "BALANCE AUDIT ABORTED — the baseline itself did not reach the first prestige.\n"
                        + $"It stopped after {worst?.Runs ?? 0} runs / {FormatTime(worst?.Sim.Clock ?? 0)} sim time (limits: {maxRuns} runs, {wallClockLimitSeconds}s wall).\n"
                        + "Raise 'Max runs' and/or 'Editor time limit (s)' until a normal 'Run simulation' with these settings reaches Prestige #1, then re-run the audit.";
                    return;
                }

                AuditJob metaBaseline = null;
                if (auditMeta)
                {
                    metaBaseline = RunAuditBaseline(AuditScope.Meta, seeds, stopwatch, runCache, runCacheStats, representativeRunCache, representativeRunCacheStats, forkSnapshots);
                    if (metaBaseline == null)
                    {
                        return;
                    }

                    jobs.Add(metaBaseline);
                    if (!metaBaseline.AllFinished(metaAuditPrestiges))
                    {
                        summary = $"BALANCE AUDIT ABORTED — the meta baseline did not reach {metaAuditPrestiges} prestiges within the limits. Raise 'Max runs'/'Editor time limit (s)' or lower the meta horizon.";
                        return;
                    }
                }

                // --- Phase 2: variant list. Items the baseline never bought are skipped: with the
                // same seed, banning an unbought item provably changes nothing (Δ = 0 exactly) —
                // and "never bought" is itself a balance finding worth reporting.
                foreach (string key in BuildShopAuditKeys())
                {
                    if (!MatchesAuditKeyFilter(key))
                    {
                        continue;
                    }

                    if (shopBaseline.SpendOn(key, false) > 0)
                    {
                        jobs.Add(new AuditJob { Key = key, Scope = AuditScope.Shop, Outcomes = new SimOutcome[seeds.Length] });
                    }
                    else
                    {
                        skippedShop.Add(key);
                    }
                }

                if (auditMeta && metaBaseline != null)
                {
                    foreach (string key in BuildMetaAuditKeys())
                    {
                        if (!MatchesAuditKeyFilter(key))
                        {
                            continue;
                        }

                        bool insightCurrency = key.StartsWith("Research.", StringComparison.Ordinal);
                        if (metaBaseline.SpendOn(key, insightCurrency) > 0)
                        {
                            jobs.Add(new AuditJob { Key = key, Scope = AuditScope.Meta, Outcomes = new SimOutcome[seeds.Length] });
                        }
                        else
                        {
                            skippedMeta.Add(key);
                        }
                    }
                }

                // Variants are capped by RUNS relative to the baseline (wall-clock is unreliable
                // under thread contention): 2× the baseline's runs is enough to detect any sane
                // slowdown; beyond that the item is effectively load-bearing.
                int shopRunCap = Mathf.CeilToInt(shopBaseline.MaxRuns() * auditVariantRunCapMultiplier) + 100;
                int metaRunCap = metaBaseline != null ? Mathf.CeilToInt(metaBaseline.MaxRuns() * auditVariantRunCapMultiplier) + 100 : maxRuns;
                int variantWallLimit = Math.Max(wallClockLimitSeconds * 4, 2400);

                var workItems = new List<(AuditJob Job, int SeedIndex)>();
                foreach (AuditJob job in jobs)
                {
                    if (job.Key == BaselineKey)
                    {
                        continue;
                    }

                    for (int i = 0; i < seeds.Length; i++)
                    {
                        workItems.Add((job, i));
                    }
                }

                int total = workItems.Count;
                int completed = 0;
                double nextBatchProgressLog = 0;
                var batchMessages = new ConcurrentQueue<string>();

                Task worker = Task.Run(() =>
                {
                    Parallel.ForEach(workItems, new ParallelOptions { MaxDegreeOfParallelism = auditThreads }, item =>
                    {
                        var config = new SimCoreConfig
                        {
                            Seed = seeds[item.SeedIndex],
                            MaxRuns = item.Job.Scope == AuditScope.Shop ? shopRunCap : metaRunCap,
                            StopAtPpo = false,
                            SimulateResearch = true,
                            TargetPrestiges = item.Job.Scope == AuditScope.Shop ? 1 : metaAuditPrestiges,
                            WallClockLimitSeconds = variantWallLimit,
                            BannedKeys = new HashSet<string> { item.Job.Key },
                            CollectDetails = false,
                            RunCache = runCache,
                            RunCacheStats = runCacheStats,
                            RepresentativeRunCache = representativeRunCache,
                            RepresentativeRunCacheStats = representativeRunCacheStats,
                            UseRepresentativeRunCache = auditRepresentativeRunCache,
                            StartSnapshot = forkSnapshots.TryGetValue(MakeForkSnapshotKey(item.Job.Scope, item.SeedIndex, item.Job.Key), out SimulationForkSnapshot snapshot)
                                ? snapshot
                                : null
                        };
                        item.Job.Outcomes[item.SeedIndex] = RunSimulationCore(config);
                        int done = Interlocked.Increment(ref completed);
                        if (batchMode)
                        {
                            SimOutcome outcome = item.Job.Outcomes[item.SeedIndex];
                            batchMessages.Enqueue($"Balance audit variant done {done}/{total}: {item.Job.Key} seed {item.SeedIndex + 1}, runs {outcome?.Runs ?? 0}, sim {FormatTime(outcome?.Sim.Clock ?? 0)}, stop {GetStopReason(outcome)}");
                        }
                    });
                });

                while (!worker.IsCompleted)
                {
                    float progress = total > 0 ? completed / (float)total : 1f;
                    if (batchMode)
                    {
                        while (batchMessages.TryDequeue(out string message))
                        {
                            Debug.Log(message);
                        }
                    }

                    if (batchMode && stopwatch.Elapsed.TotalSeconds >= nextBatchProgressLog)
                    {
                        Debug.Log($"Balance audit progress: variants {completed}/{total}, elapsed {stopwatch.Elapsed.TotalSeconds:0}s, cache {runCacheStats?.Hits ?? 0:N0}/{runCacheStats?.Misses ?? 0:N0}");
                        nextBatchProgressLog = stopwatch.Elapsed.TotalSeconds + 10.0;
                    }

                    if (!batchMode && EditorUtility.DisplayCancelableProgressBar("Balance audit", $"Variants {completed}/{total}  ({stopwatch.Elapsed.TotalSeconds:0}s, {auditThreads} threads)", progress))
                    {
                        auditCancelRequested = true;
                    }

                    Thread.Sleep(120);
                }

                worker.Wait();
                if (batchMode)
                {
                    while (batchMessages.TryDequeue(out string message))
                    {
                        Debug.Log(message);
                    }
                }
            }
            catch (AggregateException exception)
            {
                summary = $"Balance audit failed: {exception.InnerException?.Message ?? exception.Message}";
                return;
            }
            finally
            {
                if (!batchMode)
                {
                    EditorUtility.ClearProgressBar();
                    Repaint();
                }
            }

            summary = BuildAuditReport(jobs, seeds, stopwatch.Elapsed.TotalSeconds, skippedShop, skippedMeta, runCache, runCacheStats, representativeRunCache, representativeRunCacheStats, forkSnapshots);
            lastPath = WriteAuditFile(jobs, seeds);
        }

        /// <summary>Runs the baseline for one scope sequentially with progress UI. Null = cancelled.</summary>
        private AuditJob RunAuditBaseline(
            AuditScope scope,
            int[] seeds,
            System.Diagnostics.Stopwatch stopwatch,
            ConcurrentDictionary<string, CachedRunResult> runCache,
            RunReplayCacheStats runCacheStats,
            ConcurrentDictionary<string, CachedRunResult> representativeRunCache,
            RunReplayCacheStats representativeRunCacheStats,
            ConcurrentDictionary<string, SimulationForkSnapshot> forkSnapshots)
        {
            var baseline = new AuditJob { Key = BaselineKey, Scope = scope, Outcomes = new SimOutcome[seeds.Length] };
            for (int i = 0; i < seeds.Length; i++)
            {
                if (Application.isBatchMode)
                {
                    Debug.Log($"Balance audit baseline start: {scope} seed {i + 1}/{seeds.Length}");
                }

                if (!Application.isBatchMode && EditorUtility.DisplayCancelableProgressBar("Balance audit", $"Baseline ({scope}) seed {i + 1}/{seeds.Length}  ({stopwatch.Elapsed.TotalSeconds:0}s)", 0f))
                {
                    auditCancelRequested = true;
                }

                if (auditCancelRequested)
                {
                    summary = "Balance audit cancelled.";
                    return null;
                }

                baseline.Outcomes[i] = RunSimulationCore(new SimCoreConfig
                {
                    Seed = seeds[i],
                    MaxRuns = maxRuns,
                    StopAtPpo = false,
                    SimulateResearch = true,
                    TargetPrestiges = scope == AuditScope.Shop ? 1 : metaAuditPrestiges,
                    WallClockLimitSeconds = wallClockLimitSeconds,
                    BannedKeys = null,
                    CollectDetails = false,
                    RunCache = runCache,
                    RunCacheStats = runCacheStats,
                    RepresentativeRunCache = representativeRunCache,
                    RepresentativeRunCacheStats = representativeRunCacheStats,
                    ForkSnapshots = forkSnapshots,
                    ForkSnapshotPrefix = MakeForkSnapshotPrefix(scope, i)
                });
                if (Application.isBatchMode)
                {
                    SimOutcome outcome = baseline.Outcomes[i];
                    Debug.Log($"Balance audit baseline done: {scope} seed {i + 1}/{seeds.Length}, runs {outcome?.Runs ?? 0}, sim {FormatTime(outcome?.Sim.Clock ?? 0)}, stop {GetStopReason(outcome)}");
                }
            }

            return baseline;
        }

        private List<string> BuildShopAuditKeys()
        {
            var keys = new List<string>();
            if (auditSolvers)
            {
                foreach (SolverDefinition definition in StackMergeSolverCatalog.Definitions)
                {
                    if (definition.Id != SolverId.MachineLearning)
                    {
                        keys.Add($"Solver.{definition.DisplayName}");
                    }
                }
            }

            if (auditUnlocks)
            {
                keys.AddRange(new[]
                {
                    "Unlock.AutoSolve", "Unlock.AutoRestart", "Unlock.SolverTuning",
                    "Unlock.AgentsMenu", "Unlock.ExtraAgentSlot", "Unlock.ModifiersMenu"
                });
            }

            if (auditUpgrades)
            {
                keys.AddRange(new[]
                {
                    "Upgrade.Speed", "Upgrade.ComputeSpeed", "Upgrade.StackCapacity", "Upgrade.QueuePreview",
                    "Upgrade.ChipYield", "Upgrade.Difficulty", "Upgrade.ScalingFrequency", "Upgrade.ProfitableEnding",
                    "Upgrade.PassiveYield", "Upgrade.PassiveTickRate", "Upgrade.ActiveMultiplier",
                    "Upgrade.ComboEngine", "Upgrade.SalvageProtocol", "Upgrade.TokenDividend"
                });
            }

            if (auditTokenPack)
            {
                keys.Add("TokenPack");
            }

            if (auditAgents)
            {
                keys.AddRange(StackMergeProgression.Agents.Select(agent => $"Agent.{agent.DisplayName}"));
            }

            if (auditModifiers)
            {
                keys.AddRange(StackMergeProgression.Modifiers.Select(modifier => $"Modifier.{modifier.DisplayName}"));
            }

            return keys;
        }

        private bool MatchesAuditKeyFilter(string key)
        {
            string filter = auditKeyFilter?.Trim();
            if (string.IsNullOrEmpty(filter))
            {
                return true;
            }

            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            string[] parts = filter.Split(new[] { '|', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts)
            {
                if (key.IndexOf(part.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> BuildMetaAuditKeys()
        {
            var keys = new List<string>();
            keys.AddRange(StackMergeProgression.Research.Select(research => $"Research.{research.DisplayName}"));
            keys.AddRange(StackMergeProgression.DatacenterRacks.Select(rack => $"Rack.{rack.DisplayName}"));
            keys.AddRange(StackMergeProgression.DatacenterFacilities.Select(facility => $"Facility.{facility.DisplayName}"));
            return keys;
        }

        // ---------------------------------------------------------------------------------------
        // Fast impact lab (single-baseline observational balance view)
        // ---------------------------------------------------------------------------------------

        private void RunFastImpactLab()
        {
            StackMergeProgression.DebugUnlockDatacenter = false;
            auditCancelRequested = false;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            ConcurrentDictionary<string, CachedRunResult> runCache = auditRunReplayCache ? new ConcurrentDictionary<string, CachedRunResult>() : null;
            var runCacheStats = auditRunReplayCache ? new RunReplayCacheStats() : null;
            var outcome = RunSimulationCore(new SimCoreConfig
            {
                Seed = seed,
                MaxRuns = maxRuns,
                StopAtPpo = false,
                SimulateResearch = true,
                TargetPrestiges = fastImpactPrestigeHorizon,
                WallClockLimitSeconds = wallClockLimitSeconds,
                BannedKeys = null,
                CollectDetails = false,
                CollectRunHistory = true,
                RunCache = runCache,
                RunCacheStats = runCacheStats,
                RepresentativeRunCache = null,
                RepresentativeRunCacheStats = null,
                UseRepresentativeRunCache = false
            });

            List<FastImpactRow> rows = BuildFastImpactRows(outcome);
            summary = BuildFastImpactReport(outcome, rows, stopwatch.Elapsed.TotalSeconds, runCache, runCacheStats);
            lastPath = WriteFastImpactFile(outcome, rows);
            Repaint();
        }

        private List<FastImpactRow> BuildFastImpactRows(SimOutcome outcome)
        {
            var rows = new List<FastImpactRow>();
            if (outcome?.Sim?.Purchases == null || outcome.RunSamples.Count == 0)
            {
                return rows;
            }

            IEnumerable<IGrouping<string, PurchaseRecord>> groups = outcome.Sim.Purchases
                .Where(record => record.Cost > 0
                    && !string.IsNullOrEmpty(record.Key)
                    && MatchesAuditKeyFilter(record.Key))
                .GroupBy(record => record.Key);

            foreach (IGrouping<string, PurchaseRecord> group in groups)
            {
                List<PurchaseRecord> purchases = group.OrderBy(record => record.SimSeconds).ThenBy(record => record.Run).ToList();
                List<PurchaseImpactRecord> impacts = outcome.Sim.PurchaseImpacts
                    .Where(impact => impact.Purchase.Key == group.Key)
                    .ToList();
                PurchaseRecord first = purchases[0];
                bool insightCurrency = purchases.Any(record => record.Currency.Equals("Insight", StringComparison.OrdinalIgnoreCase));
                long spent = purchases.Sum(record => Math.Max(0, record.Cost));
                DirectImpact direct = group.Key == "TokenPack"
                    ? CalculateTokenPackDirectImpact(impacts)
                    : CalculateDirectImpact(impacts);

                RunWindowStats before = CalculateWindowStats(outcome.RunSamples
                    .Where(sample => sample.Run < first.Run)
                    .OrderByDescending(sample => sample.Run)
                    .Take(fastImpactWindowRuns)
                    .OrderBy(sample => sample.Run));
                RunWindowStats after = CalculateWindowStats(outcome.RunSamples
                    .Where(sample => sample.Run > first.Run)
                    .OrderBy(sample => sample.Run)
                    .Take(fastImpactWindowRuns));

                double incomeDeltaPct = PercentDelta(after.ChipsPerSecond, before.ChipsPerSecond);
                double scoreDeltaPct = PercentDelta(after.ScorePerRun, before.ScorePerRun);
                double throughputDeltaPct = PercentDelta(after.RunsPerMinute, before.RunsPerMinute);
                double highMultiplier = before.MaxHigh > 0 ? after.MaxHigh / (double)before.MaxHigh : (after.MaxHigh > 0 ? after.MaxHigh : 0);
                double? cycleSpeedupPct = CalculateCycleSpeedup(outcome.Sim.Cycles, first.SimSeconds);
                double coveragePct = outcome.Sim.Clock > 1 ? Math.Max(0, outcome.Sim.Clock - first.SimSeconds) / outcome.Sim.Clock * 100.0 : 0;
                double payback = 0;
                double incomeDeltaPerSecond = after.ChipsPerSecond - before.ChipsPerSecond;
                if (!insightCurrency && spent > 0 && incomeDeltaPerSecond > 0)
                {
                    payback = incomeDeltaPerSecond * Math.Max(0, outcome.Sim.Clock - first.SimSeconds) / spent;
                }

                var row = new FastImpactRow
                {
                    Key = group.Key,
                    Role = GetImpactRole(group.Key),
                    PurchaseCount = purchases.Count,
                    Spent = spent,
                    Currency = insightCurrency ? "Insight" : "chips",
                    FirstRun = first.Run,
                    FirstSeconds = first.SimSeconds,
                    CoveragePercent = coveragePct,
                    Before = before,
                    After = after,
                    DirectDeltaPercent = direct.DeltaPercent,
                    DirectDriver = direct.Driver,
                    IncomeDeltaPercent = incomeDeltaPct,
                    ScoreDeltaPercent = scoreDeltaPct,
                    ThroughputDeltaPercent = throughputDeltaPct,
                    HighMultiplier = highMultiplier,
                    CycleSpeedupPercent = cycleSpeedupPct,
                    EstimatedPayback = payback
                };
                row.Verdict = GetFastImpactVerdict(row);
                rows.Add(row);
            }

            return rows
                .OrderByDescending(row => row.SortScore)
                .ThenBy(row => row.FirstSeconds)
                .ToList();
        }

        private string BuildFastImpactReport(
            SimOutcome outcome,
            List<FastImpactRow> rows,
            double wallSeconds,
            ConcurrentDictionary<string, CachedRunResult> runCache,
            RunReplayCacheStats runCacheStats)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== FAST IMPACT LAB ===  ({rows.Count} purchased item groups, wall {wallSeconds:0}s)");
            sb.AppendLine("Single-baseline observational model: it does NOT replay the game per item. It compares local before/after windows around each purchase, then adds cycle-speedup signals for meta effects.");
            sb.AppendLine("Use it for balance direction: strong / weak / late / gate. Keep exact ablation only for suspicious edge cases.");
            sb.AppendLine($"Horizon: {fastImpactPrestigeHorizon} prestige(s), seed {seed}, window {fastImpactWindowRuns} runs before/after.");
            sb.AppendLine($"Baseline reached: {FormatTime(outcome.Sim.Clock)} ({outcome.Runs:N0} runs, {outcome.Progression.PrestigeCount} prestiges, stop {GetStopReason(outcome)}).");
            double? ppoSeconds = outcome.FirstPpoSeconds();
            if (ppoSeconds.HasValue)
            {
                sb.AppendLine($"PPO first unlocked: {FormatTime(ppoSeconds.Value)}.");
            }

            if (runCacheStats != null)
            {
                sb.AppendLine($"Run replay cache: {runCacheStats.Hits:N0} hits / {runCacheStats.Misses:N0} misses / {runCache?.Count ?? 0:N0} stored deterministic runs.");
            }

            sb.AppendLine();
            sb.AppendLine($"{"Item",-31} {"Role",-11} {"Spent",9} {"Buy#",5} {"First",9} {"Cover",7} {"Direct",8} {"Driver",-16} {"Income",8} {"Score",8} {"High",7} {"Cycle",8} {"Payback",8}  Verdict");
            foreach (FastImpactRow row in rows)
            {
                string spent = $"{FormatCompact(row.Spent)}{(row.Currency == "Insight" ? "i" : "")}";
                string first = FormatTime(row.FirstSeconds);
                string direct = FormatPercent(row.DirectDeltaPercent);
                string income = FormatPercent(row.IncomeDeltaPercent);
                string score = FormatPercent(row.ScoreDeltaPercent);
                string high = row.HighMultiplier > 0 ? $"{row.HighMultiplier:0.#}x" : "—";
                string cycle = row.CycleSpeedupPercent.HasValue ? FormatPercent(row.CycleSpeedupPercent.Value) : "—";
                string payback = row.EstimatedPayback > 0 ? $"{row.EstimatedPayback:0.0}x" : "—";
                sb.AppendLine($"{Truncate(row.Key, 31),-31} {row.Role,-11} {spent,9} {row.PurchaseCount,5} {first,9} {$"{row.CoveragePercent:0}%",7} {direct,8} {Truncate(row.DirectDriver, 16),-16} {income,8} {score,8} {high,7} {cycle,8} {payback,8}  {row.Verdict}");
            }

            if (rows.Count == 0)
            {
                sb.AppendLine("No purchased items matched the current filter.");
            }

            sb.AppendLine();
            sb.AppendLine("Columns: Direct is the purchase's immediate formula/snapshot delta. Income/Score/High are local after-vs-before windows. Cycle is completed-prestige-cycle speedup after the first purchase when available. Payback estimates extra chips over the remaining baseline horizon divided by chips spent.");
            return sb.ToString();
        }

        private string WriteFastImpactFile(SimOutcome outcome, List<FastImpactRow> rows)
        {
            try
            {
                string dir = Path.Combine(Application.dataPath, "..", "BenchmarkResults");
                Directory.CreateDirectory(dir);
                string path = Path.GetFullPath(Path.Combine(dir, $"fast_impact_{DateTime.Now:yyyyMMdd_HHmmss}.tsv"));

                var sb = new StringBuilder();
                sb.AppendLine("# Fast impact lab (single-baseline local before/after model)");
                sb.AppendLine($"# seed={seed} maxRuns={maxRuns} horizonPrestiges={fastImpactPrestigeHorizon} windowRuns={fastImpactWindowRuns} totalSeconds={outcome.Sim.Clock:0} runs={outcome.Runs}");
                sb.AppendLine("Key\tRole\tSpent\tCurrency\tPurchaseCount\tFirstRun\tFirstSeconds\tCoveragePercent\tDirectDeltaPercent\tDirectDriver\tBeforeChipsPerSecond\tAfterChipsPerSecond\tIncomeDeltaPercent\tBeforeScorePerRun\tAfterScorePerRun\tScoreDeltaPercent\tBeforeRunsPerMinute\tAfterRunsPerMinute\tThroughputDeltaPercent\tBeforeMaxHigh\tAfterMaxHigh\tHighMultiplier\tCycleSpeedupPercent\tEstimatedPayback\tVerdict");
                foreach (FastImpactRow row in rows)
                {
                    sb.AppendLine($"{row.Key}\t{row.Role}\t{row.Spent}\t{row.Currency}\t{row.PurchaseCount}\t{row.FirstRun}\t{row.FirstSeconds:0}\t{row.CoveragePercent:0.###}\t{row.DirectDeltaPercent:0.###}\t{row.DirectDriver}\t{row.Before.ChipsPerSecond:0.###}\t{row.After.ChipsPerSecond:0.###}\t{row.IncomeDeltaPercent:0.###}\t{row.Before.ScorePerRun:0.###}\t{row.After.ScorePerRun:0.###}\t{row.ScoreDeltaPercent:0.###}\t{row.Before.RunsPerMinute:0.###}\t{row.After.RunsPerMinute:0.###}\t{row.ThroughputDeltaPercent:0.###}\t{row.Before.MaxHigh}\t{row.After.MaxHigh}\t{row.HighMultiplier:0.###}\t{(row.CycleSpeedupPercent.HasValue ? row.CycleSpeedupPercent.Value.ToString("0.###") : "")}\t{row.EstimatedPayback:0.###}\t{row.Verdict}");
                }

                File.WriteAllText(path, sb.ToString());
                AssetDatabase.Refresh();
                return path;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Fast impact lab: failed to write TSV: {exception.Message}");
                return string.Empty;
            }
        }

        private ImpactSnapshot CaptureImpactSnapshot(StackMergeProgression progression)
        {
            bool auto = progression.AutoSolveEnabled && progression.HasPurchasedSolver;
            SolverId solver = auto ? BestIncomeSolver(progression) : manualProxy;
            double interval = Math.Max(0.001, auto ? progression.GetMoveInterval(solver) : manualSecondsPerMove);
            double throughput = 1.0 / interval;
            double passiveActiveMultiplier = 1.0 + StackMergeProgression.GetActiveMultiplierEffectPercent(progression.ActiveMultiplierLevel) / 100.0;
            double passiveRate = progression.PassiveYieldLevel > 0
                ? StackMergeProgression.GetPassiveYieldPerTick(progression.PassiveYieldLevel)
                    * passiveActiveMultiplier
                    / Math.Max(0.001, StackMergeProgression.GetPassiveTickInterval(progression.PassiveTickRateLevel))
                    * progression.CurrentGlobalIncomeMultiplier
                : 0.0;
            double boardPower = Math.Max(1.0, progression.StackCapacity)
                * (1.0 + progression.QueuePreviewLevel * 0.12)
                * (1.0 + progression.DifficultyLevel * 0.28)
                * (1.0 + progression.ScalingFrequencyLevel * 0.08);
            double modifierPower = 1.0;
            foreach (ModifierDefinition modifier in StackMergeProgression.Modifiers)
            {
                modifierPower *= 1.0 + progression.GetModifierLevel(modifier.Id) * 0.12;
            }

            double solverPower = EstimateSolverPower(solver);
            double agentPower = EstimateAgentPower(progression);
            double outputPower = progression.CurrentGlobalIncomeMultiplier
                * throughput
                * solverPower
                * agentPower
                * boardPower
                * modifierPower;

            return new ImpactSnapshot
            {
                GlobalIncomeMultiplier = progression.CurrentGlobalIncomeMultiplier,
                Throughput = throughput,
                PassiveChipsPerSecond = passiveRate,
                BoardPower = boardPower,
                SolverPower = solverPower,
                AgentPower = agentPower,
                ModifierPower = modifierPower,
                DatacenterMarketMultiplier = progression.DatacenterMarketMultiplier,
                OutputPower = outputPower,
                StackCapacity = progression.StackCapacity,
                QueueLength = progression.QueueLength,
                DifficultyLevel = progression.DifficultyLevel,
                ScalingFrequencyLevel = progression.ScalingFrequencyLevel,
                ActiveAgentSlots = progression.ActiveAgentSlots,
                UnlockedSolverCount = progression.UnlockedSolverCount,
                Tokens = progression.Tokens
            };
        }

        private static double EstimateSolverPower(SolverId solver)
        {
            return solver switch
            {
                SolverId.Rand => 1.00,
                SolverId.Merge => 1.20,
                SolverId.Balance => 1.35,
                SolverId.Look => 1.55,
                SolverId.Heur => 1.70,
                SolverId.AntiStall => 1.80,
                SolverId.Moca => 1.95,
                SolverId.Plan3 => 2.25,
                SolverId.Combo => 2.35,
                SolverId.Plan5 => 2.55,
                SolverId.MocaPlus => 2.20,
                SolverId.Mcts => 2.00,
                SolverId.MachineLearning => 3.00,
                _ => 1.00
            };
        }

        private static double EstimateAgentPower(StackMergeProgression progression)
        {
            double power = 1.0;
            if (progression.IsAgentActive(AgentId.MergeBroker))
            {
                power *= 1.35;
            }

            if (progression.IsAgentActive(AgentId.HighwaterAnalyst))
            {
                power *= 1.25;
            }

            if (progression.IsAgentActive(AgentId.ScoreAuditor))
            {
                power *= 1.18;
            }

            if (progression.IsAgentActive(AgentId.Quartermaster))
            {
                power *= 1.08;
            }

            if (progression.IsAgentActive(AgentId.RestartSponsor))
            {
                power *= 1.05;
            }

            if (progression.IsAgentActive(AgentId.TokenProspector))
            {
                power *= 1.06;
            }

            if (progression.IsAgentActive(AgentId.MoveDividend))
            {
                power *= 1.14;
            }

            if (progression.IsAgentActive(AgentId.VelocityTrader))
            {
                power *= 1.18;
            }

            return power;
        }

        private static DirectImpact CalculateDirectImpact(List<PurchaseImpactRecord> impacts)
        {
            if (impacts == null || impacts.Count == 0)
            {
                return default;
            }

            double outputRatio = AggregateRatio(impacts, snapshot => snapshot.OutputPower);
            double incomeRatio = AggregateRatio(impacts, snapshot => snapshot.GlobalIncomeMultiplier);
            double throughputRatio = AggregateRatio(impacts, snapshot => snapshot.Throughput);
            double passiveRatio = AggregateRatio(impacts, snapshot => snapshot.PassiveChipsPerSecond);
            double boardRatio = AggregateRatio(impacts, snapshot => snapshot.BoardPower);
            double solverRatio = AggregateRatio(impacts, snapshot => snapshot.SolverPower);
            double agentRatio = AggregateRatio(impacts, snapshot => snapshot.AgentPower);
            double modifierRatio = AggregateRatio(impacts, snapshot => snapshot.ModifierPower);
            double datacenterRatio = AggregateRatio(impacts, snapshot => snapshot.DatacenterMarketMultiplier);

            var candidates = new[]
            {
                ("output", outputRatio),
                ("income", incomeRatio),
                ("throughput", throughputRatio),
                ("passive", passiveRatio),
                ("board", boardRatio),
                ("solver", solverRatio),
                ("agent", agentRatio),
                ("modifier", modifierRatio),
                ("market", datacenterRatio)
            };
            (string Name, double Ratio) best = candidates
                .OrderByDescending(candidate => Math.Abs(candidate.Item2 - 1.0))
                .First();

            return new DirectImpact
            {
                DeltaPercent = (best.Ratio - 1.0) * 100.0,
                Driver = $"{best.Name} {(best.Ratio >= 1.0 ? "+" : "")}{(best.Ratio - 1.0) * 100.0:0.#}%"
            };
        }

        private static DirectImpact CalculateTokenPackDirectImpact(List<PurchaseImpactRecord> impacts)
        {
            long gained = impacts?.Sum(impact => Math.Max(0, impact.After.Tokens - impact.Before.Tokens)) ?? 0;
            return new DirectImpact
            {
                DeltaPercent = 0.0,
                Driver = gained > 0 ? $"tokens +{FormatCompact(gained)}" : "restart fuel"
            };
        }

        private static double AggregateRatio(List<PurchaseImpactRecord> impacts, Func<ImpactSnapshot, double> selector)
        {
            double ratio = 1.0;
            foreach (PurchaseImpactRecord impact in impacts)
            {
                double before = selector(impact.Before);
                double after = selector(impact.After);
                if (before <= 0.000001)
                {
                    ratio *= after > 0.000001 ? 2.0 : 1.0;
                }
                else
                {
                    ratio *= Math.Max(0.000001, after) / before;
                }
            }

            return ratio;
        }

        private RunWindowStats CalculateWindowStats(IEnumerable<RunSample> samples)
        {
            List<RunSample> list = samples?.ToList() ?? new List<RunSample>();
            if (list.Count == 0)
            {
                return default;
            }

            double seconds = list.Sum(sample => sample.DurationSeconds);
            long gross = list.Sum(sample => Math.Max(0, sample.GrossIncome));
            int scoreRuns = list.Count(sample => sample.Score > 0);
            double scorePerRun = scoreRuns > 0 ? list.Where(sample => sample.Score > 0).Average(sample => sample.Score) : 0;
            return new RunWindowStats
            {
                RunCount = list.Count,
                Seconds = seconds,
                ChipsPerSecond = seconds > 0 ? gross / seconds : 0,
                ScorePerRun = scorePerRun,
                RunsPerMinute = seconds > 0 ? list.Count / seconds * 60.0 : 0,
                MovesPerRun = list.Average(sample => sample.Moves),
                MaxHigh = list.Max(sample => sample.High)
            };
        }

        private static double PercentDelta(double after, double before)
        {
            if (before <= 0)
            {
                return after > 0 ? 100.0 : 0.0;
            }

            return (after - before) / before * 100.0;
        }

        private static double? CalculateCycleSpeedup(List<CycleRecord> cycles, double purchaseSeconds)
        {
            if (cycles == null || cycles.Count == 0)
            {
                return null;
            }

            CycleRecord before = cycles
                .Where(cycle => !cycle.Unfinished && cycle.EndSeconds <= purchaseSeconds && cycle.DurationSeconds > 0)
                .OrderByDescending(cycle => cycle.EndSeconds)
                .FirstOrDefault();
            CycleRecord after = cycles
                .Where(cycle => !cycle.Unfinished && cycle.EndSeconds > purchaseSeconds && cycle.DurationSeconds > 0)
                .OrderBy(cycle => cycle.EndSeconds)
                .FirstOrDefault();
            if (before == null || after == null || before.DurationSeconds <= 0)
            {
                return null;
            }

            return (before.DurationSeconds - after.DurationSeconds) / before.DurationSeconds * 100.0;
        }

        private static string GetImpactRole(string key)
        {
            if (key == "TokenPack")
            {
                return "Utility";
            }

            if (key.StartsWith("Research.", StringComparison.Ordinal) || key.StartsWith("Rack.", StringComparison.Ordinal) || key.StartsWith("Facility.", StringComparison.Ordinal))
            {
                return "Meta";
            }

            if (key.StartsWith("Agent.", StringComparison.Ordinal))
            {
                return "Agent";
            }

            if (key.StartsWith("Modifier.", StringComparison.Ordinal))
            {
                return "Modifier";
            }

            if (key.StartsWith("Solver.", StringComparison.Ordinal))
            {
                return "Solver";
            }

            if (key.StartsWith("Unlock.", StringComparison.Ordinal) || key.Contains("StackCapacity") || key.Contains("Difficulty") || key.Contains("QueuePreview") || key.Contains("ScalingFrequency"))
            {
                return "Gate";
            }

            if (key.Contains("Speed") || key.Contains("Compute"))
            {
                return "Throughput";
            }

            return "Economy";
        }

        private static string GetFastImpactVerdict(FastImpactRow row)
        {
            bool gateSignal = row.Role == "Gate" && (row.HighMultiplier >= 1.9 || Math.Abs(row.ScoreDeltaPercent) >= 20.0);
            double cycle = row.CycleSpeedupPercent ?? 0.0;
            double best = Math.Max(Math.Max(row.DirectDeltaPercent, row.IncomeDeltaPercent), Math.Max(row.ScoreDeltaPercent, Math.Max(row.ThroughputDeltaPercent, cycle)));
            if (gateSignal)
            {
                return "GATE";
            }

            if (row.DirectDeltaPercent >= 25.0 || best >= 35.0 || row.EstimatedPayback >= 2.0)
            {
                return "STRONG";
            }

            if (row.DirectDeltaPercent >= 7.5 || best >= 10.0 || row.EstimatedPayback >= 0.75)
            {
                return "OK";
            }

            if (best <= -7.5)
            {
                return "NOISY/NEG";
            }

            return row.CoveragePercent < 15.0 ? "LATE" : "WEAK";
        }

        private static string FormatPercent(double value)
        {
            return $"{(value >= 0 ? "+" : "")}{value:0.#}%";
        }

        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            {
                return value ?? string.Empty;
            }

            return value.Substring(0, Math.Max(0, maxLength - 1)) + "…";
        }

        private string BuildAuditReport(
            List<AuditJob> jobs,
            int[] seeds,
            double wallSeconds,
            List<string> skippedShop,
            List<string> skippedMeta,
            ConcurrentDictionary<string, CachedRunResult> runCache,
            RunReplayCacheStats runCacheStats,
            ConcurrentDictionary<string, CachedRunResult> representativeRunCache,
            RunReplayCacheStats representativeRunCacheStats,
            ConcurrentDictionary<string, SimulationForkSnapshot> forkSnapshots)
        {
            var sb = new StringBuilder();
            int variantCount = jobs.Count(job => job.Key != BaselineKey);
            sb.AppendLine($"=== BALANCE AUDIT ===  ({variantCount} variants × {seeds.Length} seed(s), wall {wallSeconds:0}s, {auditThreads} threads)");
            sb.AppendLine("Δ% = how much LONGER the playthrough takes without the item (fixed seeds — differences are causal, not noise).");
            sb.AppendLine("ROI = simulated seconds saved per 1M chips (shop) / per 1K Insight (meta) spent on the item in the baseline.");
            sb.AppendLine("Verdicts: STRONG ≥ 10% | OK | WEAK ≤ 0.5% | HARMFUL < 0 (faster without it!) | REQUIRED = didn't finish within 2× the baseline's runs.");
            sb.AppendLine("Note: banning a research node also locks its descendants — their value is included in the node's Δ.");
            if (!string.IsNullOrWhiteSpace(auditKeyFilter))
            {
                sb.AppendLine($"Key filter: \"{auditKeyFilter.Trim()}\".");
            }
            sb.AppendLine($"Variant run cap: baseline runs x {auditVariantRunCapMultiplier:0.##} + 100.");

            if (runCacheStats != null)
            {
                sb.AppendLine($"Run replay cache: {runCacheStats.Hits:N0} hits / {runCacheStats.Misses:N0} misses / {runCache?.Count ?? 0:N0} stored deterministic runs.");
            }
            if (representativeRunCacheStats != null)
            {
                sb.AppendLine($"Experimental estimate cache: {representativeRunCacheStats.Hits:N0} hits / {representativeRunCacheStats.Misses:N0} misses / {representativeRunCache?.Count ?? 0:N0} gameplay states.");
                sb.AppendLine("Experimental estimate cache is not suitable for final balance numbers; it reuses variant board outcomes by gameplay state instead of random seed.");
            }
            if (forkSnapshots != null)
            {
                sb.AppendLine($"Fork snapshots: {forkSnapshots.Count:N0} captured first-purchase states; variants resume from these instead of replaying the shared prefix.");
            }

            sb.AppendLine();

            AppendAuditScope(sb, jobs, AuditScope.Shop, $"SHOP scope (horizon: first prestige, seed {seed})", skippedShop);
            if (auditMeta)
            {
                sb.AppendLine();
                AppendAuditScope(sb, jobs, AuditScope.Meta, $"META scope (horizon: {metaAuditPrestiges} prestiges)", skippedMeta);
            }

            return sb.ToString();
        }

        private void AppendAuditScope(StringBuilder sb, List<AuditJob> jobs, AuditScope scope, string title, List<string> skipped)
        {
            AuditJob baseline = jobs.FirstOrDefault(job => job.Scope == scope && job.Key == BaselineKey);
            if (baseline == null)
            {
                return;
            }

            double baselineSeconds = baseline.MeanSeconds();
            sb.AppendLine($"=== {title} ===");
            sb.AppendLine($"Baseline: {FormatTime(baselineSeconds)} ({baseline.MaxRuns()} runs)");

            double? baselinePpo = baseline.MeanFirstPpoSeconds();
            if (baselinePpo.HasValue)
            {
                sb.AppendLine($"Baseline time-to-PPO: {FormatTime(baselinePpo.Value)}");
            }

            sb.AppendLine();
            sb.AppendLine($"{"Item",-30} {"Spent",12} {"Δtime",12} {"Δ%",8} {"ΔPPO%",8} {"ROI",9}  Verdict");

            int horizon = scope == AuditScope.Shop ? 1 : metaAuditPrestiges;
            var rows = new List<(string Line, double SortValue)>();
            var requiredDiagnostics = new List<string>();
            foreach (AuditJob job in jobs)
            {
                if (job.Scope != scope || job.Key == BaselineKey)
                {
                    continue;
                }

                bool finished = job.AllFinished(horizon);
                double deltaSeconds = job.MeanSeconds() - baselineSeconds;
                double deltaPercent = baselineSeconds > 1 ? deltaSeconds / baselineSeconds * 100.0 : 0;

                double? jobPpo = job.MeanFirstPpoSeconds();
                string ppoDelta = baselinePpo.HasValue && jobPpo.HasValue
                    ? $"{(jobPpo.Value - baselinePpo.Value) / Math.Max(1.0, baselinePpo.Value) * 100.0:+0.0;-0.0}%"
                    : "—";

                bool insightCurrency = job.Key.StartsWith("Research.", StringComparison.Ordinal);
                long spent = baseline.SpendOn(job.Key, insightCurrency);
                string spentText = spent > 0 ? FormatCompact(spent) + (insightCurrency ? " ins" : string.Empty) : "—";
                string roi = "—";
                if (spent > 0 && finished)
                {
                    double unit = insightCurrency ? spent / 1_000.0 : spent / 1_000_000.0;
                    if (unit > 0.0001)
                    {
                        roi = $"{deltaSeconds / unit:0}";
                    }
                }

                string verdict;
                if (!finished)
                {
                    verdict = "REQUIRED";
                    deltaPercent = double.MaxValue; // sort to top
                    requiredDiagnostics.Add(DescribeRequiredJob(job));
                }
                else if (deltaPercent < -1.0)
                {
                    verdict = "HARMFUL(!)";
                }
                else if (deltaPercent >= 10.0)
                {
                    verdict = "STRONG";
                }
                else if (deltaPercent <= 0.5)
                {
                    verdict = "WEAK";
                }
                else
                {
                    verdict = "OK";
                }

                string deltaText = finished
                    ? $"{(deltaSeconds < 0 ? "-" : "+")}{FormatTime(Math.Abs(deltaSeconds))}"
                    : "> cap";
                string percentText = finished ? $"{(deltaSeconds < 0 ? "-" : "+")}{Math.Abs(deltaSeconds) / Math.Max(1.0, baselineSeconds) * 100.0:0.0}%" : "—";
                rows.Add(($"{job.Key,-30} {spentText,12} {deltaText,-12} {percentText,8} {ppoDelta,8} {roi,9}  {verdict}", deltaPercent));
            }

            foreach ((string line, double _) in rows.OrderByDescending(row => row.SortValue))
            {
                sb.AppendLine(line);
            }

            if (skipped != null && skipped.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"Not purchased in the baseline (banning them provably changes nothing, Δ = 0 — but 'never worth buying' is itself a finding):");
                sb.AppendLine($"  {string.Join(", ", skipped)}");
            }
            if (requiredDiagnostics.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine($"REQUIRED diagnostics (cap = baseline runs x {auditVariantRunCapMultiplier:0.##} + 100):");
                foreach (string line in requiredDiagnostics)
                {
                    sb.AppendLine($"  {line}");
                }
            }
        }

        private static string FormatCompact(long value)
        {
            return value >= 1_000_000_000 ? $"{value / 1_000_000_000d:0.#}B"
                : value >= 1_000_000 ? $"{value / 1_000_000d:0.#}M"
                : value >= 1_000 ? $"{value / 1_000d:0.#}K"
                : value.ToString();
        }

        private static string DescribeRequiredJob(AuditJob job)
        {
            SimOutcome outcome = job.Outcomes.FirstOrDefault(value => value != null);
            if (outcome == null)
            {
                return $"{job.Key}: no outcome";
            }

            StackMergeProgression p = outcome.Progression;
            string stopReason = GetStopReason(outcome);
            return $"{job.Key}: stopped by {stopReason}; runs {outcome.Runs:N0}, sim {FormatTime(outcome.Sim.Clock)}, prestiges {p.PrestigeCount}, PPO {(outcome.PpoUnlockedAtEnd ? "yes" : "no")}, chips {FormatCompact(p.Chips)}, earned {FormatCompact(p.TotalChipsEarned)}, high {p.HighestBlockEver}, best {FormatCompact(p.BestRunScore)}, levels cap/yield/diff/scale/speed {p.StackCapacityLevel}/{p.IncomeLevel}/{p.DifficultyLevel}/{p.ScalingFrequencyLevel}/{p.SpeedLevel}";
        }

        private static string GetStopReason(SimOutcome outcome)
        {
            if (outcome == null)
            {
                return "no outcome";
            }

            if (outcome.ReachedHorizon && !outcome.HitSafetyLimit && !outcome.HitRunLimit && !outcome.Cancelled)
            {
                return "finished";
            }

            if (outcome.HitSafetyLimit)
            {
                return "wall cap";
            }

            if (outcome.HitRunLimit)
            {
                return "run cap";
            }

            return outcome.Cancelled ? "cancelled" : "horizon not reached";
        }

        private string WriteAuditFile(List<AuditJob> jobs, int[] seeds)
        {
            try
            {
                string dir = Path.Combine(Application.dataPath, "..", "BenchmarkResults");
                Directory.CreateDirectory(dir);
                string path = Path.GetFullPath(Path.Combine(dir, $"balance_audit_{DateTime.Now:yyyyMMdd_HHmmss}.tsv"));

                var sb = new StringBuilder();
                sb.AppendLine("# Balance audit (ablation, common random numbers)");
                sb.AppendLine($"# seeds={string.Join(",", seeds)} maxRuns={maxRuns} moveCap={moveCap} shopHorizon=1 metaHorizon={metaAuditPrestiges} auditMeta={auditMeta}");
                sb.AppendLine("Scope\tKey\tSeed\tTotalSeconds\tRuns\tPrestiges\tFirstPpoSeconds\tFirstPrestigeSeconds\tTotalEarned\tFinished\tHitCap\tHitRunLimit\tPpoUnlocked\tFinalChips\tHighestBlock\tBestRunScore\tLevelsCapYieldDiffScaleSpeed\tStopReason");
                foreach (AuditJob job in jobs)
                {
                    for (int i = 0; i < job.Outcomes.Length; i++)
                    {
                        SimOutcome outcome = job.Outcomes[i];
                        if (outcome == null)
                        {
                            continue;
                        }

                        double? ppoSeconds = outcome.FirstPpoSeconds();
                        double? prestigeSeconds = outcome.FirstPrestigeSeconds();
                        StackMergeProgression p = outcome.Progression;
                        sb.AppendLine($"{job.Scope}\t{job.Key}\t{seeds[i]}\t{outcome.Sim.Clock:0}\t{outcome.Runs}\t{p.PrestigeCount}\t{(ppoSeconds.HasValue ? ppoSeconds.Value.ToString("0") : "")}\t{(prestigeSeconds.HasValue ? prestigeSeconds.Value.ToString("0") : "")}\t{p.TotalChipsEarned}\t{outcome.ReachedHorizon && !outcome.HitSafetyLimit && !outcome.Cancelled}\t{outcome.HitSafetyLimit}\t{outcome.HitRunLimit}\t{outcome.PpoUnlockedAtEnd}\t{p.Chips}\t{p.HighestBlockEver}\t{p.BestRunScore}\t{p.StackCapacityLevel}/{p.IncomeLevel}/{p.DifficultyLevel}/{p.ScalingFrequencyLevel}/{p.SpeedLevel}\t{GetStopReason(outcome)}");
                    }
                }

                File.WriteAllText(path, sb.ToString());
                AssetDatabase.Refresh();
                return path;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Balance audit: failed to write log: {exception.Message}");
                return string.Empty;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Phase / cycle tracking
        // ---------------------------------------------------------------------------------------

        private static void RecordPhases(StackMergeProgression p, SimState sim, int run)
        {
            void Check(string name, bool condition)
            {
                if (condition)
                {
                    RecordPhase(sim, name, run);
                }
            }

            Check("First solver bought", p.HasPurchasedSolver);
            Check("Auto Solve", p.AutoSolveUnlocked);
            Check("Auto Restart", p.AutoRestartUnlocked);
            Check("Solver Tuning", p.SolverTuningUnlocked);
            Check("Agents Menu", p.AgentsMenuUnlocked);
            Check("All agents hired", StackMergeProgression.Agents.All(agent => p.IsAgentUnlocked(agent.Id)));
            Check("Modifiers Menu", p.ModifiersMenuUnlocked);
            Check("All modifiers maxed (PPO gate)", p.CanUnlockMachineLearning);
            Check("Datacenter unlocked", p.DatacenterUnlocked);
            Check("PPO bought", p.IsSolverUnlocked(SolverId.MachineLearning));
            Check("PPO Normal mode", p.IsSolverUnlocked(SolverId.MachineLearning) && p.MachineLearningPlayingModeUnlocked);
            Check("All chip upgrades maxed",
                p.IsMaxSpeed && p.IsMaxComputeSpeed && p.IsMaxStackCapacity && p.IsMaxQueuePreview
                && p.IsMaxIncome && p.IsMaxDifficulty && p.IsMaxScalingFrequency && p.IsMaxProfitableEnding
                && p.IsMaxPassiveYield && p.IsMaxPassiveTickRate && p.IsMaxActiveMultiplier);
        }

        private static void RecordPhase(SimState sim, string name, int run)
        {
            if (!sim.PhasesThisCycle.Add(name))
            {
                return;
            }

            sim.Phases.Add(new PhaseRecord(sim.CycleIndex, name, run, sim.Clock, sim.Clock - sim.CycleStartSeconds));
        }

        private static void StartNewCycle(SimState sim, StackMergeProgression progression, int run, long insightGained, int prestigeCount)
        {
            sim.Cycles.Add(MakeCycleRecord(sim, progression, run, insightGained, prestigeCount, false));
            sim.CycleIndex++;
            sim.CycleStartSeconds = sim.Clock;
            sim.CycleStartRun = run + 1;
            sim.PhasesThisCycle.Clear();
        }

        private static void CloseOpenCycle(SimState sim, StackMergeProgression progression, int run)
        {
            if (run >= sim.CycleStartRun)
            {
                sim.Cycles.Add(MakeCycleRecord(sim, progression, run, 0, progression.PrestigeCount, true));
            }
        }

        private static CycleRecord MakeCycleRecord(SimState sim, StackMergeProgression progression, int run, long insightGained, int prestigeCount, bool unfinished)
        {
            long[] now = progression.CopyIncomeLedger();
            long[] delta = new long[now.Length];
            for (int i = 0; i < now.Length; i++)
            {
                delta[i] = now[i] - (i < sim.LastLedgerSnapshot.Length ? sim.LastLedgerSnapshot[i] : 0);
            }

            sim.LastLedgerSnapshot = now;
            return new CycleRecord(sim.CycleIndex, sim.CycleStartRun, run, sim.CycleStartSeconds, sim.Clock, insightGained, prestigeCount)
            {
                Unfinished = unfinished,
                LedgerDelta = delta
            };
        }

        private static string FormatLedgerMix(long[] ledger)
        {
            if (ledger == null)
            {
                return "n/a";
            }

            long total = ledger.Sum();
            if (total <= 0)
            {
                return "n/a";
            }

            var parts = new List<string>();
            for (int i = 0; i < ledger.Length && i < IncomeSourceNames.Length; i++)
            {
                if (ledger[i] > 0)
                {
                    parts.Add($"{IncomeSourceNames[i]} {ledger[i] * 100.0 / total:0.0}%");
                }
            }

            return string.Join(" | ", parts);
        }

        private static string FormatTime(double seconds)
        {
            if (seconds < 0 || double.IsNaN(seconds))
            {
                seconds = 0;
            }

            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return t.TotalDays >= 1.0
                ? $"{(int)t.TotalDays}d {t.Hours:00}:{t.Minutes:00}:{t.Seconds:00}"
                : $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}";
        }

        // ---------------------------------------------------------------------------------------
        // Reporting (normal mode)
        // ---------------------------------------------------------------------------------------

        private string BuildSummary(StackMergeProgression progression, SimState sim, int runs)
        {
            List<PurchaseRecord> purchases = sim.Purchases;
            var sb = new StringBuilder();
            sb.AppendLine($"Simulated {runs} runs  |  total sim time {FormatTime(sim.Clock)} (active play)  |  final chips {progression.Chips:N0}  |  total earned {progression.TotalChipsEarned:N0}");
            sb.AppendLine($"Prestiges: {progression.PrestigeCount:N0}  |  Insight {progression.ResearchInsight:N0}  |  Cycle Insight {progression.ResearchInsightEarnedThisPrestige:N0}  |  Lifetime Insight {progression.LifetimeResearchInsight:N0}  |  Last prestige {progression.LastPrestigeInsight:N0}");
            PurchaseRecord? firstPpo = FindFirst(purchases, "Solver PPO");
            sb.AppendLine($"PPO first unlocked: {(firstPpo.HasValue ? $"run {firstPpo.Value.Run} ({FormatTime(firstPpo.Value.SimSeconds)})" : "never")}");
            sb.AppendLine($"Highest block ever: {progression.HighestBlockEver}  |  best run score: {progression.BestRunScore:N0}");
            if (progression.DatacenterUnlocked)
            {
                sb.AppendLine($"Datacenter: {progression.TotalDatacenterRackUnits} rack units  |  {progression.DatacenterTotalGigaflops:N1} GF/s  |  market x{progression.DatacenterMarketMultiplier:0.00}");
            }

            sb.AppendLine();

            sb.AppendLine("=== Prestige cycles (real-time length of each playthrough) ===");
            foreach (CycleRecord cycle in sim.Cycles)
            {
                string ending = cycle.Unfinished
                    ? "(unfinished / end of sim)"
                    : $"→ Prestige #{cycle.PrestigeCountAfter} (+{cycle.InsightGained} Insight)";
                sb.AppendLine($"  Cycle {cycle.Index}: {FormatTime(cycle.DurationSeconds),-14}  runs {cycle.StartRun}-{cycle.EndRun}  {ending}");
            }

            if (sim.Cycles.Count(c => !c.Unfinished) >= 2)
            {
                CycleRecord first = sim.Cycles.First(c => !c.Unfinished);
                CycleRecord last = sim.Cycles.Last(c => !c.Unfinished);
                if (first.DurationSeconds > 1)
                {
                    sb.AppendLine($"  Permanent-bonus effect: cycle {last.Index} took {last.DurationSeconds / first.DurationSeconds:P0} of cycle {first.Index}'s time.");
                }
            }

            sb.AppendLine();
            sb.AppendLine("=== Income mix per cycle (share of chips earned) ===");
            foreach (CycleRecord cycle in sim.Cycles)
            {
                sb.AppendLine($"  Cycle {cycle.Index}: {FormatLedgerMix(cycle.LedgerDelta)}");
            }

            sb.AppendLine();
            sb.AppendLine("=== Phase timeline per cycle (Δ = time since cycle start) ===");
            int currentCycle = -1;
            foreach (PhaseRecord phase in sim.Phases)
            {
                if (phase.Cycle != currentCycle)
                {
                    currentCycle = phase.Cycle;
                    sb.AppendLine($"  Cycle {currentCycle}:");
                }

                sb.AppendLine($"    {phase.Name,-32} run {phase.Run,6}   Δ {FormatTime(phase.CycleSeconds),-12} (t = {FormatTime(phase.SimSeconds)})");
            }

            sb.AppendLine();
            sb.AppendLine("=== Key milestones (first occurrence) ===");
            AppendMilestone(sb, purchases, "Solver RAND", "Solver RAND");
            AppendMilestone(sb, purchases, "Auto Solve", "Auto Solve");
            AppendMilestone(sb, purchases, "First Chip Yield", "Chip Yield");
            AppendMilestone(sb, purchases, "Solver MERG", "Solver MERG");
            AppendMilestone(sb, purchases, "Solver BAL", "Solver BAL");
            AppendMilestone(sb, purchases, "Auto Restart", "Auto Restart");
            AppendMilestone(sb, purchases, "Solver STALL", "Solver STALL");
            AppendMilestone(sb, purchases, "Solver HEUR", "Solver HEUR");
            AppendMilestone(sb, purchases, "Stack Cap L1", "Stack Cap L1");
            AppendMilestone(sb, purchases, "Solver Tuning", "Solver Tuning");
            AppendMilestone(sb, purchases, "Solver LOOK", "Solver LOOK");
            AppendMilestone(sb, purchases, "Solver COMBO", "Solver COMBO");
            AppendMilestone(sb, purchases, "Agents Menu", "Agents Menu");
            AppendMilestone(sb, purchases, "Modifiers Menu", "Modifiers Menu");
            AppendMilestone(sb, purchases, "Solver PPO", "Solver PPO");
            AppendMilestone(sb, purchases, "PPO Training Done", "PPO Training Complete");
            AppendMilestone(sb, purchases, "First Prestige", "Prestige +");
            AppendMilestone(sb, purchases, "Root Research", "Research Seed Capital");
            sb.AppendLine();

            sb.AppendLine("=== Full purchase timeline ===");
            foreach (PurchaseRecord record in purchases)
            {
                sb.AppendLine($"Run {record.Run,6}  t {FormatTime(record.SimSeconds),-12}  |  {record.Label,-36}  {record.Cost,14:N0} {record.Currency,-7}  (balance {record.BalanceAfter,14:N0})");
            }

            return sb.ToString();
        }

        private static void AppendMilestone(StringBuilder sb, List<PurchaseRecord> purchases, string label, string startsWith)
        {
            PurchaseRecord? record = FindFirst(purchases, startsWith);
            sb.AppendLine($"  {label,-22}: {(record.HasValue ? $"run {record.Value.Run}  ({FormatTime(record.Value.SimSeconds)})" : "not reached")}");
        }

        private static PurchaseRecord? FindFirst(List<PurchaseRecord> purchases, string startsWith)
        {
            foreach (PurchaseRecord record in purchases)
            {
                if (record.Label.StartsWith(startsWith, StringComparison.Ordinal))
                {
                    return record;
                }
            }

            return null;
        }

        private string WriteFile(SimOutcome outcome)
        {
            try
            {
                SimState sim = outcome.Sim;
                StackMergeProgression progression = outcome.Progression;
                string dir = Path.Combine(Application.dataPath, "..", "BenchmarkResults");
                Directory.CreateDirectory(dir);
                string path = Path.GetFullPath(Path.Combine(dir, $"progression_sim_{DateTime.Now:yyyyMMdd_HHmmss}.tsv"));

                var sb = new StringBuilder();
                sb.AppendLine("# Progression simulation");
                sb.AppendLine($"# runs={outcome.Runs} seed={seed} moveCap={moveCap} manualProxy={manualProxy} manualSecPerMove={manualSecondsPerMove} manualRestartSec={manualRestartSeconds} stopAtPpo={stopAtPpo} simulateResearch={simulateResearch} targetPrestiges={targetPrestiges} ppoNormalRunsPerPrestige={ppoNormalRunsPerPrestige} ppoTrainingMovesPerRun={ppoTrainingMovesPerRun}");
                sb.AppendLine($"# finalChips={progression.Chips} totalEarned={progression.TotalChipsEarned} prestiges={progression.PrestigeCount} insight={progression.ResearchInsight} cycleInsight={progression.ResearchInsightEarnedThisPrestige} lifetimeInsight={progression.LifetimeResearchInsight} simSeconds={sim.Clock:0}");
                sb.AppendLine("# Cycles:");
                sb.AppendLine("Cycle\tStartRun\tEndRun\tStartSeconds\tEndSeconds\tDurationSeconds\tInsightGained\tUnfinished\tIncomeMix");
                foreach (CycleRecord cycle in sim.Cycles)
                {
                    sb.AppendLine($"{cycle.Index}\t{cycle.StartRun}\t{cycle.EndRun}\t{cycle.StartSeconds:0}\t{cycle.EndSeconds:0}\t{cycle.DurationSeconds:0}\t{cycle.InsightGained}\t{cycle.Unfinished}\t{FormatLedgerMix(cycle.LedgerDelta)}");
                }

                sb.AppendLine();
                sb.AppendLine("# Phases:");
                sb.AppendLine("Cycle\tPhase\tRun\tSimSeconds\tCycleSeconds");
                foreach (PhaseRecord phase in sim.Phases)
                {
                    sb.AppendLine($"{phase.Cycle}\t{phase.Name}\t{phase.Run}\t{phase.SimSeconds:0}\t{phase.CycleSeconds:0}");
                }

                sb.AppendLine();
                sb.AppendLine("# Purchases:");
                sb.AppendLine("Run\tSimSeconds\tKey\tLabel\tCost\tCurrency\tBalanceAfter\tTotalEarnedAfter");
                foreach (PurchaseRecord record in sim.Purchases)
                {
                    sb.AppendLine($"{record.Run}\t{record.SimSeconds:0}\t{record.Key}\t{record.Label}\t{record.Cost}\t{record.Currency}\t{record.BalanceAfter}\t{record.TotalEarnedAfter}");
                }

                if (outcome.Details != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("# Per-run economy (sampled):");
                    sb.Append(outcome.Details);
                }

                File.WriteAllText(path, sb.ToString());
                AssetDatabase.Refresh();
                return path;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Progression sim: failed to write log: {exception.Message}");
                return string.Empty;
            }
        }

        // ---------------------------------------------------------------------------------------
        // Data holders
        // ---------------------------------------------------------------------------------------

        private enum AuditScope
        {
            Shop,
            Meta
        }

        private sealed class SimCoreConfig
        {
            public int Seed;
            public int MaxRuns;
            public bool StopAtPpo;
            public bool SimulateResearch;
            public int TargetPrestiges;
            public int WallClockLimitSeconds;
            public HashSet<string> BannedKeys;
            public bool CollectDetails;
            public bool CollectRunHistory;
            public ConcurrentDictionary<string, CachedRunResult> RunCache;
            public RunReplayCacheStats RunCacheStats;
            public ConcurrentDictionary<string, CachedRunResult> RepresentativeRunCache;
            public RunReplayCacheStats RepresentativeRunCacheStats;
            public bool UseRepresentativeRunCache;
            public ConcurrentDictionary<string, SimulationForkSnapshot> ForkSnapshots;
            public string ForkSnapshotPrefix;
            public SimulationForkSnapshot StartSnapshot;
        }

        private sealed class SimOutcome
        {
            public SimState Sim;
            public StackMergeProgression Progression;
            public int Runs;
            public bool PpoUnlockedAtEnd;
            public bool ReachedHorizon;
            public bool HitSafetyLimit;
            public bool HitRunLimit;
            public bool Cancelled;
            public StringBuilder Details;
            public readonly List<RunSample> RunSamples = new();

            public double? FirstPpoSeconds()
            {
                foreach (PurchaseRecord record in Sim.Purchases)
                {
                    if (record.Label.StartsWith("Solver PPO", StringComparison.Ordinal))
                    {
                        return record.SimSeconds;
                    }
                }

                return null;
            }

            public double? FirstPrestigeSeconds()
            {
                foreach (CycleRecord cycle in Sim.Cycles)
                {
                    if (!cycle.Unfinished)
                    {
                        return cycle.EndSeconds;
                    }
                }

                return null;
            }
        }

        private sealed class AuditJob
        {
            public string Key;
            public AuditScope Scope;
            public SimOutcome[] Outcomes;

            public double MeanSeconds()
            {
                return Outcomes.Where(outcome => outcome != null).Select(outcome => outcome.Sim.Clock).DefaultIfEmpty(0).Average();
            }

            public double? MeanFirstPpoSeconds()
            {
                double[] values = Outcomes
                    .Where(outcome => outcome != null)
                    .Select(outcome => outcome.FirstPpoSeconds())
                    .Where(value => value.HasValue)
                    .Select(value => value.Value)
                    .ToArray();
                return values.Length > 0 ? values.Average() : null;
            }

            public int MaxRuns()
            {
                return Outcomes.Where(outcome => outcome != null).Select(outcome => outcome.Runs).DefaultIfEmpty(0).Max();
            }

            public bool AllFinished(int prestigeHorizon)
            {
                foreach (SimOutcome outcome in Outcomes)
                {
                    if (outcome == null || !outcome.ReachedHorizon || outcome.HitSafetyLimit || outcome.HitRunLimit || outcome.Cancelled || outcome.Progression.PrestigeCount < prestigeHorizon)
                    {
                        return false;
                    }
                }

                return true;
            }

            public long SpendOn(string key, bool insightCurrency)
            {
                SimOutcome first = Outcomes.FirstOrDefault(outcome => outcome != null);
                if (first == null)
                {
                    return 0;
                }

                Dictionary<string, long> spend = insightCurrency ? first.Sim.InsightSpendByKey : first.Sim.ChipSpendByKey;
                return spend.TryGetValue(key, out long value) ? value : 0;
            }
        }

        private sealed class SimState
        {
            public double Clock;
            public int CycleIndex;
            public double CycleStartSeconds;
            public int CycleStartRun = 1;
            public long[] LastLedgerSnapshot = new long[8];
            public readonly List<PurchaseRecord> Purchases = new();
            public readonly List<PurchaseImpactRecord> PurchaseImpacts = new();
            public readonly List<PhaseRecord> Phases = new();
            public readonly HashSet<string> PhasesThisCycle = new();
            public readonly List<CycleRecord> Cycles = new();
            public readonly Dictionary<string, long> ChipSpendByKey = new();
            public readonly Dictionary<string, long> InsightSpendByKey = new();

            public SimState Clone()
            {
                var clone = new SimState
                {
                    Clock = Clock,
                    CycleIndex = CycleIndex,
                    CycleStartSeconds = CycleStartSeconds,
                    CycleStartRun = CycleStartRun,
                    LastLedgerSnapshot = (long[])LastLedgerSnapshot.Clone()
                };
                clone.Purchases.AddRange(Purchases);
                clone.PurchaseImpacts.AddRange(PurchaseImpacts);
                clone.Phases.AddRange(Phases);
                clone.PhasesThisCycle.UnionWith(PhasesThisCycle);
                clone.Cycles.AddRange(Cycles.Select(cycle => cycle.Clone()));
                foreach (KeyValuePair<string, long> entry in ChipSpendByKey)
                {
                    clone.ChipSpendByKey[entry.Key] = entry.Value;
                }

                foreach (KeyValuePair<string, long> entry in InsightSpendByKey)
                {
                    clone.InsightSpendByKey[entry.Key] = entry.Value;
                }

                return clone;
            }

            public void RecordPurchase(
                int run,
                double clock,
                string key,
                string label,
                long cost,
                bool insightCurrency,
                long balanceAfter,
                long totalEarnedAfter,
                ImpactSnapshot before = null,
                ImpactSnapshot after = null)
            {
                var purchase = new PurchaseRecord(run, clock, key ?? string.Empty, label, cost, insightCurrency ? "Insight" : "chips", balanceAfter, totalEarnedAfter);
                Purchases.Add(purchase);
                if (before != null && after != null && !string.IsNullOrEmpty(key))
                {
                    PurchaseImpacts.Add(new PurchaseImpactRecord(purchase, before, after));
                }

                if (string.IsNullOrEmpty(key) || cost <= 0)
                {
                    return;
                }

                Dictionary<string, long> spend = insightCurrency ? InsightSpendByKey : ChipSpendByKey;
                spend.TryGetValue(key, out long current);
                spend[key] = current + cost;
            }
        }

        private readonly struct PhaseRecord
        {
            public PhaseRecord(int cycle, string name, int run, double simSeconds, double cycleSeconds)
            {
                Cycle = cycle;
                Name = name;
                Run = run;
                SimSeconds = simSeconds;
                CycleSeconds = cycleSeconds;
            }

            public int Cycle { get; }

            public string Name { get; }

            public int Run { get; }

            public double SimSeconds { get; }

            public double CycleSeconds { get; }
        }

        private sealed class CycleRecord
        {
            public CycleRecord(int index, int startRun, int endRun, double startSeconds, double endSeconds, long insightGained, int prestigeCountAfter)
            {
                Index = index;
                StartRun = startRun;
                EndRun = endRun;
                StartSeconds = startSeconds;
                EndSeconds = endSeconds;
                InsightGained = insightGained;
                PrestigeCountAfter = prestigeCountAfter;
            }

            public int Index { get; }

            public int StartRun { get; }

            public int EndRun { get; }

            public double StartSeconds { get; }

            public double EndSeconds { get; }

            public double DurationSeconds => Math.Max(0, EndSeconds - StartSeconds);

            public long InsightGained { get; }

            public int PrestigeCountAfter { get; }

            public bool Unfinished { get; set; }

            public long[] LedgerDelta { get; set; }

            public CycleRecord Clone()
            {
                return new CycleRecord(Index, StartRun, EndRun, StartSeconds, EndSeconds, InsightGained, PrestigeCountAfter)
                {
                    Unfinished = Unfinished,
                    LedgerDelta = LedgerDelta != null ? (long[])LedgerDelta.Clone() : null
                };
            }
        }

        private sealed class SimulationForkSnapshot
        {
            public SimulationForkSnapshot(StackMergeProgression progression, SimState sim, SimRandom random, int completedRuns, bool ppoUnlocked)
            {
                Progression = progression;
                Sim = sim;
                Random = random;
                CompletedRuns = completedRuns;
                PpoUnlocked = ppoUnlocked;
            }

            public StackMergeProgression Progression { get; }

            public SimState Sim { get; }

            public SimRandom Random { get; }

            public int CompletedRuns { get; }

            public bool PpoUnlocked { get; }

            public SimulationForkSnapshot Clone()
            {
                return new SimulationForkSnapshot(
                    Progression.CloneForSimulation(),
                    Sim.Clone(),
                    Random.Clone(),
                    CompletedRuns,
                    PpoUnlocked);
            }
        }

        private sealed class SimRandom
        {
            private readonly int seed;
            private readonly System.Random random;
            private int calls;

            public SimRandom(int seed)
            {
                this.seed = seed;
                random = new System.Random(seed);
            }

            private SimRandom(int seed, int calls)
            {
                this.seed = seed;
                random = new System.Random(seed);
                for (int i = 0; i < calls; i++)
                {
                    random.Next();
                }

                this.calls = calls;
            }

            public SimRandom Clone()
            {
                return new SimRandom(seed, calls);
            }

            public int Next()
            {
                calls++;
                return random.Next();
            }

            public double NextDouble()
            {
                calls++;
                return random.NextDouble();
            }
        }

        private readonly struct RunResult
        {
            public RunResult(long score, int moves, int merges, int high, long moveIncome, long strandedBoardValue)
            {
                Score = score;
                Moves = moves;
                Merges = merges;
                High = high;
                MoveIncome = moveIncome;
                StrandedBoardValue = strandedBoardValue;
            }

            public long Score { get; }

            public int Moves { get; }

            public int Merges { get; }

            public int High { get; }

            public long MoveIncome { get; }

            public long StrandedBoardValue { get; }
        }

        private sealed class FastImpactRow
        {
            public string Key;
            public string Role;
            public int PurchaseCount;
            public long Spent;
            public string Currency;
            public int FirstRun;
            public double FirstSeconds;
            public double CoveragePercent;
            public RunWindowStats Before;
            public RunWindowStats After;
            public double DirectDeltaPercent;
            public string DirectDriver;
            public double IncomeDeltaPercent;
            public double ScoreDeltaPercent;
            public double ThroughputDeltaPercent;
            public double HighMultiplier;
            public double? CycleSpeedupPercent;
            public double EstimatedPayback;
            public string Verdict;

            public double SortScore
            {
                get
                {
                    double cycle = CycleSpeedupPercent ?? 0.0;
                    double best = Math.Max(Math.Max(DirectDeltaPercent, IncomeDeltaPercent), Math.Max(ScoreDeltaPercent, Math.Max(ThroughputDeltaPercent, cycle)));
                    double gateBoost = Verdict == "GATE" ? 1000.0 : 0.0;
                    double paybackBoost = Math.Min(250.0, EstimatedPayback * 25.0);
                    return gateBoost + best + paybackBoost + CoveragePercent * 0.05;
                }
            }
        }

        private struct RunWindowStats
        {
            public int RunCount;
            public double Seconds;
            public double ChipsPerSecond;
            public double ScorePerRun;
            public double RunsPerMinute;
            public double MovesPerRun;
            public int MaxHigh;
        }

        private struct DirectImpact
        {
            public double DeltaPercent;
            public string Driver;
        }

        private sealed class ImpactSnapshot
        {
            public double GlobalIncomeMultiplier;
            public double Throughput;
            public double PassiveChipsPerSecond;
            public double BoardPower;
            public double SolverPower;
            public double AgentPower;
            public double ModifierPower;
            public double DatacenterMarketMultiplier;
            public double OutputPower;
            public int StackCapacity;
            public int QueueLength;
            public int DifficultyLevel;
            public int ScalingFrequencyLevel;
            public int ActiveAgentSlots;
            public int UnlockedSolverCount;
            public long Tokens;
        }

        private readonly struct PurchaseImpactRecord
        {
            public PurchaseImpactRecord(PurchaseRecord purchase, ImpactSnapshot before, ImpactSnapshot after)
            {
                Purchase = purchase;
                Before = before;
                After = after;
            }

            public PurchaseRecord Purchase { get; }

            public ImpactSnapshot Before { get; }

            public ImpactSnapshot After { get; }
        }

        private readonly struct RunSample
        {
            public RunSample(
                int run,
                double startSeconds,
                double endSeconds,
                long chips,
                long totalEarned,
                long grossIncome,
                long score,
                int moves,
                int merges,
                int high,
                SolverId solver,
                int prestigeCount,
                string bought)
            {
                Run = run;
                StartSeconds = startSeconds;
                EndSeconds = endSeconds;
                Chips = chips;
                TotalEarned = totalEarned;
                GrossIncome = grossIncome;
                Score = score;
                Moves = moves;
                Merges = merges;
                High = high;
                Solver = solver;
                PrestigeCount = prestigeCount;
                Bought = bought ?? string.Empty;
            }

            public int Run { get; }

            public double StartSeconds { get; }

            public double EndSeconds { get; }

            public double DurationSeconds => Math.Max(0, EndSeconds - StartSeconds);

            public long Chips { get; }

            public long TotalEarned { get; }

            public long GrossIncome { get; }

            public long Score { get; }

            public int Moves { get; }

            public int Merges { get; }

            public int High { get; }

            public SolverId Solver { get; }

            public int PrestigeCount { get; }

            public string Bought { get; }
        }

        private sealed class CachedRunResult
        {
            public CachedRunResult(
                long score,
                int moves,
                int merges,
                int high,
                long strandedBoardValue,
                long[] moveIncomeBeforeGlobal,
                long[] moveLedgerShare,
                int unstableSaves,
                int jokerMerges)
            {
                Score = score;
                Moves = moves;
                Merges = merges;
                High = high;
                StrandedBoardValue = strandedBoardValue;
                MoveIncomeBeforeGlobal = moveIncomeBeforeGlobal ?? Array.Empty<long>();
                MoveLedgerShare = moveLedgerShare ?? Array.Empty<long>();
                UnstableSaves = unstableSaves;
                JokerMerges = jokerMerges;
            }

            public long Score { get; }

            public int Moves { get; }

            public int Merges { get; }

            public int High { get; }

            public long StrandedBoardValue { get; }

            public long[] MoveIncomeBeforeGlobal { get; }

            public long[] MoveLedgerShare { get; }

            public int UnstableSaves { get; }

            public int JokerMerges { get; }
        }

        private sealed class RunReplayCacheStats
        {
            private long hits;
            private long misses;

            public long Hits => Interlocked.Read(ref hits);

            public long Misses => Interlocked.Read(ref misses);

            public void RecordHit()
            {
                Interlocked.Increment(ref hits);
            }

            public void RecordMiss()
            {
                Interlocked.Increment(ref misses);
            }
        }

        private readonly struct Candidate
        {
            public Candidate(string key, string label, long cost, Func<bool> buy)
            {
                Key = key;
                Label = label;
                Cost = cost;
                Buy = buy;
            }

            public string Key { get; }

            public string Label { get; }

            public long Cost { get; }

            public Func<bool> Buy { get; }
        }

        private readonly struct PurchaseRecord
        {
            public PurchaseRecord(int run, double simSeconds, string label, long cost, string currency, long balanceAfter, long totalEarnedAfter)
                : this(run, simSeconds, string.Empty, label, cost, currency, balanceAfter, totalEarnedAfter)
            {
            }

            public PurchaseRecord(int run, double simSeconds, string key, string label, long cost, string currency, long balanceAfter, long totalEarnedAfter)
            {
                Run = run;
                SimSeconds = simSeconds;
                Key = key;
                Label = label;
                Cost = cost;
                Currency = currency;
                BalanceAfter = balanceAfter;
                TotalEarnedAfter = totalEarnedAfter;
            }

            public int Run { get; }

            public double SimSeconds { get; }

            public string Key { get; }

            public string Label { get; }

            public long Cost { get; }

            public string Currency { get; }

            public long BalanceAfter { get; }

            public long TotalEarnedAfter { get; }
        }
    }
}
