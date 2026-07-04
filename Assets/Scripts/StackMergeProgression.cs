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
        public int[] solverLifetimeRuns;
        public bool solverDeselected;
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
        public int scalingFrequencyLevel;
        public int profitableEndingLevel;
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
        public long lifetimeChipsEarned;
        public long lifetimeChipsSpent;
        public int lifetimeRunsCompleted;
        public int lifetimeManualRunsCompleted;
        public int lifetimeMoves;
        public int lifetimeMerges;
        public int lifetimeHighestBlockEver;
        public bool lifetimeAgentsUnlocked;
        public bool lifetimeModifiersUnlocked;
        public int[] agentLifetimeRuns;
        public int lifetimeUnstableSaves;
        public int lifetimeJokerMerges;
        public long bestRunScore;
        public int mergeTokenProgress;
        public bool machineLearningTrainingMode;
        public float machineLearningExperience;
        public int machineLearningRuns;
        public long machineLearningBestScore;
        public int machineLearningBestHigh;
        public StackMergePpoTrainingData machineLearningPolicy;
        public int machineLearningNormalRuns;
        public long machineLearningNormalBestScore;
        public int machineLearningNormalBestHigh;
        public long machineLearningNormalFrames;
        public StackMergePpoTrainingData machineLearningPrestigeMemoryPolicy;
        public int machineLearningPrestigeMemoryRuns;
        public long researchInsight;
        public long lifetimeResearchInsight;
        public int prestigeCount;
        public long lastPrestigeInsight;
        public long bestPrestigeInsight;
        public int[] researchLevels;
        public double passiveResearchProgress;
        public double researchInsightEarnedThisPrestige;
        public long lastSaveUnixSeconds;
        public long lastOfflineChips;
        public long lastOfflineInsight;
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
        LifetimeChipsEarned,
        LifetimeChipsSpent,
        LifetimeManualRunsCompleted,
        MaxSolverLifetimeRuns,
        LifetimeMoves,
        LifetimeMerges,
        LifetimeHighestBlockEver,
        AgentsUnlockedEver,
        ModifiersUnlockedEver,
        SolversUsed,
        AgentsUsed,
        LifetimeUnstableSaves,
        LifetimeJokerMerges,
        PrestigeCount,
        MaxedResearchCount
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

    public enum ResearchId
    {
        InsightAmplifier = 0,
        SeedCapital = 1,
        AutomationMemory = 2,
        AlgorithmArchive = 3,
        YieldTheory = 4,
        PpoBootcamp = 5,
        PpoMemory = 6,
        PpoHighFocus = 7,
        PpoStability = 8,
        InsightExtractor = 9,
        PassiveInsight = 10,
        OfflineEfficiency = 11,
        OfflineTime = 12
    }

    public readonly struct ResearchDefinition
    {
        public ResearchDefinition(ResearchId id, string displayName, string description, int branchColumn, int tier, params int[] costs)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            BranchColumn = Mathf.Clamp(branchColumn, 0, 2);
            Tier = Math.Max(0, tier);
            Costs = costs ?? Array.Empty<int>();
        }

        public ResearchId Id { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public int BranchColumn { get; }

        public int Tier { get; }

        public int[] Costs { get; }

        public int MaxLevel => Costs.Length;
    }

    public sealed class StackMergeProgression
    {
        private const string PlayerPrefsKey = "StackMerge.Progression.v2";
        private const int BaseAgentSlots = 2;
        // Global income calibration knob. Lower = chips accrue slower. Tuned against the progression
        // simulator to hit the intended run-count curve (manual runs ~150 chips each, etc.).
        private const double IncomeScale = 0.25;

        private const int AutoSolveUnlockCost = 1200;
        private const int AutoRestartUnlockCost = 8000;
        private const int SolverTuningUnlockCost = 30000;
        private const int TokenPackCost = 300;
        private const int TokenPackSize = 50;
        private const int ExtraAgentSlotUpgradeCost = 1500000;
        private const int ModifiersMenuUnlockCost = 3000000;
        // The Modifier gate must be reachable WITHOUT modifiers (they come after it). Reaching 2048 /
        // a 40k run needs the longer runs that modifiers enable, so those are impossible pre-gate.
        // 1024 / ~6k are the realistic ceiling of a strong solver at max capacity without modifiers;
        // the real "late game" pacing comes from the chip cost of the upgrades + Modifiers menu (3M).
        private const int ModifierGateRuns = 20;
        private const int ModifierGateSolvers = 7;
        private const int ModifierGateBestScore = 6000;
        private const int ModifierGateMerges = 3000;
        private const int ModifierGateHighestBlock = 1024;
        private const int TokenProspectorMergeTarget = 8;

        private static readonly int[] SpeedUpgradeCosts = { 6000, 30000, 150000, 800000, 4000000 };
        private static readonly float[] MoveIntervals = { 0.18f, 0.12f, 0.08f, 0.055f, 0.035f, 0.022f };
        private static readonly int[] StackCapacityCosts = { 12000, 70000, 400000, 2200000, 11000000 };
        private static readonly int[] QueuePreviewUpgradeCosts = { 40000, 400000 };
        private static readonly int[] IncomeUpgradeCosts = { 4000, 40000, 350000, 2500000, 15000000 };
        private static readonly int[] DifficultyUpgradeCosts = { 60000, 180000, 600000, 1800000, 6000000 };
        private static readonly int[] ScalingFrequencyUpgradeCosts = { 90000, 300000, 900000, 2700000, 8000000 };
        private static readonly int[] ProfitableEndingUpgradeCosts = { 80000, 240000, 750000, 2200000, 7000000 };
        private const double ScalingFrequencyPressurePerLevel = 0.09;
        private const double ProfitableEndingBonusPerLevel = 0.08;
        private const int AgentsMenuUnlockCost = 120000;
        private const int MaxHistoryEntries = 250;

        public static readonly AgentDefinition[] Agents =
        {
            new(AgentId.MergeBroker, "Merge Broker", 60000, "Boosts merge income.", "+75% <sprite name=\"chips\"> from merge rewards."),
            new(AgentId.HighwaterAnalyst, "Highwater Analyst", 120000, "Rewards new highs.", "+140% <sprite name=\"chips\"> from new highest-block rewards."),
            new(AgentId.ScoreAuditor, "Score Auditor", 220000, "Turns score into <sprite name=\"chips\">.", "+60% <sprite name=\"chips\"> from end-of-run score bonus."),
            new(AgentId.Overclocker, "Overclocker", 400000, "Runs the solver faster.", "Solver move interval is 25% shorter."),
            new(AgentId.Quartermaster, "Quartermaster", 650000, "Improves baseline income.", "+4 <sprite name=\"chips\"> on every successful placement."),
            new(AgentId.RestartSponsor, "Restart Sponsor", 1000000, "Keeps restarts funded.", "Auto Restart consumes no tokens while this agent is active."),
            new(AgentId.TokenProspector, "Token Prospector", 1500000, "Turns merge volume into restart fuel.", $"+1 token for every {TokenProspectorMergeTarget} merges while active."),
            new(AgentId.MoveDividend, "Move Dividend", 2200000, "Rewards long, stable runs.", "End-of-run <sprite name=\"chips\"> gain a bonus from total moves completed."),
            new(AgentId.VelocityTrader, "Velocity Trader", 3000000, "Rewards fast solvers.", "End-of-run <sprite name=\"chips\"> gain a throughput bonus from moves per second.")
        };

        public static readonly ModifierDefinition[] Modifiers =
        {
            new(ModifierId.UnstableStack, "Unstable Stack", "Deletes bottom blocks when a full stack would fail.", "Each level gives one rescue per run. If a full stack receives a non-merge block, its bottom block is removed without reducing score.", 800000, 1600000, 3200000, 6400000, 12800000),
            new(ModifierId.CatalystStack, "Catalyst Stack", "Converts merges into more <sprite name=\"chips\">.", "Merge rewards are permanently doubled on every run after purchase.", 3000000),
            new(ModifierId.MirrorStack, "Mirror Stack", "Lets stack ends interact.", "Unlocks a special merge. If the top and bottom block of a stack match, they merge through the stack.", 3000000),
            new(ModifierId.Joker, "Joker", "Adds wild blocks to the queue.", "Unlocks occasional Joker blocks. A Joker placed onto any block merges with it.", 4500000),
            new(ModifierId.MinersPickaxe, "Miner's Pickaxe", "Lets solvers remove blocks from the board.", "Each level gives one pickaxe use per run. The solver may delete any block in any stack.", 2000000, 4000000, 8000000, 16000000, 32000000),
            new(ModifierId.QueueScrubber, "Queue Scrubber", "Lets solvers delete bad upcoming blocks.", "Each level gives one queue skip per run. The current next block is removed and the following block moves forward.", 1800000, 3600000, 7200000, 14400000, 28800000),
            new(ModifierId.NeuralAccelerator, "Neural Accelerator", "Speeds up expensive solvers.", "MOCA, MOCA+, and MCTS run permanently about twice as fast. Negative speed tuning on those solvers is also twice as effective.", 6000000)
        };

        public static readonly AchievementDefinition[] Achievements =
        {
            new(0, "Chip Bank", "Earn 10000 <sprite name=\"chips\"> in total", AchievementMetric.LifetimeChipsEarned, 10_000),
            new(1, "Chip Million", "Earn 1 M <sprite name=\"chips\"> in total", AchievementMetric.LifetimeChipsEarned, 1_000_000),
            new(2, "Chip Billion", "Earn 1 B <sprite name=\"chips\"> in total", AchievementMetric.LifetimeChipsEarned, 1_000_000_000),
            new(3, "First Budget", "Spend 10000 <sprite name=\"chips\"> in total", AchievementMetric.LifetimeChipsSpent, 10_000),
            new(4, "Serious Budget", "Spend 100 K <sprite name=\"chips\"> in total", AchievementMetric.LifetimeChipsSpent, 100_000),
            new(5, "Mega Budget", "Spend 100 M <sprite name=\"chips\"> in total", AchievementMetric.LifetimeChipsSpent, 100_000_000),
            new(6, "Manual Finish", "Complete 10 runs while Auto Solver is turned off", AchievementMetric.LifetimeManualRunsCompleted, 10),
            new(7, "Solver Loyalty", "Complete 1000 runs with a solver", AchievementMetric.MaxSolverLifetimeRuns, 1000),
            new(8, "Move Habit", "Move a total of 10000 times", AchievementMetric.LifetimeMoves, 10_000),
            new(9, "Move Engine", "Move a total of 100 K times", AchievementMetric.LifetimeMoves, 100_000),
            new(10, "Move Singularity", "Move a total of 1 M times", AchievementMetric.LifetimeMoves, 1_000_000),
            new(11, "Merge Habit", "Merge a total of 10000 times", AchievementMetric.LifetimeMerges, 10_000),
            new(12, "Merge Engine", "Merge a total of 100 K times", AchievementMetric.LifetimeMerges, 100_000),
            new(13, "Merge Singularity", "Merge a total of 1 M times", AchievementMetric.LifetimeMerges, 1_000_000),
            new(14, "High 1024", "Reach high 1024", AchievementMetric.LifetimeHighestBlockEver, 1024),
            new(15, "High 8192", "Reach high 8192", AchievementMetric.LifetimeHighestBlockEver, 8192),
            new(16, "High 32768", "Reach high 32768", AchievementMetric.LifetimeHighestBlockEver, 32768),
            new(17, "Agents Online", "Unlock Agents", AchievementMetric.AgentsUnlockedEver, 1),
            new(18, "Modifiers Online", "Unlock Modifiers", AchievementMetric.ModifiersUnlockedEver, 1),
            new(19, "Solver Tour", "Use all solvers at least once", AchievementMetric.SolversUsed, StackMergeSolverCatalog.Definitions.Length),
            new(20, "Agent Tour", "Use all Agents at least once", AchievementMetric.AgentsUsed, Agents.Length),
            new(21, "Unstable Lifeline", "Let Unstable Stack save your run a total of 100 times", AchievementMetric.LifetimeUnstableSaves, 100),
            new(22, "Joker Merges", "Merge with a Joker for a total of 100 times", AchievementMetric.LifetimeJokerMerges, 100),
            new(23, "First Prestige", "Prestige reset for the first time", AchievementMetric.PrestigeCount, 1),
            new(24, "Prestige Loop", "Prestige reset for a total of 5 times", AchievementMetric.PrestigeCount, 5),
            new(25, "Research Complete", "Buy all the researches", AchievementMetric.MaxedResearchCount, 13)
        };

        public static readonly ResearchDefinition[] Research =
        {
            new(ResearchId.InsightAmplifier, "Insight Amplifier", "+35% Insight from every future prestige. This is the root research: every branch starts here.", 1, 0, 50, 130, 300, 700, 1600),
            new(ResearchId.SeedCapital, "Seed Capital", "Start each prestige with chips already banked. It shortens the first slow minutes after a reset without skipping entire stages by itself.", 0, 1, 200, 500, 1200, 2800, 6500),
            new(ResearchId.AutomationMemory, "Automation Memory", "Permanently remembers automation milestones after prestige: Auto Solve, Auto Restart tokens, then Solver Tuning.", 0, 2, 1000, 2500, 6000),
            new(ResearchId.AlgorithmArchive, "Algorithm Archive", "Start future prestiges with early algorithms already known: RAND, MERG, BAL, then HEUR.", 0, 3, 6000, 14000, 32000, 75000),
            new(ResearchId.YieldTheory, "Yield Theory", "+18% chips from every chip reward per level. It stacks with Chip Yield and stage multipliers.", 0, 4, 30000, 70000, 160000, 380000, 850000),
            new(ResearchId.PpoBootcamp, "PPO Bootcamp", "PPO still resets every prestige, but each level lowers the trained-frame requirement for Normal mode by 8%.", 1, 1, 200, 500, 1200, 2800, 6500),
            new(ResearchId.PpoMemory, "PPO Memory", "Prestige keeps a pre-trained PPO snapshot. L1 remembers roughly the first 500 PPO runs; higher levels retain deeper warm starts.", 1, 2, 1000, 2500, 6000, 14000, 32000),
            new(ResearchId.PpoHighFocus, "High Focus", "Raises PPO's reward signal for creating new highest blocks. This pushes the learner toward bigger tiles instead of only safer runs.", 1, 3, 6000, 14000, 32000, 75000, 170000),
            new(ResearchId.PpoStability, "Stability Model", "Improves PPO's survival shaping and danger penalties, making high-focus policies less likely to crash early.", 1, 4, 30000, 70000, 160000, 380000, 850000),
            new(ResearchId.InsightExtractor, "Insight Extractor", "+20% prestige Insight from PPO Normal-mode performance per level. This is the late neural payoff node.", 1, 5, 150000, 350000, 800000, 1800000, 4000000),
            new(ResearchId.PassiveInsight, "Passive Insight", "Boosts Insight earned directly from PPO Normal-mode runs. Training mode never feeds this, and long cycles softcap so prestige stays valuable.", 2, 1, 200, 500, 1200, 2800, 6500),
            new(ResearchId.OfflineEfficiency, "Offline Engine", "While the game is closed, chips and Passive Insight continue at a reduced rate based on your current prestige strength.", 2, 2, 1000, 2500, 6000, 14000, 32000),
            new(ResearchId.OfflineTime, "Offline Buffer", "Extends how many closed-game hours can be converted into offline chips and Insight.", 2, 3, 6000, 14000, 32000, 75000, 170000)
        };

        private readonly StackMergeProgressionData data;
        private readonly StackMergePpoAgent machineLearningAgent;

        public StackMergeProgression(StackMergeProgressionData data)
        {
            this.data = data ?? new StackMergeProgressionData();
            Normalize();
            machineLearningAgent = new StackMergePpoAgent(this.data.machineLearningPolicy);
            ApplyMachineLearningResearchBonuses();
            ApplyOfflineProgress();
        }

        public long Chips => data.chips;

        public long Tokens => data.tokens;

        public SolverId SelectedSolver => (SolverId)data.selectedSolver;

        public bool SolverDeselected => data.solverDeselected;

        public int SpeedLevel => data.speedLevel;

        public bool HasPurchasedSolver => data.solverUnlocked != null && data.solverUnlocked.Any(unlocked => unlocked);

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

        public int ScalingFrequencyLevel => data.scalingFrequencyLevel;

        public int MaxScalingFrequencyLevel => ScalingFrequencyUpgradeCosts.Length;

        public int ProfitableEndingLevel => data.profitableEndingLevel;

        public int MaxProfitableEndingLevel => ProfitableEndingUpgradeCosts.Length;

        public int RunsCompleted => data.runsCompleted;

        public bool AgentsMenuUnlocked => data.agentsMenuUnlocked;

        public bool ExtraAgentSlotUnlocked => data.extraAgentSlotUnlocked;

        public bool ModifiersMenuUnlocked => data.modifiersMenuUnlocked;

        public bool AllModifiersMaxed => data.modifiersMenuUnlocked && Modifiers.All(modifier => IsModifierMaxed(modifier.Id));

        public bool NeuralAcceleratorActive => data.modifiersMenuUnlocked && GetModifierLevel(ModifierId.NeuralAccelerator) > 0;

        public bool CanUnlockMachineLearning => AllModifiersMaxed;

        public long ResearchInsight => data.researchInsight;

        public long LifetimeResearchInsight => data.lifetimeResearchInsight;

        public int PrestigeCount => data.prestigeCount;

        public long LastPrestigeInsight => data.lastPrestigeInsight;

        public long BestPrestigeInsight => data.bestPrestigeInsight;

        public long ResearchInsightEarnedThisPrestige => Math.Max(0, (long)Math.Floor(data.researchInsightEarnedThisPrestige));

        public bool PrestigeAvailable => IsSolverUnlocked(SolverId.MachineLearning) && MachineLearningPlayingModeUnlocked;

        public bool CanPrestige => PreviewPrestigeInsightGain() > 0;

        public int MachineLearningNormalRuns => Math.Max(0, data.machineLearningNormalRuns);

        public long MachineLearningNormalBestScore => Math.Max(0, data.machineLearningNormalBestScore);

        public int MachineLearningNormalBestHigh => Math.Max(0, data.machineLearningNormalBestHigh);

        public long MachineLearningNormalFrames => Math.Max(0, data.machineLearningNormalFrames);

        public int MachineLearningMemoryRuns => Math.Max(0, data.machineLearningPrestigeMemoryRuns);

        public long LastOfflineChips => Math.Max(0, data.lastOfflineChips);

        public long LastOfflineInsight => Math.Max(0, data.lastOfflineInsight);

        public bool MachineLearningTrainingMode
        {
            get => data.machineLearningTrainingMode;
            set => data.machineLearningTrainingMode = value;
        }

        // Normal ("Playing") mode for PPO unlocks only after this many trained frames; until then the
        // agent can only be run in Training mode.
        public const long PlayingModeFrameRequirement = 500000;

        public long MachineLearningFrames => machineLearningAgent?.Metrics.Steps ?? Math.Max(0, (long)data.machineLearningExperience);

        public long MachineLearningPlayingModeFrameRequirement
        {
            get
            {
                double multiplier = 1.0 - GetResearchLevel(ResearchId.PpoBootcamp) * 0.08;
                return Math.Max(250000, (long)Math.Round(PlayingModeFrameRequirement * Math.Max(0.5, multiplier)));
            }
        }

        public bool MachineLearningPlayingModeUnlocked => MachineLearningFrames >= MachineLearningPlayingModeFrameRequirement;

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

        public long LifetimeChipsEarned => data.lifetimeChipsEarned;

        public long LifetimeChipsSpent => data.lifetimeChipsSpent;

        public int LifetimeRunsCompleted => data.lifetimeRunsCompleted;

        public int LifetimeManualRunsCompleted => data.lifetimeManualRunsCompleted;

        public int LifetimeMoves => data.lifetimeMoves;

        public int LifetimeMerges => data.lifetimeMerges;

        public int LifetimeHighestBlockEver => data.lifetimeHighestBlockEver;

        public int LifetimeUnstableSaves => data.lifetimeUnstableSaves;

        public int LifetimeJokerMerges => data.lifetimeJokerMerges;

        public long BestRunScore => data.bestRunScore;

        public int MonteCarloSimulationCount => 3 + data.speedLevel * 2;

        public int MonteCarloRolloutDepth => 3 + data.speedLevel;

        public float MoveInterval => GetMoveInterval(SelectedSolver);

        /// <summary>
        /// Raw speed-upgrade effect at a given level, expressed as "% faster than base". Independent
        /// of the active solver's own pacing multiplier and agent bonuses, so it's a stable number
        /// for UI to display (unlike <see cref="MoveInterval"/>, which varies per solver).
        /// </summary>
        public static float GetSpeedUpgradeEffectPercent(int level)
        {
            int clamped = Mathf.Clamp(level, 0, MoveIntervals.Length - 1);
            return (1f - MoveIntervals[clamped] / MoveIntervals[0]) * 100f;
        }

        public static float GetDifficultyMaxTierBonus(int level)
        {
            int clamped = Mathf.Clamp(level, 0, DifficultyUpgradeCosts.Length);
            return clamped * 3f / DifficultyUpgradeCosts.Length;
        }

        public static float GetScalingFrequencyEffectPercent(int level)
        {
            int clamped = Mathf.Clamp(level, 0, ScalingFrequencyUpgradeCosts.Length);
            return (float)(clamped * ScalingFrequencyPressurePerLevel * 100.0);
        }

        public static float GetProfitableEndingEffectPercent(int level)
        {
            int clamped = Mathf.Clamp(level, 0, ProfitableEndingUpgradeCosts.Length);
            return (float)(clamped * ProfitableEndingBonusPerLevel * 100.0);
        }

        public bool IsMaxSpeed => data.speedLevel >= MoveIntervals.Length - 1;

        public int MaxSpeedLevel => SpeedUpgradeCosts.Length;

        public bool IsMaxStackCapacity => StackCapacity >= StackMergeGameState.MaxStackCapacity;

        public bool IsMaxQueuePreview => data.queuePreviewLevel >= QueuePreviewUpgradeCosts.Length;

        public bool IsMaxIncome => data.incomeLevel >= IncomeUpgradeCosts.Length;

        public bool IsMaxDifficulty => data.difficultyLevel >= DifficultyUpgradeCosts.Length;

        public bool IsMaxScalingFrequency => data.scalingFrequencyLevel >= ScalingFrequencyUpgradeCosts.Length;

        public bool IsMaxProfitableEnding => data.profitableEndingLevel >= ProfitableEndingUpgradeCosts.Length;

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
            // Gentle, BOUNDED growth. The old curve went exponential past 1024 (up to 1e9x), which —
            // multiplied by the raw tile value in the income formula — made chips explode into the
            // quadrillions once modifiers let runs reach huge tiles. A capped curve keeps the high
            // blocks worthwhile without breaking the economy.
            int log = FloorLog2(Math.Max(1, highestBlock));
            return log switch
            {
                >= 14 => 6.0,
                >= 13 => 5.5,
                >= 12 => 5.0,
                >= 11 => 4.4,
                >= 10 => 3.8,
                >= 9 => 3.0,
                >= 8 => 2.4,
                >= 7 => 1.9,
                >= 6 => 1.5,
                >= 5 => 1.2,
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
            data.lastSaveUnixSeconds = GetUnixNow();
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

        public ResearchDefinition GetResearchDefinition(ResearchId researchId)
        {
            int index = (int)researchId;
            return index >= 0 && index < Research.Length ? Research[index] : Research[0];
        }

        public int GetResearchLevel(ResearchId researchId)
        {
            int index = (int)researchId;
            return index >= 0 && index < data.researchLevels.Length ? data.researchLevels[index] : 0;
        }

        public bool IsResearchMaxed(ResearchId researchId)
        {
            ResearchDefinition definition = GetResearchDefinition(researchId);
            return GetResearchLevel(researchId) >= definition.MaxLevel;
        }

        public long GetResearchCost(ResearchId researchId)
        {
            ResearchDefinition definition = GetResearchDefinition(researchId);
            int level = GetResearchLevel(researchId);
            return level >= 0 && level < definition.Costs.Length ? definition.Costs[level] : 0;
        }

        public bool CanBuyResearch(ResearchId researchId)
        {
            return PrestigeCount > 0
                && !IsResearchMaxed(researchId)
                && IsResearchPrerequisiteMet(researchId, out _)
                && ResearchInsight >= GetResearchCost(researchId);
        }

        public string GetResearchUnavailableReason(ResearchId researchId)
        {
            if (PrestigeCount <= 0)
            {
                return "Prestige once to open Research.";
            }

            if (IsResearchMaxed(researchId))
            {
                return "Research maxed.";
            }

            if (!IsResearchPrerequisiteMet(researchId, out string reason))
            {
                return reason;
            }

            return ResearchInsight >= GetResearchCost(researchId) ? string.Empty : "Not enough Insight.";
        }

        public string GetResearchEffectSummary(ResearchId researchId)
        {
            int level = GetResearchLevel(researchId);
            return researchId switch
            {
                ResearchId.InsightAmplifier => $"Prestige Insight x{GetPrestigeInsightMultiplier():0.00}",
                ResearchId.SeedCapital => $"Start chips: {GetPrestigeStartChips(level)}",
                ResearchId.AutomationMemory => level switch
                {
                    0 => "No automation is remembered yet.",
                    1 => "Auto Solve unlock remembered.",
                    2 => "Auto Restart unlock and 50 tokens remembered.",
                    _ => "Auto Solve, Auto Restart, tokens, and Solver Tuning remembered."
                },
                ResearchId.AlgorithmArchive => level switch
                {
                    0 => "No algorithms remembered yet.",
                    1 => "Start with RAND.",
                    2 => "Start with RAND and MERG.",
                    3 => "Start with RAND, MERG, and BAL.",
                    _ => "Start with RAND, MERG, BAL, and HEUR."
                },
                ResearchId.YieldTheory => $"Chip rewards x{GetResearchIncomeMultiplier():0.00}",
                ResearchId.PpoBootcamp => $"PPO Normal mode at {MachineLearningPlayingModeFrameRequirement} frames",
                ResearchId.PpoMemory => $"Warm start: {GetPpoMemoryRunLimit(level)} PPO runs retained",
                ResearchId.PpoHighFocus => $"New-high learning x{GetPpoHighFocusMultiplier():0.00}",
                ResearchId.PpoStability => $"Survival shaping x{GetPpoStabilityMultiplier():0.00}",
                ResearchId.InsightExtractor => $"Normal-mode prestige x{GetInsightExtractorMultiplier():0.00}",
                ResearchId.PassiveInsight => $"Normal Insight x{GetNormalModeInsightMultiplier():0.00}",
                ResearchId.OfflineEfficiency => $"Offline efficiency {GetOfflineEfficiency() * 100:0}%",
                ResearchId.OfflineTime => $"Offline cap {GetOfflineHourCap():0.#}h",
                _ => string.Empty
            };
        }

        public bool BuyResearch(ResearchId researchId)
        {
            if (!CanBuyResearch(researchId))
            {
                return false;
            }

            int index = (int)researchId;
            data.researchInsight -= GetResearchCost(researchId);
            data.researchLevels[index]++;
            ApplyImmediateResearchEffects(researchId);
            return true;
        }

        public long PreviewPrestigeInsightGain()
        {
            if (!PrestigeAvailable || data.machineLearningNormalRuns <= 0)
            {
                return 0;
            }

            long bestScore = Math.Max(0, data.machineLearningNormalBestScore);
            int bestHigh = Math.Max(1, data.machineLearningNormalBestHigh);
            long frames = Math.Max(0, data.machineLearningNormalFrames);
            int normalRuns = Math.Max(0, data.machineLearningNormalRuns);

            if (data.prestigeCount <= 0)
            {
                return 1;
            }

            double performance = ComputeInsightValue(bestScore, bestHigh, normalRuns, bestScore, bestHigh);
            double usage = 1.0
                + 1.55 * (1.0 - Math.Exp(-normalRuns / 160.0))
                + 1.10 * (1.0 - Math.Exp(-normalRuns / 1100.0))
                + Math.Log10(1.0 + frames / 120000.0) * 0.35;
            double cycleCarry = Math.Log(1.0 + Math.Max(0.0, data.researchInsightEarnedThisPrestige)) * 0.42;
            double raw = (1.0 + performance * 1.25 + cycleCarry) * usage;
            double multiplier = GetNormalModeInsightMultiplier();
            return Math.Max(1, (long)Math.Round(raw * multiplier, MidpointRounding.AwayFromZero));
        }

        public string GetPrestigeSummary()
        {
            // The Research tab itself only requires PPO to be bought (see Bootstrap's
            // IsResearchMenuUnlocked), so this text is normally only read after that's already
            // true. Kept as a defensive fallback in case it's ever surfaced earlier.
            if (!IsSolverUnlocked(SolverId.MachineLearning))
            {
                return "Unlock PPO to begin the prestige layer.";
            }

            if (!MachineLearningPlayingModeUnlocked)
            {
                return $"Finish PPO Training first. {MachineLearningFrames}/{MachineLearningPlayingModeFrameRequirement} frames.";
            }

            long gain = PreviewPrestigeInsightGain();
            return $"Prestige for {gain} insights. You can keep playing PPO in Playing Mode to increase insight.";
        }

        public long ExecutePrestige()
        {
            CaptureMachineLearningMemoryIfEligible();
            long gained = PreviewPrestigeInsightGain();
            if (gained <= 0)
            {
                return 0;
            }

            int[] preservedResearch = NormalizeResearchLevels(data.researchLevels);
            long preservedInsight = Math.Max(0, data.researchInsight) + gained;
            long preservedLifetimeInsight = Math.Max(0, data.lifetimeResearchInsight) + gained;
            int nextPrestigeCount = Math.Max(0, data.prestigeCount) + 1;
            long bestPrestige = Math.Max(data.bestPrestigeInsight, gained);

            ResetPrestigeProgress();

            data.researchLevels = preservedResearch;
            data.researchInsight = preservedInsight;
            data.lifetimeResearchInsight = preservedLifetimeInsight;
            data.prestigeCount = nextPrestigeCount;
            data.lastPrestigeInsight = gained;
            data.bestPrestigeInsight = bestPrestige;

            machineLearningAgent?.ResetForPrestige(24681357 + nextPrestigeCount * 9973);
            ApplyMachineLearningMemoryAfterReset();
            data.machineLearningPolicy = machineLearningAgent?.Data ?? new StackMergePpoTrainingData();
            ApplyMachineLearningResearchBonuses();
            ApplyPrestigeStartResearchBonuses();
            Normalize();
            Save();
            return gained;
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
                data.solverDeselected = false;
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
            data.solverDeselected = false;
            return true;
        }

        public void SetSolverDeselected(bool deselected)
        {
            data.solverDeselected = deselected && IsSolverUnlocked(SelectedSolver);
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

        public long GetScalingFrequencyUpgradeCost()
        {
            return IsMaxScalingFrequency ? 0 : ScalingFrequencyUpgradeCosts[data.scalingFrequencyLevel];
        }

        public long GetScalingFrequencyUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < ScalingFrequencyUpgradeCosts.Length ? ScalingFrequencyUpgradeCosts[upgradeIndex] : 0;
        }

        public bool BuyScalingFrequencyUpgrade()
        {
            if (IsMaxScalingFrequency || !Spend(GetScalingFrequencyUpgradeCost()))
            {
                return false;
            }

            data.scalingFrequencyLevel++;
            return true;
        }

        public bool BuyScalingFrequencyUpgrade(int upgradeIndex)
        {
            return upgradeIndex == data.scalingFrequencyLevel && BuyScalingFrequencyUpgrade();
        }

        public long GetProfitableEndingUpgradeCost()
        {
            return IsMaxProfitableEnding ? 0 : ProfitableEndingUpgradeCosts[data.profitableEndingLevel];
        }

        public long GetProfitableEndingUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < ProfitableEndingUpgradeCosts.Length ? ProfitableEndingUpgradeCosts[upgradeIndex] : 0;
        }

        public bool BuyProfitableEndingUpgrade()
        {
            if (IsMaxProfitableEnding || !Spend(GetProfitableEndingUpgradeCost()))
            {
                return false;
            }

            data.profitableEndingLevel++;
            return true;
        }

        public bool BuyProfitableEndingUpgrade(int upgradeIndex)
        {
            return upgradeIndex == data.profitableEndingLevel && BuyProfitableEndingUpgrade();
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
            data.lifetimeAgentsUnlocked = true;
            return true;
        }

        public long GetExtraAgentSlotUpgradeCost()
        {
            return data.extraAgentSlotUnlocked ? 0 : ExtraAgentSlotUpgradeCost;
        }

        public bool BuyExtraAgentSlotUpgrade()
        {
            if (!data.agentsMenuUnlocked)
            {
                return false;
            }

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
            data.lifetimeModifiersUnlocked = true;
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
            gained = (long)Math.Ceiling(gained * IncomeScale);
            if (gained > 0 && !suppressChips)
            {
                gained = ApplyStageMultiplier(gained);
                gained = ApplyIncomeMultiplier(gained);
                data.chips += gained;
                data.totalChipsEarned += gained;
                data.lifetimeChipsEarned += gained;
            }
            else if (suppressChips)
            {
                gained = 0;
            }

            if (result.ActionKind == SolverActionKind.Place)
            {
                data.totalBlocksDropped++;
                data.lifetimeMoves++;
            }

            data.totalMerges += Math.Max(0, result.MergeCount);
            data.lifetimeMerges += Math.Max(0, result.MergeCount);
            data.highestBlockEver = Math.Max(data.highestBlockEver, result.HighestBlock);
            data.lifetimeHighestBlockEver = Math.Max(data.lifetimeHighestBlockEver, result.HighestBlock);
            if (result.UnstableSaveUsed)
            {
                data.lifetimeUnstableSaves++;
            }

            data.lifetimeJokerMerges += Math.Max(0, result.JokerMergeCount);
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
            data.lifetimeRunsCompleted++;
            if (manualRun)
            {
                data.manualRunsCompleted++;
                data.lifetimeManualRunsCompleted++;
            }

            data.bestRunScore = Math.Max(data.bestRunScore, Math.Max(0, runScore));
            data.highestBlockEver = Math.Max(data.highestBlockEver, highestMergedBlock);
            data.lifetimeHighestBlockEver = Math.Max(data.lifetimeHighestBlockEver, highestMergedBlock);
            double highestMultiplier = GetHighestBlockRewardMultiplier(highestMergedBlock);
            double scoreBonus = Math.Max(1, runScore) * 0.22 * highestMultiplier * AgentScoreMultiplier;
            double moveBonus = IsAgentActive(AgentId.MoveDividend)
                ? Math.Max(0, moves) * Math.Max(1.0, highestMultiplier * 0.35) * 4.0
                : 0;
            double speedBonus = IsAgentActive(AgentId.VelocityTrader) && elapsedSeconds > 0.01f
                ? scoreBonus * Math.Min(2.5, Math.Max(0.0, (moves / elapsedSeconds) - 1.0) * 0.18)
                : 0;
            double profitableEndingMultiplier = 1.0 + data.profitableEndingLevel * ProfitableEndingBonusPerLevel;
            long bonus = Math.Max(1, (long)Math.Ceiling((scoreBonus + moveBonus + speedBonus) * profitableEndingMultiplier * IncomeScale));
            if (!suppressChips)
            {
                bonus = ApplyStageMultiplier(bonus);
                bonus = ApplyIncomeMultiplier(bonus);
                data.chips += bonus;
                data.totalChipsEarned += bonus;
                data.lifetimeChipsEarned += bonus;
            }
            else
            {
                bonus = 0;
            }

            EnsureSolverLifetimeRuns();
            int solverIndex = (int)solverId;
            if (!manualRun && solverIndex >= 0 && solverIndex < data.solverLifetimeRuns.Length)
            {
                data.solverLifetimeRuns[solverIndex]++;
            }

            EnsureAgentLifetimeRuns();
            foreach (int agentId in data.activeAgentIds.Distinct())
            {
                if (agentId >= 0 && agentId < data.agentLifetimeRuns.Length)
                {
                    data.agentLifetimeRuns[agentId]++;
                }
            }

            RecordRunHistory(runScore, solverId, moves, merges, highestMergedBlock);
            return bonus;
        }

        /// <summary>Lifetime (uncapped) number of completed runs for a solver.</summary>
        public int GetSolverLifetimeRuns(SolverId solverId)
        {
            EnsureSolverLifetimeRuns();
            int index = (int)solverId;
            return index >= 0 && index < data.solverLifetimeRuns.Length ? data.solverLifetimeRuns[index] : 0;
        }

        private void EnsureSolverLifetimeRuns()
        {
            int count = StackMergeSolverCatalog.Definitions.Length;
            if (data.solverLifetimeRuns != null && data.solverLifetimeRuns.Length >= count)
            {
                return;
            }

            int[] resized = new int[count];
            if (data.solverLifetimeRuns != null)
            {
                Array.Copy(data.solverLifetimeRuns, resized, Math.Min(data.solverLifetimeRuns.Length, count));
            }

            data.solverLifetimeRuns = resized;
        }

        private void EnsureAgentLifetimeRuns()
        {
            if (data.agentLifetimeRuns != null && data.agentLifetimeRuns.Length >= Agents.Length)
            {
                return;
            }

            int[] resized = new int[Agents.Length];
            if (data.agentLifetimeRuns != null)
            {
                Array.Copy(data.agentLifetimeRuns, resized, Math.Min(data.agentLifetimeRuns.Length, resized.Length));
            }

            data.agentLifetimeRuns = resized;
        }

        public float AwardMachineLearningRun(long runScore, int moves, int merges, int highestMergedBlock, bool trainingMode)
        {
            data.machineLearningRuns++;
            data.machineLearningBestScore = Math.Max(data.machineLearningBestScore, Math.Max(0, runScore));
            data.machineLearningBestHigh = Math.Max(data.machineLearningBestHigh, Math.Max(0, highestMergedBlock));
            if (!trainingMode)
            {
                data.machineLearningNormalRuns++;
                data.machineLearningNormalBestScore = Math.Max(data.machineLearningNormalBestScore, Math.Max(0, runScore));
                data.machineLearningNormalBestHigh = Math.Max(data.machineLearningNormalBestHigh, Math.Max(0, highestMergedBlock));
                data.machineLearningNormalFrames += Math.Max(0, moves);
                AwardPassiveInsightFromNormalRun(runScore, highestMergedBlock);
            }

            CaptureMachineLearningMemoryIfEligible();
            return machineLearningAgent?.Metrics.LastPolicyLoss ?? 0f;
        }

        public void ObserveMachineLearningMove(MoveResult result, StackMergeGameState stateAfterMove, bool trainingMode)
        {
            ApplyMachineLearningResearchBonuses();
            machineLearningAgent?.Observe(result, stateAfterMove, trainingMode);
        }

        public void FlushMachineLearningTraining(bool trainingMode)
        {
            ApplyMachineLearningResearchBonuses();
            machineLearningAgent?.ForceUpdate(trainingMode);
        }

#if UNITY_EDITOR
        public void AddMachineLearningSimulationProgress(int trainingFrames, int normalRuns, long bestScore, int bestHigh, int movesPerRun, int mergesPerRun)
        {
            trainingFrames = Math.Max(0, trainingFrames);
            normalRuns = Math.Max(0, normalRuns);
            movesPerRun = Math.Max(1, movesPerRun);
            mergesPerRun = Math.Max(0, mergesPerRun);
            bestScore = Math.Max(0, bestScore);
            bestHigh = Math.Max(0, bestHigh);

            StackMergePpoTrainingData policy = machineLearningAgent?.Data ?? data.machineLearningPolicy ?? new StackMergePpoTrainingData();
            int simulatedFrames = trainingFrames + normalRuns * movesPerRun;
            policy.steps = Math.Max(0, policy.steps) + simulatedFrames;
            policy.updates = Math.Max(0, policy.updates) + Math.Max(0, simulatedFrames / 256);
            policy.episodes = Math.Max(0, policy.episodes) + normalRuns;
            policy.bestScore = Math.Max(policy.bestScore, bestScore);
            policy.bestHigh = Math.Max(policy.bestHigh, bestHigh);

            if (normalRuns > 0)
            {
                policy.recentAverageScore = BlendAverage(policy.recentAverageScore, bestScore, 0.28f);
                policy.recentAverageMoves = BlendAverage(policy.recentAverageMoves, movesPerRun, 0.28f);
                policy.recentAverageMerges = BlendAverage(policy.recentAverageMerges, mergesPerRun, 0.28f);
                policy.recentAverageHigh = BlendAverage(policy.recentAverageHigh, bestHigh, 0.28f);
                policy.recentAverageReward = BlendAverage(policy.recentAverageReward, (float)Math.Log10(1.0 + bestScore), 0.28f);
                policy.bestEpisodeReward = Math.Max(policy.bestEpisodeReward, policy.recentAverageReward);
                data.machineLearningRuns += normalRuns;
                data.machineLearningBestScore = Math.Max(data.machineLearningBestScore, bestScore);
                data.machineLearningBestHigh = Math.Max(data.machineLearningBestHigh, bestHigh);
                data.machineLearningNormalRuns += normalRuns;
                data.machineLearningNormalBestScore = Math.Max(data.machineLearningNormalBestScore, bestScore);
                data.machineLearningNormalBestHigh = Math.Max(data.machineLearningNormalBestHigh, bestHigh);
                data.machineLearningNormalFrames += normalRuns * movesPerRun;

                for (int i = 0; i < normalRuns; i++)
                {
                    AwardPassiveInsightFromNormalRun(bestScore, bestHigh);
                }
            }

            data.machineLearningExperience = Math.Max(data.machineLearningExperience, policy.steps);
            data.machineLearningPolicy = policy;
            CaptureMachineLearningMemoryIfEligible();
            Normalize();
        }

        private static float BlendAverage(float current, float next, float weight)
        {
            if (current <= 0f)
            {
                return next;
            }

            return current * (1f - weight) + next * weight;
        }
#endif

        public long GetAchievementProgress(AchievementDefinition achievement)
        {
            return achievement.Metric switch
            {
                AchievementMetric.LifetimeChipsEarned => LifetimeChipsEarned,
                AchievementMetric.LifetimeChipsSpent => LifetimeChipsSpent,
                AchievementMetric.LifetimeManualRunsCompleted => LifetimeManualRunsCompleted,
                AchievementMetric.MaxSolverLifetimeRuns => GetMaxSolverLifetimeRuns(),
                AchievementMetric.LifetimeMoves => LifetimeMoves,
                AchievementMetric.LifetimeMerges => LifetimeMerges,
                AchievementMetric.LifetimeHighestBlockEver => LifetimeHighestBlockEver,
                AchievementMetric.AgentsUnlockedEver => data.lifetimeAgentsUnlocked ? 1 : 0,
                AchievementMetric.ModifiersUnlockedEver => data.lifetimeModifiersUnlocked ? 1 : 0,
                AchievementMetric.SolversUsed => GetUsedSolverCount(),
                AchievementMetric.AgentsUsed => GetUsedAgentCount(),
                AchievementMetric.LifetimeUnstableSaves => LifetimeUnstableSaves,
                AchievementMetric.LifetimeJokerMerges => LifetimeJokerMerges,
                AchievementMetric.PrestigeCount => PrestigeCount,
                AchievementMetric.MaxedResearchCount => GetMaxedResearchCount(),
                _ => 0
            };
        }

        private long GetMaxSolverLifetimeRuns()
        {
            EnsureSolverLifetimeRuns();
            return data.solverLifetimeRuns.Length == 0 ? 0 : data.solverLifetimeRuns.Max();
        }

        private long GetUsedSolverCount()
        {
            EnsureSolverLifetimeRuns();
            return data.solverLifetimeRuns.Take(StackMergeSolverCatalog.Definitions.Length).Count(runs => runs > 0);
        }

        private long GetUsedAgentCount()
        {
            EnsureAgentLifetimeRuns();
            return data.agentLifetimeRuns.Take(Agents.Length).Count(runs => runs > 0);
        }

        private long GetMaxedResearchCount()
        {
            return Research.Count(research => IsResearchMaxed(research.Id));
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

        private double GetPrestigeInsightMultiplier()
        {
            return 1.0 + GetResearchLevel(ResearchId.InsightAmplifier) * 0.35;
        }

        private double GetInsightExtractorMultiplier()
        {
            return 1.0 + GetResearchLevel(ResearchId.InsightExtractor) * 0.20;
        }

        private double GetResearchIncomeMultiplier()
        {
            return 1.0 + GetResearchLevel(ResearchId.YieldTheory) * 0.18;
        }

        private double GetPassiveInsightMultiplier()
        {
            return GetResearchLevel(ResearchId.PassiveInsight) <= 0
                ? 0.0
                : 0.03 + GetResearchLevel(ResearchId.PassiveInsight) * 0.035;
        }

        private double GetNormalModeInsightMultiplier()
        {
            if (data.prestigeCount <= 0)
            {
                return 0.0;
            }

            double prestigeMomentum = 1.0 + Math.Log(1.0 + data.prestigeCount, 2.0) * 0.55;
            double researchMomentum = GetPrestigeInsightMultiplier()
                * GetInsightExtractorMultiplier()
                * (1.0 + GetResearchLevel(ResearchId.PassiveInsight) * 0.45);
            double ppoMomentum = 1.0
                + GetResearchLevel(ResearchId.PpoMemory) * 0.08
                + GetResearchLevel(ResearchId.PpoHighFocus) * 0.10
                + GetResearchLevel(ResearchId.PpoStability) * 0.07;
            return Math.Min(26.0, prestigeMomentum * researchMomentum * ppoMomentum);
        }

        private double GetInsightCycleSoftCap()
        {
            double prestigeTerm = 55.0 + Math.Pow(Math.Max(1, data.prestigeCount), 1.25) * 45.0;
            double bestTerm = Math.Sqrt(Math.Max(0, data.bestPrestigeInsight)) * 18.0;
            double researchTerm = GetResearchLevel(ResearchId.PassiveInsight) * 70.0
                + GetResearchLevel(ResearchId.InsightExtractor) * 120.0;
            return Math.Max(60.0, prestigeTerm + bestTerm + researchTerm);
        }

        private double GetOfflineEfficiency()
        {
            int level = GetResearchLevel(ResearchId.OfflineEfficiency);
            return level <= 0 ? 0.0 : 0.08 + level * 0.05;
        }

        private double GetOfflineHourCap()
        {
            if (GetResearchLevel(ResearchId.OfflineEfficiency) <= 0)
            {
                return 0.0;
            }

            return GetResearchLevel(ResearchId.OfflineTime) switch
            {
                <= 0 => 1.0,
                1 => 3.0,
                2 => 6.0,
                3 => 12.0,
                4 => 18.0,
                _ => 24.0
            };
        }

        private float GetPpoHighFocusMultiplier()
        {
            return 1f + GetResearchLevel(ResearchId.PpoHighFocus) * 0.12f;
        }

        private float GetPpoStabilityMultiplier()
        {
            return 1f + GetResearchLevel(ResearchId.PpoStability) * 0.10f;
        }

        private static int GetPpoMemoryRunLimit(int level)
        {
            return level switch
            {
                <= 0 => 0,
                1 => 500,
                2 => 1000,
                3 => 2000,
                4 => 3500,
                _ => 5000
            };
        }

        private int GetPpoMemoryRunLimit()
        {
            return GetPpoMemoryRunLimit(GetResearchLevel(ResearchId.PpoMemory));
        }

        private static long GetPrestigeStartChips(int seedCapitalLevel)
        {
            return seedCapitalLevel switch
            {
                <= 0 => 0,
                1 => 2500,
                2 => 12000,
                3 => 60000,
                4 => 250000,
                _ => 900000
            };
        }

        private bool IsResearchPrerequisiteMet(ResearchId researchId, out string reason)
        {
            reason = string.Empty;
            switch (researchId)
            {
                case ResearchId.SeedCapital:
                case ResearchId.PpoBootcamp:
                case ResearchId.PassiveInsight:
                    if (GetResearchLevel(ResearchId.InsightAmplifier) < 1)
                    {
                        reason = "Requires Insight Amplifier L1.";
                        return false;
                    }

                    return true;
                case ResearchId.AutomationMemory:
                    if (GetResearchLevel(ResearchId.SeedCapital) < 1)
                    {
                        reason = "Requires Seed Capital L1.";
                        return false;
                    }

                    return true;
                case ResearchId.AlgorithmArchive:
                    if (GetResearchLevel(ResearchId.AutomationMemory) < 1)
                    {
                        reason = "Requires Automation Memory L1.";
                        return false;
                    }

                    return true;
                case ResearchId.YieldTheory:
                    if (GetResearchLevel(ResearchId.AlgorithmArchive) < 1)
                    {
                        reason = "Requires Algorithm Archive L1.";
                        return false;
                    }

                    return true;
                case ResearchId.PpoMemory:
                    if (GetResearchLevel(ResearchId.PpoBootcamp) < 1)
                    {
                        reason = "Requires PPO Bootcamp L1.";
                        return false;
                    }

                    return true;
                case ResearchId.PpoHighFocus:
                    if (GetResearchLevel(ResearchId.PpoMemory) < 1)
                    {
                        reason = "Requires PPO Memory L1.";
                        return false;
                    }

                    return true;
                case ResearchId.PpoStability:
                    if (GetResearchLevel(ResearchId.PpoHighFocus) < 1)
                    {
                        reason = "Requires High Focus L1.";
                        return false;
                    }

                    return true;
                case ResearchId.InsightExtractor:
                    // Same-column only: this sits directly below Stability Model in the middle
                    // column. It must NOT also require Passive Insight (right column) — the tree
                    // only allows moving straight down within a column, never sideways.
                    if (GetResearchLevel(ResearchId.PpoStability) < 1)
                    {
                        reason = "Requires Stability Model L1.";
                        return false;
                    }

                    return true;
                case ResearchId.OfflineEfficiency:
                    if (GetResearchLevel(ResearchId.PassiveInsight) < 1)
                    {
                        reason = "Requires Passive Insight L1.";
                        return false;
                    }

                    return true;
                case ResearchId.OfflineTime:
                    if (GetResearchLevel(ResearchId.OfflineEfficiency) < 1)
                    {
                        reason = "Requires Offline Engine L1.";
                        return false;
                    }

                    return true;
                default:
                    return true;
            }
        }

        private void ApplyImmediateResearchEffects(ResearchId researchId)
        {
            // Yield / prestige multiplier research is read dynamically. Unlock-memory research should
            // also feel good if bought mid-prestige, so it can apply to the current run layer too.
            if (researchId == ResearchId.AutomationMemory || researchId == ResearchId.AlgorithmArchive)
            {
                ApplyPrestigeStartResearchBonuses();
            }

            if (researchId == ResearchId.PpoHighFocus || researchId == ResearchId.PpoStability)
            {
                ApplyMachineLearningResearchBonuses();
            }

            if (researchId == ResearchId.PpoMemory)
            {
                CaptureMachineLearningMemoryIfEligible();
            }
        }

        private void ApplyPrestigeStartResearchBonuses()
        {
            data.chips = Math.Max(data.chips, GetPrestigeStartChips(GetResearchLevel(ResearchId.SeedCapital)));

            int algorithmArchiveLevel = GetResearchLevel(ResearchId.AlgorithmArchive);
            UnlockStarterSolver(SolverId.Rand, algorithmArchiveLevel >= 1);
            UnlockStarterSolver(SolverId.Merge, algorithmArchiveLevel >= 2);
            UnlockStarterSolver(SolverId.Balance, algorithmArchiveLevel >= 3);
            UnlockStarterSolver(SolverId.Heur, algorithmArchiveLevel >= 4);

            if (!data.solverUnlocked[data.selectedSolver] && HasPurchasedSolver)
            {
                data.selectedSolver = Array.FindIndex(data.solverUnlocked, unlocked => unlocked);
            }

            int automationLevel = GetResearchLevel(ResearchId.AutomationMemory);
            if (automationLevel >= 1 && HasPurchasedSolver)
            {
                data.autoSolveUnlocked = true;
                data.autoSolveEnabled = true;
            }

            if (automationLevel >= 2 && HasPurchasedSolver)
            {
                data.autoRestartUnlocked = true;
                data.autoRestartEnabled = true;
                data.tokens = Math.Max(data.tokens, TokenPackSize);
            }

            if (automationLevel >= 3)
            {
                data.solverTuningUnlocked = true;
            }
        }

        private void UnlockStarterSolver(SolverId solverId, bool shouldUnlock)
        {
            if (!shouldUnlock)
            {
                return;
            }

            int index = (int)solverId;
            if (index < 0 || index >= data.solverUnlocked.Length)
            {
                return;
            }

            data.solverUnlocked[index] = true;
            data.highestUnlockedSolver = Math.Max(data.highestUnlockedSolver, index);
            if (!data.solverUnlocked[data.selectedSolver])
            {
                data.selectedSolver = index;
            }
        }

        private void ResetPrestigeProgress()
        {
            int solverCount = StackMergeSolverCatalog.Definitions.Length;
            data.chips = 0;
            data.selectedSolver = 0;
            data.highestUnlockedSolver = 0;
            data.solverUnlocked = new bool[solverCount];
            data.solverMergeTuning = new int[solverCount];
            data.solverSafetyTuning = new int[solverCount];
            data.solverLookaheadTuning = new int[solverCount];
            data.solverTuningValues = new int[solverCount * SolverTuningSettings.MaxSlots];
            data.solverDeselected = false;
            data.solverTuningUnlocked = false;
            data.tokens = 0;
            data.speedLevel = 0;
            data.autoSolveUnlocked = false;
            data.autoSolveEnabled = false;
            data.autoRestartUnlocked = false;
            data.autoRestartEnabled = false;
            data.stackCapacityLevel = 0;
            data.queuePreviewLevel = 0;
            data.incomeLevel = 0;
            data.difficultyLevel = 0;
            data.scalingFrequencyLevel = 0;
            data.profitableEndingLevel = 0;
            data.modifiersMenuUnlocked = false;
            data.modifierLevels = new int[Modifiers.Length];
            data.runsCompleted = 0;
            data.agentsMenuUnlocked = false;
            data.extraAgentSlotUnlocked = false;
            data.agentUnlocked = new bool[Agents.Length];
            data.activeAgentIds = new[] { -1, -1, -1 };
            data.runHistory = Array.Empty<RunHistoryEntry>();
            data.totalChipsEarned = 0;
            data.totalChipsSpent = 0;
            data.manualRunsCompleted = 0;
            data.totalBlocksDropped = 0;
            data.totalMerges = 0;
            data.highestBlockEver = 2;
            data.bestRunScore = 0;
            data.mergeTokenProgress = 0;
            data.machineLearningTrainingMode = false;
            data.machineLearningExperience = 0f;
            data.machineLearningRuns = 0;
            data.machineLearningBestScore = 0;
            data.machineLearningBestHigh = 0;
            data.machineLearningNormalRuns = 0;
            data.machineLearningNormalBestScore = 0;
            data.machineLearningNormalBestHigh = 0;
            data.machineLearningNormalFrames = 0;
            data.researchInsightEarnedThisPrestige = 0.0;
            data.machineLearningPolicy ??= new StackMergePpoTrainingData();
        }

        private void ApplyMachineLearningResearchBonuses()
        {
            machineLearningAgent?.ApplyResearchBonuses(GetPpoHighFocusMultiplier(), GetPpoStabilityMultiplier());
        }

        private void AwardPassiveInsightFromNormalRun(long runScore, int highestMergedBlock)
        {
            double multiplier = GetNormalModeInsightMultiplier();
            if (multiplier <= 0.0)
            {
                return;
            }

            double value = ComputeInsightValue(
                runScore,
                highestMergedBlock,
                Math.Max(1, data.machineLearningNormalRuns),
                data.machineLearningNormalBestScore,
                data.machineLearningNormalBestHigh);
            double fatigue = 1.0 / Math.Sqrt(1.0 + Math.Max(0.0, data.researchInsightEarnedThisPrestige) / GetInsightCycleSoftCap());
            data.passiveResearchProgress += value * 0.18 * multiplier * fatigue;
            FlushPassiveResearchProgress();
        }

        private void FlushPassiveResearchProgress()
        {
            long whole = (long)Math.Floor(Math.Max(0.0, data.passiveResearchProgress));
            if (whole <= 0)
            {
                return;
            }

            data.passiveResearchProgress -= whole;
            AddResearchInsight(whole, true);
        }

        private void AddResearchInsight(long amount, bool countsTowardCurrentPrestige)
        {
            if (amount <= 0)
            {
                return;
            }

            data.researchInsight += amount;
            data.lifetimeResearchInsight += amount;
            if (countsTowardCurrentPrestige)
            {
                data.researchInsightEarnedThisPrestige += amount;
            }
        }

        private void CaptureMachineLearningMemoryIfEligible()
        {
            int limit = GetPpoMemoryRunLimit();
            if (limit <= 0 || machineLearningAgent == null)
            {
                return;
            }

            int runs = Math.Min(MachineLearningRuns, limit);
            if (runs <= 0)
            {
                return;
            }

            if (data.machineLearningPrestigeMemoryPolicy != null
                && data.machineLearningPrestigeMemoryRuns >= runs
                && data.machineLearningPrestigeMemoryRuns >= Math.Min(limit, MachineLearningRuns))
            {
                return;
            }

            data.machineLearningPrestigeMemoryPolicy = StackMergePpoAgent.CloneData(machineLearningAgent.Data);
            data.machineLearningPrestigeMemoryRuns = runs;
        }

        private void ApplyMachineLearningMemoryAfterReset()
        {
            int limit = GetPpoMemoryRunLimit();
            if (limit <= 0 || data.machineLearningPrestigeMemoryPolicy == null || machineLearningAgent == null)
            {
                return;
            }

            int retainedRuns = Math.Min(Math.Max(0, data.machineLearningPrestigeMemoryRuns), limit);
            if (retainedRuns <= 0)
            {
                return;
            }

            StackMergePpoTrainingData snapshot = StackMergePpoAgent.CloneData(data.machineLearningPrestigeMemoryPolicy);
            snapshot.episodes = Math.Min(Math.Max(0, snapshot.episodes), retainedRuns);
            machineLearningAgent.LoadSnapshot(snapshot);
            data.machineLearningPolicy = machineLearningAgent.Data;
            data.machineLearningRuns = Math.Max(data.machineLearningRuns, snapshot.episodes);
            data.machineLearningExperience = Math.Max(data.machineLearningExperience, snapshot.steps);
            data.machineLearningBestScore = Math.Max(data.machineLearningBestScore, snapshot.bestScore);
            data.machineLearningBestHigh = Math.Max(data.machineLearningBestHigh, snapshot.bestHigh);
        }

        private void ApplyOfflineProgress()
        {
            long now = GetUnixNow();
            data.lastOfflineChips = 0;
            data.lastOfflineInsight = 0;
            if (data.lastSaveUnixSeconds <= 0)
            {
                data.lastSaveUnixSeconds = now;
                return;
            }

            double elapsedHours = Math.Max(0.0, (now - data.lastSaveUnixSeconds) / 3600.0);
            double cappedHours = Math.Min(elapsedHours, GetOfflineHourCap());
            double efficiency = GetOfflineEfficiency();
            if (cappedHours <= 0.01 || efficiency <= 0.0)
            {
                data.lastSaveUnixSeconds = now;
                return;
            }

            long offlineChips = ComputeOfflineChips(cappedHours, efficiency);
            long offlineInsight = ComputeOfflineInsight(cappedHours, efficiency);
            if (offlineChips > 0)
            {
                data.chips += offlineChips;
                data.totalChipsEarned += offlineChips;
                data.lifetimeChipsEarned += offlineChips;
                data.lastOfflineChips = offlineChips;
            }

            if (offlineInsight > 0)
            {
                AddResearchInsight(offlineInsight, true);
                data.lastOfflineInsight = offlineInsight;
            }

            data.lastSaveUnixSeconds = now;
            if (offlineChips > 0 || offlineInsight > 0)
            {
                Save();
            }
        }

        private long ComputeOfflineChips(double cappedHours, double efficiency)
        {
            if (data.runsCompleted <= 0 || data.totalChipsEarned <= 0)
            {
                return 0;
            }

            double averageRunIncome = data.totalChipsEarned / (double)Math.Max(1, data.runsCompleted);
            double offlineRunsPerHour = 10.0 + data.speedLevel * 3.0;
            return Math.Max(0, (long)Math.Floor(averageRunIncome * offlineRunsPerHour * cappedHours * efficiency));
        }

        private long ComputeOfflineInsight(double cappedHours, double efficiency)
        {
            if (GetResearchLevel(ResearchId.PassiveInsight) <= 0 || data.machineLearningNormalRuns <= 0)
            {
                return 0;
            }

            double hourly = ComputeInsightValue(
                    data.machineLearningNormalBestScore,
                    data.machineLearningNormalBestHigh,
                    data.machineLearningNormalRuns,
                    data.machineLearningNormalBestScore,
                    data.machineLearningNormalBestHigh)
                * GetPassiveInsightMultiplier()
                * 4.0;
            return Math.Max(0, (long)Math.Floor(hourly * cappedHours * efficiency));
        }

        private static double ComputeInsightValue(long score, int highestBlock, int normalRuns, long bestScore, int bestHighestBlock)
        {
            double currentScoreTerm = Math.Log(1.0 + Math.Max(0, score) / 1400.0, 2.0) * 0.34;
            double bestScoreTerm = Math.Log(1.0 + Math.Max(0, bestScore) / 4500.0, 2.0) * 0.22;
            double currentHighTerm = Math.Max(0, FloorLog2(Math.Max(1, highestBlock)) - 7) * 0.46;
            double bestHighTerm = Math.Max(0, FloorLog2(Math.Max(1, bestHighestBlock)) - 8) * 0.24;
            double depthTerm = 1.0
                + 1.35 * (1.0 - Math.Exp(-Math.Max(0, normalRuns) / 120.0))
                + 1.10 * (1.0 - Math.Exp(-Math.Max(0, normalRuns) / 900.0));
            return Math.Max(0.25, (0.35 + currentScoreTerm + bestScoreTerm + currentHighTerm + bestHighTerm) * depthTerm);
        }

        private static long GetUnixNow()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        private long ApplyStageMultiplier(long amount)
        {
            double multiplier = data.modifiersMenuUnlocked ? 24.0 : data.agentsMenuUnlocked ? 5.0 : 1.0;
            return Math.Max(1, (long)Math.Ceiling(amount * multiplier));
        }

        private long ApplyIncomeMultiplier(long amount)
        {
            return Math.Max(1, (long)Math.Ceiling(amount * (1.0 + data.incomeLevel * 0.35) * GetResearchIncomeMultiplier()));
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
            data.lifetimeChipsSpent += cost;
            return true;
        }

        private void Normalize()
        {
            int solverCount = StackMergeSolverCatalog.Definitions.Length;
            if (data.solverUnlocked == null || data.solverUnlocked.Length != solverCount)
            {
                // Fresh saves start with NO solver unlocked — RAND is the first purchasable solver.
                // Existing saves keep whatever they had unlocked (copied below).
                bool[] migrated = new bool[solverCount];
                if (data.solverUnlocked != null)
                {
                    for (int i = 0; i < data.solverUnlocked.Length && i < migrated.Length; i++)
                    {
                        migrated[i] |= data.solverUnlocked[i];
                    }
                }

                data.solverUnlocked = migrated;
            }

            // RAND is no longer free — it is the first purchasable solver (see progression plan).
            data.solverMergeTuning = NormalizeSolverTuningArray(data.solverMergeTuning, solverCount);
            data.solverSafetyTuning = NormalizeSolverTuningArray(data.solverSafetyTuning, solverCount);
            data.solverLookaheadTuning = NormalizeSolverTuningArray(data.solverLookaheadTuning, solverCount);
            data.solverTuningValues = NormalizeSolverTuningValues(data.solverTuningValues, solverCount);
            MigrateLegacySolverTunings();
            EnsureSolverLifetimeRuns();
            data.selectedSolver = Mathf.Clamp(data.selectedSolver, 0, solverCount - 1);
            if (!data.solverUnlocked[data.selectedSolver])
            {
                data.selectedSolver = 0;
            }

            data.solverDeselected = data.solverDeselected && data.solverUnlocked[data.selectedSolver];
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
            data.scalingFrequencyLevel = Mathf.Clamp(data.scalingFrequencyLevel, 0, ScalingFrequencyUpgradeCosts.Length);
            data.profitableEndingLevel = Mathf.Clamp(data.profitableEndingLevel, 0, ProfitableEndingUpgradeCosts.Length);
            data.modifierLevels = NormalizeModifierLevels(data.modifierLevels);
            data.researchInsight = Math.Max(0, data.researchInsight);
            data.lifetimeResearchInsight = Math.Max(data.lifetimeResearchInsight, data.researchInsight);
            data.prestigeCount = Math.Max(0, data.prestigeCount);
            data.lastPrestigeInsight = Math.Max(0, data.lastPrestigeInsight);
            data.bestPrestigeInsight = Math.Max(data.bestPrestigeInsight, data.lastPrestigeInsight);
            data.researchLevels = NormalizeResearchLevels(data.researchLevels);
            if (!data.modifiersMenuUnlocked && data.modifierLevels.Any(level => level > 0))
            {
                data.modifiersMenuUnlocked = true;
            }

            data.lifetimeModifiersUnlocked |= data.modifiersMenuUnlocked;

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

            data.lifetimeAgentsUnlocked |= data.agentsMenuUnlocked;
            EnsureAgentLifetimeRuns();

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
            data.lifetimeChipsSpent = Math.Max(data.lifetimeChipsSpent, data.totalChipsSpent);
            data.lifetimeChipsEarned = Math.Max(data.lifetimeChipsEarned, data.totalChipsEarned);
            data.lifetimeRunsCompleted = Math.Max(data.lifetimeRunsCompleted, data.runsCompleted);
            data.lifetimeManualRunsCompleted = Math.Max(data.lifetimeManualRunsCompleted, data.manualRunsCompleted);
            data.lifetimeMoves = Math.Max(data.lifetimeMoves, data.totalBlocksDropped);
            data.lifetimeMerges = Math.Max(data.lifetimeMerges, data.totalMerges);
            data.lifetimeHighestBlockEver = Math.Max(2, Math.Max(data.lifetimeHighestBlockEver, data.highestBlockEver));
            data.lifetimeUnstableSaves = Math.Max(0, data.lifetimeUnstableSaves);
            data.lifetimeJokerMerges = Math.Max(0, data.lifetimeJokerMerges);
            data.machineLearningExperience = Math.Max(0f, data.machineLearningExperience);
            data.machineLearningRuns = Math.Max(0, data.machineLearningRuns);
            data.machineLearningBestScore = Math.Max(0, data.machineLearningBestScore);
            data.machineLearningBestHigh = Math.Max(0, data.machineLearningBestHigh);
            data.machineLearningPolicy ??= new StackMergePpoTrainingData();
            data.machineLearningNormalRuns = Math.Max(0, data.machineLearningNormalRuns);
            data.machineLearningNormalBestScore = Math.Max(0, data.machineLearningNormalBestScore);
            data.machineLearningNormalBestHigh = Math.Max(0, data.machineLearningNormalBestHigh);
            data.machineLearningNormalFrames = Math.Max(0, data.machineLearningNormalFrames);
            data.machineLearningPrestigeMemoryRuns = Math.Max(0, data.machineLearningPrestigeMemoryRuns);
            if (data.machineLearningPrestigeMemoryRuns <= 0)
            {
                data.machineLearningPrestigeMemoryPolicy = null;
            }

            data.passiveResearchProgress = Math.Max(0.0, data.passiveResearchProgress);
            data.researchInsightEarnedThisPrestige = Math.Max(0.0, data.researchInsightEarnedThisPrestige);
            data.lastSaveUnixSeconds = Math.Max(0, data.lastSaveUnixSeconds);
            data.lastOfflineChips = Math.Max(0, data.lastOfflineChips);
            data.lastOfflineInsight = Math.Max(0, data.lastOfflineInsight);
            if (!IsSolverUnlocked(SolverId.MachineLearning))
            {
                data.machineLearningTrainingMode = false;
            }

            if (data.runHistory.Length > 0)
            {
                data.highestBlockEver = Math.Max(data.highestBlockEver, data.runHistory.Max(entry => Math.Max(0, entry.highestMergedBlock)));
                data.lifetimeHighestBlockEver = Math.Max(data.lifetimeHighestBlockEver, data.highestBlockEver);
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

        private static int[] NormalizeResearchLevels(int[] source)
        {
            int[] normalized = new int[Research.Length];
            if (source == null)
            {
                return normalized;
            }

            for (int i = 0; i < source.Length && i < normalized.Length; i++)
            {
                normalized[i] = Mathf.Clamp(source[i], 0, Research[i].MaxLevel);
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
