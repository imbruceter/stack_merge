using System;
using System.Linq;

namespace StackMerge
{
    public enum SolverId
    {
        Rand = 0,
        Heur = 1,
        Moca = 2
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

                double score = ScoreMove(copy, result, copy.Score - beforeScore);
                score += context.Random.NextDouble() * 0.001;
                if (!best.HasMove || score > best.Score)
                {
                    string moveReason = result.MergeCount > 0 ? $"Merge x{result.MergeCount}" : reason;
                    best = new SolverDecision(true, move, score, moveReason);
                }
            }

            return best;
        }

        internal static double ScoreMove(StackMergeGameState state, MoveResult result, long scoreDelta)
        {
            int maxHeight = 0;
            int totalHeight = 0;
            int futureTopMatches = 0;
            int equalPairs = 0;

            foreach (var stack in state.Stacks)
            {
                maxHeight = Math.Max(maxHeight, stack.Count);
                totalHeight += stack.Count;

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

            int freeSlots = state.StackCount * state.StackCapacity - totalHeight;
            int dangerStacks = state.Stacks.Count(stack => stack.Count >= state.StackCapacity - 1);

            double score = 0;
            score += scoreDelta * 0.4;
            score += result.MergeCount * 180;
            score += FloorLog2(Math.Max(1, result.ResultingTopValue)) * 32;
            score += freeSlots * 14;
            score += futureTopMatches * 42;
            score += equalPairs * 55;
            score -= maxHeight * maxHeight * 7;
            score -= dangerStacks * 120;
            score += FloorLog2(Math.Max(1, state.HighestBlock)) * 20;
            return score;
        }

        private static int FloorLog2(int value)
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
}
