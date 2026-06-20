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
        public int runsCompleted;
        public bool[] agentUnlocked;
        public int[] activeAgentIds;
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

    public sealed class StackMergeProgression
    {
        private const string PlayerPrefsKey = "StackMerge.Progression.v2";
        private const int BaseAgentSlots = 2;

        private static readonly int[] SpeedUpgradeCosts = { 20, 55, 130, 300, 680, 1500, 3300 };
        private static readonly float[] MoveIntervals = { 1.4f, 0.95f, 0.65f, 0.45f, 0.32f, 0.23f, 0.16f, 0.11f };
        private static readonly int[] StackCapacityCosts = { 60, 140, 320, 720, 1600 };

        public static readonly AgentDefinition[] Agents =
        {
            new(AgentId.MergeBroker, "Merge Broker", 120, "Boosts merge income.", "+35% chips from merge rewards."),
            new(AgentId.HighwaterAnalyst, "Highwater Analyst", 240, "Rewards new highs.", "+70% chips from new highest-block rewards."),
            new(AgentId.ScoreAuditor, "Score Auditor", 420, "Turns score into chips.", "+20% chips from end-of-run score bonus."),
            new(AgentId.Overclocker, "Overclocker", 680, "Runs the solver faster.", "Solver move interval is 15% shorter."),
            new(AgentId.Quartermaster, "Quartermaster", 950, "Improves baseline income.", "+1 chip on every successful placement."),
            new(AgentId.Coordinator, "Coordinator", 1500, "Manages a larger crew.", "+1 active agent slot while equipped.")
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

        public int RunsCompleted => data.runsCompleted;

        public int MonteCarloSimulationCount => 8 + data.speedLevel * 4;

        public int MonteCarloRolloutDepth => 8 + data.speedLevel;

        public float MoveInterval => MoveIntervals[Mathf.Clamp(data.speedLevel, 0, MoveIntervals.Length - 1)] * AgentMoveIntervalMultiplier;

        public bool IsMaxSpeed => data.speedLevel >= MoveIntervals.Length - 1;

        public bool IsMaxStackCapacity => StackCapacity >= StackMergeGameState.MaxStackCapacity;

        public int ActiveAgentSlots => BaseAgentSlots + (IsAgentActive(AgentId.Coordinator) ? 1 : 0);

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

        public bool BuySpeedUpgrade()
        {
            if (IsMaxSpeed || !Spend(GetSpeedUpgradeCost()))
            {
                return false;
            }

            data.speedLevel++;
            return true;
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

        public bool BuyStackCapacityUpgrade()
        {
            if (IsMaxStackCapacity || !Spend(GetStackCapacityUpgradeCost()))
            {
                return false;
            }

            data.stackCapacityLevel++;
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
            return data.activeAgentIds.Any(id => id == (int)agentId);
        }

        public int ActiveAgentCount => data.activeAgentIds.Count(id => id >= 0);

        public string GetAgentInfo(AgentId agentId)
        {
            AgentDefinition definition = GetAgentDefinition(agentId);
            return IsAgentUnlocked(agentId) ? definition.Description : definition.LockedHint;
        }

        public bool BuyOrToggleAgent(AgentId agentId)
        {
            int index = (int)agentId;
            if (index < 0 || index >= Agents.Length)
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
                UnequipAgent(agentId);
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
            data.chips += gained;
            return gained;
        }

        public long AwardRunCompleted(long runScore)
        {
            data.runsCompleted++;
            long bonus = Math.Max(1, (long)Math.Ceiling((runScore / 50.0) * AgentScoreMultiplier));
            data.chips += bonus;
            return bonus;
        }

        private bool TryEquipAgent(AgentId agentId)
        {
            if (IsAgentActive(agentId))
            {
                return true;
            }

            int emptySlot = Array.FindIndex(data.activeAgentIds, id => id < 0);
            if (emptySlot < 0)
            {
                return false;
            }

            bool coordinatorCreatesSlot = agentId == AgentId.Coordinator && ActiveAgentCount == BaseAgentSlots;
            if (!coordinatorCreatesSlot && ActiveAgentCount >= ActiveAgentSlots)
            {
                return false;
            }

            data.activeAgentIds[emptySlot] = (int)agentId;
            return true;
        }

        private void UnequipAgent(AgentId agentId)
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

        private bool Spend(long cost)
        {
            if (cost < 0 || data.chips < cost)
            {
                return false;
            }

            data.chips -= cost;
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

            for (int i = 0; i < data.activeAgentIds.Length; i++)
            {
                int agentId = data.activeAgentIds[i];
                if (agentId < 0 || agentId >= Agents.Length || !data.agentUnlocked[agentId])
                {
                    data.activeAgentIds[i] = -1;
                }
            }

            TrimActiveAgentsToSlotLimit();
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
