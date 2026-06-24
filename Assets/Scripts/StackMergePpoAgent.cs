using System;
using System.Collections.Generic;
using System.Linq;
using static StackMerge.SolverScoring;

namespace StackMerge
{
    [Serializable]
    public sealed class StackMergePpoTrainingData
    {
        public int version;
        public int updates;
        public int episodes;
        public int steps;
        public float totalReward;
        public float recentAverageReward;
        public float recentAverageScore;
        public float recentAverageMoves;
        public float recentAverageMerges;
        public float recentAverageHigh;
        public float bestEpisodeReward;
        public long bestScore;
        public int bestHigh;
        public float lastPolicyLoss;
        public float lastValueLoss;
        public float lastEntropy;
        public float[] actorW1;
        public float[] actorB1;
        public float[] actorW2;
        public float[] actorB2;
        public float[] criticW1;
        public float[] criticB1;
        public float[] criticW2;
        public float[] criticB2;
    }

    public readonly struct StackMergePpoMetrics
    {
        public StackMergePpoMetrics(
            int updates,
            int steps,
            int episodes,
            float progress,
            float recentAverageReward,
            float recentAverageScore,
            float recentAverageMoves,
            float recentAverageMerges,
            float recentAverageHigh,
            float lastPolicyLoss,
            float lastValueLoss,
            float lastEntropy,
            float bestEpisodeReward)
        {
            Updates = updates;
            Steps = steps;
            Episodes = episodes;
            Progress = progress;
            RecentAverageReward = recentAverageReward;
            RecentAverageScore = recentAverageScore;
            RecentAverageMoves = recentAverageMoves;
            RecentAverageMerges = recentAverageMerges;
            RecentAverageHigh = recentAverageHigh;
            LastPolicyLoss = lastPolicyLoss;
            LastValueLoss = lastValueLoss;
            LastEntropy = lastEntropy;
            BestEpisodeReward = bestEpisodeReward;
        }

        public int Updates { get; }

        public int Steps { get; }

        public int Episodes { get; }

        public float Progress { get; }

        public float RecentAverageReward { get; }

        public float RecentAverageScore { get; }

        public float RecentAverageMoves { get; }

        public float RecentAverageMerges { get; }

        public float RecentAverageHigh { get; }

        public float LastPolicyLoss { get; }

        public float LastValueLoss { get; }

        public float LastEntropy { get; }

        public float BestEpisodeReward { get; }
    }

    public sealed class StackMergePpoAgent
    {
        private const int Version = 2;
        private const int MaxStacks = StackMergeGameState.DefaultStackCount;
        private const int MaxStackCapacity = StackMergeGameState.MaxStackCapacity;
        private const int MaxQueueLength = StackMergeGameState.DefaultQueueLength + 2;
        private const int InputSize = 96;
        private const int HiddenSize = 48;
        private const int PlaceActionCount = MaxStacks;
        private const int QueueSkipAction = PlaceActionCount;
        private const int PickaxeActionStart = QueueSkipAction + 1;
        private const int ActionCount = PickaxeActionStart + MaxStacks * MaxStackCapacity;
        private const float Gamma = 0.992f;
        private const float Lambda = 0.94f;
        private const float ClipEpsilon = 0.18f;
        private const float ProgressUpdateScale = 8000f;
        private const float ProgressScoreScale = 18000f;

        private readonly StackMergePpoTrainingData data;
        private readonly List<Transition> trajectory = new(512);
        private bool hasPendingTransition;
        private Transition pendingTransition;
        private float episodeReward;

        public StackMergePpoAgent(StackMergePpoTrainingData data, int seed = 24681357)
        {
            this.data = data ?? new StackMergePpoTrainingData();
            EnsureInitialized(new Random(seed));
        }

        public StackMergePpoTrainingData Data => data;

