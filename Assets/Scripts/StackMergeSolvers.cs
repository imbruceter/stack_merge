using System;
using System.Collections.Generic;
using System.Linq;
using static StackMerge.SolverScoring;

namespace StackMerge
{
    public enum SolverId
    {
        Rand = 0,
        Merge = 1,
        Balance = 2,
        Heur = 3,
        Look = 4,
        Moca = 5,
        Plan3 = 6,
        Plan5 = 7,
        MocaPlus = 8,
        Mcts = 9,
        AntiStall = 10,
        Combo = 11,
        MachineLearning = 12
    }

    public enum SolverTuneParameterId
    {
        ScoreDelta,
        MergeReward,
        HighBlockValue,
        FreeSpace,
        Smoothness,
        DangerPenalty,
        QueueFit,
        PairSetup,
        FollowUpWeight,
        PlanningDepth,
        SimulationRounds,
        RolloutMoves,
        RolloutPlanning,
        BoardEvaluation,
        AntiStallPressure,
        ComboSetup,
        Exploration,
        PriorBias,
        TreeVisits,
        SafetyCushion,
        FutureDepth,
        Gamma,
        Lambda,
        ClipEpsilon
    }

    public readonly struct SolverTuningParameterDefinition
    {
        public SolverTuningParameterDefinition(SolverTuneParameterId id, string displayName, string description, int minValue, int maxValue, float displayStep, float displayBase = 0f)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            MinValue = minValue;
            MaxValue = maxValue;
            DisplayStep = Math.Max(0.001f, displayStep);
            DisplayBase = displayBase;
        }

        public SolverTuneParameterId Id { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public int MinValue { get; }

        public int MaxValue { get; }

        public float DisplayStep { get; }

        /// <summary>
        /// When non-zero, the parameter represents an ABSOLUTE value (base + raw * step) rather than
        /// a relative offset, and is formatted as that absolute value (e.g. Gamma "0.94"). Offset
        /// parameters keep DisplayBase = 0 and format as "+2" / "-1".
        /// </summary>
        public float DisplayBase { get; }

        public bool WholeNumbers => DisplayStep >= 0.999f;

        public bool IsAbsolute => Math.Abs(DisplayBase) > 0.0001f;

        public float MinDisplayValue => DisplayBase + MinValue * DisplayStep;

        public float MaxDisplayValue => DisplayBase + MaxValue * DisplayStep;

        public float ToDisplayValue(int rawValue)
        {
            return DisplayBase + rawValue * DisplayStep;
        }

        public int FromDisplayValue(float displayValue)
        {
            int rawValue = (int)Math.Round((displayValue - DisplayBase) / DisplayStep, MidpointRounding.AwayFromZero);
            return Math.Min(MaxValue, Math.Max(MinValue, rawValue));
        }

        public string FormatValue(int rawValue)
        {
            // Absolute parameters always show their real value; the neutral (raw 0) entry also
            // labels itself as the default, e.g. "0.99 (Default)".
            if (IsAbsolute)
            {
                string formatted = WholeNumbers ? $"{ToDisplayValue(rawValue):0}" : $"{ToDisplayValue(rawValue):0.00}";
                return rawValue == 0 ? $"{formatted} (Default)" : formatted;
            }

            if (rawValue == 0)
            {
                return "Default";
            }

            float displayValue = ToDisplayValue(rawValue);
            return WholeNumbers
                ? (displayValue > 0 ? $"+{displayValue:0}" : $"{displayValue:0}")
                : (displayValue > 0 ? $"+{displayValue:0.0}" : $"{displayValue:0.0}");
        }
    }

    public readonly struct SolverTuningDefinition
    {
        public SolverTuningDefinition(SolverId solverId, string summary, params SolverTuningParameterDefinition[] parameters)
        {
            SolverId = solverId;
            Summary = summary;
            Parameters = parameters ?? Array.Empty<SolverTuningParameterDefinition>();
        }

        public SolverId SolverId { get; }

        public string Summary { get; }

        public SolverTuningParameterDefinition[] Parameters { get; }

        public bool HasParameters => Parameters.Length > 0;
    }

