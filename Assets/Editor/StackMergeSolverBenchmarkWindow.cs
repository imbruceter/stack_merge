using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace StackMerge.Editor
{
    public sealed class StackMergeSolverBenchmarkWindow : EditorWindow
    {
        private bool benchmarkEnabled = true;
        private bool runAllSolvers = true;
        private bool fastBenchmarkMode = true;
        private SolverId selectedSolver = SolverId.Rand;
        private int runCount = 300;
        private int stackCapacity = StackMergeGameState.DefaultStackCapacity;
        private int queueLength = StackMergeGameState.DefaultQueueLength;
        private int difficultyLevel;
        private int scalingFrequencyLevel;
        private int monteCarloSimulations = 2;
        private int rolloutDepth = 2;
        private int planningDepthLimit = 2;
        private int[] benchmarkTuningSlots = new int[SolverTuningSettings.MaxSlots];
        private int[] benchmarkModifierLevels = new int[StackMergeProgression.Modifiers.Length];
        private int maxMovesPerRun = 700;
        private int maxSecondsPerRun = 3;
        private int maxSecondsPerSolver = 30;
        private int seed = 12345;
        private bool mlBenchmarkTrainingMode = true;
        private bool ppoEvaluationEnabled = true;
        private int ppoEvaluationInterval = 100;
        private int ppoEvaluationRuns = 3;
        private int ppoLogEveryNthRun = 1;
        private int ppoHiddenSize = 64;
        private int ppoMeasureAgents = 8;
        private int ppoMeasureBin = 500;
        private Vector2 scroll;
        private string lastOutput = "No benchmark run yet.";
        private string lastPpoDetailPath = string.Empty;
        private readonly List<MlBenchmarkRunLine> lastMlRunLines = new();

        [MenuItem("Tools/Stack Merge/Solver Benchmark")]
        public static void Open()
        {
            GetWindow<StackMergeSolverBenchmarkWindow>("Solver Benchmark");
        }

        /// <summary>
        /// Headless entry point for weight/tuning iteration:
        /// Unity -batchmode -quit -executeMethod StackMerge.Editor.StackMergeSolverBenchmarkWindow.RunSolverBenchmarkBatch
        ///   [-benchSolver STALL|all] [-benchRuns 100] [-benchMaxMods] [-benchMoveCap 1500]
        ///   [-benchSolverSecs 300] [-benchRunSecs 3] [-benchSeed 12345] [-benchDifficulty 5]
        ///   [-benchScalingFrequency 10] [-benchPlanningDepthLimit 5] [-benchFull]
        ///   [-benchTuning 0,0,3,0,0,0]
        /// Single-solver runs draw the same run seeds for a given seed, so before/after weight
        /// changes compare on identical boards. Summary TSV goes to BenchmarkResults/.
        /// </summary>
        public static void RunSolverBenchmarkBatch()
        {
            var runner = CreateInstance<StackMergeSolverBenchmarkWindow>();
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                runner.EnsureBenchmarkModifierArray();
                string solverArg = GetBatchString(args, "-benchSolver");
                runner.runAllSolvers = string.IsNullOrEmpty(solverArg) || solverArg.Equals("all", StringComparison.OrdinalIgnoreCase);
                if (!runner.runAllSolvers)
                {
                    SolverDefinition match = StackMergeSolverCatalog.Definitions
                        .FirstOrDefault(definition => definition.DisplayName.Equals(solverArg, StringComparison.OrdinalIgnoreCase));
                    if (string.IsNullOrEmpty(match.DisplayName))
                    {
                        throw new ArgumentException($"Unknown solver '{solverArg}'. Use a display name (RAND, MERG, ..., PPO) or 'all'.");
                    }

                    runner.selectedSolver = match.Id;
                }

                runner.runCount = Mathf.Clamp(GetBatchInt(args, "-benchRuns", 100), 1, 100000);
                // Calibration baseline plays on the maxed board (stack 10) — the shipped default
                // capacity would silently benchmark a different game.
                runner.stackCapacity = Mathf.Clamp(GetBatchInt(args, "-benchStackCap", 10), 2, StackMergeGameState.MaxStackCapacity);
                runner.queueLength = Mathf.Clamp(GetBatchInt(args, "-benchQueue", runner.queueLength), 1, StackMergeGameState.DefaultQueueLength + 2);
                runner.difficultyLevel = Mathf.Clamp(GetBatchInt(args, "-benchDifficulty", runner.difficultyLevel), 0, 5);
                runner.scalingFrequencyLevel = Mathf.Clamp(GetBatchInt(args, "-benchScalingFrequency", runner.scalingFrequencyLevel), 0, 10);
                runner.planningDepthLimit = Mathf.Clamp(GetBatchInt(args, "-benchPlanningDepthLimit", runner.planningDepthLimit), 1, 5);
                runner.fastBenchmarkMode = Array.IndexOf(args, "-benchFull") < 0;
                runner.maxMovesPerRun = Mathf.Clamp(GetBatchInt(args, "-benchMoveCap", 1500), 100, 10000);
                runner.maxSecondsPerSolver = Mathf.Clamp(GetBatchInt(args, "-benchSolverSecs", 300), 5, 36000);
                runner.maxSecondsPerRun = Mathf.Clamp(GetBatchInt(args, "-benchRunSecs", 3), 1, 60);
                runner.seed = GetBatchInt(args, "-benchSeed", runner.seed);
                if (Array.IndexOf(args, "-benchMaxMods") >= 0)
                {
                    for (int i = 0; i < StackMergeProgression.Modifiers.Length; i++)
                    {
                        runner.benchmarkModifierLevels[i] = StackMergeProgression.Modifiers[i].MaxLevel;
                    }
                }

                string tuningArg = GetBatchString(args, "-benchTuning");
                if (!string.IsNullOrEmpty(tuningArg))
                {
                    string[] parts = tuningArg.Split(',');
                    for (int i = 0; i < parts.Length && i < runner.benchmarkTuningSlots.Length; i++)
                    {
                        runner.benchmarkTuningSlots[i] = int.TryParse(parts[i].Trim(), out int value) ? value : 0;
                    }
                }

                runner.RunBenchmark();

                string dir = Path.Combine(Application.dataPath, "..", "BenchmarkResults");
                Directory.CreateDirectory(dir);
                string path = Path.GetFullPath(Path.Combine(dir, $"solver_benchmark_{DateTime.Now:yyyyMMdd_HHmmss}.txt"));
                File.WriteAllText(path, runner.lastOutput, Encoding.UTF8);
                UnityEngine.Debug.Log($"Solver benchmark summary written to: {path}");
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogException(exception);
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

        private static string GetBatchString(string[] args, string name)
        {
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }

        private static int GetBatchInt(string[] args, string name, int fallback)
        {
            string raw = GetBatchString(args, name);
            return !string.IsNullOrEmpty(raw) && int.TryParse(raw, out int value) ? value : fallback;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Stack Merge Solver Benchmark", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Fast benchmark mode caps expensive planning so 300-run samples stay useful for balance work. Disable it only for small, production-accurate spot checks.",
                MessageType.Info);
            EditorGUILayout.Space(6f);

            benchmarkEnabled = EditorGUILayout.Toggle("Demo benchmark mode", benchmarkEnabled);
            using (new EditorGUI.DisabledScope(!benchmarkEnabled))
            {
                EnsureBenchmarkModifierArray();
                runAllSolvers = EditorGUILayout.Toggle("Run all solvers", runAllSolvers);
                if (!runAllSolvers)
                {
                    selectedSolver = (SolverId)EditorGUILayout.EnumPopup("Solver", selectedSolver);
                }

                bool selectedPpo = !runAllSolvers && selectedSolver == SolverId.MachineLearning;
                fastBenchmarkMode = EditorGUILayout.Toggle("Fast benchmark mode", fastBenchmarkMode);
                runCount = EditorGUILayout.IntSlider("Runs", runCount, 1, selectedPpo ? 100000 : 2000);
                stackCapacity = EditorGUILayout.IntSlider("Stack capacity", stackCapacity, 2, StackMergeGameState.MaxStackCapacity);
                queueLength = EditorGUILayout.IntSlider("Queue length", queueLength, 1, StackMergeGameState.DefaultQueueLength + 2);
                difficultyLevel = EditorGUILayout.IntSlider("Difficulty max", difficultyLevel, 0, 5);
                scalingFrequencyLevel = EditorGUILayout.IntSlider("Scaling frequency", scalingFrequencyLevel, 0, 5);
                monteCarloSimulations = EditorGUILayout.IntSlider("MC simulations", monteCarloSimulations, 1, fastBenchmarkMode ? 12 : 40);
                rolloutDepth = EditorGUILayout.IntSlider("Rollout depth", rolloutDepth, 1, fastBenchmarkMode ? 8 : 30);
                using (new EditorGUI.DisabledScope(!fastBenchmarkMode))
                {
                    planningDepthLimit = EditorGUILayout.IntSlider("Planning depth cap", planningDepthLimit, 1, 5);
                }

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Solver tuning", EditorStyles.boldLabel);
                if (runAllSolvers)
                {
                    EditorGUILayout.HelpBox("Tuning sliders are available when benchmarking one selected solver.", MessageType.None);
                }
                else
                {
                    SolverTuningDefinition tuningDefinition = StackMergeSolverCatalog.GetTuningDefinition(selectedSolver);
                    if (!tuningDefinition.HasParameters)
                    {
                        EditorGUILayout.HelpBox("This solver has no tuning parameters.", MessageType.None);
                    }
                    else
                    {
                        for (int i = 0; i < tuningDefinition.Parameters.Length; i++)
                        {
                            SolverTuningParameterDefinition parameter = tuningDefinition.Parameters[i];
                            float displayValue = parameter.ToDisplayValue(SolverTuningSettings.ClampValue(selectedSolver, i, benchmarkTuningSlots[i]));
                            displayValue = parameter.WholeNumbers
                                ? EditorGUILayout.IntSlider(parameter.DisplayName, Mathf.RoundToInt(displayValue), Mathf.RoundToInt(parameter.MinDisplayValue), Mathf.RoundToInt(parameter.MaxDisplayValue))
                                : EditorGUILayout.Slider(parameter.DisplayName, displayValue, parameter.MinDisplayValue, parameter.MaxDisplayValue);
                            benchmarkTuningSlots[i] = parameter.FromDisplayValue(displayValue);
                        }
                    }
                }

                if (selectedPpo)
                {
                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("PPO benchmark", EditorStyles.boldLabel);
                    mlBenchmarkTrainingMode = EditorGUILayout.Toggle("Training mode learning", mlBenchmarkTrainingMode);
                    ppoHiddenSize = Mathf.Clamp(EditorGUILayout.IntField("PPO brain size (hidden)", ppoHiddenSize), 16, 512);
                    ppoEvaluationEnabled = EditorGUILayout.Toggle("Greedy evaluation", ppoEvaluationEnabled);
                    using (new EditorGUI.DisabledScope(!ppoEvaluationEnabled))
                    {
                        ppoEvaluationInterval = EditorGUILayout.IntSlider("Eval every N runs", ppoEvaluationInterval, 10, 5000);
                        ppoEvaluationRuns = EditorGUILayout.IntSlider("Eval runs", ppoEvaluationRuns, 1, 50);
                    }

                    ppoLogEveryNthRun = EditorGUILayout.IntSlider("UI log every N runs", ppoLogEveryNthRun, 1, 1000);
                    EditorGUILayout.HelpBox("PPO supports larger training runs. The full per-run log is written to a TSV file; the UI can show every Nth training row plus evaluation rows.", MessageType.Info);

                    EditorGUILayout.Space(4f);
                    EditorGUILayout.LabelField("Parallel learning measurement", EditorStyles.boldLabel);
                    ppoMeasureAgents = Mathf.Clamp(EditorGUILayout.IntField("Parallel agents", ppoMeasureAgents), 1, 64);
                    ppoMeasureBin = Mathf.Clamp(EditorGUILayout.IntField("Bin size (runs)", ppoMeasureBin), 50, 100000);
                    EditorGUILayout.HelpBox("Trains N independent PPO agents in parallel across all CPU cores and reports the AVERAGED learning curve (avg/peak High & Score per bin). Uses 'Runs' per agent and the config above. Far more measurement data per wall-clock than the single-threaded benchmark.", MessageType.Info);
                    if (GUILayout.Button("Measure PPO learning (parallel)", GUILayout.Height(28f)))
                    {
                        MeasurePpoLearningParallel();
                    }
                }

                EditorGUILayout.Space(4f);
                EditorGUILayout.LabelField("Run modifiers", EditorStyles.boldLabel);
                for (int i = 0; i < StackMergeProgression.Modifiers.Length; i++)
                {
                    ModifierDefinition modifier = StackMergeProgression.Modifiers[i];
                    benchmarkModifierLevels[i] = EditorGUILayout.IntSlider(modifier.DisplayName, benchmarkModifierLevels[i], 0, modifier.MaxLevel);
                }

                maxMovesPerRun = EditorGUILayout.IntSlider("Max moves per run", maxMovesPerRun, 100, 10000);
                maxSecondsPerRun = EditorGUILayout.IntSlider("Max seconds per run", maxSecondsPerRun, 1, 60);
                maxSecondsPerSolver = EditorGUILayout.IntSlider("Max seconds per solver", maxSecondsPerSolver, 5, selectedPpo ? 36000 : 600);
                seed = EditorGUILayout.IntField("Seed", seed);

                EditorGUILayout.Space(8f);
                if (GUILayout.Button("Run Benchmark", GUILayout.Height(34f)))
                {
                    RunBenchmark();
                }
            }

            EditorGUILayout.Space(8f);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.TextArea(lastOutput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void RunBenchmark()
        {
            EnsureBenchmarkModifierArray();
            lastMlRunLines.Clear();
            lastPpoDetailPath = string.Empty;
            SolverId[] solverIds = runAllSolvers
                ? StackMergeSolverCatalog.Definitions.Where(definition => definition.Available).Select(definition => definition.Id).ToArray()
                : new[] { selectedSolver };

            var random = new System.Random(seed);
            var rows = new List<BenchmarkSummary>(solverIds.Length);
            Stopwatch totalStopwatch = Stopwatch.StartNew();
            bool canceled = false;

            try
            {
                for (int solverIndex = 0; solverIndex < solverIds.Length; solverIndex++)
                {
                    SolverId solverId = solverIds[solverIndex];
                    BenchmarkSummary summary = RunSolverBenchmark(solverId, random, solverIndex, solverIds.Length, out canceled);
                    rows.Add(summary);

                    if (canceled)
                    {
                        break;
                    }
                }
            }
            finally
            {
                totalStopwatch.Stop();
                EditorUtility.ClearProgressBar();
            }

            lastOutput = BuildOutput(rows, totalStopwatch.Elapsed, canceled);
            UnityEngine.Debug.Log(lastOutput);
        }

        private void EnsureBenchmarkModifierArray()
        {
            if (benchmarkModifierLevels != null && benchmarkModifierLevels.Length == StackMergeProgression.Modifiers.Length)
            {
                return;
            }

            int[] migrated = new int[StackMergeProgression.Modifiers.Length];
            if (benchmarkModifierLevels != null)
            {
                for (int i = 0; i < benchmarkModifierLevels.Length && i < migrated.Length; i++)
                {
                    migrated[i] = benchmarkModifierLevels[i];
                }
            }

            benchmarkModifierLevels = migrated;
        }

        private BenchmarkSummary RunSolverBenchmark(SolverId solverId, System.Random seedRandom, int solverIndex, int solverCount, out bool canceled)
        {
            IStackMergeSolver solver = StackMergeSolverFactory.Create(solverId);
            var results = new List<BenchmarkRunResult>(runCount);
            Stopwatch solverStopwatch = Stopwatch.StartNew();
            canceled = false;
            bool solverTimedOut = false;
            StackMergePpoAgent ppoAgent = solverId == SolverId.MachineLearning
                ? new StackMergePpoAgent(new StackMergePpoTrainingData { hiddenSize = ppoHiddenSize }, seedRandom.Next())
                : null;
            StringBuilder ppoDetails = ppoAgent != null && !runAllSolvers ? CreatePpoDetailBuilder() : null;

            for (int i = 0; i < runCount; i++)
            {
                float solverProgress = (solverIndex + i / (float)Math.Max(1, runCount)) / Math.Max(1, solverCount);
                canceled = EditorUtility.DisplayCancelableProgressBar(
                    "Stack Merge Solver Benchmark",
                    $"{StackMergeSolverCatalog.GetDefinition(solverId).DisplayName}: run {i + 1}/{runCount}, elapsed {solverStopwatch.Elapsed.TotalSeconds:0.0}s",
                    solverProgress);

                if (canceled)
                {
                    break;
                }

                if (solverStopwatch.Elapsed.TotalSeconds >= maxSecondsPerSolver)
                {
                    solverTimedOut = true;
                    break;
                }

                int runSeed = seedRandom.Next();
                StackMergePpoMetrics ppoBefore = ppoAgent?.Metrics ?? default;
                BenchmarkRunResult result = RunSingleGame(
                    solver,
                    runSeed,
                    solverStopwatch,
                    ppoAgent,
                    mlBenchmarkTrainingMode,
                    ppoAgent != null);
                results.Add(result);
                if (solverId == SolverId.MachineLearning)
                {
                    StackMergePpoMetrics ppoAfter = ppoAgent?.Metrics ?? default;
                    AppendPpoDetailLine(ppoDetails, "Train", i + 1, 0, ppoBefore, ppoAfter, result);
                    if (!runAllSolvers)
                    {
                        AddVisiblePpoLine("Train", i + 1, 0, ppoBefore, ppoAfter, result);
                    }

                    if (!runAllSolvers
                        && ppoEvaluationEnabled
                        && ppoEvaluationInterval > 0
                        && (i + 1) % ppoEvaluationInterval == 0)
                    {
                        for (int evalIndex = 1; evalIndex <= ppoEvaluationRuns; evalIndex++)
                        {
                            if (solverStopwatch.Elapsed.TotalSeconds >= maxSecondsPerSolver)
                            {
                                solverTimedOut = true;
                                break;
                            }

                            StackMergePpoMetrics evalBefore = ppoAgent?.Metrics ?? default;
                            BenchmarkRunResult evalResult = RunSingleGame(
                                solver,
                                seedRandom.Next(),
                                solverStopwatch,
                                ppoAgent,
                                ppoTrainingMode: false,
                                ppoLearningEnabled: false);
                            StackMergePpoMetrics evalAfter = ppoAgent?.Metrics ?? default;
                            AppendPpoDetailLine(ppoDetails, "Eval", i + 1, evalIndex, evalBefore, evalAfter, evalResult);
                            AddVisiblePpoLine("Eval", i + 1, evalIndex, evalBefore, evalAfter, evalResult);
                        }
                    }
                }

                if (result.SolverTimedOut)
                {
                    solverTimedOut = true;
                    break;
                }
            }

            if (ppoDetails != null)
            {
                lastPpoDetailPath = WritePpoDetailFile(ppoDetails);
            }

            solverStopwatch.Stop();
            return BenchmarkSummary.Create(solverId, results, solverStopwatch.Elapsed, solverTimedOut, canceled, runCount);
        }

        private BenchmarkRunResult RunSingleGame(
            IStackMergeSolver solver,
            int runSeed,
            Stopwatch solverStopwatch,
            StackMergePpoAgent ppoAgent = null,
            bool ppoTrainingMode = false,
            bool ppoLearningEnabled = false)
        {
            var state = new StackMergeGameState(
                stackCapacity: stackCapacity,
                queueLength: queueLength,
                difficultyLevel: difficultyLevel,
                scalingFrequencyLevel: scalingFrequencyLevel,
                modifiers: BuildBenchmarkModifiers(),
                seed: runSeed);

            var context = new SolverContext(
                new System.Random(runSeed ^ 0x5f3759df),
                monteCarloSimulations,
                rolloutDepth,
                fastBenchmarkMode,
                fastBenchmarkMode ? planningDepthLimit : int.MaxValue,
                BuildBenchmarkTuning(solver.Id),
                IsBenchmarkModifierActive(ModifierId.NeuralAccelerator),
                ppoAgent,
                ppoTrainingMode);

            Stopwatch runStopwatch = Stopwatch.StartNew();
            int moves = 0;
            bool runTimedOut = false;
            bool solverTimedOut = false;

            // Economy-relevant per-run stats, mirroring the exact rules the game pays out on
            // (CalculateMoveIncomeBreakdown): combo streak advances on every accepted move
            // (+1 on merge moves, reset otherwise) and new-high triggers on merge moves whose
            // resulting top ties or beats the pre-move highest block.
            int comboStreak = 0;
            int maxComboStreak = 0;
            long effectiveStreakSum = 0;
            int mergeMoves = 0;
            int newHighEvents = 0;

            while (!state.IsGameOver && moves < maxMovesPerRun)
            {
                if (runStopwatch.Elapsed.TotalSeconds >= maxSecondsPerRun)
                {
                    runTimedOut = true;
                    break;
                }

                if (solverStopwatch.Elapsed.TotalSeconds >= maxSecondsPerSolver)
                {
                    solverTimedOut = true;
                    break;
                }

                SolverDecision decision = solver.ChooseMove(state, context);
                if (!SolverScoring.CanApplyDecision(state, decision))
                {
                    break;
                }

                MoveResult result = SolverScoring.ApplyDecision(state, decision);
                if (!result.Accepted)
                {
                    break;
                }

                if (result.MergeCount > 0)
                {
                    comboStreak++;
                    maxComboStreak = Math.Max(maxComboStreak, comboStreak);
                    effectiveStreakSum += Math.Min(comboStreak, 20);
                    mergeMoves++;
                    if (result.ResultingTopValue >= result.HighestBlock)
                    {
                        newHighEvents++;
                    }
                }
                else
                {
                    comboStreak = 0;
                }

                if (solver.Id == SolverId.MachineLearning && ppoLearningEnabled)
                {
                    ppoAgent?.Observe(result, state, ppoTrainingMode);
                }

                moves++;
            }

            if (solver.Id == SolverId.MachineLearning && ppoLearningEnabled)
            {
                ppoAgent?.ForceUpdate(ppoTrainingMode);
            }

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

            runStopwatch.Stop();
            return new BenchmarkRunResult(
                state.Score,
                state.BlocksDropped,
                state.TotalMerges,
                state.HighestMergedBlock,
                state.IsGameOver,
                moves >= maxMovesPerRun && !state.IsGameOver,
                runTimedOut,
                solverTimedOut,
                runStopwatch.Elapsed.TotalSeconds,
                maxComboStreak,
                mergeMoves > 0 ? effectiveStreakSum / (double)mergeMoves : 0,
                newHighEvents,
                strandedValue);
        }

        private string BuildOutput(IReadOnlyList<BenchmarkSummary> rows, TimeSpan elapsed, bool canceled)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Stack Merge Solver Benchmark");
            builder.AppendLine($"Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"Total time: {elapsed.TotalSeconds:0.00}s");
            builder.AppendLine($"Tuning: {BuildTuningSummary()}");
            builder.AppendLine($"Modifiers: {BuildModifierSummary()}");
            if (canceled)
            {
                builder.AppendLine("Status: canceled by user");
            }

            builder.AppendLine();
            builder.AppendLine("Solver\tRuns\tMin\tMedian\tAvg\tMax\tCV%\tAvgMoves\tAvgMerges\tMrg/Move\tAvgHigh\tBestHigh\tNewHighs\tAvgStreak\tMaxStreak\tStranded\tms/Move\tEnded%\tTimed%\tMoveCap%\tSecs\tNotes");

            foreach (BenchmarkSummary row in rows.OrderByDescending(row => row.MedianScore))
            {
                builder.AppendLine(
                    $"{row.SolverName}\t{row.Runs}/{row.TargetRuns}\t{row.MinScore}\t{row.MedianScore}\t{row.AverageScore:0}\t{row.MaxScore}\t{row.ScoreCv * 100:0.0}%\t{row.AverageMoves:0.0}\t{row.AverageMerges:0.0}\t{row.MergeRate:0.00}\t{row.AverageHighestMerged:0}\t{row.BestHighestMerged}\t{row.AverageNewHighEvents:0.0}\t{row.AverageMergeStreak:0.0}\t{row.AverageMaxComboStreak:0.0}\t{row.AverageStrandedValue:0}\t{row.AverageMsPerMove:0.00}\t{row.GameOverRate * 100:0}%\t{row.TimeoutRate * 100:0}%\t{row.MoveCapRate * 100:0}%\t{row.Elapsed.TotalSeconds:0.0}\t{row.Notes}");
            }

            if (rows.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("Stat champions (specialization check — runner-up in parentheses):");
                builder.AppendLine($"  Best median score: {Champion(rows, row => row.MedianScore, row => $"{row.MedianScore}")}");
                builder.AppendLine($"  Best peak score: {Champion(rows, row => row.MaxScore, row => $"{row.MaxScore}")}");
                builder.AppendLine($"  Most merges/run: {Champion(rows, row => row.AverageMerges, row => $"{row.AverageMerges:0.0}")}");
                builder.AppendLine($"  Highest merge density: {Champion(rows, row => row.MergeRate, row => $"{row.MergeRate:0.00}/move")}");
                builder.AppendLine($"  Longest runs (moves): {Champion(rows, row => row.AverageMoves, row => $"{row.AverageMoves:0.0}")}");
                builder.AppendLine($"  Highest avg block: {Champion(rows, row => row.AverageHighestMerged, row => $"{row.AverageHighestMerged:0}")}");
                builder.AppendLine($"  Most new-high events: {Champion(rows, row => row.AverageNewHighEvents, row => $"{row.AverageNewHighEvents:0.0}/run")}");
                builder.AppendLine($"  Best combo streak: {Champion(rows, row => row.AverageMergeStreak, row => $"{row.AverageMergeStreak:0.0} avg")}");
                builder.AppendLine($"  Most consistent (lowest CV): {Champion(rows, row => -row.ScoreCv, row => $"{row.ScoreCv * 100:0.0}%")}");
                builder.AppendLine($"  Cheapest CPU (ms/move): {Champion(rows, row => -row.AverageMsPerMove, row => $"{row.AverageMsPerMove:0.00} ms")}");
            }

            if (!runAllSolvers && selectedSolver == SolverId.MachineLearning && lastMlRunLines.Count > 0)
            {
                builder.AppendLine();
                if (!string.IsNullOrWhiteSpace(lastPpoDetailPath))
                {
                    builder.AppendLine($"Full PPO TSV: {lastPpoDetailPath}");
                }

                builder.AppendLine("PPO Learning Runs");
                builder.AppendLine("Phase\tRun\tEval\tUpdatesBefore\tUpdatesAfter\tProgressBefore\tProgressAfter\tAvgScore\tAvgMoves\tAvgMerges\tAvgHigh\tAvgReward\tPolicyLoss\tValueLoss\tEntropy\tScore\tMoves\tMerges\tHigh\tEnded\tTimed\tMoveCap\tSecs");
                foreach (MlBenchmarkRunLine line in lastMlRunLines)
                {
                    BenchmarkRunResult result = line.Result;
                    StackMergePpoMetrics before = line.Before;
                    StackMergePpoMetrics after = line.After;
                    builder.AppendLine(
                        $"{line.Phase}\t{line.RunIndex}\t{line.EvalIndex}\t{before.Updates}\t{after.Updates}\t{before.Progress * 100f:0.0}%\t{after.Progress * 100f:0.0}%\t{after.RecentAverageScore:0.0}\t{after.RecentAverageMoves:0.0}\t{after.RecentAverageMerges:0.0}\t{after.RecentAverageHigh:0.0}\t{after.RecentAverageReward:0.000}\t{after.LastPolicyLoss:0.000}\t{after.LastValueLoss:0.000}\t{after.LastEntropy:0.000}\t{result.Score}\t{result.Moves}\t{result.Merges}\t{result.HighestMergedBlock}\t{(result.GameOver ? "Y" : "N")}\t{(result.RunTimedOut || result.SolverTimedOut ? "Y" : "N")}\t{(result.HitMoveCap ? "Y" : "N")}\t{result.ElapsedSeconds:0.000}");
                }
            }

            return builder.ToString();
        }

        // Names the leader for one stat, with the runner-up in parentheses so the specialization
        // gap ("miben és mennyivel erősebb") is visible at a glance.
        private static string Champion(
            IReadOnlyList<BenchmarkSummary> rows,
            Func<BenchmarkSummary, double> rank,
            Func<BenchmarkSummary, string> display)
        {
            BenchmarkSummary[] ordered = rows.OrderByDescending(rank).ToArray();
            BenchmarkSummary best = ordered[0];
            if (ordered.Length < 2)
            {
                return $"{best.SolverName} ({display(best)})";
            }

            BenchmarkSummary second = ordered[1];
            return $"{best.SolverName} ({display(best)}) — 2. {second.SolverName} ({display(second)})";
        }

        private SolverTuningSettings BuildBenchmarkTuning(SolverId solverId)
        {
            return runAllSolvers
                ? SolverTuningSettings.Neutral(solverId)
                : new SolverTuningSettings(solverId, benchmarkTuningSlots);
        }

        private string BuildTuningSummary()
        {
            if (runAllSolvers)
            {
                return "neutral for all solvers";
            }

            SolverTuningDefinition tuningDefinition = StackMergeSolverCatalog.GetTuningDefinition(selectedSolver);
            if (!tuningDefinition.HasParameters)
            {
                return "no tuning";
            }

            return string.Join(", ", tuningDefinition.Parameters.Select((parameter, index) =>
                $"{parameter.DisplayName} {parameter.FormatValue(SolverTuningSettings.ClampValue(selectedSolver, index, benchmarkTuningSlots[index]))}"));
        }

        private StackMergeRunModifiers BuildBenchmarkModifiers()
        {
            return new StackMergeRunModifiers(
                benchmarkModifierLevels[(int)ModifierId.UnstableStack],
                benchmarkModifierLevels[(int)ModifierId.MirrorStack] > 0,
                benchmarkModifierLevels[(int)ModifierId.Joker] > 0,
                benchmarkModifierLevels[(int)ModifierId.MinersPickaxe],
                benchmarkModifierLevels[(int)ModifierId.QueueScrubber]);
        }

        private bool IsBenchmarkModifierActive(ModifierId modifierId)
        {
            int index = (int)modifierId;
            return index >= 0 && index < benchmarkModifierLevels.Length && benchmarkModifierLevels[index] > 0;
        }

        // ONE shared PPO model trained with parallel experience collection (A2C-style): N worker
        // agents play in parallel each cycle, then their weights are averaged and broadcast back, so
        // the consensus model learns from N games per cycle. This produces a single CUMULATIVE
        // learning curve over the full effective run count, at roughly 1/N the wall-clock — unlike
        // independent agents, the training genuinely builds up across all the runs.
        private void MeasurePpoLearningParallel()
        {
            int agents = Mathf.Clamp(ppoMeasureAgents, 1, 64);
            int totalRuns = Mathf.Max(agents, runCount);
            int cycleGames = 8;
            int perCycle = agents * cycleGames;
            int cycles = (totalRuns + perCycle - 1) / perCycle;
            int bin = Mathf.Clamp(ppoMeasureBin, 50, 100000);
            int binCount = (totalRuns + bin - 1) / bin;
            int capacity = Mathf.Clamp(stackCapacity, 2, StackMergeGameState.MaxStackCapacity);
            int queue = Mathf.Clamp(queueLength, 1, StackMergeGameState.DefaultQueueLength + 2);
            int diff = Mathf.Clamp(difficultyLevel, 0, 5);
            int hidden = Mathf.Clamp(ppoHiddenSize, 16, 512);
            int moveCap = Mathf.Max(50, maxMovesPerRun);
            int baseSeed = seed;
            StackMergeRunModifiers modifiers = BuildBenchmarkModifiers();

            var pool = new StackMergePpoAgent[agents];
            var rngs = new System.Random[agents];
            for (int a = 0; a < agents; a++)
            {
                pool[a] = new StackMergePpoAgent(new StackMergePpoTrainingData { hiddenSize = hidden }, baseSeed + a * 7919 + 1);
                rngs[a] = new System.Random(baseSeed + a * 104729 + 17);
            }

            int[][] cycleHigh = new int[agents][];
            long[][] cycleScore = new long[agents][];
            for (int a = 0; a < agents; a++)
            {
                cycleHigh[a] = new int[cycleGames];
                cycleScore[a] = new long[cycleGames];
            }

            double[] binHigh = new double[binCount];
            double[] binScore = new double[binCount];
            long[] binCount2 = new long[binCount];
            int[] binPeak = new int[binCount];

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            long effective = 0;
            for (int cycle = 0; cycle < cycles; cycle++)
            {
                System.Threading.Tasks.Parallel.For(0, agents, a =>
                {
                    StackMergePpoAgent agent = pool[a];
                    System.Random rng = rngs[a];
                    for (int g = 0; g < cycleGames; g++)
                    {
                        var state = new StackMergeGameState(stackCapacity: capacity, queueLength: queue, difficultyLevel: diff, scalingFrequencyLevel: scalingFrequencyLevel, modifiers: modifiers, seed: rng.Next());
                        int moves = 0;
                        while (!state.IsGameOver && moves < moveCap)
                        {
                            SolverDecision decision = agent.ChooseMove(state, rng, true);
                            if (!decision.HasMove || !SolverScoring.CanApplyDecision(state, decision))
                            {
                                break;
                            }

                            MoveResult result = SolverScoring.ApplyDecision(state, decision);
                            if (!result.Accepted)
                            {
                                break;
                            }

                            agent.Observe(result, state, true);
                            moves++;
                        }

                        agent.ForceUpdate(true);
                        cycleHigh[a][g] = state.HighestMergedBlock;
                        cycleScore[a][g] = state.Score;
                    }
                });

                AverageAgentWeights(pool);

                for (int a = 0; a < agents; a++)
                {
                    for (int g = 0; g < cycleGames; g++)
                    {
                        int b = (int)Math.Min(binCount - 1, effective / bin);
                        binHigh[b] += cycleHigh[a][g];
                        binScore[b] += cycleScore[a][g];
                        binPeak[b] = Math.Max(binPeak[b], cycleHigh[a][g]);
                        binCount2[b]++;
                        effective++;
                    }
                }

                if (effective >= totalRuns)
                {
                    break;
                }
            }

            stopwatch.Stop();

            var sb = new StringBuilder();
            sb.AppendLine($"PPO learning measurement (shared model, {agents} parallel workers) | hidden {hidden} | cap {capacity} q{queue} diff {diff}");
            sb.AppendLine($"Modifiers: {BuildModifierSummary()}");
            sb.AppendLine($"Effective training runs: {effective:N0} in {stopwatch.Elapsed.TotalSeconds:0}s  ({effective / Math.Max(1.0, stopwatch.Elapsed.TotalSeconds):0} runs/s, ~{agents}x wall-clock speedup)");
            sb.AppendLine("Note: weights are averaged across workers each cycle (parallel SGD); the curve is the consensus model's cumulative progress.");
            sb.AppendLine();
            sb.AppendLine("EffectiveRuns\tAvgHigh\tAvgScore\tPeakHigh");
            for (int b = 0; b < binCount; b++)
            {
                if (binCount2[b] == 0)
                {
                    continue;
                }

                long from = (long)b * bin + 1;
                long to = Math.Min(effective, (long)(b + 1) * bin);
                sb.AppendLine($"{from}-{to}\t{binHigh[b] / binCount2[b]:0}\t{binScore[b] / binCount2[b]:0}\t{binPeak[b]}");
            }

            string text = sb.ToString();
            lastOutput = text;

            try
            {
                string dir = Path.Combine(Application.dataPath, "..", "BenchmarkResults");
                Directory.CreateDirectory(dir);
                string path = Path.GetFullPath(Path.Combine(dir, $"ppo_learning_{DateTime.Now:yyyyMMdd_HHmmss}.tsv"));
                File.WriteAllText(path, text);
                AssetDatabase.Refresh();
                lastOutput += $"\n\nSaved: {path}";
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"PPO learning measurement: failed to write file: {exception.Message}");
            }

            Repaint();
        }

        // Averages every weight/bias array across the worker pool in place (the agents' DenseNets
        // reference these same arrays, so this both averages and broadcasts the consensus weights).
        private static void AverageAgentWeights(StackMergePpoAgent[] pool)
        {
            if (pool.Length <= 1)
            {
                return;
            }

            StackMergePpoTrainingData[] data = pool.Select(p => p.Data).ToArray();
            AverageInPlace(data.Select(d => d.actorW1).ToArray());
            AverageInPlace(data.Select(d => d.actorB1).ToArray());
            AverageInPlace(data.Select(d => d.actorW2).ToArray());
            AverageInPlace(data.Select(d => d.actorB2).ToArray());
            AverageInPlace(data.Select(d => d.criticW1).ToArray());
            AverageInPlace(data.Select(d => d.criticB1).ToArray());
            AverageInPlace(data.Select(d => d.criticW2).ToArray());
            AverageInPlace(data.Select(d => d.criticB2).ToArray());
        }

        private static void AverageInPlace(float[][] arrays)
        {
            int n = arrays.Length;
            if (n == 0 || arrays[0] == null)
            {
                return;
            }

            int len = arrays[0].Length;
            for (int i = 0; i < len; i++)
            {
                double sum = 0;
                for (int a = 0; a < n; a++)
                {
                    sum += arrays[a][i];
                }

                float avg = (float)(sum / n);
                for (int a = 0; a < n; a++)
                {
                    arrays[a][i] = avg;
                }
            }
        }

        private string BuildModifierSummary()
        {
            return string.Join(", ", StackMergeProgression.Modifiers.Select((modifier, index) => $"{modifier.DisplayName} L{benchmarkModifierLevels[index]}"));
        }

        private StringBuilder CreatePpoDetailBuilder()
        {
            var builder = new StringBuilder();
            builder.AppendLine("Phase\tRun\tEval\tUpdatesBefore\tUpdatesAfter\tProgressBefore\tProgressAfter\tAvgScore\tAvgMoves\tAvgMerges\tAvgHigh\tAvgReward\tPolicyLoss\tValueLoss\tEntropy\tScore\tMoves\tMerges\tHigh\tEnded\tTimed\tMoveCap\tSecs");
            return builder;
        }

        private void AppendPpoDetailLine(
            StringBuilder builder,
            string phase,
            int runIndex,
            int evalIndex,
            StackMergePpoMetrics before,
            StackMergePpoMetrics after,
            BenchmarkRunResult result)
        {
            if (builder == null)
            {
                return;
            }

            builder.AppendLine(
                $"{phase}\t{runIndex}\t{evalIndex}\t{before.Updates}\t{after.Updates}\t{before.Progress * 100f:0.0}%\t{after.Progress * 100f:0.0}%\t{after.RecentAverageScore:0.0}\t{after.RecentAverageMoves:0.0}\t{after.RecentAverageMerges:0.0}\t{after.RecentAverageHigh:0.0}\t{after.RecentAverageReward:0.000}\t{after.LastPolicyLoss:0.000}\t{after.LastValueLoss:0.000}\t{after.LastEntropy:0.000}\t{result.Score}\t{result.Moves}\t{result.Merges}\t{result.HighestMergedBlock}\t{(result.GameOver ? "Y" : "N")}\t{(result.RunTimedOut || result.SolverTimedOut ? "Y" : "N")}\t{(result.HitMoveCap ? "Y" : "N")}\t{result.ElapsedSeconds:0.000}");
        }

        private void AddVisiblePpoLine(
            string phase,
            int runIndex,
            int evalIndex,
            StackMergePpoMetrics before,
            StackMergePpoMetrics after,
            BenchmarkRunResult result)
        {
            if (phase != "Train" || runIndex == 1 || runIndex == runCount || runIndex % Math.Max(1, ppoLogEveryNthRun) == 0)
            {
                lastMlRunLines.Add(new MlBenchmarkRunLine(phase, runIndex, evalIndex, before, after, result));
            }
        }

        private static string WritePpoDetailFile(StringBuilder builder)
        {
            try
            {
                string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BenchmarkResults"));
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, $"ppo_benchmark_{DateTime.Now:yyyyMMdd_HHmmss}.tsv");
                File.WriteAllText(path, builder.ToString(), Encoding.UTF8);
                return path;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"Could not write PPO benchmark TSV: {exception.Message}");
                return string.Empty;
            }
        }

        private readonly struct BenchmarkRunResult
        {
            public BenchmarkRunResult(
                long score,
                int moves,
                int merges,
                int highestMergedBlock,
                bool gameOver,
                bool hitMoveCap,
                bool runTimedOut,
                bool solverTimedOut,
                double elapsedSeconds,
                int maxComboStreak = 0,
                double averageMergeStreak = 0,
                int newHighEvents = 0,
                long strandedBoardValue = 0)
            {
                Score = score;
                Moves = moves;
                Merges = merges;
                HighestMergedBlock = highestMergedBlock;
                GameOver = gameOver;
                HitMoveCap = hitMoveCap;
                RunTimedOut = runTimedOut;
                SolverTimedOut = solverTimedOut;
                ElapsedSeconds = elapsedSeconds;
                MaxComboStreak = maxComboStreak;
                AverageMergeStreak = averageMergeStreak;
                NewHighEvents = newHighEvents;
                StrandedBoardValue = strandedBoardValue;
            }

            public long Score { get; }

            public int Moves { get; }

            public int Merges { get; }

            public int HighestMergedBlock { get; }

            public bool GameOver { get; }

            public bool HitMoveCap { get; }

            public bool RunTimedOut { get; }

            public bool SolverTimedOut { get; }

            public double ElapsedSeconds { get; }

            /// <summary>Longest uninterrupted merge streak in the run (raw, uncapped).</summary>
            public int MaxComboStreak { get; }

            /// <summary>Average Combo Engine-effective streak (capped at 20) over the run's merge moves.</summary>
            public double AverageMergeStreak { get; }

            /// <summary>Merge moves whose resulting top tied/beat the pre-move highest block (new-high payouts).</summary>
            public int NewHighEvents { get; }

            /// <summary>Block value left on the board at run end (Jokers excluded).</summary>
            public long StrandedBoardValue { get; }
        }

        private readonly struct MlBenchmarkRunLine
        {
            public MlBenchmarkRunLine(string phase, int runIndex, int evalIndex, StackMergePpoMetrics before, StackMergePpoMetrics after, BenchmarkRunResult result)
            {
                Phase = phase;
                RunIndex = runIndex;
                EvalIndex = evalIndex;
                Before = before;
                After = after;
                Result = result;
            }

            public string Phase { get; }

            public int RunIndex { get; }

            public int EvalIndex { get; }

            public StackMergePpoMetrics Before { get; }

            public StackMergePpoMetrics After { get; }

            public BenchmarkRunResult Result { get; }
        }

        private readonly struct BenchmarkSummary
        {
            private BenchmarkSummary(
                SolverId solverId,
                int runs,
                int targetRuns,
                long minScore,
                long medianScore,
                double averageScore,
                long maxScore,
                double scoreCv,
                double averageMoves,
                double averageMerges,
                double mergeRate,
                double averageHighestMerged,
                int bestHighestMerged,
                double averageNewHighEvents,
                double averageMergeStreak,
                double averageMaxComboStreak,
                double averageStrandedValue,
                double averageMsPerMove,
                double gameOverRate,
                double timeoutRate,
                double moveCapRate,
                TimeSpan elapsed,
                string notes)
            {
                SolverId = solverId;
                Runs = runs;
                TargetRuns = targetRuns;
                MinScore = minScore;
                MedianScore = medianScore;
                AverageScore = averageScore;
                MaxScore = maxScore;
                ScoreCv = scoreCv;
                AverageMoves = averageMoves;
                AverageMerges = averageMerges;
                MergeRate = mergeRate;
                AverageHighestMerged = averageHighestMerged;
                BestHighestMerged = bestHighestMerged;
                AverageNewHighEvents = averageNewHighEvents;
                AverageMergeStreak = averageMergeStreak;
                AverageMaxComboStreak = averageMaxComboStreak;
                AverageStrandedValue = averageStrandedValue;
                AverageMsPerMove = averageMsPerMove;
                GameOverRate = gameOverRate;
                TimeoutRate = timeoutRate;
                MoveCapRate = moveCapRate;
                Elapsed = elapsed;
                Notes = notes;
            }

            public SolverId SolverId { get; }

            public string SolverName => StackMergeSolverCatalog.GetDefinition(SolverId).DisplayName;

            public int Runs { get; }

            public int TargetRuns { get; }

            public long MinScore { get; }

            public long MedianScore { get; }

            public double AverageScore { get; }

            public long MaxScore { get; }

            /// <summary>Score coefficient of variation (σ/avg) — consistency across runs.</summary>
            public double ScoreCv { get; }

            public double AverageMoves { get; }

            public double AverageMerges { get; }

            /// <summary>Merges per move — merge density.</summary>
            public double MergeRate { get; }

            public double AverageHighestMerged { get; }

            public int BestHighestMerged { get; }

            public double AverageNewHighEvents { get; }

            public double AverageMergeStreak { get; }

            public double AverageMaxComboStreak { get; }

            public double AverageStrandedValue { get; }

            /// <summary>Average solver think+apply wall-clock per move — the CPU-cost axis.</summary>
            public double AverageMsPerMove { get; }

            public double GameOverRate { get; }

            public double TimeoutRate { get; }

            public double MoveCapRate { get; }

            public TimeSpan Elapsed { get; }

            public string Notes { get; }

            public static BenchmarkSummary Create(
                SolverId solverId,
                IReadOnlyList<BenchmarkRunResult> results,
                TimeSpan elapsed,
                bool solverTimedOut,
                bool canceled,
                int targetRuns)
            {
                long[] orderedScores = results.Select(result => result.Score).OrderBy(score => score).ToArray();
                string notes = canceled
                    ? "Canceled"
                    : solverTimedOut
                        ? "Stopped at solver time limit"
                        : results.Count < targetRuns
                            ? "Partial"
                            : string.Empty;

                double averageScore = results.Count > 0 ? results.Average(result => result.Score) : 0;
                double scoreVariance = results.Count > 0
                    ? results.Average(result => Math.Pow(result.Score - averageScore, 2))
                    : 0;
                double scoreCv = averageScore > 0 ? Math.Sqrt(scoreVariance) / averageScore : 0;
                double averageMoves = results.Count > 0 ? results.Average(result => result.Moves) : 0;
                double averageMerges = results.Count > 0 ? results.Average(result => result.Merges) : 0;
                double totalMoves = results.Sum(result => (double)result.Moves);

                return new BenchmarkSummary(
                    solverId,
                    results.Count,
                    targetRuns,
                    orderedScores.Length > 0 ? orderedScores.First() : 0,
                    Median(orderedScores),
                    averageScore,
                    orderedScores.Length > 0 ? orderedScores.Last() : 0,
                    scoreCv,
                    averageMoves,
                    averageMerges,
                    averageMoves > 0 ? averageMerges / averageMoves : 0,
                    results.Count > 0 ? results.Average(result => result.HighestMergedBlock) : 0,
                    results.Count > 0 ? results.Max(result => result.HighestMergedBlock) : 0,
                    results.Count > 0 ? results.Average(result => result.NewHighEvents) : 0,
                    results.Count > 0 ? results.Average(result => result.AverageMergeStreak) : 0,
                    results.Count > 0 ? results.Average(result => result.MaxComboStreak) : 0,
                    results.Count > 0 ? results.Average(result => result.StrandedBoardValue) : 0,
                    totalMoves > 0 ? results.Sum(result => result.ElapsedSeconds) * 1000.0 / totalMoves : 0,
                    results.Count > 0 ? results.Count(result => result.GameOver) / (double)results.Count : 0,
                    results.Count > 0 ? results.Count(result => result.RunTimedOut || result.SolverTimedOut) / (double)results.Count : 0,
                    results.Count > 0 ? results.Count(result => result.HitMoveCap) / (double)results.Count : 0,
                    elapsed,
                    notes);
            }

            private static long Median(long[] orderedValues)
            {
                if (orderedValues.Length == 0)
                {
                    return 0;
                }

                int middle = orderedValues.Length / 2;
                if (orderedValues.Length % 2 == 1)
                {
                    return orderedValues[middle];
                }

                return (long)Math.Round((orderedValues[middle - 1] + orderedValues[middle]) / 2.0);
            }
        }
    }
}