        public StackMergePpoMetrics Metrics => new(
            data.updates,
            data.steps,
            data.episodes,
            TrainingProgress,
            data.recentAverageReward,
            data.recentAverageScore,
            data.recentAverageMoves,
            data.recentAverageMerges,
            data.recentAverageHigh,
            data.lastPolicyLoss,
            data.lastValueLoss,
            data.lastEntropy,
            data.bestEpisodeReward);

        public int Level => Math.Min(100, Math.Max(0, (int)Math.Floor(TrainingProgress * 100f)));

        public float TrainingProgress
        {
            get
            {
                float updateReadiness = 1f - (float)Math.Exp(-Math.Max(0, data.updates) / ProgressUpdateScale);
                float scoreReadiness = 1f - (float)Math.Exp(-Math.Max(0f, data.recentAverageScore) / ProgressScoreScale);
                float highReadiness = Clamp01(LogBlock(Math.Max(1f, data.recentAverageHigh)) / 14f);
                float performanceReadiness = Clamp01(scoreReadiness * 0.76f + highReadiness * 0.24f);
                return Clamp01(Math.Min(updateReadiness, performanceReadiness));
            }
        }

        public SolverDecision ChooseMove(StackMergeGameState state, Random random, bool trainingMode)
        {
            if (state == null || state.IsGameOver)
            {
                return SolverDecision.NoMove;
            }

            EnsureInitialized(random);
            random ??= new Random();

            float[] features = ExtractFeatures(state);
            bool[] mask = BuildActionMask(state);
            int validActions = CountValid(mask);
            if (validActions == 0)
            {
                return SolverDecision.NoMove;
            }

            ActorForward(features, mask, out _, out _, out float[] probabilities, out float entropy);
            float value = CriticValue(features);
            int action = SelectAction(probabilities, mask, random, trainingMode);
            float actionProbability = Math.Max(1e-7f, probabilities[action]);

            pendingTransition = new Transition
            {
                Features = features,
                Mask = mask,
                Action = action,
                OldLogProbability = (float)Math.Log(actionProbability),
                Value = value,
                ScoreBefore = state.Score,
                Entropy = entropy
            };
            hasPendingTransition = true;

            return ToSolverDecision(action, actionProbability, value, trainingMode);
        }

        public void Observe(MoveResult result, StackMergeGameState nextState, bool trainingMode)
        {
            if (!hasPendingTransition || nextState == null)
            {
                return;
            }

            Transition transition = pendingTransition;
            hasPendingTransition = false;

            float[] nextFeatures = ExtractFeatures(nextState);
            transition.Reward = ComputeReward(result, nextState, transition.ScoreBefore);
            transition.Done = result.IsGameOver;
            transition.NextValue = result.IsGameOver ? 0f : CriticValue(nextFeatures);
            trajectory.Add(transition);
            episodeReward += transition.Reward;
            data.steps++;

            int targetBatch = trainingMode ? 192 : 384;
            if (trajectory.Count >= targetBatch || result.IsGameOver)
            {
                UpdatePolicy(trainingMode);
            }

            if (result.IsGameOver)
            {
                CompleteEpisode(nextState);
            }
        }

        public void ForceUpdate(bool trainingMode)
        {
            if (trajectory.Count > 0)
            {
                UpdatePolicy(trainingMode);
            }
        }

        public string BuildStatus(long bestScore, int bestHigh, bool trainingMode)
        {
            StackMergePpoMetrics metrics = Metrics;
            return $"PPO Lv {Level} | Updates {metrics.Updates} | Steps {metrics.Steps} | Avg score {metrics.RecentAverageScore:0} | Avg high {metrics.RecentAverageHigh:0} | Policy loss {metrics.LastPolicyLoss:0.000} | Value loss {metrics.LastValueLoss:0.000} | Entropy {metrics.LastEntropy:0.000} | Best {bestScore} / {bestHigh} | Mode {(trainingMode ? "Training: no chips, faster PPO updates" : "Normal: earns chips, slower PPO updates")}";
        }

