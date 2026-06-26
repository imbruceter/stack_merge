using System;
using System.Linq;
using NUnit.Framework;

namespace StackMerge.Tests
{
    public sealed class StackMergeGameStateTests
    {
        private static StackMergePpoTrainingData CreateInitializedPpoPolicy(int steps, int updates, int episodes, long bestScore, int bestHigh)
        {
            var data = new StackMergePpoTrainingData();
            _ = new StackMergePpoAgent(data);
            data.steps = steps;
            data.updates = updates;
            data.episodes = episodes;
            data.bestScore = bestScore;
            data.bestHigh = bestHigh;
            return data;
        }

        [Test]
        public void NewGame_UsesLowerStarterStackCapacity()
        {
            var state = new StackMergeGameState(seed: 10);

            Assert.That(state.StackCapacity, Is.EqualTo(5));
            Assert.That(state.HighestMergedBlock, Is.EqualTo(0));
        }

        [Test]
        public void PlaceNext_MergesTwoEqualTopBlocks()
        {
            var state = new StackMergeGameState(seed: 1);
            state.SetNextBlocksForTesting(1, 2, 1);
            state.SetStacksForTesting(
                new[] { 1 },
                new int[] { },
                new int[] { },
                new int[] { });

            MoveResult result = state.PlaceNext(0);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.MergeCount, Is.EqualTo(1));
            Assert.That(state.Stacks[0], Is.EqualTo(new[] { 2 }));
            Assert.That(state.Score, Is.EqualTo(3));
            Assert.That(state.HighestBlock, Is.EqualTo(2));
            Assert.That(state.TotalMerges, Is.EqualTo(1));
            Assert.That(state.HighestMergedBlock, Is.EqualTo(2));
        }

