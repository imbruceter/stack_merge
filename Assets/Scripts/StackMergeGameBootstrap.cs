using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace StackMerge
{
    public sealed class StackMergeGameBootstrap : MonoBehaviour
    {
        private const string HighScoreKey = "StackMerge.HighScore";
        private const float AutoRestartDelay = 1.2f;

        private readonly Dictionary<string, Sprite> spriteCache = new();
        private readonly IStackMergeSolver[] solvers =
        {
            new RandomStackMergeSolver(),
            new MergeFirstStackMergeSolver(),
            new BalancedStackMergeSolver(),
            new HeuristicStackMergeSolver(),
            new LookaheadStackMergeSolver(),
            new MonteCarloStackMergeSolver(),
            new Plan3StackMergeSolver(),
            new Plan5StackMergeSolver()
        };

        [Header("Scene UI")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Canvas canvas;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private TMP_Text highestText;
        [SerializeField] private TMP_Text droppedText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private RectTransform nextBlocksRoot;
        [SerializeField] private Button[] stackButtons = Array.Empty<Button>();
        [SerializeField] private RectTransform[] stackBlockLayers = Array.Empty<RectTransform>();
        [SerializeField] private Button[] newGameButtons = Array.Empty<Button>();

        [Header("Tabs")]
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject algorithmsPanel;
        [SerializeField] private GameObject upgradesPanel;
        [SerializeField] private GameObject agentsPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button[] tabButtons = Array.Empty<Button>();

        [Header("AI UI")]
        [SerializeField] private TMP_Text[] chipsTexts = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text solverText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private TMP_Text queueText;
        [SerializeField] private TMP_Text runStatusText;
        [SerializeField] private TMP_Text agentSlotsText;
        [SerializeField] private Toggle autoSolveToggle;
        [SerializeField] private Button[] solverButtons = Array.Empty<Button>();
        [SerializeField] private TMP_Text solverDetailNameText;
        [SerializeField] private TMP_Text solverDetailInfoText;
        [SerializeField] private TMP_Text solverDetailStatusText;
        [SerializeField] private Button solverDetailActionButton;
        [SerializeField] private Button[] speedUpgradeButtons = Array.Empty<Button>();
        [SerializeField] private Button autoRestartButton;
        [SerializeField] private Button[] stackCapacityUpgradeButtons = Array.Empty<Button>();
        [SerializeField] private Button[] queuePreviewUpgradeButtons = Array.Empty<Button>();
        [SerializeField] private Button[] incomeUpgradeButtons = Array.Empty<Button>();
        [SerializeField] private Button[] agentButtons = Array.Empty<Button>();
        [SerializeField] private TMP_Text[] agentSlotTexts = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text agentDetailNameText;
        [SerializeField] private TMP_Text agentDetailInfoText;
        [SerializeField] private TMP_Text agentDetailStatusText;
        [SerializeField] private Button agentDetailActionButton;

        [Header("Templates")]
        [SerializeField] private RectTransform blockTemplate;
        [SerializeField] private bool useGeneratedBlockSprites = true;

        [Header("Game Over")]
        [SerializeField] private GameObject gameOverOverlay;
        [SerializeField] private TMP_Text gameOverScoreText;
        [SerializeField] private TMP_Text gameOverBestText;

        private StackMergeGameState gameState;
        private StackMergeProgression progression;
        private readonly System.Random solverRandom = new();
        private long highScore;
        private float autoSolveTimer;
        private float autoRestartTimer;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private int selectedTabIndex;
        private SolverId selectedSolverId = SolverId.Rand;
        private AgentId selectedAgentId = AgentId.MergeBroker;

        private void Awake()
        {
            ConfigureCamera();
            EnsureEventSystem();
            WireButtons();
            HideTemplate();
            SelectTab(0);
        }

        private void Start()
        {
            highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            progression = StackMergeProgression.Load();
            selectedSolverId = progression.SelectedSolver;
            CreateFreshGame();
            RefreshEverything();
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                RefreshColumns();
            }

            TickAutomation();
        }

        public void ConfigureSceneReferences(
            Camera cameraReference,
            Canvas canvasReference,
            TMP_Text score,
            TMP_Text best,
            TMP_Text highest,
            TMP_Text dropped,
            TMP_Text feedback,
            RectTransform nextRoot,
            Button[] columns,
            RectTransform[] blockLayers,
            Button[] resetButtons,
            GameObject gameplay,
            GameObject algorithms,
            GameObject upgrades,
            GameObject agents,
            GameObject settings,
            Button[] bottomTabs,
            TMP_Text[] chipsDisplays,
            TMP_Text solver,
            TMP_Text speed,
            TMP_Text capacity,
            TMP_Text queue,
            TMP_Text runStatus,
            TMP_Text agentSlots,
            Toggle autoSolve,
            Button[] solverSelectionButtons,
            TMP_Text selectedSolverName,
            TMP_Text selectedSolverInfo,
            TMP_Text selectedSolverStatus,
            Button selectedSolverAction,
            Button[] speedButtons,
            Button restartButton,
            Button[] capacityButtons,
            Button[] queueButtons,
            Button[] incomeButtons,
            Button[] agentSelectionButtons,
            TMP_Text[] agentSlotsDisplays,
            TMP_Text selectedAgentName,
            TMP_Text selectedAgentInfo,
            TMP_Text selectedAgentStatus,
            Button selectedAgentAction,
            RectTransform blockTemplateReference,
            GameObject gameOver,
            TMP_Text gameOverScore,
            TMP_Text gameOverBest)
        {
            gameCamera = cameraReference;
            canvas = canvasReference;
            scoreText = score;
            bestText = best;
            highestText = highest;
            droppedText = dropped;
            feedbackText = feedback;
            nextBlocksRoot = nextRoot;
            stackButtons = columns;
            stackBlockLayers = blockLayers;
            newGameButtons = resetButtons;
            gameplayPanel = gameplay;
            algorithmsPanel = algorithms;
            upgradesPanel = upgrades;
            agentsPanel = agents;
            settingsPanel = settings;
            tabButtons = bottomTabs;
            chipsTexts = chipsDisplays;
            solverText = solver;
            speedText = speed;
            capacityText = capacity;
            queueText = queue;
            runStatusText = runStatus;
            agentSlotsText = agentSlots;
            autoSolveToggle = autoSolve;
            solverButtons = solverSelectionButtons;
            solverDetailNameText = selectedSolverName;
            solverDetailInfoText = selectedSolverInfo;
            solverDetailStatusText = selectedSolverStatus;
            solverDetailActionButton = selectedSolverAction;
            speedUpgradeButtons = speedButtons;
            autoRestartButton = restartButton;
            stackCapacityUpgradeButtons = capacityButtons;
            queuePreviewUpgradeButtons = queueButtons;
            incomeUpgradeButtons = incomeButtons;
            agentButtons = agentSelectionButtons;
            agentSlotTexts = agentSlotsDisplays;
            agentDetailNameText = selectedAgentName;
            agentDetailInfoText = selectedAgentInfo;
            agentDetailStatusText = selectedAgentStatus;
            agentDetailActionButton = selectedAgentAction;
            blockTemplate = blockTemplateReference;
            gameOverOverlay = gameOver;
            gameOverScoreText = gameOverScore;
            gameOverBestText = gameOverBest;
        }

        private void ConfigureCamera()
        {
            Camera targetCamera = gameCamera != null ? gameCamera : Camera.main;
            if (targetCamera == null)
            {
                return;
            }

            targetCamera.orthographic = true;
            targetCamera.orthographicSize = 5f;
            targetCamera.backgroundColor = HexColor("#111827");
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private void WireButtons()
        {
            for (int i = 0; i < stackButtons.Length; i++)
            {
                int stackIndex = i;
                if (stackButtons[i] == null)
                {
                    continue;
                }

                stackButtons[i].onClick.RemoveAllListeners();
                stackButtons[i].onClick.AddListener(() => PlaceOnStack(stackIndex, "Manual"));
            }

            foreach (Button newGameButton in newGameButtons)
            {
                if (newGameButton == null)
                {
                    continue;
                }

                newGameButton.onClick.RemoveAllListeners();
                newGameButton.onClick.AddListener(StartNewGame);
            }

            for (int i = 0; i < tabButtons.Length; i++)
            {
                int tabIndex = i;
                if (tabButtons[i] == null)
                {
                    continue;
                }

                tabButtons[i].onClick.RemoveAllListeners();
                tabButtons[i].onClick.AddListener(() => SelectTab(tabIndex));
            }

            for (int i = 0; i < solverButtons.Length; i++)
            {
                int solverIndex = i;
                if (solverButtons[i] == null)
                {
                    continue;
                }

                solverButtons[i].onClick.RemoveAllListeners();
                solverButtons[i].onClick.AddListener(() => SelectSolver((SolverId)solverIndex));
            }

            for (int i = 0; i < agentButtons.Length; i++)
            {
                int agentIndex = i;
                if (agentButtons[i] == null)
                {
                    continue;
                }

                agentButtons[i].onClick.RemoveAllListeners();
                agentButtons[i].onClick.AddListener(() => SelectAgent((AgentId)agentIndex));
            }

            for (int i = 0; i < speedUpgradeButtons.Length; i++)
            {
                int upgradeIndex = i;
                if (speedUpgradeButtons[i] == null)
                {
                    continue;
                }

                speedUpgradeButtons[i].onClick.RemoveAllListeners();
                speedUpgradeButtons[i].onClick.AddListener(() => BuySpeedUpgrade(upgradeIndex));
            }

            if (autoRestartButton != null)
            {
                autoRestartButton.onClick.RemoveAllListeners();
                autoRestartButton.onClick.AddListener(ToggleOrBuyAutoRestart);
            }

            for (int i = 0; i < stackCapacityUpgradeButtons.Length; i++)
            {
                int upgradeIndex = i;
                if (stackCapacityUpgradeButtons[i] == null)
                {
                    continue;
                }

                stackCapacityUpgradeButtons[i].onClick.RemoveAllListeners();
                stackCapacityUpgradeButtons[i].onClick.AddListener(() => BuyStackCapacityUpgrade(upgradeIndex));
            }

            for (int i = 0; i < queuePreviewUpgradeButtons.Length; i++)
            {
                int upgradeIndex = i;
                if (queuePreviewUpgradeButtons[i] == null)
                {
                    continue;
                }

                queuePreviewUpgradeButtons[i].onClick.RemoveAllListeners();
                queuePreviewUpgradeButtons[i].onClick.AddListener(() => BuyQueuePreviewUpgrade(upgradeIndex));
            }

            for (int i = 0; i < incomeUpgradeButtons.Length; i++)
            {
                int upgradeIndex = i;
                if (incomeUpgradeButtons[i] == null)
                {
                    continue;
                }

                incomeUpgradeButtons[i].onClick.RemoveAllListeners();
                incomeUpgradeButtons[i].onClick.AddListener(() => BuyIncomeUpgrade(upgradeIndex));
            }

            if (autoSolveToggle != null)
            {
                autoSolveToggle.onValueChanged.RemoveAllListeners();
                autoSolveToggle.onValueChanged.AddListener(SetAutoSolveEnabled);
            }

            if (solverDetailActionButton != null)
            {
                solverDetailActionButton.onClick.RemoveAllListeners();
                solverDetailActionButton.onClick.AddListener(HandleSelectedSolverAction);
            }

            if (agentDetailActionButton != null)
            {
                agentDetailActionButton.onClick.RemoveAllListeners();
                agentDetailActionButton.onClick.AddListener(HandleSelectedAgentAction);
            }
        }

        private void SelectTab(int tabIndex)
        {
            selectedTabIndex = Mathf.Clamp(tabIndex, 0, 4);
            SetActive(gameplayPanel, selectedTabIndex == 0);
            SetActive(algorithmsPanel, selectedTabIndex == 1);
            SetActive(upgradesPanel, selectedTabIndex == 2);
            SetActive(agentsPanel, selectedTabIndex == 3);
            SetActive(settingsPanel, selectedTabIndex == 4);

            for (int i = 0; i < tabButtons.Length; i++)
            {
                TMP_Text label = tabButtons[i] != null ? tabButtons[i].GetComponentInChildren<TMP_Text>(true) : null;
                Image background = tabButtons[i] != null ? tabButtons[i].GetComponent<Image>() : null;
                bool selected = i == selectedTabIndex;
                if (label != null)
                {
                    label.color = selected ? HexColor("#FDE68A") : Color.white;
                }

                if (background != null)
                {
                    background.color = selected ? HexColor("#1D4ED8") : HexColor("#1F2937");
                }
            }

            if (gameState != null)
            {
                RefreshGameOver();
            }
        }

        private void HideTemplate()
        {
            if (blockTemplate != null)
            {
                blockTemplate.gameObject.SetActive(false);
            }
        }

        private void TickAutomation()
        {
            if (gameState == null || progression == null)
            {
                return;
            }

            if (gameState.IsGameOver)
            {
                if (progression.AutoRestartUnlocked && progression.AutoRestartEnabled)
                {
                    autoRestartTimer += Time.deltaTime;
                    SetText(runStatusText, $"Restart in {Mathf.Max(0f, AutoRestartDelay - autoRestartTimer):0.0}s");
                    if (autoRestartTimer >= AutoRestartDelay)
                    {
                        StartNewGame();
                    }
                }

                return;
            }

            autoRestartTimer = 0f;
            if (!progression.AutoSolveEnabled)
            {
                return;
            }

            autoSolveTimer += Time.deltaTime;
            if (autoSolveTimer < progression.MoveInterval)
            {
                return;
            }

            autoSolveTimer = 0f;
            SolverDecision decision = GetSelectedSolver().ChooseMove(
                gameState,
                new SolverContext(solverRandom, progression.MonteCarloSimulationCount, progression.MonteCarloRolloutDepth));

            if (decision.HasMove)
            {
                PlaceOnStack(decision.StackIndex, decision.Reason);
            }
        }

        private IStackMergeSolver GetSelectedSolver()
        {
            int solverIndex = Mathf.Clamp((int)progression.SelectedSolver, 0, solvers.Length - 1);
            return solvers[solverIndex];
        }

        private void PlaceOnStack(int stackIndex, string reason)
        {
            if (gameState == null || progression == null)
            {
                return;
            }

            bool wasGameOver = gameState.IsGameOver;
            MoveResult result = gameState.PlaceNext(stackIndex);
            if (!result.Accepted)
            {
                SetText(feedbackText, result.Reason);
                RefreshEverything();
                return;
            }

            long chipsGained = progression.AwardMove(result);
            long runBonus = 0;
            if (!wasGameOver && result.IsGameOver)
            {
                runBonus = progression.AwardRunCompleted(gameState.Score);
            }

            progression.Save();
            UpdateHighScore();

            string chipText = runBonus > 0 ? $"+{chipsGained + runBonus} chips" : $"+{chipsGained} chips";
            string moveText = result.MergeCount > 0
                ? $"Merge x{result.MergeCount}: {FormatNumber(result.ResultingTopValue)}"
                : $"+{FormatNumber(result.PlacedValue)}";
            SetText(feedbackText, $"{moveText} | {chipText} | {reason}");

            RefreshEverything();
        }

        public void StartNewGame()
        {
            CreateFreshGame();
            SetText(feedbackText, string.Empty);
            RefreshEverything();
        }

        private void CreateFreshGame()
        {
            int capacity = progression != null ? progression.StackCapacity : StackMergeGameState.DefaultStackCapacity;
            int queueLength = progression != null ? progression.QueueLength : StackMergeGameState.DefaultQueueLength;
            gameState = new StackMergeGameState(stackCapacity: capacity, queueLength: queueLength, seed: Environment.TickCount);
            autoSolveTimer = 0f;
            autoRestartTimer = 0f;
        }

        private void UpdateHighScore()
        {
            if (gameState.Score <= highScore)
            {
                return;
            }

            highScore = gameState.Score;
            int storedScore = highScore > int.MaxValue ? int.MaxValue : (int)highScore;
            PlayerPrefs.SetInt(HighScoreKey, storedScore);
            PlayerPrefs.Save();
        }

        private void SelectSolver(SolverId solverId)
        {
            selectedSolverId = solverId;
            RefreshSolverButtons();
            RefreshSolverDetails();
        }

        private void HandleSelectedSolverAction()
        {
            if (progression == null)
            {
                return;
            }

            SolverDefinition definition = StackMergeSolverCatalog.GetDefinition(selectedSolverId);
            bool changed = progression.SelectOrUnlockSolver(selectedSolverId);
            SetText(feedbackText, changed ? $"Solver: {definition.DisplayName}" : "Not enough chips");
            progression.Save();
            RefreshEverything();
        }

        private void BuySpeedUpgrade(int upgradeIndex)
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuySpeedUpgrade(upgradeIndex);
            SetText(feedbackText, bought ? $"Speed level {progression.SpeedLevel}" : "Speed upgrade unavailable");
            progression.Save();
            RefreshEverything();
        }

        private void ToggleOrBuyAutoRestart()
        {
            if (progression == null)
            {
                return;
            }

            bool changed = progression.ToggleOrBuyAutoRestart();
            SetText(feedbackText, changed ? "Auto restart updated" : "Not enough chips");
            progression.Save();
            RefreshEverything();
        }

        private void BuyStackCapacityUpgrade(int upgradeIndex)
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyStackCapacityUpgrade(upgradeIndex);
            SetText(feedbackText, bought ? $"Stack capacity: {progression.StackCapacity}" : "Stack upgrade unavailable");
            if (bought)
            {
                ApplyCurrentBoardSettingsToGameState();
            }

            progression.Save();
            RefreshEverything();
        }

        private void BuyQueuePreviewUpgrade(int upgradeIndex)
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyQueuePreviewUpgrade(upgradeIndex);
            SetText(feedbackText, bought ? $"Next preview: {progression.QueueLength} blocks" : "Next preview upgrade unavailable");
            if (bought)
            {
                ApplyCurrentBoardSettingsToGameState();
            }

            progression.Save();
            RefreshEverything();
        }

        private void BuyIncomeUpgrade(int upgradeIndex)
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyIncomeUpgrade(upgradeIndex);
            SetText(feedbackText, bought ? $"Chip yield level {progression.IncomeLevel}" : "Income upgrade unavailable");
            progression.Save();
            RefreshEverything();
        }

        private void SelectAgent(AgentId agentId)
        {
            selectedAgentId = agentId;
            RefreshAgentButtons();
            RefreshAgentDetails();
        }

        private void HandleSelectedAgentAction()
        {
            if (progression == null)
            {
                return;
            }

            AgentDefinition definition = progression.GetAgentDefinition(selectedAgentId);
            bool changed;
            if (!progression.IsAgentUnlocked(selectedAgentId))
            {
                changed = progression.BuyAgent(selectedAgentId);
                SetText(feedbackText, changed ? $"Agent bought: {definition.DisplayName}" : "Not enough chips");
            }
            else if (progression.IsAgentActive(selectedAgentId))
            {
                changed = progression.UnequipAgent(selectedAgentId);
                SetText(feedbackText, changed ? $"Agent unequipped: {definition.DisplayName}" : "Agent unavailable");
            }
            else
            {
                changed = progression.EquipAgent(selectedAgentId);
                SetText(feedbackText, changed ? $"Agent equipped: {definition.DisplayName}" : "No free agent slot");
            }

            progression.Save();
            RefreshEverything();
        }

        private void ApplyCurrentBoardSettingsToGameState()
        {
            if (gameState == null
                || (gameState.StackCapacity == progression.StackCapacity && gameState.QueueLength == progression.QueueLength))
            {
                return;
            }

            StackMergeSnapshot snapshot = gameState.CreateSnapshot();
            var resizedGame = new StackMergeGameState(
                stackCapacity: progression.StackCapacity,
                queueLength: progression.QueueLength,
                seed: Environment.TickCount);
            resizedGame.RestoreSnapshotResized(snapshot);
            gameState = resizedGame;
        }

        private void SetAutoSolveEnabled(bool enabled)
        {
            if (progression == null)
            {
                return;
            }

            progression.AutoSolveEnabled = enabled;
            progression.Save();
            RefreshProgressionUi();
        }

        private void RefreshEverything()
        {
            if (gameState == null)
            {
                return;
            }

            SetText(scoreText, FormatNumber(gameState.Score));
            SetText(bestText, FormatNumber(highScore));
            SetText(highestText, FormatNumber(gameState.HighestBlock));
            SetText(droppedText, $"Dobasok: {FormatNumber(gameState.BlocksDropped)}");

            RefreshNextBlocks();
            RefreshColumns();
            RefreshGameOver();
            RefreshProgressionUi();
        }

        private void RefreshProgressionUi()
        {
            if (progression == null)
            {
                return;
            }

            SetText(chipsTexts, $"Chips: {FormatNumber(progression.Chips)}");
            SetText(solverText, $"Solver: {GetSelectedSolver().DisplayName}");
            SetText(speedText, $"Speed L{progression.SpeedLevel} | {progression.MoveInterval:0.00}s");
            SetText(capacityText, $"Stack cap: {progression.StackCapacity}/{StackMergeGameState.MaxStackCapacity}");
            SetText(queueText, $"Next: {progression.QueueLength}");
            SetText(agentSlotsText, $"Active agents: {progression.ActiveAgentCount}/{progression.ActiveAgentSlots}");

            if (gameState != null && !gameState.IsGameOver)
            {
                SetText(runStatusText, progression.AutoSolveEnabled ? "Auto solving" : "Manual mode");
            }

            if (autoSolveToggle != null)
            {
                autoSolveToggle.SetIsOnWithoutNotify(progression.AutoSolveEnabled);
            }

            RefreshSolverButtons();
            RefreshSolverDetails();
            RefreshAgentButtons();
            RefreshAgentSlots();
            RefreshAgentDetails();
            RefreshUpgradeButtons();
        }

        private void RefreshSolverButtons()
        {
            for (int i = 0; i < solverButtons.Length && i < StackMergeSolverCatalog.Definitions.Length; i++)
            {
                Button button = solverButtons[i];
                if (button == null)
                {
                    continue;
                }

                SolverDefinition definition = StackMergeSolverCatalog.Definitions[i];
                bool unlocked = progression.IsSolverUnlocked(definition.Id);
                bool selectedInPanel = selectedSolverId == definition.Id;
                bool active = progression.SelectedSolver == definition.Id;

                string label = selectedInPanel ? $"> {definition.DisplayName}" : definition.DisplayName;
                if (unlocked)
                {
                    label += active ? "\nActive" : "\nUnlocked";
                }
                else
                {
                    label += $"\n{FormatNumber(definition.Cost)} chips";
                }

                SetButtonText(button, label);
                button.interactable = true;
                SetButtonColor(button, selectedInPanel ? HexColor("#1D4ED8") : active ? HexColor("#0F766E") : HexColor("#2563EB"));
            }
        }

        private void RefreshSolverDetails()
        {
            SolverDefinition definition = StackMergeSolverCatalog.GetDefinition(selectedSolverId);
            bool unlocked = progression.IsSolverUnlocked(definition.Id);
            bool active = progression.SelectedSolver == definition.Id;

            SetText(solverDetailNameText, definition.DisplayName);
            SetText(solverDetailInfoText, unlocked ? definition.Description : $"Unlock this algorithm to reveal details.\nCost: {FormatNumber(definition.Cost)} chips");

            if (!unlocked)
            {
                SetText(solverDetailStatusText, "Locked");
                SetButtonText(solverDetailActionButton, $"Unlock\n{FormatNumber(definition.Cost)}");
                if (solverDetailActionButton != null)
                {
                    solverDetailActionButton.interactable = progression.Chips >= definition.Cost;
                }
                return;
            }

            SetText(solverDetailStatusText, active ? "Active" : "Unlocked");
            SetButtonText(solverDetailActionButton, active ? "Selected" : "Select");
            if (solverDetailActionButton != null)
            {
                solverDetailActionButton.interactable = !active;
            }
        }

        private void RefreshAgentButtons()
        {
            for (int i = 0; i < agentButtons.Length && i < StackMergeProgression.Agents.Length; i++)
            {
                Button button = agentButtons[i];
                if (button == null)
                {
                    continue;
                }

                AgentDefinition definition = StackMergeProgression.Agents[i];
                bool unlocked = progression.IsAgentUnlocked(definition.Id);
                bool active = progression.IsAgentActive(definition.Id);
                bool selected = selectedAgentId == definition.Id;

                string label = selected ? $"> {definition.DisplayName}" : definition.DisplayName;
                if (unlocked)
                {
                    label += active ? "\nActive" : "\nUnlocked";
                }
                else
                {
                    label += $"\n{FormatNumber(definition.Cost)} chips";
                }

                SetButtonText(button, label);
                button.interactable = true;
                SetButtonColor(button, selected ? HexColor("#7C3AED") : active ? HexColor("#0F766E") : HexColor("#9333EA"));
            }
        }

        private void RefreshAgentSlots()
        {
            for (int i = 0; i < agentSlotTexts.Length; i++)
            {
                TMP_Text text = agentSlotTexts[i];
                if (text == null)
                {
                    continue;
                }

                int activeAgentId = progression.GetActiveAgentIdAtSlot(i);
                if (activeAgentId >= 0)
                {
                    AgentDefinition definition = progression.GetAgentDefinition((AgentId)activeAgentId);
                    SetText(text, $"Slot {i + 1}\n{definition.DisplayName}");
                    text.color = HexColor("#FDE68A");
                }
                else if (i >= progression.ActiveAgentSlots)
                {
                    SetText(text, "Bonus slot\nNeeds Coordinator");
                    text.color = HexColor("#64748B");
                }
                else
                {
                    SetText(text, $"Slot {i + 1}\nEmpty");
                    text.color = HexColor("#CBD5E1");
                }
            }
        }

        private void RefreshAgentDetails()
        {
            AgentDefinition definition = progression.GetAgentDefinition(selectedAgentId);
            bool unlocked = progression.IsAgentUnlocked(selectedAgentId);
            bool active = progression.IsAgentActive(selectedAgentId);

            SetText(agentDetailNameText, definition.DisplayName);
            SetText(agentDetailInfoText, unlocked ? definition.Description : $"{definition.LockedHint}\nCost: {FormatNumber(definition.Cost)} chips");

            if (!unlocked)
            {
                SetText(agentDetailStatusText, "Locked");
                SetButtonText(agentDetailActionButton, $"Buy\n{FormatNumber(definition.Cost)}");
                if (agentDetailActionButton != null)
                {
                    agentDetailActionButton.interactable = progression.Chips >= definition.Cost;
                }
                return;
            }

            if (active)
            {
                SetText(agentDetailStatusText, "Equipped");
                SetButtonText(agentDetailActionButton, "Unequip");
                if (agentDetailActionButton != null)
                {
                    agentDetailActionButton.interactable = true;
                }
                return;
            }

            bool hasFreeSlot = progression.ActiveAgentCount < progression.ActiveAgentSlots;
            SetText(agentDetailStatusText, hasFreeSlot ? "Unlocked" : "No free slot");
            SetButtonText(agentDetailActionButton, hasFreeSlot ? "Equip" : "Unequip someone first");
            if (agentDetailActionButton != null)
            {
                agentDetailActionButton.interactable = hasFreeSlot;
            }
        }

        private void RefreshUpgradeButtons()
        {
            for (int i = 0; i < speedUpgradeButtons.Length; i++)
            {
                Button button = speedUpgradeButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i < progression.SpeedLevel)
                {
                    SetButtonText(button, $"L{i + 1}\nDone");
                    button.interactable = false;
                    SetButtonColor(button, HexColor("#0F766E"));
                }
                else if (i == progression.SpeedLevel && !progression.IsMaxSpeed)
                {
                    long cost = progression.GetSpeedUpgradeCost(i);
                    SetButtonText(button, $"L{i + 1}\n{FormatNumber(cost)}");
                    button.interactable = progression.Chips >= cost;
                    SetButtonColor(button, HexColor("#0891B2"));
                }
                else
                {
                    SetButtonText(button, $"L{i + 1}\nLocked");
                    button.interactable = false;
                    SetButtonColor(button, HexColor("#334155"));
                }
            }

            if (autoRestartButton != null)
            {
                if (progression.AutoRestartUnlocked)
                {
                    SetButtonText(autoRestartButton, progression.AutoRestartEnabled ? "Auto restart\nON" : "Auto restart\nOFF");
                    autoRestartButton.interactable = true;
                }
                else
                {
                    long cost = progression.GetAutoRestartCost();
                    SetButtonText(autoRestartButton, $"Auto restart\n{FormatNumber(cost)}");
                    autoRestartButton.interactable = progression.Chips >= cost;
                }
            }

            for (int i = 0; i < stackCapacityUpgradeButtons.Length; i++)
            {
                Button button = stackCapacityUpgradeButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i < progression.StackCapacityLevel)
                {
                    SetButtonText(button, $"Cap {StackMergeGameState.DefaultStackCapacity + i + 1}\nDone");
                    button.interactable = false;
                    SetButtonColor(button, HexColor("#0F766E"));
                }
                else if (i == progression.StackCapacityLevel && !progression.IsMaxStackCapacity)
                {
                    long cost = progression.GetStackCapacityUpgradeCost(i);
                    SetButtonText(button, $"Cap {StackMergeGameState.DefaultStackCapacity + i + 1}\n{FormatNumber(cost)}");
                    button.interactable = progression.Chips >= cost;
                    SetButtonColor(button, HexColor("#4F46E5"));
                }
                else
                {
                    SetButtonText(button, $"Cap {StackMergeGameState.DefaultStackCapacity + i + 1}\nLocked");
                    button.interactable = false;
                    SetButtonColor(button, HexColor("#334155"));
                }
            }

            for (int i = 0; i < queuePreviewUpgradeButtons.Length; i++)
            {
                Button button = queuePreviewUpgradeButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i < progression.QueuePreviewLevel)
                {
                    SetButtonText(button, $"+{i + 1} next\nDone");
                    button.interactable = false;
                    SetButtonColor(button, HexColor("#0F766E"));
                }
                else if (i == progression.QueuePreviewLevel && !progression.IsMaxQueuePreview)
                {
                    long cost = progression.GetQueuePreviewUpgradeCost(i);
                    SetButtonText(button, $"+{i + 1} next\n{FormatNumber(cost)}");
                    button.interactable = progression.Chips >= cost;
                    SetButtonColor(button, HexColor("#7C3AED"));
                }
                else
                {
                    SetButtonText(button, $"+{i + 1} next\nLocked");
                    button.interactable = false;
                    SetButtonColor(button, HexColor("#334155"));
                }
            }

            for (int i = 0; i < incomeUpgradeButtons.Length; i++)
            {
                Button button = incomeUpgradeButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i < progression.IncomeLevel)
                {
                    SetButtonText(button, $"+{(i + 1) * 12}%\nDone");
                    button.interactable = false;
                    SetButtonColor(button, HexColor("#0F766E"));
                }
                else if (i == progression.IncomeLevel && !progression.IsMaxIncome)
                {
                    long cost = progression.GetIncomeUpgradeCost(i);
                    SetButtonText(button, $"+{(i + 1) * 12}%\n{FormatNumber(cost)}");
                    button.interactable = progression.Chips >= cost;
                    SetButtonColor(button, HexColor("#CA8A04"));
                }
                else
                {
                    SetButtonText(button, $"+{(i + 1) * 12}%\nLocked");
                    button.interactable = false;
                    SetButtonColor(button, HexColor("#334155"));
                }
            }
        }

        private void RefreshNextBlocks()
        {
            if (nextBlocksRoot == null)
            {
                return;
            }

            ClearChildren(nextBlocksRoot);

            int blockCount = gameState.NextBlocks.Count;
            float spacing = 16f;
            float availableWidth = Mathf.Max(360f, nextBlocksRoot.rect.width);
            float blockWidth = Mathf.Clamp((availableWidth - spacing * Mathf.Max(0, blockCount - 1)) / Mathf.Max(1, blockCount), 96f, 144f);
            float blockHeight = blockWidth >= 132f ? 78f : 70f;
            int fontSize = Mathf.RoundToInt(blockHeight * 0.44f);

            for (int i = 0; i < gameState.NextBlocks.Count; i++)
            {
                RectTransform block = CreateBlock(nextBlocksRoot, gameState.NextBlocks[i], blockWidth, blockHeight, fontSize);
                LayoutElement layout = EnsureComponent<LayoutElement>(block.gameObject);
                layout.preferredWidth = blockWidth;
                layout.preferredHeight = blockHeight;
            }
        }

        private void RefreshColumns()
        {
            if (stackBlockLayers == null || stackButtons == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();

            for (int stackIndex = 0; stackIndex < stackBlockLayers.Length; stackIndex++)
            {
                RectTransform layer = stackBlockLayers[stackIndex];
                if (layer == null)
                {
                    continue;
                }

                ClearChildren(layer);

                float layerWidth = Mathf.Max(128f, layer.rect.width);
                float layerHeight = Mathf.Max(420f, layer.rect.height);
                float padding = 12f;
                float spacing = 7f;
                float blockHeight = Mathf.Min(74f, (layerHeight - padding * 2f - spacing * (gameState.StackCapacity - 1)) / gameState.StackCapacity);
                float blockWidth = Mathf.Max(110f, layerWidth - padding * 2f);

                IReadOnlyList<int> stack = gameState.Stacks[stackIndex];
                for (int i = 0; i < stack.Count; i++)
                {
                    RectTransform block = CreateBlock(layer, stack[i], blockWidth, blockHeight, Mathf.RoundToInt(blockHeight * 0.44f));
                    block.anchorMin = new Vector2(0.5f, 0f);
                    block.anchorMax = new Vector2(0.5f, 0f);
                    block.pivot = new Vector2(0.5f, 0f);
                    block.anchoredPosition = new Vector2(0f, padding + i * (blockHeight + spacing));
                }

                if (stackIndex < stackButtons.Length && stackButtons[stackIndex] != null)
                {
                    stackButtons[stackIndex].interactable = gameState.CanPlace(stackIndex);
                }
            }
        }

        private void RefreshGameOver()
        {
            if (gameOverOverlay == null)
            {
                return;
            }

            gameOverOverlay.SetActive(gameState.IsGameOver && selectedTabIndex == 0);
            if (!gameState.IsGameOver)
            {
                return;
            }

            SetText(gameOverScoreText, $"Pont: {FormatNumber(gameState.Score)}");
            SetText(gameOverBestText, $"Rekord: {FormatNumber(highScore)}");
            SetText(runStatusText, progression != null && progression.AutoRestartUnlocked && progression.AutoRestartEnabled
                ? "Auto restart armed"
                : "Run ended");
        }

        private RectTransform CreateBlock(Transform parent, int value, float width, float height, int fontSize)
        {
            RectTransform block = blockTemplate != null
                ? Instantiate(blockTemplate, parent)
                : CreateFallbackBlock(parent);

            block.gameObject.SetActive(true);
            block.name = $"Block {value}";
            block.sizeDelta = new Vector2(width, height);

            Color color = GetBlockColor(value);
            Image image = block.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
                if (useGeneratedBlockSprites)
                {
                    image.sprite = GetRoundedSprite(color, HexColor("#000000", 0.18f), 18);
                    image.type = Image.Type.Sliced;
                }
            }

            TMP_Text text = block.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = FormatNumber(value);
                text.fontSize = fontSize;
                text.color = GetReadableTextColor(color);
                text.enableAutoSizing = true;
                text.fontSizeMin = 12;
                text.fontSizeMax = Mathf.Max(14, fontSize);
            }

            return block;
        }

        private static RectTransform CreateFallbackBlock(Transform parent)
        {
            GameObject gameObject = new GameObject("Block", typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);

            GameObject labelObject = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(gameObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            Stretch(labelRect, 6f, 4f, 6f, 4f);

            TMP_Text text = labelObject.GetComponent<TMP_Text>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;

            return gameObject.GetComponent<RectTransform>();
        }

        private Sprite GetRoundedSprite(Color fill, Color border, int radius)
        {
            string key = $"{ColorUtility.ToHtmlStringRGBA(fill)}-{ColorUtility.ToHtmlStringRGBA(border)}-{radius}";
            if (spriteCache.TryGetValue(key, out Sprite sprite))
            {
                return sprite;
            }

            const int size = 64;
            const int borderSize = 3;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color transparent = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool outer = InsideRoundedRect(x, y, size, size, radius);
                    bool inner = InsideRoundedRect(x, y, size, size, Mathf.Max(0, radius - borderSize), borderSize);
                    texture.SetPixel(x, y, outer ? (inner ? fill : border) : transparent);
                }
            }

            texture.Apply();
            Sprite created = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
            created.name = $"Rounded {key}";
            spriteCache[key] = created;
            return created;
        }

        private static bool InsideRoundedRect(int x, int y, int width, int height, int radius, int inset = 0)
        {
            int minX = inset;
            int minY = inset;
            int maxX = width - 1 - inset;
            int maxY = height - 1 - inset;

            if (x < minX || x > maxX || y < minY || y > maxY)
            {
                return false;
            }

            int localRadius = Mathf.Min(radius, Mathf.Min(maxX - minX, maxY - minY) / 2);
            int cx = Mathf.Clamp(x, minX + localRadius, maxX - localRadius);
            int cy = Mathf.Clamp(y, minY + localRadius, maxY - localRadius);
            int dx = x - cx;
            int dy = y - cy;
            return dx * dx + dy * dy <= localRadius * localRadius;
        }

        private static void ClearChildren(Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetText(TMP_Text[] targets, string value)
        {
            if (targets == null)
            {
                return;
            }

            foreach (TMP_Text target in targets)
            {
                SetText(target, value);
            }
        }

        private static void SetButtonText(Button button, string value)
        {
            TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            SetText(text, value);
        }

        private static void SetButtonColor(Button button, Color color)
        {
            Image image = button != null ? button.GetComponent<Image>() : null;
            if (image != null)
            {
                image.color = color;
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static void Stretch(RectTransform rectTransform, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static Color HexColor(string hex, float alpha = 1f)
        {
            if (!ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                color = Color.magenta;
            }

            color.a = alpha;
            return color;
        }

        private static Color GetBlockColor(int value)
        {
            return value switch
            {
                1 => HexColor("#FDE68A"),
                2 => HexColor("#FDBA74"),
                4 => HexColor("#5EEAD4"),
                8 => HexColor("#60A5FA"),
                16 => HexColor("#A78BFA"),
                32 => HexColor("#FB7185"),
                64 => HexColor("#34D399"),
                128 => HexColor("#F472B6"),
                256 => HexColor("#F59E0B"),
                512 => HexColor("#22D3EE"),
                1024 => HexColor("#8B5CF6"),
                2048 => HexColor("#EF4444"),
                _ => Color.Lerp(HexColor("#EC4899"), HexColor("#10B981"), Mathf.PingPong(Mathf.Log(value, 2f) * 0.17f, 1f))
            };
        }

        private static Color GetReadableTextColor(Color background)
        {
            float luminance = background.r * 0.299f + background.g * 0.587f + background.b * 0.114f;
            return luminance > 0.62f ? HexColor("#111827") : Color.white;
        }

        private static string FormatNumber(long value)
        {
            if (value >= 1_000_000_000)
            {
                return $"{value / 1_000_000_000f:0.#}B";
            }

            if (value >= 1_000_000)
            {
                return $"{value / 1_000_000f:0.#}M";
            }

            if (value >= 10_000)
            {
                return $"{value / 1_000f:0.#}k";
            }

            return value.ToString();
        }
    }
}