        private void CompleteEpisode(StackMergeGameState state)
        {
            data.episodes++;
            data.totalReward += episodeReward;
            data.bestEpisodeReward = Math.Max(data.bestEpisodeReward, episodeReward);
            float smoothing = data.episodes <= 1 ? 1f : 0.025f;
            data.recentAverageReward = data.episodes <= 1
                ? episodeReward
                : data.recentAverageReward * (1f - smoothing) + episodeReward * smoothing;
            data.recentAverageScore = SmoothEpisodeMetric(data.recentAverageScore, state.Score, smoothing);
            data.recentAverageMoves = SmoothEpisodeMetric(data.recentAverageMoves, state.BlocksDropped, smoothing);
            data.recentAverageMerges = SmoothEpisodeMetric(data.recentAverageMerges, state.TotalMerges, smoothing);
            data.recentAverageHigh = SmoothEpisodeMetric(data.recentAverageHigh, Math.Max(1, state.HighestMergedBlock), smoothing);
            data.bestScore = Math.Max(data.bestScore, Math.Max(0, state.Score));
            data.bestHigh = Math.Max(data.bestHigh, Math.Max(0, state.HighestMergedBlock));
            episodeReward = 0f;
        }

        private void UpdatePolicy(bool trainingMode)
        {
            int count = trajectory.Count;
            if (count <= 0)
            {
                return;
            }

            float[] advantages = new float[count];
            float[] returns = new float[count];
            float gae = 0f;
            for (int i = count - 1; i >= 0; i--)
            {
                Transition transition = trajectory[i];
                float continuation = transition.Done ? 0f : 1f;
                float delta = transition.Reward + Gamma * transition.NextValue * continuation - transition.Value;
                gae = delta + Gamma * Lambda * continuation * gae;
                advantages[i] = gae;
                returns[i] = gae + transition.Value;
            }

            Normalize(advantages);

            float maturity = 1f - (float)Math.Exp(-Math.Max(0, data.updates) / 5000f);
            int epochs = trainingMode ? 3 : 1;
            float actorLearningRate = (trainingMode ? 0.00090f : 0.00018f) * (1f - maturity * 0.55f);
            float criticLearningRate = (trainingMode ? 0.00110f : 0.00024f) * (1f - maturity * 0.50f);
            float entropyCoefficient = trainingMode
                ? 0.020f * (1f - maturity) + 0.006f * maturity
                : 0.003f;
            float totalPolicyLoss = 0f;
            float totalValueLoss = 0f;
            float totalEntropy = 0f;
            int totalSamples = 0;

            for (int epoch = 0; epoch < epochs; epoch++)
            {
                for (int i = 0; i < count; i++)
                {
                    Transition transition = trajectory[i];
                    float policyLoss = TrainActor(transition, advantages[i], actorLearningRate, entropyCoefficient, out float entropy);
                    float valueLoss = TrainCritic(transition.Features, returns[i], criticLearningRate);
                    totalPolicyLoss += policyLoss;
                    totalValueLoss += valueLoss;
                    totalEntropy += entropy;
                    totalSamples++;
                }
            }

            if (totalSamples > 0)
            {
                data.lastPolicyLoss = totalPolicyLoss / totalSamples;
                data.lastValueLoss = totalValueLoss / totalSamples;
                data.lastEntropy = totalEntropy / totalSamples;
            }

            data.updates++;
            trajectory.Clear();
        }

