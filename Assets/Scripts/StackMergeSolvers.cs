using System;
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
        Plan5 = 7
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
            new(SolverId.Plan5, "PLAN-5", 2400, "Uses the extended queue.", "Deep queue planner: searches lines through up to 5 visible next blocks. Stronger once next preview upgrades are unlocked.")
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

    public readonly struct SolverContext
    {
        public SolverContext(Random random, int monteCarloSimulations, int monteCarloRolloutDepth)
        {
            Random = random ?? new Random();
            MonteCarloSimulations = Math.Max(1, monteCarloSimulations);
            MonteCarloRolloutDepth = Math.Max(1, monteCarloRolloutDepth);
        }

        public Random Random { get; }

        public int MonteCarloSimulations { get; }

        public int MonteCarloRolloutDepth { get; }
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
            int[] legalMoves = state.GetLegalMoveIndices();
            if (legalMoves.Length == 0)
            {
                return SolverDecision.NoMove;
            }

            SolverDecision best = SolverDecision.NoMove;
            int depth = Math.Min(planningDepth, state.NextBlocks.Count);
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
                    best = new SolverDecision(true, firstMove, score, $"Plans {depth} blocks");
                }
            }

            return best;
        }

        private static double Search(StackMergeGameState state, SolverContext context, int depth)
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

        private static double EvaluateBoard(StackMergeGameState state)
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
