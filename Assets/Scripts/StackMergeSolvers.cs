using System;
using System.Collections.Generic;
using System.Linq;
using static StackMerge.SolverScoring;

namespace StackMerge
{
    public enum SolverId
    {
        Rand = 0,
        Merge = 1,
        Balance = 2,
        Heur = 3,
        Look = 4,
        Moca = 5,
        Plan3 = 6,
        Plan5 = 7,
        MocaPlus = 8,
        Mcts = 9,
        AntiStall = 10,
        Combo = 11
    }

    public readonly struct SolverDefinition
    {
        public SolverDefinition(SolverId id, string displayName, long cost, string lockedHint, string description)
        {
            Id = id;
            DisplayName = displayName;
            Cost = cost;
            LockedHint = lockedHint;
            Description = description;
        }

        public SolverId Id { get; }

        public string DisplayName { get; }

        public long Cost { get; }

        public string LockedHint { get; }

        public string Description { get; }
    }

    public static class StackMergeSolverCatalog
    {
        public static readonly SolverDefinition[] Definitions =
        {
            new(SolverId.Rand, "RAND", 0, "Free baseline solver.", "Randomly chooses any valid stack. Weak, chaotic, but fast."),
            new(SolverId.Merge, "MERG", 75, "Looks for direct merges.", "Prioritizes immediate merges and cascades before anything else."),
            new(SolverId.Balance, "BAL", 160, "Keeps stacks even.", "Avoids tall dangerous stacks and spreads risk across the board."),
            new(SolverId.Heur, "HEUR", 320, "Scores every legal move.", "Uses handcrafted heuristics: merge value, danger, future queue fit, and free space."),
            new(SolverId.Look, "LOOK", 700, "Plans one move deeper.", "Tests each move, then estimates the best follow-up move before deciding."),
            new(SolverId.Moca, "MOCA", 1400, "Runs simulations.", "Monte Carlo solver: rolls out multiple futures and picks the best average result."),
            new(SolverId.Plan3, "PLAN-3", 950, "Reads the visible queue.", "Queue planner: searches lines through up to 3 visible next blocks before choosing."),
            new(SolverId.Plan5, "PLAN-5", 2400, "Uses the extended queue.", "Deep queue planner: searches lines through up to 5 visible next blocks. Stronger once next preview upgrades are unlocked."),
            new(SolverId.MocaPlus, "MOCA+", 3600, "Smarter Monte Carlo rollouts.", "Enhanced Monte Carlo: each rollout uses short queue planning and an anti-stall board score."),
            new(SolverId.Mcts, "MCTS", 6200, "Builds a search tree.", "Monte Carlo Tree Search: balances exploring new lines with exploiting lines that already score well."),
            new(SolverId.AntiStall, "STALL", 1250, "Avoids dead boards.", "Anti-stall solver: heavily protects legal moves, semi-empty stacks, and escape routes over greedy merges."),
            new(SolverId.Combo, "COMBO", 1800, "Sets up chain merges.", "Combo-focused solver: reads the queue and rewards positions that can cascade over the next 2-3 turns.")
        };

        public static SolverDefinition GetDefinition(SolverId id)
        {
            int index = (int)id;
            if (index < 0)
            {
                index = 0;
            }

            if (index >= Definitions.Length)
            {
                index = Definitions.Length - 1;
            }

            return Definitions[index];
        }
    }

    public interface IStackMergeSolver
    {
        SolverId Id { get; }

        string DisplayName { get; }

        SolverDecision ChooseMove(StackMergeGameState state, SolverContext context);
    }

    public static class StackMergeSolverFactory
    {
        public static IStackMergeSolver[] CreateAll()
        {
            return new IStackMergeSolver[]
            {
                new RandomStackMergeSolver(),
                new MergeFirstStackMergeSolver(),
                new BalancedStackMergeSolver(),
                new HeuristicStackMergeSolver(),
                new LookaheadStackMergeSolver(),
                new MonteCarloStackMergeSolver(),
                new Plan3StackMergeSolver(),
                new Plan5StackMergeSolver(),
                new EnhancedMonteCarloStackMergeSolver(),
                new MctsStackMergeSolver(),
                new AntiStallStackMergeSolver(),
                new ComboFocusedStackMergeSolver()
            };
        }

