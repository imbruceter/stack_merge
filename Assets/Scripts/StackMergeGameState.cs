using System;
using System.Collections.Generic;
using System.Linq;

namespace StackMerge
{
    public readonly struct StackMergeRunModifiers
    {
        public StackMergeRunModifiers(
            int unstableSaves,
            bool mirrorStack,
            bool jokerBlocks,
            int pickaxeUses,
            int queueSkips)
        {
            UnstableSaves = Math.Max(0, unstableSaves);
            MirrorStack = mirrorStack;
            JokerBlocks = jokerBlocks;
            PickaxeUses = Math.Max(0, pickaxeUses);
            QueueSkips = Math.Max(0, queueSkips);
        }

        public int UnstableSaves { get; }

        public bool MirrorStack { get; }

        public bool JokerBlocks { get; }

        public int PickaxeUses { get; }

        public int QueueSkips { get; }
    }

    public sealed class StackMergeGameState
    {
        public const int JokerBlockValue = 0;
        public const int DefaultStackCount = 4;
        public const int DefaultStackCapacity = 5;
        public const int MaxStackCapacity = 10;
        public const int DefaultQueueLength = 3;

        private readonly List<int>[] stacks;
        private readonly List<int> nextBlocks = new();
        private readonly StackMergeRunModifiers startingModifiers;
        private Random random;
        private int unstableSavesRemaining;
        private int pickaxeUsesRemaining;
        private int queueSkipsRemaining;
        private bool mirrorStackEnabled;
        private bool jokerBlocksEnabled;

        public StackMergeGameState(
            int stackCount = DefaultStackCount,
            int stackCapacity = DefaultStackCapacity,
            int queueLength = DefaultQueueLength,
            int difficultyLevel = 0,
            StackMergeRunModifiers modifiers = default,
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
            DifficultyLevel = Math.Max(0, difficultyLevel);
            startingModifiers = modifiers;
            stacks = Enumerable.Range(0, stackCount).Select(_ => new List<int>(stackCapacity)).ToArray();
            random = seed.HasValue ? new Random(seed.Value) : new Random();
            NewGame(seed);
        }

        public int StackCount => stacks.Length;

        public int StackCapacity { get; }

        public int QueueLength { get; }

        public int DifficultyLevel { get; }

        public long Score { get; private set; }

        public int BlocksDropped { get; private set; }

        public int TotalMerges { get; private set; }

        public int HighestBlock { get; private set; }

        public int HighestMergedBlock { get; private set; }

        public bool IsGameOver { get; private set; }

        public IReadOnlyList<IReadOnlyList<int>> Stacks => stacks;

        public IReadOnlyList<int> NextBlocks => nextBlocks;

        public int UnstableSavesRemaining => unstableSavesRemaining;

        public int PickaxeUsesRemaining => pickaxeUsesRemaining;

        public int QueueSkipsRemaining => queueSkipsRemaining;

        public bool MirrorStackEnabled => mirrorStackEnabled;

        public bool JokerBlocksEnabled => jokerBlocksEnabled;

        public StackMergeRunModifiers ActiveModifiers => new(
            unstableSavesRemaining,
            mirrorStackEnabled,
            jokerBlocksEnabled,
            pickaxeUsesRemaining,
            queueSkipsRemaining);

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
            TotalMerges = 0;
            HighestBlock = 2;
            HighestMergedBlock = 0;
            IsGameOver = false;
            unstableSavesRemaining = startingModifiers.UnstableSaves;
            pickaxeUsesRemaining = startingModifiers.PickaxeUses;
            queueSkipsRemaining = startingModifiers.QueueSkips;
            mirrorStackEnabled = startingModifiers.MirrorStack;
            jokerBlocksEnabled = startingModifiers.JokerBlocks;
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

            if (stack.Count <= 0)
            {
                return false;
            }

            int next = nextBlocks[0];
            if (CanMergeWithTop(next, stack[^1]))
            {
                return true;
            }

            return unstableSavesRemaining > 0;
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
            bool usedUnstableSave = false;
            if (stack.Count >= StackCapacity && stack.Count > 0 && !CanMergeWithTop(placedValue, stack[^1]) && unstableSavesRemaining > 0)
            {
                stack.RemoveAt(0);
                unstableSavesRemaining--;
                usedUnstableSave = true;
            }

