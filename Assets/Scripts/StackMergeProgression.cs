using System;
using System.Linq;
using UnityEngine;

namespace StackMerge
{
    [Serializable]
    public sealed class StackMergeProgressionData
    {
        public long chips;
        public int selectedSolver;
        public int highestUnlockedSolver;
        public bool[] solverUnlocked;
        public int[] solverMergeTuning;
        public int[] solverSafetyTuning;
        public int[] solverLookaheadTuning;
        public int[] solverTuningValues;
        public bool solverTuningUnlocked;
        public long tokens;
        public int speedLevel;
        public bool autoSolveUnlocked;
        public bool autoSolveEnabled;
        public bool autoRestartUnlocked;
        public bool autoRestartEnabled;
        public int stackCapacityLevel;
        public int queuePreviewLevel;
        public int incomeLevel;
        public int difficultyLevel;
        public bool modifiersMenuUnlocked;
        public int[] modifierLevels;
        public int runsCompleted;
        public bool agentsMenuUnlocked;
        public bool extraAgentSlotUnlocked;
        public bool[] agentUnlocked;
        public int[] activeAgentIds;
        public RunHistoryEntry[] runHistory;
        public long totalChipsEarned;
        public long totalChipsSpent;
        public int manualRunsCompleted;
        public int totalBlocksDropped;
        public int totalMerges;
        public int highestBlockEver;
        public long bestRunScore;
        public int mergeTokenProgress;
        public bool machineLearningTrainingMode;
        public float machineLearningExperience;
        public int machineLearningRuns;
        public long machineLearningBestScore;
        public int machineLearningBestHigh;
        public StackMergePpoTrainingData machineLearningPolicy;
    }

    [Serializable]
    public sealed class RunHistoryEntry
    {
        public int runIndex;
        public int solverId;
        public long score;
        public int moves;
        public int merges;
        public int highestMergedBlock;
        public int difficultyLevel;
    }

    public enum AgentId
    {
        MergeBroker = 0,
        HighwaterAnalyst = 1,
        ScoreAuditor = 2,
        Overclocker = 3,
        Quartermaster = 4,
        RestartSponsor = 5,
        TokenProspector = 6,
        MoveDividend = 7,
        VelocityTrader = 8
    }

    public readonly struct AgentDefinition
    {
        public AgentDefinition(AgentId id, string displayName, long cost, string lockedHint, string description)
        {
            Id = id;
            DisplayName = displayName;
            Cost = cost;
            LockedHint = lockedHint;
            Description = description;
        }

        public AgentId Id { get; }

        public string DisplayName { get; }

        public long Cost { get; }

        public string LockedHint { get; }

        public string Description { get; }
    }

    public enum AchievementMetric
    {
        TotalChipsEarned,
        TotalChipsSpent,
        RunsCompleted,
        ManualRunsCompleted,
        TotalMerges,
        HighestBlockEver
    }

    public enum ModifierId
    {
        UnstableStack = 0,
        CatalystStack = 1,
        MirrorStack = 2,
        Joker = 3,
        MinersPickaxe = 4,
        QueueScrubber = 5,
        NeuralAccelerator = 6
    }

    public readonly struct ModifierDefinition
    {
        public ModifierDefinition(ModifierId id, string displayName, string lockedHint, string description, params int[] costs)
        {
            Id = id;
            DisplayName = displayName;
            LockedHint = lockedHint;
            Description = description;
            Costs = costs ?? Array.Empty<int>();
        }

        public ModifierId Id { get; }

        public string DisplayName { get; }

        public string LockedHint { get; }

        public string Description { get; }

        public int[] Costs { get; }

        public int MaxLevel => Costs.Length;
    }

    public readonly struct AchievementDefinition
    {
        public AchievementDefinition(int id, string displayName, string description, AchievementMetric metric, long target)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            Metric = metric;
            Target = target;
        }

        public int Id { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public AchievementMetric Metric { get; }

        public long Target { get; }
    }

    public sealed class StackMergeProgression
    {
        private const string PlayerPrefsKey = "StackMerge.Progression.v2";
        private const int BaseAgentSlots = 2;
        private const int AutoSolveUnlockCost = 140;
        private const int AutoRestartUnlockCost = 220;
        private const int SolverTuningUnlockCost = 700;
        private const int TokenPackCost = 300;
        private const int TokenPackSize = 50;
        private const int ExtraAgentSlotUpgradeCost = 1800;
        private const int ModifiersMenuUnlockCost = 5000;
        private const int ModifierGateRuns = 20;
        private const int ModifierGateSolvers = 7;
        private const int ModifierGateBestScore = 8000;
        private const int ModifierGateMerges = 1000;
        private const int ModifierGateHighestBlock = 1024;
        private const int TokenProspectorMergeTarget = 8;

        private static readonly int[] SpeedUpgradeCosts = { 20, 55, 130, 300, 680 };
        private static readonly float[] MoveIntervals = { 0.18f, 0.12f, 0.08f, 0.055f, 0.035f, 0.022f };
        private static readonly int[] StackCapacityCosts = { 60, 140, 320, 720, 1600 };
        private static readonly int[] QueuePreviewUpgradeCosts = { 260, 900 };
        private static readonly int[] IncomeUpgradeCosts = { 90, 220, 520, 1200, 2800 };
        private static readonly int[] DifficultyUpgradeCosts = { 350, 1200, 3600 };
        private const int AgentsMenuUnlockCost = 650;
        private const int MaxHistoryEntries = 250;

        public static readonly AgentDefinition[] Agents =
        {
            new(AgentId.MergeBroker, "Merge Broker", 120, "Boosts merge income.", "+75% chips from merge rewards."),
            new(AgentId.HighwaterAnalyst, "Highwater Analyst", 240, "Rewards new highs.", "+140% chips from new highest-block rewards."),
            new(AgentId.ScoreAuditor, "Score Auditor", 420, "Turns score into chips.", "+60% chips from end-of-run score bonus."),
            new(AgentId.Overclocker, "Overclocker", 680, "Runs the solver faster.", "Solver move interval is 25% shorter."),
            new(AgentId.Quartermaster, "Quartermaster", 950, "Improves baseline income.", "+4 chips on every successful placement."),
            new(AgentId.RestartSponsor, "Restart Sponsor", 1500, "Keeps restarts funded.", "Auto Restart consumes no tokens while this agent is active."),
            new(AgentId.TokenProspector, "Token Prospector", 1800, "Turns merge volume into restart fuel.", $"+1 token for every {TokenProspectorMergeTarget} merges while active."),
            new(AgentId.MoveDividend, "Move Dividend", 2200, "Rewards long, stable runs.", "End-of-run chips gain a bonus from total moves completed."),
            new(AgentId.VelocityTrader, "Velocity Trader", 3000, "Rewards fast solvers.", "End-of-run chips gain a throughput bonus from moves per second.")
        };

