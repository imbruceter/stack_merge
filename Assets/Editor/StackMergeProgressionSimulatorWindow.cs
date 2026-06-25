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
                "Reports the run number each thing is bought, up to PPO. Use it to tune costs/income to the target curve.",
                MessageType.Info);

            maxRuns = Mathf.Clamp(EditorGUILayout.IntField("Max runs", maxRuns), 10, 1000000);
            seed = EditorGUILayout.IntField("Seed", seed);
            moveCap = Mathf.Clamp(EditorGUILayout.IntField("Move cap / run", moveCap), 20, 5000);
            stopAtPpo = EditorGUILayout.Toggle("Stop when PPO unlocks", stopAtPpo);
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
            details.AppendLine("Run\tChips\tGrossIncome\tCumEarned\tSolver\tScore\tMoves\tHigh\tBought");

            long previousEarned = 0;
            int run = 0;
            bool ppoUnlocked = false;
            int sampleEvery = Mathf.Max(1, maxRuns / 400);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (run < maxRuns && !(stopAtPpo && ppoUnlocked))
            {
                run++;
                bool auto = progression.AutoSolveEnabled && progression.HasPurchasedSolver;
                SolverId solverId = auto ? BestIncomeSolver(progression) : manualProxy;

                RunResult result = PlayRun(progression, solvers, solverId, rng);
                long runBonus = progression.AwardRunCompleted(result.Score, solverId, result.Moves, result.Merges, result.High, !auto, 0f);
                long gross = result.MoveIncome + runBonus;

                string bought = AutoBuy(progression, purchases, run);
                ppoUnlocked = progression.IsSolverUnlocked(SolverId.MachineLearning);

                if (run % sampleEvery == 0 || bought.Length > 0 || run <= 12)
                {
                    long earned = progression.TotalChipsEarned;
                    details.AppendLine($"{run}\t{progression.Chips}\t{gross}\t{earned}\t{solverId}\t{result.Score}\t{result.Moves}\t{result.High}\t{bought}");
                    previousEarned = earned;
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
            sb.AppendLine();

            sb.AppendLine("=== Full purchase timeline ===");
            foreach (PurchaseRecord record in purchases)
            {
                sb.AppendLine($"Run {record.Run,6}  |  {record.Label,-26}  {record.Cost,14:N0}  (chips left {record.ChipsAfter,14:N0})");
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
                sb.AppendLine($"# runs={runs} seed={seed} moveCap={moveCap} manualProxy={manualProxy}");
                sb.AppendLine("# Purchases:");
                sb.AppendLine("Run\tLabel\tCost\tChipsAfter\tTotalEarnedAfter");
                foreach (PurchaseRecord record in purchases)
                {
                    sb.AppendLine($"{record.Run}\t{record.Label}\t{record.Cost}\t{record.ChipsAfter}\t{record.TotalEarnedAfter}");
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
            {
                Run = run;
                Label = label;
                Cost = cost;
                ChipsAfter = chipsAfter;
                TotalEarnedAfter = totalEarnedAfter;
            }

            public int Run { get; }

            public string Label { get; }

            public long Cost { get; }

            public long ChipsAfter { get; }

            public long TotalEarnedAfter { get; }
        }
    }
}