    public readonly struct SolverTuningSettings
    {
        public const int MinValue = -30;
        public const int MaxValue = 30;
        public const int MaxSlots = 6;

        private readonly int[] values;

        public SolverTuningSettings(SolverId solverId, IReadOnlyList<int> values)
        {
            SolverId = solverId;
            this.values = new int[MaxSlots];
            if (values == null)
            {
                return;
            }

            for (int i = 0; i < values.Count && i < MaxSlots; i++)
            {
                this.values[i] = ClampValue(solverId, i, values[i]);
            }
        }

        public SolverId SolverId { get; }

        public bool IsNeutral
        {
            get
            {
                for (int i = 0; i < MaxSlots; i++)
                {
                    if (GetSlotValue(i) != 0)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public static SolverTuningSettings Neutral(SolverId solverId) => new(solverId, null);

        public static int ClampValue(int value)
        {
            return Math.Min(MaxValue, Math.Max(MinValue, value));
        }

        public static int ClampValue(SolverId solverId, int slotIndex, int value)
        {
            SolverTuningDefinition definition = StackMergeSolverCatalog.GetTuningDefinition(solverId);
            if (slotIndex < 0 || slotIndex >= definition.Parameters.Length)
            {
                return ClampValue(value);
            }

            SolverTuningParameterDefinition parameter = definition.Parameters[slotIndex];
            return Math.Min(parameter.MaxValue, Math.Max(parameter.MinValue, value));
        }

        public int GetSlotValue(int slotIndex)
        {
            return values != null && slotIndex >= 0 && slotIndex < values.Length ? values[slotIndex] : 0;
        }

        public int GetValue(SolverTuneParameterId parameterId)
        {
            int slotIndex = StackMergeSolverCatalog.GetTuningParameterIndex(SolverId, parameterId);
            return slotIndex >= 0 ? GetSlotValue(slotIndex) : 0;
        }

        public double GetTunedValue(SolverTuneParameterId parameterId)
        {
            int slotIndex = StackMergeSolverCatalog.GetTuningParameterIndex(SolverId, parameterId);
            if (slotIndex < 0)
            {
                return 0;
            }

            SolverTuningParameterDefinition parameter = StackMergeSolverCatalog.GetTuningDefinition(SolverId).Parameters[slotIndex];
            return parameter.ToDisplayValue(GetSlotValue(slotIndex));
        }

        public double Factor(SolverTuneParameterId parameterId, double step = 0.12)
        {
            return Math.Max(0.05, 1.0 + GetTunedValue(parameterId) * step);
        }

        public int Additive(SolverTuneParameterId parameterId, int step = 1)
        {
            return GetValue(parameterId) * step;
        }

        public int[] ToArray()
        {
            int[] copy = new int[MaxSlots];
            for (int i = 0; i < copy.Length; i++)
            {
                copy[i] = GetSlotValue(i);
            }

            return copy;
        }
    }

    public readonly struct SolverDefinition
    {
        public SolverDefinition(SolverId id, string displayName, long cost, string lockedHint, string description, bool available = true)
        {
            Id = id;
            DisplayName = displayName;
            Cost = cost;
            LockedHint = lockedHint;
            Description = description;
            Available = available;
        }

        public SolverId Id { get; }

        public string DisplayName { get; }

        public long Cost { get; }

        public string LockedHint { get; }

        public string Description { get; }

        /// <summary>
        /// Whether this solver is offered in the shop / benchmark / auto-buyer. Removed solvers
        /// (MERG, STALL, MCTS, PLAN-5, MOCA+) are kept in the catalog (their SolverId indices and
        /// classes must stay — e.g. STALL's ScoreAntiStall is a shared static used by MOCA+) but
        /// flagged unavailable. Flip back to true to restore any of them.
        /// </summary>
        public bool Available { get; }
    }

    public static class StackMergeSolverCatalog
    {
        public static readonly SolverDefinition[] Definitions =
        {
            new(SolverId.Rand, "RAND", 1000, "Free baseline solver.", "Randomly chooses any valid stack. Weak, chaotic, but fast."),
            // MERG removed (2026-07-12): BAL is the more sensible cheap-tier pick. Class kept; flagged unavailable.
            new(SolverId.Merge, "MERG", 3000, "Looks for direct merges.", "Prioritizes immediate merges and cascades before anything else.", available: false),
            new(SolverId.Balance, "BAL", 6000, "Keeps stacks even.", "Avoids tall dangerous stacks and spreads risk across the board."),
            new(SolverId.Heur, "HEUR", 30000, "Scores every legal move.", "Uses handcrafted heuristics: merge value, danger, future queue fit, and free space."),
            // LOOK repriced above COMBO to match the intended quality ladder (RAND<BAL<HEUR<COMBO<LOOK<MOCA<PLAN).
            new(SolverId.Look, "LOOK", 500000, "Plans one move deeper.", "Tests each move, then estimates the best follow-up move before deciding."),
            // MOCA & PLAN repriced high — they double as the mid-late ("agents phase") chip sinks.
            new(SolverId.Moca, "MOCA", 5000000, "Runs simulations.", "Rolls out multiple futures, judging each by the surviving board and the peak tier it reaches."),
            new(SolverId.Plan3, "PLAN", 15000000, "Reads the visible queue.", "Queue planner. Searches lines through the visible next blocks. Planning depth (3–5) can be tuned for patience vs practicality."),
            // PLAN-5 removed (2026-07-12): merged into PLAN via its Planning depth tuning (up to 5). Class kept; unavailable.
            new(SolverId.Plan5, "PLAN-5", 400000, "Uses the extended queue.", "Deep queue planner. Searches lines through up to 5 visible next blocks. Stronger once next preview upgrades are unlocked.", available: false),
            // MOCA+ removed (2026-07-12): its smart-rollout params fold into MOCA's tuning (Stage B). Class kept; unavailable.
            new(SolverId.MocaPlus, "MOCA+", 900000, "Smarter Monte Carlo rollouts.", "Enhanced Monte Carlo. Each rollout uses short queue planning and an anti-stall board score.", available: false),
            // MCTS removed (2026-07-12): weaker than MOCA, its niche folds into MOCA tuning (Stage B). Class kept; unavailable.
            new(SolverId.Mcts, "MCTS", 1800000, "Builds a search tree.", "Monte Carlo Tree Search. Balances exploring new lines with exploiting lines that already score well.", available: false),
            // STALL removed (2026-07-12): run-length niche not worth a slot. Class kept (ScoreAntiStall is a shared static); unavailable.
            new(SolverId.AntiStall, "STALL", 18000, "Avoids dead boards.", "Anti-stall solver. Heavily protects legal moves, semi-empty stacks, and escape routes over greedy merges.", available: false),
            new(SolverId.Combo, "COMBO", 250000, "Sets up chain merges.", "Combo-focused solver. Reads the queue and rewards positions that can cascade over the next 2-3 turns."),
            new(SolverId.MachineLearning, "PPO", 10000000000, "Endgame learner. Requires every Modifier to be fully purchased.", "Proximal Policy Optimization is a lightweight actor-critic neural network that learns its policy from run trajectories, clipped policy updates, value estimates, and entropy-driven exploration.")
        };

        public static readonly SolverTuningDefinition[] TuningDefinitions =
        {
            new(SolverId.Rand, "RAND has no tuning. Its identity is pure randomness."),
            new(SolverId.Merge, "MERG stays greedy, but these sliders decide how hard it chases immediate value.",
                Tune(SolverTuneParameterId.MergeReward, "Merge reward", "Raises or lowers the reward for immediate merges and cascade size."),
                Tune(SolverTuneParameterId.ScoreDelta, "Score delta", "Changes how much raw score gained by the move matters."),
                Tune(SolverTuneParameterId.HighBlockValue, "High block", "Pushes the solver toward producing larger top blocks."),
                Tune(SolverTuneParameterId.Smoothness, "Height penalty", "Positive values avoid tall uneven stacks more strongly.")),
            new(SolverId.Balance, "BAL remains a board stabilizer. Tune how defensive or merge-friendly it should be.",
                Tune(SolverTuneParameterId.FreeSpace, "Empty cells", "Rewards boards with more open cells after the move."),
                Tune(SolverTuneParameterId.Smoothness, "Smoothness", "Rewards similar stack heights and penalizes lopsided boards."),
                Tune(SolverTuneParameterId.DangerPenalty, "Danger penalty", "Penalizes stacks near capacity more strongly."),
                Tune(SolverTuneParameterId.MergeReward, "Merge reward", "Lets BAL accept more merge value before spreading out.")),
            new(SolverId.Heur, "HEUR is a weighted score formula. These are the clearest direct knobs.",
                Tune(SolverTuneParameterId.ScoreDelta, "Score delta", "Changes how strongly immediate score gain affects the move score."),
                Tune(SolverTuneParameterId.FreeSpace, "Empty cells", "Rewards free stack cells after the move."),
                Tune(SolverTuneParameterId.Smoothness, "Smoothness", "Rewards smoother stack heights and fewer awkward bottlenecks."),
                Tune(SolverTuneParameterId.QueueFit, "Queue fit", "Rewards top blocks that match upcoming queue values."),
                Tune(SolverTuneParameterId.MergeReward, "Merge reward", "Raises or lowers the value of immediate merge chains."),
                Tune(SolverTuneParameterId.DangerPenalty, "Danger penalty", "Makes near-full stacks scarier to the heuristic.")),
            new(SolverId.Look, "LOOK uses HEUR plus a follow-up estimate. Tune its greed and its second-step trust.",
                Tune(SolverTuneParameterId.ScoreDelta, "Score delta", "Changes how much the first move's score gain matters."),
                Tune(SolverTuneParameterId.FreeSpace, "Empty cells", "Rewards keeping more room open after the first move."),
                Tune(SolverTuneParameterId.FollowUpWeight, "Follow-up trust", "Changes how much the simulated next move influences the choice."),
                Tune(SolverTuneParameterId.QueueFit, "Queue fit", "Rewards stack tops that line up with visible upcoming blocks."),
                Tune(SolverTuneParameterId.DangerPenalty, "Danger penalty", "Punishes positions that are close to stalling.")),
            new(SolverId.Moca, "MOCA samples futures. Its tuning can spend a little more thinking on rollout depth or sample count.",
                TuneWhole(SolverTuneParameterId.SimulationRounds, "Simulation rounds", "Adjusts how many futures are sampled for each legal move.", -2, 2),
                TuneWhole(SolverTuneParameterId.RolloutMoves, "Rollout moves", "Adjusts how many moves each future is played forward.", -2, 1),
                Tune(SolverTuneParameterId.ScoreDelta, "Score delta", "Changes the immediate score bias inside rollout evaluation."),
                Tune(SolverTuneParameterId.FreeSpace, "Empty cells", "Rewards futures that leave more cells open."),
                Tune(SolverTuneParameterId.DangerPenalty, "Danger penalty", "Makes dangerous simulated boards less attractive."),
                Tune(SolverTuneParameterId.QueueFit, "Queue fit", "Rewards rollouts that keep useful top blocks for the visible queue.")),
            new(SolverId.Plan3, "PLAN searches the visible queue. Tune how much it trusts short plans over current safety.",
                TuneWhole(SolverTuneParameterId.PlanningDepth, "Planning depth", "Shifts how many visible queued blocks the search tries to use.", -2, 2),
                Tune(SolverTuneParameterId.FollowUpWeight, "Future weight", "Changes how strongly future planned moves affect the first move."),
                Tune(SolverTuneParameterId.QueueFit, "Queue fit", "Rewards stack tops that match upcoming values."),
                Tune(SolverTuneParameterId.FreeSpace, "Empty cells", "Rewards planned lines that keep space available."),
                Tune(SolverTuneParameterId.DangerPenalty, "Danger penalty", "Penalizes plans that leave near-full stacks.")),
            new(SolverId.Plan5, "PLAN-5 searches deeper queue lines. Tuning lets you decide whether it should be patient or practical.",
                TuneWhole(SolverTuneParameterId.PlanningDepth, "Planning depth", "Shifts how many queued blocks the search tries to use.", -2, 2),
                Tune(SolverTuneParameterId.FollowUpWeight, "Future weight", "Changes how strongly deeper planned scores affect the first move."),
                Tune(SolverTuneParameterId.QueueFit, "Queue fit", "Rewards preserving useful stack tops for upcoming blocks."),
                Tune(SolverTuneParameterId.FreeSpace, "Empty cells", "Rewards plans that leave more board room."),
                Tune(SolverTuneParameterId.DangerPenalty, "Danger penalty", "Penalizes risky deep plans more strongly.")),
            new(SolverId.MocaPlus, "MOCA+ uses smarter rollouts. Tuning affects both how much it samples and how it values rollout boards.",
                TuneWhole(SolverTuneParameterId.SimulationRounds, "Simulation rounds", "Adjusts how many smart futures are sampled for each move.", -2, 1),
                TuneWhole(SolverTuneParameterId.RolloutMoves, "Rollout moves", "Adjusts how far smart futures are played forward.", -2, 1),
                TuneWhole(SolverTuneParameterId.RolloutPlanning, "Rollout planning", "Adjusts how much queue planning each rollout uses.", -1, 1),
                Tune(SolverTuneParameterId.BoardEvaluation, "Board eval", "Changes how much the final simulated board shape matters."),
                Tune(SolverTuneParameterId.AntiStallPressure, "Anti-stall", "Rewards futures that preserve legal moves and escape routes.")),
            new(SolverId.Mcts, "MCTS builds a tree. These sliders tune search behavior without replacing the tree search identity.",
                TuneWhole(SolverTuneParameterId.TreeVisits, "Tree visits", "Adjusts how many tree iterations are spent per decision.", -3, 2),
                Tune(SolverTuneParameterId.Exploration, "Exploration", "Higher values try less-proven branches more often."),
                Tune(SolverTuneParameterId.PriorBias, "Prior bias", "Changes how strongly heuristic prior scores guide the tree."),
                TuneWhole(SolverTuneParameterId.RolloutMoves, "Rollout moves", "Adjusts how far rollouts play from a tree node.", -2, 1),
                Tune(SolverTuneParameterId.SafetyCushion, "Safety cushion", "Rewards tree lines that leave room and legal moves."),
                Tune(SolverTuneParameterId.ComboSetup, "Combo setup", "Rewards lines that create potential chain merges.")),
            new(SolverId.AntiStall, "STALL is defensive. Tune how much it sacrifices score to keep the board alive.",
                Tune(SolverTuneParameterId.AntiStallPressure, "Legal moves", "Rewards positions with multiple available stacks."),
                Tune(SolverTuneParameterId.FreeSpace, "Empty cells", "Rewards spare capacity after each move."),
                Tune(SolverTuneParameterId.DangerPenalty, "Danger penalty", "Punishes near-full stacks more strongly."),
                Tune(SolverTuneParameterId.Smoothness, "Height spread", "Penalizes uneven stack heights."),
                Tune(SolverTuneParameterId.MergeReward, "Merge reward", "Lets STALL take more immediate merge value.")),
            new(SolverId.Combo, "COMBO looks for chain setups. Tune how patient it should be while preparing cascades.",
                Tune(SolverTuneParameterId.ComboSetup, "Combo setup", "Rewards adjacent equal values and future cascade potential."),
                Tune(SolverTuneParameterId.QueueFit, "Queue fit", "Rewards top blocks that match upcoming queue values."),
                Tune(SolverTuneParameterId.MergeReward, "Merge reward", "Changes how much immediate merging competes with setup."),
                TuneWhole(SolverTuneParameterId.FutureDepth, "Future depth", "Adjusts how many setup moves the combo estimate looks through.", -2, 2),
                Tune(SolverTuneParameterId.SafetyCushion, "Safety cushion", "Keeps some space open while building combos.")),
            new(SolverId.MachineLearning, "PPO trains its own actor-critic network. These nudge the learning hyperparameters within safe bounds.",
                TuneAbsolute(SolverTuneParameterId.Gamma, "Gamma (discount)", "How far ahead future reward is valued. Higher plans longer-term, lower is greedier.", 0.99f, 0.01f, 0.80f, 0.99f),
                TuneAbsolute(SolverTuneParameterId.Lambda, "Lambda (GAE)", "Advantage estimation bias/variance trade-off. Higher = lower bias, more variance.", 0.95f, 0.01f, 0.80f, 0.99f),
                TuneAbsolute(SolverTuneParameterId.ClipEpsilon, "Clip epsilon", "How big a policy update each step may make. Smaller is more conservative and stable.", 0.20f, 0.01f, 0.10f, 0.30f))
        };

        /// <summary>Count of solvers offered in the shop (Available). Removed solvers are excluded.</summary>
        public static int AvailableSolverCount
        {
            get
            {
                int count = 0;
                foreach (SolverDefinition definition in Definitions)
                {
                    if (definition.Available)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public static SolverDefinition GetDefinition(SolverId id)
        {
            int index = (int)id;
            if (index < 0)
            {
                index = 0;
            }

            if (index >= Definitions.Length)
            {
                index = Definitions.Length - 1;
            }

            return Definitions[index];
        }

        public static SolverTuningDefinition GetTuningDefinition(SolverId id)
        {
            int index = (int)id;
            if (index < 0)
            {
                index = 0;
            }

            if (index >= TuningDefinitions.Length)
            {
                index = TuningDefinitions.Length - 1;
            }

            return TuningDefinitions[index];
        }

        public static int GetTuningParameterIndex(SolverId solverId, SolverTuneParameterId parameterId)
        {
            SolverTuningParameterDefinition[] parameters = GetTuningDefinition(solverId).Parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Id == parameterId)
                {
                    return i;
                }
            }

            return -1;
        }

        private static SolverTuningParameterDefinition Tune(SolverTuneParameterId id, string displayName, string description, int minValue = SolverTuningSettings.MinValue, int maxValue = SolverTuningSettings.MaxValue)
        {
            return new SolverTuningParameterDefinition(id, displayName, description, minValue, maxValue, 0.1f);
        }

        private static SolverTuningParameterDefinition TuneWhole(SolverTuneParameterId id, string displayName, string description, int minValue, int maxValue)
        {
            return new SolverTuningParameterDefinition(id, displayName, description, minValue, maxValue, 1f);
        }

        // An absolute-valued parameter: the slider shows the real value (e.g. 0.80 .. 0.99) and raw 0
        // maps to the supplied default, so "neutral" is the default and tuning nudges around it.
        private static SolverTuningParameterDefinition TuneAbsolute(SolverTuneParameterId id, string displayName, string description, float defaultValue, float step, float minValue, float maxValue)
        {
            int min = (int)Math.Round((minValue - defaultValue) / step);
            int max = (int)Math.Round((maxValue - defaultValue) / step);
            return new SolverTuningParameterDefinition(id, displayName, description, min, max, step, defaultValue);
        }
    }

    public interface IStackMergeSolver
    {
        SolverId Id { get; }

        string DisplayName { get; }

        SolverDecision ChooseMove(StackMergeGameState state, SolverContext context);
    }

    public static class StackMergeSolverFactory
    {
        public static IStackMergeSolver[] CreateAll()
        {
            return new IStackMergeSolver[]
            {
                new RandomStackMergeSolver(),
                new MergeFirstStackMergeSolver(),
                new BalancedStackMergeSolver(),
                new HeuristicStackMergeSolver(),
                new LookaheadStackMergeSolver(),
                new MonteCarloStackMergeSolver(),
                new Plan3StackMergeSolver(),
                new Plan5StackMergeSolver(),
                new EnhancedMonteCarloStackMergeSolver(),
                new MctsStackMergeSolver(),
                new AntiStallStackMergeSolver(),
                new ComboFocusedStackMergeSolver(),
                new MachineLearningStackMergeSolver()
            };
        }

        public static IStackMergeSolver Create(SolverId solverId)
        {
            return solverId switch
            {
                SolverId.Merge => new MergeFirstStackMergeSolver(),
                SolverId.Balance => new BalancedStackMergeSolver(),
                SolverId.Heur => new HeuristicStackMergeSolver(),
                SolverId.Look => new LookaheadStackMergeSolver(),
                SolverId.Moca => new MonteCarloStackMergeSolver(),
                SolverId.Plan3 => new Plan3StackMergeSolver(),
                SolverId.Plan5 => new Plan5StackMergeSolver(),
                SolverId.MocaPlus => new EnhancedMonteCarloStackMergeSolver(),
                SolverId.Mcts => new MctsStackMergeSolver(),
                SolverId.AntiStall => new AntiStallStackMergeSolver(),
                SolverId.Combo => new ComboFocusedStackMergeSolver(),
                SolverId.MachineLearning => new MachineLearningStackMergeSolver(),
                _ => new RandomStackMergeSolver()
            };
        }
    }

    public readonly struct SolverContext
    {
        public SolverContext(
            Random random,
            int monteCarloSimulations,
            int monteCarloRolloutDepth,
            bool lightweightMode = false,
            int planningDepthLimit = int.MaxValue,
            SolverTuningSettings? tuning = null,
            bool highTierSpeedTuningAccelerator = false,
            StackMergePpoAgent machineLearningAgent = null,
            bool machineLearningTrainingMode = false)
        {
            Random = random ?? new Random();
            MonteCarloSimulations = Math.Max(1, monteCarloSimulations);
            MonteCarloRolloutDepth = Math.Max(1, monteCarloRolloutDepth);
            LightweightMode = lightweightMode;
            PlanningDepthLimit = Math.Max(1, planningDepthLimit);
            Tuning = tuning ?? SolverTuningSettings.Neutral(SolverId.Rand);
            HighTierSpeedTuningAccelerator = highTierSpeedTuningAccelerator;
            MachineLearningAgent = machineLearningAgent;
            MachineLearningTrainingMode = machineLearningTrainingMode;
        }

        public Random Random { get; }

        public int MonteCarloSimulations { get; }

        public int MonteCarloRolloutDepth { get; }

        public bool LightweightMode { get; }

        public int PlanningDepthLimit { get; }

        public SolverTuningSettings Tuning { get; }

        public bool HighTierSpeedTuningAccelerator { get; }

        public StackMergePpoAgent MachineLearningAgent { get; }

        public bool MachineLearningTrainingMode { get; }

        public int LimitPlanningDepth(int requestedDepth)
        {
            return Math.Min(Math.Max(1, requestedDepth), PlanningDepthLimit);
        }

        public int TunedSimulationCount()
        {
            int additive = GetSpeedAdditive(SolverTuneParameterId.SimulationRounds);
            return AccelerateHighTierCompute(Math.Max(1, MonteCarloSimulations + additive));
        }

        public int TunedRolloutDepth()
        {
            int additive = GetSpeedAdditive(SolverTuneParameterId.RolloutMoves);
            return AccelerateHighTierCompute(Math.Max(1, MonteCarloRolloutDepth + additive));
        }

        public int TunedPlanningDepth(int requestedDepth)
        {
            return LimitPlanningDepth(requestedDepth + Tuning.Additive(SolverTuneParameterId.PlanningDepth));
        }

        public int TunedRolloutPlanningDepth(int requestedDepth)
        {
            return LimitPlanningDepth(requestedDepth + Tuning.Additive(SolverTuneParameterId.RolloutPlanning));
        }

        public int TunedFutureDepth(int requestedDepth)
        {
            return Math.Max(1, requestedDepth + Tuning.Additive(SolverTuneParameterId.FutureDepth));
        }

        public int TunedTreeIterations(int baseIterations)
        {
            int additive = GetSpeedAdditive(SolverTuneParameterId.TreeVisits, 2);
            return AccelerateHighTierCompute(Math.Max(2, baseIterations + additive));
        }

        private int GetSpeedAdditive(SolverTuneParameterId parameterId, int step = 1)
        {
            int additive = Tuning.Additive(parameterId, step);
            if (HighTierSpeedTuningAccelerator && IsAcceleratedHighTierSolver(Tuning.SolverId) && additive < 0)
            {
                additive *= 2;
            }

            return additive;
        }

        private int AccelerateHighTierCompute(int value)
        {
            if (!HighTierSpeedTuningAccelerator || !IsAcceleratedHighTierSolver(Tuning.SolverId))
            {
                return value;
            }

            return Math.Max(1, (int)Math.Ceiling(value * 0.5));
        }

        private static bool IsAcceleratedHighTierSolver(SolverId solverId)
        {
            return solverId == SolverId.Moca || solverId == SolverId.MocaPlus || solverId == SolverId.Mcts;
        }
    }

    public readonly struct SolverDecision
    {
        public SolverDecision(bool hasMove, int stackIndex, double score, string reason)
            : this(hasMove, SolverActionKind.Place, stackIndex, -1, score, reason)
        {
        }

        public SolverDecision(bool hasMove, SolverActionKind actionKind, int stackIndex, int blockIndex, double score, string reason)
        {
            HasMove = hasMove;
            ActionKind = actionKind;
            StackIndex = stackIndex;
            BlockIndex = blockIndex;
            Score = score;
            Reason = reason;
        }

        public bool HasMove { get; }

        public SolverActionKind ActionKind { get; }

        public int StackIndex { get; }

        public int BlockIndex { get; }

        public double Score { get; }

        public string Reason { get; }

        public static SolverDecision NoMove => new(false, SolverActionKind.Place, -1, -1, double.NegativeInfinity, "No valid move");
    }

    public sealed class RandomStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Rand;

        public string DisplayName => "RAND";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            if (legalMoves.Length == 0)
            {
                return ChooseBestToolAction(state, context, "Random tool");
            }

            int selected = legalMoves[context.Random.Next(legalMoves.Length)];
            return new SolverDecision(true, selected, 0, "Random valid stack");
        }
    }

    public sealed class MergeFirstStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Merge;

        public string DisplayName => "MERG";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            return ChooseBestByScore(state, context, "Direct merge hunter", ScoreMergeFirst);
        }

        private static double ScoreMergeFirst(StackMergeGameState copy, MoveResult result, long scoreDelta)
        {
            double score = result.MergeCount * 500;
            score += FloorLog2(Math.Max(1, result.ResultingTopValue)) * 75;
            score += scoreDelta;
            score -= MaxHeight(copy) * 8;
            return score;
        }
    }