        [Test]
        public void PlaceNext_ResolvesCascadeMergesFromTopDown()
        {
            var state = new StackMergeGameState(stackCapacity: 10, seed: 2);
            state.SetNextBlocksForTesting(1, 1, 1);
            state.SetStacksForTesting(
                new[] { 8, 4, 2, 1 },
                new int[] { },
                new int[] { },
                new int[] { });

            MoveResult result = state.PlaceNext(0);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.MergeCount, Is.EqualTo(4));
            Assert.That(state.Stacks[0], Is.EqualTo(new[] { 16 }));
            Assert.That(state.HighestBlock, Is.EqualTo(16));
        }

        [Test]
        public void CanPlace_AllowsFullStackWhenTopWillImmediatelyMerge()
        {
            var state = new StackMergeGameState(stackCapacity: 3, seed: 3);
            state.SetNextBlocksForTesting(2, 1, 1);
            state.SetStacksForTesting(
                new[] { 4, 2, 2 },
                new int[] { },
                new int[] { },
                new int[] { });

            Assert.That(state.CanPlace(0), Is.True);

            MoveResult result = state.PlaceNext(0);

            Assert.That(result.Accepted, Is.True);
            Assert.That(state.Stacks[0], Is.EqualTo(new[] { 4, 2, 4 }));
            Assert.That(state.Stacks[0].Count, Is.EqualTo(3));
        }

        [Test]
        public void SetStacksForTesting_MarksGameOverWhenNoStackCanAcceptNextBlock()
        {
            var state = new StackMergeGameState(stackCapacity: 2, seed: 4);
            state.SetNextBlocksForTesting(1, 2, 4);
            state.SetStacksForTesting(
                new[] { 2, 4 },
                new[] { 4, 8 },
                new[] { 8, 16 },
                new[] { 16, 32 });

            Assert.That(state.IsGameOver, Is.True);
            Assert.That(state.HasLegalMove(), Is.False);
        }

        [Test]
        public void HeuristicSolver_PrefersImmediateMerge()
        {
            var state = new StackMergeGameState(seed: 5);
            state.SetNextBlocksForTesting(1, 2, 1);
            state.SetStacksForTesting(
                new[] { 1 },
                new[] { 4 },
                new[] { 8 },
                new int[] { });

            var solver = new HeuristicStackMergeSolver();
            SolverDecision decision = solver.ChooseMove(state, new SolverContext(new Random(5), 4, 4));

            Assert.That(decision.HasMove, Is.True);
            Assert.That(decision.StackIndex, Is.EqualTo(0));
        }

        [Test]
        public void SolverCatalog_OffersThirteenUnlockableAlgorithms()
        {
            Assert.That(StackMergeSolverCatalog.Definitions.Length, Is.EqualTo(13));
            Assert.That(StackMergeSolverCatalog.Definitions[0].Id, Is.EqualTo(SolverId.Rand));
            Assert.That(StackMergeSolverCatalog.Definitions[5].Id, Is.EqualTo(SolverId.Moca));
            Assert.That(StackMergeSolverCatalog.Definitions[6].Id, Is.EqualTo(SolverId.Plan3));
            Assert.That(StackMergeSolverCatalog.Definitions[7].Id, Is.EqualTo(SolverId.Plan5));
            Assert.That(StackMergeSolverCatalog.Definitions[8].Id, Is.EqualTo(SolverId.MocaPlus));
            Assert.That(StackMergeSolverCatalog.Definitions[9].Id, Is.EqualTo(SolverId.Mcts));
            Assert.That(StackMergeSolverCatalog.Definitions[10].Id, Is.EqualTo(SolverId.AntiStall));
            Assert.That(StackMergeSolverCatalog.Definitions[11].Id, Is.EqualTo(SolverId.Combo));
            Assert.That(StackMergeSolverCatalog.Definitions[12].Id, Is.EqualTo(SolverId.MachineLearning));
        }

        [Test]
        public void SolverFactory_CreatesAllCatalogSolversInOrder()
        {
            IStackMergeSolver[] solvers = StackMergeSolverFactory.CreateAll();

            Assert.That(solvers.Length, Is.EqualTo(StackMergeSolverCatalog.Definitions.Length));
            for (int i = 0; i < solvers.Length; i++)
            {
                Assert.That(solvers[i].Id, Is.EqualTo(StackMergeSolverCatalog.Definitions[i].Id));
            }
        }

        [Test]
        public void PpoSolver_RecordsTrajectoryAndUpdatesPolicy()
        {
            var state = new StackMergeGameState(seed: 77);
            var agent = new StackMergePpoAgent(new StackMergePpoTrainingData(), 77);
            var solver = new MachineLearningStackMergeSolver();

            for (int i = 0; i < 64 && !state.IsGameOver; i++)
            {
                var context = new SolverContext(
                    new Random(77 + i),
                    1,
                    1,
                    machineLearningAgent: agent,
                    machineLearningTrainingMode: true);
                SolverDecision decision = solver.ChooseMove(state, context);

                Assert.That(SolverScoring.CanApplyDecision(state, decision), Is.True);
                MoveResult result = SolverScoring.ApplyDecision(state, decision);
                Assert.That(result.Accepted, Is.True);
                agent.Observe(result, state, true);
            }

            agent.ForceUpdate(true);

            Assert.That(agent.Metrics.Steps, Is.GreaterThan(0));
            Assert.That(agent.Metrics.Updates, Is.GreaterThan(0));
        }

        [Test]
        public void HighTierAccelerator_HalvesExpensiveSolverCompute()
        {
            var neutral = new SolverContext(
                new Random(1),
                8,
                6,
                tuning: SolverTuningSettings.Neutral(SolverId.Mcts));
            var accelerated = new SolverContext(
                new Random(1),
                8,
                6,
                tuning: SolverTuningSettings.Neutral(SolverId.Mcts),
                highTierSpeedTuningAccelerator: true);

            Assert.That(accelerated.TunedTreeIterations(12), Is.EqualTo(6));
            Assert.That(accelerated.TunedRolloutDepth(), Is.EqualTo(3));
            Assert.That(accelerated.TunedSimulationCount(), Is.EqualTo(4));
            Assert.That(neutral.TunedTreeIterations(12), Is.EqualTo(12));
        }

        [Test]
        public void Progression_RevealsSolverDescriptionAfterUnlock()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData { chips = 100000 });
            SolverDefinition definition = StackMergeSolverCatalog.GetDefinition(SolverId.Look);

            Assert.That(progression.IsSolverUnlocked(SolverId.Look), Is.False);
            Assert.That(progression.GetSolverDescription(SolverId.Look), Is.EqualTo(definition.LockedHint));
            Assert.That(progression.SelectOrUnlockSolver(SolverId.Look), Is.True);
            Assert.That(progression.SelectedSolver, Is.EqualTo(SolverId.Look));
            Assert.That(progression.GetSolverDescription(SolverId.Look), Is.EqualTo(definition.Description));
        }

        [Test]
        public void Progression_UnlocksSpeedRestartAndStackCapacity()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData { chips = 120000 });

            Assert.That(progression.StackCapacity, Is.EqualTo(5));
            Assert.That(progression.SelectOrUnlockSolver(SolverId.Heur), Is.True);
            Assert.That(progression.SelectedSolver, Is.EqualTo(SolverId.Heur));
            Assert.That(progression.BuySpeedUpgrade(), Is.True);
            Assert.That(progression.SpeedLevel, Is.EqualTo(1));
            Assert.That(progression.ToggleOrBuyAutoRestart(), Is.True);
            Assert.That(progression.AutoRestartUnlocked, Is.True);
            Assert.That(progression.AutoRestartEnabled, Is.True);
            Assert.That(progression.BuyStackCapacityUpgrade(), Is.True);
            Assert.That(progression.StackCapacity, Is.EqualTo(6));
            Assert.That(progression.QueueLength, Is.EqualTo(3));
            Assert.That(progression.BuyQueuePreviewUpgrade(), Is.True);
            Assert.That(progression.QueueLength, Is.EqualTo(4));
            Assert.That(progression.BuyIncomeUpgrade(), Is.True);
            Assert.That(progression.IncomeLevel, Is.EqualTo(1));
        }

        [Test]
        public void Progression_UnlocksDifficultyAndRecordsRunHistory()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData { chips = 100000 });

            Assert.That(progression.DifficultyLevel, Is.EqualTo(0));
            Assert.That(progression.BuyDifficultyUpgrade(), Is.True);
            Assert.That(progression.DifficultyLevel, Is.EqualTo(1));

            long bonus = progression.AwardRunCompleted(640, SolverId.Combo, 42, 7, 64);

            Assert.That(bonus, Is.GreaterThan(0));
            Assert.That(progression.RunHistory.Length, Is.EqualTo(1));
            Assert.That(progression.RunHistory[0].score, Is.EqualTo(640));
            Assert.That(progression.RunHistory[0].solverId, Is.EqualTo((int)SolverId.Combo));
            Assert.That(progression.RunHistory[0].moves, Is.EqualTo(42));
            Assert.That(progression.RunHistory[0].merges, Is.EqualTo(7));
            Assert.That(progression.RunHistory[0].highestMergedBlock, Is.EqualTo(64));
            Assert.That(progression.RunHistory[0].difficultyLevel, Is.EqualTo(1));
        }

        [Test]
        public void Progression_TracksLifetimeStatsAndAchievements()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData { chips = 1000 });
            MoveResult bigMerge = new MoveResult(
                accepted: true,
                stackIndex: 0,
                placedValue: 4,
                resultingTopValue: 1024,
                mergeCount: 50,
                score: 1024,
                highestBlock: 1024,
                isGameOver: false,
                reason: string.Empty);

            long chipsGained = progression.AwardMove(bigMerge);
            long runBonus = progression.AwardRunCompleted(5000, SolverId.Heur, 12, 50, 1024, manualRun: true);
            Assert.That(progression.BuySpeedUpgrade(), Is.True);

            Assert.That(chipsGained, Is.GreaterThan(0));
            Assert.That(runBonus, Is.GreaterThan(0));
            Assert.That(progression.TotalChipsEarned, Is.GreaterThanOrEqualTo(chipsGained + runBonus));
            Assert.That(progression.TotalChipsSpent, Is.EqualTo(6000));
            Assert.That(progression.TotalBlocksDropped, Is.EqualTo(1));
            Assert.That(progression.TotalMerges, Is.EqualTo(50));
            Assert.That(progression.HighestBlockEver, Is.EqualTo(1024));
            Assert.That(progression.BestRunScore, Is.EqualTo(5000));
            Assert.That(progression.ManualRunsCompleted, Is.EqualTo(1));
            Assert.That(progression.IsAchievementComplete(StackMergeProgression.Achievements[0]), Is.True);
            Assert.That(progression.IsAchievementComplete(StackMergeProgression.Achievements[9]), Is.True);
            Assert.That(progression.IsAchievementComplete(StackMergeProgression.Achievements[15]), Is.True);
        }

        [Test]
        public void Progression_KeepsLargeBoundedRunHistory()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData());

            for (int i = 0; i < 260; i++)
            {
                progression.AwardRunCompleted(i + 1, SolverId.Rand, i, 0, 0);
            }

            Assert.That(progression.RunHistory.Length, Is.EqualTo(250));
            Assert.That(progression.RunHistory[0].runIndex, Is.EqualTo(260));
            Assert.That(progression.RunHistory[^1].runIndex, Is.EqualTo(11));
        }

        [Test]
        public void RestoreSnapshotResized_ExtendsNextQueueWithoutDroppingBoard()
        {
            var state = new StackMergeGameState(seed: 6);
            state.SetNextBlocksForTesting(1, 2, 4);
            state.SetStacksForTesting(
                new[] { 1 },
                new[] { 2 },
                new int[] { },
                new int[] { });

            StackMergeSnapshot snapshot = state.CreateSnapshot();
            var resized = new StackMergeGameState(stackCapacity: 6, queueLength: 5, seed: 7);
            resized.RestoreSnapshotResized(snapshot);

            Assert.That(resized.StackCapacity, Is.EqualTo(6));
            Assert.That(resized.QueueLength, Is.EqualTo(5));
            Assert.That(resized.Stacks[0], Is.EqualTo(new[] { 1 }));
            Assert.That(resized.Stacks[1], Is.EqualTo(new[] { 2 }));
            Assert.That(resized.NextBlocks[0], Is.EqualTo(1));
            Assert.That(resized.NextBlocks[1], Is.EqualTo(2));
            Assert.That(resized.NextBlocks[2], Is.EqualTo(4));
            Assert.That(resized.NextBlocks.Count, Is.EqualTo(5));
        }

        [Test]
        public void Plan5Solver_ReturnsLegalMoveWithExtendedQueue()
        {
            var state = new StackMergeGameState(queueLength: 5, seed: 8);
            state.SetNextBlocksForTesting(1, 2, 1, 4, 1);
            state.SetStacksForTesting(
                new[] { 1 },
                new[] { 4 },
                new[] { 8 },
                new int[] { });

            var solver = new Plan5StackMergeSolver();
            SolverDecision decision = solver.ChooseMove(state, new SolverContext(new Random(8), 4, 4));

            Assert.That(decision.HasMove, Is.True);
            Assert.That(state.CanPlace(decision.StackIndex), Is.True);
        }

        [Test]
        public void AdvancedSolvers_ReturnLegalMoves()
        {
            var state = new StackMergeGameState(queueLength: 5, seed: 9);
            state.SetNextBlocksForTesting(1, 2, 1, 4, 2);
            state.SetStacksForTesting(
                new[] { 1, 2 },
                new[] { 4 },
                new[] { 2, 2 },
                new int[] { });

            IStackMergeSolver[] solvers =
            {
                new EnhancedMonteCarloStackMergeSolver(),
                new MctsStackMergeSolver(),
                new AntiStallStackMergeSolver(),
                new ComboFocusedStackMergeSolver()
            };

            foreach (IStackMergeSolver solver in solvers)
            {
                SolverDecision decision = solver.ChooseMove(state, new SolverContext(new Random(9), 4, 4));

                Assert.That(decision.HasMove, Is.True, solver.DisplayName);
                Assert.That(state.CanPlace(decision.StackIndex), Is.True, solver.DisplayName);
            }
        }

        [Test]
        public void AdvancedSolvers_ReturnLegalMovesInLightweightBenchmarkContext()
        {
            var state = new StackMergeGameState(queueLength: 5, seed: 12);
            state.SetNextBlocksForTesting(1, 2, 1, 4, 2);
            state.SetStacksForTesting(
                new[] { 1, 2 },
                new[] { 4 },
                new[] { 2, 2 },
                new int[] { });

            var context = new SolverContext(new Random(12), 1, 1, lightweightMode: true, planningDepthLimit: 1);
            IStackMergeSolver[] solvers =
            {
                new Plan5StackMergeSolver(),
                new EnhancedMonteCarloStackMergeSolver(),
                new MctsStackMergeSolver(),
                new ComboFocusedStackMergeSolver()
            };

            foreach (IStackMergeSolver solver in solvers)
            {
                SolverDecision decision = solver.ChooseMove(state, context);

                Assert.That(decision.HasMove, Is.True, solver.DisplayName);
                Assert.That(state.CanPlace(decision.StackIndex), Is.True, solver.DisplayName);
            }
        }

        [Test]
        public void Progression_StoresSolverTuningPerSolver()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData());

            progression.SetSolverTuningValue(SolverId.Heur, 0, 40);
            progression.SetSolverTuningValue(SolverId.Heur, 2, -40);
            progression.SetSolverTuningValue(SolverId.Heur, 3, 1);

            SolverTuningSettings tuning = progression.GetSolverTuning(SolverId.Heur);
            Assert.That(tuning.GetValue(SolverTuneParameterId.ScoreDelta), Is.EqualTo(SolverTuningSettings.MaxValue));
            Assert.That(tuning.GetValue(SolverTuneParameterId.Smoothness), Is.EqualTo(SolverTuningSettings.MinValue));
            Assert.That(tuning.GetValue(SolverTuneParameterId.QueueFit), Is.EqualTo(1));
            Assert.That(progression.GetSolverTuning(SolverId.Rand).IsNeutral, Is.True);

            progression.ResetSolverTuning(SolverId.Heur);
            Assert.That(progression.GetSolverTuning(SolverId.Heur).IsNeutral, Is.True);
        }

        [Test]
        public void MctsSolver_ReturnsLegalMoveWithTuning()
        {
            var state = new StackMergeGameState(queueLength: 5, seed: 13);
            state.SetNextBlocksForTesting(1, 2, 1, 4, 2);
            state.SetStacksForTesting(
                new[] { 1, 2, 4 },
                new[] { 2, 2 },
                new[] { 4 },
                new int[] { });

            var context = new SolverContext(
                new Random(13),
                3,
                3,
                lightweightMode: true,
                planningDepthLimit: 2,
                tuning: new SolverTuningSettings(SolverId.Mcts, new[] { 2, 1, 2, 1, 1, 0 }));

            SolverDecision decision = new MctsStackMergeSolver().ChooseMove(state, context);

            Assert.That(decision.HasMove, Is.True);
            Assert.That(state.CanPlace(decision.StackIndex), Is.True);
        }

        [Test]
        public void Progression_AgentsUnlockEquipAndExtraSlotUpgradeAddsSlot()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData { chips = 5000000 });
            AgentDefinition quartermaster = progression.GetAgentDefinition(AgentId.Quartermaster);

            Assert.That(progression.ActiveAgentSlots, Is.EqualTo(2));
            Assert.That(progression.AgentsMenuUnlocked, Is.False);
            Assert.That(progression.BuyAgent(AgentId.MergeBroker), Is.False);
            Assert.That(progression.BuyAgentsMenuUnlock(), Is.True);
            Assert.That(progression.AgentsMenuUnlocked, Is.True);
            Assert.That(progression.GetAgentInfo(AgentId.Quartermaster), Is.EqualTo(quartermaster.LockedHint));

            Assert.That(progression.BuyAgent(AgentId.MergeBroker), Is.True);
            Assert.That(progression.EquipAgent(AgentId.MergeBroker), Is.True);
            Assert.That(progression.BuyAgent(AgentId.HighwaterAnalyst), Is.True);
            Assert.That(progression.EquipAgent(AgentId.HighwaterAnalyst), Is.True);
            Assert.That(progression.ActiveAgentCount, Is.EqualTo(2));

            Assert.That(progression.BuyAgent(AgentId.Quartermaster), Is.True);
            Assert.That(progression.IsAgentUnlocked(AgentId.Quartermaster), Is.True);
            Assert.That(progression.IsAgentActive(AgentId.Quartermaster), Is.False);
            Assert.That(progression.GetAgentInfo(AgentId.Quartermaster), Is.EqualTo(quartermaster.Description));

            Assert.That(progression.BuyExtraAgentSlotUpgrade(), Is.True);
            Assert.That(progression.ActiveAgentSlots, Is.EqualTo(3));
            Assert.That(progression.UnequipAgent(AgentId.HighwaterAnalyst), Is.True);
            Assert.That(progression.EquipAgent(AgentId.Quartermaster), Is.True);
            Assert.That(progression.IsAgentActive(AgentId.Quartermaster), Is.True);
            Assert.That(progression.ActiveAgentCount, Is.EqualTo(2));

            Assert.That(progression.BuyAgent(AgentId.RestartSponsor), Is.True);
            Assert.That(progression.EquipAgent(AgentId.RestartSponsor), Is.True);
            Assert.That(progression.IsAgentActive(AgentId.RestartSponsor), Is.True);
            Assert.That(progression.ActiveAgentCount, Is.EqualTo(3));
        }

        [Test]
        public void Progression_AutomationRequiresPurchasedSolverAndAutoRestartUsesTokens()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData { chips = 2000000 });

            Assert.That(progression.ToggleOrBuyAutoSolve(), Is.False);
            Assert.That(progression.ToggleOrBuyAutoRestart(), Is.False);

            Assert.That(progression.SelectOrUnlockSolver(SolverId.Heur), Is.True);
            Assert.That(progression.ToggleOrBuyAutoSolve(), Is.True);
            Assert.That(progression.AutoSolveUnlocked, Is.True);
            Assert.That(progression.AutoSolveEnabled, Is.True);

            Assert.That(progression.ToggleOrBuyAutoRestart(), Is.True);
            Assert.That(progression.AutoRestartUnlocked, Is.True);
            Assert.That(progression.TryConsumeAutoRestartToken(), Is.False);

            Assert.That(progression.BuyTokenPack(), Is.True);
            Assert.That(progression.Tokens, Is.EqualTo(progression.GetTokenPackSize()));
            Assert.That(progression.TryConsumeAutoRestartToken(), Is.True);
            Assert.That(progression.Tokens, Is.EqualTo(progression.GetTokenPackSize() - 1));

            Assert.That(progression.BuyAgentsMenuUnlock(), Is.True);
            Assert.That(progression.BuyAgent(AgentId.RestartSponsor), Is.True);
            Assert.That(progression.EquipAgent(AgentId.RestartSponsor), Is.True);
            long tokensBeforeSponsorRestart = progression.Tokens;
            Assert.That(progression.TryConsumeAutoRestartToken(), Is.True);
            Assert.That(progression.Tokens, Is.EqualTo(tokensBeforeSponsorRestart));
        }

        [Test]
        public void Progression_UnlocksSolverTuningAndModifiers()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData { chips = 50000 });

            Assert.That(progression.SolverTuningUnlocked, Is.False);
            Assert.That(progression.BuySolverTuningUnlock(), Is.True);
            Assert.That(progression.SolverTuningUnlocked, Is.True);

            Assert.That(progression.BuyModifierUpgrade(ModifierId.UnstableStack), Is.False);

            bool[] unlockedSolvers = new bool[StackMergeSolverCatalog.Definitions.Length];
            for (int i = 0; i < 7; i++)
            {
                unlockedSolvers[i] = true;
            }

            progression = new StackMergeProgression(new StackMergeProgressionData
            {
                chips = 10000000,
                solverUnlocked = unlockedSolvers,
                agentsMenuUnlocked = true,
                runsCompleted = 20,
                totalMerges = 3000,
                bestRunScore = 8000,
                highestBlockEver = 1024
            });

            Assert.That(progression.CanUnlockModifiersMenu, Is.True);
            Assert.That(progression.BuyModifiersMenuUnlock(), Is.True);
            Assert.That(progression.BuyModifierUpgrade(ModifierId.UnstableStack), Is.True);
            Assert.That(progression.BuyModifierUpgrade(ModifierId.Joker), Is.True);
            StackMergeRunModifiers modifiers = progression.BuildRunModifiers();

            Assert.That(modifiers.UnstableSaves, Is.EqualTo(1));
            Assert.That(modifiers.JokerBlocks, Is.True);
        }

        [Test]
        public void Progression_GatesMachineLearningBehindMaxedModifiers()
        {
            var lockedProgression = new StackMergeProgression(new StackMergeProgressionData
            {
                chips = 101_000_000,
                modifiersMenuUnlocked = true,
                modifierLevels = new int[StackMergeProgression.Modifiers.Length]
            });

            Assert.That(lockedProgression.CanUnlockMachineLearning, Is.False);
            Assert.That(lockedProgression.SelectOrUnlockSolver(SolverId.MachineLearning), Is.False);

            int[] maxedModifiers = StackMergeProgression.Modifiers.Select(modifier => modifier.MaxLevel).ToArray();
            var unlockedProgression = new StackMergeProgression(new StackMergeProgressionData
            {
                chips = 101_000_000,
                modifiersMenuUnlocked = true,
                modifierLevels = maxedModifiers
            });

            Assert.That(unlockedProgression.CanUnlockMachineLearning, Is.True);
            Assert.That(unlockedProgression.SelectOrUnlockSolver(SolverId.MachineLearning), Is.True);
            Assert.That(unlockedProgression.SelectedSolver, Is.EqualTo(SolverId.MachineLearning));
        }

        [Test]
        public void Prestige_StartsAtOneInsightAndResetsPpoBrain()
        {
            bool[] unlockedSolvers = new bool[StackMergeSolverCatalog.Definitions.Length];
            unlockedSolvers[(int)SolverId.MachineLearning] = true;
            var progression = new StackMergeProgression(new StackMergeProgressionData
            {
                chips = 554_918,
                solverUnlocked = unlockedSolvers,
                selectedSolver = (int)SolverId.MachineLearning,
                bestRunScore = 33_908,
                highestBlockEver = 8192,
                machineLearningNormalRuns = 1,
                machineLearningNormalBestScore = 33_908,
                machineLearningNormalBestHigh = 8192,
                machineLearningNormalFrames = 200,
                machineLearningPolicy = CreateInitializedPpoPolicy(500_000, 25, 4, 0, 0)
            });

            Assert.That(progression.PrestigeAvailable, Is.True);
            Assert.That(progression.PreviewPrestigeInsightGain(), Is.EqualTo(1));

            long gained = progression.ExecutePrestige();

            Assert.That(gained, Is.EqualTo(1));
            Assert.That(progression.ResearchInsight, Is.EqualTo(1));
            Assert.That(progression.PrestigeCount, Is.EqualTo(1));
            Assert.That(progression.IsSolverUnlocked(SolverId.MachineLearning), Is.False);
            Assert.That(progression.MachineLearningFrames, Is.EqualTo(0));
            Assert.That(progression.Chips, Is.EqualTo(0));
            Assert.That(progression.BestRunScore, Is.EqualTo(0));
            Assert.That(progression.HighestBlockEver, Is.EqualTo(2));
        }

        [Test]
        public void Prestige_PreservesResearchAndAppliesStartBonuses()
        {
            bool[] unlockedSolvers = new bool[StackMergeSolverCatalog.Definitions.Length];
            unlockedSolvers[(int)SolverId.MachineLearning] = true;
            int[] researchLevels = new int[StackMergeProgression.Research.Length];
            researchLevels[(int)ResearchId.SeedCapital] = 1;
            researchLevels[(int)ResearchId.AutomationMemory] = 1;
            researchLevels[(int)ResearchId.AlgorithmArchive] = 1;

            var progression = new StackMergeProgression(new StackMergeProgressionData
            {
                chips = 1_000_000,
                solverUnlocked = unlockedSolvers,
                selectedSolver = (int)SolverId.MachineLearning,
                bestRunScore = 80_000,
                highestBlockEver = 16384,
                prestigeCount = 1,
                researchInsight = 4,
                researchLevels = researchLevels,
                machineLearningNormalRuns = 12,
                machineLearningNormalBestScore = 80_000,
                machineLearningNormalBestHigh = 16384,
                machineLearningNormalFrames = 500_000,
                machineLearningPolicy = CreateInitializedPpoPolicy(500_000, 50, 12, 70_000, 16384)
            });

            long gained = progression.ExecutePrestige();

            Assert.That(gained, Is.GreaterThan(1));
            Assert.That(progression.GetResearchLevel(ResearchId.SeedCapital), Is.EqualTo(1));
            Assert.That(progression.GetResearchLevel(ResearchId.AutomationMemory), Is.EqualTo(1));
            Assert.That(progression.GetResearchLevel(ResearchId.AlgorithmArchive), Is.EqualTo(1));
            Assert.That(progression.Chips, Is.EqualTo(2500));
            Assert.That(progression.IsSolverUnlocked(SolverId.Rand), Is.True);
            Assert.That(progression.AutoSolveUnlocked, Is.True);
            Assert.That(progression.AutoSolveEnabled, Is.True);
            Assert.That(progression.PrestigeCount, Is.EqualTo(2));
            Assert.That(progression.ResearchInsight, Is.EqualTo(4 + gained));
        }

        [Test]
        public void Prestige_RequiresNormalPpoRunsAfterTraining()
        {
            bool[] unlockedSolvers = new bool[StackMergeSolverCatalog.Definitions.Length];
            unlockedSolvers[(int)SolverId.MachineLearning] = true;
            var progression = new StackMergeProgression(new StackMergeProgressionData
            {
                solverUnlocked = unlockedSolvers,
                selectedSolver = (int)SolverId.MachineLearning,
                machineLearningBestScore = 120_000,
                machineLearningBestHigh = 16384,
                machineLearningPolicy = CreateInitializedPpoPolicy(500_000, 100, 500, 120_000, 16384)
            });

            Assert.That(progression.PrestigeAvailable, Is.True);
            Assert.That(progression.PreviewPrestigeInsightGain(), Is.EqualTo(0));
            Assert.That(progression.ExecutePrestige(), Is.EqualTo(0));
            Assert.That(progression.ResearchInsight, Is.EqualTo(0));
        }

        [Test]
        public void PpoNormalRuns_EarnInsightAfterFirstPrestige()
        {
            bool[] unlockedSolvers = new bool[StackMergeSolverCatalog.Definitions.Length];
            unlockedSolvers[(int)SolverId.MachineLearning] = true;
            var progression = new StackMergeProgression(new StackMergeProgressionData
            {
                solverUnlocked = unlockedSolvers,
                selectedSolver = (int)SolverId.MachineLearning,
                prestigeCount = 1,
                machineLearningPolicy = CreateInitializedPpoPolicy(500_000, 100, 10, 12_000, 4096)
            });

            progression.AwardMachineLearningRun(12_000, 260, 210, 4096, trainingMode: false);

            Assert.That(progression.ResearchInsight, Is.GreaterThan(0));
            Assert.That(progression.ResearchInsightEarnedThisPrestige, Is.EqualTo(progression.ResearchInsight));
            Assert.That(progression.PreviewPrestigeInsightGain(), Is.GreaterThan(1));
        }

        [Test]
        public void Prestige_PpoMemoryWarmStartsAfterReset()
        {
            bool[] unlockedSolvers = new bool[StackMergeSolverCatalog.Definitions.Length];
            unlockedSolvers[(int)SolverId.MachineLearning] = true;
            int[] researchLevels = new int[StackMergeProgression.Research.Length];
            researchLevels[(int)ResearchId.PpoMemory] = 1;
            var progression = new StackMergeProgression(new StackMergeProgressionData
            {
                solverUnlocked = unlockedSolvers,
                selectedSolver = (int)SolverId.MachineLearning,
                researchLevels = researchLevels,
                machineLearningRuns = 500,
                machineLearningNormalRuns = 1,
                machineLearningNormalBestScore = 40_000,
                machineLearningNormalBestHigh = 8192,
                machineLearningNormalFrames = 300,
                machineLearningPolicy = CreateInitializedPpoPolicy(500_000, 80, 500, 40_000, 8192)
            });

            long gained = progression.ExecutePrestige();

            Assert.That(gained, Is.GreaterThan(0));
            Assert.That(progression.MachineLearningMemoryRuns, Is.EqualTo(500));
            Assert.That(progression.MachineLearningFrames, Is.EqualTo(500_000));
            Assert.That(progression.MachineLearningRuns, Is.EqualTo(500));
            Assert.That(progression.IsSolverUnlocked(SolverId.MachineLearning), Is.False);
        }

        [Test]
        public void Modifiers_RescueFullStackAndSupportMirrorAndJokerMerges()
        {
            var unstable = new StackMergeGameState(stackCapacity: 2, modifiers: new StackMergeRunModifiers(1, false, false, 0, 0), seed: 21);
            unstable.SetNextBlocksForTesting(4, 1, 1);
            unstable.SetStacksForTesting(
                new[] { 1, 2 },
                new[] { 8, 16 },
                new[] { 16, 32 },
                new[] { 32, 64 });

            Assert.That(unstable.CanPlace(0), Is.True);
            MoveResult rescue = unstable.PlaceNext(0);
            Assert.That(rescue.Accepted, Is.True);
            Assert.That(rescue.UnstableSaveUsed, Is.True);
            Assert.That(unstable.UnstableSavesRemaining, Is.EqualTo(0));
            Assert.That(unstable.Stacks[0], Is.EqualTo(new[] { 2, 4 }));

            var mirror = new StackMergeGameState(stackCapacity: 5, modifiers: new StackMergeRunModifiers(0, true, false, 0, 0), seed: 22);
            mirror.SetNextBlocksForTesting(4, 1, 1);
            mirror.SetStacksForTesting(
                new[] { 4, 8 },
                new int[] { },
                new int[] { },
                new int[] { });
            MoveResult mirrored = mirror.PlaceNext(0);
            Assert.That(mirrored.MergeCount, Is.EqualTo(2));
            Assert.That(mirror.Stacks[0], Is.EqualTo(new[] { 16 }));

            var fullMirror = new StackMergeGameState(stackCapacity: 2, modifiers: new StackMergeRunModifiers(0, true, false, 0, 0), seed: 24);
            fullMirror.SetNextBlocksForTesting(2, 1, 1);
            fullMirror.SetStacksForTesting(
                new[] { 2, 4 },
                new[] { 8, 16 },
                new[] { 16, 32 },
                new[] { 32, 64 });
            Assert.That(fullMirror.CanPlace(0), Is.True);
            MoveResult fullMirrorMerge = fullMirror.PlaceNext(0);
            Assert.That(fullMirrorMerge.Accepted, Is.True);
            Assert.That(fullMirrorMerge.MergeCount, Is.EqualTo(2));
            Assert.That(fullMirror.Stacks[0], Is.EqualTo(new[] { 8 }));

            var mirrorBeforeUnstable = new StackMergeGameState(stackCapacity: 2, modifiers: new StackMergeRunModifiers(1, true, false, 0, 0), seed: 25);
            mirrorBeforeUnstable.SetNextBlocksForTesting(2, 1, 1);
            mirrorBeforeUnstable.SetStacksForTesting(
                new[] { 2, 4 },
                new[] { 8, 16 },
                new[] { 16, 32 },
                new[] { 32, 64 });
            MoveResult mirrorSaved = mirrorBeforeUnstable.PlaceNext(0);
            Assert.That(mirrorSaved.UnstableSaveUsed, Is.False);
            Assert.That(mirrorBeforeUnstable.UnstableSavesRemaining, Is.EqualTo(1));

            var joker = new StackMergeGameState(stackCapacity: 5, modifiers: new StackMergeRunModifiers(0, false, true, 0, 0), seed: 23);
            joker.SetNextBlocksForTesting(StackMergeGameState.JokerBlockValue, 1, 1);
            joker.SetStacksForTesting(
                new[] { 8 },
                new int[] { },
                new int[] { },
                new int[] { });
            MoveResult jokerMerge = joker.PlaceNext(0);
            Assert.That(jokerMerge.MergeCount, Is.EqualTo(1));
            Assert.That(joker.Stacks[0], Is.EqualTo(new[] { 16 }));
        }

        [Test]
        public void Modifiers_PickaxeAndQueueScrubberAreRunActions()
        {
            var pickaxe = new StackMergeGameState(stackCapacity: 5, modifiers: new StackMergeRunModifiers(0, false, false, 1, 0), seed: 31);
            pickaxe.SetNextBlocksForTesting(8, 1, 1);
            pickaxe.SetStacksForTesting(
                new[] { 2, 4, 2 },
                new int[] { },
                new int[] { },
                new int[] { });

            Assert.That(pickaxe.CanUsePickaxe(0, 1), Is.True);
            MoveResult pickaxeResult = pickaxe.UsePickaxe(0, 1);
            Assert.That(pickaxeResult.Accepted, Is.True);
            Assert.That(pickaxeResult.ActionKind, Is.EqualTo(SolverActionKind.Pickaxe));
            Assert.That(pickaxeResult.MergeCount, Is.EqualTo(1));
            Assert.That(pickaxe.PickaxeUsesRemaining, Is.EqualTo(0));
            Assert.That(pickaxe.Stacks[0], Is.EqualTo(new[] { 4 }));

            var scrubber = new StackMergeGameState(modifiers: new StackMergeRunModifiers(0, false, false, 0, 1), seed: 32);
            scrubber.SetNextBlocksForTesting(8, 2, 1);

            Assert.That(scrubber.CanSkipNextBlock(), Is.True);
            MoveResult scrubResult = scrubber.SkipNextBlock();
            Assert.That(scrubResult.Accepted, Is.True);
            Assert.That(scrubResult.ActionKind, Is.EqualTo(SolverActionKind.QueueSkip));
            Assert.That(scrubResult.RemovedValue, Is.EqualTo(8));
            Assert.That(scrubber.QueueSkipsRemaining, Is.EqualTo(0));
            Assert.That(scrubber.NextBlocks[0], Is.EqualTo(2));
        }
    }
}