        public static readonly ModifierDefinition[] Modifiers =
        {
            new(ModifierId.UnstableStack, "Unstable Stack", "Deletes bottom blocks when a full stack would fail.", "Each level gives one rescue per run: if a full stack receives a non-merge block, its bottom block is removed without reducing score.", 650, 1300, 2600, 5200, 10400),
            new(ModifierId.CatalystStack, "Catalyst Stack", "Converts merges into more chips.", "Permanent unlock: merge rewards are doubled on every run after purchase.", 2400),
            new(ModifierId.MirrorStack, "Mirror Stack", "Lets stack ends interact.", "Unlocks a special merge: if the top and bottom block of a stack match, they merge through the stack.", 2400),
            new(ModifierId.Joker, "Joker", "Adds wild blocks to the queue.", "Unlocks occasional Joker blocks. A Joker placed onto any block merges with it.", 3600),
            new(ModifierId.MinersPickaxe, "Miner's Pickaxe", "Lets solvers remove blocks from the board.", "Each level gives one solver-controlled pickaxe use per run. The solver may delete any block in any stack, including middle blocks, to open space or trigger merges.", 1600, 3200, 6400, 12800, 25600),
            new(ModifierId.QueueScrubber, "Queue Scrubber", "Lets solvers delete bad upcoming blocks.", "Each level gives one solver-controlled queue skip per run. The current next block is removed and the following block moves forward.", 1400, 2800, 5600, 11200, 22400),
            new(ModifierId.NeuralAccelerator, "Neural Accelerator", "Speeds up expensive solvers.", "Permanent unlock: MOCA, MOCA+, and MCTS run about twice as fast. Negative speed tuning on those solvers is also twice as effective.", 48000)
        };

        public static readonly AchievementDefinition[] Achievements =
        {
            new(0, "Chip Spark", "Earn 100 total chips.", AchievementMetric.TotalChipsEarned, 100),
            new(1, "Chip Engine", "Earn 1,000 total chips.", AchievementMetric.TotalChipsEarned, 1000),
            new(2, "Chip Grid", "Earn 10,000 total chips.", AchievementMetric.TotalChipsEarned, 10000),
            new(3, "First Investment", "Spend 100 chips on upgrades, agents, or algorithms.", AchievementMetric.TotalChipsSpent, 100),
            new(4, "Lab Budget", "Spend 1,000 chips.", AchievementMetric.TotalChipsSpent, 1000),
            new(5, "Capital Flow", "Spend 10,000 chips.", AchievementMetric.TotalChipsSpent, 10000),
            new(6, "Run Loop", "Complete 5 runs.", AchievementMetric.RunsCompleted, 5),
            new(7, "Run Habit", "Complete 25 runs.", AchievementMetric.RunsCompleted, 25),
            new(8, "Run Factory", "Complete 100 runs.", AchievementMetric.RunsCompleted, 100),
            new(9, "Hands On", "Complete 1 run without any auto-solver moves.", AchievementMetric.ManualRunsCompleted, 1),
            new(10, "Manual Tuning", "Complete 10 manual runs.", AchievementMetric.ManualRunsCompleted, 10),
            new(11, "Human Benchmark", "Complete 25 manual runs.", AchievementMetric.ManualRunsCompleted, 25),
            new(12, "Merge Warmup", "Create 50 total merges.", AchievementMetric.TotalMerges, 50),
            new(13, "Merge Engine", "Create 250 total merges.", AchievementMetric.TotalMerges, 250),
            new(14, "Merge Reactor", "Create 1,000 total merges.", AchievementMetric.TotalMerges, 1000),
            new(15, "First Tower", "Reach block 64.", AchievementMetric.HighestBlockEver, 64),
            new(16, "Signal Peak", "Reach block 256.", AchievementMetric.HighestBlockEver, 256),
            new(17, "Stack Singularity", "Reach block 1,024.", AchievementMetric.HighestBlockEver, 1024)
        };

        private readonly StackMergeProgressionData data;
        private readonly StackMergePpoAgent machineLearningAgent;

        public StackMergeProgression(StackMergeProgressionData data)
        {
            this.data = data ?? new StackMergeProgressionData();
            Normalize();
            machineLearningAgent = new StackMergePpoAgent(this.data.machineLearningPolicy);
        }

        public long Chips => data.chips;

        public long Tokens => data.tokens;

        public SolverId SelectedSolver => (SolverId)data.selectedSolver;

        public int SpeedLevel => data.speedLevel;

        public bool HasPurchasedSolver => data.solverUnlocked != null && data.solverUnlocked.Skip(1).Any(unlocked => unlocked);

        public int UnlockedSolverCount => data.solverUnlocked?.Count(unlocked => unlocked) ?? 1;

        public bool SolverTuningUnlocked => data.solverTuningUnlocked;

        public bool AutoSolveUnlocked => data.autoSolveUnlocked;

        public bool AutoSolveEnabled
        {
            get => data.autoSolveEnabled;
            set => data.autoSolveEnabled = data.autoSolveUnlocked && value;
        }

        public bool AutoRestartUnlocked => data.autoRestartUnlocked;

        public bool AutoRestartEnabled => data.autoRestartEnabled;

        public bool AutoRestartIsTokenFree => IsAgentActive(AgentId.RestartSponsor);

        public int StackCapacity => StackMergeGameState.DefaultStackCapacity + data.stackCapacityLevel;

        public int StackCapacityLevel => data.stackCapacityLevel;

        public int MaxStackCapacityLevel => StackCapacityCosts.Length;

        public int QueueLength => StackMergeGameState.DefaultQueueLength + data.queuePreviewLevel;

        public int QueuePreviewLevel => data.queuePreviewLevel;

        public int MaxQueuePreviewLevel => QueuePreviewUpgradeCosts.Length;

        public int IncomeLevel => data.incomeLevel;

        public int MaxIncomeLevel => IncomeUpgradeCosts.Length;

        public int DifficultyLevel => data.difficultyLevel;

        public int MaxDifficultyLevel => DifficultyUpgradeCosts.Length;

        public int RunsCompleted => data.runsCompleted;

        public bool AgentsMenuUnlocked => data.agentsMenuUnlocked;

        public bool ExtraAgentSlotUnlocked => data.extraAgentSlotUnlocked;

        public bool ModifiersMenuUnlocked => data.modifiersMenuUnlocked;

        public bool AllModifiersMaxed => data.modifiersMenuUnlocked && Modifiers.All(modifier => IsModifierMaxed(modifier.Id));

        public bool NeuralAcceleratorActive => data.modifiersMenuUnlocked && GetModifierLevel(ModifierId.NeuralAccelerator) > 0;

        public bool CanUnlockMachineLearning => AllModifiersMaxed;

        public bool MachineLearningTrainingMode
        {
            get => data.machineLearningTrainingMode;
            set => data.machineLearningTrainingMode = value;
        }

        // Normal ("Playing") mode for PPO unlocks only after this many trained frames; until then the
        // agent can only be run in Training mode.
        public const long PlayingModeFrameRequirement = 500000;

        public long MachineLearningFrames => machineLearningAgent?.Metrics.Steps ?? Math.Max(0, (long)data.machineLearningExperience);

        public bool MachineLearningPlayingModeUnlocked => MachineLearningFrames >= PlayingModeFrameRequirement;

        // PPO runs in Training mode whenever it hasn't unlocked Playing mode yet, or when the player
        // has explicitly chosen Training. Once Playing mode is unlocked the player can switch it off.
        public bool IsMachineLearningTrainingActive => SelectedSolver == SolverId.MachineLearning
            && IsSolverUnlocked(SolverId.MachineLearning)
            && (!MachineLearningPlayingModeUnlocked || data.machineLearningTrainingMode);

