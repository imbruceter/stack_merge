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
        public int computeSpeedLevel;
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
        public int passiveYieldLevel;
        public int passiveTickRateLevel;
        public int activeMultiplierLevel;
        // PPO Curriculum: two per-cycle upgrades (amount + rate) that shave the PPO Playing-mode frame
        // REQUIREMENT (not frames — frames are the PPO's real knowledge and can't be faked) while the
        // player is actively in PPO Training mode. Reset on prestige.
        public int curriculumAmountLevel;
        public int curriculumRateLevel;
        public long curriculumFrameReduction;
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
        public long lifetimeTokensConsumed;
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
        public long machineLearningPrestigeMemoryFrames;
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
        // Datacenter layer (unlocks at prestige 5; persists across prestiges like research).
        public int[] datacenterRackCounts;
        public int[] datacenterFacilityLevels;
        public float[] datacenterAllocation;
        // Auto Buy (reward of the "First Prestige" goal; persists across prestiges).
        public bool autoBuyAlgorithms;
        public bool autoBuyUpgrades;
        public bool autoBuyAgents;
        public bool autoBuyModifiers;
        // Gameplay-texture upgrades.
        public int comboEngineLevel;
        public int salvageProtocolLevel;
        public int tokenDividendLevel;
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

    /// <summary>
    /// Cosmetic numeral skins for block faces (and the PPO training matrix). Order matches the
    /// Settings dropdown. All non-Standard styles are goal rewards.
    /// </summary>
    public enum BlockNumeralStyle
    {
        Standard = 0,
        Byte = 1,
        Hexadecimal = 2,
        Power = 3,
        Roman = 4,
        Scientific = 5
    }

    /// <summary>
    /// Income bookkeeping categories for the balance tooling. Source amounts sum exactly to
    /// TotalChipsEarned; multiplier-type effects (Combo) are credited with the EXTRA chips they
    /// produced on top of the base amount.
    /// </summary>
    public enum IncomeSource
    {
        Placement = 0,
        Merge = 1,
        NewHigh = 2,
        Combo = 3,
        RunBonus = 4,
        Salvage = 5,
        Passive = 6,
        Offline = 7
    }

    public enum AutoBuyCategory
    {
        Algorithms = 0,
        Upgrades = 1,
        Agents = 2,
        Modifiers = 3
    }

    public enum DatacenterRackId
    {
        CpuRack = 0,
        GpuRack = 1,
        TpuPod = 2,
        NeuralFabric = 3
    }

    public enum DatacenterFacilityId
    {
        PowerGrid = 0,
        CoolingLoop = 1,
        FabricInterconnect = 2
    }

    public enum DatacenterAllocationId
    {
        TrainingCluster = 0,
        AnalysisNode = 1,
        MarketBots = 2
    }

    public readonly struct DatacenterRackDefinition
    {
        public DatacenterRackDefinition(DatacenterRackId id, string displayName, string description, long baseCost, double costGrowth, double baseGigaflops)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            BaseCost = baseCost;
            CostGrowth = costGrowth;
            BaseGigaflops = baseGigaflops;
        }

        public DatacenterRackId Id { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public long BaseCost { get; }

        public double CostGrowth { get; }

        public double BaseGigaflops { get; }
    }

    public readonly struct DatacenterFacilityDefinition
    {
        public DatacenterFacilityDefinition(DatacenterFacilityId id, string displayName, string description, int maxLevel, long baseCost, double costGrowth)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            MaxLevel = maxLevel;
            BaseCost = baseCost;
            CostGrowth = costGrowth;
        }

        public DatacenterFacilityId Id { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public int MaxLevel { get; }

        public long BaseCost { get; }

        public double CostGrowth { get; }
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
        MaxedResearchCount,
        DatacenterRackTypesOwned,
        DatacenterGigaflops,
        LifetimeTokensConsumed,
        TokensHeld
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
        OfflineTime = 12,
        AgentSynergy = 13,
        BulkDiscount = 14,
        EvaluationEfficiency = 15
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
        private const int TokenPackCost = 10000;
        // 10 (was 50): tokens should be a resource to MANAGE, not a rounding error — especially
        // now that Token Dividend pays for hoarding them.
        private const int TokenPackSize = 20;
        private const int ExtraAgentSlotUpgradeCost = 1500000;
        private const int ModifiersMenuUnlockCost = 3000000;
        // The Modifier gate must be reachable WITHOUT modifiers (they come after it). Reaching 2048 /
        // a 40k run needs the longer runs that modifiers enable, so those are impossible pre-gate.
        // 1024 / ~6k are the realistic ceiling of a strong solver at max capacity without modifiers;
        // the real "late game" pacing comes from the chip cost of the upgrades + Modifiers menu (3M).
        private const int ModifierGateRuns = 1000;
        // 5 of the 7 available non-PPO solvers (was 7 of 12 before the solver cull).
        private const int ModifierGateSolvers = 5;
        private const int ModifierGateBestScore = 7000;
        private const int ModifierGateMerges = 100000;
        private const int ModifierGateHighestBlock = 512;
        private const int TokenProspectorMergeTarget = 8;

        // 10-level ladders: the old 5-level versions had ×5-6 price jumps that left long purchase
        // droughts. Endpoints (first cost, last cost, total effect) are unchanged — the same climb
        // is just spread over twice as many, more frequent purchases.
        private static readonly int[] SpeedUpgradeCosts = { 6000, 12000, 25000, 55000, 110000, 250000, 550000, 1250000, 3000000, 7000000 };
        private static readonly float[] MoveIntervals = { 0.18f, 0.146f, 0.118f, 0.096f, 0.078f, 0.063f, 0.051f, 0.041f, 0.034f, 0.027f, 0.022f };
        // Second Solver Speed axis: shrinks the pacing overhead specific to the compute-heavy
        // search solvers (Plan3/Plan5/MOCA/MOCA+/MCTS) instead of every solver uniformly. Priced a
        // little above base Speed since it's a narrower, more specialized payoff.
        private static readonly int[] ComputeSpeedUpgradeCosts = { 150000, 300000, 900000, 2500000, 6500000 };
        private const double ComputeSpeedReductionPerLevel = 0.11; // -11%/level pacing overhead for heavy solvers, capped at 65% (Mathf.Max floor in GetMoveInterval)
        private static readonly int[] StackCapacityCosts = { 12000, 70000, 400000, 2200000, 11000000 };
        private static readonly int[] QueuePreviewUpgradeCosts = { 40000, 400000 };
        private static readonly int[] IncomeUpgradeCosts = { 4000, 10000, 25000, 62000, 155000, 390000, 970000, 2400000, 6000000, 15000000 };
        // +22%/level over 10 levels ≈ the old +35%/level over 5.
        public const double IncomeBonusPerLevel = 0.22;
        private static readonly int[] DifficultyUpgradeCosts = { 60000, 180000, 600000, 1800000, 6000000 };
        private static readonly int[] ScalingFrequencyUpgradeCosts = { 90000, 150000, 245000, 400000, 660000, 1100000, 1800000, 2950000, 4850000, 8000000 };
        private static readonly int[] ProfitableEndingUpgradeCosts = { 60000, 170000, 480000, 1350000, 3800000 };
        // Halved from 0.09 when the ladder went 5 → 10 levels; MUST stay in sync with the weight
        // formula in StackMergeGameState.GenerateNextBlock (same 0.045 constant there).
        private const double ScalingFrequencyPressurePerLevel = 0.045;
        private const double ProfitableEndingBonusPerLevel = 0.15;

        // Passive Production family: chips trickle in on a timer, independent of moves/merges, so
        // there's always something accruing even while just watching the solver play.
        private static readonly int[] PassiveYieldUpgradeCosts = { 5000, 10500, 22000, 46000, 96000, 190000, 370000, 700000, 1300000, 2400000 };
        // Chips per tick by level, before stage/income multipliers. Geometric so late levels stay
        // relevant next to per-run income (the old linear 3/level topped out at a laughable 15).
        private static readonly long[] PassiveYieldPerTickByLevel = { 0, 100, 200, 350, 500, 800, 1200, 1500, 2000, 3000, 5000 };
        private static readonly int[] PassiveTickRateUpgradeCosts = { 7000, 14000, 29000, 60000, 120000, 235000, 450000, 850000, 1550000, 2800000 };
        // Seconds between passive ticks. Index 0 is the base rate before any Tick Rate level is bought.
        private static readonly float[] PassiveTickIntervals = { 10f, 9f, 8f, 7f, 6f, 5f, 4f, 3f, 2f, 1.5f, 1f };
        private static readonly int[] ActiveMultiplierUpgradeCosts = { 4000, 8700, 19000, 41000, 88000, 180000, 360000, 700000, 1350000, 2500000 };
        private const double ActiveMultiplierBonusPerLevel = 0.2; // +20% passive yield per level while actively playing (10 levels = the old +200% total)

        // PPO Curriculum family: shaves the Playing-mode frame requirement while the player is in PPO
        // Training mode. Amount = frames shaved per tick; Rate = tick interval. Billion-scale prices
        // (long[] — 3B+ overflow int) are intentional — early prestiges afford the low levels; late
        // prestiges (huge multipliers) can max it. At max both: 150k/hour reduction; at L5/L5: 50k/hour.
        private static readonly long[] CurriculumAmountUpgradeCosts = { 500_000_000L, 1_500_000_000L, 3_000_000_000L, 5_000_000_000L, 8_000_000_000L, 12_000_000_000L, 18_000_000_000L, 25_000_000_000L, 35_000_000_000L, 50_000_000_000L };
        private static readonly long[] CurriculumFrameReductionPerTick = { 0, 20, 40, 60, 80, 100, 120, 140, 160, 180, 200 };
        private static readonly long[] CurriculumRateUpgradeCosts = { 500_000_000L, 1_500_000_000L, 3_000_000_000L, 5_000_000_000L, 8_000_000_000L, 12_000_000_000L, 18_000_000_000L, 25_000_000_000L, 35_000_000_000L, 50_000_000_000L };
        // Seconds between curriculum ticks. Index 0 is the base rate before any Rate level is bought.
        private static readonly float[] CurriculumTickIntervals = { 10f, 9.44f, 8.88f, 8.32f, 7.76f, 7.2f, 6.72f, 6.24f, 5.76f, 5.28f, 4.8f };

        // Combo Engine: consecutive merging moves build a streak; every streak step adds
        // +1%/level to move income, streak capped at 20 (L10 → up to ×3 at full streak).
        private static readonly int[] ComboEngineUpgradeCosts = { 15000, 30000, 60000, 125000, 250000, 500000, 1000000, 2000000, 4000000, 8000000 };
        private const double ComboBonusPerStreakPerLevel = 0.01;
        private const int ComboStreakCap = 20;

        // Salvage Protocol: at game over, a share of the run's final score converts to chips
        // (+4%/level, 40% at max) — makes the run's end a real payout instead of a full stop.
        // (Score-based: the old stranded-board basis was so small it could never pay for itself.)
        private static readonly int[] SalvageProtocolUpgradeCosts = { 25000, 50000, 100000, 200000, 400000, 800000, 1600000, 3200000, 6400000, 12800000 };
        private const double SalvageSharePerLevel = 0.06;

        // Token Dividend: held restart tokens pay chip income, scaling with the square root of the
        // hoard (+1%/level × √tokens, no cap — quadrupling the hoard doubles the bonus). Pairs with
        // the held-token-scaled pack price so hoarding finds its own equilibrium.
        private static readonly int[] TokenDividendUpgradeCosts = { 50000, 200000, 800000, 3200000, 12800000 };
        private const double TokenDividendPerSqrtTokenPerLevel = 0.01;

        private const int AgentsMenuUnlockCost = 120000;
        private const int MaxHistoryEntries = 250;
        // Base "Evaluating…" pause after a PPO Training run — mirrors the pacing the bootstrap used
        // to hardcode; Evaluation Efficiency research shrinks it (see MachineLearningEvaluationSeconds).
        private const float BaseTrainingEvaluationSeconds = 2.5f;

        public static readonly AgentDefinition[] Agents =
        {
            // MUST stay in AgentId enum order — indexed by (int)AgentId elsewhere. UI DISPLAY order is
            // controlled separately (AgentDisplayOrder in the bootstrap via SetSiblingIndex), so reorder
            // THAT, never this array. Prices are the user's 2026-07-12 sink-ladder tuning.
            new(AgentId.MergeBroker, "Merge Broker", 12000000, "Boosts merge income.", "+75% <sprite name=\"chips\" tint=1> from merge rewards."),
            new(AgentId.HighwaterAnalyst, "Highwater Analyst", 250000, "Rewards new highs.", "+200% <sprite name=\"chips\" tint=1> from new highest-block rewards."),
            new(AgentId.ScoreAuditor, "Score Auditor", 800000, "Turns score into <sprite name=\"chips\" tint=1>.", "+100% <sprite name=\"chips\" tint=1> from end-of-run score bonus."),
            new(AgentId.Overclocker, "Overclocker", 1500000, "Runs the solver faster.", "Solver move interval is 25% shorter."),
            new(AgentId.Quartermaster, "Quartermaster", 500000, "Improves baseline income.", "+10 <sprite name=\"chips\" tint=1> on every successful placement."),
            new(AgentId.RestartSponsor, "Restart Sponsor", 100000, "Keeps restarts funded.", "Auto Restart consumes no tokens while this agent is active."),
            new(AgentId.TokenProspector, "Token Prospector", 20000000, "Turns merge volume into restart fuel.", $"+1 token for every {TokenProspectorMergeTarget} merges while active."),
            new(AgentId.MoveDividend, "Move Dividend", 6000000, "Rewards long, stable runs.", "End-of-run <sprite name=\"chips\" tint=1> gain a bonus from total moves completed."),
            new(AgentId.VelocityTrader, "Velocity Trader", 4000000, "Rewards fast solvers.", "End-of-run <sprite name=\"chips\" tint=1> gain a throughput bonus from moves per second.")
        };

        public static readonly ModifierDefinition[] Modifiers =
        {
            // Costs doubled 2026-07-12: at the ×24 modifier stage the old prices let the whole tier
            // be burst-bought in minutes. Sim-calibrated with the stage cliff and agent/solver sinks.
            // MUST stay in ModifierId enum order — indexed by (int)ModifierId (GetModifierDefinition,
            // effects, save data). UI DISPLAY order is controlled separately (ModifierDisplayOrder in the
            // bootstrap via SetSiblingIndex), so reorder THAT, never this array. Prices are the user's tuning.
            new(ModifierId.UnstableStack, "Unstable Stack", "Deletes bottom blocks when a full stack would fail.", "Each level gives one rescue per run. If a full stack receives a non-merge block, its bottom block is removed without reducing score.", 3000000, 5000000, 10000000, 20000000, 40000000),
            new(ModifierId.CatalystStack, "Catalyst Stack", "Converts merges into more <sprite name=\"chips\" tint=1>.", "Merge rewards are permanently doubled on every run after purchase.", 10000000),
            new(ModifierId.MirrorStack, "Mirror Stack", "Lets stack ends interact.", "Unlocks a special merge. If the top and bottom block of a stack match, they merge through the stack.", 25000000),
            new(ModifierId.Joker, "Joker", "Adds wild blocks to the queue.", "Unlocks occasional Joker blocks. A Joker placed onto any block merges with it.", 80000000),
            new(ModifierId.MinersPickaxe, "Miner's Pickaxe", "Lets solvers remove blocks from the board.", "Each level gives one pickaxe use per run. The solver may delete any block in any stack.", 5000000, 8000000, 15000000, 30000000, 50000000),
            new(ModifierId.QueueScrubber, "Queue Scrubber", "Lets solvers delete bad upcoming blocks.", "Each level gives one queue skip per run. The current next block is removed and the following block moves forward.", 5000000, 8000000, 15000000, 30000000, 50000000),
            new(ModifierId.NeuralAccelerator, "Neural Accelerator", "Speeds up expensive solvers.", "MOCA runs permanently about twice as fast. Negative speed tuning on it is also twice as effective.", 2500000)
        };

        // ------------------------------------------------------------------------------------
        // Datacenter layer. Unlocks at prestige 5 and PERSISTS across prestiges — it is the
        // meta-progression the cycles fund: racks are an endless late-game chip sink, and the
        // produced compute is split between background PPO training, passive Insight and a chip
        // income multiplier. All rates are per-second of active play (ticked from the bootstrap).
        // ------------------------------------------------------------------------------------
        public const int DatacenterUnlockPrestigeCount = 5;

        // Idle-producer style growth (~1.12-1.18/unit): the first simulator pass used 1.30-1.50,
        // which capped the whole farm at ~13 units over 25 prestiges — far too steep for a layer
        // meant to absorb chips forever.
        public static readonly DatacenterRackDefinition[] DatacenterRacks =
        {
            new(DatacenterRackId.CpuRack, "CPU Rack", "Commodity silicon. Cheap, dependable, slow.", 250_000, 1.12, 1.0),
            new(DatacenterRackId.GpuRack, "GPU Rack", "Parallel throughput for tensor math.", 3_000_000, 1.14, 6.0),
            new(DatacenterRackId.TpuPod, "TPU Pod", "Purpose-built matrix engines.", 25_000_000, 1.16, 40.0),
            new(DatacenterRackId.NeuralFabric, "Neural Fabric", "Experimental wafer-scale compute.", 200_000_000, 1.18, 300.0)
        };

        public static readonly DatacenterFacilityDefinition[] DatacenterFacilities =
        {
            new(DatacenterFacilityId.PowerGrid, "Power Grid", "Dedicated feeders and battery buffer.", 10, 2_000_000, 1.8),
            new(DatacenterFacilityId.CoolingLoop, "Cooling Loop", "Immersion + rear-door heat exchangers.", 10, 8_000_000, 1.8),
            new(DatacenterFacilityId.FabricInterconnect, "Fabric Interconnect", "Low-latency mesh between pods.", 10, 60_000_000, 2.0)
        };

        private const double PowerGridFlopsBonusPerLevel = 0.06;          // +6% total rack FLOPS / level
        private const double CoolingLoopFlopsBonusPerLevel = 0.04;        // +4% total rack FLOPS / level
        private const double FabricInterconnectBonusPerLevel = 0.05;      // +5% TPU Pod + Neural Fabric output / level
        private const double DatacenterPrestigeBonusPerPrestige = 0.05;   // +5% compute per prestige — prestiging feeds the layer
        private const double DatacenterFramesPerSecondPerGigaflop = 0.50; // Training Cluster: PPO frames/sec per allocated GF/s
        private const double DatacenterInsightPerSecondPerGigaflop = 0.002; // Analysis Node: Insight/sec per allocated GF/s, pre-softcap
        private const double DatacenterMarketLogFactor = 0.25;            // Market Bots: ×(1 + 0.25·log10(1 + allocated GF/s))

        public static readonly AchievementDefinition[] Achievements =
        {
            new(0, "Chip Bank", "Earn 10000 <sprite name=\"chips\" tint=1> in total", AchievementMetric.LifetimeChipsEarned, 10_000),
            new(1, "Chip Million", "Earn 1 M <sprite name=\"chips\" tint=1> in total", AchievementMetric.LifetimeChipsEarned, 1_000_000),
            new(2, "Chip Billion", "Earn 1 B <sprite name=\"chips\" tint=1> in total", AchievementMetric.LifetimeChipsEarned, 1_000_000_000),
            new(3, "First Budget", "Spend 10000 <sprite name=\"chips\" tint=1> in total", AchievementMetric.LifetimeChipsSpent, 10_000),
            new(4, "Serious Budget", "Spend 100 K <sprite name=\"chips\" tint=1> in total", AchievementMetric.LifetimeChipsSpent, 100_000),
            new(5, "Mega Budget", "Spend 100 M <sprite name=\"chips\" tint=1> in total", AchievementMetric.LifetimeChipsSpent, 100_000_000),
            new(6, "Manual Finish", "Complete 10 runs while Auto Solver is turned off", AchievementMetric.LifetimeManualRunsCompleted, 10),
            new(7, "Solver Loyalty", "Complete 1000 runs with a solver", AchievementMetric.MaxSolverLifetimeRuns, 1000),
            new(8, "Move Habit", "Move a total of 10000 times", AchievementMetric.LifetimeMoves, 10_000),
            new(9, "Move Engine", "Move a total of 100 K times", AchievementMetric.LifetimeMoves, 100_000),
            new(10, "Move Singularity", "Move a total of 1 M times", AchievementMetric.LifetimeMoves, 1_000_000),
            new(11, "Merge Habit", "Merge a total of 10000 times", AchievementMetric.LifetimeMerges, 10_000),
            new(12, "Merge Engine", "Merge a total of 100 K times", AchievementMetric.LifetimeMerges, 100_000),
            new(13, "Merge Singularity", "Merge a total of 1 M times", AchievementMetric.LifetimeMerges, 1_000_000),
            new(14, "High 512", "Reach high 512", AchievementMetric.LifetimeHighestBlockEver, 512),
            new(15, "High 8192", "Reach high 8192", AchievementMetric.LifetimeHighestBlockEver, 8192),
            new(16, "High 32768", "Reach high 32768", AchievementMetric.LifetimeHighestBlockEver, 32768),
            new(17, "Agents Online", "Unlock Agents", AchievementMetric.AgentsUnlockedEver, 1),
            new(18, "Modifiers Online", "Unlock Modifiers", AchievementMetric.ModifiersUnlockedEver, 1),
            new(19, "Solver Tour", "Use all solvers at least once", AchievementMetric.SolversUsed, StackMergeSolverCatalog.AvailableSolverCount),
            new(20, "Agent Tour", "Use all Agents at least once", AchievementMetric.AgentsUsed, Agents.Length),
            new(21, "Unstable Lifeline", "Let Unstable Stack save your run a total of 100 times", AchievementMetric.LifetimeUnstableSaves, 100),
            new(22, "Joker Merges", "Merge with a Joker for a total of 100 times", AchievementMetric.LifetimeJokerMerges, 100),
            // IDs 28/29 sit here by ARRAY position (goal rows render in array order, not by Id), so they
            // appear directly above "First Prestige".
            new(28, "Token Furnace", "Consume a total of 5000 <sprite name=\"token\" tint=1>", AchievementMetric.LifetimeTokensConsumed, 5000),
            new(29, "Token Reserve", "Hold at least 10000 <sprite name=\"token\" tint=1> at once", AchievementMetric.TokensHeld, 10_000),
            new(23, "First Prestige", "Prestige reset for the first time", AchievementMetric.PrestigeCount, 1),
            new(24, "Prestige Loop", "Prestige reset for a total of 5 times", AchievementMetric.PrestigeCount, 5),
            new(25, "Research Complete", "Buy all the researches", AchievementMetric.MaxedResearchCount, 16),
            new(26, "Rack Collector", "Have at least 1 unit in each Server Rack", AchievementMetric.DatacenterRackTypesOwned, 4),
            new(27, "Gigaflop Farm", "Go above 1000 GF/s total compute", AchievementMetric.DatacenterGigaflops, 1000)
        };

        // Array index MUST equal the ResearchId enum value (GetResearchDefinition indexes by id).
        // Tree: Seed Capital is the root; three columns below it, strictly downward within a column.
        //   col0: Automation Memory → Algorithm Archive → Agent Synergy → Bulk Discount → Yield Theory
        //   col1: PPO Bootcamp → Evaluation Efficiency → PPO Memory → High Focus → Stability → Insight Extractor
        //   col2: Insight Amplifier → Passive Insight → Offline Engine → Offline Buffer
        // Cost ladders are shared per tier so every branch progresses at the same pace:
        //   T0 root 1/45/120/280/650 | T1 25/70/180/450/1100 | T2 90/240/600/1500/3600
        //   T3 300/800/2000/5000/12000 | T4 1200/3000/7500(/18000/42000)
        //   T5 2500/6000/15000/36000/85000 | T6 5000/12000/30000/72000/170000
        public static readonly ResearchDefinition[] Research =
        {
            new(ResearchId.InsightAmplifier, "Insight Amplifier", "+35% <sprite name=\"insight\" tint=1> from every future prestige per level.", 2, 1, 25, 70, 180, 450, 1100),
            new(ResearchId.SeedCapital, "Seed Capital", "Start after each prestige reset with <sprite name=\"chips\" tint=1> already banked. This allows making purchases right at the beginning of the game.", 1, 0, 1, 45, 120, 280, 650),
            new(ResearchId.AutomationMemory, "Automation Memory", "Permanently remembers automation unlocks per level after prestige: Auto Solve, Auto Restart <sprite name=\"token\" tint=1>, then Solver Tuning.", 0, 1, 25, 70, 180),
            new(ResearchId.AlgorithmArchive, "Algorithm Archive", "Start future prestiges with early algorithms already known: BAL, HEUR, COMBO, then LOOK.", 0, 2, 90, 240, 600, 1500),
            new(ResearchId.YieldTheory, "Yield Theory", "+30% <sprite name=\"chips\" tint=1> from every reward per level. It stacks with Chip Yield and stage multipliers, making every future playthrough visibly faster.", 0, 5, 2500, 6000, 15000, 36000, 85000),
            new(ResearchId.PpoBootcamp, "PPO Bootcamp", "PPO still resets every prestige, but each level lowers the trained frame requirement for Normal Mode by 8%.", 1, 1, 25, 70, 180, 450, 1100),
            new(ResearchId.PpoMemory, "PPO Memory", "Prestige banks 50000 PPO training frames per level. The next playthrough's PPO Training starts from that saved progress, with the knowledge already learned (instead of zero).", 1, 3, 300, 800, 2000, 5000, 12000),
            new(ResearchId.PpoHighFocus, "High Focus", "Raises PPO's reward signal for creating new highest blocks. This pushes the learner toward bigger blocks instead of only safer runs.", 1, 4, 1200, 3000, 7500, 18000, 42000),
            new(ResearchId.PpoStability, "Stability Model", "Improves PPO's survival shaping and danger penalties, making high-focus policies less likely to crash early.", 1, 5, 2500, 6000, 15000, 36000, 85000),
            new(ResearchId.InsightExtractor, "Insight Extractor", "+50% <sprite name=\"insight\" tint=1> from PPO Normal Mode performance per level. This is the late neural payoff node.", 1, 6, 5000, 12000, 30000, 72000, 170000),
            new(ResearchId.PassiveInsight, "Passive Insight", "Boosts <sprite name=\"insight\" tint=1> earned directly from PPO Normal Mode runs. Training mode never feeds this, and long cycles softcap so prestige stays valuable.", 2, 2, 90, 240, 600, 1500, 3600),
            new(ResearchId.OfflineEfficiency, "Offline Engine", "While the game is closed, <sprite name=\"chips\" tint=1> and passive <sprite name=\"insight\" tint=1> continue at a reduced rate based on your current prestige strength.", 2, 3, 300, 800, 2000, 5000, 12000),
            new(ResearchId.OfflineTime, "Offline Buffer", "Extends how many closed-game hours can be converted into offline <sprite name=\"chips\" tint=1> and <sprite name=\"insight\" tint=1>.", 2, 4, 1200, 3000, 7500),
            new(ResearchId.AgentSynergy, "Agent Synergy", "Every hired Agent's bonus effect is 25% stronger per level. Agents become a core engine of faster replays.", 0, 3, 300, 800, 2000, 5000, 12000),
            new(ResearchId.BulkDiscount, "Bulk Discount", "Upgrades, Agents and Modifiers cost 5% less per level (25% at max), and the Modifiers gate needs 15% fewer Runs & Merges per level (75% at max). Cheaper, faster shopping shortens every cycle.", 0, 4, 1200, 3000, 7500, 18000, 42000),
            new(ResearchId.EvaluationEfficiency, "Evaluation Efficiency", "Shortens the result-evaluation pause after every PPO Training run by 15% per level, so Training mode gets through its runs much faster.", 1, 2, 90, 240, 600, 1500, 3600)
        };

        private readonly StackMergeProgressionData data;
        private readonly StackMergePpoAgent machineLearningAgent;
        // Runtime-only clock for Passive Production — deliberately not persisted (losing a
        // fraction of a tick across a save/quit is inconsequential, and offline accrual is handled
        // separately by the Offline Engine/Buffer research).
        private float passiveProductionTimer;
        // Runtime-only clock for the PPO Curriculum tick (accumulated reduction itself IS persisted).
        private float curriculumTimer;
        // Runtime-only fractional accumulators for the Datacenter's per-second production.
        private double datacenterFrameCarry;
        private double datacenterInsightCarry;
        // Combo Engine streak — runtime-only, reset every run end.
        private int comboStreak;
        // Income ledger — runtime-only, per-source chip bookkeeping for the balance tooling.
        private readonly long[] incomeLedger = new long[8];

        public StackMergeProgression(StackMergeProgressionData data)
            : this(data, true)
        {
        }

        private StackMergeProgression(StackMergeProgressionData data, bool applyOfflineProgress)
        {
            this.data = data ?? new StackMergeProgressionData();
            Normalize();
            machineLearningAgent = new StackMergePpoAgent(this.data.machineLearningPolicy);
            ApplyMachineLearningResearchBonuses();
            if (applyOfflineProgress)
            {
                ApplyOfflineProgress();
            }
        }

#if UNITY_EDITOR
        public StackMergeProgression CloneForSimulation()
        {
            string json = JsonUtility.ToJson(data);
            var clone = new StackMergeProgression(JsonUtility.FromJson<StackMergeProgressionData>(json), false)
            {
                passiveProductionTimer = passiveProductionTimer,
                datacenterFrameCarry = datacenterFrameCarry,
                datacenterInsightCarry = datacenterInsightCarry,
                comboStreak = comboStreak
            };
            Array.Copy(incomeLedger, clone.incomeLedger, Math.Min(incomeLedger.Length, clone.incomeLedger.Length));
            return clone;
        }
#endif

        public long Chips => data.chips;

        public long Tokens => data.tokens;

        public SolverId SelectedSolver => (SolverId)data.selectedSolver;

        public bool SolverDeselected => data.solverDeselected;

        public int SpeedLevel => data.speedLevel;

        public int ComputeSpeedLevel => data.computeSpeedLevel;

        public int MaxComputeSpeedLevel => ComputeSpeedUpgradeCosts.Length;

        public bool IsMaxComputeSpeed => data.computeSpeedLevel >= ComputeSpeedUpgradeCosts.Length;

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

        public int PassiveYieldLevel => data.passiveYieldLevel;

        public int MaxPassiveYieldLevel => PassiveYieldUpgradeCosts.Length;

        public int PassiveTickRateLevel => data.passiveTickRateLevel;

        public int MaxPassiveTickRateLevel => PassiveTickRateUpgradeCosts.Length;

        public int ActiveMultiplierLevel => data.activeMultiplierLevel;

        public int MaxActiveMultiplierLevel => ActiveMultiplierUpgradeCosts.Length;

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

        public long MachineLearningMemoryFrames => Math.Max(0, data.machineLearningPrestigeMemoryFrames);

        public long LastOfflineChips => Math.Max(0, data.lastOfflineChips);

        public long LastOfflineInsight => Math.Max(0, data.lastOfflineInsight);

        /// <summary>Hours of the last load's rewarded offline period (already capped). Runtime-only.</summary>
        public double LastOfflineHours { get; private set; }

        /// <summary>Current offline reward cap in hours (0 while Offline Engine is unresearched).</summary>
        public double OfflineHourCap => GetOfflineHourCap();

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

        /// <summary>The frame requirement after the PPO Curriculum's accumulated reduction (floored at 1).</summary>
        public long EffectivePlayingModeFrameRequirement =>
            Math.Max(1, MachineLearningPlayingModeFrameRequirement - Math.Max(0, data.curriculumFrameReduction));

        public bool MachineLearningPlayingModeUnlocked => MachineLearningFrames >= EffectivePlayingModeFrameRequirement;

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
            && AllAgentsUnlocked
            && UnlockedSolverCount >= ModifierGateSolvers
            && data.runsCompleted >= GatedModifierRuns
            && data.totalMerges >= GatedModifierMerges
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

        /// <summary>How much pacing overhead Compute Speed strips from the heavy search solvers at a given level.</summary>
        public static float GetComputeSpeedEffectPercent(int level)
        {
            int clamped = Mathf.Clamp(level, 0, ComputeSpeedUpgradeCosts.Length);
            return Mathf.Min(65f, (float)(clamped * ComputeSpeedReductionPerLevel * 100.0));
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

        /// <summary>Raw chips-per-tick at a given Passive Yield level, before stage/income multipliers.</summary>
        public static long GetPassiveYieldPerTick(int level)
        {
            int clamped = Mathf.Clamp(level, 0, PassiveYieldPerTickByLevel.Length - 1);
            return PassiveYieldPerTickByLevel[clamped];
        }

        /// <summary>Combo Engine: % income bonus per streak step at a given level.</summary>
        public static float GetComboEffectPercentPerStreak(int level)
        {
            int clamped = Mathf.Clamp(level, 0, ComboEngineUpgradeCosts.Length);
            return (float)(clamped * ComboBonusPerStreakPerLevel * 100.0);
        }

        /// <summary>Salvage Protocol: % of the run's final score converted to chips at game over.</summary>
        public static float GetSalvageEffectPercent(int level)
        {
            int clamped = Mathf.Clamp(level, 0, SalvageProtocolUpgradeCosts.Length);
            return (float)(clamped * SalvageSharePerLevel * 100.0);
        }

        /// <summary>Token Dividend: % income bonus per √(held tokens) at a given level.</summary>
        public static float GetTokenDividendPercentPerSqrtToken(int level)
        {
            int clamped = Mathf.Clamp(level, 0, TokenDividendUpgradeCosts.Length);
            return (float)(clamped * TokenDividendPerSqrtTokenPerLevel * 100.0);
        }

        /// <summary>Seconds between passive ticks at a given Passive Tick Rate level.</summary>
        public static float GetPassiveTickInterval(int level)
        {
            int clamped = Mathf.Clamp(level, 0, PassiveTickIntervals.Length - 1);
            return PassiveTickIntervals[clamped];
        }

        public static float GetActiveMultiplierEffectPercent(int level)
        {
            int clamped = Mathf.Clamp(level, 0, ActiveMultiplierUpgradeCosts.Length);
            return (float)(clamped * ActiveMultiplierBonusPerLevel * 100.0);
        }

        public bool IsMaxSpeed => data.speedLevel >= MoveIntervals.Length - 1;

        public int MaxSpeedLevel => SpeedUpgradeCosts.Length;

        public bool IsMaxStackCapacity => StackCapacity >= StackMergeGameState.MaxStackCapacity;

        public bool IsMaxQueuePreview => data.queuePreviewLevel >= QueuePreviewUpgradeCosts.Length;

        public bool IsMaxIncome => data.incomeLevel >= IncomeUpgradeCosts.Length;

        public bool IsMaxDifficulty => data.difficultyLevel >= DifficultyUpgradeCosts.Length;

        public bool IsMaxScalingFrequency => data.scalingFrequencyLevel >= ScalingFrequencyUpgradeCosts.Length;

        public bool IsMaxProfitableEnding => data.profitableEndingLevel >= ProfitableEndingUpgradeCosts.Length;

        public bool IsMaxPassiveYield => data.passiveYieldLevel >= PassiveYieldUpgradeCosts.Length;

        /// <summary>Passive Tick Rate and Active Multiplier only make sense once Passive Yield produces anything.</summary>
        public bool PassiveSupportUpgradesUnlocked => data.passiveYieldLevel >= 1;

        /// <summary>Scaling Frequency modifies the difficulty scaling curve, so it needs Difficulty L1 first.</summary>
        public bool ScalingFrequencyPurchasable => data.difficultyLevel >= 1;

        public bool IsMaxPassiveTickRate => data.passiveTickRateLevel >= PassiveTickRateUpgradeCosts.Length;

        public bool IsMaxActiveMultiplier => data.activeMultiplierLevel >= ActiveMultiplierUpgradeCosts.Length;

        public int ActiveAgentSlots => BaseAgentSlots + (data.extraAgentSlotUnlocked ? 1 : 0);

        public int MaxAgentSlots => BaseAgentSlots + 1;

        public float GetMoveInterval(SolverId solverId)
        {
            float baseInterval = MoveIntervals[Mathf.Clamp(data.speedLevel, 0, MoveIntervals.Length - 1)];
            float minInterval = solverId == SolverId.MachineLearning ? 0.006f : 0.012f;
            float trainingMultiplier = solverId == SolverId.MachineLearning && data.machineLearningTrainingMode ? 0.70f : 1f;
            float pacing = GetSolverPacingMultiplier(solverId);
            if (IsComputeHeavySolver(solverId))
            {
                // Compute Speed only shrinks the EXTRA overhead that heavy search-based solvers
                // carry on top of the shared baseline — it does nothing for light solvers (RAND,
                // MERG, BAL, ...), which is what makes it a different lever from Solver Speed
                // instead of just the same upgrade under a second name.
                pacing *= Mathf.Max(0.35f, 1f - data.computeSpeedLevel * (float)ComputeSpeedReductionPerLevel);
            }

            return Mathf.Max(minInterval, baseInterval * pacing * AgentMoveIntervalMultiplier * trainingMultiplier);
        }

        /// <summary>
        /// The compute-heavy, search-based solvers Compute Speed targets. Light solvers (RAND
        /// through AntiStall) already run near-instantly and get nothing from this upgrade — only
        /// Solver Speed helps them.
        /// </summary>
        public static bool IsComputeHeavySolver(SolverId solverId)
        {
            return solverId is SolverId.Plan3 or SolverId.Plan5 or SolverId.Moca or SolverId.MocaPlus or SolverId.Mcts;
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
            return GetResearchEffectSummary(researchId, GetResearchLevel(researchId));
        }

        /// <summary>
        /// Effect string computed at an EXPLICIT level (for current→next previews). The simple
        /// per-level researches inline their formula so the next level can be shown even at level 0;
        /// the aggregate ones (PassiveInsight) read live state and so show the same value regardless
        /// of the passed level — the UI just omits the arrow when current == next.
        /// </summary>
        public string GetResearchEffectSummary(ResearchId researchId, int level)
        {
            return researchId switch
            {
                ResearchId.InsightAmplifier => $"Prestige Insight x{1.0 + level * 0.35:0.00}",
                ResearchId.SeedCapital => $"Start chips: {GetPrestigeStartChips(level)}",
                ResearchId.AutomationMemory => level switch
                {
                    0 => "No automation is remembered yet.",
                    1 => "Auto Solve unlock remembered.",
                    2 => $"Auto Restart unlock and {TokenPackSize} tokens remembered.",
                    _ => "Auto Solve, Auto Restart, tokens, and Solver Tuning remembered."
                },
                ResearchId.AlgorithmArchive => level switch
                {
                    0 => "No algorithms remembered yet.",
                    1 => "Start with BAL.",
                    2 => "Start with BAL and HEUR.",
                    3 => "Start with BAL, HEUR, and COMBO.",
                    _ => "Start with BAL, HEUR, COMBO, and LOOK."
                },
                ResearchId.YieldTheory => $"Chip rewards x{1.0 + level * 0.30:0.00}",
                ResearchId.PpoBootcamp => $"PPO Normal mode at {MachineLearningPlayingModeFrameRequirement} frames",
                ResearchId.PpoMemory => $"Warm start: {GetPpoMemoryFrameAllowance(level):N0} frames retained",
                ResearchId.PpoHighFocus => $"New-high learning x{1f + level * 0.12f:0.00}",
                ResearchId.PpoStability => $"Survival shaping x{1f + level * 0.10f:0.00}",
                ResearchId.InsightExtractor => $"Normal-mode prestige x{1.0 + level * 0.50:0.00}",
                ResearchId.PassiveInsight => $"Normal Insight x{GetNormalModeInsightMultiplier():0.00}",
                ResearchId.OfflineEfficiency => $"Offline efficiency {(level <= 0 ? 0.0 : 0.08 + level * 0.05) * 100:0}%",
                ResearchId.OfflineTime => $"Offline cap {(level switch { <= 0 => 1.0, 1 => 3.0, 2 => 6.0, 3 => 12.0, 4 => 18.0, _ => 24.0 }):0.#}h",
                ResearchId.AgentSynergy => $"Agent bonuses x{1.0 + level * 0.25:0.00}",
                ResearchId.BulkDiscount => $"Shop prices x{Math.Max(0.75, 1.0 - level * 0.05):0.00}",
                ResearchId.EvaluationEfficiency => $"Training eval pause {BaseTrainingEvaluationSeconds * Mathf.Max(0.25f, 1f - level * 0.15f):0.0}s",
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
            if (!PrestigeAvailable)
            {
                return 0;
            }

            // The first prestige always yields exactly 1 Insight once PPO is trained (that is what
            // Seed Capital L1 costs) — it must NOT require a Normal-mode run first. This check has to
            // come before the normal-runs guard below, or a freshly-trained PPO previews 0.
            if (data.prestigeCount <= 0)
            {
                return 1;
            }

            if (data.machineLearningNormalRuns <= 0)
            {
                return 0;
            }

            long bestScore = Math.Max(0, data.machineLearningNormalBestScore);
            int bestHigh = Math.Max(1, data.machineLearningNormalBestHigh);
            long frames = Math.Max(0, data.machineLearningNormalFrames);
            int normalRuns = Math.Max(0, data.machineLearningNormalRuns);

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
                return $"Finish PPO Training first.\n<b>{MachineLearningFrames:N0} / {EffectivePlayingModeFrameRequirement:N0} frames</b>.";
            }

            return $"PPO is trained, prestige reset unlocked.\n<b>Gain: <sprite name=\"insight\" tint=1> {PreviewPrestigeInsightGain():N0}</b>.";
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
            return data.solverTuningUnlocked ? 0 : ApplyShopDiscount(SolverTuningUnlockCost);
        }

        public bool BuySolverTuningUnlock()
        {
            if (data.solverTuningUnlocked)
            {
                return true;
            }

            if (!Spend(GetSolverTuningUnlockCost()))
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
            return IsMaxSpeed ? 0 : ApplyShopDiscount(SpeedUpgradeCosts[data.speedLevel]);
        }

        public long GetSpeedUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < SpeedUpgradeCosts.Length ? ApplyShopDiscount(SpeedUpgradeCosts[upgradeIndex]) : 0;
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

        public long GetComputeSpeedUpgradeCost()
        {
            return IsMaxComputeSpeed ? 0 : ApplyShopDiscount(ComputeSpeedUpgradeCosts[data.computeSpeedLevel]);
        }

        public bool BuyComputeSpeedUpgrade()
        {
            if (IsMaxComputeSpeed || !Spend(GetComputeSpeedUpgradeCost()))
            {
                return false;
            }

            data.computeSpeedLevel++;
            return true;
        }

        public long GetAutoSolveCost()
        {
            return data.autoSolveUnlocked ? 0 : ApplyShopDiscount(AutoSolveUnlockCost);
        }

        public bool ToggleOrBuyAutoSolve()
        {
            if (!data.autoSolveUnlocked)
            {
                if (!HasPurchasedSolver || !Spend(GetAutoSolveCost()))
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
            return data.autoRestartUnlocked ? 0 : ApplyShopDiscount(AutoRestartUnlockCost);
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

        /// <summary>
        /// Token pack price scales with the currently HELD tokens (+1% per held token): spenders
        /// restock cheap, hoarders pay a rising premium. Counterweight to the uncapped sqrt Token
        /// Dividend — the two curves meet at a natural hoard equilibrium instead of a hard cap.
        /// </summary>
        public long GetTokenPackCost()
        {
            return (long)Math.Ceiling(TokenPackCost * (1.0 + Math.Max(0, Tokens) / 100.0));
        }

        public int GetTokenPackSize()
        {
            return TokenPackSize;
        }

        public bool BuyTokenPack()
        {
            if (!Spend(GetTokenPackCost()))
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
            data.lifetimeTokensConsumed++;
            return true;
        }

        public long GetStackCapacityUpgradeCost()
        {
            return IsMaxStackCapacity ? 0 : ApplyShopDiscount(StackCapacityCosts[data.stackCapacityLevel]);
        }

        public long GetStackCapacityUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < StackCapacityCosts.Length ? ApplyShopDiscount(StackCapacityCosts[upgradeIndex]) : 0;
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
            return IsMaxQueuePreview ? 0 : ApplyShopDiscount(QueuePreviewUpgradeCosts[data.queuePreviewLevel]);
        }

        public long GetQueuePreviewUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < QueuePreviewUpgradeCosts.Length ? ApplyShopDiscount(QueuePreviewUpgradeCosts[upgradeIndex]) : 0;
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
            return IsMaxIncome ? 0 : ApplyShopDiscount(IncomeUpgradeCosts[data.incomeLevel]);
        }

        public long GetIncomeUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < IncomeUpgradeCosts.Length ? ApplyShopDiscount(IncomeUpgradeCosts[upgradeIndex]) : 0;
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
            return IsMaxDifficulty ? 0 : ApplyShopDiscount(DifficultyUpgradeCosts[data.difficultyLevel]);
        }

        public long GetDifficultyUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < DifficultyUpgradeCosts.Length ? ApplyShopDiscount(DifficultyUpgradeCosts[upgradeIndex]) : 0;
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
            return IsMaxScalingFrequency ? 0 : ApplyShopDiscount(ScalingFrequencyUpgradeCosts[data.scalingFrequencyLevel]);
        }

        public long GetScalingFrequencyUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < ScalingFrequencyUpgradeCosts.Length ? ApplyShopDiscount(ScalingFrequencyUpgradeCosts[upgradeIndex]) : 0;
        }

        public bool BuyScalingFrequencyUpgrade()
        {
            if (!ScalingFrequencyPurchasable || IsMaxScalingFrequency || !Spend(GetScalingFrequencyUpgradeCost()))
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
            return IsMaxProfitableEnding ? 0 : ApplyShopDiscount(ProfitableEndingUpgradeCosts[data.profitableEndingLevel]);
        }

        public long GetProfitableEndingUpgradeCost(int upgradeIndex)
        {
            return upgradeIndex >= 0 && upgradeIndex < ProfitableEndingUpgradeCosts.Length ? ApplyShopDiscount(ProfitableEndingUpgradeCosts[upgradeIndex]) : 0;
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

        public int ComboEngineLevel => data.comboEngineLevel;

        public int MaxComboEngineLevel => ComboEngineUpgradeCosts.Length;

        public bool IsMaxComboEngine => data.comboEngineLevel >= ComboEngineUpgradeCosts.Length;

        public long GetComboEngineUpgradeCost()
        {
            return IsMaxComboEngine ? 0 : ApplyShopDiscount(ComboEngineUpgradeCosts[data.comboEngineLevel]);
        }

        public bool BuyComboEngineUpgrade()
        {
            if (IsMaxComboEngine || !Spend(GetComboEngineUpgradeCost()))
            {
                return false;
            }

            data.comboEngineLevel++;
            return true;
        }

        public int SalvageProtocolLevel => data.salvageProtocolLevel;

        public int MaxSalvageProtocolLevel => SalvageProtocolUpgradeCosts.Length;

        public bool IsMaxSalvageProtocol => data.salvageProtocolLevel >= SalvageProtocolUpgradeCosts.Length;

        public long GetSalvageProtocolUpgradeCost()
        {
            return IsMaxSalvageProtocol ? 0 : ApplyShopDiscount(SalvageProtocolUpgradeCosts[data.salvageProtocolLevel]);
        }

        public bool BuySalvageProtocolUpgrade()
        {
            if (IsMaxSalvageProtocol || !Spend(GetSalvageProtocolUpgradeCost()))
            {
                return false;
            }

            data.salvageProtocolLevel++;
            return true;
        }

        public int TokenDividendLevel => data.tokenDividendLevel;

        public int MaxTokenDividendLevel => TokenDividendUpgradeCosts.Length;

        public bool IsMaxTokenDividend => data.tokenDividendLevel >= TokenDividendUpgradeCosts.Length;

        public long GetTokenDividendUpgradeCost()
        {
            return IsMaxTokenDividend ? 0 : ApplyShopDiscount(TokenDividendUpgradeCosts[data.tokenDividendLevel]);
        }

        public bool BuyTokenDividendUpgrade()
        {
            if (IsMaxTokenDividend || !Spend(GetTokenDividendUpgradeCost()))
            {
                return false;
            }

            data.tokenDividendLevel++;
            return true;
        }

        public long GetPassiveYieldUpgradeCost()
        {
            return IsMaxPassiveYield ? 0 : ApplyShopDiscount(PassiveYieldUpgradeCosts[data.passiveYieldLevel]);
        }

        public bool BuyPassiveYieldUpgrade()
        {
            if (IsMaxPassiveYield || !Spend(GetPassiveYieldUpgradeCost()))
            {
                return false;
            }

            data.passiveYieldLevel++;
            return true;
        }

        public long GetPassiveTickRateUpgradeCost()
        {
            return IsMaxPassiveTickRate ? 0 : ApplyShopDiscount(PassiveTickRateUpgradeCosts[data.passiveTickRateLevel]);
        }

        public bool BuyPassiveTickRateUpgrade()
        {
            if (!PassiveSupportUpgradesUnlocked || IsMaxPassiveTickRate || !Spend(GetPassiveTickRateUpgradeCost()))
            {
                return false;
            }

            data.passiveTickRateLevel++;
            return true;
        }

        public long GetActiveMultiplierUpgradeCost()
        {
            return IsMaxActiveMultiplier ? 0 : ApplyShopDiscount(ActiveMultiplierUpgradeCosts[data.activeMultiplierLevel]);
        }

        public bool BuyActiveMultiplierUpgrade()
        {
            if (!PassiveSupportUpgradesUnlocked || IsMaxActiveMultiplier || !Spend(GetActiveMultiplierUpgradeCost()))
            {
                return false;
            }

            data.activeMultiplierLevel++;
            return true;
        }

        // Advances the passive-production clock by `deltaSeconds` and grants any whole ticks that
        // elapsed. `isActivelyPlaying` applies the Active Multiplier bonus for this call only — it's
        // not stored, so it can vary frame to frame with no extra bookkeeping. Chips pass through the
        // same stage/income multipliers as every other income source, so late-game investment (Income
        // upgrade, Yield Theory research) keeps Passive Production relevant instead of it going stale.
        public long TickPassiveProduction(float deltaSeconds, bool isActivelyPlaying)
        {
            if (data.passiveYieldLevel <= 0 || IsMachineLearningTrainingActive)
            {
                return 0;
            }

            passiveProductionTimer += Math.Max(0f, deltaSeconds);
            float interval = GetPassiveTickInterval(data.passiveTickRateLevel);
            if (passiveProductionTimer < interval)
            {
                return 0;
            }

            int ticks = (int)(passiveProductionTimer / interval);
            passiveProductionTimer -= ticks * interval;

            double multiplier = isActivelyPlaying ? 1.0 + data.activeMultiplierLevel * ActiveMultiplierBonusPerLevel : 1.0;
            long perTick = Math.Max(1, (long)Math.Ceiling(GetPassiveYieldPerTick(data.passiveYieldLevel) * multiplier));
            long gained = perTick * ticks;

            gained = ApplyStageMultiplier(gained);
            gained = ApplyIncomeMultiplier(gained);
            data.chips += gained;
            data.totalChipsEarned += gained;
            data.lifetimeChipsEarned += gained;
            RecordIncome(IncomeSource.Passive, gained);
            Save();

            return gained;
        }

        // ------------------------------------------------------------------------------------
        // PPO Curriculum — two per-cycle upgrades that shave the Playing-mode frame REQUIREMENT
        // while the player is actively in PPO Training mode (see the design notes on the data fields).
        // ------------------------------------------------------------------------------------
        public int CurriculumAmountLevel => data.curriculumAmountLevel;
        public int MaxCurriculumAmountLevel => CurriculumAmountUpgradeCosts.Length;
        public bool IsMaxCurriculumAmount => data.curriculumAmountLevel >= CurriculumAmountUpgradeCosts.Length;

        public int CurriculumRateLevel => data.curriculumRateLevel;
        public int MaxCurriculumRateLevel => CurriculumRateUpgradeCosts.Length;
        public bool IsMaxCurriculumRate => data.curriculumRateLevel >= CurriculumRateUpgradeCosts.Length;

        /// <summary>The amount upgrade unlocks once PPO is owned; the rate upgrade only after amount L1.</summary>
        public bool CurriculumUnlocked => IsSolverUnlocked(SolverId.MachineLearning);
        public bool CurriculumRateUnlocked => data.curriculumAmountLevel >= 1;

        /// <summary>Frames shaved off the requirement per curriculum tick, at a given amount level.</summary>
        public static long GetCurriculumReductionPerTick(int level)
        {
            int clamped = Mathf.Clamp(level, 0, CurriculumFrameReductionPerTick.Length - 1);
            return CurriculumFrameReductionPerTick[clamped];
        }

        /// <summary>Seconds between curriculum ticks, at a given rate level.</summary>
        public static float GetCurriculumTickInterval(int level)
        {
            int clamped = Mathf.Clamp(level, 0, CurriculumTickIntervals.Length - 1);
            return CurriculumTickIntervals[clamped];
        }

        /// <summary>Frames-per-hour the requirement drops at the current levels (while training) — for UI.</summary>
        public long CurriculumReductionPerHour
        {
            get
            {
                float interval = GetCurriculumTickInterval(data.curriculumRateLevel);
                return interval <= 0f ? 0 : (long)Math.Round(3600f / interval) * GetCurriculumReductionPerTick(data.curriculumAmountLevel);
            }
        }

        public long GetCurriculumAmountUpgradeCost()
        {
            return IsMaxCurriculumAmount ? 0 : ApplyShopDiscount(CurriculumAmountUpgradeCosts[data.curriculumAmountLevel]);
        }

        public bool BuyCurriculumAmountUpgrade()
        {
            if (!CurriculumUnlocked || IsMaxCurriculumAmount || !Spend(GetCurriculumAmountUpgradeCost()))
            {
                return false;
            }

            data.curriculumAmountLevel++;
            return true;
        }

        public long GetCurriculumRateUpgradeCost()
        {
            return IsMaxCurriculumRate ? 0 : ApplyShopDiscount(CurriculumRateUpgradeCosts[data.curriculumRateLevel]);
        }

        public bool BuyCurriculumRateUpgrade()
        {
            if (!CurriculumRateUnlocked || IsMaxCurriculumRate || !Spend(GetCurriculumRateUpgradeCost()))
            {
                return false;
            }

            data.curriculumRateLevel++;
            return true;
        }

        /// <summary>
        /// Advances the curriculum clock. Only accrues while the player is in ACTIVE PPO Training mode
        /// — this is the anti-abuse guard (the requirement can't drop unless the PPO is actually
        /// training), so no floor/cap is needed: reduction and real frames climb together.
        /// </summary>
        public void TickCurriculum(float deltaSeconds)
        {
            if (data.curriculumAmountLevel <= 0 || !IsMachineLearningTrainingActive)
            {
                return;
            }

            curriculumTimer += Math.Max(0f, deltaSeconds);
            float interval = GetCurriculumTickInterval(data.curriculumRateLevel);
            if (interval <= 0f || curriculumTimer < interval)
            {
                return;
            }

            int ticks = (int)(curriculumTimer / interval);
            curriculumTimer -= ticks * interval;
            data.curriculumFrameReduction += GetCurriculumReductionPerTick(data.curriculumAmountLevel) * ticks;
            Save();
        }

        private long GetOwnedDatacenterRackTypeCount()
        {
            int owned = 0;
            foreach (DatacenterRackDefinition rack in DatacenterRacks)
            {
                if (GetDatacenterRackCount(rack.Id) >= 1)
                {
                    owned++;
                }
            }

            return owned;
        }

        /// <summary>
        /// Cosmetic block-numeral skins are goal rewards. The conditions mirror the goals exactly
        /// (lifetime metrics, so they never re-lock after a prestige).
        /// </summary>
        public bool IsBlockNumeralUnlocked(BlockNumeralStyle style)
        {
            return style switch
            {
                BlockNumeralStyle.Standard => true,
                BlockNumeralStyle.Byte => LifetimeChipsEarned >= 1_000_000_000,          // "Chip Billion"
                BlockNumeralStyle.Power => LifetimeChipsSpent >= 100_000_000,            // "Mega Budget"
                BlockNumeralStyle.Roman => LifetimeMoves >= 1_000_000,                   // "Move Singularity"
                BlockNumeralStyle.Hexadecimal => LifetimeMerges >= 1_000_000,            // "Merge Singularity"
                BlockNumeralStyle.Scientific => LifetimeHighestBlockEver >= 32768,       // "High 32768"
                _ => false
            };
        }

        // ------------------------------------------------------------------------------------
        // Auto Buy — reward of the "First Prestige" goal. Four independent per-menu toggles;
        // the bootstrap's ticker does the actual purchasing while a toggle is on.
        // ------------------------------------------------------------------------------------

        public bool AutoBuyUnlocked => PrestigeCount >= 1;

        public bool GetAutoBuyEnabled(AutoBuyCategory category)
        {
            return category switch
            {
                AutoBuyCategory.Algorithms => data.autoBuyAlgorithms,
                AutoBuyCategory.Upgrades => data.autoBuyUpgrades,
                AutoBuyCategory.Agents => data.autoBuyAgents,
                AutoBuyCategory.Modifiers => data.autoBuyModifiers,
                _ => false
            };
        }

        public void SetAutoBuyEnabled(AutoBuyCategory category, bool enabled)
        {
            if (!AutoBuyUnlocked)
            {
                return;
            }

            switch (category)
            {
                case AutoBuyCategory.Algorithms:
                    data.autoBuyAlgorithms = enabled;
                    break;
                case AutoBuyCategory.Upgrades:
                    data.autoBuyUpgrades = enabled;
                    break;
                case AutoBuyCategory.Agents:
                    data.autoBuyAgents = enabled;
                    break;
                case AutoBuyCategory.Modifiers:
                    data.autoBuyModifiers = enabled;
                    break;
            }

            Save();
        }

        // ------------------------------------------------------------------------------------
        // Datacenter layer
        // ------------------------------------------------------------------------------------

        /// <summary>
        /// Testing override — the bootstrap sets this from its Inspector toggle (Editor only) so
        /// the Datacenter can be exercised without grinding five prestiges.
        /// </summary>
        public static bool DebugUnlockDatacenter;

        public bool DatacenterUnlocked => DebugUnlockDatacenter || PrestigeCount >= DatacenterUnlockPrestigeCount;

        public int GetDatacenterRackCount(DatacenterRackId rackId)
        {
            int index = (int)rackId;
            return index >= 0 && index < data.datacenterRackCounts.Length ? Math.Max(0, data.datacenterRackCounts[index]) : 0;
        }

        public int TotalDatacenterRackUnits
        {
            get
            {
                int total = 0;
                foreach (DatacenterRackDefinition rack in DatacenterRacks)
                {
                    total += GetDatacenterRackCount(rack.Id);
                }

                return total;
            }
        }

        public long GetDatacenterRackCost(DatacenterRackId rackId)
        {
            DatacenterRackDefinition definition = DatacenterRacks[(int)rackId];
            double cost = definition.BaseCost * Math.Pow(definition.CostGrowth, GetDatacenterRackCount(rackId));
            // No Bulk Discount here: racks are the endless late-game chip sink and must stay one.
            return cost >= long.MaxValue / 4d ? long.MaxValue / 4 : (long)Math.Round(cost);
        }

        public bool BuyDatacenterRack(DatacenterRackId rackId)
        {
            int index = (int)rackId;
            if (!DatacenterUnlocked || index < 0 || index >= data.datacenterRackCounts.Length)
            {
                return false;
            }

            if (!Spend(GetDatacenterRackCost(rackId)))
            {
                return false;
            }

            data.datacenterRackCounts[index]++;
            return true;
        }

        public int GetDatacenterFacilityLevel(DatacenterFacilityId facilityId)
        {
            int index = (int)facilityId;
            return index >= 0 && index < data.datacenterFacilityLevels.Length ? Math.Max(0, data.datacenterFacilityLevels[index]) : 0;
        }

        public bool IsDatacenterFacilityMaxed(DatacenterFacilityId facilityId)
        {
            return GetDatacenterFacilityLevel(facilityId) >= DatacenterFacilities[(int)facilityId].MaxLevel;
        }

        public long GetDatacenterFacilityCost(DatacenterFacilityId facilityId)
        {
            if (IsDatacenterFacilityMaxed(facilityId))
            {
                return 0;
            }

            DatacenterFacilityDefinition definition = DatacenterFacilities[(int)facilityId];
            double cost = definition.BaseCost * Math.Pow(definition.CostGrowth, GetDatacenterFacilityLevel(facilityId));
            return cost >= long.MaxValue / 4d ? long.MaxValue / 4 : (long)Math.Round(cost);
        }

        public bool BuyDatacenterFacility(DatacenterFacilityId facilityId)
        {
            int index = (int)facilityId;
            if (!DatacenterUnlocked || index < 0 || index >= data.datacenterFacilityLevels.Length || IsDatacenterFacilityMaxed(facilityId))
            {
                return false;
            }

            if (!Spend(GetDatacenterFacilityCost(facilityId)))
            {
                return false;
            }

            data.datacenterFacilityLevels[index]++;
            return true;
        }

        /// <summary>Effective GF/s of one unit of a rack, with facility and prestige bonuses applied.</summary>
        public double GetDatacenterRackUnitGigaflops(DatacenterRackId rackId)
        {
            DatacenterRackDefinition definition = DatacenterRacks[(int)rackId];
            double facilityBonus = 1.0
                + GetDatacenterFacilityLevel(DatacenterFacilityId.PowerGrid) * PowerGridFlopsBonusPerLevel
                + GetDatacenterFacilityLevel(DatacenterFacilityId.CoolingLoop) * CoolingLoopFlopsBonusPerLevel;
            double interconnect = rackId == DatacenterRackId.TpuPod || rackId == DatacenterRackId.NeuralFabric
                ? 1.0 + GetDatacenterFacilityLevel(DatacenterFacilityId.FabricInterconnect) * FabricInterconnectBonusPerLevel
                : 1.0;
            double prestigeBonus = 1.0 + Math.Max(0, PrestigeCount) * DatacenterPrestigeBonusPerPrestige;
            return definition.BaseGigaflops * facilityBonus * interconnect * prestigeBonus;
        }

        /// <summary>Total datacenter output in GF/s.</summary>
        public double DatacenterTotalGigaflops
        {
            get
            {
                double total = 0;
                foreach (DatacenterRackDefinition rack in DatacenterRacks)
                {
                    total += GetDatacenterRackCount(rack.Id) * GetDatacenterRackUnitGigaflops(rack.Id);
                }

                return total;
            }
        }

        public float GetDatacenterAllocation(DatacenterAllocationId allocationId)
        {
            int index = (int)allocationId;
            return index >= 0 && index < data.datacenterAllocation.Length ? Mathf.Clamp01(data.datacenterAllocation[index]) : 0f;
        }

        public float DatacenterAllocatedFraction =>
            Mathf.Clamp01(GetDatacenterAllocation(DatacenterAllocationId.TrainingCluster)
                + GetDatacenterAllocation(DatacenterAllocationId.AnalysisNode)
                + GetDatacenterAllocation(DatacenterAllocationId.MarketBots));

        /// <summary>
        /// Sets one allocation share (0..1). The share is clamped so the three together never
        /// exceed 100% — the caller's slider snaps back to the granted value on refresh.
        /// </summary>
        public void SetDatacenterAllocation(DatacenterAllocationId allocationId, float fraction)
        {
            int index = (int)allocationId;
            if (index < 0 || index >= data.datacenterAllocation.Length)
            {
                return;
            }

            float othersTotal = 0f;
            for (int i = 0; i < data.datacenterAllocation.Length; i++)
            {
                if (i != index)
                {
                    othersTotal += Mathf.Clamp01(data.datacenterAllocation[i]);
                }
            }

            data.datacenterAllocation[index] = Mathf.Clamp(fraction, 0f, Mathf.Max(0f, 1f - othersTotal));
            Save();
        }

        /// <summary>
        /// True while background training frames have somewhere to go this cycle. Deliberately does
        /// NOT require PPO to be bought yet: the datacenter pre-trains the network for the whole
        /// cycle, so by the time PPO is purchased a chunk of the Training requirement is done.
        /// Without this the accrual window was only the short stretch between buying PPO and
        /// finishing Training, which made the Training Cluster nearly worthless.
        /// </summary>
        public bool DatacenterTrainingHasTarget => !MachineLearningPlayingModeUnlocked;

        public double DatacenterTrainingFramesPerSecond =>
            DatacenterTotalGigaflops * GetDatacenterAllocation(DatacenterAllocationId.TrainingCluster) * DatacenterFramesPerSecondPerGigaflop;

        /// <summary>Insight/sec of the Analysis Node AFTER the per-prestige softcap fatigue.</summary>
        public double DatacenterInsightPerSecond
        {
            get
            {
                if (data.prestigeCount <= 0)
                {
                    return 0.0;
                }

                double raw = DatacenterTotalGigaflops * GetDatacenterAllocation(DatacenterAllocationId.AnalysisNode) * DatacenterInsightPerSecondPerGigaflop;
                if (raw <= 0.0)
                {
                    return 0.0;
                }

                // Same fatigue curve as Passive Insight, so a huge farm can't out-earn prestiging.
                double fatigue = 1.0 / Math.Sqrt(1.0 + Math.Max(0.0, data.researchInsightEarnedThisPrestige) / GetInsightCycleSoftCap());
                return raw * fatigue;
            }
        }

        public double DatacenterMarketMultiplier
        {
            get
            {
                if (!DatacenterUnlocked)
                {
                    return 1.0;
                }

                double allocated = DatacenterTotalGigaflops * GetDatacenterAllocation(DatacenterAllocationId.MarketBots);
                return allocated <= 0.0 ? 1.0 : 1.0 + DatacenterMarketLogFactor * Math.Log10(1.0 + allocated);
            }
        }

        /// <summary>
        /// Advances datacenter production by `deltaSeconds`. Training frames only accrue while PPO
        /// is owned this cycle and Training is still incomplete; Analysis Insight accrues whenever
        /// the layer is unlocked. Fractions carry over between calls.
        /// </summary>
        public void TickDatacenter(float deltaSeconds)
        {
            if (!DatacenterUnlocked || deltaSeconds <= 0f)
            {
                return;
            }

            bool granted = false;
            if (DatacenterTrainingHasTarget)
            {
                datacenterFrameCarry += DatacenterTrainingFramesPerSecond * deltaSeconds;
                int wholeFrames = (int)Math.Min(int.MaxValue / 2d, Math.Floor(datacenterFrameCarry));
                if (wholeFrames > 0)
                {
                    datacenterFrameCarry -= wholeFrames;
                    ApplyBackgroundTrainingFrames(wholeFrames);
                    granted = true;
                }
            }
            else
            {
                datacenterFrameCarry = 0.0;
            }

            double insightPerSecond = DatacenterInsightPerSecond;
            if (insightPerSecond > 0.0)
            {
                datacenterInsightCarry += insightPerSecond * deltaSeconds;
                long wholeInsight = (long)Math.Floor(datacenterInsightCarry);
                if (wholeInsight > 0)
                {
                    datacenterInsightCarry -= wholeInsight;
                    AddResearchInsight(wholeInsight, true);
                    granted = true;
                }
            }

            if (granted)
            {
                Save();
            }
        }

        // Background frames are an abstraction: the policy's step counter advances (unlocking
        // Normal mode / feeding PPO Memory) without literally replaying moves, exactly like the
        // editor simulator's injected progress.
        private void ApplyBackgroundTrainingFrames(int frames)
        {
            if (frames <= 0 || machineLearningAgent == null)
            {
                return;
            }

            StackMergePpoTrainingData policy = machineLearningAgent.Data;
            if (policy == null)
            {
                return;
            }

            policy.steps = (int)Math.Min(int.MaxValue - 1L, Math.Max(0, policy.steps) + (long)frames);
            data.machineLearningExperience = Math.Max(data.machineLearningExperience, policy.steps);
            data.machineLearningPolicy = policy;
            CaptureMachineLearningMemoryIfEligible();
        }

        public long GetAgentsMenuUnlockCost()
        {
            return data.agentsMenuUnlocked ? 0 : ApplyShopDiscount(AgentsMenuUnlockCost);
        }

        public bool BuyAgentsMenuUnlock()
        {
            if (data.agentsMenuUnlocked)
            {
                return true;
            }

            if (!Spend(GetAgentsMenuUnlockCost()))
            {
                return false;
            }

            data.agentsMenuUnlocked = true;
            data.lifetimeAgentsUnlocked = true;
            return true;
        }

        public long GetExtraAgentSlotUpgradeCost()
        {
            return data.extraAgentSlotUnlocked ? 0 : ApplyShopDiscount(ExtraAgentSlotUpgradeCost);
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

            if (!Spend(GetExtraAgentSlotUpgradeCost()))
            {
                return false;
            }

            data.extraAgentSlotUnlocked = true;
            return true;
        }

        public long GetModifiersMenuUnlockCost()
        {
            return data.modifiersMenuUnlocked ? 0 : ApplyShopDiscount(ModifiersMenuUnlockCost);
        }

        public string GetModifiersGateStatus()
        {
            if (data.modifiersMenuUnlocked)
            {
                return "Modifier Lab unlocked";
            }

            return $"Requires: Agents {FormatGate(data.agentsMenuUnlocked)}, All agents {FormatGate(AllAgentsUnlocked)}, Solvers {UnlockedSolverCount}/{ModifierGateSolvers}, Runs {data.runsCompleted}/{GatedModifierRuns}, Merges {data.totalMerges}/{GatedModifierMerges}, Best {data.bestRunScore}/{ModifierGateBestScore}, High {data.highestBlockEver}/{ModifierGateHighestBlock}";
        }

        public bool BuyModifiersMenuUnlock()
        {
            if (data.modifiersMenuUnlocked)
            {
                return true;
            }

            if (!CanUnlockModifiersMenu || !Spend(GetModifiersMenuUnlockCost()))
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
            return level >= definition.MaxLevel ? 0 : ApplyShopDiscount(definition.Costs[level]);
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

        /// <summary>True once every agent has been hired — a Modifiers-menu gate condition so the
        /// player can't skip agent engagement (and can't bank idle toward modifiers unspent).</summary>
        public bool AllAgentsUnlocked
        {
            get
            {
                for (int i = 0; i < Agents.Length; i++)
                {
                    if (data.agentUnlocked == null || i >= data.agentUnlocked.Length || !data.agentUnlocked[i])
                    {
                        return false;
                    }
                }

                return true;
            }
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

        /// <summary>Hire price of an Agent with the Bulk Discount research applied (0 once hired).</summary>
        public long GetAgentCost(AgentId agentId)
        {
            int index = (int)agentId;
            if (index < 0 || index >= Agents.Length || IsAgentUnlocked(agentId))
            {
                return 0;
            }

            return ApplyShopDiscount(Agents[index].Cost);
        }

        public bool BuyAgent(AgentId agentId)
        {
            int index = (int)agentId;
            if (!data.agentsMenuUnlocked || index < 0 || index >= Agents.Length || IsAgentUnlocked(agentId))
            {
                return false;
            }

            if (!Spend(GetAgentCost(agentId)))
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
                if (!Spend(GetAgentCost(agentId)))
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

            MoveIncomeBreakdown breakdown = CalculateMoveIncomeBreakdown(result, true);
            long gained = breakdown.PreGlobalIncome;
            if (gained > 0 && !suppressChips)
            {
                gained = ApplyStageMultiplier(gained);
                gained = ApplyIncomeMultiplier(gained);
                data.chips += gained;
                data.totalChipsEarned += gained;
                data.lifetimeChipsEarned += gained;
                RecordMoveIncome(gained, breakdown.PlacementComponent, breakdown.MergeComponent, breakdown.HighComponent, breakdown.ComboMultiplier);
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

        /// <summary>Current Combo Engine streak (consecutive merging moves this run) — for UI.</summary>
        public int CurrentComboStreak => comboStreak;

        /// <summary>Chips credited to one income source since load (runtime-only, not persisted).</summary>
        public long GetIncomeBySource(IncomeSource source)
        {
            int index = (int)source;
            return index >= 0 && index < incomeLedger.Length ? incomeLedger[index] : 0;
        }

        /// <summary>Snapshot of the whole income ledger — the balance simulator diffs these per cycle.</summary>
        public long[] CopyIncomeLedger()
        {
            return (long[])incomeLedger.Clone();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Simulator replay helper: captures the per-move payout before global economy multipliers,
        /// without advancing Combo Engine streak. AwardMove immediately after this will use the same
        /// input and advance the real state.
        /// </summary>
        public long CalculateReplayMoveIncomeBeforeGlobal(MoveResult result)
        {
            return CalculateMoveIncomeBreakdown(result, false).PreGlobalIncome;
        }
#endif

        private struct MoveIncomeBreakdown
        {
            public long PlacementComponent;
            public long MergeComponent;
            public long HighComponent;
            public double ComboMultiplier;
            public long PreGlobalIncome;
        }

        private MoveIncomeBreakdown CalculateMoveIncomeBreakdown(MoveResult result, bool advanceComboStreak)
        {
            if (!result.Accepted)
            {
                return default;
            }

            double highestMultiplier = GetHighestBlockRewardMultiplier(Math.Max(result.HighestBlock, result.ResultingTopValue));
            long placement = result.ActionKind == SolverActionKind.Place ? 2 + AgentFlatPlacementBonus : 0;
            double merge = result.MergeCount <= 0
                ? 0
                : result.MergeCount * Math.Max(1, result.ResultingTopValue) * highestMultiplier * 0.55;
            long highest = result.MergeCount > 0 && result.ResultingTopValue >= result.HighestBlock
                ? (long)Math.Ceiling(Math.Max(1, result.HighestBlock) * highestMultiplier * 0.85)
                : 0;

            int nextComboStreak = result.MergeCount > 0 ? comboStreak + 1 : 0;
            if (advanceComboStreak)
            {
                comboStreak = nextComboStreak;
            }

            double comboMultiplier = 1.0 + Math.Min(nextComboStreak, ComboStreakCap) * data.comboEngineLevel * ComboBonusPerStreakPerLevel;
            long mergeComponent = (long)Math.Ceiling(merge * AgentMergeMultiplier * ModifierMergeMultiplier);
            long highComponent = (long)Math.Ceiling(highest * AgentHighestMultiplier);
            long preGlobalIncome = placement + mergeComponent + highComponent;
            preGlobalIncome = (long)Math.Ceiling(preGlobalIncome * comboMultiplier * IncomeScale);

            return new MoveIncomeBreakdown
            {
                PlacementComponent = placement,
                MergeComponent = mergeComponent,
                HighComponent = highComponent,
                ComboMultiplier = comboMultiplier,
                PreGlobalIncome = preGlobalIncome
            };
        }

        private void RecordIncome(IncomeSource source, long amount)
        {
            if (amount > 0)
            {
                incomeLedger[(int)source] += amount;
            }
        }

        // Splits a credited move payout across its components. The combo share is the extra the
        // multiplier produced ((m-1)/m of the total); the remainder is split proportionally to the
        // pre-multiplier components so the ledger still sums exactly to the credited amount.
        private void RecordMoveIncome(long credited, long placementComponent, long mergeComponent, long highComponent, double comboMultiplier)
        {
            long comboShare = comboMultiplier > 1.0
                ? (long)Math.Round(credited * (comboMultiplier - 1.0) / comboMultiplier)
                : 0;
            long rest = credited - comboShare;
            long componentTotal = Math.Max(1, placementComponent + mergeComponent + highComponent);
            long placementShare = (long)Math.Round(rest * (double)placementComponent / componentTotal);
            long highShare = (long)Math.Round(rest * (double)highComponent / componentTotal);
            long mergeShare = rest - placementShare - highShare;
            RecordIncome(IncomeSource.Placement, placementShare);
            RecordIncome(IncomeSource.Merge, mergeShare);
            RecordIncome(IncomeSource.NewHigh, highShare);
            RecordIncome(IncomeSource.Combo, comboShare);
        }

        /// <summary>
        /// Salvage Protocol: converts a share of the run's final score into chips at game over.
        /// (Score-based since the balance rework — the stranded-board basis was too small to matter.)
        /// </summary>
        public long AwardSalvage(long runScore, bool suppressChips = false)
        {
            if (data.salvageProtocolLevel <= 0 || runScore <= 0 || suppressChips)
            {
                return 0;
            }

            long gained = (long)Math.Ceiling(runScore * data.salvageProtocolLevel * SalvageSharePerLevel * IncomeScale);
            if (gained <= 0)
            {
                return 0;
            }

            gained = ApplyStageMultiplier(gained);
            gained = ApplyIncomeMultiplier(gained);
            data.chips += gained;
            data.totalChipsEarned += gained;
            data.lifetimeChipsEarned += gained;
            RecordIncome(IncomeSource.Salvage, gained);
            return gained;
        }

        public long AwardRunCompleted(long runScore, SolverId solverId, int moves, int merges, int highestMergedBlock, bool manualRun, float elapsedSeconds, bool suppressChips = false)
        {
            comboStreak = 0;
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
            // Move Dividend & Velocity Trader tripled (2026-07-12): they read as near-useless in playtest.
            double moveBonus = IsAgentActive(AgentId.MoveDividend)
                ? Math.Max(0, moves) * Math.Max(1.0, highestMultiplier * 0.35) * 12.0 * AgentSynergyMultiplier
                : 0;
            double speedBonus = IsAgentActive(AgentId.VelocityTrader) && elapsedSeconds > 0.01f
                ? scoreBonus * Math.Min(7.5, Math.Max(0.0, (moves / elapsedSeconds) - 1.0) * 0.54) * AgentSynergyMultiplier
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
                RecordIncome(IncomeSource.RunBonus, bonus);
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
                AchievementMetric.DatacenterRackTypesOwned => GetOwnedDatacenterRackTypeCount(),
                AchievementMetric.DatacenterGigaflops => (long)DatacenterTotalGigaflops,
                AchievementMetric.LifetimeTokensConsumed => data.lifetimeTokensConsumed,
                AchievementMetric.TokensHeld => data.tokens,
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

        private double AgentMergeMultiplier => IsAgentActive(AgentId.MergeBroker) ? 1.0 + 0.75 * AgentSynergyMultiplier : 1.0;

        private double AgentHighestMultiplier => IsAgentActive(AgentId.HighwaterAnalyst) ? 1.0 + 2.00 * AgentSynergyMultiplier : 1.0;

        private double AgentScoreMultiplier => IsAgentActive(AgentId.ScoreAuditor) ? 1.0 + 1.00 * AgentSynergyMultiplier : 1.0;

        private float AgentMoveIntervalMultiplier => IsAgentActive(AgentId.Overclocker) ? 0.75f : 1f;

        private int AgentFlatPlacementBonus => IsAgentActive(AgentId.Quartermaster) ? (int)Math.Round(10 * AgentSynergyMultiplier) : 0;

        private double ModifierMergeMultiplier => GetModifierLevel(ModifierId.CatalystStack) > 0 ? 2.0 : 1.0;

        private double GetPrestigeInsightMultiplier()
        {
            return 1.0 + GetResearchLevel(ResearchId.InsightAmplifier) * 0.35;
        }

        private double GetInsightExtractorMultiplier()
        {
            return 1.0 + GetResearchLevel(ResearchId.InsightExtractor) * 0.50;
        }

        private double GetResearchIncomeMultiplier()
        {
            return 1.0 + GetResearchLevel(ResearchId.YieldTheory) * 0.30;
        }

        /// <summary>Agent Synergy research: scales the bonus part of every hired Agent's effect.</summary>
        private double AgentSynergyMultiplier => 1.0 + GetResearchLevel(ResearchId.AgentSynergy) * 0.25;

        /// <summary>Bulk Discount research: price multiplier for Upgrades, Agents and Modifiers (not solvers or token packs). -5%/level, -25% at max.</summary>
        private double ShopDiscountMultiplier => Math.Max(0.75, 1.0 - GetResearchLevel(ResearchId.BulkDiscount) * 0.05);

        /// <summary>Bulk Discount also eases the Modifiers gate's Runs/Merges requirements: -15%/level, -75% at max.</summary>
        private double ModifierGateRequirementMultiplier => Math.Max(0.25, 1.0 - GetResearchLevel(ResearchId.BulkDiscount) * 0.15);

        private int GatedModifierRuns => (int)Math.Ceiling(ModifierGateRuns * ModifierGateRequirementMultiplier);

        private int GatedModifierMerges => (int)Math.Ceiling(ModifierGateMerges * ModifierGateRequirementMultiplier);

        /// <summary>
        /// Seconds of the artificial "Evaluating…" pause after each PPO Training run (see
        /// TickAutomation in the bootstrap). Evaluation Efficiency research trims it 15%/level.
        /// </summary>
        public float MachineLearningEvaluationSeconds =>
            BaseTrainingEvaluationSeconds * Mathf.Max(0.25f, 1f - GetResearchLevel(ResearchId.EvaluationEfficiency) * 0.15f);

        private long ApplyShopDiscount(long cost)
        {
            return cost <= 0 ? cost : Math.Max(1, (long)Math.Round(cost * ShopDiscountMultiplier));
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

        /// <summary>PPO Memory research: how many training frames survive a prestige (50k per level).</summary>
        public static long GetPpoMemoryFrameAllowance(int level)
        {
            return Math.Max(0, level) * 50000L;
        }

        private long GetPpoMemoryFrameAllowance()
        {
            return GetPpoMemoryFrameAllowance(GetResearchLevel(ResearchId.PpoMemory));
        }

        private static long GetPrestigeStartChips(int seedCapitalLevel)
        {
            // A head start, not a skip: L1 covers the first solver + Auto Solve + a bit of shopping,
            // L5 is still well short of the whole Agents/Modifiers economy, so every cycle is
            // actually played. The big cycle-time cuts must come from the multiplicative researches
            // (Yield Theory, Bulk Discount, Agent Synergy), which speed the whole cycle up evenly
            // instead of deleting its opening.
            return seedCapitalLevel switch
            {
                <= 0 => 0,
                1 => 100000,
                2 => 200000,
                3 => 350000,
                4 => 500000,
                _ => 1000000
            };
        }

        private bool IsResearchPrerequisiteMet(ResearchId researchId, out string reason)
        {
            reason = string.Empty;
            // Strictly downward within a column, never sideways. Seed Capital is the root: its L1
            // opens the top of all three columns; from there each node only needs the node above it.
            switch (researchId)
            {
                // Tier 1 — one entry point per column, all gated only on the root.
                case ResearchId.AutomationMemory:
                case ResearchId.PpoBootcamp:
                case ResearchId.InsightAmplifier:
                    if (GetResearchLevel(ResearchId.SeedCapital) < 1)
                    {
                        reason = "Requires Seed Capital L1.";
                        return false;
                    }

                    return true;
                // Left column: Automation Memory → Algorithm Archive → Agent Synergy → Bulk Discount → Yield Theory.
                case ResearchId.AlgorithmArchive:
                    if (GetResearchLevel(ResearchId.AutomationMemory) < 1)
                    {
                        reason = "Requires Automation Memory L1.";
                        return false;
                    }

                    return true;
                case ResearchId.AgentSynergy:
                    if (GetResearchLevel(ResearchId.AlgorithmArchive) < 1)
                    {
                        reason = "Requires Algorithm Archive L1.";
                        return false;
                    }

                    return true;
                case ResearchId.BulkDiscount:
                    if (GetResearchLevel(ResearchId.AgentSynergy) < 1)
                    {
                        reason = "Requires Agent Synergy L1.";
                        return false;
                    }

                    return true;
                case ResearchId.YieldTheory:
                    if (GetResearchLevel(ResearchId.BulkDiscount) < 1)
                    {
                        reason = "Requires Bulk Discount L1.";
                        return false;
                    }

                    return true;
                // Middle column: PPO Bootcamp → Evaluation Efficiency → PPO Memory → High Focus → Stability → Insight Extractor.
                case ResearchId.EvaluationEfficiency:
                    if (GetResearchLevel(ResearchId.PpoBootcamp) < 1)
                    {
                        reason = "Requires PPO Bootcamp L1.";
                        return false;
                    }

                    return true;
                case ResearchId.PpoMemory:
                    if (GetResearchLevel(ResearchId.EvaluationEfficiency) < 1)
                    {
                        reason = "Requires Evaluation Efficiency L1.";
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
                    if (GetResearchLevel(ResearchId.PpoStability) < 1)
                    {
                        reason = "Requires Stability Model L1.";
                        return false;
                    }

                    return true;
                // Right column: Insight Amplifier → Passive Insight → Offline Engine → Offline Buffer.
                case ResearchId.PassiveInsight:
                    if (GetResearchLevel(ResearchId.InsightAmplifier) < 1)
                    {
                        reason = "Requires Insight Amplifier L1.";
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
                    // Seed Capital (the root) has no prerequisite.
                    return true;
            }
        }

        private void ApplyImmediateResearchEffects(ResearchId researchId)
        {
            // Yield / prestige multiplier research is read dynamically. Prestige-start research should
            // also feel good if bought mid-prestige, so it applies to the current run layer too.
            // Seed Capital MUST be here: bought right after the first prestige (the only affordable
            // node), it top-ups chips to the new level's start amount immediately — otherwise the
            // grant only ran at reset (level 0 = 0 chips) and the player got nothing for their Insight.
            if (researchId == ResearchId.SeedCapital
                || researchId == ResearchId.AutomationMemory
                || researchId == ResearchId.AlgorithmArchive)
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
            UnlockStarterSolver(SolverId.Balance, algorithmArchiveLevel >= 1);
            UnlockStarterSolver(SolverId.Heur, algorithmArchiveLevel >= 2);
            UnlockStarterSolver(SolverId.Combo, algorithmArchiveLevel >= 3);
            UnlockStarterSolver(SolverId.Look, algorithmArchiveLevel >= 4);

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
            data.computeSpeedLevel = 0;
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
            data.passiveYieldLevel = 0;
            data.passiveTickRateLevel = 0;
            data.activeMultiplierLevel = 0;
            data.curriculumAmountLevel = 0;
            data.curriculumRateLevel = 0;
            data.curriculumFrameReduction = 0;
            data.comboEngineLevel = 0;
            data.salvageProtocolLevel = 0;
            data.tokenDividendLevel = 0;
            comboStreak = 0;
            passiveProductionTimer = 0f;
            curriculumTimer = 0f;
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

        // PPO Memory is frames-based: each research level banks 50k training frames. The snapshot is
        // refreshed while training progresses and freezes once the allowance is reached, so the next
        // prestige restores the network roughly as it was at `allowance` frames — PPO keeps its
        // knowledge and the Training requirement starts partially filled instead of at zero.
        private void CaptureMachineLearningMemoryIfEligible()
        {
            long allowance = GetPpoMemoryFrameAllowance();
            if (allowance <= 0 || machineLearningAgent == null)
            {
                return;
            }

            long frames = Math.Min(MachineLearningFrames, allowance);
            if (frames <= 0)
            {
                return;
            }

            if (data.machineLearningPrestigeMemoryPolicy != null
                && data.machineLearningPrestigeMemoryFrames >= frames
                && data.machineLearningPrestigeMemoryFrames >= Math.Min(allowance, MachineLearningFrames))
            {
                return;
            }

            data.machineLearningPrestigeMemoryPolicy = StackMergePpoAgent.CloneData(machineLearningAgent.Data);
            data.machineLearningPrestigeMemoryFrames = frames;
        }

        private void ApplyMachineLearningMemoryAfterReset()
        {
            long allowance = GetPpoMemoryFrameAllowance();
            if (allowance <= 0 || data.machineLearningPrestigeMemoryPolicy == null || machineLearningAgent == null)
            {
                return;
            }

            long retainedFrames = Math.Min(Math.Max(0, data.machineLearningPrestigeMemoryFrames), allowance);
            if (retainedFrames <= 0)
            {
                return;
            }

            StackMergePpoTrainingData snapshot = StackMergePpoAgent.CloneData(data.machineLearningPrestigeMemoryPolicy);
            snapshot.steps = (int)Math.Min(Math.Max(0, snapshot.steps), retainedFrames);
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

            LastOfflineHours = cappedHours;
            long offlineChips = ComputeOfflineChips(cappedHours, efficiency);
            long offlineInsight = ComputeOfflineInsight(cappedHours, efficiency);
            if (offlineChips > 0)
            {
                data.chips += offlineChips;
                data.totalChipsEarned += offlineChips;
                data.lifetimeChipsEarned += offlineChips;
                data.lastOfflineChips = offlineChips;
                RecordIncome(IncomeSource.Offline, offlineChips);
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

        /// <summary>Token Dividend: held tokens pay chip income, scaling with √(hoard size), no cap.</summary>
        public double TokenDividendMultiplier =>
            1.0 + Math.Sqrt(Math.Max(0, Tokens)) * data.tokenDividendLevel * TokenDividendPerSqrtTokenPerLevel;

        /// <summary>
        /// Product of every GLOBAL chip-income multiplier (stage × Chip Yield × Yield Theory ×
        /// datacenter market × token dividend). The simulator's run cache divides income by this at
        /// capture time and multiplies by the current value at replay, so cached runs stay exact
        /// while the global economy moves on.
        /// </summary>
        public double CurrentGlobalIncomeMultiplier =>
            (data.modifiersMenuUnlocked ? 24.0 : data.agentsMenuUnlocked ? 5.0 : 1.0)
            * (1.0 + data.incomeLevel * IncomeBonusPerLevel)
            * GetResearchIncomeMultiplier()
            * DatacenterMarketMultiplier
            * TokenDividendMultiplier;

#if UNITY_EDITOR
        /// <summary>
        /// Simulator-only bulk twin of the per-move AwardMove effects for a replayed run (see the
        /// simulator's run cache): re-applies every captured pre-global move payout through the
        /// current global economy multipliers, advances the same lifetime counters and distributes
        /// the income ledger in the same proportions the cached real run produced.
        /// </summary>
        public long AwardReplayRun(int moves, int merges, int highestBlock, long[] moveIncomeBeforeGlobal, long[] ledgerShare, int unstableSaves = 0, int jokerMerges = 0)
        {
            long gained = 0;
            if (moveIncomeBeforeGlobal != null)
            {
                foreach (long baseIncome in moveIncomeBeforeGlobal)
                {
                    if (baseIncome <= 0)
                    {
                        continue;
                    }

                    gained += ApplyIncomeMultiplier(ApplyStageMultiplier(baseIncome));
                }
            }

            if (gained > 0)
            {
                data.chips += gained;
                data.totalChipsEarned += gained;
                data.lifetimeChipsEarned += gained;

                long shareTotal = 0;
                if (ledgerShare != null)
                {
                    foreach (long share in ledgerShare)
                    {
                        shareTotal += Math.Max(0, share);
                    }
                }

                if (shareTotal > 0)
                {
                    long distributed = 0;
                    for (int i = 0; i < ledgerShare.Length && i < incomeLedger.Length; i++)
                    {
                        long slice = (long)Math.Round(gained * (double)Math.Max(0, ledgerShare[i]) / shareTotal);
                        incomeLedger[i] += slice;
                        distributed += slice;
                    }

                    incomeLedger[(int)IncomeSource.Merge] += gained - distributed; // rounding remainder
                }
                else
                {
                    RecordIncome(IncomeSource.Merge, gained);
                }
            }

            data.totalBlocksDropped += Math.Max(0, moves);
            data.lifetimeMoves += Math.Max(0, moves);
            data.totalMerges += Math.Max(0, merges);
            data.lifetimeMerges += Math.Max(0, merges);
            data.highestBlockEver = Math.Max(data.highestBlockEver, highestBlock);
            data.lifetimeHighestBlockEver = Math.Max(data.lifetimeHighestBlockEver, highestBlock);
            data.lifetimeUnstableSaves += Math.Max(0, unstableSaves);
            data.lifetimeJokerMerges += Math.Max(0, jokerMerges);
            AwardMergeTokens(Math.Max(0, merges));
            return gained;
        }
#endif

        private long ApplyIncomeMultiplier(long amount)
        {
            return Math.Max(1, (long)Math.Ceiling(amount * (1.0 + data.incomeLevel * IncomeBonusPerLevel) * GetResearchIncomeMultiplier() * DatacenterMarketMultiplier * TokenDividendMultiplier));
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
                // MCTS's real compute is light for a heavy solver (measured ~0.29 ms/move, faster
                // than MOCA and the PLANs), so it earns a fast pacing instead of the slowest. This
                // is its identity: the expensive-to-unlock but efficient heavy solver, whose neutral
                // score is mediocre-by-design and pays off under tuning (Safety cushion / Combo setup).
                SolverId.Mcts => 0.55f,
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
            data.computeSpeedLevel = Mathf.Clamp(data.computeSpeedLevel, 0, ComputeSpeedUpgradeCosts.Length);
            data.stackCapacityLevel = Mathf.Clamp(data.stackCapacityLevel, 0, StackMergeGameState.MaxStackCapacity - StackMergeGameState.DefaultStackCapacity);
            data.queuePreviewLevel = Mathf.Clamp(data.queuePreviewLevel, 0, QueuePreviewUpgradeCosts.Length);
            data.incomeLevel = Mathf.Clamp(data.incomeLevel, 0, IncomeUpgradeCosts.Length);
            data.difficultyLevel = Mathf.Clamp(data.difficultyLevel, 0, DifficultyUpgradeCosts.Length);
            data.scalingFrequencyLevel = Mathf.Clamp(data.scalingFrequencyLevel, 0, ScalingFrequencyUpgradeCosts.Length);
            data.comboEngineLevel = Mathf.Clamp(data.comboEngineLevel, 0, ComboEngineUpgradeCosts.Length);
            data.salvageProtocolLevel = Mathf.Clamp(data.salvageProtocolLevel, 0, SalvageProtocolUpgradeCosts.Length);
            data.tokenDividendLevel = Mathf.Clamp(data.tokenDividendLevel, 0, TokenDividendUpgradeCosts.Length);
            data.profitableEndingLevel = Mathf.Clamp(data.profitableEndingLevel, 0, ProfitableEndingUpgradeCosts.Length);
            data.passiveYieldLevel = Mathf.Clamp(data.passiveYieldLevel, 0, PassiveYieldUpgradeCosts.Length);
            data.passiveTickRateLevel = Mathf.Clamp(data.passiveTickRateLevel, 0, PassiveTickRateUpgradeCosts.Length);
            data.activeMultiplierLevel = Mathf.Clamp(data.activeMultiplierLevel, 0, ActiveMultiplierUpgradeCosts.Length);
            data.curriculumAmountLevel = Mathf.Clamp(data.curriculumAmountLevel, 0, CurriculumAmountUpgradeCosts.Length);
            data.curriculumRateLevel = Mathf.Clamp(data.curriculumRateLevel, 0, CurriculumRateUpgradeCosts.Length);
            data.curriculumFrameReduction = Math.Max(0, data.curriculumFrameReduction);
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
            if (data.datacenterRackCounts == null || data.datacenterRackCounts.Length != DatacenterRacks.Length)
            {
                int[] rackCounts = new int[DatacenterRacks.Length];
                for (int i = 0; data.datacenterRackCounts != null && i < data.datacenterRackCounts.Length && i < rackCounts.Length; i++)
                {
                    rackCounts[i] = data.datacenterRackCounts[i];
                }

                data.datacenterRackCounts = rackCounts;
            }

            for (int i = 0; i < data.datacenterRackCounts.Length; i++)
            {
                data.datacenterRackCounts[i] = Math.Max(0, data.datacenterRackCounts[i]);
            }

            if (data.datacenterFacilityLevels == null || data.datacenterFacilityLevels.Length != DatacenterFacilities.Length)
            {
                int[] facilityLevels = new int[DatacenterFacilities.Length];
                for (int i = 0; data.datacenterFacilityLevels != null && i < data.datacenterFacilityLevels.Length && i < facilityLevels.Length; i++)
                {
                    facilityLevels[i] = data.datacenterFacilityLevels[i];
                }

                data.datacenterFacilityLevels = facilityLevels;
            }

            for (int i = 0; i < data.datacenterFacilityLevels.Length; i++)
            {
                data.datacenterFacilityLevels[i] = Mathf.Clamp(data.datacenterFacilityLevels[i], 0, DatacenterFacilities[i].MaxLevel);
            }

            if (data.datacenterAllocation == null || data.datacenterAllocation.Length != 3)
            {
                float[] allocation = new float[3];
                for (int i = 0; data.datacenterAllocation != null && i < data.datacenterAllocation.Length && i < allocation.Length; i++)
                {
                    allocation[i] = data.datacenterAllocation[i];
                }

                data.datacenterAllocation = allocation;
            }

            float allocationTotal = 0f;
            for (int i = 0; i < data.datacenterAllocation.Length; i++)
            {
                data.datacenterAllocation[i] = Mathf.Clamp01(data.datacenterAllocation[i]);
                allocationTotal += data.datacenterAllocation[i];
            }

            if (allocationTotal > 1f)
            {
                for (int i = 0; i < data.datacenterAllocation.Length; i++)
                {
                    data.datacenterAllocation[i] /= allocationTotal;
                }
            }

            data.machineLearningPrestigeMemoryFrames = Math.Max(0, data.machineLearningPrestigeMemoryFrames);
            if (data.machineLearningPrestigeMemoryFrames <= 0)
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
