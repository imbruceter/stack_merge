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
        public int hiddenSize;
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

        // Actor: input -> hidden -> action logits.
        public float[] actorW1;
        public float[] actorB1;
        public float[] actorW2;
        public float[] actorB2;

        // Critic: input -> hidden -> value.
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

    /// <summary>
    /// Proximal Policy Optimization solver. A two-hidden-layer actor-critic MLP trained with the
    /// clipped PPO surrogate, GAE advantages, the Adam optimizer, and an entropy bonus. The reward
    /// is shaped primarily around reaching the highest merged tile, which is the player's objective
    /// for this idle game.
    /// </summary>
    public sealed class StackMergePpoAgent
    {
        private const int Version = 4;
        private const int MaxStacks = StackMergeGameState.DefaultStackCount;
        private const int MaxStackCapacity = StackMergeGameState.MaxStackCapacity;
        private const int MaxQueueLength = StackMergeGameState.DefaultQueueLength + 2;
        private const int InputSize = 112;
        // Brain width. Configurable (stored in the training data) so the research tree can grow the
        // PPO's capacity across prestiges, and so the benchmark can measure learnability per size.
        private const int DefaultHidden = 64;
        private const int MinHidden = 16;
        private const int MaxHidden = 512;
        private int hidden = DefaultHidden;
        private const int PlaceActionCount = MaxStacks;
        private const int QueueSkipAction = PlaceActionCount;
        private const int PickaxeActionStart = QueueSkipAction + 1;
        private const int ActionCount = PickaxeActionStart + MaxStacks * MaxStackCapacity;

        // Learning hyperparameters — defaults match standard PPO. Adjustable within safe bounds via
        // solver tuning (see ApplyTuning); kept as fields rather than consts for that reason.
        private float Gamma = 0.99f;
        private float Lambda = 0.95f;
        private float ClipEpsilon = 0.2f;

        private readonly StackMergePpoTrainingData data;
        private readonly List<Transition> trajectory = new(1024);
        private DenseNet actor;
        private DenseNet critic;
        private Random rng;
        private bool hasPendingTransition;
        private Transition pendingTransition;
        private float episodeReward;
        private float highRewardMultiplier = 1f;
        private float stabilityRewardMultiplier = 1f;

        public StackMergePpoAgent(StackMergePpoTrainingData data, int seed = 24681357)
        {
            this.data = data ?? new StackMergePpoTrainingData();
            rng = new Random(seed);
            EnsureInitialized(rng);
        }

        public StackMergePpoTrainingData Data => data;

        public static StackMergePpoTrainingData CloneData(StackMergePpoTrainingData source)
        {
            if (source == null)
            {
                return new StackMergePpoTrainingData();
            }

            return new StackMergePpoTrainingData
            {
                version = source.version,
                hiddenSize = source.hiddenSize,
                updates = source.updates,
                episodes = source.episodes,
                steps = source.steps,
                totalReward = source.totalReward,
                recentAverageReward = source.recentAverageReward,
                recentAverageScore = source.recentAverageScore,
                recentAverageMoves = source.recentAverageMoves,
                recentAverageMerges = source.recentAverageMerges,
                recentAverageHigh = source.recentAverageHigh,
                bestEpisodeReward = source.bestEpisodeReward,
                bestScore = source.bestScore,
                bestHigh = source.bestHigh,
                lastPolicyLoss = source.lastPolicyLoss,
                lastValueLoss = source.lastValueLoss,
                lastEntropy = source.lastEntropy,
                actorW1 = CloneArray(source.actorW1),
                actorB1 = CloneArray(source.actorB1),
                actorW2 = CloneArray(source.actorW2),
                actorB2 = CloneArray(source.actorB2),
                criticW1 = CloneArray(source.criticW1),
                criticB1 = CloneArray(source.criticB1),
                criticW2 = CloneArray(source.criticW2),
                criticB2 = CloneArray(source.criticB2)
            };
        }

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
                // Performance-driven progress: it tracks how high the agent actually plays, plus
                // how much score it earns, with a smaller contribution from raw training time.
                // The old metric was hard-capped by an unreachable score scale, which made it look
                // permanently stuck around 20% even while behaviour changed.
                float updateReadiness = 1f - (float)Math.Exp(-Math.Max(0, data.updates) / 6000f);
                float highReadiness = Clamp01(LogBlock(Math.Max(1f, data.recentAverageHigh)) / 13f);
                float scoreReadiness = Clamp01(Math.Max(0f, data.recentAverageScore) / 14000f);
                float performanceReadiness = Clamp01(highReadiness * 0.62f + scoreReadiness * 0.38f);
                return Clamp01(performanceReadiness * 0.85f + updateReadiness * 0.15f);
            }
        }

        public SolverDecision ChooseMove(StackMergeGameState state, Random random, bool trainingMode)
        {
            if (state == null || state.IsGameOver)
            {
                return SolverDecision.NoMove;
            }

            random ??= rng;

            float[] features = ExtractFeatures(state);
            bool[] mask = BuildActionMask(state);
            int validActions = CountValid(mask);
            if (validActions == 0)
            {
                return SolverDecision.NoMove;
            }

            float[] logits = actor.Forward(features);
            Softmax(logits, mask, out float[] probabilities, out float entropy);
            float value = critic.Forward(features)[0];
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
                HighBefore = Math.Max(1, state.HighestMergedBlock),
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
            transition.Reward = ComputeReward(result, nextState, transition.ScoreBefore, transition.HighBefore);
            transition.Done = result.IsGameOver;
            transition.NextValue = result.IsGameOver ? 0f : critic.Forward(nextFeatures)[0];
            trajectory.Add(transition);
            episodeReward += transition.Reward;
            data.steps++;

            // Smaller training batch (64) so the backprop happens as several small updates spread
            // through the run instead of one ~450-backprop spike at game-over — that spike was the
            // main cause of the FPS drop in Training mode (Normal mode does no updates and stays smooth).
            int targetBatch = trainingMode ? 64 : 512;
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

        public int TrainBackgroundFrames(
            int frameBudget,
            int stackCapacity,
            int queueLength,
            int difficultyLevel,
            int scalingFrequencyLevel,
            StackMergeRunModifiers modifiers,
            double scalingFrequencyRewardBonus,
            int seed)
        {
            frameBudget = Math.Max(0, frameBudget);
            if (frameBudget <= 0)
            {
                return 0;
            }

            var localRandom = new Random(seed);
            int startingSteps = Math.Max(0, data.steps);
            StackMergeGameState state = CreateBackgroundTrainingState(
                stackCapacity,
                queueLength,
                difficultyLevel,
                scalingFrequencyLevel,
                modifiers,
                scalingFrequencyRewardBonus,
                localRandom);
            int trainedFrames = 0;
            while (trainedFrames < frameBudget)
            {
                if (state.IsGameOver || state.GetLegalMoveIndices().Length == 0)
                {
                    state = CreateBackgroundTrainingState(
                        stackCapacity,
                        queueLength,
                        difficultyLevel,
                        scalingFrequencyLevel,
                        modifiers,
                        scalingFrequencyRewardBonus,
                        localRandom);
                }

                SolverDecision decision = ChooseMove(state, localRandom, trainingMode: true);
                if (!SolverScoring.CanApplyDecision(state, decision))
                {
                    hasPendingTransition = false;
                    decision = ChooseFallbackDecision(state, localRandom);
                }

                if (!decision.HasMove)
                {
                    state.ForceGameOver();
                    continue;
                }

                MoveResult result = SolverScoring.ApplyDecision(state, decision);
                if (!result.Accepted)
                {
                    hasPendingTransition = false;
                    state.ForceGameOver();
                    continue;
                }

                if (hasPendingTransition)
                {
                    // Background Datacenter learning must not make the whole game run like Training
                    // Mode. It still records real PPO transitions, but updates in the low-intensity
                    // path (larger batch, one epoch, smaller learning rate) so passive allocation
                    // stays playable while accumulating permanent knowledge.
                    Observe(result, state, trainingMode: false);
                }

                trainedFrames++;
            }

            return Math.Max(0, data.steps - startingSteps);
        }

        public void ResetForPrestige(int seed = 24681357)
        {
            rng = new Random(seed);
            trajectory.Clear();
            hasPendingTransition = false;
            pendingTransition = default;
            episodeReward = 0f;

            data.version = 0;
            data.hiddenSize = DefaultHidden;
            data.updates = 0;
            data.episodes = 0;
            data.steps = 0;
            data.totalReward = 0f;
            data.recentAverageReward = 0f;
            data.recentAverageScore = 0f;
            data.recentAverageMoves = 0f;
            data.recentAverageMerges = 0f;
            data.recentAverageHigh = 0f;
            data.bestEpisodeReward = 0f;
            data.bestScore = 0;
            data.bestHigh = 0;
            data.lastPolicyLoss = 0f;
            data.lastValueLoss = 0f;
            data.lastEntropy = 0f;
            data.actorW1 = null;
            data.actorB1 = null;
            data.actorW2 = null;
            data.actorB2 = null;
            data.criticW1 = null;
            data.criticB1 = null;
            data.criticW2 = null;
            data.criticB2 = null;
            EnsureInitialized(rng);
        }

        private static StackMergeGameState CreateBackgroundTrainingState(
            int stackCapacity,
            int queueLength,
            int difficultyLevel,
            int scalingFrequencyLevel,
            StackMergeRunModifiers modifiers,
            double scalingFrequencyRewardBonus,
            Random random)
        {
            return new StackMergeGameState(
                stackCapacity: Math.Max(StackMergeGameState.DefaultStackCapacity, stackCapacity),
                queueLength: Math.Max(1, queueLength),
                difficultyLevel: Math.Max(0, difficultyLevel),
                scalingFrequencyLevel: Math.Max(0, scalingFrequencyLevel),
                modifiers: modifiers,
                seed: random.Next(),
                scalingFrequencyRewardBonus: Math.Max(0.0, scalingFrequencyRewardBonus));
        }

        private static SolverDecision ChooseFallbackDecision(StackMergeGameState state, Random random)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            if (legalMoves.Length <= 0)
            {
                return SolverDecision.NoMove;
            }

            int stackIndex = legalMoves[random.Next(legalMoves.Length)];
            return new SolverDecision(true, stackIndex, 0.0, "Background fallback");
        }

        public void LoadSnapshot(StackMergePpoTrainingData snapshot)
        {
            CopyData(snapshot, data);
            trajectory.Clear();
            hasPendingTransition = false;
            pendingTransition = default;
            episodeReward = 0f;
            EnsureInitialized(rng);
        }

        public void ApplyResearchBonuses(float highFocusMultiplier, float stabilityMultiplier)
        {
            highRewardMultiplier = Math.Max(1f, highFocusMultiplier);
            stabilityRewardMultiplier = Math.Max(1f, stabilityMultiplier);
        }

        /// <summary>
        /// Applies the player's PPO solver-tuning to the learning hyperparameters. Values are read as
        /// absolute numbers and clamped to safe bounds so tuning can nudge behaviour without ever
        /// putting the agent into a broken regime.
        /// </summary>
        public void ApplyTuning(SolverTuningSettings tuning)
        {
            Gamma = ResolveHyper(tuning, SolverTuneParameterId.Gamma, 0.99f, 0.80f, 0.999f);
            Lambda = ResolveHyper(tuning, SolverTuneParameterId.Lambda, 0.95f, 0.80f, 0.999f);
            ClipEpsilon = ResolveHyper(tuning, SolverTuneParameterId.ClipEpsilon, 0.20f, 0.05f, 0.40f);
        }

        private static float ResolveHyper(SolverTuningSettings tuning, SolverTuneParameterId id, float fallback, float min, float max)
        {
            if (tuning.SolverId != SolverId.MachineLearning)
            {
                return fallback;
            }

            if (StackMergeSolverCatalog.GetTuningParameterIndex(SolverId.MachineLearning, id) < 0)
            {
                return fallback;
            }

            double value = tuning.GetTunedValue(id); // absolute value (base + raw * step)
            if (value <= 0)
            {
                return fallback;
            }

            return (float)Math.Min(max, Math.Max(min, value));
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

            // Adam adapts the step size per-parameter, so the base learning rate can stay roughly
            // constant. The old plain-SGD rate decayed all the way to ~0 after a few thousand
            // updates, which is why late training did nothing. Here it floors at half.
            float decay = 0.5f + 0.5f * (float)Math.Exp(-Math.Max(0, data.updates) / 40000f);
            float actorLearningRate = (trainingMode ? 0.00030f : 0.00006f) * decay;
            float criticLearningRate = (trainingMode ? 0.00080f : 0.00015f) * decay;
            float entropyCoefficient = trainingMode
                ? 0.02f * (float)Math.Exp(-Math.Max(0, data.updates) / 30000f) + 0.004f
                : 0.003f;

            // Multiple epochs per batch make the PPO clip actually matter and improve sample
            // efficiency; 2 (was 3) keeps most of that benefit while cutting the per-update backprop
            // cost by a third, which — together with the smaller batch — smooths the Training-mode FPS.
            int epochs = trainingMode ? 2 : 1;
            int[] order = new int[count];
            for (int i = 0; i < count; i++)
            {
                order[i] = i;
            }

            float totalPolicyLoss = 0f;
            float totalValueLoss = 0f;
            float totalEntropy = 0f;
            int totalSamples = 0;

            for (int epoch = 0; epoch < epochs; epoch++)
            {
                Shuffle(order);
                for (int s = 0; s < count; s++)
                {
                    int i = order[s];
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
            float[] logits = actor.Forward(transition.Features);
            Softmax(logits, transition.Mask, out float[] probabilities, out entropy);

            float newProbability = Math.Max(1e-7f, probabilities[transition.Action]);
            float ratio = newProbability / Math.Max(1e-7f, (float)Math.Exp(transition.OldLogProbability));
            bool clipped = advantage >= 0f ? ratio > 1f + ClipEpsilon : ratio < 1f - ClipEpsilon;

            // Gradient of the clipped surrogate w.r.t. the action logits:
            //   d(ratio * A)/d(logit_k) = A * ratio * (1{k = a} - prob_k)
            // and zero when the clip is active (the objective is flat there).
            float surrogateCoefficient = clipped ? 0f : Clip(advantage * ratio, -6f, 6f);

            float[] outputGradient = new float[ActionCount];
            for (int action = 0; action < ActionCount; action++)
            {
                if (!transition.Mask[action])
                {
                    outputGradient[action] = 0f;
                    continue;
                }

                float target = action == transition.Action ? 1f : 0f;
                float policyGradient = surrogateCoefficient * (target - probabilities[action]);

                // Entropy bonus gradient: dH/d(logit_k) = prob_k * (-log prob_k - H).
                float probability = probabilities[action];
                float logProbability = probability > 1e-7f ? (float)Math.Log(probability) : 0f;
                float entropyGradient = probability * (-logProbability - entropy);

                outputGradient[action] = Clip(policyGradient + entropyCoefficient * entropyGradient, -6f, 6f);
            }

            actor.Backward(outputGradient, learningRate);

            float unclipped = ratio * advantage;
            float clippedObjective = Clip(ratio, 1f - ClipEpsilon, 1f + ClipEpsilon) * advantage;
            return -Math.Min(unclipped, clippedObjective);
        }

        private float TrainCritic(float[] features, float targetReturn, float learningRate)
        {
            float value = critic.Forward(features)[0];
            float error = Clip(targetReturn - value, -12f, 12f);
            critic.Backward(new[] { error }, learningRate);
            return 0.5f * error * error;
        }

        private static void Softmax(float[] logits, bool[] mask, out float[] probabilities, out float entropy)
        {
            probabilities = new float[ActionCount];
            float maxLogit = -1e30f;
            for (int action = 0; action < ActionCount; action++)
            {
                if (mask[action] && logits[action] > maxLogit)
                {
                    maxLogit = logits[action];
                }
            }

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

        private static int SelectAction(float[] probabilities, bool[] mask, Random random, bool trainingMode)
        {
            if (!trainingMode)
            {
                // Greedy play with a tiny amount of exploration to break ties / avoid loops.
                if (random.NextDouble() > 0.02)
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
            int next = state.NextBlocks.Count > 0 ? state.NextBlocks[0] : -1;

            // Global context.
            Add(LogBlock(state.Score) / 16f);
            Add(LogBlock(state.HighestMergedBlock) / 14f);
            Add(LogBlock(state.HighestBlock) / 14f);
            Add(Math.Min(1f, state.BlocksDropped / 600f));
            Add(Math.Min(1f, state.TotalMerges / 400f));
            Add(capacity / StackMergeGameState.MaxStackCapacity);
            Add(FreeSlots(state) / (float)Math.Max(1, state.StackCount * state.StackCapacity));
            Add(state.GetLegalMoveIndices().Length / (float)Math.Max(1, state.StackCount));
            Add(Math.Min(1f, state.DifficultyLevel / 3f));
            Add(Math.Min(1f, state.UnstableSavesRemaining / 5f));
            Add(Math.Min(1f, state.PickaxeUsesRemaining / 5f));
            Add(Math.Min(1f, state.QueueSkipsRemaining / 5f));
            Add(state.MirrorStackEnabled ? 1f : 0f);
            Add(state.JokerBlocksEnabled ? 1f : 0f);
            Add(state.QueueLength / (float)MaxQueueLength);

            // Visible queue.
            for (int queueIndex = 0; queueIndex < MaxQueueLength; queueIndex++)
            {
                Add(queueIndex < state.NextBlocks.Count ? EncodeBlock(state.NextBlocks[queueIndex]) : 0f);
            }

            // Per-stack summary — gives the policy the signal it needs to choose a target stack.
            for (int stackIndex = 0; stackIndex < MaxStacks; stackIndex++)
            {
                IReadOnlyList<int> stack = stackIndex < state.StackCount ? state.Stacks[stackIndex] : Array.Empty<int>();
                int height = stack.Count;
                int top = height > 0 ? stack[^1] : 0;
                int bottom = height > 0 ? stack[0] : 0;
                bool mergesIfPlaced = height > 0 && (
                    top == next
                    || (state.JokerBlocksEnabled && next == StackMergeGameState.JokerBlockValue && top > 0)
                    || (state.MirrorStackEnabled && bottom == next));

                Add(height / capacity);
                Add((capacity - height) / capacity);
                Add(height > 0 ? LogBlock(top) / 14f : 0f);
                Add(height > 0 ? LogBlock(bottom) / 14f : 0f);
                Add(height >= state.StackCapacity - 1 ? 1f : 0f);
                Add(height >= 2 && stack[^1] == stack[^2] ? 1f : 0f);
                Add(mergesIfPlaced ? 1f : 0f);
                Add(stackIndex < state.StackCount && state.CanPlace(stackIndex) ? 1f : 0f);
                Add(height > 0 && top == next ? 1f : 0f);
            }

            // Full board contents (lets the policy reason about pickaxe targets).
            for (int stackIndex = 0; stackIndex < MaxStacks; stackIndex++)
            {
                IReadOnlyList<int> stack = stackIndex < state.StackCount ? state.Stacks[stackIndex] : Array.Empty<int>();
                for (int blockIndex = 0; blockIndex < MaxStackCapacity; blockIndex++)
                {
                    Add(blockIndex < stack.Count ? EncodeBlock(stack[blockIndex]) : 0f);
                }
            }

            // Board-wide opportunity counts.
            Add(state.Stacks.Count(stack => stack.Count > 0 && stack[^1] == next) / (float)Math.Max(1, state.StackCount));
            Add(CountQueueTopMatches(state) / (float)Math.Max(1, state.StackCount));
            Add(CountAdjacentEqualPairs(state) / (float)Math.Max(1, state.StackCount * state.StackCapacity));

            return features;
        }

        private float ComputeReward(MoveResult result, StackMergeGameState nextState, long scoreBefore, int highBefore)
        {
            if (!result.Accepted)
            {
                return -1.5f;
            }

            // Tiny alive bonus + a small dense nudge toward productive moves. Deliberately small:
            // a flat per-merge reward (what this used to have) makes "churn many tiny 2->4 merges"
            // the most rewarding behaviour, which is exactly why runs had high moves/merges but a
            // low highest tile. The dominant signal must come from reaching higher tiers instead.
            float reward = 0.005f;
            float scoreDelta = Math.Max(0, nextState.Score - scoreBefore);
            reward += (float)Math.Log(1.0 + scoreDelta, 2.0) * 0.015f;

            // THE core objective: reaching a NEW highest tier. Rewarded densely (the moment it
            // happens, so credit assignment isn't discounted away over a 400-step episode) and
            // CONVEXLY in the tier, so climbing 1024 -> 2048 -> 4096 is worth far more than churning
            // small merges. A churned board that never beats its own record earns almost nothing.
            int highAfter = Math.Max(1, nextState.HighestMergedBlock);
            int high = Math.Max(1, highBefore);
            if (highAfter > high)
            {
                float tier = LogBlock(highAfter);
                reward += tier * tier * 0.06f * highRewardMultiplier;
            }

            // Light positional shaping.
            reward += result.UnstableSaveUsed ? 0.1f : 0f;
            reward -= result.ActionKind == SolverActionKind.QueueSkip ? 0.04f : 0f;
            reward -= result.ActionKind == SolverActionKind.Pickaxe ? 0.04f : 0f;

            int danger = 0;
            foreach (IReadOnlyList<int> stack in nextState.Stacks)
            {
                if (stack.Count >= nextState.StackCapacity - 1)
                {
                    danger++;
                }
            }

            reward -= danger * (0.05f / stabilityRewardMultiplier);

            if (result.IsGameOver)
            {
                // Superlinear in the final tier so the end-of-run signal sharply separates a weak
                // finish (high 128 -> ~7) from a strong one (high 2048 -> ~18, high 16384 -> ~29).
                // That gap is what teaches the agent which runs were genuinely good rather than
                // merely long — i.e. how to learn from a bad run.
                float highLog = LogBlock(highAfter);
                float survival = Math.Min(1f, nextState.BlocksDropped / 450f);
                reward += highLog * highLog * 0.15f * highRewardMultiplier;
                reward += survival * 0.6f * stabilityRewardMultiplier;
                reward -= (1f - survival) * (1.2f / stabilityRewardMultiplier);
            }

            return Math.Min(40f, Math.Max(-6f, reward));
        }

        private void EnsureInitialized(Random random)
        {
            random ??= new Random(24681357);
            hidden = data.hiddenSize > 0 ? Math.Min(MaxHidden, Math.Max(MinHidden, data.hiddenSize)) : DefaultHidden;
            data.hiddenSize = hidden;

            bool versionMismatch = data.version != Version;
            bool invalid = versionMismatch
                || !HasSize(data.actorW1, InputSize * hidden)
                || !HasSize(data.actorB1, hidden)
                || !HasSize(data.actorW2, hidden * ActionCount)
                || !HasSize(data.actorB2, ActionCount)
                || !HasSize(data.criticW1, InputSize * hidden)
                || !HasSize(data.criticB1, hidden)
                || !HasSize(data.criticW2, hidden * 1)
                || !HasSize(data.criticB2, 1);

            if (invalid)
            {
                if (versionMismatch)
                {
                    data.version = Version;
                    data.updates = 0;
                    data.episodes = 0;
                    data.steps = 0;
                    data.totalReward = 0f;
                    data.recentAverageReward = 0f;
                    data.recentAverageScore = 0f;
                    data.recentAverageMoves = 0f;
                    data.recentAverageMerges = 0f;
                    data.recentAverageHigh = 0f;
                    data.bestEpisodeReward = 0f;
                    data.bestScore = 0;
                    data.bestHigh = 0;
                    data.lastPolicyLoss = 0f;
                    data.lastValueLoss = 0f;
                    data.lastEntropy = 0f;
                }

                data.version = Version;
                data.hiddenSize = hidden;
                // Xavier-style scaling for tanh layers keeps activations in a sane range.
                data.actorW1 = CreateWeights(InputSize * hidden, random, (float)Math.Sqrt(1.0 / InputSize));
                data.actorB1 = new float[hidden];
                data.actorW2 = CreateWeights(hidden * ActionCount, random, 0.01f);
                data.actorB2 = new float[ActionCount];
                InitializeActionPriors(data.actorB2);
                data.criticW1 = CreateWeights(InputSize * hidden, random, (float)Math.Sqrt(1.0 / InputSize));
                data.criticB1 = new float[hidden];
                data.criticW2 = CreateWeights(hidden * 1, random, 0.01f);
                data.criticB2 = new float[1];
            }

            actor = new DenseNet(
                new[] { InputSize, hidden, ActionCount },
                new[] { data.actorW1, data.actorW2 },
                new[] { data.actorB1, data.actorB2 });
            critic = new DenseNet(
                new[] { InputSize, hidden, 1 },
                new[] { data.criticW1, data.criticW2 },
                new[] { data.criticB1, data.criticB2 });
        }

        private static bool HasSize(float[] array, int size)
        {
            return array != null && array.Length == size;
        }

        private static float[] CloneArray(float[] source)
        {
            return source == null ? null : (float[])source.Clone();
        }

        private static void CopyData(StackMergePpoTrainingData source, StackMergePpoTrainingData target)
        {
            source ??= new StackMergePpoTrainingData();
            target.version = source.version;
            target.hiddenSize = source.hiddenSize;
            target.updates = source.updates;
            target.episodes = source.episodes;
            target.steps = source.steps;
            target.totalReward = source.totalReward;
            target.recentAverageReward = source.recentAverageReward;
            target.recentAverageScore = source.recentAverageScore;
            target.recentAverageMoves = source.recentAverageMoves;
            target.recentAverageMerges = source.recentAverageMerges;
            target.recentAverageHigh = source.recentAverageHigh;
            target.bestEpisodeReward = source.bestEpisodeReward;
            target.bestScore = source.bestScore;
            target.bestHigh = source.bestHigh;
            target.lastPolicyLoss = source.lastPolicyLoss;
            target.lastValueLoss = source.lastValueLoss;
            target.lastEntropy = source.lastEntropy;
            target.actorW1 = CloneArray(source.actorW1);
            target.actorB1 = CloneArray(source.actorB1);
            target.actorW2 = CloneArray(source.actorW2);
            target.actorB2 = CloneArray(source.actorB2);
            target.criticW1 = CloneArray(source.criticW1);
            target.criticB1 = CloneArray(source.criticB1);
            target.criticW2 = CloneArray(source.criticW2);
            target.criticB2 = CloneArray(source.criticB2);
        }

        private static float[] CreateWeights(int length, Random random, float scale)
        {
            float[] weights = new float[length];
            for (int i = 0; i < weights.Length; i++)
            {
                // Approximately Gaussian via sum of uniforms, scaled.
                double sample = (random.NextDouble() + random.NextDouble() + random.NextDouble() - 1.5) / 1.5;
                weights[i] = (float)(sample * scale);
            }

            return weights;
        }

        private void Shuffle(int[] order)
        {
            for (int i = order.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
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
            return value == StackMergeGameState.JokerBlockValue ? -0.12f : LogBlock(value) / 14f;
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
            public int HighBefore;
            public float Entropy;
        }

        /// <summary>
        /// A small fully-connected network with tanh hidden layers and a linear output. It operates
        /// in place on the weight/bias arrays it is handed (which live on the serialized training
        /// data) and keeps its own transient Adam moment buffers. Forward caches activations so the
        /// immediately following Backward can backpropagate without recomputing them.
        /// </summary>
        private sealed class DenseNet
        {
            private const float Beta1 = 0.9f;
            private const float Beta2 = 0.999f;
            private const float Eps = 1e-8f;

            private readonly int[] sizes;
            private readonly int layers;
            private readonly float[][] w;
            private readonly float[][] b;
            private readonly float[][] mw;
            private readonly float[][] vw;
            private readonly float[][] mb;
            private readonly float[][] vb;
            private readonly float[][] act;
            private float[] deltaA;
            private float[] deltaB;
            private int adamT;

            public DenseNet(int[] sizes, float[][] weights, float[][] biases)
            {
                this.sizes = sizes;
                layers = weights.Length;
                w = weights;
                b = biases;
                mw = new float[layers][];
                vw = new float[layers][];
                mb = new float[layers][];
                vb = new float[layers][];
                for (int l = 0; l < layers; l++)
                {
                    mw[l] = new float[w[l].Length];
                    vw[l] = new float[w[l].Length];
                    mb[l] = new float[b[l].Length];
                    vb[l] = new float[b[l].Length];
                }

                act = new float[layers + 1][];
                int maxWidth = sizes[0];
                for (int l = 1; l <= layers; l++)
                {
                    act[l] = new float[sizes[l]];
                    if (sizes[l] > maxWidth)
                    {
                        maxWidth = sizes[l];
                    }
                }

                deltaA = new float[maxWidth];
                deltaB = new float[maxWidth];
            }

            public float[] Forward(float[] input)
            {
                act[0] = input;
                for (int l = 0; l < layers; l++)
                {
                    float[] x = act[l];
                    float[] y = act[l + 1];
                    float[] wl = w[l];
                    float[] bl = b[l];
                    int inSize = sizes[l];
                    int outSize = sizes[l + 1];
                    bool hidden = l < layers - 1;
                    for (int o = 0; o < outSize; o++)
                    {
                        float sum = bl[o];
                        int baseIdx = o * inSize;
                        for (int i = 0; i < inSize; i++)
                        {
                            sum += wl[baseIdx + i] * x[i];
                        }

                        y[o] = hidden ? (float)Math.Tanh(sum) : sum;
                    }
                }

                return act[layers];
            }

            /// <summary>
            /// Backpropagates the supplied output gradient (gradient of the objective w.r.t. the
            /// network output) and applies an Adam ascent step with the given learning rate.
            /// Must be called immediately after <see cref="Forward"/> with the same sample.
            /// </summary>
            public void Backward(float[] outputGradient, float learningRate)
            {
                adamT++;
                float biasCorrection1 = 1f - (float)Math.Pow(Beta1, adamT);
                float biasCorrection2 = 1f - (float)Math.Pow(Beta2, adamT);

                float[] delta = deltaA;
                float[] nextDelta = deltaB;
                int outputSize = sizes[layers];
                for (int o = 0; o < outputSize; o++)
                {
                    delta[o] = outputGradient[o];
                }

                for (int l = layers - 1; l >= 0; l--)
                {
                    int inSize = sizes[l];
                    int outSize = sizes[l + 1];
                    float[] inp = act[l];
                    float[] wl = w[l];
                    float[] bl = b[l];
                    float[] mwl = mw[l];
                    float[] vwl = vw[l];
                    float[] mbl = mb[l];
                    float[] vbl = vb[l];

                    // Propagate the gradient to this layer's input (before its tanh) using the
                    // current weights — must be computed before the weights are updated below.
                    if (l > 0)
                    {
                        for (int i = 0; i < inSize; i++)
                        {
                            float sum = 0f;
                            for (int o = 0; o < outSize; o++)
                            {
                                sum += wl[o * inSize + i] * delta[o];
                            }

                            float a = inp[i];
                            nextDelta[i] = sum * (1f - a * a);
                        }
                    }

                    for (int o = 0; o < outSize; o++)
                    {
                        float d = delta[o];
                        if (d > 6f)
                        {
                            d = 6f;
                        }
                        else if (d < -6f)
                        {
                            d = -6f;
                        }

                        mbl[o] = Beta1 * mbl[o] + (1f - Beta1) * d;
                        vbl[o] = Beta2 * vbl[o] + (1f - Beta2) * d * d;
                        bl[o] += learningRate * (mbl[o] / biasCorrection1) / ((float)Math.Sqrt(vbl[o] / biasCorrection2) + Eps);

                        int baseIdx = o * inSize;
                        for (int i = 0; i < inSize; i++)
                        {
                            float g = d * inp[i];
                            int idx = baseIdx + i;
                            mwl[idx] = Beta1 * mwl[idx] + (1f - Beta1) * g;
                            vwl[idx] = Beta2 * vwl[idx] + (1f - Beta2) * g * g;
                            wl[idx] += learningRate * (mwl[idx] / biasCorrection1) / ((float)Math.Sqrt(vwl[idx] / biasCorrection2) + Eps);
                        }
                    }

                    float[] tmp = delta;
                    delta = nextDelta;
                    nextDelta = tmp;
                }
            }
        }
    }
}