    public sealed class BalancedStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Balance;

        public string DisplayName => "BAL";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            return ChooseBestByScore(state, context, "Balanced stack risk", ScoreBalanced);
        }

        private static double ScoreBalanced(StackMergeGameState copy, MoveResult result, long scoreDelta)
        {
            int maxHeight = MaxHeight(copy);
            int minHeight = copy.Stacks.Min(stack => stack.Count);
            int dangerStacks = copy.Stacks.Count(stack => stack.Count >= copy.StackCapacity - 1);

            // BAL is deliberately a weak "easy to use, hard to miss" solver — consistent, not strong.
            // Tuned for LOW VARIANCE, not high average: stronger danger avoidance raises the floor
            // (fewer early crashes), weaker greedy score chasing lowers the ceiling (fewer risky
            // spikes), heavier evenness keeps play steady. NOTE: most of the run-to-run spread is
            // block-RNG, not the solver — pushing these weights harder than this stops helping
            // (benchmarked: min/max pin to the same seed-driven values), so this is the sweet spot.
            double score = 0;
            score += result.MergeCount * 160;
            score += scoreDelta * 0.32;
            score -= (maxHeight - minHeight) * 130;
            score -= maxHeight * maxHeight * 18;
            score -= dangerStacks * 360;
            score += FreeSlots(copy) * 20;
            return score;
        }
    }

    public sealed class HeuristicStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Heur;

        public string DisplayName => "HEUR";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            return ChooseHeuristicMove(state, context, "Heuristic best score");
        }

        internal static SolverDecision ChooseHeuristicMove(StackMergeGameState state, SolverContext context, string reason)
        {
            return ChooseBestByScore(state, context, reason, ScoreMove);
        }

        internal static double ScoreMove(StackMergeGameState state, MoveResult result, long scoreDelta)
        {
            int maxHeight = MaxHeight(state);
            int futureTopMatches = 0;
            int equalPairs = 0;

            foreach (var stack in state.Stacks)
            {
                if (stack.Count > 0 && state.NextBlocks.Contains(stack[^1]))
                {
                    futureTopMatches++;
                }

                for (int i = 1; i < stack.Count; i++)
                {
                    if (stack[i] == stack[i - 1])
                    {
                        equalPairs++;
                    }
                }
            }

            int dangerStacks = state.Stacks.Count(stack => stack.Count >= state.StackCapacity - 1);

            double score = 0;
            score += scoreDelta * 0.4;
            score += result.MergeCount * 180;
            score += FloorLog2(Math.Max(1, result.ResultingTopValue)) * 32;
            score += FreeSlots(state) * 14;
            score += futureTopMatches * 42;
            score += equalPairs * 55;
            score -= maxHeight * maxHeight * 7;
            score -= dangerStacks * 120;
            score += FloorLog2(Math.Max(1, state.HighestBlock)) * 20;
            return score;
        }
    }

    public sealed class LookaheadStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Look;

        public string DisplayName => "LOOK";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            SolverDecision best = ChooseBestToolAction(state, context, "Lookahead tool");
            foreach (int firstMove in legalMoves)
            {
                StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                long beforeScore = copy.Score;
                MoveResult firstResult = copy.PlaceNext(firstMove);
                if (!firstResult.Accepted)
                {
                    continue;
                }

                double score = HeuristicStackMergeSolver.ScoreMove(copy, firstResult, copy.Score - beforeScore);
                score += TuningScore(copy, firstResult, copy.Score - beforeScore, context);
                SolverDecision next = HeuristicStackMergeSolver.ChooseHeuristicMove(copy, context, "Follow-up");
                if (next.HasMove)
                {
                    StackMergeGameState secondCopy = copy.CreateSimulationCopy(context.Random.Next());
                    long secondBefore = secondCopy.Score;
                    MoveResult second = ApplyDecision(secondCopy, next);
                    if (second.Accepted)
                    {
                        double secondScore = HeuristicStackMergeSolver.ScoreMove(secondCopy, second, secondCopy.Score - secondBefore);
                        secondScore += TuningScore(secondCopy, second, secondCopy.Score - secondBefore, context);
                        score += secondScore * 0.72 * context.Tuning.Factor(SolverTuneParameterId.FollowUpWeight, 0.10);
                    }
                }

                score += context.Random.NextDouble() * 0.001;
                if (!best.HasMove || score > best.Score)
                {
                    best = new SolverDecision(true, firstMove, score, "Lookahead best line");
                }
            }

            return best;
        }
    }

    public class QueuePlannerStackMergeSolver : IStackMergeSolver
    {
        private readonly SolverId id;
        private readonly string displayName;
        private readonly int planningDepth;

        public QueuePlannerStackMergeSolver(SolverId id, string displayName, int planningDepth)
        {
            this.id = id;
            this.displayName = displayName;
            this.planningDepth = Math.Max(1, planningDepth);
        }

        public SolverId Id => id;

        public string DisplayName => displayName;

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int activeDepth = context.TunedPlanningDepth(planningDepth);
            return ChooseQueuePlan(state, context, activeDepth, $"Plans {activeDepth} blocks");
        }

        internal static SolverDecision ChooseQueuePlan(StackMergeGameState state, SolverContext context, int planningDepth, string reason)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            SolverDecision best = ChooseBestToolAction(state, context, reason);
            int depth = Math.Min(context.LimitPlanningDepth(planningDepth), state.NextBlocks.Count);
            foreach (int firstMove in legalMoves)
            {
                StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                long beforeScore = copy.Score;
                MoveResult result = copy.PlaceNext(firstMove);
                if (!result.Accepted)
                {
                    continue;
                }

                double score = HeuristicStackMergeSolver.ScoreMove(copy, result, copy.Score - beforeScore);
                score += TuningScore(copy, result, copy.Score - beforeScore, context);
                score += Search(copy, context, depth - 1) * 0.78 * context.Tuning.Factor(SolverTuneParameterId.FollowUpWeight, 0.10);
                score += context.Random.NextDouble() * 0.001;

                if (!best.HasMove || score > best.Score)
                {
                    best = new SolverDecision(true, firstMove, score, reason);
                }
            }

            return best;
        }

        internal static double Search(StackMergeGameState state, SolverContext context, int depth)
        {
            if (depth <= 0 || state.IsGameOver)
            {
                return EvaluateBoard(state);
            }

            int[] legalMoves = state.GetLegalMoveIndices();
            double best = double.NegativeInfinity;
            SolverDecision tool = ChooseBestToolAction(state, context, "Plan tool");
            if (tool.HasMove)
            {
                StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                long beforeScore = copy.Score;
                MoveResult result = ApplyDecision(copy, tool);
                if (result.Accepted)
                {
                    double score = ToolBoardScore(copy, context) - ToolBoardScore(state, context);
                    score += TuningScore(copy, result, copy.Score - beforeScore, context);
                    score += Search(copy, context, depth - 1) * 0.52;
                    best = score;
                }
            }

            foreach (int move in legalMoves)
            {
                StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                long beforeScore = copy.Score;
                MoveResult result = copy.PlaceNext(move);
                if (!result.Accepted)
                {
                    continue;
                }

                double score = HeuristicStackMergeSolver.ScoreMove(copy, result, copy.Score - beforeScore);
                score += TuningScore(copy, result, copy.Score - beforeScore, context);
                score += Search(copy, context, depth - 1) * 0.70 * context.Tuning.Factor(SolverTuneParameterId.FollowUpWeight, 0.10);
                if (score > best)
                {
                    best = score;
                }
            }

            return double.IsNegativeInfinity(best) ? EvaluateBoard(state) - 5000 : best;
        }

        internal static double EvaluateBoard(StackMergeGameState state)
        {
            int dangerStacks = state.Stacks.Count(stack => stack.Count >= state.StackCapacity - 1);
            int topMatches = state.Stacks.Count(stack => stack.Count > 0 && state.NextBlocks.Contains(stack[^1]));
            return state.Score * 0.1
                + FloorLog2(Math.Max(1, state.HighestBlock)) * 90
                + FreeSlots(state) * 24
                + topMatches * 80
                - MaxHeight(state) * MaxHeight(state) * 12
                - dangerStacks * 280;
        }
    }

    public sealed class Plan3StackMergeSolver : QueuePlannerStackMergeSolver
    {
        public Plan3StackMergeSolver() : base(SolverId.Plan3, "PLAN", 3)
        {
        }
    }

    public sealed class Plan5StackMergeSolver : QueuePlannerStackMergeSolver
    {
        public Plan5StackMergeSolver() : base(SolverId.Plan5, "PLAN-5", 5)
        {
        }
    }

    public sealed class AntiStallStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.AntiStall;

        public string DisplayName => "STALL";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            return ChooseBestByScore(state, context, "Avoiding stall", ScoreAntiStall);
        }

        internal static double ScoreAntiStall(StackMergeGameState state, MoveResult result, long scoreDelta)
        {
            int legalMoves = state.GetLegalMoveIndices().Length;
            int dangerStacks = state.Stacks.Count(stack => stack.Count >= state.StackCapacity - 1);
            int emptyStacks = state.Stacks.Count(stack => stack.Count == 0);
            int breathingStacks = state.Stacks.Count(stack => stack.Count <= Math.Max(1, state.StackCapacity - 3));
            int maxHeight = MaxHeight(state);
            int minHeight = state.Stacks.Min(stack => stack.Count);

            // Survival IS merge availability: the board only empties through merges, so the
            // longest runs come from merging often (at any tile value) while keeping the queue
            // matched — not from passively hoarding space. scoreDelta stays low on purpose:
            // STALL's identity is run LENGTH, not chip value per merge.
            double score = 0;
            score += legalMoves * 260;
            score += FreeSlots(state) * 34;
            score += emptyStacks * 210;
            score += breathingStacks * 120;
            score += result.MergeCount * 620;
            score += CountQueueTopMatches(state) * 170;
            score += CountAdjacentEqualPairs(state) * 60;
            score += scoreDelta * 0.25;
            score -= dangerStacks * 520;
            score -= maxHeight * maxHeight * 18;
            score -= (maxHeight - minHeight) * 80;

            if (legalMoves <= 1)
            {
                score -= 1800;
            }
            else if (legalMoves == 2)
            {
                score -= 500;
            }

            if (state.IsGameOver)
            {
                score -= 10000;
            }

            return score;
        }
    }

    public sealed class ComboFocusedStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Combo;

        public string DisplayName => "COMBO";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int futureDepth = context.LightweightMode ? 1 : context.TunedFutureDepth(3);
            return ChooseBestByScore(state, context, "Combo setup", (copy, result, scoreDelta) => ScoreComboMove(copy, result, scoreDelta, futureDepth));
        }

        internal static double ScoreComboMove(StackMergeGameState state, MoveResult result, long scoreDelta)
        {
            return ScoreComboMove(state, result, scoreDelta, 3);
        }

        private static double ScoreComboMove(StackMergeGameState state, MoveResult result, long scoreDelta, int futureDepth)
        {
            double score = 0;
            score += result.MergeCount * 260;
            score += FloorLog2(Math.Max(1, result.ResultingTopValue)) * 52;
            score += scoreDelta * 0.35;
            score += CountAdjacentEqualPairs(state) * 90;
            score += CountQueueTopMatches(state) * 80;
            score += EstimateComboFuture(state, futureDepth) * 0.82;
            score += FreeSlots(state) * 8;
            score -= MaxHeight(state) * 6;
            return score;
        }

        private static double EstimateComboFuture(StackMergeGameState state, int depth)
        {
            if (depth <= 0 || state.IsGameOver)
            {
                return CountAdjacentEqualPairs(state) * 40 + CountQueueTopMatches(state) * 36;
            }

            double best = double.NegativeInfinity;
            foreach (int move in state.GetLegalMoveIndices())
            {
                StackMergeGameState copy = state.CreateSimulationCopy(move + depth * 97);
                long beforeScore = copy.Score;
                MoveResult result = copy.PlaceNext(move);
                if (!result.Accepted)
                {
                    continue;
                }

                double score = result.MergeCount * 380;
                score += FloorLog2(Math.Max(1, result.ResultingTopValue)) * 45;
                score += (copy.Score - beforeScore) * 0.2;
                score += CountAdjacentEqualPairs(copy) * 70;
                score += CountQueueTopMatches(copy) * 55;
                score += EstimateComboFuture(copy, depth - 1) * 0.58;
                if (score > best)
                {
                    best = score;
                }
            }

            return double.IsNegativeInfinity(best) ? -1000 : best;
        }
    }

    public sealed class MachineLearningStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.MachineLearning;

        public string DisplayName => "PPO";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            if (context.MachineLearningAgent != null)
            {
                context.MachineLearningAgent.ApplyTuning(context.Tuning);
                return context.MachineLearningAgent.ChooseMove(state, context.Random, context.MachineLearningTrainingMode);
            }

            int[] legalMoves = state.GetLegalMoveIndices();
            if (legalMoves.Length == 0)
            {
                return SolverDecision.NoMove;
            }

            int randomMove = legalMoves[context.Random.Next(legalMoves.Length)];
            return new SolverDecision(true, randomMove, 0, "PPO uninitialized fallback");
        }
    }

    public sealed class MonteCarloStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Moca;

        public string DisplayName => "MOCA";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            SolverDecision best = ChooseBestToolAction(state, context, "Monte Carlo tool");
            // MOCA is the "smart sampler" — but its old scoring only counted points grabbed during
            // the rollout (score-delta), so it played myopically and under-scored the planners it is
            // meant to rival. Now each rollout is judged by the QUALITY of the surviving board plus
            // the peak tier it reached, so MOCA seeks sustainable, height-building lines. Rollouts run
            // deeper and more numerous than before; the policy stays HEUR (fast) — the per-move queue
            // planning is what makes MOCA+ ~60x slower for a marginal gain, so MOCA deliberately skips it.
            // Real-time (in-game) budget is capped tight so MOCA stays at 120fps on phones — each
            // rollout step runs a full HEUR scan over every legal move, so cost is ~sims×depth×moves²;
            // 3×2 keeps it smooth. The benchmark (full mode) keeps the larger budget for measured strength.
            int simulations = context.LightweightMode
                ? Math.Clamp(context.TunedSimulationCount(), 2, 3)
                : Math.Max(6, context.TunedSimulationCount() + 2);
            int rolloutDepth = context.LightweightMode
                ? Math.Clamp(context.TunedRolloutDepth(), 1, 2)
                : Math.Max(5, context.TunedRolloutDepth() + 2);
            foreach (int firstMove in legalMoves)
            {
                double totalScore = 0;
                int successfulRuns = 0;

                for (int i = 0; i < simulations; i++)
                {
                    StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                    long startScore = copy.Score;
                    MoveResult firstResult = copy.PlaceNext(firstMove);
                    if (!firstResult.Accepted)
                    {
                        continue;
                    }

                    int peakHigh = copy.HighestBlock;
                    for (int depth = 0; depth < rolloutDepth && !copy.IsGameOver; depth++)
                    {
                        SolverDecision rolloutMove = HeuristicStackMergeSolver.ChooseHeuristicMove(copy, context, "Rollout");
                        if (!rolloutMove.HasMove)
                        {
                            break;
                        }

                        ApplyDecision(copy, rolloutMove);
                        peakHigh = Math.Max(peakHigh, copy.HighestBlock);
                    }

                    // Score-SEEKING terminal signal: keep the points earned over the rollout (the
                    // original working signal) and add a strong peak-tier reward so MOCA actively
                    // builds toward new highs. Deliberately does NOT use the survival-biased
                    // EvaluateBoard — its -maxHeight² term punishes the tall stacks MOCA must build
                    // to reach high tiers (that made an earlier rework play timid and score LESS).
                    // Only a light free-space term and a death penalty keep it from suiciding.
                    double runScore = copy.Score - startScore;
                    runScore += FloorLog2(Math.Max(1, peakHigh)) * 220;
                    runScore += FreeSlots(copy) * 10;
                    if (copy.IsGameOver)
                    {
                        runScore -= 6000;
                    }

                    double firstMoveScore = HeuristicStackMergeSolver.ScoreMove(copy, firstResult, copy.Score - startScore);
                    firstMoveScore += TuningScore(copy, firstResult, copy.Score - startScore, context);
                    runScore += firstMoveScore * 0.3;
                    totalScore += runScore;
                    successfulRuns++;
                }

                if (successfulRuns == 0)
                {
                    continue;
                }

                double average = totalScore / successfulRuns;
                average += context.Random.NextDouble() * 0.001;
                if (!best.HasMove || average > best.Score)
                {
                    best = new SolverDecision(true, firstMove, average, $"{simulations} simulations");
                }
            }

            return best;
        }
    }

    public sealed class EnhancedMonteCarloStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.MocaPlus;

        public string DisplayName => "MOCA+";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            SolverDecision best = ChooseBestToolAction(state, context, "Enhanced Monte Carlo tool");
            // Real-time (lightweight) play caps the sample/rollout budget so MOCA+ stays smooth;
            // the benchmark (full mode) keeps the larger budget for measured strength.
            int simulations = context.LightweightMode ? Math.Min(5, context.TunedSimulationCount()) : Math.Max(4, context.TunedSimulationCount());
            int rolloutDepth = context.LightweightMode ? Math.Min(3, context.TunedRolloutDepth()) : Math.Min(4, Math.Max(2, context.TunedRolloutDepth() + 1));
            int smartPlanningDepth = context.LightweightMode ? 1 : context.TunedRolloutPlanningDepth(3);

            foreach (int firstMove in legalMoves)
            {
                double totalScore = 0;
                int successfulRuns = 0;

                for (int i = 0; i < simulations; i++)
                {
                    StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                    long startScore = copy.Score;
                    MoveResult firstResult = copy.PlaceNext(firstMove);
                    if (!firstResult.Accepted)
                    {
                        continue;
                    }

                    for (int depth = 0; depth < rolloutDepth && !copy.IsGameOver; depth++)
                    {
                        SolverDecision rolloutMove = QueuePlannerStackMergeSolver.ChooseQueuePlan(copy, context, smartPlanningDepth, "Smart rollout");
                        if (!rolloutMove.HasMove)
                        {
                            break;
                        }

                        ApplyDecision(copy, rolloutMove);
                    }

                    double runScore = copy.Score - startScore;
                    runScore += QueuePlannerStackMergeSolver.EvaluateBoard(copy) * 0.20 * context.Tuning.Factor(SolverTuneParameterId.BoardEvaluation, 0.12);
                    runScore += AntiStallStackMergeSolver.ScoreAntiStall(copy, firstResult, copy.Score - startScore) * 0.18 * context.Tuning.Factor(SolverTuneParameterId.AntiStallPressure, 0.12);
                    runScore += TuningScore(copy, firstResult, copy.Score - startScore, context) * 0.35;
                    totalScore += runScore;
                    successfulRuns++;
                }

                if (successfulRuns == 0)
                {
                    continue;
                }

                double average = totalScore / successfulRuns;
                average += context.Random.NextDouble() * 0.001;
                if (!best.HasMove || average > best.Score)
                {
                    best = new SolverDecision(true, firstMove, average, $"{simulations} smart simulations");
                }
            }

            return best;
        }
    }

    public sealed class MctsStackMergeSolver : IStackMergeSolver
    {
        public SolverId Id => SolverId.Mcts;

        public string DisplayName => "MCTS";

        public SolverDecision ChooseMove(StackMergeGameState state, SolverContext context)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            SolverDecision toolDecision = ChooseBestToolAction(state, context, "MCTS tool");
            if (legalMoves.Length == 0)
            {
                return toolDecision;
            }

            StackMergeSnapshot rootSnapshot = state.CreateSnapshot();
            var root = new SearchNode(null, rootSnapshot, legalMoves, -1, 0);

            // Iterations are modest because each one is cheap: it expands a single node and scores
            // it with a strong STATIC board heuristic (the same evaluation the strong queue planner
            // uses) after a short allocation-free playout. There is no per-candidate heavy scoring
            // during expansion and no heavy heuristic rollout — those were what made MCTS slow.
            int iterations = context.LightweightMode
                ? Math.Max(24, context.MonteCarloSimulations * 3)
                : Math.Max(60, context.MonteCarloSimulations * 6);
            iterations = context.TunedTreeIterations(iterations);
            int playoutDepth = context.LightweightMode ? 2 : 3;

            // Single reusable scratch state: restored from a node snapshot each iteration, then
            // mutated freely during the playout (discarded on the next restore).
            StackMergeGameState working = state.CreateSimulationCopy(context.Random.Next());

            for (int i = 0; i < iterations; i++)
            {
                SearchNode node = root;
                while (node.UntriedMoves.Count == 0 && node.Children.Count > 0)
                {
                    node = SelectChild(node, context);
                }

                working.RestoreSnapshot(node.Snapshot);

                if (node.UntriedMoves.Count > 0 && !working.IsGameOver)
                {
                    int lastIndex = node.UntriedMoves.Count - 1;
                    int move = node.UntriedMoves[lastIndex];
                    node.UntriedMoves.RemoveAt(lastIndex);

                    MoveResult result = working.PlaceNext(move);
                    if (result.Accepted)
                    {
                        var child = new SearchNode(node, working.CreateSnapshot(), working.GetLegalMoveIndices(), move, 0);
                        node.Children.Add(child);
                        node = child;
                    }
                }

                double value = LeafEvaluate(working, context, playoutDepth);
                while (node != null)
                {
                    node.Visits++;
                    node.Value += value;
                    node = node.Parent;
                }
            }

            // Robust child: pick the most-visited root move (standard MCTS final selection),
            // which is scale-free, with average value as the tiebreak.
            SearchNode bestChild = null;
            foreach (SearchNode child in root.Children)
            {
                if (bestChild == null
                    || child.Visits > bestChild.Visits
                    || (child.Visits == bestChild.Visits && child.AverageValue > bestChild.AverageValue))
                {
                    bestChild = child;
                }
            }

            if (bestChild == null)
            {
                return toolDecision;
            }

            return new SolverDecision(true, bestChild.MoveFromParent, bestChild.AverageValue, $"{iterations} tree visits");
        }

        private static SearchNode SelectChild(SearchNode node, SolverContext context)
        {
            // Normalize the exploitation term to ~[0,1] using the sibling value range so the
            // UCT exploration term is on a comparable scale. Raw rollout rewards are in the
            // thousands, which previously drowned out exploration entirely and collapsed the
            // search into greedy first-rollout selection.
            double minValue = double.PositiveInfinity;
            double maxValue = double.NegativeInfinity;
            foreach (SearchNode child in node.Children)
            {
                double value = child.AverageValue;
                if (value < minValue)
                {
                    minValue = value;
                }

                if (value > maxValue)
                {
                    maxValue = value;
                }
            }

            double range = maxValue - minValue;
            if (range < 1e-9)
            {
                range = 1.0;
            }

            double logParent = Math.Log(Math.Max(1, node.Visits) + 1);
            double explorationWeight = 1.4 * context.Tuning.Factor(SolverTuneParameterId.Exploration, 0.10);

            SearchNode best = null;
            double bestScore = double.NegativeInfinity;
            foreach (SearchNode child in node.Children)
            {
                double exploit = (child.AverageValue - minValue) / range;
                double explore = Math.Sqrt(logParent / Math.Max(1, child.Visits));
                double score = exploit + explorationWeight * explore + context.Random.NextDouble() * 1e-4;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = child;
                }
            }

            return best ?? node.Children[0];
        }

        /// <summary>
        /// Leaf value = a strong STATIC board heuristic after a short, cheap playout. The static
        /// evaluation (the queue planner's board score) is exactly what makes the lookahead solvers
        /// strong; using it as the tree's leaf value — instead of a weak random rollout — keeps MCTS
        /// fast AND competitive, while the tree itself supplies the adaptive lookahead depth.
        /// </summary>
        private static double LeafEvaluate(StackMergeGameState state, SolverContext context, int playoutDepth)
        {
            for (int i = 0; i < playoutDepth && !state.IsGameOver; i++)
            {
                int move = FastPlayoutMove(state);
                if (move < 0)
                {
                    break;
                }

                state.PlaceNext(move);
            }

            double value = QueuePlannerStackMergeSolver.EvaluateBoard(state);
            if (!context.Tuning.IsNeutral)
            {
                value += TuningBoardScore(state, context) * 0.45;
            }

            return value - (state.IsGameOver ? 6000 : 0);
        }

        /// <summary>
        /// Allocation-free playout policy: take an immediate merge if one exists, otherwise drop on
        /// the shortest stack to keep the board alive. Returns -1 when nothing can be placed.
        /// </summary>
        private static int FastPlayoutMove(StackMergeGameState state)
        {
            int next = state.NextBlocks.Count > 0 ? state.NextBlocks[0] : -1;
            int shortest = -1;
            int shortestHeight = int.MaxValue;

            for (int move = 0; move < state.StackCount; move++)
            {
                if (!state.CanPlace(move))
                {
                    continue;
                }

                IReadOnlyList<int> stack = state.Stacks[move];
                if (stack.Count > 0)
                {
                    int top = stack[^1];
                    bool merges = top == next
                        || (state.JokerBlocksEnabled && next == StackMergeGameState.JokerBlockValue && top > 0)
                        || (state.MirrorStackEnabled && stack[0] == next);
                    if (merges)
                    {
                        return move;
                    }
                }

                if (stack.Count < shortestHeight)
                {
                    shortestHeight = stack.Count;
                    shortest = move;
                }
            }

            return shortest;
        }

        private sealed class SearchNode
        {
            public SearchNode(SearchNode parent, StackMergeSnapshot snapshot, int[] legalMoves, int moveFromParent, double priorScore)
            {
                Parent = parent;
                Snapshot = snapshot;
                UntriedMoves = new List<int>(legalMoves);
                MoveFromParent = moveFromParent;
                PriorScore = priorScore;
            }

            public SearchNode Parent { get; }

            public StackMergeSnapshot Snapshot { get; }

            public List<int> UntriedMoves { get; }

            public List<SearchNode> Children { get; } = new();

            public int MoveFromParent { get; }

            public double PriorScore { get; }

            public int Visits { get; set; }

            public double Value { get; set; }

            public double AverageValue => Visits == 0 ? 0 : Value / Visits;
        }
    }

    public static class SolverScoring
    {
        public static SolverDecision ChooseBestByScore(
            StackMergeGameState state,
            SolverContext context,
            string reason,
            Func<StackMergeGameState, MoveResult, long, double> scoreFunction)
        {
            int[] legalMoves = state.GetLegalMoveIndices();
            SolverDecision best = ChooseBestToolAction(state, context, reason);
            foreach (int move in legalMoves)
            {
                StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                long beforeScore = copy.Score;
                MoveResult result = copy.PlaceNext(move);
                if (!result.Accepted)
                {
                    continue;
                }

                long scoreDelta = copy.Score - beforeScore;
                double score = scoreFunction(copy, result, scoreDelta);
                score += TuningScore(copy, result, scoreDelta, context);
                score += context.Random.NextDouble() * 0.001;
                if (!best.HasMove || score > best.Score)
                {
                    string moveReason = result.MergeCount > 0 ? $"Merge x{result.MergeCount}" : reason;
                    best = new SolverDecision(true, move, score, moveReason);
                }
            }

            return best;
        }

        public static bool CanApplyDecision(StackMergeGameState state, SolverDecision decision)
        {
            if (!decision.HasMove)
            {
                return false;
            }

            return decision.ActionKind switch
            {
                SolverActionKind.Place => decision.StackIndex >= 0 && decision.StackIndex < state.StackCount && state.CanPlace(decision.StackIndex),
                SolverActionKind.Pickaxe => state.CanUsePickaxe(decision.StackIndex, decision.BlockIndex),
                SolverActionKind.QueueSkip => state.CanSkipNextBlock(),
                _ => false
            };
        }

        public static MoveResult ApplyDecision(StackMergeGameState state, SolverDecision decision)
        {
            return decision.ActionKind switch
            {
                SolverActionKind.Place => state.PlaceNext(decision.StackIndex),
                SolverActionKind.Pickaxe => state.UsePickaxe(decision.StackIndex, decision.BlockIndex),
                SolverActionKind.QueueSkip => state.SkipNextBlock(),
                _ => MoveResult.Rejected(decision.StackIndex, "Unknown action")
            };
        }

        public static SolverDecision ChooseBestToolAction(StackMergeGameState state, SolverContext context, string reason)
        {
            SolverDecision best = SolverDecision.NoMove;
            bool emergency = !state.HasLegalMove();
            int beforeLegalMoves = state.GetLegalMoveIndices().Length;
            int beforeFreeSlots = FreeSlots(state);
            int beforeQueueMatches = CountQueueTopMatches(state);
            int currentNext = state.NextBlocks.Count > 0 ? state.NextBlocks[0] : 0;
            int currentNextMatches = CountStacksWithTopValue(state, currentNext);
            double beforeToolScore = ToolBoardScore(state, context);

            if (state.CanSkipNextBlock())
            {
                StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                MoveResult result = copy.SkipNextBlock();
                if (result.Accepted)
                {
                    int newNext = copy.NextBlocks.Count > 0 ? copy.NextBlocks[0] : 0;
                    int newNextMatches = CountStacksWithTopValue(copy, newNext);
                    double score = ToolBoardScore(copy, context) - beforeToolScore;
                    score += (copy.GetLegalMoveIndices().Length - beforeLegalMoves) * 620;
                    score += (CountQueueTopMatches(copy) - beforeQueueMatches) * 360;
                    score += (newNextMatches - currentNextMatches) * 420;
                    score += CountVisibleQueueContinuity(copy) * 120;
                    score += emergency ? 2600 : newNextMatches > currentNextMatches ? 280 : -120;
                    score += context.Tuning.GetTunedValue(SolverTuneParameterId.QueueFit) * 420;
                    score -= context.Tuning.GetTunedValue(SolverTuneParameterId.MergeReward) * currentNextMatches * 160;
                    score += context.Random.NextDouble() * 0.001;
                    best = new SolverDecision(true, SolverActionKind.QueueSkip, -1, -1, score, "Queue scrubber");
                }
            }

            for (int stackIndex = 0; stackIndex < state.StackCount; stackIndex++)
            {
                IReadOnlyList<int> stack = state.Stacks[stackIndex];
                for (int blockIndex = 0; blockIndex < stack.Count; blockIndex++)
                {
                    if (!state.CanUsePickaxe(stackIndex, blockIndex))
                    {
                        continue;
                    }

                    StackMergeGameState copy = state.CreateSimulationCopy(context.Random.Next());
                    long beforeScore = copy.Score;
                    MoveResult result = copy.UsePickaxe(stackIndex, blockIndex);
                    if (!result.Accepted)
                    {
                        continue;
                    }

                    int removedValue = Math.Max(1, result.RemovedValue);
                    double score = ToolBoardScore(copy, context) - beforeToolScore;
                    score += (copy.Score - beforeScore) * 0.8;
                    score += result.MergeCount * 1850;
                    score += (copy.GetLegalMoveIndices().Length - beforeLegalMoves) * 760;
                    score += (FreeSlots(copy) - beforeFreeSlots) * 180;
                    score += stack.Count >= state.StackCapacity - 1 ? 1050 : stack.Count >= state.StackCapacity - 2 ? 360 : 0;
                    score += blockIndex > 0 && blockIndex < stack.Count - 1 ? 280 : 0;
                    score -= FloorLog2(removedValue) * 22;
                    score += emergency ? 3200 : result.MergeCount > 0 ? 240 : -180;
                    score += context.Tuning.GetTunedValue(SolverTuneParameterId.FreeSpace) * 300;
                    score += context.Tuning.GetTunedValue(SolverTuneParameterId.SafetyCushion) * 360;
                    score -= context.Tuning.GetTunedValue(SolverTuneParameterId.HighBlockValue) * FloorLog2(removedValue) * 40;
                    score += context.Random.NextDouble() * 0.001;

                    if (!best.HasMove || score > best.Score)
                    {
                        best = new SolverDecision(true, SolverActionKind.Pickaxe, stackIndex, blockIndex, score, $"Pickaxe {removedValue}");
                    }
                }
            }

            return best;
        }

        public static double ToolBoardScore(StackMergeGameState state, SolverContext context)
        {
            int dangerStacks = state.Stacks.Count(stack => stack.Count >= state.StackCapacity - 1);
            int legalMoves = state.GetLegalMoveIndices().Length;
            return QueuePlannerStackMergeSolver.EvaluateBoard(state)
                + TuningBoardScore(state, context) * 0.70
                + legalMoves * 230
                + FreeSlots(state) * 36
                - dangerStacks * 320
                - (state.IsGameOver ? 5000 : 0);
        }

        private static int CountStacksWithTopValue(StackMergeGameState state, int value)
        {
            if (value <= 0)
            {
                return 0;
            }

            return state.Stacks.Count(stack => stack.Count > 0 && stack[^1] == value);
        }

        public static double TuningScore(StackMergeGameState state, MoveResult result, long scoreDelta, SolverContext context)
        {
            SolverTuningSettings tuning = context.Tuning;
            if (tuning.IsNeutral)
            {
                return 0;
            }

            int topLog = FloorLog2(Math.Max(1, result.ResultingTopValue));
            int maxHeight = MaxHeight(state);
            int minHeight = state.Stacks.Min(stack => stack.Count);
            int dangerStacks = state.Stacks.Count(stack => stack.Count >= state.StackCapacity - 1);
            int legalMoves = state.GetLegalMoveIndices().Length;
            int emptyStacks = state.Stacks.Count(stack => stack.Count == 0);

            double score = 0;
            score += tuning.GetTunedValue(SolverTuneParameterId.ScoreDelta) * scoreDelta * 0.34;
            score += tuning.GetTunedValue(SolverTuneParameterId.MergeReward) * (result.MergeCount * 285 + topLog * 58 + scoreDelta * 0.06);
            score += tuning.GetTunedValue(SolverTuneParameterId.HighBlockValue) * (FloorLog2(Math.Max(1, state.HighestBlock)) * 112 + topLog * 48);
            score += tuning.GetTunedValue(SolverTuneParameterId.FreeSpace) * (FreeSlots(state) * 50 + emptyStacks * 165 + legalMoves * 48);
            score -= tuning.GetTunedValue(SolverTuneParameterId.Smoothness) * ((maxHeight - minHeight) * 128 + maxHeight * 24 + maxHeight * maxHeight * 4);
            score -= tuning.GetTunedValue(SolverTuneParameterId.DangerPenalty) * (dangerStacks * 540 + (legalMoves <= 1 ? 1050 : 0) + (state.IsGameOver ? 6500 : 0));
            score += tuning.GetTunedValue(SolverTuneParameterId.QueueFit) * (CountQueueTopMatches(state) * 245 + CountVisibleQueueContinuity(state) * 118 + CountAdjacentEqualPairs(state) * 62);
            score += tuning.GetTunedValue(SolverTuneParameterId.PairSetup) * CountAdjacentEqualPairs(state) * 205;
            score += tuning.GetTunedValue(SolverTuneParameterId.AntiStallPressure) * (
                legalMoves * 120 + FreeSlots(state) * 16 - dangerStacks * 220 - (state.IsGameOver ? 3600 : 0));
            score += tuning.GetTunedValue(SolverTuneParameterId.ComboSetup) * (
                CountAdjacentEqualPairs(state) * 128 + CountQueueTopMatches(state) * 74 + result.MergeCount * 58);
            score += tuning.GetTunedValue(SolverTuneParameterId.SafetyCushion) * (
                FreeSlots(state) * 24 + legalMoves * 92 - dangerStacks * 235 - maxHeight * 18);
            score += tuning.GetTunedValue(SolverTuneParameterId.BoardEvaluation) * QueuePlannerStackMergeSolver.EvaluateBoard(state) * 0.20;
            return score;
        }

        public static double TuningBoardScore(StackMergeGameState state, SolverContext context)
        {
            SolverTuningSettings tuning = context.Tuning;
            if (tuning.IsNeutral)
            {
                return 0;
            }

            int maxHeight = MaxHeight(state);
            int minHeight = state.Stacks.Min(stack => stack.Count);
            int dangerStacks = state.Stacks.Count(stack => stack.Count >= state.StackCapacity - 1);
            int legalMoves = state.GetLegalMoveIndices().Length;

            double score = 0;
            score += tuning.GetTunedValue(SolverTuneParameterId.FreeSpace) * (FreeSlots(state) * 58 + legalMoves * 70);
            score += tuning.GetTunedValue(SolverTuneParameterId.SafetyCushion) * (
                FreeSlots(state) * 28
                + legalMoves * 120
                - dangerStacks * 310
                - (maxHeight - minHeight) * 46
                - maxHeight * maxHeight * 7);
            score -= tuning.GetTunedValue(SolverTuneParameterId.DangerPenalty) * (dangerStacks * 680 + maxHeight * 28 + (legalMoves <= 1 ? 900 : 0));
            score += tuning.GetTunedValue(SolverTuneParameterId.QueueFit) * (
                CountQueueTopMatches(state) * 135
                + CountAdjacentEqualPairs(state) * 95
                + CountVisibleQueueContinuity(state) * 72);
            score += tuning.GetTunedValue(SolverTuneParameterId.ComboSetup) * (
                CountAdjacentEqualPairs(state) * 116 + CountQueueTopMatches(state) * 62);
            score += tuning.GetTunedValue(SolverTuneParameterId.HighBlockValue) * FloorLog2(Math.Max(1, state.HighestBlock)) * 92;
            return score;
        }

        public static int MaxHeight(StackMergeGameState state)
        {
            return state.Stacks.Max(stack => stack.Count);
        }

        public static int FreeSlots(StackMergeGameState state)
        {
            return state.StackCount * state.StackCapacity - state.Stacks.Sum(stack => stack.Count);
        }

        public static int CountQueueTopMatches(StackMergeGameState state)
        {
            return state.Stacks.Count(stack => stack.Count > 0 && state.NextBlocks.Contains(stack[^1]));
        }

        public static int CountAdjacentEqualPairs(StackMergeGameState state)
        {
            int pairs = 0;
            foreach (var stack in state.Stacks)
            {
                for (int i = 1; i < stack.Count; i++)
                {
                    if (stack[i] == stack[i - 1])
                    {
                        pairs++;
                    }
                }
            }

            return pairs;
        }

        private static int CountVisibleQueueContinuity(StackMergeGameState state)
        {
            int continuity = 0;
            for (int i = 1; i < state.NextBlocks.Count; i++)
            {
                if (state.NextBlocks[i] == state.NextBlocks[i - 1])
                {
                    continuity++;
                }
            }

            return continuity;
        }

        public static int FloorLog2(int value)
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
