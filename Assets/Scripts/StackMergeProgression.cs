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
        public int speedLevel;
        public bool autoSolveEnabled = true;
        public bool autoRestartUnlocked;
        public bool autoRestartEnabled;
        public int stackCapacityLevel;
        public int queuePreviewLevel;
        public int incomeLevel;
        public int difficultyLevel;
        public int runsCompleted;
        public bool agentsMenuUnlocked;
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
        Coordinator = 5
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

        private static readonly int[] SpeedUpgradeCosts = { 20, 55, 130, 300, 680 };
        private static readonly float[] MoveIntervals = { 1.4f, 0.95f, 0.65f, 0.45f, 0.30f, 0.20f };
        private static readonly int[] StackCapacityCosts = { 60, 140, 320, 720, 1600 };
        private static readonly int[] QueuePreviewUpgradeCosts = { 260, 900 };
        private static readonly int[] IncomeUpgradeCosts = { 90, 220, 520, 1200, 2800 };
        private static readonly int[] DifficultyUpgradeCosts = { 350, 1200, 3600 };
        private const int AgentsMenuUnlockCost = 650;
        private const int MaxHistoryEntries = 250;

        public static readonly AgentDefinition[] Agents =
        {
            new(AgentId.MergeBroker, "Merge Broker", 120, "Boosts merge income.", "+35% chips from merge rewards."),
            new(AgentId.HighwaterAnalyst, "Highwater Analyst", 240, "Rewards new highs.", "+70% chips from new highest-block rewards."),
            new(AgentId.ScoreAuditor, "Score Auditor", 420, "Turns score into chips.", "+20% chips from end-of-run score bonus."),
            new(AgentId.Overclocker, "Overclocker", 680, "Runs the solver faster.", "Solver move interval is 15% shorter."),
            new(AgentId.Quartermaster, "Quartermaster", 950, "Improves baseline income.", "+1 chip on every successful placement."),
            new(AgentId.Coordinator, "Coordinator", 1500, "Manages a larger crew.", "+1 active agent slot while equipped.")
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

        public StackMergeProgression(StackMergeProgressionData data)
        {
            this.data = data ?? new StackMergeProgressionData();
            Normalize();
        }

        public long Chips => data.chips;

        public SolverId SelectedSolver => (SolverId)data.selectedSolver;

        public int SpeedLevel => data.speedLevel;

        public bool AutoSolveEnabled
        {
            get => data.autoSolveEnabled;
            set => data.autoSolveEnabled = value;
        }

        public bool AutoRestartUnlocked => data.autoRestartUnlocked;

        public bool AutoRestartEnabled => data.autoRestartEnabled;

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

        public RunHistoryEntry[] RunHistory => data.runHistory ?? Array.Empty<RunHistoryEntry>();

        public long TotalChipsEarned => data.totalChipsEarned;

        public long TotalChipsSpent => data.totalChipsSpent;

        public int ManualRunsCompleted => data.manualRunsCompleted;

        public int TotalBlocksDropped => data.totalBlocksDropped;

        public int TotalMerges => data.totalMerges;

        public int HighestBlockEver => data.highestBlockEver;

        public long BestRunScore => data.bestRunScore;

        public int MonteCarloSimulationCount => 8 + data.speedLevel * 4;

        public int MonteCarloRolloutDepth => 8 + data.speedLevel;

        public float MoveInterval => MoveIntervals[Mathf.Clamp(data.speedLevel, 0, MoveIntervals.Length - 1)] * AgentMoveIntervalMultiplier;

        public bool IsMaxSpeed => data.speedLevel >= MoveIntervals.Length - 1;

        public int MaxSpeedLevel => SpeedUpgradeCosts.Length;

        public bool IsMaxStackCapacity => StackCapacity >= StackMergeGameState.MaxStackCapacity;

        public bool IsMaxQueuePreview => data.queuePreviewLevel >= QueuePreviewUpgradeCosts.Length;

        public bool IsMaxIncome => data.incomeLevel >= IncomeUpgradeCosts.Length;

        public bool IsMaxDifficulty => data.difficultyLevel >= DifficultyUpgradeCosts.Length;

        public int ActiveAgentSlots => BaseAgentSlots + (IsAgentActive(AgentId.Coordinator) ? 1 : 0);

        public int MaxAgentSlots => BaseAgentSlots + 1;

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

        public void Save()
        {
            PlayerPrefs.SetString(PlayerPrefsKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public bool IsSolverUnlocked(SolverId solverId)
        {
            int index = (int)solverId;
            return index >= 0 && index < data.solverUnlocked.Length && data.solverUnlocked[index];
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

        public long GetAutoRestartCost()
        {
            return data.autoRestartUnlocked ? 0 : 180;
        }

        public bool ToggleOrBuyAutoRestart()
        {
            if (!data.autoRestartUnlocked)
            {
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

        public long AwardMove(MoveResult result)
        {
            if (!result.Accepted)
            {
                return 0;
            }

            long placement = 1 + AgentFlatPlacementBonus;
            long merge = result.MergeCount * (2 + FloorLog2(Math.Max(1, result.ResultingTopValue)));
            long highest = result.MergeCount > 0 && result.ResultingTopValue >= result.HighestBlock
                ? 2 + FloorLog2(Math.Max(1, result.HighestBlock)) * 2
                : 0;

            long gained = placement;
            gained += (long)Math.Ceiling(merge * AgentMergeMultiplier);
            gained += (long)Math.Ceiling(highest * AgentHighestMultiplier);
            gained = ApplyIncomeMultiplier(gained);
            data.chips += gained;
            data.totalChipsEarned += gained;
            data.totalBlocksDropped++;
            data.totalMerges += Math.Max(0, result.MergeCount);
            data.highestBlockEver = Math.Max(data.highestBlockEver, result.HighestBlock);
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
            data.runsCompleted++;
            if (manualRun)
            {
                data.manualRunsCompleted++;
            }

            data.bestRunScore = Math.Max(data.bestRunScore, Math.Max(0, runScore));
            data.highestBlockEver = Math.Max(data.highestBlockEver, highestMergedBlock);
            long bonus = Math.Max(1, (long)Math.Ceiling((runScore / 50.0) * AgentScoreMultiplier));
            bonus = ApplyIncomeMultiplier(bonus);
            data.chips += bonus;
            data.totalChipsEarned += bonus;
            RecordRunHistory(runScore, solverId, moves, merges, highestMergedBlock);
            return bonus;
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

        private double AgentMergeMultiplier => IsAgentActive(AgentId.MergeBroker) ? 1.35 : 1.0;

        private double AgentHighestMultiplier => IsAgentActive(AgentId.HighwaterAnalyst) ? 1.70 : 1.0;

        private double AgentScoreMultiplier => IsAgentActive(AgentId.ScoreAuditor) ? 1.20 : 1.0;

        private float AgentMoveIntervalMultiplier => IsAgentActive(AgentId.Overclocker) ? 0.85f : 1f;

        private int AgentFlatPlacementBonus => IsAgentActive(AgentId.Quartermaster) ? 1 : 0;

        private long ApplyIncomeMultiplier(long amount)
        {
            return Math.Max(1, (long)Math.Ceiling(amount * (1.0 + data.incomeLevel * 0.12)));
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
            data.selectedSolver = Mathf.Clamp(data.selectedSolver, 0, solverCount - 1);
            if (!data.solverUnlocked[data.selectedSolver])
            {
                data.selectedSolver = 0;
            }

            data.highestUnlockedSolver = Math.Max(data.highestUnlockedSolver, data.selectedSolver);
            data.speedLevel = Mathf.Clamp(data.speedLevel, 0, MoveIntervals.Length - 1);
            data.stackCapacityLevel = Mathf.Clamp(data.stackCapacityLevel, 0, StackMergeGameState.MaxStackCapacity - StackMergeGameState.DefaultStackCapacity);
            data.queuePreviewLevel = Mathf.Clamp(data.queuePreviewLevel, 0, QueuePreviewUpgradeCosts.Length);
            data.incomeLevel = Mathf.Clamp(data.incomeLevel, 0, IncomeUpgradeCosts.Length);
            data.difficultyLevel = Mathf.Clamp(data.difficultyLevel, 0, DifficultyUpgradeCosts.Length);

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
            if (data.runHistory.Length > 0)
            {
                data.highestBlockEver = Math.Max(data.highestBlockEver, data.runHistory.Max(entry => Math.Max(0, entry.highestMergedBlock)));
                data.bestRunScore = Math.Max(data.bestRunScore, data.runHistory.Max(entry => Math.Max(0, entry.score)));
            }
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