            int blockValue = placedValue == JokerBlockValue && stack.Count == 0 ? 1 : placedValue;
            stack.Add(blockValue);

            Score += Math.Max(0, blockValue);
            BlocksDropped++;
            HighestBlock = Math.Max(HighestBlock, Math.Max(1, blockValue));

            int mergeCount = ResolveMerges(stack);
            TotalMerges += mergeCount;
            int resultingTop = stack.Count > 0 ? stack[^1] : blockValue;

            nextBlocks.RemoveAt(0);
            nextBlocks.Add(GenerateNextBlock());

            StabilizeWithRunModifiers();
            IsGameOver = !HasLegalMove();

            return new MoveResult(
                accepted: true,
                stackIndex: stackIndex,
                placedValue: blockValue,
                resultingTopValue: resultingTop,
                mergeCount: mergeCount,
                score: Score,
                highestBlock: HighestBlock,
                isGameOver: IsGameOver,
                reason: usedUnstableSave ? "Unstable stack saved the move" : string.Empty,
                unstableSaveUsed: usedUnstableSave);
        }

        public StackMergeSnapshot CreateSnapshot()
        {
            return new StackMergeSnapshot(
                stacks.Select(stack => stack.ToArray()).ToArray(),
                nextBlocks.ToArray(),
                Score,
                BlocksDropped,
                TotalMerges,
                HighestBlock,
                HighestMergedBlock,
                IsGameOver,
                ActiveModifiers);
        }