        private float TrainActor(Transition transition, float advantage, float learningRate, float entropyCoefficient, out float entropy)
        {
            ActorForward(transition.Features, transition.Mask, out float[] hidden, out _, out float[] probabilities, out entropy);
            float newProbability = Math.Max(1e-7f, probabilities[transition.Action]);
            float ratio = newProbability / Math.Max(1e-7f, (float)Math.Exp(transition.OldLogProbability));
            float clippedRatio = Math.Min(1f + ClipEpsilon, Math.Max(1f - ClipEpsilon, ratio));
            float unclippedObjective = ratio * advantage;
            float clippedObjective = clippedRatio * advantage;
            bool clipped = advantage >= 0f ? ratio > 1f + ClipEpsilon : ratio < 1f - ClipEpsilon;
            float coefficient = clipped ? 0f : Clip(advantage * ratio, -4f, 4f);
            int validCount = CountValid(transition.Mask);
            float uniform = validCount > 0 ? 1f / validCount : 0f;
            float[] outputGradient = new float[ActionCount];
            float[] hiddenGradient = new float[HiddenSize];

            for (int action = 0; action < ActionCount; action++)
            {
                if (!transition.Mask[action])
                {
                    continue;
                }

                float target = action == transition.Action ? 1f : 0f;
                float gradient = coefficient * (target - probabilities[action]);
                gradient -= entropyCoefficient * (probabilities[action] - uniform);
                gradient = Clip(gradient, -4f, 4f);
                outputGradient[action] = gradient;

                int weightOffset = action * HiddenSize;
                for (int hiddenIndex = 0; hiddenIndex < HiddenSize; hiddenIndex++)
                {
                    hiddenGradient[hiddenIndex] += data.actorW2[weightOffset + hiddenIndex] * gradient;
                }
            }

            for (int action = 0; action < ActionCount; action++)
            {
                float gradient = outputGradient[action];
                if (Math.Abs(gradient) < 1e-8f)
                {
                    continue;
                }

                int weightOffset = action * HiddenSize;
                for (int hiddenIndex = 0; hiddenIndex < HiddenSize; hiddenIndex++)
                {
                    data.actorW2[weightOffset + hiddenIndex] += learningRate * gradient * hidden[hiddenIndex];
                }

                data.actorB2[action] += learningRate * gradient;
            }

            for (int hiddenIndex = 0; hiddenIndex < HiddenSize; hiddenIndex++)
            {
                float gradient = Clip(hiddenGradient[hiddenIndex] * (1f - hidden[hiddenIndex] * hidden[hiddenIndex]), -3f, 3f);
                if (Math.Abs(gradient) < 1e-8f)
                {
                    continue;
                }

                int weightOffset = hiddenIndex * InputSize;
                for (int inputIndex = 0; inputIndex < InputSize; inputIndex++)
                {
                    data.actorW1[weightOffset + inputIndex] += learningRate * gradient * transition.Features[inputIndex];
                }

                data.actorB1[hiddenIndex] += learningRate * gradient;
            }

            return -Math.Min(unclippedObjective, clippedObjective);
        }

        private float TrainCritic(float[] features, float targetReturn, float learningRate)
        {
            CriticForward(features, out float[] hidden, out float value);
            float error = Clip(targetReturn - value, -18f, 18f);
            float[] criticW2Before = data.criticW2.ToArray();

            for (int hiddenIndex = 0; hiddenIndex < HiddenSize; hiddenIndex++)
            {
                data.criticW2[hiddenIndex] += learningRate * error * hidden[hiddenIndex];
            }

            data.criticB2[0] += learningRate * error;

            for (int hiddenIndex = 0; hiddenIndex < HiddenSize; hiddenIndex++)
            {
                float gradient = Clip(criticW2Before[hiddenIndex] * error * (1f - hidden[hiddenIndex] * hidden[hiddenIndex]), -6f, 6f);
                int weightOffset = hiddenIndex * InputSize;
                for (int inputIndex = 0; inputIndex < InputSize; inputIndex++)
                {
                    data.criticW1[weightOffset + inputIndex] += learningRate * gradient * features[inputIndex];
                }

                data.criticB1[hiddenIndex] += learningRate * gradient;
            }

            return 0.5f * error * error;
        }

