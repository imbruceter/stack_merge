using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private int monteCarloSimulations = 2;
        private int rolloutDepth = 2;
        private int planningDepthLimit = 2;
        private int[] benchmarkTuningSlots = new int[SolverTuningSettings.MaxSlots];
        private int maxMovesPerRun = 700;
        private int maxSecondsPerRun = 3;
        private int maxSecondsPerSolver = 30;
        private int seed = 12345;
        private Vector2 scroll;
        private string lastOutput = "No benchmark run yet.";

        [MenuItem("Tools/Stack Merge/Solver Benchmark")]
        public static void Open()
        {
            GetWindow<StackMergeSolverBenchmarkWindow>("Solver Benchmark");
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
                runAllSolvers = EditorGUILayout.Toggle("Run all solvers", runAllSolvers);
                if (!runAllSolvers)
                {
                    selectedSolver = (SolverId)EditorGUILayout.EnumPopup("Solver", selectedSolver);
                }

                fastBenchmarkMode = EditorGUILayout.Toggle("Fast benchmark mode", fastBenchmarkMode);
                runCount = EditorGUILayout.IntSlider("Runs", runCount, 1, 2000);
                stackCapacity = EditorGUILayout.IntSlider("Stack capacity", stackCapacity, 2, StackMergeGameState.MaxStackCapacity);
                queueLength = EditorGUILayout.IntSlider("Queue length", queueLength, 1, StackMergeGameState.DefaultQueueLength + 2);
                difficultyLevel = EditorGUILayout.IntSlider("Difficulty", difficultyLevel, 0, 3);
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
                            benchmarkTuningSlots[i] = EditorGUILayout.IntSlider(parameter.DisplayName, benchmarkTuningSlots[i], SolverTuningSettings.MinValue, SolverTuningSettings.MaxValue);
                        }
                    }
                }

                maxMovesPerRun = EditorGUILayout.IntSlider("Max moves per run", maxMovesPerRun, 100, 10000);
                maxSecondsPerRun = EditorGUILayout.IntSlider("Max seconds per run", maxSecondsPerRun, 1, 60);
                maxSecondsPerSolver = EditorGUILayout.IntSlider("Max seconds per solver", maxSecondsPerSolver, 5, 600);
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
            SolverId[] solverIds = runAllSolvers
                ? StackMergeSolverCatalog.Definitions.Select(definition => definition.Id).ToArray()
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

        private BenchmarkSummary RunSolverBenchmark(SolverId solverId, System.Random seedRandom, int solverIndex, int solverCount, out bool canceled)
        {
            IStackMergeSolver solver = StackMergeSolverFactory.Create(solverId);
            var results = new List<BenchmarkRunResult>(runCount);
            Stopwatch solverStopwatch = Stopwatch.StartNew();
            canceled = false;
            bool solverTimedOut = false;

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
                BenchmarkRunResult result = RunSingleGame(solver, runSeed, solverStopwatch);
                results.Add(result);

                if (result.SolverTimedOut)
                {
                    solverTimedOut = true;
                    break;
                }
            }

            solverStopwatch.Stop();
            return BenchmarkSummary.Create(solverId, results, solverStopwatch.Elapsed, solverTimedOut, canceled, runCount);
        }

        private BenchmarkRunResult RunSingleGame(IStackMergeSolver solver, int runSeed, Stopwatch solverStopwatch)
        {
            var state = new StackMergeGameState(
                stackCapacity: stackCapacity,
                queueLength: queueLength,
                difficultyLevel: difficultyLevel,
                seed: runSeed);

            var context = new SolverContext(
                new System.Random(runSeed ^ 0x5f3759df),
                monteCarloSimulations,
                rolloutDepth,
                fastBenchmarkMode,
                fastBenchmarkMode ? planningDepthLimit : int.MaxValue,
                BuildBenchmarkTuning(solver.Id));

            Stopwatch runStopwatch = Stopwatch.StartNew();
            int moves = 0;
            bool runTimedOut = false;
            bool solverTimedOut = false;

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
                if (!decision.HasMove || !state.CanPlace(decision.StackIndex))
                {
                    break;
                }

                MoveResult result = state.PlaceNext(decision.StackIndex);
                if (!result.Accepted)
                {
                    break;
                }

                moves++;
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
                solverTimedOut);
        }

        private string BuildOutput(IReadOnlyList<BenchmarkSummary> rows, TimeSpan elapsed, bool canceled)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Stack Merge Solver Benchmark");
            builder.AppendLine($"Completed: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            builder.AppendLine($"Total time: {elapsed.TotalSeconds:0.00}s");
            builder.AppendLine($"Tuning: {BuildTuningSummary()}");
            if (canceled)
            {
                builder.AppendLine("Status: canceled by user");
            }

            builder.AppendLine();
            builder.AppendLine("Solver\tRuns\tMin\tMedian\tAvg\tMax\tAvgMoves\tAvgMerges\tBestHigh\tEnded%\tTimed%\tMoveCap%\tSecs\tNotes");

            foreach (BenchmarkSummary row in rows.OrderByDescending(row => row.MedianScore))
            {
                builder.AppendLine(
                    $"{row.SolverName}\t{row.Runs}/{row.TargetRuns}\t{row.MinScore}\t{row.MedianScore}\t{row.AverageScore:0}\t{row.MaxScore}\t{row.AverageMoves:0.0}\t{row.AverageMerges:0.0}\t{row.BestHighestMerged}\t{row.GameOverRate * 100:0}%\t{row.TimeoutRate * 100:0}%\t{row.MoveCapRate * 100:0}%\t{row.Elapsed.TotalSeconds:0.0}\t{row.Notes}");
            }

            if (rows.Count > 0)
            {
                BenchmarkSummary bestMedian = rows.OrderByDescending(row => row.MedianScore).First();
                BenchmarkSummary bestPeak = rows.OrderByDescending(row => row.MaxScore).First();
                builder.AppendLine();
                builder.AppendLine($"Best median: {bestMedian.SolverName} ({bestMedian.MedianScore})");
                builder.AppendLine($"Best peak: {bestPeak.SolverName} ({bestPeak.MaxScore})");
            }

            return builder.ToString();
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
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

            return string.Join(", ", tuningDefinition.Parameters.Select((parameter, index) => $"{parameter.DisplayName} {FormatSigned(benchmarkTuningSlots[index])}"));
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
                bool solverTimedOut)
            {
                Score = score;
                Moves = moves;
                Merges = merges;
                HighestMergedBlock = highestMergedBlock;
                GameOver = gameOver;
                HitMoveCap = hitMoveCap;
                RunTimedOut = runTimedOut;
                SolverTimedOut = solverTimedOut;
            }

            public long Score { get; }

            public int Moves { get; }

            public int Merges { get; }

            public int HighestMergedBlock { get; }

            public bool GameOver { get; }

            public bool HitMoveCap { get; }

            public bool RunTimedOut { get; }

            public bool SolverTimedOut { get; }
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
                double averageMoves,
                double averageMerges,
                int bestHighestMerged,
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
                AverageMoves = averageMoves;
                AverageMerges = averageMerges;
                BestHighestMerged = bestHighestMerged;
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

            public double AverageMoves { get; }

            public double AverageMerges { get; }

            public int BestHighestMerged { get; }

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

                return new BenchmarkSummary(
                    solverId,
                    results.Count,
                    targetRuns,
                    orderedScores.Length > 0 ? orderedScores.First() : 0,
                    Median(orderedScores),
                    results.Count > 0 ? results.Average(result => result.Score) : 0,
                    orderedScores.Length > 0 ? orderedScores.Last() : 0,
                    results.Count > 0 ? results.Average(result => result.Moves) : 0,
                    results.Count > 0 ? results.Average(result => result.Merges) : 0,
                    results.Count > 0 ? results.Max(result => result.HighestMergedBlock) : 0,
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
