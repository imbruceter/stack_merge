using System;
using UnityEngine;

namespace StackMerge
{
    [Serializable]
    public sealed class StackMergeProgressionData
    {
        public long chips;
        public int selectedSolver;
        public int highestUnlockedSolver;
        public int speedLevel;
        public bool autoSolveEnabled = true;
        public bool autoRestartUnlocked;
        public bool autoRestartEnabled;
        public int stackCapacityLevel;
        public int runsCompleted;
    }

    public sealed class StackMergeProgression
    {
        private const string PlayerPrefsKey = "StackMerge.Progression.v1";

        private static readonly int[] SpeedUpgradeCosts = { 20, 55, 130, 300, 680, 1500, 3300 };
        private static readonly float[] MoveIntervals = { 1.4f, 0.95f, 0.65f, 0.45f, 0.32f, 0.23f, 0.16f, 0.11f };
        private static readonly int[] StackCapacityCosts = { 60, 140, 320, 720, 1600 };

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

        public float MoveInterval => MoveIntervals[Mathf.Clamp(data.speedLevel, 0, MoveIntervals.Length - 1)];

        public bool IsMaxSpeed => data.speedLevel >= MoveIntervals.Length - 1;

        public bool IsMaxStackCapacity => StackCapacity >= StackMergeGameState.MaxStackCapacity;

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
            return (int)solverId <= data.highestUnlockedSolver;
        }

        public long GetSolverUnlockCost(SolverId solverId)
        {
            return solverId switch
            {
                SolverId.Rand => 0,
                SolverId.Heur => 90,
                SolverId.Moca => 420,
                _ => long.MaxValue
            };
        }

        public bool SelectOrUnlockSolver(SolverId solverId)
        {
            if (IsSolverUnlocked(solverId))
            {
                data.selectedSolver = (int)solverId;
                return true;
            }

            long cost = GetSolverUnlockCost(solverId);
            if (!Spend(cost))
            {
                return false;
            }

            data.highestUnlockedSolver = Math.Max(data.highestUnlockedSolver, (int)solverId);
            data.selectedSolver = (int)solverId;
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

        public long AwardMove(MoveResult result)
        {
            if (!result.Accepted)
            {
                return 0;
            }

            long gained = 1 + result.MergeCount * (2 + FloorLog2(Math.Max(1, result.ResultingTopValue)));
            data.chips += gained;
            return gained;
        }

        public long AwardRunCompleted(long runScore)
        {
            data.runsCompleted++;
            long bonus = Math.Max(1, runScore / 50);
            data.chips += bonus;
            return bonus;
        }

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
            data.selectedSolver = Mathf.Clamp(data.selectedSolver, 0, (int)SolverId.Moca);
            data.highestUnlockedSolver = Mathf.Clamp(data.highestUnlockedSolver, 0, (int)SolverId.Moca);
            data.speedLevel = Mathf.Clamp(data.speedLevel, 0, MoveIntervals.Length - 1);
            data.stackCapacityLevel = Mathf.Clamp(data.stackCapacityLevel, 0, StackMergeGameState.MaxStackCapacity - StackMergeGameState.DefaultStackCapacity);

            if (data.selectedSolver > data.highestUnlockedSolver)
            {
                data.selectedSolver = data.highestUnlockedSolver;
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
