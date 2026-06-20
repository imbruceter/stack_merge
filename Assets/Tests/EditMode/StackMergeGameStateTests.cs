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
        public void SolverCatalog_OffersSixUnlockableAlgorithms()
        {
            Assert.That(StackMergeSolverCatalog.Definitions.Length, Is.EqualTo(6));
            Assert.That(StackMergeSolverCatalog.Definitions[0].Id, Is.EqualTo(SolverId.Rand));
            Assert.That(StackMergeSolverCatalog.Definitions[5].Id, Is.EqualTo(SolverId.Moca));
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
        }

        [Test]
        public void Progression_AgentsUnlockEquipAndCoordinatorAddsSlot()
        {
            var progression = new StackMergeProgression(new StackMergeProgressionData { chips = 10000 });
            AgentDefinition quartermaster = progression.GetAgentDefinition(AgentId.Quartermaster);

            Assert.That(progression.ActiveAgentSlots, Is.EqualTo(2));
            Assert.That(progression.GetAgentInfo(AgentId.Quartermaster), Is.EqualTo(quartermaster.LockedHint));

            Assert.That(progression.BuyOrToggleAgent(AgentId.MergeBroker), Is.True);
            Assert.That(progression.BuyOrToggleAgent(AgentId.HighwaterAnalyst), Is.True);
            Assert.That(progression.ActiveAgentCount, Is.EqualTo(2));

            Assert.That(progression.BuyOrToggleAgent(AgentId.Quartermaster), Is.True);
            Assert.That(progression.IsAgentUnlocked(AgentId.Quartermaster), Is.True);
            Assert.That(progression.IsAgentActive(AgentId.Quartermaster), Is.False);
            Assert.That(progression.GetAgentInfo(AgentId.Quartermaster), Is.EqualTo(quartermaster.Description));

            Assert.That(progression.BuyOrToggleAgent(AgentId.Coordinator), Is.True);
            Assert.That(progression.IsAgentActive(AgentId.Coordinator), Is.True);
            Assert.That(progression.ActiveAgentSlots, Is.EqualTo(3));
            Assert.That(progression.ActiveAgentCount, Is.EqualTo(3));

            Assert.That(progression.BuyOrToggleAgent(AgentId.HighwaterAnalyst), Is.True);
            Assert.That(progression.ActiveAgentCount, Is.EqualTo(2));
            Assert.That(progression.BuyOrToggleAgent(AgentId.Quartermaster), Is.True);
            Assert.That(progression.IsAgentActive(AgentId.Quartermaster), Is.True);
            Assert.That(progression.ActiveAgentCount, Is.EqualTo(3));
        }
    }
}