        public void SetMachineLearningTrainingMode(bool training)
        {
            // Playing (normal) mode can only be selected once it has been unlocked.
            data.machineLearningTrainingMode = training || !MachineLearningPlayingModeUnlocked;
        }

        public StackMergePpoAgent MachineLearningAgent => machineLearningAgent;

        public float MachineLearningExperience => machineLearningAgent != null
            ? machineLearningAgent.Metrics.Steps
            : Math.Max(0f, data.machineLearningExperience);

        public float MachineLearningSkill => machineLearningAgent?.TrainingProgress ?? 0f;

        public int MachineLearningLevel => machineLearningAgent?.Level ?? 0;

        public int MachineLearningRuns => Math.Max(data.machineLearningRuns, machineLearningAgent?.Metrics.Episodes ?? 0);

        public long MachineLearningBestScore => Math.Max(data.machineLearningBestScore, data.machineLearningPolicy?.bestScore ?? 0);

        public int MachineLearningBestHigh => Math.Max(data.machineLearningBestHigh, data.machineLearningPolicy?.bestHigh ?? 0);

        public bool CanUnlockModifiersMenu => data.agentsMenuUnlocked
            && UnlockedSolverCount >= ModifierGateSolvers
            && data.runsCompleted >= ModifierGateRuns
            && data.totalMerges >= ModifierGateMerges
            && data.bestRunScore >= ModifierGateBestScore
            && data.highestBlockEver >= ModifierGateHighestBlock;

        public RunHistoryEntry[] RunHistory => data.runHistory ?? Array.Empty<RunHistoryEntry>();

        public long TotalChipsEarned => data.totalChipsEarned;

        public long TotalChipsSpent => data.totalChipsSpent;

        public int ManualRunsCompleted => data.manualRunsCompleted;

        public int TotalBlocksDropped => data.totalBlocksDropped;

        public int TotalMerges => data.totalMerges;

        public int HighestBlockEver => data.highestBlockEver;

        public long BestRunScore => data.bestRunScore;

        public int MonteCarloSimulationCount => 3 + data.speedLevel * 2;

        public int MonteCarloRolloutDepth => 3 + data.speedLevel;

        public float MoveInterval => GetMoveInterval(SelectedSolver);

        public bool IsMaxSpeed => data.speedLevel >= MoveIntervals.Length - 1;

        public int MaxSpeedLevel => SpeedUpgradeCosts.Length;

        public bool IsMaxStackCapacity => StackCapacity >= StackMergeGameState.MaxStackCapacity;

        public bool IsMaxQueuePreview => data.queuePreviewLevel >= QueuePreviewUpgradeCosts.Length;

        public bool IsMaxIncome => data.incomeLevel >= IncomeUpgradeCosts.Length;

        public bool IsMaxDifficulty => data.difficultyLevel >= DifficultyUpgradeCosts.Length;

        public int ActiveAgentSlots => BaseAgentSlots + (data.extraAgentSlotUnlocked ? 1 : 0);

        public int MaxAgentSlots => BaseAgentSlots + 1;

        public float GetMoveInterval(SolverId solverId)
        {
            float baseInterval = MoveIntervals[Mathf.Clamp(data.speedLevel, 0, MoveIntervals.Length - 1)];
            float minInterval = solverId == SolverId.MachineLearning ? 0.006f : 0.012f;
            float trainingMultiplier = solverId == SolverId.MachineLearning && data.machineLearningTrainingMode ? 0.70f : 1f;
            return Mathf.Max(minInterval, baseInterval * GetSolverPacingMultiplier(solverId) * AgentMoveIntervalMultiplier * trainingMultiplier);
        }

        public double GetHighestBlockRewardMultiplier(int highestBlock)
        {
            int value = Math.Max(1, highestBlock);
            int log = FloorLog2(value);
            if (log >= 10)
            {
                return Math.Min(1_000_000_000.0, Math.Pow(10.0, log - 9));
            }

            return log switch
            {
                >= 9 => 7.5,
                >= 8 => 5.0,
                >= 7 => 3.0,
                >= 6 => 2.0,
                >= 5 => 1.45,
                _ => 1.0
            };
        }

        public static StackMergeProgression Load()
        {
            string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new StackMergeProgression(new StackMergeProgressionData());
            }

            try
            {
                return new StackMergeProgression(JsonUtility.FromJson<StackMergeProgressionData>(json));
            }
            catch (Exception)
            {
                return new StackMergeProgression(new StackMergeProgressionData());
            }
        }

        private bool saveDirty;

        /// <summary>
        /// True when in-memory state has changed since the last disk write.
        /// </summary>
        public bool HasUnsavedChanges => saveDirty;

        /// <summary>
        /// Marks the progression as dirty. The actual disk write is deferred and
        /// throttled by the caller (see <see cref="FlushIfDirty"/>). Serializing the
        /// full state — which includes the PPO network weights — on every single move
        /// was the dominant per-move cost and tanked the frame rate, so the hot path
        /// must never touch the disk directly.
        /// </summary>
        public void Save()
        {
            saveDirty = true;
        }

        /// <summary>
        /// Immediately serializes and flushes the progression to PlayerPrefs.
        /// Use sparingly (quit, pause, periodic autosave) — not in the per-move loop.
        /// </summary>
        public void SaveImmediate()
        {
            data.machineLearningPolicy = machineLearningAgent?.Data ?? data.machineLearningPolicy;
            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
            saveDirty = false;
        }

        /// <summary>
        /// Writes pending changes to disk if any exist. Returns true when a write happened.
        /// </summary>
        public bool FlushIfDirty()
        {
            if (!saveDirty)
            {
                return false;
            }

            SaveImmediate();
            return true;
        }

        public bool IsSolverUnlocked(SolverId solverId)
        {
            int index = (int)solverId;
            return index >= 0 && index < data.solverUnlocked.Length && data.solverUnlocked[index];
        }

        public string GetMachineLearningGateStatus()
        {
            if (IsSolverUnlocked(SolverId.MachineLearning))
            {
                return GetMachineLearningStatus();
            }

            return CanUnlockMachineLearning
                ? "Ready: every Modifier is fully purchased."
                : $"Requires every Modifier maxed: {Modifiers.Count(modifier => IsModifierMaxed(modifier.Id))}/{Modifiers.Length}";
        }

        public string GetMachineLearningStatus()
        {
            return machineLearningAgent != null
                ? machineLearningAgent.BuildStatus(MachineLearningBestScore, MachineLearningBestHigh, IsMachineLearningTrainingActive)
                : "PPO model is not initialized yet.";
        }

        public void ToggleMachineLearningTrainingMode()
        {
            if (IsSolverUnlocked(SolverId.MachineLearning))
            {
                data.machineLearningTrainingMode = !data.machineLearningTrainingMode;
            }
        }

        public SolverTuningSettings GetSolverTuning(SolverId solverId)
        {
            int index = ClampSolverIndex(solverId);
            int[] values = new int[SolverTuningSettings.MaxSlots];
            int offset = index * SolverTuningSettings.MaxSlots;
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = data.solverTuningValues[offset + i];
            }