        public StackMergeGameState CreateSimulationCopy(int? seed = null)
        {
            var copy = new StackMergeGameState(StackCount, StackCapacity, QueueLength, DifficultyLevel, ActiveModifiers, seed);
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
            TotalMerges = Math.Max(0, snapshot.TotalMerges);
            HighestBlock = Math.Max(2, snapshot.HighestBlock);
            HighestMergedBlock = Math.Max(0, snapshot.HighestMergedBlock);
            RestoreModifierState(snapshot.Modifiers);
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
            TotalMerges = Math.Max(0, snapshot.TotalMerges);
            HighestBlock = Math.Max(2, snapshot.HighestBlock);
            HighestMergedBlock = Math.Max(0, snapshot.HighestMergedBlock);
            RestoreModifierState(snapshot.Modifiers);

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
            HighestBlock = Math.Max(HighestBlock, blocks.Where(block => block > 0).DefaultIfEmpty(1).Max());
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

        private void RestoreModifierState(StackMergeRunModifiers modifiers)
        {
            unstableSavesRemaining = Math.Max(0, modifiers.UnstableSaves);
            pickaxeUsesRemaining = Math.Max(0, modifiers.PickaxeUses);
            queueSkipsRemaining = Math.Max(0, modifiers.QueueSkips);
            mirrorStackEnabled = modifiers.MirrorStack;
            jokerBlocksEnabled = modifiers.JokerBlocks;
        }

        private bool CanMergeWithTop(int placedValue, int topValue)
        {
            if (placedValue == topValue)
            {
                return true;
            }

            return jokerBlocksEnabled && placedValue == JokerBlockValue && topValue > 0;
        }

        private int ResolveMerges(List<int> stack)
        {
            int mergeCount = 0;

            while (true)
            {
                bool merged = false;
                while (stack.Count >= 2)
                {
                    int topIndex = stack.Count - 1;
                    int top = stack[topIndex];
                    int below = stack[topIndex - 1];

                    bool jokerMerge = jokerBlocksEnabled && top == JokerBlockValue && below > 0;
                    if (!jokerMerge && top != below)
                    {
                        break;
                    }

                    int mergedValue = below * 2;
                    stack.RemoveAt(topIndex);
                    stack[topIndex - 1] = mergedValue;
                    Score += mergedValue;
                    HighestBlock = Math.Max(HighestBlock, mergedValue);
                    HighestMergedBlock = Math.Max(HighestMergedBlock, mergedValue);
                    mergeCount++;
                    merged = true;
                }

                if (mirrorStackEnabled && stack.Count >= 2 && stack[0] > 0 && stack[0] == stack[^1])
                {
                    int mergedValue = stack[0] * 2;
                    stack.RemoveAt(stack.Count - 1);
                    stack[0] = mergedValue;
                    Score += mergedValue;
                    HighestBlock = Math.Max(HighestBlock, mergedValue);
                    HighestMergedBlock = Math.Max(HighestMergedBlock, mergedValue);
                    mergeCount++;
                    merged = true;
                }

                if (!merged)
                {
                    break;
                }
            }

            return mergeCount;
        }

        private void StabilizeWithRunModifiers()
        {
            while (!HasLegalMove())
            {
                if (queueSkipsRemaining > 0)
                {
                    queueSkipsRemaining--;
                    nextBlocks.RemoveAt(0);
                    nextBlocks.Add(GenerateNextBlock());
                    continue;
                }

                if (pickaxeUsesRemaining > 0 && RemoveBestPickaxeTarget())
                {
                    pickaxeUsesRemaining--;
                    continue;
                }

                break;
            }
        }

        private bool RemoveBestPickaxeTarget()
        {
            int bestStack = -1;
            int bestIndex = -1;
            int bestHeight = -1;
            int bestValue = int.MaxValue;

            for (int stackIndex = 0; stackIndex < stacks.Length; stackIndex++)
            {
                List<int> stack = stacks[stackIndex];
                for (int blockIndex = 0; blockIndex < stack.Count; blockIndex++)
                {
                    int value = stack[blockIndex];
                    if (stack.Count > bestHeight || (stack.Count == bestHeight && value < bestValue))
                    {
                        bestStack = stackIndex;
                        bestIndex = blockIndex;
                        bestHeight = stack.Count;
                        bestValue = value;
                    }
                }
            }

            if (bestStack < 0)
            {
                return false;
            }

            stacks[bestStack].RemoveAt(bestIndex);
            return true;
        }

        private int GenerateNextBlock()
        {
            if (jokerBlocksEnabled)
            {
                double jokerChance = 0.055 + Math.Min(0.035, DifficultyLevel * 0.01);
                if (random.NextDouble() < jokerChance)
                {
                    return JokerBlockValue;
                }
            }

            int maxSpawnExponent = Math.Max(1, FloorLog2(HighestBlock) - 2 + DifficultyLevel);
            if (DifficultyLevel >= 2)
            {
                maxSpawnExponent = Math.Max(maxSpawnExponent, 2);
            }

            if (DifficultyLevel >= 3)
            {
                maxSpawnExponent = Math.Max(maxSpawnExponent, 3);
            }

            maxSpawnExponent = Math.Min(maxSpawnExponent, 7 + Math.Min(2, DifficultyLevel));

            double totalWeight = 0;
            double[] weights = new double[maxSpawnExponent + 1];

            for (int exponent = 0; exponent <= maxSpawnExponent; exponent++)
            {
                double pressure = 1.0 + DifficultyLevel * 0.18 * exponent;
                double weight = Math.Pow(0.56, exponent) * pressure;
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
            string reason,
            bool unstableSaveUsed = false)
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
            UnstableSaveUsed = unstableSaveUsed;
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

        public bool UnstableSaveUsed { get; }

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
            int totalMerges,
            int highestBlock,
            int highestMergedBlock,
            bool isGameOver,
            StackMergeRunModifiers modifiers = default)
        {
            Stacks = stacks;
            NextBlocks = nextBlocks;
            Score = score;
            BlocksDropped = blocksDropped;
            TotalMerges = totalMerges;
            HighestBlock = highestBlock;
            HighestMergedBlock = highestMergedBlock;
            IsGameOver = isGameOver;
            Modifiers = modifiers;
        }

        public int[][] Stacks { get; }

        public int[] NextBlocks { get; }

        public long Score { get; }

        public int BlocksDropped { get; }

        public int TotalMerges { get; }

        public int HighestBlock { get; }

        public int HighestMergedBlock { get; }

        public bool IsGameOver { get; }

        public StackMergeRunModifiers Modifiers { get; }
    }
}