        private void ActorForward(float[] features, bool[] mask, out float[] hidden, out float[] logits, out float[] probabilities, out float entropy)
        {
            hidden = ForwardHidden(features, data.actorW1, data.actorB1);
            logits = new float[ActionCount];
            float maxLogit = -30f;
            for (int action = 0; action < ActionCount; action++)
            {
                if (!mask[action])
                {
                    logits[action] = -30f;
                    continue;
                }

                float value = data.actorB2[action];
                int weightOffset = action * HiddenSize;
                for (int hiddenIndex = 0; hiddenIndex < HiddenSize; hiddenIndex++)
                {
                    value += data.actorW2[weightOffset + hiddenIndex] * hidden[hiddenIndex];
                }

                logits[action] = value;
                maxLogit = Math.Max(maxLogit, value);
            }

            probabilities = new float[ActionCount];
            float total = 0f;
            for (int action = 0; action < ActionCount; action++)
            {
                if (!mask[action])
                {
                    continue;
                }

                float probability = (float)Math.Exp(Math.Max(-40f, logits[action] - maxLogit));
                probabilities[action] = probability;
                total += probability;
            }

            if (total <= 1e-7f)
            {
                float uniform = 1f / Math.Max(1, CountValid(mask));
                for (int action = 0; action < ActionCount; action++)
                {
                    probabilities[action] = mask[action] ? uniform : 0f;
                }
            }
            else
            {
                for (int action = 0; action < ActionCount; action++)
                {
                    probabilities[action] = mask[action] ? probabilities[action] / total : 0f;
                }
            }

            entropy = 0f;
            for (int action = 0; action < ActionCount; action++)
            {
                if (probabilities[action] > 1e-7f)
                {
                    entropy -= probabilities[action] * (float)Math.Log(probabilities[action]);
                }
            }
        }

        private float CriticValue(float[] features)
        {
            CriticForward(features, out _, out float value);
            return value;
        }

        private void CriticForward(float[] features, out float[] hidden, out float value)
        {
            hidden = ForwardHidden(features, data.criticW1, data.criticB1);
            value = data.criticB2[0];
            for (int hiddenIndex = 0; hiddenIndex < HiddenSize; hiddenIndex++)
            {
                value += data.criticW2[hiddenIndex] * hidden[hiddenIndex];
            }
        }

        private static float[] ForwardHidden(float[] features, float[] weights, float[] bias)
        {
            float[] hidden = new float[HiddenSize];
            for (int hiddenIndex = 0; hiddenIndex < HiddenSize; hiddenIndex++)
            {
                float value = bias[hiddenIndex];
                int weightOffset = hiddenIndex * InputSize;
                for (int inputIndex = 0; inputIndex < InputSize; inputIndex++)
                {
                    value += weights[weightOffset + inputIndex] * features[inputIndex];
                }

                hidden[hiddenIndex] = (float)Math.Tanh(value);
            }

            return hidden;
        }

        private static int SelectAction(float[] probabilities, bool[] mask, Random random, bool trainingMode)
        {
            float exploration = trainingMode ? 1f : 0.015f;
            if (!trainingMode && random.NextDouble() > exploration)
            {
                int best = -1;
                float bestProbability = -1f;
                for (int action = 0; action < ActionCount; action++)
                {
                    if (mask[action] && probabilities[action] > bestProbability)
                    {
                        bestProbability = probabilities[action];
                        best = action;
                    }
                }

                if (best >= 0)
                {
                    return best;
                }
            }

            double roll = random.NextDouble();
            double cumulative = 0;
            int fallback = -1;
            for (int action = 0; action < ActionCount; action++)
            {
                if (!mask[action])
                {
                    continue;
                }

                fallback = action;
                cumulative += probabilities[action];
                if (roll <= cumulative)
                {
                    return action;
                }
            }

            return fallback >= 0 ? fallback : 0;
        }

