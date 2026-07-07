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
    /// (respecting prerequisites) across every purchasable system: solvers, upgrades (including the
    /// Passive Production family and Compute Speed), agents, token packs, modifiers and research.
    ///
    /// On top of the run counter it keeps a virtual real-time clock: every move costs the real
    /// <see cref="StackMergeProgression.GetMoveInterval"/> (so Speed / Compute Speed / Overclocker /
    /// PPO training pacing all matter), every game-over costs the restart delay (auto-restart tokens
    /// are consumed like in the game), PPO training frames cost training-mode move intervals, and the
    /// Passive Production ticker is fed the same simulated seconds. Phase milestones (Auto Solve,
    /// Agents, Modifiers, PPO, Prestige, ...) are stamped with run number AND simulated wall-clock
    /// time per prestige cycle, so the report directly shows how long a playthrough takes and how
    /// much the permanent research bonuses shorten each subsequent cycle.
    /// </summary>
    public sealed class StackMergeProgressionSimulatorWindow : EditorWindow
    {
        // Mirrors StackMergeGameBootstrap.AutoRestartDelay — keep in sync.
        private const double AutoRestartDelaySeconds = 1.2;
        // Keep at least this many restart tokens banked before spending chips on other purchases.
        private const int TokenReserve = 25;

        // Solvers preferred for grinding income — fastest strong solvers first. The two slowest
        // (MCTS, MOCA+) are skipped so the simulation stays fast; a grinding player uses a quick
        // solver anyway. PPO is excluded (training-only / endgame).
        private static readonly SolverId[] IncomeSolverPreference =
        {
            SolverId.Combo, SolverId.Plan5, SolverId.Plan3, SolverId.Look,
            SolverId.Moca, SolverId.Heur, SolverId.AntiStall, SolverId.Merge,
            SolverId.Balance, SolverId.Rand
        };

        // Which agents to keep equipped when there are more hired agents than slots (2, or 3 with
        // the Extra Slot). Ordered by chips-per-real-second impact: Overclocker shortens every move
        // interval (+33% throughput), Merge Broker boosts the dominant merge income, Velocity Trader's
        // end-of-run throughput bonus is large once move intervals get short. Restart Sponsor is last:
        // token packs are cheap relative to late-game income, so a slot is worth more than the tokens.
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
        private Vector2 scroll;
        private string summary = "Configure and press Run.";
        private string lastPath = string.Empty;

        [MenuItem("Tools/Stack Merge/Progression Simulator")]
        public static void Open()
        {
            GetWindow<StackMergeProgressionSimulatorWindow>("Progression Sim").minSize = new Vector2(520f, 560f);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Progression Simulator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Plays the real economy run-by-run with an auto-buyer (cheapest affordable first) covering solvers, " +
                "all upgrades, agents (best loadout equipped), token packs, modifiers and research. Tracks a virtual " +
                "real-time clock (move intervals, restart delays, PPO training pacing, passive production) and reports " +
                "when each phase is reached per prestige cycle — both in runs and in simulated wall-clock time.",
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
            wallClockLimitSeconds = Mathf.Clamp(EditorGUILayout.IntField(new GUIContent("Editor time limit (s)", "Safety cap on how long the simulation may hog the editor."), wallClockLimitSeconds), 30, 14400);

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
            var sim = new SimState();

            var details = new StringBuilder();
            details.AppendLine("Run\tSimTime\tChips\tInsight\tCycleInsight\tPrestiges\tGrossIncome\tCumEarned\tSolver\tScore\tMoves\tHigh\tBought");

            int run = 0;
            bool ppoUnlocked = false;
            int sampleEvery = Mathf.Max(1, maxRuns / 400);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            while (run < maxRuns && !(stopAtPpo && ppoUnlocked) && !ResearchTargetReached(progression))
            {
                run++;
                bool researchLayer = simulateResearch && !stopAtPpo && progression.IsSolverUnlocked(SolverId.MachineLearning);
                long gross = 0;
                string activity;
                RunResult result = default;
                SolverId solverId = manualProxy;

                if (researchLayer && !progression.MachineLearningPlayingModeUnlocked)
                {
                    // PPO training phase. It is EXCLUSIVE like in the real game: the board plays PPO in
                    // training mode, so there is no chip income and Passive Production is suspended.
                    activity = AdvancePpoTraining(progression, sim, run);
                    solverId = SolverId.MachineLearning;
                }
                else if (researchLayer)
                {
                    // PPO Normal-mode phase: batch of Normal runs (estimated performance, real time
                    // cost per move), then prestige once Insight is on the table.
                    activity = AdvancePpoNormalAndPrestige(progression, sim, run, rng);
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

                    result = PlayRun(progression, solvers, solverId, rng);
                    double moveInterval = auto ? progression.GetMoveInterval(solverId) : manualSecondsPerMove;
                    double runSeconds = result.Moves * moveInterval;
                    sim.Clock += runSeconds;
                    progression.TickPassiveProduction((float)runSeconds, true);

                    long runBonus = progression.AwardRunCompleted(result.Score, solverId, result.Moves, result.Merges, result.High, !auto, (float)runSeconds);
                    gross = result.MoveIncome + runBonus;

                    double restartSeconds = ConsumeRestartDelay(progression, auto);
                    sim.Clock += restartSeconds;
                    progression.TickPassiveProduction((float)restartSeconds, false);
                    activity = string.Empty;
                }

                string bought = AutoBuy(progression, sim, run);
                if (!string.IsNullOrEmpty(activity))
                {
                    bought = string.IsNullOrEmpty(bought) ? activity : $"{bought}, {activity}";
                }

                ApplyAgentLoadout(progression);
                RecordPhases(progression, sim, run);
                ppoUnlocked = progression.IsSolverUnlocked(SolverId.MachineLearning);

                if (run % sampleEvery == 0 || bought.Length > 0 || run <= 12)
                {
                    long earned = progression.TotalChipsEarned;
                    details.AppendLine($"{run}\t{FormatTime(sim.Clock)}\t{progression.Chips}\t{progression.ResearchInsight}\t{progression.ResearchInsightEarnedThisPrestige}\t{progression.PrestigeCount}\t{gross}\t{earned}\t{solverId}\t{result.Score}\t{result.Moves}\t{result.High}\t{bought}");
                }

                if (stopwatch.Elapsed.TotalSeconds > wallClockLimitSeconds)
                {
                    CloseOpenCycle(sim, progression, run);
                    summary = $"Stopped after {run} runs ({stopwatch.Elapsed.TotalSeconds:0}s safety limit). Raise 'Editor time limit' or reduce Max runs.\n\n" + BuildSummary(progression, sim, run, false);
                    lastPath = WriteFile(details, sim, progression, run);
                    Repaint();
                    return;
                }
            }

            CloseOpenCycle(sim, progression, run);
            summary = BuildSummary(progression, sim, run, ppoUnlocked);
            lastPath = WriteFile(details, sim, progression, run);
            Repaint();
        }

        private RunResult PlayRun(StackMergeProgression progression, IStackMergeSolver[] solvers, SolverId solverId, System.Random rng)
        {
            StackMergeRunModifiers modifiers = progression.BuildRunModifiers();
            var state = new StackMergeGameState(
                stackCapacity: progression.StackCapacity,
                queueLength: progression.QueueLength,
                difficultyLevel: progression.DifficultyLevel,
                scalingFrequencyLevel: progression.ScalingFrequencyLevel,
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

        /// <summary>
        /// Advances the clock across one game-over → next-run gap. Auto Restart takes 1.2s and eats a
        /// token exactly like the game does; without a token (or without Auto Restart) the player
        /// restarts by hand.
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
        /// PPO training phase step: injects a chunk of training frames and charges the clock the real
        /// training-mode move interval per frame. No chips are earned while training (matches the game).
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
                // Each training run ends in the artificial "Evaluating…" pause (research-reducible),
                // then restarts for free — so training time = moves + per-run evaluation pauses.
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
        /// PPO Normal-mode phase step: a batch of Normal runs (estimated result, real per-move time in
        /// non-training pacing, restart delays and token use included), then prestige + research buys.
        /// </summary>
        private string AdvancePpoNormalAndPrestige(StackMergeProgression progression, SimState sim, int run, System.Random rng)
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
                // Normal-mode runs trickle Insight directly (Passive Insight mechanic) on top of the
                // prestige payout — surface it so the Insight bookkeeping in the report adds up.
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
            if (preview > 0 && progression.PrestigeCount < targetPrestiges)
            {
                long gained = progression.ExecutePrestige();
                sim.Purchases.Add(new PurchaseRecord(run, sim.Clock, $"Prestige +{gained} Insight", 0, "Insight", progression.ResearchInsight, progression.TotalChipsEarned));
                RecordPhase(sim, $"Prestige #{progression.PrestigeCount}", run);
                events.Add($"Prestige +{gained}");
                StartNewCycle(sim, run, gained, progression.PrestigeCount);

                string research = AutoBuyResearch(progression, sim, run);
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

        private string AutoBuy(StackMergeProgression progression, SimState sim, int run)
        {
            var boughtThisRun = new List<string>();

            // Token maintenance first: Auto Restart is worthless without tokens, and the pack is a
            // repeatable purchase the cheapest-first loop must not see (it would buy packs forever).
            if (progression.AutoRestartUnlocked && !progression.AutoRestartIsTokenFree)
            {
                int packGuard = 0;
                while (progression.Tokens < TokenReserve
                       && progression.Chips >= progression.GetTokenPackCost() * 4
                       && packGuard++ < 10
                       && progression.BuyTokenPack())
                {
                    sim.Purchases.Add(new PurchaseRecord(run, sim.Clock, "Token Pack", progression.GetTokenPackCost(), progression.Chips, progression.TotalChipsEarned));
                    boughtThisRun.Add("Token Pack");
                }
            }

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

                sim.Purchases.Add(new PurchaseRecord(run, sim.Clock, cheapest.Value.Label, costPaid, progression.Chips, progression.TotalChipsEarned));
                boughtThisRun.Add(cheapest.Value.Label);
            }

            return string.Join(", ", boughtThisRun);
        }

        /// <summary>
        /// Keeps the best hired agents equipped. The game auto-equips whatever was bought first, but a
        /// real player swaps to the strongest loadout once slots are contended.
        /// </summary>
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

        private bool ResearchTargetReached(StackMergeProgression progression)
        {
            return simulateResearch
                && !stopAtPpo
                && targetPrestiges > 0
                && progression.PrestigeCount >= targetPrestiges;
        }

        private string AutoBuyResearch(StackMergeProgression progression, SimState sim, int run)
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

                sim.Purchases.Add(new PurchaseRecord(run, sim.Clock, cheapest.Value.Label, costPaid, "Insight", progression.ResearchInsight, progression.TotalChipsEarned));
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

            if (!p.IsMaxComputeSpeed)
            {
                list.Add(new Candidate($"Compute Speed L{p.ComputeSpeedLevel + 1}", p.GetComputeSpeedUpgradeCost(), p.BuyComputeSpeedUpgrade));
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

            if (!p.IsMaxScalingFrequency && p.ScalingFrequencyPurchasable)
            {
                list.Add(new Candidate($"Scaling Frequency L{p.ScalingFrequencyLevel + 1}", p.GetScalingFrequencyUpgradeCost(), p.BuyScalingFrequencyUpgrade));
            }

            if (!p.IsMaxProfitableEnding)
            {
                list.Add(new Candidate($"Profitable Ending L{p.ProfitableEndingLevel + 1}", p.GetProfitableEndingUpgradeCost(), p.BuyProfitableEndingUpgrade));
            }

            if (!p.IsMaxPassiveYield)
            {
                list.Add(new Candidate($"Passive Yield L{p.PassiveYieldLevel + 1}", p.GetPassiveYieldUpgradeCost(), p.BuyPassiveYieldUpgrade));
            }

            if (!p.IsMaxPassiveTickRate && p.PassiveSupportUpgradesUnlocked)
            {
                list.Add(new Candidate($"Passive Tick Rate L{p.PassiveTickRateLevel + 1}", p.GetPassiveTickRateUpgradeCost(), p.BuyPassiveTickRateUpgrade));
            }

            if (!p.IsMaxActiveMultiplier && p.PassiveSupportUpgradesUnlocked)
            {
                list.Add(new Candidate($"Active Multiplier L{p.ActiveMultiplierLevel + 1}", p.GetActiveMultiplierUpgradeCost(), p.BuyActiveMultiplierUpgrade));
            }

            if (p.AgentsMenuUnlocked)
            {
                foreach (AgentDefinition agent in StackMergeProgression.Agents)
                {
                    if (!p.IsAgentUnlocked(agent.Id))
                    {
                        AgentId agentId = agent.Id;
                        list.Add(new Candidate($"Agent {agent.DisplayName}", p.GetAgentCost(agentId), () => p.BuyOrToggleAgent(agentId)));
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

        private static void StartNewCycle(SimState sim, int run, long insightGained, int prestigeCount)
        {
            sim.Cycles.Add(new CycleRecord(sim.CycleIndex, sim.CycleStartRun, run, sim.CycleStartSeconds, sim.Clock, insightGained, prestigeCount));
            sim.CycleIndex++;
            sim.CycleStartSeconds = sim.Clock;
            sim.CycleStartRun = run + 1;
            sim.PhasesThisCycle.Clear();
        }

        private static void CloseOpenCycle(SimState sim, StackMergeProgression progression, int run)
        {
            if (run >= sim.CycleStartRun)
            {
                sim.Cycles.Add(new CycleRecord(sim.CycleIndex, sim.CycleStartRun, run, sim.CycleStartSeconds, sim.Clock, 0, progression.PrestigeCount) { Unfinished = true });
            }
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
        // Reporting
        // ---------------------------------------------------------------------------------------

        private string BuildSummary(StackMergeProgression progression, SimState sim, int runs, bool ppoUnlocked)
        {
            List<PurchaseRecord> purchases = sim.Purchases;
            var sb = new StringBuilder();
            sb.AppendLine($"Simulated {runs} runs  |  total sim time {FormatTime(sim.Clock)} (active play)  |  final chips {progression.Chips:N0}  |  total earned {progression.TotalChipsEarned:N0}");
            sb.AppendLine($"Prestiges: {progression.PrestigeCount:N0}  |  Insight {progression.ResearchInsight:N0}  |  Cycle Insight {progression.ResearchInsightEarnedThisPrestige:N0}  |  Lifetime Insight {progression.LifetimeResearchInsight:N0}  |  Last prestige {progression.LastPrestigeInsight:N0}");
            // ppoUnlocked only reflects the final state (a prestige resets it) — report the first
            // actual purchase instead so multi-prestige summaries don't claim "no".
            PurchaseRecord? firstPpo = FindFirst(purchases, "Solver PPO");
            sb.AppendLine($"PPO first unlocked: {(firstPpo.HasValue ? $"run {firstPpo.Value.Run} ({FormatTime(firstPpo.Value.SimSeconds)})" : "never")}");
            sb.AppendLine($"Highest block ever: {progression.HighestBlockEver}  |  best run score: {progression.BestRunScore:N0}");
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

        private static int FirstRun(List<PurchaseRecord> purchases, string startsWith)
        {
            PurchaseRecord? record = FindFirst(purchases, startsWith);
            return record?.Run ?? 0;
        }

        private static string FirstTime(List<PurchaseRecord> purchases, string startsWith)
        {
            PurchaseRecord? record = FindFirst(purchases, startsWith);
            return record.HasValue ? FormatTime(record.Value.SimSeconds) : "-";
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

        private string WriteFile(StringBuilder details, SimState sim, StackMergeProgression progression, int runs)
        {
            try
            {
                string dir = Path.Combine(Application.dataPath, "..", "BenchmarkResults");
                Directory.CreateDirectory(dir);
                string path = Path.GetFullPath(Path.Combine(dir, $"progression_sim_{DateTime.Now:yyyyMMdd_HHmmss}.tsv"));

                var sb = new StringBuilder();
                sb.AppendLine("# Progression simulation");
                sb.AppendLine($"# runs={runs} seed={seed} moveCap={moveCap} manualProxy={manualProxy} manualSecPerMove={manualSecondsPerMove} manualRestartSec={manualRestartSeconds} stopAtPpo={stopAtPpo} simulateResearch={simulateResearch} targetPrestiges={targetPrestiges} ppoNormalRunsPerPrestige={ppoNormalRunsPerPrestige} ppoTrainingMovesPerRun={ppoTrainingMovesPerRun}");
                sb.AppendLine($"# finalChips={progression.Chips} totalEarned={progression.TotalChipsEarned} prestiges={progression.PrestigeCount} insight={progression.ResearchInsight} cycleInsight={progression.ResearchInsightEarnedThisPrestige} lifetimeInsight={progression.LifetimeResearchInsight} simSeconds={sim.Clock:0}");
                sb.AppendLine("# Cycles:");
                sb.AppendLine("Cycle\tStartRun\tEndRun\tStartSeconds\tEndSeconds\tDurationSeconds\tInsightGained\tUnfinished");
                foreach (CycleRecord cycle in sim.Cycles)
                {
                    sb.AppendLine($"{cycle.Index}\t{cycle.StartRun}\t{cycle.EndRun}\t{cycle.StartSeconds:0}\t{cycle.EndSeconds:0}\t{cycle.DurationSeconds:0}\t{cycle.InsightGained}\t{cycle.Unfinished}");
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
                sb.AppendLine("Run\tSimSeconds\tLabel\tCost\tCurrency\tBalanceAfter\tTotalEarnedAfter");
                foreach (PurchaseRecord record in sim.Purchases)
                {
                    sb.AppendLine($"{record.Run}\t{record.SimSeconds:0}\t{record.Label}\t{record.Cost}\t{record.Currency}\t{record.BalanceAfter}\t{record.TotalEarnedAfter}");
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

        // ---------------------------------------------------------------------------------------
        // Data holders
        // ---------------------------------------------------------------------------------------

        private sealed class SimState
        {
            public double Clock;
            public int CycleIndex;
            public double CycleStartSeconds;
            public int CycleStartRun = 1;
            public readonly List<PurchaseRecord> Purchases = new();
            public readonly List<PhaseRecord> Phases = new();
            public readonly HashSet<string> PhasesThisCycle = new();
            public readonly List<CycleRecord> Cycles = new();
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
            public PurchaseRecord(int run, double simSeconds, string label, long cost, long chipsAfter, long totalEarnedAfter)
                : this(run, simSeconds, label, cost, "chips", chipsAfter, totalEarnedAfter)
            {
            }

            public PurchaseRecord(int run, double simSeconds, string label, long cost, string currency, long balanceAfter, long totalEarnedAfter)
            {
                Run = run;
                SimSeconds = simSeconds;
                Label = label;
                Cost = cost;
                Currency = currency;
                BalanceAfter = balanceAfter;
                TotalEarnedAfter = totalEarnedAfter;
            }

            public int Run { get; }

            public double SimSeconds { get; }

            public string Label { get; }

            public long Cost { get; }

            public string Currency { get; }

            public long BalanceAfter { get; }

            public long TotalEarnedAfter { get; }
        }
    }
}
