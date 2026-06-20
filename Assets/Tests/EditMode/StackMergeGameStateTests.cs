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
        public void SolverCatalog_OffersEightUnlockableAlgorithms()
        {
            Assert.That(StackMergeSolverCatalog.Definitions.Length, Is.EqualTo(8));
            Assert.That(StackMergeSolverCatalog.Definitions[0].Id, Is.EqualTo(SolverId.Rand));
            Assert.That(StackMergeSolverCatalog.Definitions[5].Id, Is.EqualTo(SolverId.Moca));
            Assert.That(StackMergeSolverCatalog.Definitions[6].Id, Is.EqualTo(SolverId.Plan3));
            Assert.That(StackMergeSolverCatalog.Definitions[7].Id, Is.EqualTo(SolverId.Plan5));
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
        public void Progression_AgentsUnlockEquipAndCoordinatorAddsSlot()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData { chips = 10000 });
            AgentDefinition quartermaster = progression.GetAgentDefinition(AgentId.Quartermaster);

            Assert.That(progression.ActiveAgentSlots, Is.EqualTo(2));
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