        private static SolverDecision ToSolverDecision(int action, float probability, float value, bool trainingMode)
        {
            string reason = trainingMode ? "PPO policy sample" : "PPO policy";
            double score = probability * 1000.0 + value;
            if (action < PlaceActionCount)
            {
                return new SolverDecision(true, action, score, reason);
            }

            if (action == QueueSkipAction)
            {
                return new SolverDecision(true, SolverActionKind.QueueSkip, -1, -1, score, $"{reason}: queue skip");
            }

            int pickaxeTarget = action - PickaxeActionStart;
            int stackIndex = pickaxeTarget / MaxStackCapacity;
            int blockIndex = pickaxeTarget % MaxStackCapacity;
            return new SolverDecision(true, SolverActionKind.Pickaxe, stackIndex, blockIndex, score, $"{reason}: pickaxe");
        }

        private static bool[] BuildActionMask(StackMergeGameState state)
        {
            bool[] mask = new bool[ActionCount];
            for (int stackIndex = 0; stackIndex < Math.Min(MaxStacks, state.StackCount); stackIndex++)
            {
                mask[stackIndex] = state.CanPlace(stackIndex);
            }

            mask[QueueSkipAction] = state.CanSkipNextBlock();

            for (int stackIndex = 0; stackIndex < Math.Min(MaxStacks, state.StackCount); stackIndex++)
            {
                IReadOnlyList<int> stack = state.Stacks[stackIndex];
                for (int blockIndex = 0; blockIndex < Math.Min(MaxStackCapacity, stack.Count); blockIndex++)
                {
                    mask[PickaxeActionStart + stackIndex * MaxStackCapacity + blockIndex] = state.CanUsePickaxe(stackIndex, blockIndex);
                }
            }

            return mask;
        }

        private static float[] ExtractFeatures(StackMergeGameState state)
        {
            float[] features = new float[InputSize];
            int index = 0;

            void Add(float value)
            {
                if (index < features.Length)
                {
                    features[index++] = Math.Min(1.5f, Math.Max(-1.5f, value));
                }
            }

            float capacity = Math.Max(1, state.StackCapacity);
            Add(LogBlock(state.Score) / 18f);
            Add(LogBlock(state.HighestBlock) / 18f);
            Add(LogBlock(state.HighestMergedBlock) / 18f);
            Add(Math.Min(1f, state.BlocksDropped / 900f));
            Add(Math.Min(1f, state.TotalMerges / 600f));
            Add(capacity / StackMergeGameState.MaxStackCapacity);
            Add(state.QueueLength / (float)MaxQueueLength);
            Add(Math.Min(1f, state.DifficultyLevel / 3f));
            Add(Math.Min(1f, state.UnstableSavesRemaining / 5f));
            Add(Math.Min(1f, state.PickaxeUsesRemaining / 5f));
            Add(Math.Min(1f, state.QueueSkipsRemaining / 5f));
            Add(state.MirrorStackEnabled ? 1f : 0f);
            Add(state.JokerBlocksEnabled ? 1f : 0f);
            Add(FreeSlots(state) / (float)Math.Max(1, state.StackCount * state.StackCapacity));
            Add(state.GetLegalMoveIndices().Length / (float)Math.Max(1, state.StackCount));

            for (int stackIndex = 0; stackIndex < MaxStacks; stackIndex++)
            {
                IReadOnlyList<int> stack = stackIndex < state.StackCount ? state.Stacks[stackIndex] : Array.Empty<int>();
                Add(stack.Count / capacity);
                Add((capacity - stack.Count) / capacity);
                Add(stack.Count > 0 ? LogBlock(stack[^1]) / 18f : 0f);
                Add(stack.Count > 0 ? LogBlock(stack[0]) / 18f : 0f);
                Add(stack.Count >= state.StackCapacity - 1 ? 1f : 0f);
                Add(stack.Count >= 2 && stack[^1] == stack[^2] ? 1f : 0f);

                for (int blockIndex = 0; blockIndex < MaxStackCapacity; blockIndex++)
                {
                    Add(blockIndex < stack.Count ? EncodeBlock(stack[blockIndex]) : 0f);
                }
            }

            for (int queueIndex = 0; queueIndex < MaxQueueLength; queueIndex++)
            {
                Add(queueIndex < state.NextBlocks.Count ? EncodeBlock(state.NextBlocks[queueIndex]) : 0f);
            }

            int next = state.NextBlocks.Count > 0 ? state.NextBlocks[0] : -1;
            Add(state.Stacks.Count(stack => stack.Count > 0 && stack[^1] == next) / (float)Math.Max(1, state.StackCount));
            Add(CountQueueTopMatches(state) / (float)Math.Max(1, state.StackCount));
            Add(CountAdjacentEqualPairs(state) / (float)Math.Max(1, state.StackCount * state.StackCapacity));

            return features;
        }

