using System;
using System.Collections.Generic;
using System.Linq;

namespace StackMerge
{
    public sealed class StackMergeGameState
    {
        public const int DefaultStackCount = 4;
        public const int DefaultStackCapacity = 5;
        public const int MaxStackCapacity = 10;
        public const int DefaultQueueLength = 3;

        private readonly List<int>[] stacks;
        private readonly List<int> nextBlocks = new();
        private Random random;

        public StackMergeGameState(
            int stackCount = DefaultStackCount,
            int stackCapacity = DefaultStackCapacity,
            int queueLength = DefaultQueueLength,
            int? seed = null)
        {
            if (stackCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stackCount));
            }

            if (stackCapacity <= 1)
            {
                throw new ArgumentOutOfRangeException(nameof(stackCapacity));
            }

            if (queueLength <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(queueLength));
            }

            StackCapacity = stackCapacity;
            QueueLength = queueLength;
            stacks = Enumerable.Range(0, stackCount).Select(_ => new List<int>(stackCapacity)).ToArray();
            random = seed.HasValue ? new Random(seed.Value) : new Random();
            NewGame(seed);
        }

        public int StackCount => stacks.Length;

        public int StackCapacity { get; }

        public int QueueLength { get; }

        public long Score { get; private set; }

        public int BlocksDropped { get; private set; }

        public int HighestBlock { get; private set; }

        public bool IsGameOver { get; private set; }

        public IReadOnlyList<IReadOnlyList<int>> Stacks => stacks;

        public IReadOnlyList<int> NextBlocks => nextBlocks;

        public void NewGame(int? seed = null)
        {
            if (seed.HasValue)
            {
                random = new Random(seed.Value);
            }

            foreach (List<int> stack in stacks)
            {
                stack.Clear();
            }

            Score = 0;
            BlocksDropped = 0;
            HighestBlock = 2;
            IsGameOver = false;
            nextBlocks.Clear();

            for (int i = 0; i < QueueLength; i++)
            {
                nextBlocks.Add(GenerateNextBlock());
            }
        }

        public bool CanPlace(int stackIndex)
        {
            ValidateStackIndex(stackIndex);

            if (IsGameOver)
            {
                return false;
            }

            return CanPlaceIgnoringGameOver(stackIndex);
        }

        public bool HasLegalMove()
        {
            for (int i = 0; i < StackCount; i++)
            {
                if (CanPlaceIgnoringGameOver(i))
                {
                    return true;
                }
            }

            return false;
        }

        public int[] GetLegalMoveIndices()
        {
            if (IsGameOver)
            {
                return Array.Empty<int>();
            }

            List<int> legalMoves = new(StackCount);
            for (int i = 0; i < StackCount; i++)
            {
                if (CanPlaceIgnoringGameOver(i))
                {
                    legalMoves.Add(i);
                }
            }

            return legalMoves.ToArray();
        }

        private bool CanPlaceIgnoringGameOver(int stackIndex)
        {
            List<int> stack = stacks[stackIndex];
            if (stack.Count < StackCapacity)
            {
                return true;
            }

            return stack.Count > 0 && stack[^1] == nextBlocks[0];
        }

        public MoveResult PlaceNext(int stackIndex)
        {
            ValidateStackIndex(stackIndex);

            if (IsGameOver)
            {
                return MoveResult.Rejected(stackIndex, "Game over");
            }

            if (!CanPlace(stackIndex))
            {
                IsGameOver = !HasLegalMove();
                return MoveResult.Rejected(stackIndex, "Stack is full");
            }

            int placedValue = nextBlocks[0];
            List<int> stack = stacks[stackIndex];
            stack.Add(placedValue);

            Score += placedValue;
            BlocksDropped++;
            HighestBlock = Math.Max(HighestBlock, placedValue);

            int mergeCount = ResolveMerges(stack);
            int resultingTop = stack.Count > 0 ? stack[^1] : placedValue;

            nextBlocks.RemoveAt(0);
            nextBlocks.Add(GenerateNextBlock());

            IsGameOver = !HasLegalMove();

            return new MoveResult(
                accepted: true,
                stackIndex: stackIndex,
                placedValue: placedValue,
                resultingTopValue: resultingTop,
                mergeCount: mergeCount,
                score: Score,
                highestBlock: HighestBlock,
                isGameOver: IsGameOver,
                reason: string.Empty);
        }

        public StackMergeSnapshot CreateSnapshot()
        {
            return new StackMergeSnapshot(
                stacks.Select(stack => stack.ToArray()).ToArray(),
                nextBlocks.ToArray(),
                Score,
                BlocksDropped,
                HighestBlock,
                IsGameOver);
        }

        public StackMergeGameState CreateSimulationCopy(int? seed = null)
        {
            var copy = new StackMergeGameState(StackCount, StackCapacity, QueueLength, seed);
            copy.RestoreSnapshot(CreateSnapshot());
            return copy;
        }

        public void RestoreSnapshot(StackMergeSnapshot snapshot)
        {
            if (snapshot.Stacks.Length != StackCount)
            {
                throw new ArgumentException("Snapshot stack count does not match this game.", nameof(snapshot));
            }

            if (snapshot.NextBlocks.Length != QueueLength)
            {
                throw new ArgumentException("Snapshot queue length does not match this game.", nameof(snapshot));
            }

            for (int i = 0; i < StackCount; i++)
            {
                if (snapshot.Stacks[i].Length > StackCapacity)
                {
                    throw new ArgumentException("Snapshot contains a stack above capacity.", nameof(snapshot));
                }

                stacks[i].Clear();
                stacks[i].AddRange(snapshot.Stacks[i]);
            }

            nextBlocks.Clear();
            nextBlocks.AddRange(snapshot.NextBlocks);
            Score = snapshot.Score;
            BlocksDropped = snapshot.BlocksDropped;
            HighestBlock = Math.Max(2, snapshot.HighestBlock);
            IsGameOver = snapshot.IsGameOver || !HasLegalMove();
        }

        public void RestoreSnapshotResized(StackMergeSnapshot snapshot)
        {
            if (snapshot.Stacks.Length != StackCount)
            {
                throw new ArgumentException("Snapshot stack count does not match this game.", nameof(snapshot));
            }

            for (int i = 0; i < StackCount; i++)
            {
                if (snapshot.Stacks[i].Length > StackCapacity)
                {
                    throw new ArgumentException("Snapshot contains a stack above capacity.", nameof(snapshot));
                }

                stacks[i].Clear();
                stacks[i].AddRange(snapshot.Stacks[i]);
            }

            Score = snapshot.Score;
            BlocksDropped = snapshot.BlocksDropped;
            HighestBlock = Math.Max(2, snapshot.HighestBlock);

            nextBlocks.Clear();
            int preservedBlocks = Math.Min(snapshot.NextBlocks.Length, QueueLength);
            for (int i = 0; i < preservedBlocks; i++)
            {
                nextBlocks.Add(snapshot.NextBlocks[i]);
            }

            while (nextBlocks.Count < QueueLength)
            {
                nextBlocks.Add(GenerateNextBlock());
            }

            IsGameOver = snapshot.IsGameOver || !HasLegalMove();
        }

        public void SetNextBlocksForTesting(params int[] blocks)
        {
            if (blocks.Length != QueueLength)
            {
                throw new ArgumentException($"Expected exactly {QueueLength} blocks.", nameof(blocks));
            }

            nextBlocks.Clear();
            nextBlocks.AddRange(blocks);
            HighestBlock = Math.Max(HighestBlock, blocks.Max());
            IsGameOver = !HasLegalMove();
        }

        public void SetStacksForTesting(params int[][] newStacks)
        {
            if (newStacks.Length != StackCount)
            {
                throw new ArgumentException($"Expected exactly {StackCount} stacks.", nameof(newStacks));
            }

            for (int i = 0; i < StackCount; i++)
            {
                if (newStacks[i].Length > StackCapacity)
                {
                    throw new ArgumentException("A stack is above capacity.", nameof(newStacks));
                }

                stacks[i].Clear();
                stacks[i].AddRange(newStacks[i]);
            }

            int maxStackBlock = stacks.SelectMany(stack => stack).DefaultIfEmpty(2).Max();
            HighestBlock = Math.Max(HighestBlock, maxStackBlock);
            IsGameOver = !HasLegalMove();
        }

        private int ResolveMerges(List<int> stack)
        {
            int mergeCount = 0;

            while (stack.Count >= 2)
            {
                int topIndex = stack.Count - 1;
                int top = stack[topIndex];
                int below = stack[topIndex - 1];

                if (top != below)
                {
                    break;
                }

                int mergedValue = top * 2;
                stack.RemoveAt(topIndex);
                stack[topIndex - 1] = mergedValue;
                HighestBlock = Math.Max(HighestBlock, mergedValue);
                mergeCount++;
            }

            return mergeCount;
        }

        private int GenerateNextBlock()
        {
            int maxSpawnExponent = Math.Max(1, FloorLog2(HighestBlock) - 2);
            maxSpawnExponent = Math.Min(maxSpawnExponent, 7);

            double totalWeight = 0;
            double[] weights = new double[maxSpawnExponent + 1];

            for (int exponent = 0; exponent <= maxSpawnExponent; exponent++)
            {
                double weight = Math.Pow(0.56, exponent);
                weights[exponent] = weight;
                totalWeight += weight;
            }

            double roll = random.NextDouble() * totalWeight;
            double cumulative = 0;

            for (int exponent = 0; exponent < weights.Length; exponent++)
            {
                cumulative += weights[exponent];
                if (roll <= cumulative)
                {
                    return 1 << exponent;
                }
            }

            return 1 << maxSpawnExponent;
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

        private void ValidateStackIndex(int stackIndex)
        {
            if (stackIndex < 0 || stackIndex >= StackCount)
            {
                throw new ArgumentOutOfRangeException(nameof(stackIndex));
            }
        }
    }

    public readonly struct MoveResult
    {
        public MoveResult(
            bool accepted,
            int stackIndex,
            int placedValue,
            int resultingTopValue,
            int mergeCount,
            long score,
            int highestBlock,
            bool isGameOver,
            string reason)
        {
            Accepted = accepted;
            StackIndex = stackIndex;
            PlacedValue = placedValue;
            ResultingTopValue = resultingTopValue;
            MergeCount = mergeCount;
            Score = score;
            HighestBlock = highestBlock;
            IsGameOver = isGameOver;
            Reason = reason;
        }

        public bool Accepted { get; }

        public int StackIndex { get; }

        public int PlacedValue { get; }

        public int ResultingTopValue { get; }

        public int MergeCount { get; }

        public long Score { get; }

        public int HighestBlock { get; }

        public bool IsGameOver { get; }

        public string Reason { get; }

        public static MoveResult Rejected(int stackIndex, string reason)
        {
            return new MoveResult(false, stackIndex, 0, 0, 0, 0, 0, false, reason);
        }
    }

    public readonly struct StackMergeSnapshot
    {
        public StackMergeSnapshot(
            int[][] stacks,
            int[] nextBlocks,
            long score,
            int blocksDropped,
            int highestBlock,
            bool isGameOver)
        {
            Stacks = stacks;
            NextBlocks = nextBlocks;
            Score = score;
            BlocksDropped = blocksDropped;
            HighestBlock = highestBlock;
            IsGameOver = isGameOver;
        }

        public int[][] Stacks { get; }

        public int[] NextBlocks { get; }

        public long Score { get; }

        public int BlocksDropped { get; }

        public int HighestBlock { get; }

        public bool IsGameOver { get; }
    }
}