        public static IStackMergeSolver Create(SolverId solverId)
        {
            return solverId switch
            {
                SolverId.Merge => new MergeFirstStackMergeSolver(),
                SolverId.Balance => new BalancedStackMergeSolver(),
                SolverId.Heur => new HeuristicStackMergeSolver(),
                SolverId.Look => new LookaheadStackMergeSolver(),
                SolverId.Moca => new MonteCarloStackMergeSolver(),
                SolverId.Plan3 => new Plan3StackMergeSolver(),
                SolverId.Plan5 => new Plan5StackMergeSolver(),
                SolverId.MocaPlus => new EnhancedMonteCarloStackMergeSolver(),
                SolverId.Mcts => new MctsStackMergeSolver(),
                SolverId.AntiStall => new AntiStallStackMergeSolver(),
                SolverId.Combo => new ComboFocusedStackMergeSolver(),
                _ => new RandomStackMergeSolver()
            };
        }
    }

    public readonly struct SolverContext
    {
        public SolverContext(
            Random random,
            int monteCarloSimulations,
            int monteCarloRolloutDepth,
            bool lightweightMode = false,
            int planningDepthLimit = int.MaxValue)
        {
            Random = random ?? new Random();
            MonteCarloSimulations = Math.Max(1, monteCarloSimulations);
            MonteCarloRolloutDepth = Math.Max(1, monteCarloRolloutDepth);
            LightweightMode = lightweightMode;
            PlanningDepthLimit = Math.Max(1, planningDepthLimit);
        }

        public Random Random { get; }

        public int MonteCarloSimulations { get; }

        public int MonteCarloRolloutDepth { get; }

        public bool LightweightMode { get; }

        public int PlanningDepthLimit { get; }

        public int LimitPlanningDepth(int requestedDepth)
        {
            return Math.Min(Math.Max(1, requestedDepth), PlanningDepthLimit);
        }
    }

    public readonly struct SolverDecision
    {
        public SolverDecision(bool hasMove, int stackIndex, double score, string reason)
        {
            HasMove = hasMove;
            StackIndex = stackIndex;
            Score = score;
            Reason = reason;
        }

        public bool HasMove { get; }

        public int StackIndex { get; }

        public double Score { get; }

        public string Reason { get; }

        public static SolverDecision NoMove => new(false, -1, double.NegativeInfinity, "No valid move");
    }

    public sealed class RandomStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Rand;

        public string DisplayName => "RAND";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            if (legalMoves.Length == 0)
            {
                return SolverDecision.NoMove;
            }