        private static float ComputeReward(MoveResult result, StackMergeGameState nextState, long scoreBefore)
        {
            if (!result.Accepted)
            {
                return -2.0f;
            }

            float scoreDelta = Math.Max(0, nextState.Score - scoreBefore);
            float reward = 0.01f;
            reward += (float)Math.Log(1.0 + scoreDelta, 2.0) * 0.24f;
            reward += result.MergeCount * 0.44f;
            reward += LogBlock(Math.Max(result.ResultingTopValue, result.HighestBlock)) * 0.075f;
            reward += FreeSlots(nextState) * 0.006f;
            reward += nextState.GetLegalMoveIndices().Length * 0.030f;
            reward += result.UnstableSaveUsed ? 0.28f : 0f;
            reward += result.ActionKind == SolverActionKind.QueueSkip ? -0.08f : 0f;
            reward += result.ActionKind == SolverActionKind.Pickaxe ? -0.10f : 0f;
            reward -= nextState.Stacks.Count(stack => stack.Count >= nextState.StackCapacity - 1) * 0.085f;

            if (result.IsGameOver)
            {
                float scoreLog = (float)Math.Log(1.0 + Math.Max(0, nextState.Score), 2.0);
                float highLog = LogBlock(Math.Max(1, nextState.HighestMergedBlock));
                float longRun = Math.Min(1f, nextState.BlocksDropped / 520f);
                reward += scoreLog * 0.62f;
                reward += highLog * 0.95f;
                reward += Math.Min(3.2f, nextState.TotalMerges * 0.014f);
                reward += longRun * 2.2f;
                reward -= (1f - longRun) * 3.4f;
            }

            return Math.Min(24f, Math.Max(-8f, reward));
        }

        private void EnsureInitialized(Random random)
        {
            random ??= new Random(24681357);
            bool versionMismatch = data.version != Version;
            bool invalid = versionMismatch
                || data.actorW1 == null
                || data.actorW1.Length != InputSize * HiddenSize
                || data.actorB1 == null
                || data.actorB1.Length != HiddenSize
                || data.actorW2 == null
                || data.actorW2.Length != HiddenSize * ActionCount
                || data.actorB2 == null
                || data.actorB2.Length != ActionCount
                || data.criticW1 == null
                || data.criticW1.Length != InputSize * HiddenSize
                || data.criticB1 == null
                || data.criticB1.Length != HiddenSize
                || data.criticW2 == null
                || data.criticW2.Length != HiddenSize
                || data.criticB2 == null
                || data.criticB2.Length != 1;

            if (!invalid)
            {
                return;
            }

            int updates = versionMismatch ? 0 : Math.Max(0, data.updates);
            int episodes = versionMismatch ? 0 : Math.Max(0, data.episodes);
            int steps = versionMismatch ? 0 : Math.Max(0, data.steps);
            float totalReward = versionMismatch ? 0f : Math.Max(0f, data.totalReward);
            float recentAverageReward = versionMismatch ? 0f : data.recentAverageReward;
            float bestEpisodeReward = versionMismatch ? 0f : data.bestEpisodeReward;
            long bestScore = versionMismatch ? 0 : Math.Max(0, data.bestScore);
            int bestHigh = versionMismatch ? 0 : Math.Max(0, data.bestHigh);
            float recentAverageScore = versionMismatch ? 0f : Math.Max(0f, data.recentAverageScore);
            float recentAverageMoves = versionMismatch ? 0f : Math.Max(0f, data.recentAverageMoves);
            float recentAverageMerges = versionMismatch ? 0f : Math.Max(0f, data.recentAverageMerges);
            float recentAverageHigh = versionMismatch ? 0f : Math.Max(0f, data.recentAverageHigh);

            data.version = Version;
            data.updates = updates;
            data.episodes = episodes;
            data.steps = steps;
            data.totalReward = totalReward;
            data.recentAverageReward = recentAverageReward;
            data.recentAverageScore = recentAverageScore;
            data.recentAverageMoves = recentAverageMoves;
            data.recentAverageMerges = recentAverageMerges;
            data.recentAverageHigh = recentAverageHigh;
            data.bestEpisodeReward = bestEpisodeReward;
            data.bestScore = bestScore;
            data.bestHigh = bestHigh;
            data.lastPolicyLoss = 0f;
            data.lastValueLoss = 0f;
            data.lastEntropy = 0f;
            data.actorW1 = CreateWeights(InputSize * HiddenSize, random, 0.10f);
            data.actorB1 = new float[HiddenSize];
            data.actorW2 = CreateWeights(HiddenSize * ActionCount, random, 0.035f);
            data.actorB2 = new float[ActionCount];
            InitializeActionPriors(data.actorB2);
            data.criticW1 = CreateWeights(InputSize * HiddenSize, random, 0.10f);
            data.criticB1 = new float[HiddenSize];
            data.criticW2 = CreateWeights(HiddenSize, random, 0.035f);
            data.criticB2 = new float[1];
        }

