using System;
using NUnit.Framework;

namespace StackMerge.Tests
{
    public sealed class StackMergeGameStateTests
    {
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
            Assert.That(state.Score, Is.EqualTo(1));
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
        public void SolverCatalog_OffersTwelveUnlockableAlgorithms()
        {
            Assert.That(StackMergeSolverCatalog.Definitions.Length, Is.EqualTo(12));
            Assert.That(StackMergeSolverCatalog.Definitions[0].Id, Is.EqualTo(SolverId.Rand));
            Assert.That(StackMergeSolverCatalog.Definitions[5].Id, Is.EqualTo(SolverId.Moca));
            Assert.That(StackMergeSolverCatalog.Definitions[6].Id, Is.EqualTo(SolverId.Plan3));
            Assert.That(StackMergeSolverCatalog.Definitions[7].Id, Is.EqualTo(SolverId.Plan5));
            Assert.That(StackMergeSolverCatalog.Definitions[8].Id, Is.EqualTo(SolverId.MocaPlus));
            Assert.That(StackMergeSolverCatalog.Definitions[9].Id, Is.EqualTo(SolverId.Mcts));
            Assert.That(StackMergeSolverCatalog.Definitions[10].Id, Is.EqualTo(SolverId.AntiStall));
            Assert.That(StackMergeSolverCatalog.Definitions[11].Id, Is.EqualTo(SolverId.Combo));
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
        public void Progression_RevealsSolverDescriptionAfterUnlock()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData { chips = 1000 });
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
            var progression = new StackMergeProgression(new StackMergeProgressionData { chips = 1000 });

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
            var progression = new StackMergeProgression(new StackMergeProgressionData { chips = 10000 });

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
            Assert.That(progression.TotalChipsSpent, Is.EqualTo(20));
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
        public void Progression_AgentsUnlockEquipAndCoordinatorAddsSlot()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData { chips = 10000 });
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

            Assert.That(progression.BuyAgent(AgentId.Coordinator), Is.True);
            Assert.That(progression.EquipAgent(AgentId.Coordinator), Is.False);
            Assert.That(progression.UnequipAgent(AgentId.HighwaterAnalyst), Is.True);
            Assert.That(progression.EquipAgent(AgentId.Coordinator), Is.True);
            Assert.That(progression.IsAgentActive(AgentId.Coordinator), Is.True);
            Assert.That(progression.ActiveAgentSlots, Is.EqualTo(3));
            Assert.That(progression.ActiveAgentCount, Is.EqualTo(2));

            Assert.That(progression.EquipAgent(AgentId.Quartermaster), Is.True);
            Assert.That(progression.IsAgentActive(AgentId.Quartermaster), Is.True);
            Assert.That(progression.ActiveAgentCount, Is.EqualTo(3));

            Assert.That(progression.UnequipAgent(AgentId.Coordinator), Is.True);
            Assert.That(progression.ActiveAgentSlots, Is.EqualTo(2));
            Assert.That(progression.IsAgentActive(AgentId.Quartermaster), Is.False);
        }
    }
}
