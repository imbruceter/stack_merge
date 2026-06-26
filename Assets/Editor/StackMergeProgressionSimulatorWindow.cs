using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace StackMerge
{
    /// <summary>
    /// Headless progression "bot". It plays the real game economy run after run — using the actual
    /// per-move and per-run chip awards, multipliers and purchase methods from
    /// <see cref="StackMergeProgression"/> — while an auto-buyer spends chips cheapest-first
    /// (respecting prerequisites). It logs at which run number every solver / upgrade / agent /
    /// modifier becomes affordable and is bought, all the way to unlocking PPO. This lets us tune the
    /// economy against the intended progression curve without playing thousands of runs by hand.
    /// </summary>
    public sealed class StackMergeProgressionSimulatorWindow : EditorWindow
    {
        // Solvers preferred for grinding income — fastest strong solvers first. The two slowest
        // (MCTS, MOCA+) are skipped so the simulation stays fast; a grinding player uses a quick
        // solver anyway. PPO is excluded (training-only / endgame).
        private static readonly SolverId[] IncomeSolverPreference =
        {
            SolverId.Combo, SolverId.Plan5, SolverId.Plan3, SolverId.Look,
            SolverId.Moca, SolverId.Heur, SolverId.AntiStall, SolverId.Merge,
            SolverId.Balance, SolverId.Rand
        };

        private int maxRuns = 8000;
        private int seed = 12345;
        private int moveCap = 1000;
        private bool stopAtPpo = true;
        private bool simulateResearch = true;
        private int targetPrestiges = 6;
        private int ppoNormalRunsPerPrestige = 90;
        private int ppoTrainingFramesPerStep = 90000;
        private SolverId manualProxy = SolverId.Rand;
        private Vector2 scroll;
        private string summary = "Configure and press Run.";
        private string lastPath = string.Empty;

        [MenuItem("Tools/Stack Merge/Progression Simulator")]
        public static void Open()
        {
            GetWindow<StackMergeProgressionSimulatorWindow>("Progression Sim").minSize = new Vector2(520f, 520f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Progression Simulator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Plays the real economy run-by-run with an auto-buyer (cheapest affordable first). " +
                "Reports the run number each thing is bought. With Research simulation enabled it continues through prestige cycles and auto-buys research nodes.",
                MessageType.Info);

            maxRuns = Mathf.Clamp(EditorGUILayout.IntField("Max runs", maxRuns), 10, 1000000);
            seed = EditorGUILayout.IntField("Seed", seed);
            moveCap = Mathf.Clamp(EditorGUILayout.IntField("Move cap / run", moveCap), 20, 5000);
            stopAtPpo = EditorGUILayout.Toggle("Stop when PPO unlocks", stopAtPpo);
            simulateResearch = EditorGUILayout.Toggle(new GUIContent("Simulate prestige / research", "Continues after PPO by injecting editor-only PPO training/normal progress and buying research."), simulateResearch);
            using (new EditorGUI.DisabledScope(!simulateResearch || stopAtPpo))
            {
                targetPrestiges = Mathf.Clamp(EditorGUILayout.IntField("Target prestiges", targetPrestiges), 1, 1000);
                ppoTrainingFramesPerStep = Mathf.Clamp(EditorGUILayout.IntField("PPO training frames / step", ppoTrainingFramesPerStep), 1000, 2000000);
                ppoNormalRunsPerPrestige = Mathf.Clamp(EditorGUILayout.IntField("PPO normal runs / prestige", ppoNormalRunsPerPrestige), 1, 10000);
            }
            manualProxy = (SolverId)EditorGUILayout.EnumPopup(new GUIContent("Manual play proxy", "Solver used to model the human player before Auto Solve is bought."), manualProxy);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run simulation", GUILayout.Height(30f)))
                {
                    Run();
                }

                if (!string.IsNullOrEmpty(lastPath) && GUILayout.Button("Reveal log", GUILayout.Height(30f), GUILayout.Width(120f)))
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

        private void Run()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData());
            IStackMergeSolver[] solvers = StackMergeSolverFactory.CreateAll();
            var rng = new System.Random(seed);

            var purchases = new List<PurchaseRecord>();
            var details = new StringBuilder();
            details.AppendLine("Run\tChips\tInsight\tPrestiges\tGrossIncome\tCumEarned\tSolver\tScore\tMoves\tHigh\tBought");

            int run = 0;
            bool ppoUnlocked = false;
            int sampleEvery = Mathf.Max(1, maxRuns / 400);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (run < maxRuns && !(stopAtPpo && ppoUnlocked) && !ResearchTargetReached(progression))
            {
                run++;
                bool auto = progression.AutoSolveEnabled && progression.HasPurchasedSolver;
                SolverId solverId = auto ? BestIncomeSolver(progression) : manualProxy;

                RunResult result = PlayRun(progression, solvers, solverId, rng);
                long runBonus = progression.AwardRunCompleted(result.Score, solverId, result.Moves, result.Merges, result.High, !auto, 0f);
                long gross = result.MoveIncome + runBonus;

                string bought = AutoBuy(progression, purchases, run);
                ppoUnlocked = progression.IsSolverUnlocked(SolverId.MachineLearning);
                string researchEvent = SimulateResearchLayer(progression, purchases, run, rng);
                if (!string.IsNullOrEmpty(researchEvent))
                {
                    bought = string.IsNullOrEmpty(bought) ? researchEvent : $"{bought}, {researchEvent}";
                }

                if (run % sampleEvery == 0 || bought.Length > 0 || run <= 12)
                {
                    long earned = progression.TotalChipsEarned;
                    details.AppendLine($"{run}\t{progression.Chips}\t{progression.ResearchInsight}\t{progression.PrestigeCount}\t{gross}\t{earned}\t{solverId}\t{result.Score}\t{result.Moves}\t{result.High}\t{bought}");
                }

                if (stopwatch.Elapsed.TotalSeconds > 600)
                {
                    summary = $"Stopped after {run} runs ({stopwatch.Elapsed.TotalSeconds:0}s safety limit). Increase the limit in code or reduce Max runs.\n\n" + BuildSummary(progression, purchases, run, false);
                    lastPath = WriteFile(details, purchases, progression, run);
                    Repaint();
                    return;
                }
            }

            summary = BuildSummary(progression, purchases, run, ppoUnlocked);
            lastPath = WriteFile(details, purchases, progression, run);
            Repaint();
        }

        private RunResult PlayRun(StackMergeProgression progression, IStackMergeSolver[] solvers, SolverId solverId, System.Random rng)
        {
            StackMergeRunModifiers modifiers = progression.BuildRunModifiers();
            var state = new StackMergeGameState(
                stackCapacity: progression.StackCapacity,
                queueLength: progression.QueueLength,
                difficultyLevel: progression.DifficultyLevel,
                modifiers: modifiers,
                seed: rng.Next());

            IStackMergeSolver solver = solvers[Mathf.Clamp((int)solverId, 0, solvers.Length - 1)];
            var context = new SolverContext(
                new System.Random(rng.Next()),
                progression.MonteCarloSimulationCount,
                progression.MonteCarloRolloutDepth,
                lightweightMode: true,
                tuning: progression.SolverTuningUnlocked ? progression.GetSolverTuning(solverId) : SolverTuningSettings.Neutral(solverId),
                highTierSpeedTuningAccelerator: progression.NeuralAcceleratorActive,
                machineLearningAgent: progression.MachineLearningAgent,
                machineLearningTrainingMode: progression.IsMachineLearningTrainingActive);

            bool training = progression.IsMachineLearningTrainingActive;
            long moveIncome = 0;
            int moves = 0;
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

                moveIncome += progression.AwardMove(moveResult, training);
                if (solverId == SolverId.MachineLearning)
                {
                    progression.ObserveMachineLearningMove(moveResult, state, training);
                }

                moves++;
            }

            return new RunResult(state.Score, state.BlocksDropped, state.TotalMerges, state.HighestMergedBlock, moveIncome);
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

        private string AutoBuy(StackMergeProgression progression, List<PurchaseRecord> purchases, int run)
        {
            var boughtThisRun = new List<string>();
            int guard = 0;
            while (guard++ < 300)
            {
                List<Candidate> candidates = GetCandidates(progression);
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
                if (!cheapest.Value.Buy())
                {
                    break;
                }

                purchases.Add(new PurchaseRecord(run, cheapest.Value.Label, costPaid, progression.Chips, progression.TotalChipsEarned));
                boughtThisRun.Add(cheapest.Value.Label);
            }

            return string.Join(", ", boughtThisRun);
        }

        private bool ResearchTargetReached(StackMergeProgression progression)
        {
            return simulateResearch
                && !stopAtPpo
                && targetPrestiges > 0
                && progression.PrestigeCount >= targetPrestiges;
        }

        private string SimulateResearchLayer(StackMergeProgression progression, List<PurchaseRecord> purchases, int run, System.Random rng)
        {
            if (!simulateResearch || stopAtPpo || !progression.IsSolverUnlocked(SolverId.MachineLearning))
            {
                return string.Empty;
            }

            var events = new List<string>();
            if (!progression.MachineLearningPlayingModeUnlocked)
            {
                long missingFrames = progression.MachineLearningPlayingModeFrameRequirement - progression.MachineLearningFrames;
                int frames = (int)Math.Min(Math.Max(0, missingFrames), ppoTrainingFramesPerStep);
                if (frames > 0)
                {
                    progression.AddMachineLearningSimulationProgress(frames, 0, 0, 0, 1, 0);
                    events.Add($"PPO train +{frames:N0}f");
                }

                if (progression.MachineLearningPlayingModeUnlocked)
                {
                    progression.SetMachineLearningTrainingMode(false);
                    purchases.Add(new PurchaseRecord(run, "PPO Training Complete", 0, "frames", progression.MachineLearningFrames, progression.TotalChipsEarned));
                    events.Add("PPO Training Complete");
                }

                return string.Join(", ", events);
            }

            (long score, int high, int moves, int merges) = EstimatePpoNormalRun(progression, rng);
            progression.AddMachineLearningSimulationProgress(0, ppoNormalRunsPerPrestige, score, high, moves, merges);
            events.Add($"PPO Normal x{ppoNormalRunsPerPrestige}");

            long preview = progression.PreviewPrestigeInsightGain();
            if (preview > 0 && progression.PrestigeCount < targetPrestiges)
            {
                long gained = progression.ExecutePrestige();
                purchases.Add(new PurchaseRecord(run, $"Prestige +{gained} Insight", 0, "Insight", progression.ResearchInsight, progression.TotalChipsEarned));
                events.Add($"Prestige +{gained}");
                string research = AutoBuyResearch(progression, purchases, run);
                if (!string.IsNullOrEmpty(research))
                {
                    events.Add(research);
                }
            }

            return string.Join(", ", events);
        }

        private (long Score, int High, int Moves, int Merges) EstimatePpoNormalRun(StackMergeProgression progression, System.Random rng)
        {
            int prestige = Math.Max(0, progression.PrestigeCount);
            // Grounded in the measured PPO benchmark: a Normal-mode session peaks around tile
            // 2048-8192, climbing SLOWLY with PPO research (warm start / high-focus / stability), the
            // prestige count, and how many Normal runs are played (more runs = more chances at a high
            // peak). Capped modestly (the small net plateaus) — no fictional 1M tiles.
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

        private string AutoBuyResearch(StackMergeProgression progression, List<PurchaseRecord> purchases, int run)
        {
            var boughtThisRun = new List<string>();
            int guard = 0;
            while (guard++ < 200)
            {
                List<Candidate> candidates = GetResearchCandidates(progression);
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
                if (!cheapest.Value.Buy())
                {
                    break;
                }

                purchases.Add(new PurchaseRecord(run, cheapest.Value.Label, costPaid, "Insight", progression.ResearchInsight, progression.TotalChipsEarned));
                boughtThisRun.Add(cheapest.Value.Label);
            }

            return string.Join(", ", boughtThisRun);
        }

        private static List<Candidate> GetResearchCandidates(StackMergeProgression p)
        {
            var list = new List<Candidate>();
            if (p.PrestigeCount <= 0)
            {
                return list;
            }

            foreach (ResearchDefinition definition in StackMergeProgression.Research)
            {
                ResearchId researchId = definition.Id;
                if (p.IsResearchMaxed(researchId))
                {
                    continue;
                }

                string reason = p.GetResearchUnavailableReason(researchId);
                if (!string.IsNullOrEmpty(reason) && reason != "Not enough Insight.")
                {
                    continue;
                }

                int nextLevel = p.GetResearchLevel(researchId) + 1;
                list.Add(new Candidate($"Research {definition.DisplayName} L{nextLevel}", p.GetResearchCost(researchId), () => p.BuyResearch(researchId)));
            }

            return list;
        }

        private static List<Candidate> GetCandidates(StackMergeProgression p)
        {
            var list = new List<Candidate>();

            // Solver unlocks (non-PPO).
            foreach (SolverDefinition definition in StackMergeSolverCatalog.Definitions)
            {
                SolverId id = definition.Id;
                if (id == SolverId.MachineLearning || p.IsSolverUnlocked(id))
                {
                    continue;
                }

                list.Add(new Candidate($"Solver {definition.DisplayName}", p.GetSolverUnlockCost(id), () => p.SelectOrUnlockSolver(id)));
            }

            // PPO — only once every modifier is maxed.
            if (p.CanUnlockMachineLearning && !p.IsSolverUnlocked(SolverId.MachineLearning))
            {
                list.Add(new Candidate("Solver PPO", p.GetSolverUnlockCost(SolverId.MachineLearning), () => p.SelectOrUnlockSolver(SolverId.MachineLearning)));
            }

            if (!p.AutoSolveUnlocked && p.HasPurchasedSolver)
            {
                list.Add(new Candidate("Auto Solve", p.GetAutoSolveCost(), p.ToggleOrBuyAutoSolve));
            }

            if (!p.AutoRestartUnlocked)
            {
                list.Add(new Candidate("Auto Restart", p.GetAutoRestartCost(), p.ToggleOrBuyAutoRestart));
            }

            if (!p.SolverTuningUnlocked)
            {
                list.Add(new Candidate("Solver Tuning", p.GetSolverTuningUnlockCost(), p.BuySolverTuningUnlock));
            }

            if (!p.AgentsMenuUnlocked)
            {
                list.Add(new Candidate("Agents Menu", p.GetAgentsMenuUnlockCost(), p.BuyAgentsMenuUnlock));
            }

            if (p.AgentsMenuUnlocked && !p.ExtraAgentSlotUnlocked)
            {
                list.Add(new Candidate("Extra Agent Slot", p.GetExtraAgentSlotUpgradeCost(), p.BuyExtraAgentSlotUpgrade));
            }

            if (p.CanUnlockModifiersMenu && !p.ModifiersMenuUnlocked)
            {
                list.Add(new Candidate("Modifiers Menu", p.GetModifiersMenuUnlockCost(), p.BuyModifiersMenuUnlock));
            }

            if (!p.IsMaxSpeed)
            {
                list.Add(new Candidate($"Speed L{p.SpeedLevel + 1}", p.GetSpeedUpgradeCost(), p.BuySpeedUpgrade));
            }

            if (!p.IsMaxStackCapacity)
            {
                list.Add(new Candidate($"Stack Cap L{p.StackCapacityLevel + 1}", p.GetStackCapacityUpgradeCost(), p.BuyStackCapacityUpgrade));
            }

            if (!p.IsMaxQueuePreview)
            {
                list.Add(new Candidate("Queue Preview", p.GetQueuePreviewUpgradeCost(), p.BuyQueuePreviewUpgrade));
            }

            if (!p.IsMaxIncome)
            {
                list.Add(new Candidate("Chip Yield", p.GetIncomeUpgradeCost(), p.BuyIncomeUpgrade));
            }

            if (!p.IsMaxDifficulty)
            {
                list.Add(new Candidate($"Difficulty L{p.DifficultyLevel + 1}", p.GetDifficultyUpgradeCost(), p.BuyDifficultyUpgrade));
            }

            if (p.AgentsMenuUnlocked)
            {
                foreach (AgentDefinition agent in StackMergeProgression.Agents)
                {
                    if (!p.IsAgentUnlocked(agent.Id))
                    {
                        AgentId agentId = agent.Id;
                        list.Add(new Candidate($"Agent {agent.DisplayName}", agent.Cost, () => p.BuyOrToggleAgent(agentId)));
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
                        list.Add(new Candidate($"Modifier {modifier.DisplayName} L{p.GetModifierLevel(modifierId) + 1}", p.GetModifierUpgradeCost(modifierId), () => p.BuyModifierUpgrade(modifierId)));
                    }
                }
            }

            return list;
        }

        private string BuildSummary(StackMergeProgression progression, List<PurchaseRecord> purchases, int runs, bool ppoUnlocked)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Simulated {runs} runs  |  final chips {progression.Chips:N0}  |  total earned {progression.TotalChipsEarned:N0}");
            sb.AppendLine($"Prestiges: {progression.PrestigeCount:N0}  |  Insight {progression.ResearchInsight:N0}  |  Lifetime Insight {progression.LifetimeResearchInsight:N0}  |  Last prestige {progression.LastPrestigeInsight:N0}");
            sb.AppendLine($"PPO unlocked: {(ppoUnlocked ? $"YES at run {FirstRun(purchases, "Solver PPO")}" : "no")}");
            sb.AppendLine($"Highest block ever: {progression.HighestBlockEver}  |  best run score: {progression.BestRunScore:N0}");
            sb.AppendLine();

            sb.AppendLine("=== Key milestones (run # when bought) ===");
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
            AppendMilestone(sb, purchases, "Root Research", "Research Insight Amplifier");
            sb.AppendLine();

            sb.AppendLine("=== Full purchase timeline ===");
            foreach (PurchaseRecord record in purchases)
            {
                sb.AppendLine($"Run {record.Run,6}  |  {record.Label,-32}  {record.Cost,14:N0} {record.Currency,-7}  (balance {record.BalanceAfter,14:N0})");
            }

            return sb.ToString();
        }

        private static void AppendMilestone(StringBuilder sb, List<PurchaseRecord> purchases, string label, string startsWith)
        {
            int run = FirstRun(purchases, startsWith);
            sb.AppendLine($"  {label,-22}: {(run > 0 ? $"run {run}" : "not reached")}");
        }

        private static int FirstRun(List<PurchaseRecord> purchases, string startsWith)
        {
            foreach (PurchaseRecord record in purchases)
            {
                if (record.Label.StartsWith(startsWith, StringComparison.Ordinal))
                {
                    return record.Run;
                }
            }

            return 0;
        }

        private string WriteFile(StringBuilder details, List<PurchaseRecord> purchases, StackMergeProgression progression, int runs)
        {
            try
            {
                string dir = Path.Combine(Application.dataPath, "..", "BenchmarkResults");
                Directory.CreateDirectory(dir);
                string path = Path.GetFullPath(Path.Combine(dir, $"progression_sim_{DateTime.Now:yyyyMMdd_HHmmss}.tsv"));

                var sb = new StringBuilder();
                sb.AppendLine("# Progression simulation");
                sb.AppendLine($"# runs={runs} seed={seed} moveCap={moveCap} manualProxy={manualProxy} stopAtPpo={stopAtPpo} simulateResearch={simulateResearch} targetPrestiges={targetPrestiges} ppoNormalRunsPerPrestige={ppoNormalRunsPerPrestige}");
                sb.AppendLine($"# finalChips={progression.Chips} totalEarned={progression.TotalChipsEarned} prestiges={progression.PrestigeCount} insight={progression.ResearchInsight} lifetimeInsight={progression.LifetimeResearchInsight}");
                sb.AppendLine("# Purchases:");
                sb.AppendLine("Run\tLabel\tCost\tCurrency\tBalanceAfter\tTotalEarnedAfter");
                foreach (PurchaseRecord record in purchases)
                {
                    sb.AppendLine($"{record.Run}\t{record.Label}\t{record.Cost}\t{record.Currency}\t{record.BalanceAfter}\t{record.TotalEarnedAfter}");
                }

                sb.AppendLine();
                sb.AppendLine("# Per-run economy (sampled):");
                sb.Append(details);

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

        private readonly struct RunResult
        {
            public RunResult(long score, int moves, int merges, int high, long moveIncome)
            {
                Score = score;
                Moves = moves;
                Merges = merges;
                High = high;
                MoveIncome = moveIncome;
            }

            public long Score { get; }

            public int Moves { get; }

            public int Merges { get; }

            public int High { get; }

            public long MoveIncome { get; }
        }

        private readonly struct Candidate
        {
            public Candidate(string label, long cost, Func<bool> buy)
            {
                Label = label;
                Cost = cost;
                Buy = buy;
            }

            public string Label { get; }

            public long Cost { get; }

            public Func<bool> Buy { get; }
        }

        private readonly struct PurchaseRecord
        {
            public PurchaseRecord(int run, string label, long cost, long chipsAfter, long totalEarnedAfter)
                : this(run, label, cost, "chips", chipsAfter, totalEarnedAfter)
            {
            }

            public PurchaseRecord(int run, string label, long cost, string currency, long balanceAfter, long totalEarnedAfter)
            {
                Run = run;
                Label = label;
                Cost = cost;
                Currency = currency;
                BalanceAfter = balanceAfter;
                TotalEarnedAfter = totalEarnedAfter;
            }

            public int Run { get; }

            public string Label { get; }

            public long Cost { get; }

            public string Currency { get; }

            public long BalanceAfter { get; }

            public long TotalEarnedAfter { get; }
        }
    }
}