        private static float[] CreateWeights(int length, Random random, float scale)
        {
            float[] weights = new float[length];
            for (int i = 0; i < weights.Length; i++)
            {
                weights[i] = (float)((random.NextDouble() * 2.0 - 1.0) * scale);
            }

            return weights;
        }

        private static void Normalize(float[] values)
        {
            if (values.Length == 0)
            {
                return;
            }

            float mean = values.Average();
            float variance = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                float delta = values[i] - mean;
                variance += delta * delta;
            }

            float std = (float)Math.Sqrt(variance / values.Length + 1e-6f);
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = (values[i] - mean) / std;
            }
        }

        private static void InitializeActionPriors(float[] actorOutputBias)
        {
            if (actorOutputBias == null || actorOutputBias.Length < ActionCount)
            {
                return;
            }

            for (int action = 0; action < actorOutputBias.Length; action++)
            {
                actorOutputBias[action] = action < PlaceActionCount
                    ? 0.45f
                    : action == QueueSkipAction
                        ? -0.65f
                        : -1.10f;
            }
        }

        private static float SmoothEpisodeMetric(float previous, float current, float smoothing)
        {
            if (previous <= 0f)
            {
                return Math.Max(0f, current);
            }

            return previous * (1f - smoothing) + Math.Max(0f, current) * smoothing;
        }

        private static int CountValid(bool[] mask)
        {
            int count = 0;
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i])
                {
                    count++;
                }
            }

            return count;
        }

        private static float EncodeBlock(int value)
        {
            return value == StackMergeGameState.JokerBlockValue ? -0.12f : LogBlock(value) / 18f;
        }

        private static float LogBlock(long value)
        {
            if (value <= 1)
            {
                return value <= 0 ? 0f : 1f;
            }

            return FloorLog2((int)Math.Min(int.MaxValue, value));
        }

        private static float LogBlock(float value)
        {
            return LogBlock((long)Math.Max(0, Math.Floor(value)));
        }

        private static float Clip(float value, float min, float max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        private static float Clamp01(float value)
        {
            return Math.Min(1f, Math.Max(0f, value));
        }

        private struct Transition
        {
            public float[] Features;
            public bool[] Mask;
            public int Action;
            public float OldLogProbability;
            public float Value;
            public float NextValue;
            public float Reward;
            public bool Done;
            public long ScoreBefore;
            public float Entropy;
        }
    }
}