            int selected = legalMoves[context.Random.Next(legalMoves.Length)];
            return new SolverDecision(true, selected, 0, "Random valid stack");
        }
    }

    public sealed class MergeFirstStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Merge;

        public string DisplayName => "MERG";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            return ChooseBestByScore(state, context, "Direct merge hunter", ScoreMergeFirst);
        }

        private static double ScoreMergeFirst(StackMergeGameState copy, MoveResult result, long scoreDelta)
        {
            double score = result.MergeCount * 500;
            score += FloorLog2(Math.Max(1, result.ResultingTopValue)) * 75;
            score += scoreDelta;
            score -= MaxHeight(copy) * 8;
            return score;
        }
    }

    public sealed class BalancedStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Balance;

        public string DisplayName => "BAL";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            return ChooseBestByScore(state, context, "Balanced stack risk", ScoreBalanced);
        }

        private static double ScoreBalanced(StackMergeGameState copy, MoveResult result, long scoreDelta)
        {
            int maxHeight = MaxHeight(copy);
            int minHeight = copy.Stacks.Min(stack => stack.Count);
            int dangerStacks = copy.Stacks.Count(stack => stack.Count >= copy.StackCapacity - 1);

            double score = 0;
            score += result.MergeCount * 180;
            score += scoreDelta * 0.5;
            score -= (maxHeight - minHeight) * 90;
            score -= maxHeight * maxHeight * 16;
            score -= dangerStacks * 220;
            score += FreeSlots(copy) * 16;
            return score;
        }
    }

    public sealed class HeuristicStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Heur;

        public string DisplayName => "HEUR";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            return ChooseHeuristicMove(state, context, "Heuristic best score");
        }

        internal static SolverDecision ChooseHeuristicMove(StackMergeGameState state, SolverContext context, string reason)
        {
            return ChooseBestByScore(state, context, reason, ScoreMove);
        }

        internal static double ScoreMove(StackMergeGameState state, MoveResult result, long scoreDelta)
        {
            int maxHeight = MaxHeight(state);
            int futureTopMatches = 0;
            int equalPairs = 0;

            foreach (var stack in state.Stacks)
            {
                if (stack.Count > 0 && state.NextBlocks.Contains(stack[^1]))
                {
                    futureTopMatches++;
                }

                for (int i = 1; i < stack.Count; i++)
                {
                    if (stack[i] == stack[i - 1])
                    {
                        equalPairs++;
                    }
                }
            }

            int dangerStacks = state.Stacks.Count(stack => stack.Count >= state.StackCapacity - 1);

            double score = 0;
            score += scoreDelta * 0.4;
            score += result.MergeCount * 180;
            score += FloorLog2(Math.Max(1, result.ResultingTopValue)) * 32;
            score += FreeSlots(state) * 14;
            score += futureTopMatches * 42;
            score += equalPairs * 55;
            score -= maxHeight * maxHeight * 7;
            score -= dangerStacks * 120;
            score += FloorLog2(Math.Max(1, state.HighestBlock)) * 20;
            return score;
        }
    }

    public sealed class LookaheadStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Look;

        public string DisplayName => "LOOK";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            if (legalMoves.Length == 0)
            {
                return SolverDecision.NoMove;
            }

            SolverDecision best = SolverDecision.NoMove;
            foreach (int firstMove in legalMoves)
            {
                StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                long beforeScore = copy.Score;
                MoveResult firstResult = copy.PlaceNext(firstMove);
                if (!firstResult.Accepted)
                {
                    continue;
                }

                double score = HeuristicStackMergeSolver.ScoreMove(copy, firstResult, copy.Score - beforeScore);
                SolverDecision next = HeuristicStackMergeSolver.ChooseHeuristicMove(copy, context, "Follow-up");
                if (next.HasMove)
                {
                    StackMergeGameState secondCopy = copy.CreateSimulationCopy(context.Random.Next());
                    long secondBefore = secondCopy.Score;
                    MoveResult second = secondCopy.PlaceNext(next.StackIndex);
                    if (second.Accepted)
                    {
                        score += HeuristicStackMergeSolver.ScoreMove(secondCopy, second, secondCopy.Score - secondBefore) * 0.72;
                    }
                }

                score += context.Random.NextDouble() * 0.001;
                if (!best.HasMove || score > best.Score)
                {
                    best = new SolverDecision(true, firstMove, score, "Lookahead best line");
                }
            }

            return best;
        }
    }

    public class QueuePlannerStackMergeSolver : IStackMergeSolver
    {
        private readonly SolverId id;
        private readonly string displayName;
        private readonly int planningDepth;

        public QueuePlannerStackMergeSolver(SolverId id, string displayName, int planningDepth)
        {
            this.id = id;
            this.displayName = displayName;
            this.planningDepth = Math.Max(1, planningDepth);
        }

        public SolverId Id => id;

        public string DisplayName => displayName;

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int activeDepth = context.LimitPlanningDepth(planningDepth);
            return ChooseQueuePlan(state, context, activeDepth, $"Plans {activeDepth} blocks");
        }

        internal static SolverDecision ChooseQueuePlan(StackMergeGameState state, SolverContext context, int planningDepth, string reason)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            if (legalMoves.Length == 0)
            {
                return SolverDecision.NoMove;
            }

            SolverDecision best = SolverDecision.NoMove;
            int depth = Math.Min(context.LimitPlanningDepth(planningDepth), state.NextBlocks.Count);
            foreach (int firstMove in legalMoves)
            {
                StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                long beforeScore = copy.Score;
                MoveResult result = copy.PlaceNext(firstMove);
                if (!result.Accepted)
                {
                    continue;
                }

                double score = HeuristicStackMergeSolver.ScoreMove(copy, result, copy.Score - beforeScore);
                score += Search(copy, context, depth - 1) * 0.78;
                score += context.Random.NextDouble() * 0.001;

                if (!best.HasMove || score > best.Score)
                {
                    best = new SolverDecision(true, firstMove, score, reason);
                }
            }

            return best;
        }

        internal static double Search(StackMergeGameState state, SolverContext context, int depth)
        {
            if (depth <= 0 || state.IsGameOver)
            {
                return EvaluateBoard(state);
            }

            int[] legalMoves = state.GetLegalMoveIndices();
            if (legalMoves.Length == 0)
            {
                return EvaluateBoard(state) - 5000;
            }

            double best = double.NegativeInfinity;
            foreach (int move in legalMoves)
            {
                StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                long beforeScore = copy.Score;
                MoveResult result = copy.PlaceNext(move);
                if (!result.Accepted)
                {
                    continue;
                }

                double score = HeuristicStackMergeSolver.ScoreMove(copy, result, copy.Score - beforeScore);
                score += Search(copy, context, depth - 1) * 0.70;
                if (score > best)
                {
                    best = score;
                }
            }

            return double.IsNegativeInfinity(best) ? EvaluateBoard(state) - 5000 : best;
        }

        internal static double EvaluateBoard(StackMergeGameState state)
        {
            int dangerStacks = state.Stacks.Count(stack => stack.Count >= state.StackCapacity - 1);
            int topMatches = state.Stacks.Count(stack => stack.Count > 0 && state.NextBlocks.Contains(stack[^1]));
            return state.Score * 0.1
                + FloorLog2(Math.Max(1, state.HighestBlock)) * 90
                + FreeSlots(state) * 24
                + topMatches * 80
                - MaxHeight(state) * MaxHeight(state) * 12
                - dangerStacks * 280;
        }
    }

    public sealed class Plan3StackMergeSolver : QueuePlannerStackMergeSolver
    {
        public Plan3StackMergeSolver() : base(SolverId.Plan3, "PLAN-3", 3)
        {
        }
    }

    public sealed class Plan5StackMergeSolver : QueuePlannerStackMergeSolver
    {
        public Plan5StackMergeSolver() : base(SolverId.Plan5, "PLAN-5", 5)
        {
        }
    }

    public sealed class AntiStallStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.AntiStall;

        public string DisplayName => "STALL";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            return ChooseBestByScore(state, context, "Avoiding stall", ScoreAntiStall);
        }

        internal static double ScoreAntiStall(StackMergeGameState state, MoveResult result, long scoreDelta)
        {
            int legalMoves = state.GetLegalMoveIndices().Length;
            int dangerStacks = state.Stacks.Count(stack => stack.Count >= state.StackCapacity - 1);
            int emptyStacks = state.Stacks.Count(stack => stack.Count == 0);
            int breathingStacks = state.Stacks.Count(stack => stack.Count <= Math.Max(1, state.StackCapacity - 3));
            int maxHeight = MaxHeight(state);
            int minHeight = state.Stacks.Min(stack => stack.Count);

            double score = 0;
            score += legalMoves * 260;
            score += FreeSlots(state) * 34;
            score += emptyStacks * 210;
            score += breathingStacks * 120;
            score += result.MergeCount * 120;
            score += scoreDelta * 0.25;
            score -= dangerStacks * 520;
            score -= maxHeight * maxHeight * 18;
            score -= (maxHeight - minHeight) * 80;

            if (legalMoves <= 1)
            {
                score -= 1800;
            }
            else if (legalMoves == 2)
            {
                score -= 500;
            }

            if (state.IsGameOver)
            {
                score -= 10000;
            }

            return score;
        }
    }

    public sealed class ComboFocusedStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Combo;

        public string DisplayName => "COMBO";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int futureDepth = context.LightweightMode ? 1 : 3;
            return ChooseBestByScore(state, context, "Combo setup", (copy, result, scoreDelta) => ScoreComboMove(copy, result, scoreDelta, futureDepth));
        }

        internal static double ScoreComboMove(StackMergeGameState state, MoveResult result, long scoreDelta)
        {
            return ScoreComboMove(state, result, scoreDelta, 3);
        }

        private static double ScoreComboMove(StackMergeGameState state, MoveResult result, long scoreDelta, int futureDepth)
        {
            double score = 0;
            score += result.MergeCount * 260;
            score += FloorLog2(Math.Max(1, result.ResultingTopValue)) * 52;
            score += scoreDelta * 0.35;
            score += CountAdjacentEqualPairs(state) * 90;
            score += CountQueueTopMatches(state) * 80;
            score += EstimateComboFuture(state, futureDepth) * 0.82;
            score += FreeSlots(state) * 8;
            score -= MaxHeight(state) * 6;
            return score;
        }

        private static double EstimateComboFuture(StackMergeGameState state, int depth)
        {
            if (depth <= 0 || state.IsGameOver)
            {
                return CountAdjacentEqualPairs(state) * 40 + CountQueueTopMatches(state) * 36;
            }

            double best = double.NegativeInfinity;
            foreach (int move in state.GetLegalMoveIndices())
            {
                StackMergeGameState copy = state.CreateSimulationCopy(move + depth * 97);
                long beforeScore = copy.Score;
                MoveResult result = copy.PlaceNext(move);
                if (!result.Accepted)
                {
                    continue;
                }

                double score = result.MergeCount * 380;
                score += FloorLog2(Math.Max(1, result.ResultingTopValue)) * 45;
                score += (copy.Score - beforeScore) * 0.2;
                score += CountAdjacentEqualPairs(copy) * 70;
                score += CountQueueTopMatches(copy) * 55;
                score += EstimateComboFuture(copy, depth - 1) * 0.58;
                if (score > best)
                {
                    best = score;
                }
            }

            return double.IsNegativeInfinity(best) ? -1000 : best;
        }
    }

    public sealed class MonteCarloStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Moca;

        public string DisplayName => "MOCA";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            if (legalMoves.Length == 0)
            {
                return SolverDecision.NoMove;
            }

            SolverDecision best = SolverDecision.NoMove;
            foreach (int firstMove in legalMoves)
            {
                double totalScore = 0;
                int successfulRuns = 0;

                for (int i = 0; i < context.MonteCarloSimulations; i++)
                {
                    StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                    long startScore = copy.Score;
                    MoveResult firstResult = copy.PlaceNext(firstMove);
                    if (!firstResult.Accepted)
                    {
                        continue;
                    }

                    for (int depth = 0; depth < context.MonteCarloRolloutDepth && !copy.IsGameOver; depth++)
                    {
                        SolverDecision rolloutMove = HeuristicStackMergeSolver.ChooseHeuristicMove(copy, context, "Rollout");
                        if (!rolloutMove.HasMove)
                        {
                            break;
                        }

                        copy.PlaceNext(rolloutMove.StackIndex);
                    }

                    double runScore = copy.Score - startScore;
                    runScore += HeuristicStackMergeSolver.ScoreMove(copy, firstResult, copy.Score - startScore) * 0.35;
                    totalScore += runScore;
                    successfulRuns++;
                }

                if (successfulRuns == 0)
                {
                    continue;
                }

                double average = totalScore / successfulRuns;
                average += context.Random.NextDouble() * 0.001;
                if (!best.HasMove || average > best.Score)
                {
                    best = new SolverDecision(true, firstMove, average, $"{context.MonteCarloSimulations} simulations");
                }
            }

            return best;
        }
    }

    public sealed class EnhancedMonteCarloStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.MocaPlus;

        public string DisplayName => "MOCA+";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            if (legalMoves.Length == 0)
            {
                return SolverDecision.NoMove;
            }

            SolverDecision best = SolverDecision.NoMove;
            int simulations = context.LightweightMode ? context.MonteCarloSimulations : Math.Max(4, context.MonteCarloSimulations);
            int rolloutDepth = context.LightweightMode ? context.MonteCarloRolloutDepth : Math.Max(3, context.MonteCarloRolloutDepth + 2);
            int smartPlanningDepth = context.LightweightMode ? 1 : context.LimitPlanningDepth(3);

            foreach (int firstMove in legalMoves)
            {
                double totalScore = 0;
                int successfulRuns = 0;

                for (int i = 0; i < simulations; i++)
                {
                    StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                    long startScore = copy.Score;
                    MoveResult firstResult = copy.PlaceNext(firstMove);
                    if (!firstResult.Accepted)
                    {
                        continue;
                    }

                    for (int depth = 0; depth < rolloutDepth && !copy.IsGameOver; depth++)
                    {
                        SolverDecision rolloutMove = QueuePlannerStackMergeSolver.ChooseQueuePlan(copy, context, smartPlanningDepth, "Smart rollout");
                        if (!rolloutMove.HasMove)
                        {
                            break;
                        }

                        copy.PlaceNext(rolloutMove.StackIndex);
                    }

                    double runScore = copy.Score - startScore;
                    runScore += QueuePlannerStackMergeSolver.EvaluateBoard(copy) * 0.20;
                    runScore += AntiStallStackMergeSolver.ScoreAntiStall(copy, firstResult, copy.Score - startScore) * 0.18;
                    totalScore += runScore;
                    successfulRuns++;
                }

                if (successfulRuns == 0)
                {
                    continue;
                }

                double average = totalScore / successfulRuns;
                average += context.Random.NextDouble() * 0.001;
                if (!best.HasMove || average > best.Score)
                {
                    best = new SolverDecision(true, firstMove, average, $"{simulations} smart simulations");
                }
            }

            return best;
        }
    }

    public sealed class MctsStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Mcts;

        public string DisplayName => "MCTS";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            if (legalMoves.Length == 0)
            {
                return SolverDecision.NoMove;
            }

            var root = new SearchNode(null, state.CreateSnapshot(), legalMoves);
            int iterations = context.LightweightMode
                ? Math.Max(4, context.MonteCarloSimulations * 2)
                : Math.Max(16, context.MonteCarloSimulations * 4);

            for (int i = 0; i < iterations; i++)
            {
                SearchNode node = root;
                StackMergeGameState working = state.CreateSimulationCopy(context.Random.Next());

                while (node.UntriedMoves.Count == 0 && node.Children.Count > 0)
                {
                    node = SelectChild(node, context);
                    working.RestoreSnapshot(node.Snapshot);
                }

                if (node.UntriedMoves.Count > 0)
                {
                    int moveIndex = context.Random.Next(node.UntriedMoves.Count);
                    int move = node.UntriedMoves[moveIndex];
                    node.UntriedMoves.RemoveAt(moveIndex);

                    MoveResult result = working.PlaceNext(move);
                    if (result.Accepted)
                    {
                        var child = new SearchNode(node, working.CreateSnapshot(), working.GetLegalMoveIndices())
                        {
                            MoveFromParent = move
                        };
                        node.Children.Add(child);
                        node = child;
                    }
                }

                int rolloutDepth = context.LightweightMode ? Math.Min(3, context.MonteCarloRolloutDepth) : context.MonteCarloRolloutDepth;
                double reward = Rollout(working, context, rolloutDepth);
                while (node != null)
                {
                    node.Visits++;
                    node.Value += reward;
                    node = node.Parent;
                }
            }

            SearchNode bestChild = root.Children
                .OrderByDescending(child => child.Visits)
                .ThenByDescending(child => child.AverageValue)
                .FirstOrDefault();

            if (bestChild == null)
            {
                return SolverDecision.NoMove;
            }

            return new SolverDecision(true, bestChild.MoveFromParent, bestChild.AverageValue, $"{iterations} tree visits");
        }

        private static SearchNode SelectChild(SearchNode node, SolverContext context)
        {
            double parentVisits = Math.Max(1, node.Visits);
            return node.Children
                .OrderByDescending(child =>
                {
                    double exploit = child.AverageValue;
                    double explore = Math.Sqrt(Math.Log(parentVisits + 1) / Math.Max(1, child.Visits));
                    return exploit + 1.42 * explore + context.Random.NextDouble() * 0.0001;
                })
                .First();
        }

        private static double Rollout(StackMergeGameState state, SolverContext context, int depth)
        {
            long startScore = state.Score;
            for (int i = 0; i < depth && !state.IsGameOver; i++)
            {
                SolverDecision decision;
                if (context.LightweightMode)
                {
                    decision = i % 2 == 0
                        ? HeuristicStackMergeSolver.ChooseHeuristicMove(state, context, "MCTS fast rollout")
                        : ChooseBestByScore(state, context, "MCTS anti-stall rollout", AntiStallStackMergeSolver.ScoreAntiStall);
                }
                else
                {
                    decision = i % 2 == 0
                        ? QueuePlannerStackMergeSolver.ChooseQueuePlan(state, context, 2, "MCTS rollout")
                        : HeuristicStackMergeSolver.ChooseHeuristicMove(state, context, "MCTS heuristic");
                }
                if (!decision.HasMove)
                {
                    break;
                }

                state.PlaceNext(decision.StackIndex);
            }

            return state.Score - startScore
                + QueuePlannerStackMergeSolver.EvaluateBoard(state) * 0.25
                - (state.IsGameOver ? 4000 : 0);
        }

        private sealed class SearchNode
        {
            public SearchNode(SearchNode parent, StackMergeSnapshot snapshot, int[] legalMoves)
            {
                Parent = parent;
                Snapshot = snapshot;
                UntriedMoves = new List<int>(legalMoves);
            }

            public SearchNode Parent { get; }

            public StackMergeSnapshot Snapshot { get; }

            public List<int> UntriedMoves { get; }

            public List<SearchNode> Children { get; } = new();

            public int MoveFromParent { get; set; } = -1;

            public int Visits { get; set; }

            public double Value { get; set; }

            public double AverageValue => Visits == 0 ? 0 : Value / Visits;
        }
    }

    internal static class SolverScoring
    {
        public static SolverDecision ChooseBestByScore(
            StackMergeGameState state,
            SolverContext context,
            string reason,
            Func<StackMergeGameState, MoveResult, long, double> scoreFunction)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            if (legalMoves.Length == 0)
            {
                return SolverDecision.NoMove;
            }

            SolverDecision best = SolverDecision.NoMove;
            foreach (int move in legalMoves)
            {
                StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                long beforeScore = copy.Score;
                MoveResult result = copy.PlaceNext(move);
                if (!result.Accepted)
                {
                    continue;
                }

                double score = scoreFunction(copy, result, copy.Score - beforeScore);
                score += context.Random.NextDouble() * 0.001;
                if (!best.HasMove || score > best.Score)
                {
                    string moveReason = result.MergeCount > 0 ? $"Merge x{result.MergeCount}" : reason;
                    best = new SolverDecision(true, move, score, moveReason);
                }
            }

            return best;
        }

        public static int MaxHeight(StackMergeGameState state)
        {
            return state.Stacks.Max(stack => stack.Count);
        }

        public static int FreeSlots(StackMergeGameState state)
        {
            return state.StackCount * state.StackCapacity - state.Stacks.Sum(stack => stack.Count);
        }

        public static int CountQueueTopMatches(StackMergeGameState state)
        {
            return state.Stacks.Count(stack => stack.Count > 0 && state.NextBlocks.Contains(stack[^1]));
        }

        public static int CountAdjacentEqualPairs(StackMergeGameState state)
        {
            int pairs = 0;
            foreach (var stack in state.Stacks)
            {
                for (int i = 1; i < stack.Count; i++)
                {
                    if (stack[i] == stack[i - 1])
                    {
                        pairs++;
                    }
                }
            }

            return pairs;
        }

        public static int FloorLog2(int value)
        {
            int exponent = 0;
            while (value > 1)
            {
                value >>= 1;
                exponent++;
            }

            return exponent;
        }
    }
}
