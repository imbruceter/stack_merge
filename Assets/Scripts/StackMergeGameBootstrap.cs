using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly IStackMergeSolver[] solvers = StackMergeSolverFactory.CreateAll();

        [Header("Scene UI")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Canvas canvas;
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text bestText;
        [SerializeField] private TMP_Text highestText;
        [SerializeField] private TMP_Text droppedText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private RectTransform nextBlocksRoot;
        [SerializeField] private RectTransform boardRoot;
        [SerializeField] private Button[] stackButtons = Array.Empty<Button>();
        [SerializeField] private RectTransform[] stackBlockLayers = Array.Empty<RectTransform>();
        [SerializeField] private Button[] newGameButtons = Array.Empty<Button>();
        [SerializeField] private Button historyButton;
        [SerializeField] private Button achievementsButton;
        [SerializeField] private Button gameplayInfoButton;
        [SerializeField] private GameObject gameplayInfoOverlay;
        [SerializeField] private TMP_Text gameplayInfoText;
        [SerializeField] private Button gameplayInfoCloseButton;

        [Header("Tabs")]
        [SerializeField] private GameObject gameplayPanel;
        [SerializeField] private GameObject algorithmsPanel;
        [SerializeField] private GameObject upgradesPanel;
        [SerializeField] private GameObject modifiersPanel;
        [SerializeField] private GameObject historyPanel;
        [SerializeField] private GameObject achievementsPanel;
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
        [SerializeField] private Button autoSolveButton;
        [SerializeField] private Button[] solverButtons = Array.Empty<Button>();
        [SerializeField] private TMP_Text solverDetailNameText;
        [SerializeField] private TMP_Text solverDetailInfoText;
        [SerializeField] private TMP_Text solverDetailStatusText;
        [SerializeField] private Button solverDetailActionButton;
        [SerializeField] private Button solverDetailTuneButton;
        [SerializeField] private GameObject solverTunePanel;
        [SerializeField] private TMP_Text solverTuneTitleText;
        [SerializeField] private TMP_Text solverTuneSummaryText;
        [SerializeField] private GameObject[] solverTuneRows = Array.Empty<GameObject>();
        [SerializeField] private TMP_Text[] solverTuneNameTexts = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] solverTuneValueTexts = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] solverTuneDescriptionTexts = Array.Empty<TMP_Text>();
        [SerializeField] private Slider[] solverTuneSliders = Array.Empty<Slider>();
        [SerializeField] private Button solverTuneBackButton;
        [SerializeField] private Button solverTuneResetButton;
        [SerializeField] private Button[] speedUpgradeButtons = Array.Empty<Button>();
        [SerializeField] private Button autoRestartButton;
        [SerializeField] private Button tokenPackButton;
        [SerializeField] private Button solverTuningUnlockButton;
        [SerializeField] private Button extraAgentSlotUpgradeButton;
        [SerializeField] private Button[] stackCapacityUpgradeButtons = Array.Empty<Button>();
        [SerializeField] private Button[] queuePreviewUpgradeButtons = Array.Empty<Button>();
        [SerializeField] private Button[] incomeUpgradeButtons = Array.Empty<Button>();
        [SerializeField] private Button[] difficultyUpgradeButtons = Array.Empty<Button>();
        [SerializeField] private TMP_Text progressionStageText;
        [SerializeField] private Button modifiersMenuUnlockButton;
        [SerializeField] private Button agentsMenuUnlockButton;
        [SerializeField] private Button[] modifierButtons = Array.Empty<Button>();
        [SerializeField] private TMP_Text modifierSummaryText;
        [SerializeField] private TMP_Text modifierDetailNameText;
        [SerializeField] private TMP_Text modifierDetailInfoText;
        [SerializeField] private TMP_Text modifierDetailStatusText;
        [SerializeField] private Button modifierDetailActionButton;
        [SerializeField] private TMP_Text historySummaryText;
        [SerializeField] private RectTransform historyChartRoot;
        [SerializeField] private RectTransform historySolverTableRoot;
        [SerializeField] private RectTransform historyRecentRunsRoot;
        [SerializeField] private TMP_Text historyInsightText;
        [SerializeField] private Button historyBackButton;
        [SerializeField] private TMP_Text achievementStatsText;
        [SerializeField] private RectTransform achievementListRoot;
        [SerializeField] private Button achievementBackButton;
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
        private bool historyOpen;
        private bool achievementsOpen;
        private bool solverTuneOpen;
        private bool gameplayInfoOpen;
        private bool currentRunUsedAutoSolve;
        private int currentRunManualMoves;
        private float currentRunElapsed;
        private SolverId selectedSolverId = SolverId.Rand;
        private AgentId selectedAgentId = AgentId.MergeBroker;
        private ModifierId selectedModifierId = ModifierId.UnstableStack;

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
            if (gameState != null && !gameState.IsGameOver)
            {
                currentRunElapsed += Time.deltaTime;
            }
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
            RectTransform board,
            Button[] columns,
            RectTransform[] blockLayers,
            Button[] resetButtons,
            Button openHistoryButton,
            Button openAchievementsButton,
            Button openGameplayInfoButton,
            GameObject gameplayInfo,
            TMP_Text gameplayInfoDetails,
            Button closeGameplayInfoButton,
            GameObject gameplay,
            GameObject algorithms,
            GameObject upgrades,
            GameObject modifiers,
            GameObject history,
            GameObject achievements,
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
            Button autoSolve,
            Button[] solverSelectionButtons,
            TMP_Text selectedSolverName,
            TMP_Text selectedSolverInfo,
            TMP_Text selectedSolverStatus,
            Button selectedSolverAction,
            Button selectedSolverTune,
            GameObject tunePanel,
            TMP_Text tuneTitle,
            TMP_Text tuneSummary,
            GameObject[] tuneRows,
            TMP_Text[] tuneNames,
            TMP_Text[] tuneValues,
            TMP_Text[] tuneDescriptions,
            Slider[] tuneSliders,
            Button tuneBack,
            Button tuneReset,
            Button[] speedButtons,
            Button restartButton,
            Button buyTokensButton,
            Button unlockSolverTuningButton,
            Button unlockExtraAgentSlotButton,
            Button[] capacityButtons,
            Button[] queueButtons,
            Button[] incomeButtons,
            Button[] difficultyButtons,
            TMP_Text stageText,
            Button unlockModifiersButton,
            Button unlockAgentsButton,
            Button[] modifierSelectionButtons,
            TMP_Text modifiersSummary,
            TMP_Text selectedModifierName,
            TMP_Text selectedModifierInfo,
            TMP_Text selectedModifierStatus,
            Button selectedModifierAction,
            TMP_Text historySummary,
            RectTransform historyChart,
            RectTransform historySolverTable,
            RectTransform historyRecentRuns,
            TMP_Text historyInsight,
            Button closeHistoryButton,
            TMP_Text achievementStats,
            RectTransform achievementList,
            Button closeAchievementButton,
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
            boardRoot = board;
            stackButtons = columns;
            stackBlockLayers = blockLayers;
            newGameButtons = resetButtons;
            historyButton = openHistoryButton;
            achievementsButton = openAchievementsButton;
            gameplayInfoButton = openGameplayInfoButton;
            gameplayInfoOverlay = gameplayInfo;
            gameplayInfoText = gameplayInfoDetails;
            gameplayInfoCloseButton = closeGameplayInfoButton;
            gameplayPanel = gameplay;
            algorithmsPanel = algorithms;
            upgradesPanel = upgrades;
            modifiersPanel = modifiers;
            historyPanel = history;
            achievementsPanel = achievements;
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
            autoSolveButton = autoSolve;
            solverButtons = solverSelectionButtons;
            solverDetailNameText = selectedSolverName;
            solverDetailInfoText = selectedSolverInfo;
            solverDetailStatusText = selectedSolverStatus;
            solverDetailActionButton = selectedSolverAction;
            solverDetailTuneButton = selectedSolverTune;
            solverTunePanel = tunePanel;
            solverTuneTitleText = tuneTitle;
            solverTuneSummaryText = tuneSummary;
            solverTuneRows = tuneRows;
            solverTuneNameTexts = tuneNames;
            solverTuneValueTexts = tuneValues;
            solverTuneDescriptionTexts = tuneDescriptions;
            solverTuneSliders = tuneSliders;
            solverTuneBackButton = tuneBack;
            solverTuneResetButton = tuneReset;
            speedUpgradeButtons = speedButtons;
            autoRestartButton = restartButton;
            tokenPackButton = buyTokensButton;
            solverTuningUnlockButton = unlockSolverTuningButton;
            extraAgentSlotUpgradeButton = unlockExtraAgentSlotButton;
            stackCapacityUpgradeButtons = capacityButtons;
            queuePreviewUpgradeButtons = queueButtons;
            incomeUpgradeButtons = incomeButtons;
            difficultyUpgradeButtons = difficultyButtons;
            progressionStageText = stageText;
            modifiersMenuUnlockButton = unlockModifiersButton;
            agentsMenuUnlockButton = unlockAgentsButton;
            modifierButtons = modifierSelectionButtons;
            modifierSummaryText = modifiersSummary;
            modifierDetailNameText = selectedModifierName;
            modifierDetailInfoText = selectedModifierInfo;
            modifierDetailStatusText = selectedModifierStatus;
            modifierDetailActionButton = selectedModifierAction;
            historySummaryText = historySummary;
            historyChartRoot = historyChart;
            historySolverTableRoot = historySolverTable;
            historyRecentRunsRoot = historyRecentRuns;
            historyInsightText = historyInsight;
            historyBackButton = closeHistoryButton;
            achievementStatsText = achievementStats;
            achievementListRoot = achievementList;
            achievementBackButton = closeAchievementButton;
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
                stackButtons[i].onClick.AddListener(() => PlaceOnStack(stackIndex, "Manual", false));
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

            if (historyButton != null)
            {
                historyButton.onClick.RemoveAllListeners();
                historyButton.onClick.AddListener(OpenHistoryPanel);
            }

            if (historyBackButton != null)
            {
                historyBackButton.onClick.RemoveAllListeners();
                historyBackButton.onClick.AddListener(CloseHistoryPanel);
            }

            if (achievementsButton != null)
            {
                achievementsButton.onClick.RemoveAllListeners();
                achievementsButton.onClick.AddListener(OpenAchievementsPanel);
            }

            if (gameplayInfoButton != null)
            {
                gameplayInfoButton.onClick.RemoveAllListeners();
                gameplayInfoButton.onClick.AddListener(OpenGameplayInfo);
            }

            if (gameplayInfoCloseButton != null)
            {
                gameplayInfoCloseButton.onClick.RemoveAllListeners();
                gameplayInfoCloseButton.onClick.AddListener(CloseGameplayInfo);
            }

            if (achievementBackButton != null)
            {
                achievementBackButton.onClick.RemoveAllListeners();
                achievementBackButton.onClick.AddListener(CloseAchievementsPanel);
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

            if (autoSolveButton != null)
            {
                autoSolveButton.onClick.RemoveAllListeners();
                autoSolveButton.onClick.AddListener(ToggleOrBuyAutoSolve);
            }

            if (tokenPackButton != null)
            {
                tokenPackButton.onClick.RemoveAllListeners();
                tokenPackButton.onClick.AddListener(BuyTokenPack);
            }

            if (solverTuningUnlockButton != null)
            {
                solverTuningUnlockButton.onClick.RemoveAllListeners();
                solverTuningUnlockButton.onClick.AddListener(BuySolverTuningUnlock);
            }

            if (extraAgentSlotUpgradeButton != null)
            {
                extraAgentSlotUpgradeButton.onClick.RemoveAllListeners();
                extraAgentSlotUpgradeButton.onClick.AddListener(BuyExtraAgentSlotUpgrade);
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

            for (int i = 0; i < difficultyUpgradeButtons.Length; i++)
            {
                int upgradeIndex = i;
                if (difficultyUpgradeButtons[i] == null)
                {
                    continue;
                }

                difficultyUpgradeButtons[i].onClick.RemoveAllListeners();
                difficultyUpgradeButtons[i].onClick.AddListener(() => BuyDifficultyUpgrade(upgradeIndex));
            }

            if (modifiersMenuUnlockButton != null)
            {
                modifiersMenuUnlockButton.onClick.RemoveAllListeners();
                modifiersMenuUnlockButton.onClick.AddListener(BuyModifiersMenuUnlock);
            }

            for (int i = 0; i < modifierButtons.Length; i++)
            {
                int modifierIndex = i;
                if (modifierButtons[i] == null)
                {
                    continue;
                }

                modifierButtons[i].onClick.RemoveAllListeners();
                modifierButtons[i].onClick.AddListener(() => SelectModifier((ModifierId)modifierIndex));
            }

            if (agentsMenuUnlockButton != null)
            {
                agentsMenuUnlockButton.onClick.RemoveAllListeners();
                agentsMenuUnlockButton.onClick.AddListener(BuyAgentsMenuUnlock);
            }

            if (solverDetailActionButton != null)
            {
                solverDetailActionButton.onClick.RemoveAllListeners();
                solverDetailActionButton.onClick.AddListener(HandleSelectedSolverAction);
            }

            if (solverDetailTuneButton != null)
            {
                solverDetailTuneButton.onClick.RemoveAllListeners();
                solverDetailTuneButton.onClick.AddListener(OpenSolverTunePanel);
            }

            for (int i = 0; i < solverTuneSliders.Length; i++)
            {
                int slotIndex = i;
                Slider slider = solverTuneSliders[i];
                if (slider == null)
                {
                    continue;
                }

                slider.onValueChanged.RemoveAllListeners();
                slider.onValueChanged.AddListener(value => SetSelectedSolverTuningFromDisplay(slotIndex, value));
            }

            if (solverTuneBackButton != null)
            {
                solverTuneBackButton.onClick.RemoveAllListeners();
                solverTuneBackButton.onClick.AddListener(CloseSolverTunePanel);
            }

            if (solverTuneResetButton != null)
            {
                solverTuneResetButton.onClick.RemoveAllListeners();
                solverTuneResetButton.onClick.AddListener(ResetSelectedSolverTuning);
            }

            if (agentDetailActionButton != null)
            {
                agentDetailActionButton.onClick.RemoveAllListeners();
                agentDetailActionButton.onClick.AddListener(HandleSelectedAgentAction);
            }

            if (modifierDetailActionButton != null)
            {
                modifierDetailActionButton.onClick.RemoveAllListeners();
                modifierDetailActionButton.onClick.AddListener(BuySelectedModifierUpgrade);
            }
        }

        private void SelectTab(int tabIndex)
        {
            historyOpen = false;
            achievementsOpen = false;
            solverTuneOpen = false;
            gameplayInfoOpen = false;
            int requestedTab = Mathf.Clamp(tabIndex, 0, 5);
            if (requestedTab == 3 && progression != null && !progression.ModifiersMenuUnlocked)
            {
                SetText(feedbackText, "Unlock Modifier Lab in Upgrades first");
                requestedTab = selectedTabIndex;
            }

            if (requestedTab == 4 && progression != null && !progression.AgentsMenuUnlocked)
            {
                SetText(feedbackText, "Unlock Agents in Upgrades first");
                requestedTab = selectedTabIndex;
            }

            selectedTabIndex = requestedTab;
            SetActive(gameplayPanel, selectedTabIndex == 0);
            SetActive(algorithmsPanel, selectedTabIndex == 1);
            SetActive(upgradesPanel, selectedTabIndex == 2);
            SetActive(modifiersPanel, selectedTabIndex == 3);
            SetActive(historyPanel, false);
            SetActive(achievementsPanel, false);
            SetActive(agentsPanel, selectedTabIndex == 4);
            SetActive(settingsPanel, selectedTabIndex == 5);
            SetActive(solverTunePanel, false);
            SetActive(gameplayInfoOverlay, false);
            RefreshTabButtons();

            if (gameState != null)
            {
                RefreshGameOver();
            }
        }

        private void OpenHistoryPanel()
        {
            historyOpen = true;
            achievementsOpen = false;
            solverTuneOpen = false;
            gameplayInfoOpen = false;
            SetActive(gameplayPanel, false);
            SetActive(algorithmsPanel, false);
            SetActive(upgradesPanel, false);
            SetActive(modifiersPanel, false);
            SetActive(achievementsPanel, false);
            SetActive(agentsPanel, false);
            SetActive(settingsPanel, false);
            SetActive(historyPanel, true);
            SetActive(solverTunePanel, false);
            SetActive(gameplayInfoOverlay, false);
            RefreshHistory();
            RefreshTabButtons();
            RefreshGameOver();
        }

        private void CloseHistoryPanel()
        {
            SelectTab(0);
        }

        private void OpenAchievementsPanel()
        {
            achievementsOpen = true;
            historyOpen = false;
            solverTuneOpen = false;
            gameplayInfoOpen = false;
            SetActive(gameplayPanel, false);
            SetActive(algorithmsPanel, false);
            SetActive(upgradesPanel, false);
            SetActive(historyPanel, false);
            SetActive(modifiersPanel, false);
            SetActive(agentsPanel, false);
            SetActive(settingsPanel, false);
            SetActive(achievementsPanel, true);
            SetActive(solverTunePanel, false);
            SetActive(gameplayInfoOverlay, false);
            RefreshAchievements();
            RefreshTabButtons();
            RefreshGameOver();
        }

        private void CloseAchievementsPanel()
        {
            SelectTab(0);
        }

        private void OpenGameplayInfo()
        {
            gameplayInfoOpen = true;
            RefreshGameplayInfo();
            SetActive(gameplayInfoOverlay, true);
        }

        private void CloseGameplayInfo()
        {
            gameplayInfoOpen = false;
            SetActive(gameplayInfoOverlay, false);
        }

        private void OpenSolverTunePanel()
        {
            if (progression == null)
            {
                return;
            }

            SolverTuningDefinition tuningDefinition = StackMergeSolverCatalog.GetTuningDefinition(selectedSolverId);
            if (selectedSolverId == SolverId.MachineLearning && progression.IsSolverUnlocked(SolverId.MachineLearning))
            {
                progression.ToggleMachineLearningTrainingMode();
                progression.Save();
                SetText(feedbackText, progression.MachineLearningTrainingMode ? "DQN training mode enabled: chips paused" : "DQN normal mode enabled");
                RefreshEverything();
                return;
            }

            if (!progression.SolverTuningUnlocked || !progression.IsSolverUnlocked(selectedSolverId) || !tuningDefinition.HasParameters)
            {
                return;
            }

            solverTuneOpen = true;
            SetActive(solverTunePanel, true);
            RefreshSolverTunePanel();
        }

        private void CloseSolverTunePanel()
        {
            solverTuneOpen = false;
            SetActive(solverTunePanel, false);
            RefreshSolverDetails();
        }

        private void RefreshTabButtons()
        {
            string[] labels = { "Jatek", "Algoritmus", "Upgrade", "Modifiers", "Agent", "Settings" };
            for (int i = 0; i < tabButtons.Length; i++)
            {
                Button button = tabButtons[i];
                if (button == null)
                {
                    continue;
                }

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                Image background = button.GetComponent<Image>();
                bool selected = !historyOpen && !achievementsOpen && i == selectedTabIndex;
                bool lockedModifierTab = i == 3 && progression != null && !progression.ModifiersMenuUnlocked;
                bool lockedAgentTab = i == 4 && progression != null && !progression.AgentsMenuUnlocked;
                bool locked = lockedModifierTab || lockedAgentTab;

                if (label != null)
                {
                    label.text = lockedModifierTab ? "Modifiers\nLocked" : lockedAgentTab ? "Agent\nLocked" : i < labels.Length ? labels[i] : label.text;
                    label.color = locked ? HexColor("#64748B") : selected ? HexColor("#FDE68A") : Color.white;
                }

                if (background != null)
                {
                    background.color = locked ? HexColor("#111827") : selected ? HexColor("#1D4ED8") : HexColor("#1F2937");
                }

                button.interactable = !locked;
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
                        if (progression.TryConsumeAutoRestartToken())
                        {
                            progression.Save();
                            StartNewGame();
                        }
                        else
                        {
                            autoRestartTimer = 0f;
                            SetText(runStatusText, "Auto restart needs token");
                            RefreshProgressionUi();
                        }
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
                new SolverContext(
                    solverRandom,
                    progression.MonteCarloSimulationCount,
                    progression.MonteCarloRolloutDepth,
                    tuning: progression.SolverTuningUnlocked
                        ? progression.GetSolverTuning(progression.SelectedSolver)
                        : SolverTuningSettings.Neutral(progression.SelectedSolver),
                    highTierSpeedTuningAccelerator: progression.NeuralAcceleratorActive,
                    machineLearningSkill: progression.MachineLearningSkill));

            if (decision.HasMove)
            {
                ApplySolverDecision(decision);
            }
        }

        private IStackMergeSolver GetSelectedSolver()
        {
            int solverIndex = Mathf.Clamp((int)progression.SelectedSolver, 0, solvers.Length - 1);
            return solvers[solverIndex];
        }

        private void PlaceOnStack(int stackIndex, string reason, bool autoSolverMove)
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

            HandleAcceptedMove(result, reason, autoSolverMove, wasGameOver);
        }

        private void ApplySolverDecision(SolverDecision decision)
        {
            if (gameState == null || progression == null)
            {
                return;
            }

            if (!SolverScoring.CanApplyDecision(gameState, decision))
            {
                SetText(feedbackText, decision.Reason);
                RefreshEverything();
                return;
            }

            bool wasGameOver = gameState.IsGameOver;
            MoveResult result = SolverScoring.ApplyDecision(gameState, decision);
            if (!result.Accepted)
            {
                SetText(feedbackText, result.Reason);
                RefreshEverything();
                return;
            }

            HandleAcceptedMove(result, decision.Reason, true, wasGameOver);
        }

        private void HandleAcceptedMove(MoveResult result, string reason, bool autoSolverMove, bool wasGameOver)
        {
            bool machineLearningTraining = progression.IsMachineLearningTrainingActive;
            long chipsGained = progression.AwardMove(result, machineLearningTraining);
            if (autoSolverMove)
            {
                currentRunUsedAutoSolve = true;
            }
            else
            {
                currentRunManualMoves++;
            }

            long runBonus = 0;
            float learningGain = 0f;
            if (!wasGameOver && result.IsGameOver)
            {
                bool manualRun = currentRunManualMoves > 0 && !currentRunUsedAutoSolve;
                runBonus = progression.AwardRunCompleted(
                    gameState.Score,
                    progression.SelectedSolver,
                    gameState.BlocksDropped,
                    gameState.TotalMerges,
                    gameState.HighestMergedBlock,
                    manualRun,
                    currentRunElapsed,
                    machineLearningTraining);
                if (progression.SelectedSolver == SolverId.MachineLearning)
                {
                    learningGain = progression.AwardMachineLearningRun(
                        gameState.Score,
                        gameState.BlocksDropped,
                        gameState.TotalMerges,
                        gameState.HighestMergedBlock,
                        machineLearningTraining);
                }
            }

            progression.Save();
            UpdateHighScore();

            string chipText = machineLearningTraining
                ? "training: +0 chips"
                : runBonus > 0 ? $"+{chipsGained + runBonus} chips" : $"+{chipsGained} chips";
            string moveText = result.ActionKind switch
            {
                SolverActionKind.Pickaxe => $"Pickaxe -{FormatNumber(result.RemovedValue)}",
                SolverActionKind.QueueSkip => $"Scrubbed {FormatNumber(result.RemovedValue)}",
                _ => result.MergeCount > 0
                    ? $"Merge x{result.MergeCount}: {FormatNumber(result.ResultingTopValue)}"
                    : $"+{FormatNumber(result.PlacedValue)}"
            };
            string resultReason = string.IsNullOrWhiteSpace(result.Reason)
                ? reason
                : string.IsNullOrWhiteSpace(reason)
                    ? result.Reason
                    : $"{reason}; {result.Reason}";
            string learningText = learningGain > 0f ? $" | +{learningGain:0} ML XP" : string.Empty;
            SetText(feedbackText, $"{moveText} | {chipText}{learningText} | {resultReason}");

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
            int difficulty = progression != null ? progression.DifficultyLevel : 0;
            StackMergeRunModifiers modifiers = progression != null ? progression.BuildRunModifiers() : default;
            gameState = new StackMergeGameState(stackCapacity: capacity, queueLength: queueLength, difficultyLevel: difficulty, modifiers: modifiers, seed: Environment.TickCount);
            autoSolveTimer = 0f;
            autoRestartTimer = 0f;
            currentRunUsedAutoSolve = false;
            currentRunManualMoves = 0;
            currentRunElapsed = 0f;
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
            solverTuneOpen = false;
            SetActive(solverTunePanel, false);
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
            string failure = selectedSolverId == SolverId.MachineLearning && !progression.CanUnlockMachineLearning
                ? "DQN requires every Modifier maxed"
                : "Not enough chips";
            SetText(feedbackText, changed ? $"Solver: {definition.DisplayName}" : failure);
            progression.Save();
            RefreshEverything();
        }

        private void SetSelectedSolverTuningFromDisplay(int slotIndex, float displayValue)
        {
            if (progression == null || !progression.SolverTuningUnlocked || !progression.IsSolverUnlocked(selectedSolverId))
            {
                RefreshSolverTunePanel();
                return;
            }

            SolverTuningDefinition tuningDefinition = StackMergeSolverCatalog.GetTuningDefinition(selectedSolverId);
            if (slotIndex < 0 || slotIndex >= tuningDefinition.Parameters.Length)
            {
                RefreshSolverTunePanel();
                return;
            }

            int value = tuningDefinition.Parameters[slotIndex].FromDisplayValue(displayValue);
            progression.SetSolverTuningValue(selectedSolverId, slotIndex, value);
            progression.Save();

            SolverDefinition definition = StackMergeSolverCatalog.GetDefinition(selectedSolverId);
            SetText(feedbackText, $"{definition.DisplayName} tuning updated");
            RefreshSolverTunePanel();
            RefreshSolverDetails();
            RefreshGameplayInfo();
        }

        private void ResetSelectedSolverTuning()
        {
            if (progression == null || !progression.SolverTuningUnlocked || !progression.IsSolverUnlocked(selectedSolverId))
            {
                return;
            }

            progression.ResetSolverTuning(selectedSolverId);
            progression.Save();

            SolverDefinition definition = StackMergeSolverCatalog.GetDefinition(selectedSolverId);
            SetText(feedbackText, $"{definition.DisplayName} tuning reset");
            RefreshSolverTunePanel();
            RefreshSolverDetails();
            RefreshGameplayInfo();
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
            SetText(feedbackText, changed ? "Auto restart updated" : progression.HasPurchasedSolver ? "Not enough chips" : "Buy an algorithm first");
            progression.Save();
            RefreshEverything();
        }

        private void ToggleOrBuyAutoSolve()
        {
            if (progression == null)
            {
                return;
            }

            bool changed = progression.ToggleOrBuyAutoSolve();
            SetText(feedbackText, changed ? "Auto solve updated" : progression.HasPurchasedSolver ? "Not enough chips" : "Buy an algorithm first");
            progression.Save();
            RefreshEverything();
        }

        private void BuyTokenPack()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyTokenPack();
            SetText(feedbackText, bought ? $"+{progression.GetTokenPackSize()} tokens" : "Not enough chips");
            progression.Save();
            RefreshEverything();
        }

        private void BuySolverTuningUnlock()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuySolverTuningUnlock();
            SetText(feedbackText, bought ? "Solver tuning unlocked" : "Not enough chips");
            progression.Save();
            RefreshEverything();
        }

        private void BuyExtraAgentSlotUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyExtraAgentSlotUpgrade();
            SetText(feedbackText, bought ? "+1 agent slot unlocked" : "Not enough chips");
            progression.Save();
            RefreshEverything();
        }

        private void BuyModifiersMenuUnlock()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyModifiersMenuUnlock();
            SetText(feedbackText, bought ? "Modifier Lab unlocked" : progression.CanUnlockModifiersMenu ? "Not enough chips" : "Modifier Lab requirements not met");
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

        private void BuyDifficultyUpgrade(int upgradeIndex)
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyDifficultyUpgrade(upgradeIndex);
            SetText(feedbackText, bought ? $"Risk level {progression.DifficultyLevel}" : "Risk upgrade unavailable");
            if (bought)
            {
                ApplyCurrentBoardSettingsToGameState();
            }

            progression.Save();
            RefreshEverything();
        }

        private void BuyModifierUpgrade(ModifierId modifierId)
        {
            if (progression == null)
            {
                return;
            }

            ModifierDefinition definition = progression.GetModifierDefinition(modifierId);
            bool bought = progression.BuyModifierUpgrade(modifierId);
            SetText(feedbackText, bought ? $"{definition.DisplayName} level {progression.GetModifierLevel(modifierId)}" : "Modifier upgrade unavailable");
            progression.Save();
            RefreshEverything();
        }

        private void SelectModifier(ModifierId modifierId)
        {
            selectedModifierId = modifierId;
            RefreshModifierButtons();
            RefreshModifierDetails();
        }

        private void BuySelectedModifierUpgrade()
        {
            BuyModifierUpgrade(selectedModifierId);
        }

        private void BuyAgentsMenuUnlock()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyAgentsMenuUnlock();
            SetText(feedbackText, bought ? "Agents menu unlocked" : "Not enough chips");
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

            if (!progression.AgentsMenuUnlocked)
            {
                SetText(feedbackText, "Unlock Agents in Upgrades first");
                RefreshEverything();
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
                || (gameState.StackCapacity == progression.StackCapacity
                    && gameState.QueueLength == progression.QueueLength
                    && gameState.DifficultyLevel == progression.DifficultyLevel))
            {
                return;
            }

            StackMergeSnapshot snapshot = gameState.CreateSnapshot();
            var resizedGame = new StackMergeGameState(
                stackCapacity: progression.StackCapacity,
                queueLength: progression.QueueLength,
                difficultyLevel: progression.DifficultyLevel,
                modifiers: snapshot.Modifiers,
                seed: Environment.TickCount);
            resizedGame.RestoreSnapshotResized(snapshot);
            gameState = resizedGame;
        }

        private void RefreshEverything()
        {
            if (gameState == null)
            {
                return;
            }

            SetText(scoreText, FormatNumber(gameState.Score));
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

            bool machineLearningTraining = progression.IsMachineLearningTrainingActive;
            SetText(chipsTexts, $"Chips: {FormatNumber(progression.Chips)} | Tokens: {FormatNumber(progression.Tokens)}");
            SetText(solverText, progression.SelectedSolver == SolverId.MachineLearning
                ? $"Solver: {GetSelectedSolver().DisplayName} Lv {progression.MachineLearningLevel}"
                : $"Solver: {GetSelectedSolver().DisplayName}");
            SetText(speedText, machineLearningTraining
                ? $"Speed L{progression.SpeedLevel} | {progression.MoveInterval:0.000}s | training"
                : $"Speed L{progression.SpeedLevel} | {progression.MoveInterval:0.00}s");

            if (gameState != null && !gameState.IsGameOver)
            {
                SetText(runStatusText, machineLearningTraining
                    ? "ML TRAINING - chips paused"
                    : progression.AutoSolveEnabled ? "Auto solving" : "Manual mode");
                if (runStatusText != null)
                {
                    runStatusText.color = machineLearningTraining ? HexColor("#F0ABFC") : HexColor("#D1D5DB");
                }
            }

            if (feedbackText != null)
            {
                feedbackText.color = machineLearningTraining ? HexColor("#F0ABFC") : HexColor("#5EEAD4");
            }

            RefreshSolverButtons();
            RefreshSolverDetails();
            if (solverTuneOpen)
            {
                RefreshSolverTunePanel();
            }
            RefreshAgentButtons();
            RefreshAgentSlots();
            RefreshAgentDetails();
            RefreshModifierButtons();
            RefreshModifierDetails();
            RefreshUpgradeButtons();
            RefreshHistory();
            RefreshAchievements();
            RefreshTabButtons();
            RefreshGameplayInfo();
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
                bool machineLearningGateReady = definition.Id != SolverId.MachineLearning || progression.CanUnlockMachineLearning;

                string label = selectedInPanel ? $"> {definition.DisplayName}" : definition.DisplayName;
                if (unlocked)
                {
                    label += active ? "\nActive" : "\nUnlocked";
                }
                else if (definition.Id == SolverId.MachineLearning && !machineLearningGateReady)
                {
                    label += "\nNeeds modifiers";
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
            SolverTuningDefinition tuningDefinition = StackMergeSolverCatalog.GetTuningDefinition(selectedSolverId);
            bool canTune = unlocked && progression.SolverTuningUnlocked && tuningDefinition.HasParameters;
            bool isMachineLearning = definition.Id == SolverId.MachineLearning;
            bool machineLearningGateReady = !isMachineLearning || progression.CanUnlockMachineLearning;

            SetText(solverDetailNameText, definition.DisplayName);
            string lockedInfo = isMachineLearning
                ? $"{progression.GetMachineLearningGateStatus()}\nCost: {FormatNumber(definition.Cost)} chips"
                : $"Unlock this algorithm to reveal details.\nCost: {FormatNumber(definition.Cost)} chips";
            SetText(solverDetailInfoText, unlocked
                ? isMachineLearning ? $"{definition.Description}\n\n{progression.GetMachineLearningStatus()}" : definition.Description
                : lockedInfo);
            SetButtonText(solverDetailTuneButton,
                isMachineLearning && unlocked
                    ? progression.MachineLearningTrainingMode ? "Training\nON" : "Training\nOFF"
                    : !unlocked ? "Tune\nLocked" : !progression.SolverTuningUnlocked ? "Tune\nUpgrade" : canTune ? "Tune" : "No tuning");
            if (solverDetailTuneButton != null)
            {
                solverDetailTuneButton.interactable = isMachineLearning && unlocked || canTune;
            }

            if (!unlocked)
            {
                SetText(solverDetailStatusText, isMachineLearning && !machineLearningGateReady ? "Stage locked" : "Locked");
                SetButtonText(solverDetailActionButton, isMachineLearning && !machineLearningGateReady ? "Needs all\nModifiers" : $"Unlock\n{FormatNumber(definition.Cost)}");
                if (solverDetailActionButton != null)
                {
                    solverDetailActionButton.interactable = machineLearningGateReady && progression.Chips >= definition.Cost;
                }
                return;
            }

            SetText(solverDetailStatusText, isMachineLearning ? active ? $"Active | Lv {progression.MachineLearningLevel}" : $"Unlocked | Lv {progression.MachineLearningLevel}" : active ? "Active" : "Unlocked");
            SetButtonText(solverDetailActionButton, active ? "Selected" : "Select");
            if (solverDetailActionButton != null)
            {
                solverDetailActionButton.interactable = !active;
            }
        }

        private void RefreshSolverTunePanel()
        {
            if (progression == null)
            {
                return;
            }

            SolverDefinition solverDefinition = StackMergeSolverCatalog.GetDefinition(selectedSolverId);
            SolverTuningDefinition tuningDefinition = StackMergeSolverCatalog.GetTuningDefinition(selectedSolverId);
            SolverTuningSettings tuning = progression.GetSolverTuning(selectedSolverId);

            SetText(solverTuneTitleText, $"{solverDefinition.DisplayName} tuning");
            SetText(solverTuneSummaryText, tuningDefinition.Summary);

            for (int i = 0; i < solverTuneRows.Length; i++)
            {
                bool visible = i < tuningDefinition.Parameters.Length;
                SetActive(solverTuneRows[i], visible);
                if (!visible)
                {
                    continue;
                }

                SolverTuningParameterDefinition parameter = tuningDefinition.Parameters[i];
                int value = tuning.GetSlotValue(i);

                if (i < solverTuneNameTexts.Length)
                {
                    SetText(solverTuneNameTexts[i], parameter.DisplayName);
                }

                if (i < solverTuneDescriptionTexts.Length)
                {
                    SetText(solverTuneDescriptionTexts[i], parameter.Description);
                }

                if (i < solverTuneValueTexts.Length)
                {
                    SetText(solverTuneValueTexts[i], parameter.FormatValue(value));
                }

                if (i < solverTuneSliders.Length && solverTuneSliders[i] != null)
                {
                    solverTuneSliders[i].minValue = parameter.MinDisplayValue;
                    solverTuneSliders[i].maxValue = parameter.MaxDisplayValue;
                    solverTuneSliders[i].wholeNumbers = parameter.WholeNumbers;
                    solverTuneSliders[i].SetValueWithoutNotify(parameter.ToDisplayValue(value));
                }
            }

            if (solverTuneResetButton != null)
            {
                solverTuneResetButton.interactable = !tuning.IsNeutral;
            }
        }

        private void RefreshGameplayInfo()
        {
            if (!gameplayInfoOpen || progression == null)
            {
                return;
            }

            SolverId solverId = progression.SelectedSolver;
            SolverDefinition solverDefinition = StackMergeSolverCatalog.GetDefinition(solverId);
            SolverTuningDefinition tuningDefinition = StackMergeSolverCatalog.GetTuningDefinition(solverId);
            SolverTuningSettings tuning = progression.GetSolverTuning(solverId);

            var builder = new StringBuilder();
            builder.AppendLine($"Stack cap: {progression.StackCapacity}/{StackMergeGameState.MaxStackCapacity}");
            builder.AppendLine($"Risk: L{progression.DifficultyLevel}");
            builder.AppendLine($"Speed: L{progression.SpeedLevel} ({progression.MoveInterval:0.00}s)");
            builder.AppendLine($"Auto solving: {(progression.AutoSolveEnabled ? "ON" : "OFF")}");
            builder.AppendLine($"Auto restart: {(progression.AutoRestartEnabled ? progression.AutoRestartIsTokenFree ? "ON (free)" : $"ON ({progression.Tokens} tokens)" : "OFF")}");
            builder.AppendLine($"Solver: {solverDefinition.DisplayName}");
            if (solverId == SolverId.MachineLearning)
            {
                builder.AppendLine(progression.GetMachineLearningStatus());
            }

            if (progression.NeuralAcceleratorActive)
            {
                builder.AppendLine("Neural Accelerator: MOCA/MOCA+/MCTS speed boost active");
            }

            if (gameState != null)
            {
                builder.AppendLine($"Run modifiers: Unstable {gameState.UnstableSavesRemaining}, Pickaxe {gameState.PickaxeUsesRemaining}, Queue skip {gameState.QueueSkipsRemaining}");
                builder.AppendLine($"Special blocks: {(gameState.JokerBlocksEnabled ? "Joker ON" : "Joker OFF")} | Mirror: {(gameState.MirrorStackEnabled ? "ON" : "OFF")}");
            }
            builder.AppendLine();
            builder.AppendLine("Solver tuning");

            if (!progression.SolverTuningUnlocked)
            {
                builder.AppendLine("Locked in Upgrades.");
            }
            else if (!tuningDefinition.HasParameters)
            {
                builder.AppendLine("No tuning available for this solver.");
            }
            else
            {
                for (int i = 0; i < tuningDefinition.Parameters.Length; i++)
                {
                    SolverTuningParameterDefinition parameter = tuningDefinition.Parameters[i];
                    builder.AppendLine($"{parameter.DisplayName}: {parameter.FormatValue(tuning.GetSlotValue(i))}");
                }
            }

            SetText(gameplayInfoText, builder.ToString());
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
                if (!progression.AgentsMenuUnlocked)
                {
                    SetButtonText(button, $"{definition.DisplayName}\nLocked");
                    button.interactable = false;
                    SetButtonColor(button, HexColor("#334155"));
                    continue;
                }

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

                if (!progression.AgentsMenuUnlocked)
                {
                    SetText(text, i == 0 ? "Agents\nLocked" : "Unlock in\nUpgrades");
                    text.color = HexColor("#64748B");
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
                    SetText(text, "Bonus slot\nNeeds upgrade");
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
            if (!progression.AgentsMenuUnlocked)
            {
                SetText(agentDetailNameText, "Agents Locked");
                SetText(agentDetailInfoText, $"Unlock the Agents menu in Upgrades.\nCost: {FormatNumber(progression.GetAgentsMenuUnlockCost())} chips");
                SetText(agentDetailStatusText, "Locked");
                SetButtonText(agentDetailActionButton, "Unlock in\nUpgrades");
                if (agentDetailActionButton != null)
                {
                    agentDetailActionButton.interactable = false;
                }

                return;
            }

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

        private void RefreshModifierButtons()
        {
            for (int i = 0; i < modifierButtons.Length && i < StackMergeProgression.Modifiers.Length; i++)
            {
                Button button = modifierButtons[i];
                if (button == null)
                {
                    continue;
                }

                ModifierDefinition definition = StackMergeProgression.Modifiers[i];
                if (!progression.ModifiersMenuUnlocked)
                {
                    SetButtonText(button, $"{definition.DisplayName}\nLocked");
                    button.interactable = false;
                    SetButtonColor(button, HexColor("#334155"));
                    continue;
                }

                int level = progression.GetModifierLevel(definition.Id);
                bool selected = selectedModifierId == definition.Id;
                bool maxed = progression.IsModifierMaxed(definition.Id);

                string label = selected ? $"> {definition.DisplayName}" : definition.DisplayName;
                label += maxed ? $"\nL{level} max" : level > 0 ? $"\nL{level}" : "\nAvailable";

                SetButtonText(button, label);
                button.interactable = true;
                SetButtonColor(button, selected ? HexColor("#B45309") : level > 0 ? HexColor("#92400E") : HexColor("#7C2D12"));
            }
        }

        private void RefreshModifierDetails()
        {
            ModifierDefinition definition = progression.GetModifierDefinition(selectedModifierId);
            if (!progression.ModifiersMenuUnlocked)
            {
                SetText(modifierSummaryText, progression.GetModifiersGateStatus());
                SetText(modifierDetailNameText, "Modifier Lab Locked");
                SetText(modifierDetailInfoText, "Reach the stage goal in Upgrades to unlock board modifiers. They make runs richer, riskier, and much harder to play optimally by hand.");
                SetText(modifierDetailStatusText, "Locked");
                SetButtonText(modifierDetailActionButton, "Unlock in\nUpgrades");
                if (modifierDetailActionButton != null)
                {
                    modifierDetailActionButton.interactable = false;
                }

                return;
            }

            int unlockedCount = 0;
            int totalLevels = 0;
            for (int i = 0; i < StackMergeProgression.Modifiers.Length; i++)
            {
                int level = progression.GetModifierLevel(StackMergeProgression.Modifiers[i].Id);
                if (level > 0)
                {
                    unlockedCount++;
                    totalLevels += level;
                }
            }

            SetText(modifierSummaryText, $"Modifier Lab online. Active families: {unlockedCount}/{StackMergeProgression.Modifiers.Length} | Total levels: {totalLevels}\nModifiers apply to new runs and amplify solver differences.");

            int selectedLevel = progression.GetModifierLevel(selectedModifierId);
            bool maxed = progression.IsModifierMaxed(selectedModifierId);
            SetText(modifierDetailNameText, definition.DisplayName);
            SetText(modifierDetailInfoText, selectedLevel > 0 ? definition.Description : $"{definition.LockedHint}\n\n{definition.Description}");
            SetText(modifierDetailStatusText, maxed ? $"Level {selectedLevel} max" : selectedLevel > 0 ? $"Level {selectedLevel}" : "Not active");

            if (maxed)
            {
                SetButtonText(modifierDetailActionButton, "Maxed");
                if (modifierDetailActionButton != null)
                {
                    modifierDetailActionButton.interactable = false;
                }

                return;
            }

            long cost = progression.GetModifierUpgradeCost(selectedModifierId);
            SetButtonText(modifierDetailActionButton, selectedLevel > 0 ? $"Upgrade\n{FormatNumber(cost)}" : $"Activate\n{FormatNumber(cost)}");
            if (modifierDetailActionButton != null)
            {
                modifierDetailActionButton.interactable = progression.Chips >= cost;
            }
        }

        private void RefreshUpgradeButtons()
        {
            if (progressionStageText != null)
            {
                string stageName = !progression.AgentsMenuUnlocked ? "Stage 1 - Core automation"
                    : !progression.ModifiersMenuUnlocked ? "Stage 2 - Agent acceleration"
                    : !progression.AllModifiersMaxed ? "Stage 3 - Modifier Lab"
                    : !progression.IsSolverUnlocked(SolverId.MachineLearning) ? "Stage 4 - Machine Learning"
                    : "Endgame - DQN training";
                string nextGoal = progression.IsSolverUnlocked(SolverId.MachineLearning)
                    ? progression.GetMachineLearningStatus()
                    : progression.ModifiersMenuUnlocked
                    ? progression.AllModifiersMaxed ? "DQN is ready to unlock in Algorithms." : "Max every Modifier to open the Machine Learning layer."
                    : progression.GetModifiersGateStatus();
                SetText(progressionStageText, $"{stageName}\n{nextGoal}");
            }

            if (modifiersMenuUnlockButton != null)
            {
                if (progression.ModifiersMenuUnlocked)
                {
                    SetButtonText(modifiersMenuUnlockButton, "Modifier Lab\nUnlocked");
                    modifiersMenuUnlockButton.interactable = false;
                    SetButtonColor(modifiersMenuUnlockButton, HexColor("#0F766E"));
                }
                else
                {
                    long cost = progression.GetModifiersMenuUnlockCost();
                    bool gateReady = progression.CanUnlockModifiersMenu;
                    SetButtonText(modifiersMenuUnlockButton, gateReady ? $"Modifier Lab\n{FormatNumber(cost)}" : "Modifier Lab\nStage locked");
                    modifiersMenuUnlockButton.interactable = gateReady && progression.Chips >= cost;
                    SetButtonColor(modifiersMenuUnlockButton, gateReady ? HexColor("#B45309") : HexColor("#334155"));
                }
            }

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

            if (autoSolveButton != null)
            {
                if (progression.AutoSolveUnlocked)
                {
                    SetButtonText(autoSolveButton, progression.AutoSolveEnabled ? "Auto solve\nON" : "Auto solve\nOFF");
                    autoSolveButton.interactable = true;
                    SetButtonColor(autoSolveButton, progression.AutoSolveEnabled ? HexColor("#0F766E") : HexColor("#334155"));
                }
                else
                {
                    long cost = progression.GetAutoSolveCost();
                    SetButtonText(autoSolveButton, progression.HasPurchasedSolver ? $"Auto solve\n{FormatNumber(cost)}" : "Auto solve\nNeeds algorithm");
                    autoSolveButton.interactable = progression.HasPurchasedSolver && progression.Chips >= cost;
                    SetButtonColor(autoSolveButton, HexColor("#0F766E"));
                }
            }

            if (autoRestartButton != null)
            {
                if (progression.AutoRestartUnlocked)
                {
                    string tokenMode = progression.AutoRestartIsTokenFree ? "free" : $"{FormatNumber(progression.Tokens)} token";
                    SetButtonText(autoRestartButton, progression.AutoRestartEnabled ? $"Auto restart\nON ({tokenMode})" : "Auto restart\nOFF");
                    autoRestartButton.interactable = true;
                    SetButtonColor(autoRestartButton, progression.AutoRestartEnabled ? HexColor("#C2410C") : HexColor("#334155"));
                }
                else
                {
                    long cost = progression.GetAutoRestartCost();
                    SetButtonText(autoRestartButton, progression.HasPurchasedSolver ? $"Auto restart\n{FormatNumber(cost)}" : "Auto restart\nNeeds algorithm");
                    autoRestartButton.interactable = progression.HasPurchasedSolver && progression.Chips >= cost;
                    SetButtonColor(autoRestartButton, HexColor("#C2410C"));
                }
            }

            if (tokenPackButton != null)
            {
                long cost = progression.GetTokenPackCost();
                SetButtonText(tokenPackButton, $"+{progression.GetTokenPackSize()} tokens\n{FormatNumber(cost)} chips");
                tokenPackButton.interactable = progression.Chips >= cost;
                SetButtonColor(tokenPackButton, HexColor("#0369A1"));
            }

            if (solverTuningUnlockButton != null)
            {
                if (progression.SolverTuningUnlocked)
                {
                    SetButtonText(solverTuningUnlockButton, "Solver tuning\nUnlocked");
                    solverTuningUnlockButton.interactable = false;
                    SetButtonColor(solverTuningUnlockButton, HexColor("#0F766E"));
                }
                else
                {
                    long cost = progression.GetSolverTuningUnlockCost();
                    SetButtonText(solverTuningUnlockButton, $"Solver tuning\n{FormatNumber(cost)}");
                    solverTuningUnlockButton.interactable = progression.Chips >= cost;
                    SetButtonColor(solverTuningUnlockButton, HexColor("#2563EB"));
                }
            }

            if (extraAgentSlotUpgradeButton != null)
            {
                if (progression.ExtraAgentSlotUnlocked)
                {
                    SetButtonText(extraAgentSlotUpgradeButton, "+1 Agent slot\nUnlocked");
                    extraAgentSlotUpgradeButton.interactable = false;
                    SetButtonColor(extraAgentSlotUpgradeButton, HexColor("#0F766E"));
                }
                else
                {
                    long cost = progression.GetExtraAgentSlotUpgradeCost();
                    SetButtonText(extraAgentSlotUpgradeButton, $"+1 Agent slot\n{FormatNumber(cost)}");
                    extraAgentSlotUpgradeButton.interactable = progression.Chips >= cost;
                    SetButtonColor(extraAgentSlotUpgradeButton, HexColor("#7C3AED"));
                }
            }

            if (agentsMenuUnlockButton != null)
            {
                if (progression.AgentsMenuUnlocked)
                {
                    SetButtonText(agentsMenuUnlockButton, "Agents\nUnlocked");
                    agentsMenuUnlockButton.interactable = false;
                    SetButtonColor(agentsMenuUnlockButton, HexColor("#0F766E"));
                }
                else
                {
                    long cost = progression.GetAgentsMenuUnlockCost();
                    SetButtonText(agentsMenuUnlockButton, $"Unlock Agents\n{FormatNumber(cost)}");
                    agentsMenuUnlockButton.interactable = progression.Chips >= cost;
                    SetButtonColor(agentsMenuUnlockButton, HexColor("#9333EA"));
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

            for (int i = 0; i < difficultyUpgradeButtons.Length; i++)
            {
                Button button = difficultyUpgradeButtons[i];
                if (button == null)
                {
                    continue;
                }

                if (i < progression.DifficultyLevel)
                {
                    SetButtonText(button, $"Risk {i + 1}\nDone");
                    button.interactable = false;
                    SetButtonColor(button, HexColor("#0F766E"));
                }
                else if (i == progression.DifficultyLevel && !progression.IsMaxDifficulty)
                {
                    long cost = progression.GetDifficultyUpgradeCost(i);
                    SetButtonText(button, $"Risk {i + 1}\n{FormatNumber(cost)}");
                    button.interactable = progression.Chips >= cost;
                    SetButtonColor(button, HexColor("#DB2777"));
                }
                else
                {
                    SetButtonText(button, $"Risk {i + 1}\nLocked");
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

        private void RefreshHistory()
        {
            if (progression == null)
            {
                return;
            }

            RunHistoryEntry[] history = progression.RunHistory;
            if (history.Length == 0)
            {
                SetText(historySummaryText, "No completed runs yet. Let a run end to start collecting solver stats.");
                SetText(historyInsightText, "Tip: use the editor benchmark window for large balance samples without touching player progression.");
                RefreshHistoryChart(Array.Empty<HistorySolverStats>());
                CreateTable(historySolverTableRoot, new[] { "Solver", "Runs", "Median", "Avg", "Min", "Max", "High" }, Array.Empty<string[]>(), "No solver data yet.");
                CreateTable(historyRecentRunsRoot, new[] { "Run", "Solver", "Score", "Moves", "Merges", "High", "Risk" }, Array.Empty<string[]>(), "Recent runs will appear here.");

                return;
            }

            RunHistoryEntry latest = history[0];
            RunHistoryEntry best = history.OrderByDescending(entry => entry.score).First();
            HistorySolverStats[] solverStats = BuildHistorySolverStats(history);
            HistorySolverStats bestMedian = solverStats.OrderByDescending(stats => stats.MedianScore).First();
            HistorySolverStats bestPeak = solverStats.OrderByDescending(stats => stats.MaxScore).First();
            HistorySolverStats mostStable = solverStats
                .Where(stats => stats.Runs >= 2)
                .OrderBy(stats => stats.ScoreRange)
                .ThenByDescending(stats => stats.MedianScore)
                .DefaultIfEmpty(bestMedian)
                .First();

            SetText(
                historySummaryText,
                $"Stored runs: {history.Length}/{250} | Latest: {FormatNumber(latest.score)} ({SolverName(latest.solverId)}) | Best run: {FormatNumber(best.score)} ({SolverName(best.solverId)})");

            SetText(
                historyInsightText,
                $"Best median: {bestMedian.SolverName} ({FormatNumber(bestMedian.MedianScore)}) | Best peak: {bestPeak.SolverName} ({FormatNumber(bestPeak.MaxScore)}) | Most stable: {mostStable.SolverName} ({FormatNumber(mostStable.MinScore)}-{FormatNumber(mostStable.MaxScore)})");

            RefreshHistoryChart(solverStats);
            RefreshHistoryTables(solverStats, history);
        }

        private void RefreshAchievements()
        {
            if (progression == null)
            {
                return;
            }

            int completed = StackMergeProgression.Achievements.Count(progression.IsAchievementComplete);
            SetText(
                achievementStatsText,
                $"Completed goals: {completed}/{StackMergeProgression.Achievements.Length} | Runs: {FormatNumber(progression.RunsCompleted)} ({FormatNumber(progression.ManualRunsCompleted)} manual) | Merges: {FormatNumber(progression.TotalMerges)} | Highest: {FormatNumber(progression.HighestBlockEver)} | Earned: {FormatNumber(progression.TotalChipsEarned)} | Spent: {FormatNumber(progression.TotalChipsSpent)} | Best run: {FormatNumber(progression.BestRunScore)}");

            string[][] rows = StackMergeProgression.Achievements
                .Select(achievement =>
                {
                    long progress = progression.GetAchievementProgress(achievement);
                    long cappedProgress = Math.Min(progress, achievement.Target);
                    bool complete = progression.IsAchievementComplete(achievement);
                    float percent = achievement.Target <= 0 ? 1f : Mathf.Clamp01(progress / (float)achievement.Target);
                    return new[]
                    {
                        achievement.Description,
                        $"{FormatNumber(cappedProgress)} / {FormatNumber(achievement.Target)}",
                        complete ? "Complete" : $"{percent * 100f:0}%"
                    };
                })
                .ToArray();

            CreateTable(
                achievementListRoot,
                new[] { "Goal", "Progress", "Status" },
                rows,
                "No achievements configured.");
        }

        private void RefreshHistoryTables(HistorySolverStats[] solverStats, RunHistoryEntry[] history)
        {
            string[][] solverRows = solverStats
                .OrderByDescending(stats => stats.MedianScore)
                .Select(stats => new[]
                {
                    stats.SolverName,
                    stats.Runs.ToString(),
                    FormatNumber(stats.MedianScore),
                    FormatNumber(stats.AverageScore),
                    FormatNumber(stats.MinScore),
                    FormatNumber(stats.MaxScore),
                    FormatNumber(stats.BestHighestMerged)
                })
                .ToArray();

            CreateTable(
                historySolverTableRoot,
                new[] { "Solver", "Runs", "Median", "Avg", "Min", "Max", "High" },
                solverRows,
                "No solver data yet.");

            string[][] recentRows = history
                .Take(12)
                .Select(entry => new[]
                {
                    $"#{entry.runIndex}",
                    SolverName(entry.solverId),
                    FormatNumber(entry.score),
                    entry.moves.ToString(),
                    entry.merges.ToString(),
                    FormatNumber(entry.highestMergedBlock),
                    $"L{entry.difficultyLevel}"
                })
                .ToArray();

            CreateTable(
                historyRecentRunsRoot,
                new[] { "Run", "Solver", "Score", "Moves", "Merges", "High", "Risk" },
                recentRows,
                "Recent runs will appear here.");
        }

        private static HistorySolverStats[] BuildHistorySolverStats(RunHistoryEntry[] history)
        {
            return history
                .GroupBy(entry => entry.solverId)
                .Select(group =>
                {
                    long[] scores = group.Select(entry => entry.score).OrderBy(value => value).ToArray();
                    int[] highest = group.Select(entry => entry.highestMergedBlock).OrderBy(value => value).ToArray();
                    return new HistorySolverStats(
                        group.Key,
                        group.Count(),
                        scores.First(),
                        scores.Last(),
                        Median(scores),
                        scores.Average(),
                        highest.Length > 0 ? highest.Last() : 0);
                })
                .ToArray();
        }

        private void RefreshHistoryChart(HistorySolverStats[] solverStats)
        {
            if (historyChartRoot == null)
            {
                return;
            }

            ClearChildren(historyChartRoot);
            HistorySolverStats[] entries = solverStats
                .OrderByDescending(stats => stats.MedianScore)
                .Take(8)
                .ToArray();
            if (entries.Length == 0)
            {
                TMP_Text empty = CreateRuntimeText("Empty Chart", historyChartRoot, "No median data yet", 22, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#64748B"));
                Stretch(empty.rectTransform, 0f, 0f, 0f, 0f);
                return;
            }

            float chartWidth = Mathf.Max(480f, historyChartRoot.rect.width);
            float chartHeight = Mathf.Max(180f, historyChartRoot.rect.height);
            float gap = 8f;
            float bottomLabelHeight = 42f;
            float barWidth = Mathf.Max(22f, (chartWidth - gap * (entries.Length - 1)) / entries.Length);
            long maxScore = Math.Max(1, entries.Max(entry => entry.MedianScore));

            for (int i = 0; i < entries.Length; i++)
            {
                HistorySolverStats entry = entries[i];
                float normalized = Mathf.Clamp01(entry.MedianScore / (float)maxScore);
                float barHeight = Mathf.Lerp(16f, chartHeight - bottomLabelHeight - 14f, normalized);
                float x = i * (barWidth + gap);

                RectTransform bar = CreateRuntimePanel($"{entry.SolverName} Median Bar", historyChartRoot, HexColor("#2563EB"));
                bar.anchorMin = new Vector2(0f, 0f);
                bar.anchorMax = new Vector2(0f, 0f);
                bar.pivot = new Vector2(0f, 0f);
                bar.anchoredPosition = new Vector2(x, bottomLabelHeight);
                bar.sizeDelta = new Vector2(barWidth, barHeight);
                Image barImage = bar.GetComponent<Image>();
                if (barImage != null)
                {
                    barImage.color = Color.Lerp(HexColor("#0F766E"), HexColor("#2563EB"), i / Mathf.Max(1f, entries.Length - 1f));
                }

                TMP_Text valueLabel = CreateRuntimeText(
                    $"{entry.SolverName} Median Label",
                    historyChartRoot,
                    $"{entry.SolverName}\n{FormatNumber(entry.MedianScore)}",
                    14,
                    FontStyles.Bold,
                    TextAlignmentOptions.Center,
                    HexColor("#CBD5E1"));
                valueLabel.enableAutoSizing = true;
                valueLabel.fontSizeMin = 9;
                valueLabel.fontSizeMax = 14;
                RectTransform labelRect = valueLabel.rectTransform;
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(0f, 0f);
                labelRect.pivot = new Vector2(0f, 0f);
                labelRect.anchoredPosition = new Vector2(x, 0f);
                labelRect.sizeDelta = new Vector2(barWidth, bottomLabelHeight - 4f);
            }
        }

        private static void CreateTable(RectTransform root, string[] headers, string[][] rows, string emptyText)
        {
            if (root == null)
            {
                return;
            }

            ClearChildren(root);
            float tableWidth = Mathf.Max(620f, root.rect.width);
            float rowHeight = 34f;
            float y = 0f;
            float[] widths = BuildEqualWidths(headers.Length, tableWidth);
            CreateTableRow(root, headers, widths, y, rowHeight, true);
            y += rowHeight + 4f;

            if (rows.Length == 0)
            {
                TMP_Text empty = CreateRuntimeText("Empty Table", root, emptyText, 18, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#64748B"));
                RectTransform emptyRect = empty.rectTransform;
                emptyRect.anchorMin = new Vector2(0f, 1f);
                emptyRect.anchorMax = new Vector2(0f, 1f);
                emptyRect.pivot = new Vector2(0f, 1f);
                emptyRect.anchoredPosition = new Vector2(0f, -y - 18f);
                emptyRect.sizeDelta = new Vector2(tableWidth, 44f);
                return;
            }

            for (int i = 0; i < rows.Length; i++)
            {
                CreateTableRow(root, rows[i], widths, y, rowHeight, false, i);
                y += rowHeight + 3f;
            }
        }

        private static void CreateTableRow(RectTransform root, string[] cells, float[] widths, float y, float height, bool header, int rowIndex = 0)
        {
            RectTransform row = CreateRuntimePanel($"Table Row {rowIndex}", root, header ? HexColor("#0F172A") : rowIndex % 2 == 0 ? HexColor("#172033") : HexColor("#111827", 0.86f));
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(0f, 1f);
            row.pivot = new Vector2(0f, 1f);
            row.anchoredPosition = new Vector2(0f, -y);
            row.sizeDelta = new Vector2(widths.Sum(), height);

            float x = 0f;
            for (int i = 0; i < cells.Length && i < widths.Length; i++)
            {
                TMP_Text text = CreateRuntimeText($"Cell {i}", row, cells[i], header ? 15 : 14, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, header ? HexColor("#FDE68A") : HexColor("#E5E7EB"));
                text.enableAutoSizing = true;
                text.fontSizeMin = 9;
                text.fontSizeMax = header ? 15 : 14;
                RectTransform rect = text.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.offsetMin = new Vector2(x + 8f, 0f);
                rect.offsetMax = new Vector2(x + widths[i] - 6f, 0f);
                x += widths[i];
            }
        }

        private static float[] BuildEqualWidths(int columnCount, float tableWidth)
        {
            float[] widths = new float[columnCount];
            float width = tableWidth / Math.Max(1, columnCount);
            for (int i = 0; i < widths.Length; i++)
            {
                widths[i] = width;
            }

            return widths;
        }

        private static long Median(long[] orderedValues)
        {
            if (orderedValues.Length == 0)
            {
                return 0;
            }

            int middle = orderedValues.Length / 2;
            if (orderedValues.Length % 2 == 1)
            {
                return orderedValues[middle];
            }

            return (long)Math.Round((orderedValues[middle - 1] + orderedValues[middle]) / 2.0);
        }

        private static string SolverName(int solverId)
        {
            return StackMergeSolverCatalog.GetDefinition((SolverId)solverId).DisplayName;
        }

        private readonly struct HistorySolverStats
        {
            public HistorySolverStats(int solverId, int runs, long minScore, long maxScore, long medianScore, double averageScore, int bestHighestMerged)
            {
                SolverId = solverId;
                Runs = runs;
                MinScore = minScore;
                MaxScore = maxScore;
                MedianScore = medianScore;
                AverageScore = averageScore;
                BestHighestMerged = bestHighestMerged;
            }

            public int SolverId { get; }

            public string SolverName => StackMergeGameBootstrap.SolverName(SolverId);

            public int Runs { get; }

            public long MinScore { get; }

            public long MaxScore { get; }

            public long MedianScore { get; }

            public double AverageScore { get; }

            public int BestHighestMerged { get; }

            public long ScoreRange => MaxScore - MinScore;
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

            ResizeBoardToCapacity();
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

        private void ResizeBoardToCapacity()
        {
            if (gameState == null)
            {
                return;
            }

            RectTransform board = boardRoot;
            if (board == null && stackBlockLayers.Length > 0 && stackBlockLayers[0] != null)
            {
                board = stackBlockLayers[0].parent.parent as RectTransform;
            }

            if (board == null)
            {
                return;
            }

            const float top = 360f;
            float height = CalculateBoardHeight(gameState.StackCapacity);
            board.anchorMin = new Vector2(0f, 1f);
            board.anchorMax = new Vector2(1f, 1f);
            board.pivot = new Vector2(0.5f, 1f);
            board.offsetMin = new Vector2(0f, -top - height);
            board.offsetMax = new Vector2(0f, -top);
        }

        private void RefreshGameOver()
        {
            if (gameOverOverlay == null)
            {
                return;
            }

            gameOverOverlay.SetActive(gameState.IsGameOver && selectedTabIndex == 0 && !historyOpen && !achievementsOpen);
            if (!gameState.IsGameOver)
            {
                return;
            }

            SetText(gameOverScoreText, $"Pont: {FormatNumber(gameState.Score)}");
            SetText(gameOverBestText, $"Rekord: {FormatNumber(highScore)}");
            SetText(runStatusText, progression != null && progression.AutoRestartUnlocked && progression.AutoRestartEnabled
                ? progression.AutoRestartIsTokenFree || progression.Tokens > 0 ? "Auto restart armed" : "Auto restart needs token"
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
                text.text = value == StackMergeGameState.JokerBlockValue ? "J" : FormatNumber(value);
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

        private static RectTransform CreateRuntimePanel(string name, Transform parent, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);
            gameObject.GetComponent<Image>().color = color;
            return gameObject.GetComponent<RectTransform>();
        }

        private static TMP_Text CreateRuntimeText(
            string name,
            Transform parent,
            string textValue,
            int fontSize,
            FontStyles style,
            TextAlignmentOptions alignment,
            Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            gameObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = gameObject.GetComponent<TextMeshProUGUI>();
            text.text = textValue;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            return text;
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

        private static float CalculateBoardHeight(int stackCapacity)
        {
            const float blockHeight = 74f;
            const float spacing = 7f;
            const float internalPadding = 44f;
            int capacity = Mathf.Max(1, stackCapacity);
            return internalPadding + capacity * blockHeight + Mathf.Max(0, capacity - 1) * spacing;
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
                StackMergeGameState.JokerBlockValue => HexColor("#F8FAFC"),
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

        private static string FormatNumber(double value)
        {
            return FormatNumber((long)Math.Round(value));
        }

        private static string FormatSigned(int value)
        {
            return value > 0 ? $"+{value}" : value.ToString();
        }
    }
}