            return new SolverTuningSettings((SolverId)index, values);
        }

        public int GetSolverTuningValue(SolverId solverId, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SolverTuningSettings.MaxSlots)
            {
                return 0;
            }

            int index = ClampSolverIndex(solverId);
            return data.solverTuningValues[index * SolverTuningSettings.MaxSlots + slotIndex];
        }

        public int GetSolverTuningValue(SolverId solverId, SolverTuneParameterId parameterId)
        {
            int slotIndex = StackMergeSolverCatalog.GetTuningParameterIndex(solverId, parameterId);
            return slotIndex >= 0 ? GetSolverTuningValue(solverId, slotIndex) : 0;
        }

        public void SetSolverTuningValue(SolverId solverId, int slotIndex, int value)
        {
            if (slotIndex < 0 || slotIndex >= SolverTuningSettings.MaxSlots)
            {
                return;
            }

            int index = ClampSolverIndex(solverId);
            data.solverTuningValues[index * SolverTuningSettings.MaxSlots + slotIndex] = SolverTuningSettings.ClampValue((SolverId)index, slotIndex, value);
        }

        public void ResetSolverTuning(SolverId solverId)
        {
            int index = ClampSolverIndex(solverId);
            int offset = index * SolverTuningSettings.MaxSlots;
            for (int i = 0; i < SolverTuningSettings.MaxSlots; i++)
            {
                data.solverTuningValues[offset + i] = 0;
            }
        }

        public long GetSolverTuningUnlockCost()
        {
            return data.solverTuningUnlocked ? 0 : SolverTuningUnlockCost;
        }

        public bool BuySolverTuningUnlock()
        {
            if (data.solverTuningUnlocked)
            {
                return true;
            }

            if (!Spend(SolverTuningUnlockCost))
            {
                return false;
            }

            data.solverTuningUnlocked = true;
            return true;
        }

        public long GetSolverUnlockCost(SolverId solverId)
        {
            return StackMergeSolverCatalog.GetDefinition(solverId).Cost;
        }

        public string GetSolverDescription(SolverId solverId)
        {
            return IsSolverUnlocked(solverId)
                ? StackMergeSolverCatalog.GetDefinition(solverId).Description
                : StackMergeSolverCatalog.GetDefinition(solverId).LockedHint;
        }

        public bool SelectOrUnlockSolver(SolverId solverId)
        {
            int index = (int)solverId;
            if (index < 0 || index >= data.solverUnlocked.Length)
            {
                return false;
            }

            if (IsSolverUnlocked(solverId))
            {
                data.selectedSolver = index;
                return true;
            }

            if (solverId == SolverId.MachineLearning && !CanUnlockMachineLearning)
            {
                return false;
            }

            long cost = GetSolverUnlockCost(solverId);
            if (!Spend(cost))
            {
                return false;
            }

            data.solverUnlocked[index] = true;
            data.highestUnlockedSolver = Math.Max(data.highestUnlockedSolver, index);
            data.selectedSolver = index;
            return true;
        }

        public long GetSpeedUpgradeCost()
        {
            return IsMaxSpeed ? 0 : SpeedUpgradeCosts[data.speedLevel];
        }

        public long GetSpeedUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < SpeedUpgradeCosts.Length ? SpeedUpgradeCosts[upgradeIndex] : 0;
        }

        public bool BuySpeedUpgrade()
        {
            if (IsMaxSpeed || !Spend(GetSpeedUpgradeCost()))
            {
                return false;
            }

            data.speedLevel++;
            return true;
        }

        public bool BuySpeedUpgrade(int upgradeIndex)
        {
            return upgradeIndex == data.speedLevel && BuySpeedUpgrade();
        }

        public long GetAutoSolveCost()
        {
            return data.autoSolveUnlocked ? 0 : AutoSolveUnlockCost;
        }

        public bool ToggleOrBuyAutoSolve()
        {
            if (!data.autoSolveUnlocked)
            {
                if (!HasPurchasedSolver || !Spend(AutoSolveUnlockCost))
                {
                    return false;
                }

                data.autoSolveUnlocked = true;
                data.autoSolveEnabled = true;
                return true;
            }

            data.autoSolveEnabled = !data.autoSolveEnabled;
            return true;
        }

        public long GetAutoRestartCost()
        {
            return data.autoRestartUnlocked ? 0 : AutoRestartUnlockCost;
        }

        public bool ToggleOrBuyAutoRestart()
        {
            if (!data.autoRestartUnlocked)
            {
                if (!HasPurchasedSolver)
                {
                    return false;
                }

                if (!Spend(GetAutoRestartCost()))
                {
                    return false;
                }

                data.autoRestartUnlocked = true;
                data.autoRestartEnabled = true;
                return true;
            }

            data.autoRestartEnabled = !data.autoRestartEnabled;
            return true;
        }

        public long GetTokenPackCost()
        {
            return TokenPackCost;
        }

        public int GetTokenPackSize()
        {
            return TokenPackSize;
        }

        public bool BuyTokenPack()
        {
            if (!Spend(TokenPackCost))
            {
                return false;
            }

            data.tokens += TokenPackSize;
            return true;
        }

        public bool TryConsumeAutoRestartToken()
        {
            if (!data.autoRestartUnlocked || !data.autoRestartEnabled)
            {
                return false;
            }

            if (AutoRestartIsTokenFree)
            {
                return true;
            }

            if (data.tokens <= 0)
            {
                return false;
            }

            data.tokens--;
            return true;
        }

        public long GetStackCapacityUpgradeCost()
        {
            return IsMaxStackCapacity ? 0 : StackCapacityCosts[data.stackCapacityLevel];
        }

        public long GetStackCapacityUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < StackCapacityCosts.Length ? StackCapacityCosts[upgradeIndex] : 0;
        }

        public bool BuyStackCapacityUpgrade()
        {
            if (IsMaxStackCapacity || !Spend(GetStackCapacityUpgradeCost()))
            {
                return false;
            }

            data.stackCapacityLevel++;
            return true;
        }

        public bool BuyStackCapacityUpgrade(int upgradeIndex)
        {
            return upgradeIndex == data.stackCapacityLevel && BuyStackCapacityUpgrade();
        }

        public long GetQueuePreviewUpgradeCost()
        {
            return IsMaxQueuePreview ? 0 : QueuePreviewUpgradeCosts[data.queuePreviewLevel];
        }

        public long GetQueuePreviewUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < QueuePreviewUpgradeCosts.Length ? QueuePreviewUpgradeCosts[upgradeIndex] : 0;
        }

        public bool BuyQueuePreviewUpgrade()
        {
            if (IsMaxQueuePreview || !Spend(GetQueuePreviewUpgradeCost()))
            {
                return false;
            }

            data.queuePreviewLevel++;
            return true;
        }

        public bool BuyQueuePreviewUpgrade(int upgradeIndex)
        {
            return upgradeIndex == data.queuePreviewLevel && BuyQueuePreviewUpgrade();
        }

        public long GetIncomeUpgradeCost()
        {
            return IsMaxIncome ? 0 : IncomeUpgradeCosts[data.incomeLevel];
        }

        public long GetIncomeUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < IncomeUpgradeCosts.Length ? IncomeUpgradeCosts[upgradeIndex] : 0;
        }

        public bool BuyIncomeUpgrade()
        {
            if (IsMaxIncome || !Spend(GetIncomeUpgradeCost()))
            {
                return false;
            }

            data.incomeLevel++;
            return true;
        }

        public bool BuyIncomeUpgrade(int upgradeIndex)
        {
            return upgradeIndex == data.incomeLevel && BuyIncomeUpgrade();
        }

        public long GetDifficultyUpgradeCost()
        {
            return IsMaxDifficulty ? 0 : DifficultyUpgradeCosts[data.difficultyLevel];
        }

        public long GetDifficultyUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < DifficultyUpgradeCosts.Length ? DifficultyUpgradeCosts[upgradeIndex] : 0;
        }

        public bool BuyDifficultyUpgrade()
        {
            if (IsMaxDifficulty || !Spend(GetDifficultyUpgradeCost()))
            {
                return false;
            }

            data.difficultyLevel++;
            return true;
        }

        public bool BuyDifficultyUpgrade(int upgradeIndex)
        {
            return upgradeIndex == data.difficultyLevel && BuyDifficultyUpgrade();
        }

        public long GetAgentsMenuUnlockCost()
        {
            return data.agentsMenuUnlocked ? 0 : AgentsMenuUnlockCost;
        }

        public bool BuyAgentsMenuUnlock()
        {
            if (data.agentsMenuUnlocked)
            {
                return true;
            }

            if (!Spend(AgentsMenuUnlockCost))
            {
                return false;
            }

            data.agentsMenuUnlocked = true;
            return true;
        }

        public long GetExtraAgentSlotUpgradeCost()
        {
            return data.extraAgentSlotUnlocked ? 0 : ExtraAgentSlotUpgradeCost;
        }

        public bool BuyExtraAgentSlotUpgrade()
        {
            if (data.extraAgentSlotUnlocked)
            {
                return true;
            }

            if (!Spend(ExtraAgentSlotUpgradeCost))
            {
                return false;
            }

            data.extraAgentSlotUnlocked = true;
            return true;
        }

        public long GetModifiersMenuUnlockCost()
        {
            return data.modifiersMenuUnlocked ? 0 : ModifiersMenuUnlockCost;
        }

        public string GetModifiersGateStatus()
        {
            if (data.modifiersMenuUnlocked)
            {
                return "Modifier Lab unlocked";
            }

            return $"Requires: Agents {FormatGate(data.agentsMenuUnlocked)}, Solvers {UnlockedSolverCount}/{ModifierGateSolvers}, Runs {data.runsCompleted}/{ModifierGateRuns}, Merges {data.totalMerges}/{ModifierGateMerges}, Best {data.bestRunScore}/{ModifierGateBestScore}, High {data.highestBlockEver}/{ModifierGateHighestBlock}";
        }

        public bool BuyModifiersMenuUnlock()
        {
            if (data.modifiersMenuUnlocked)
            {
                return true;
            }

            if (!CanUnlockModifiersMenu || !Spend(ModifiersMenuUnlockCost))
            {
                return false;
            }

            data.modifiersMenuUnlocked = true;
            return true;
        }

        public ModifierDefinition GetModifierDefinition(ModifierId modifierId)
        {
            return Modifiers[(int)modifierId];
        }

        public int GetModifierLevel(ModifierId modifierId)
        {
            int index = (int)modifierId;
            return index >= 0 && index < data.modifierLevels.Length ? data.modifierLevels[index] : 0;
        }

        public bool IsModifierMaxed(ModifierId modifierId)
        {
            ModifierDefinition definition = GetModifierDefinition(modifierId);
            return GetModifierLevel(modifierId) >= definition.MaxLevel;
        }

        public long GetModifierUpgradeCost(ModifierId modifierId)
        {
            ModifierDefinition definition = GetModifierDefinition(modifierId);
            int level = GetModifierLevel(modifierId);
            return level >= definition.MaxLevel ? 0 : definition.Costs[level];
        }

        public bool BuyModifierUpgrade(ModifierId modifierId)
        {
            int index = (int)modifierId;
            if (!data.modifiersMenuUnlocked || index < 0 || index >= data.modifierLevels.Length || IsModifierMaxed(modifierId))
            {
                return false;
            }

            if (!Spend(GetModifierUpgradeCost(modifierId)))
            {
                return false;
            }

            data.modifierLevels[index]++;
            return true;
        }

        public StackMergeRunModifiers BuildRunModifiers()
        {
            if (!data.modifiersMenuUnlocked)
            {
                return default;
            }

            return new StackMergeRunModifiers(
                GetModifierLevel(ModifierId.UnstableStack),
                GetModifierLevel(ModifierId.MirrorStack) > 0,
                GetModifierLevel(ModifierId.Joker) > 0,
                GetModifierLevel(ModifierId.MinersPickaxe),
                GetModifierLevel(ModifierId.QueueScrubber));
        }

        private static string FormatGate(bool value)
        {
            return value ? "yes" : "no";
        }

        private static string FormatNumber(long value)
        {
            return value >= 1_000_000
                ? $"{value / 1_000_000d:0.##}M"
                : value >= 1_000
                    ? $"{value / 1_000d:0.##}K"
                    : value.ToString();
        }

        public AgentDefinition GetAgentDefinition(AgentId agentId)
        {
            return Agents[(int)agentId];
        }

        public bool IsAgentUnlocked(AgentId agentId)
        {
            int index = (int)agentId;
            return index >= 0 && index < data.agentUnlocked.Length && data.agentUnlocked[index];
        }

        public bool IsAgentActive(AgentId agentId)
        {
            return data.agentsMenuUnlocked && data.activeAgentIds.Any(id => id == (int)agentId);
        }

        public int ActiveAgentCount => data.activeAgentIds.Count(id => id >= 0);

        public int GetActiveAgentIdAtSlot(int slotIndex)
        {
            return slotIndex >= 0 && slotIndex < data.activeAgentIds.Length ? data.activeAgentIds[slotIndex] : -1;
        }

        public string GetAgentInfo(AgentId agentId)
        {
            AgentDefinition definition = GetAgentDefinition(agentId);
            if (!data.agentsMenuUnlocked)
            {
                return "Unlock the Agents menu in Upgrades to inspect managers.";
            }

            return IsAgentUnlocked(agentId) ? definition.Description : definition.LockedHint;
        }

        public bool BuyAgent(AgentId agentId)
        {
            int index = (int)agentId;
            if (!data.agentsMenuUnlocked || index < 0 || index >= Agents.Length || IsAgentUnlocked(agentId))
            {
                return false;
            }

            if (!Spend(Agents[index].Cost))
            {
                return false;
            }

            data.agentUnlocked[index] = true;
            return true;
        }

        public bool EquipAgent(AgentId agentId)
        {
            if (!data.agentsMenuUnlocked || !IsAgentUnlocked(agentId))
            {
                return false;
            }

            return TryEquipAgent(agentId);
        }

        public bool UnequipAgent(AgentId agentId)
        {
            if (!IsAgentActive(agentId))
            {
                return false;
            }

            UnequipAgentInternal(agentId);
            return true;
        }

        public bool BuyOrToggleAgent(AgentId agentId)
        {
            int index = (int)agentId;
            if (!data.agentsMenuUnlocked || index < 0 || index >= Agents.Length)
            {
                return false;
            }

            if (!IsAgentUnlocked(agentId))
            {
                if (!Spend(Agents[index].Cost))
                {
                    return false;
                }

                data.agentUnlocked[index] = true;
                TryEquipAgent(agentId);
                return true;
            }

            if (IsAgentActive(agentId))
            {
                UnequipAgentInternal(agentId);
                return true;
            }

            return TryEquipAgent(agentId);
        }

        public long AwardMove(MoveResult result, bool suppressChips = false)
        {
            if (!result.Accepted)
            {
                return 0;
            }

            double highestMultiplier = GetHighestBlockRewardMultiplier(Math.Max(result.HighestBlock, result.ResultingTopValue));
            long placement = result.ActionKind == SolverActionKind.Place ? 2 + AgentFlatPlacementBonus : 0;
            double merge = result.MergeCount <= 0
                ? 0
                : result.MergeCount * Math.Max(1, result.ResultingTopValue) * highestMultiplier * 0.55;
            long highest = result.MergeCount > 0 && result.ResultingTopValue >= result.HighestBlock
                ? (long)Math.Ceiling(Math.Max(1, result.HighestBlock) * highestMultiplier * 0.85)
                : 0;

            long gained = placement;
            gained += (long)Math.Ceiling(merge * AgentMergeMultiplier * ModifierMergeMultiplier);
            gained += (long)Math.Ceiling(highest * AgentHighestMultiplier);
            if (gained > 0 && !suppressChips)
            {
                gained = ApplyStageMultiplier(gained);
                gained = ApplyIncomeMultiplier(gained);
                data.chips += gained;
                data.totalChipsEarned += gained;
            }
            else if (suppressChips)
            {
                gained = 0;
            }

            if (result.ActionKind == SolverActionKind.Place)
            {
                data.totalBlocksDropped++;
            }

            data.totalMerges += Math.Max(0, result.MergeCount);
            data.highestBlockEver = Math.Max(data.highestBlockEver, result.HighestBlock);
            if (!suppressChips)
            {
                AwardMergeTokens(result.MergeCount);
            }

            return gained;
        }

        public long AwardRunCompleted(long runScore)
        {
            return AwardRunCompleted(runScore, SelectedSolver, 0, 0, 0, false);
        }

        public long AwardRunCompleted(long runScore, SolverId solverId, int moves, int merges, int highestMergedBlock)
        {
            return AwardRunCompleted(runScore, solverId, moves, merges, highestMergedBlock, false);
        }

        public long AwardRunCompleted(long runScore, SolverId solverId, int moves, int merges, int highestMergedBlock, bool manualRun)
        {
            return AwardRunCompleted(runScore, solverId, moves, merges, highestMergedBlock, manualRun, 0f);
        }

        public long AwardRunCompleted(long runScore, SolverId solverId, int moves, int merges, int highestMergedBlock, bool manualRun, float elapsedSeconds, bool suppressChips = false)
        {
            data.runsCompleted++;
            if (manualRun)
            {
                data.manualRunsCompleted++;
            }

            data.bestRunScore = Math.Max(data.bestRunScore, Math.Max(0, runScore));
            data.highestBlockEver = Math.Max(data.highestBlockEver, highestMergedBlock);
            double highestMultiplier = GetHighestBlockRewardMultiplier(highestMergedBlock);
            double scoreBonus = Math.Max(1, runScore) * 0.22 * highestMultiplier * AgentScoreMultiplier;
            double moveBonus = IsAgentActive(AgentId.MoveDividend)
                ? Math.Max(0, moves) * Math.Max(1.0, highestMultiplier * 0.35) * 4.0
                : 0;
            double speedBonus = IsAgentActive(AgentId.VelocityTrader) && elapsedSeconds > 0.01f
                ? scoreBonus * Math.Min(2.5, Math.Max(0.0, (moves / elapsedSeconds) - 1.0) * 0.18)
                : 0;
            long bonus = Math.Max(1, (long)Math.Ceiling(scoreBonus + moveBonus + speedBonus));
            if (!suppressChips)
            {
                bonus = ApplyStageMultiplier(bonus);
                bonus = ApplyIncomeMultiplier(bonus);
                data.chips += bonus;
                data.totalChipsEarned += bonus;
            }
            else
            {
                bonus = 0;
            }

            RecordRunHistory(runScore, solverId, moves, merges, highestMergedBlock);
            return bonus;
        }

        public float AwardMachineLearningRun(long runScore, int moves, int merges, int highestMergedBlock, bool trainingMode)
        {
            data.machineLearningRuns++;
            data.machineLearningBestScore = Math.Max(data.machineLearningBestScore, Math.Max(0, runScore));
            data.machineLearningBestHigh = Math.Max(data.machineLearningBestHigh, Math.Max(0, highestMergedBlock));
            return machineLearningAgent?.Metrics.LastPolicyLoss ?? 0f;
        }

        public void ObserveMachineLearningMove(MoveResult result, StackMergeGameState stateAfterMove, bool trainingMode)
        {
            machineLearningAgent?.Observe(result, stateAfterMove, trainingMode);
        }

        public void FlushMachineLearningTraining(bool trainingMode)
        {
            machineLearningAgent?.ForceUpdate(trainingMode);
        }

        public long GetAchievementProgress(AchievementDefinition achievement)
        {
            return achievement.Metric switch
            {
                AchievementMetric.TotalChipsEarned => TotalChipsEarned,
                AchievementMetric.TotalChipsSpent => TotalChipsSpent,
                AchievementMetric.RunsCompleted => RunsCompleted,
                AchievementMetric.ManualRunsCompleted => ManualRunsCompleted,
                AchievementMetric.TotalMerges => TotalMerges,
                AchievementMetric.HighestBlockEver => HighestBlockEver,
                _ => 0
            };
        }

        public bool IsAchievementComplete(AchievementDefinition achievement)
        {
            return GetAchievementProgress(achievement) >= achievement.Target;
        }

        private void RecordRunHistory(long runScore, SolverId solverId, int moves, int merges, int highestMergedBlock)
        {
            RunHistoryEntry[] existing = RunHistory;
            int nextLength = Math.Min(MaxHistoryEntries, existing.Length + 1);
            RunHistoryEntry[] updated = new RunHistoryEntry[nextLength];
            updated[0] = new RunHistoryEntry
            {
                runIndex = data.runsCompleted,
                solverId = (int)solverId,
                score = Math.Max(0, runScore),
                moves = Math.Max(0, moves),
                merges = Math.Max(0, merges),
                highestMergedBlock = Math.Max(0, highestMergedBlock),
                difficultyLevel = data.difficultyLevel
            };

            for (int i = 1; i < updated.Length; i++)
            {
                updated[i] = existing[i - 1];
            }

            data.runHistory = updated;
        }

        private bool TryEquipAgent(AgentId agentId)
        {
            if (!data.agentsMenuUnlocked)
            {
                return false;
            }

            if (IsAgentActive(agentId))
            {
                return true;
            }

            int emptySlot = Array.FindIndex(data.activeAgentIds, id => id < 0);
            if (emptySlot < 0)
            {
                return false;
            }

            if (ActiveAgentCount >= ActiveAgentSlots)
            {
                return false;
            }

            data.activeAgentIds[emptySlot] = (int)agentId;
            return true;
        }

        private void UnequipAgentInternal(AgentId agentId)
        {
            for (int i = 0; i < data.activeAgentIds.Length; i++)
            {
                if (data.activeAgentIds[i] == (int)agentId)
                {
                    data.activeAgentIds[i] = -1;
                }
            }

            TrimActiveAgentsToSlotLimit();
        }

        private void TrimActiveAgentsToSlotLimit()
        {
            int allowedSlots = ActiveAgentSlots;
            int activeCount = 0;
            for (int i = 0; i < data.activeAgentIds.Length; i++)
            {
                if (data.activeAgentIds[i] < 0)
                {
                    continue;
                }

                if (i >= allowedSlots)
                {
                    data.activeAgentIds[i] = -1;
                    continue;
                }

                activeCount++;
                if (activeCount > allowedSlots)
                {
                    data.activeAgentIds[i] = -1;
                }
            }
        }

        private double AgentMergeMultiplier => IsAgentActive(AgentId.MergeBroker) ? 1.75 : 1.0;

        private double AgentHighestMultiplier => IsAgentActive(AgentId.HighwaterAnalyst) ? 2.40 : 1.0;

        private double AgentScoreMultiplier => IsAgentActive(AgentId.ScoreAuditor) ? 1.60 : 1.0;

        private float AgentMoveIntervalMultiplier => IsAgentActive(AgentId.Overclocker) ? 0.75f : 1f;

        private int AgentFlatPlacementBonus => IsAgentActive(AgentId.Quartermaster) ? 4 : 0;

        private double ModifierMergeMultiplier => GetModifierLevel(ModifierId.CatalystStack) > 0 ? 2.0 : 1.0;

        private long ApplyStageMultiplier(long amount)
        {
            double multiplier = data.modifiersMenuUnlocked ? 24.0 : data.agentsMenuUnlocked ? 5.0 : 1.0;
            return Math.Max(1, (long)Math.Ceiling(amount * multiplier));
        }

        private long ApplyIncomeMultiplier(long amount)
        {
            return Math.Max(1, (long)Math.Ceiling(amount * (1.0 + data.incomeLevel * 0.35)));
        }

        private void AwardMergeTokens(int mergeCount)
        {
            if (!IsAgentActive(AgentId.TokenProspector) || mergeCount <= 0)
            {
                return;
            }

            data.mergeTokenProgress += mergeCount;
            while (data.mergeTokenProgress >= TokenProspectorMergeTarget)
            {
                data.mergeTokenProgress -= TokenProspectorMergeTarget;
                data.tokens++;
            }
        }

        private static float GetSolverPacingMultiplier(SolverId solverId)
        {
            return solverId switch
            {
                SolverId.Rand => 0.25f,
                SolverId.Merge => 0.34f,
                SolverId.Balance => 0.44f,
                SolverId.Heur => 0.50f,
                SolverId.Look => 0.60f,
                SolverId.Combo => 0.62f,
                SolverId.AntiStall => 0.64f,
                SolverId.Plan3 => 0.78f,
                SolverId.Plan5 => 0.95f,
                SolverId.Moca => 0.92f,
                SolverId.MocaPlus => 1.05f,
                SolverId.Mcts => 1.12f,
                SolverId.MachineLearning => 0.20f,
                _ => 1f
            };
        }

        public static float ComputeMachineLearningExperienceGain(long runScore, int moves, int merges, int highestMergedBlock, bool trainingMode)
        {
            double scoreValue = Math.Sqrt(Math.Max(0, runScore)) * 0.10;
            double moveValue = Math.Max(0, moves) * 0.025;
            double mergeValue = Math.Max(0, merges) * 0.12;
            double highValue = FloorLog2(Math.Max(1, highestMergedBlock)) * 1.6;
            double baseGain = Math.Max(1.0, scoreValue + moveValue + mergeValue + highValue);
            return (float)Math.Min(500.0, baseGain * (trainingMode ? 1.0 : 0.35));
        }

        public static float ComputeMachineLearningSkill(float experience)
        {
            experience = Math.Max(0f, experience);
            return Mathf.Clamp01(1f - Mathf.Exp(-experience / 650f));
        }

        public static int ComputeMachineLearningLevel(float experience)
        {
            return Mathf.Clamp(Mathf.FloorToInt(ComputeMachineLearningSkill(experience) * 100f), 0, 100);
        }

        private bool Spend(long cost)
        {
            if (cost < 0 || data.chips < cost)
            {
                return false;
            }

            data.chips -= cost;
            data.totalChipsSpent += cost;
            return true;
        }

        private void Normalize()
        {
            int solverCount = StackMergeSolverCatalog.Definitions.Length;
            if (data.solverUnlocked == null || data.solverUnlocked.Length != solverCount)
            {
                bool[] migrated = new bool[solverCount];
                migrated[0] = true;
                for (int i = 0; i <= data.highestUnlockedSolver && i < migrated.Length; i++)
                {
                    migrated[i] = true;
                }

                if (data.solverUnlocked != null)
                {
                    for (int i = 0; i < data.solverUnlocked.Length && i < migrated.Length; i++)
                    {
                        migrated[i] |= data.solverUnlocked[i];
                    }
                }

                data.solverUnlocked = migrated;
            }

            data.solverUnlocked[0] = true;
            data.solverMergeTuning = NormalizeSolverTuningArray(data.solverMergeTuning, solverCount);
            data.solverSafetyTuning = NormalizeSolverTuningArray(data.solverSafetyTuning, solverCount);
            data.solverLookaheadTuning = NormalizeSolverTuningArray(data.solverLookaheadTuning, solverCount);
            data.solverTuningValues = NormalizeSolverTuningValues(data.solverTuningValues, solverCount);
            MigrateLegacySolverTunings();
            data.selectedSolver = Mathf.Clamp(data.selectedSolver, 0, solverCount - 1);
            if (!data.solverUnlocked[data.selectedSolver])
            {
                data.selectedSolver = 0;
            }

            data.highestUnlockedSolver = Math.Max(data.highestUnlockedSolver, data.selectedSolver);
            data.tokens = Math.Max(0, data.tokens);
            data.mergeTokenProgress = Mathf.Clamp(data.mergeTokenProgress, 0, TokenProspectorMergeTarget - 1);
            if (!data.autoSolveUnlocked)
            {
                data.autoSolveEnabled = false;
            }

            if (!data.autoRestartUnlocked)
            {
                data.autoRestartEnabled = false;
            }

            data.speedLevel = Mathf.Clamp(data.speedLevel, 0, MoveIntervals.Length - 1);
            data.stackCapacityLevel = Mathf.Clamp(data.stackCapacityLevel, 0, StackMergeGameState.MaxStackCapacity - StackMergeGameState.DefaultStackCapacity);
            data.queuePreviewLevel = Mathf.Clamp(data.queuePreviewLevel, 0, QueuePreviewUpgradeCosts.Length);
            data.incomeLevel = Mathf.Clamp(data.incomeLevel, 0, IncomeUpgradeCosts.Length);
            data.difficultyLevel = Mathf.Clamp(data.difficultyLevel, 0, DifficultyUpgradeCosts.Length);
            data.modifierLevels = NormalizeModifierLevels(data.modifierLevels);
            if (!data.modifiersMenuUnlocked && data.modifierLevels.Any(level => level > 0))
            {
                data.modifiersMenuUnlocked = true;
            }

            if (data.agentUnlocked == null || data.agentUnlocked.Length != Agents.Length)
            {
                bool[] migrated = new bool[Agents.Length];
                if (data.agentUnlocked != null)
                {
                    for (int i = 0; i < data.agentUnlocked.Length && i < migrated.Length; i++)
                    {
                        migrated[i] = data.agentUnlocked[i];
                    }
                }

                data.agentUnlocked = migrated;
            }

            if (data.activeAgentIds == null || data.activeAgentIds.Length != 3)
            {
                int[] migrated = { -1, -1, -1 };
                if (data.activeAgentIds != null)
                {
                    for (int i = 0; i < data.activeAgentIds.Length && i < migrated.Length; i++)
                    {
                        migrated[i] = data.activeAgentIds[i];
                    }
                }

                data.activeAgentIds = migrated;
            }

            if (!data.agentsMenuUnlocked)
            {
                bool hadAgentProgress = data.agentUnlocked.Any(unlocked => unlocked)
                    || data.activeAgentIds.Any(id => id >= 0);
                data.agentsMenuUnlocked = hadAgentProgress;
            }

            if (!data.extraAgentSlotUnlocked && data.activeAgentIds.Length > BaseAgentSlots && data.activeAgentIds[BaseAgentSlots] >= 0)
            {
                data.extraAgentSlotUnlocked = true;
            }

            for (int i = 0; i < data.activeAgentIds.Length; i++)
            {
                int agentId = data.activeAgentIds[i];
                if (agentId < 0 || agentId >= Agents.Length || !data.agentUnlocked[agentId])
                {
                    data.activeAgentIds[i] = -1;
                }
            }

            TrimActiveAgentsToSlotLimit();

            if (data.runHistory == null)
            {
                data.runHistory = Array.Empty<RunHistoryEntry>();
            }
            else if (data.runHistory.Length > MaxHistoryEntries)
            {
                data.runHistory = data.runHistory.Take(MaxHistoryEntries).Where(entry => entry != null).ToArray();
            }
            else
            {
                data.runHistory = data.runHistory.Where(entry => entry != null).ToArray();
            }

            data.totalChipsSpent = Math.Max(0, data.totalChipsSpent);
            data.totalChipsEarned = Math.Max(data.totalChipsEarned, data.chips + data.totalChipsSpent);
            data.manualRunsCompleted = Mathf.Clamp(data.manualRunsCompleted, 0, Math.Max(0, data.runsCompleted));
            data.totalBlocksDropped = Math.Max(0, data.totalBlocksDropped);
            data.totalMerges = Math.Max(data.totalMerges, data.runHistory.Sum(entry => Math.Max(0, entry.merges)));
            data.highestBlockEver = Math.Max(2, data.highestBlockEver);
            data.machineLearningExperience = Math.Max(0f, data.machineLearningExperience);
            data.machineLearningRuns = Math.Max(0, data.machineLearningRuns);
            data.machineLearningBestScore = Math.Max(0, data.machineLearningBestScore);
            data.machineLearningBestHigh = Math.Max(0, data.machineLearningBestHigh);
            data.machineLearningPolicy ??= new StackMergePpoTrainingData();
            if (!IsSolverUnlocked(SolverId.MachineLearning))
            {
                data.machineLearningTrainingMode = false;
            }

            if (data.runHistory.Length > 0)
            {
                data.highestBlockEver = Math.Max(data.highestBlockEver, data.runHistory.Max(entry => Math.Max(0, entry.highestMergedBlock)));
                data.bestRunScore = Math.Max(data.bestRunScore, data.runHistory.Max(entry => Math.Max(0, entry.score)));
            }
        }

        private int ClampSolverIndex(SolverId solverId)
        {
            int solverCount = StackMergeSolverCatalog.Definitions.Length;
            return Mathf.Clamp((int)solverId, 0, solverCount - 1);
        }

        private static int[] NormalizeSolverTuningArray(int[] source, int solverCount)
        {
            int[] normalized = new int[solverCount];
            if (source == null)
            {
                return normalized;
            }

            for (int i = 0; i < source.Length && i < normalized.Length; i++)
            {
                normalized[i] = SolverTuningSettings.ClampValue(source[i]);
            }

            return normalized;
        }

        private static int[] NormalizeSolverTuningValues(int[] source, int solverCount)
        {
            int[] normalized = new int[solverCount * SolverTuningSettings.MaxSlots];
            if (source == null)
            {
                return normalized;
            }

            for (int i = 0; i < source.Length && i < normalized.Length; i++)
            {
                SolverId solverId = (SolverId)(i / SolverTuningSettings.MaxSlots);
                int slotIndex = i % SolverTuningSettings.MaxSlots;
                normalized[i] = SolverTuningSettings.ClampValue(solverId, slotIndex, source[i]);
            }

            return normalized;
        }

        private static int[] NormalizeModifierLevels(int[] source)
        {
            int[] normalized = new int[Modifiers.Length];
            if (source == null)
            {
                return normalized;
            }

            for (int i = 0; i < source.Length && i < normalized.Length; i++)
            {
                normalized[i] = Mathf.Clamp(source[i], 0, Modifiers[i].MaxLevel);
            }

            return normalized;
        }

        private void MigrateLegacySolverTunings()
        {
            if (data.solverMergeTuning == null || data.solverSafetyTuning == null || data.solverLookaheadTuning == null)
            {
                return;
            }

            for (int solverIndex = 0; solverIndex < StackMergeSolverCatalog.Definitions.Length; solverIndex++)
            {
                int offset = solverIndex * SolverTuningSettings.MaxSlots;
                bool hasNewValues = false;
                for (int slot = 0; slot < SolverTuningSettings.MaxSlots; slot++)
                {
                    if (data.solverTuningValues[offset + slot] != 0)
                    {
                        hasNewValues = true;
                        break;
                    }
                }

                if (hasNewValues)
                {
                    continue;
                }

                SolverId solverId = (SolverId)solverIndex;
                ApplyLegacyTuning(solverId, SolverTuneParameterId.MergeReward, data.solverMergeTuning[solverIndex]);
                ApplyLegacyTuning(solverId, SolverTuneParameterId.FreeSpace, data.solverSafetyTuning[solverIndex]);
                ApplyLegacyTuning(solverId, SolverTuneParameterId.SafetyCushion, data.solverSafetyTuning[solverIndex]);
                ApplyLegacyTuning(solverId, SolverTuneParameterId.QueueFit, data.solverLookaheadTuning[solverIndex]);
                ApplyLegacyTuning(solverId, SolverTuneParameterId.FollowUpWeight, data.solverLookaheadTuning[solverIndex]);
            }

            Array.Clear(data.solverMergeTuning, 0, data.solverMergeTuning.Length);
            Array.Clear(data.solverSafetyTuning, 0, data.solverSafetyTuning.Length);
            Array.Clear(data.solverLookaheadTuning, 0, data.solverLookaheadTuning.Length);
        }

        private void ApplyLegacyTuning(SolverId solverId, SolverTuneParameterId parameterId, int value)
        {
            int slotIndex = StackMergeSolverCatalog.GetTuningParameterIndex(solverId, parameterId);
            if (slotIndex < 0)
            {
                return;
            }

            int solverIndex = ClampSolverIndex(solverId);
            data.solverTuningValues[solverIndex * SolverTuningSettings.MaxSlots + slotIndex] = SolverTuningSettings.ClampValue((SolverId)solverIndex, slotIndex, value);
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
}
