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
        [SerializeField] private GameObject researchPanel;
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
        [SerializeField] private TMP_Text prestigeSummaryText;
        [SerializeField] private Button prestigeButton;
        [SerializeField] private Button[] researchButtons = Array.Empty<Button>();
        [SerializeField] private Image[] researchConnectorImages = Array.Empty<Image>();
        [SerializeField] private TMP_Text researchDetailNameText;
        [SerializeField] private TMP_Text researchDetailInfoText;
        [SerializeField] private TMP_Text researchDetailStatusText;
        [SerializeField] private Button researchDetailActionButton;
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
        private float saveFlushTimer;
        private const float SaveFlushInterval = 4f;
        private float trainingEvalTimer;
        private const float TrainingEvalDuration = 2.5f;
        private RectTransform trainingOverlay;
        private TMP_Text trainingOverlayText;
        private RectTransform ppoModeModal;
        private Button ppoTrainingButton;
        private Button ppoNormalButton;
        private TMP_Text ppoModeHintText;
        private RectTransform[] solverTuneSegmentContainers;
        private RectTransform solverInfoModal;
        private TMP_Text solverInfoTitle;
        private TMP_Text solverInfoStatsText;
        private TMP_Text solverInfoTuningText;
        private RectTransform solverInfoChartRoot;
        private int lastRenderedCapacity = -1;
        private bool boardLayoutDirty = true;
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
        private ResearchId selectedResearchId = ResearchId.InsightAmplifier;
        private bool solverDeselected = false;
        private int blockDropStack = -1;
        private float blockDropTimer = 0f;
        private const float BlockDropDuration = 0.14f;
        private float mergePulseTimer = 0f;
        private const float MergePulseDuration = 0.22f;
        private int mergePulseStack = -1;
        private RectTransform[] stackSlotLayers;

        private static readonly (ResearchId From, ResearchId To)[] ResearchConnections =
        {
            (ResearchId.InsightAmplifier, ResearchId.SeedCapital),
            (ResearchId.InsightAmplifier, ResearchId.PpoBootcamp),
            (ResearchId.InsightAmplifier, ResearchId.PassiveInsight),
            (ResearchId.SeedCapital, ResearchId.AutomationMemory),
            (ResearchId.AutomationMemory, ResearchId.AlgorithmArchive),
            (ResearchId.AlgorithmArchive, ResearchId.YieldTheory),
            (ResearchId.PpoBootcamp, ResearchId.PpoMemory),
            (ResearchId.PpoMemory, ResearchId.PpoHighFocus),
            (ResearchId.PpoHighFocus, ResearchId.PpoStability),
            (ResearchId.PpoStability, ResearchId.InsightExtractor),
            (ResearchId.PassiveInsight, ResearchId.InsightExtractor),
            (ResearchId.PassiveInsight, ResearchId.OfflineEfficiency),
            (ResearchId.OfflineEfficiency, ResearchId.OfflineTime)
        };

        private static readonly Vector2 ResearchNodeSize = new(198f, 68f);
        private const float ResearchNodeLeft = 22f;
        private const float ResearchNodeTop = 18f;
        private const float ResearchNodeColumnSpacing = 278f;
        private const float ResearchNodeTierSpacing = 88f;

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
            ApplyModernTheme();
            EnsureResearchUi();
            WirePrestigeResearchButtons();
            CreateFreshGame();
            RefreshEverything();
            if (progression.LastOfflineChips > 0 || progression.LastOfflineInsight > 0)
            {
                SetText(feedbackText, $"Offline gain: +{FormatNumber(progression.LastOfflineChips)} chips, +{FormatNumber(progression.LastOfflineInsight)} Insight");
            }
        }

        /// <summary>
        /// Runtime visual polish that only rounds the corners of buttons and card panels.
        /// It NEVER sets a colour — every colour comes from the Image / Button components you
        /// edit in the Inspector. Images that already have a sprite assigned are left untouched,
        /// so any custom sprite you set in the Hierarchy is preserved.
        /// </summary>
        private void ApplyModernTheme()
        {
            if (canvas == null)
            {
                return;
            }

            Sprite roundedButton = GetRoundedSprite(Color.white, Color.white, 22);
            Sprite roundedCard = GetRoundedSprite(Color.white, Color.white, 20);

            foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            {
                Image image = button.image != null ? button.image : button.GetComponent<Image>();
                ApplyRoundedSprite(image, roundedButton);
            }

            // Round the corners of category cards (colour is left exactly as set in the Inspector).
            foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.EndsWith(" Category"))
                {
                    ApplyRoundedSprite(t.GetComponent<Image>(), roundedCard);
                }
            }

            ApplyRoundedSprite(GetImage(nextBlocksRoot), roundedCard);
            foreach (RectTransform layer in stackBlockLayers)
            {
                if (layer != null)
                {
                    ApplyRoundedSprite(GetImage(layer.parent as RectTransform), roundedCard);
                }
            }
        }

        private static Image GetImage(RectTransform rect)
        {
            return rect != null ? rect.GetComponent<Image>() : null;
        }

        private static void ApplyRoundedSprite(Image image, Sprite rounded)
        {
            // Only restyle flat solid-colour fills; never override something that already draws a
            // sprite (e.g. the generated tile blocks).
            if (image == null || image.sprite != null)
            {
                return;
            }

            image.sprite = rounded;
            image.type = Image.Type.Sliced;
        }

        private void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                boardLayoutDirty = true;
                RefreshColumns();
            }

            TickAutomation();
            TickBlockAnimations();
            if (gameState != null && !gameState.IsGameOver)
            {
                currentRunElapsed += Time.deltaTime;
            }

            // Persistence is throttled: the per-move path only marks the progression
            // dirty, and we flush it to disk at most a few times per second-of-play here.
            if (progression != null && progression.HasUnsavedChanges)
            {
                saveFlushTimer += Time.deltaTime;
                if (saveFlushTimer >= SaveFlushInterval)
                {
                    saveFlushTimer = 0f;
                    progression.FlushIfDirty();
                }
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                progression?.FlushIfDirty();
            }
        }

        private void OnApplicationQuit()
        {
            progression?.FlushIfDirty();
        }

        private void OnDisable()
        {
            progression?.FlushIfDirty();
        }

        private void TickBlockAnimations()
        {
            if (blockDropTimer <= 0f && mergePulseTimer <= 0f)
            {
                return;
            }

            blockDropTimer = Mathf.Max(0f, blockDropTimer - Time.deltaTime);
            mergePulseTimer = Mathf.Max(0f, mergePulseTimer - Time.deltaTime);

            if (gameState != null && !gameState.IsGameOver && selectedTabIndex == 0 && !historyOpen && !achievementsOpen)
            {
                RefreshColumns();
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
            GameObject research,
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
            TMP_Text prestigeDetails,
            Button runPrestigeButton,
            Button[] researchUpgradeButtons,
            Image[] researchConnections,
            TMP_Text selectedResearchName,
            TMP_Text selectedResearchInfo,
            TMP_Text selectedResearchStatus,
            Button selectedResearchAction,
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
            researchPanel = research;
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
            prestigeSummaryText = prestigeDetails;
            prestigeButton = runPrestigeButton;
            researchButtons = researchUpgradeButtons;
            researchConnectorImages = researchConnections;
            researchDetailNameText = selectedResearchName;
            researchDetailInfoText = selectedResearchInfo;
            researchDetailStatusText = selectedResearchStatus;
            researchDetailActionButton = selectedResearchAction;
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

            WirePrestigeResearchButtons();

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

            if (researchDetailActionButton != null)
            {
                researchDetailActionButton.onClick.RemoveAllListeners();
                researchDetailActionButton.onClick.AddListener(BuySelectedResearchUpgrade);
            }
        }

        private void WirePrestigeResearchButtons()
        {
            if (prestigeButton != null)
            {
                prestigeButton.onClick.RemoveAllListeners();
                prestigeButton.onClick.AddListener(ExecutePrestige);
            }

            for (int i = 0; i < researchButtons.Length; i++)
            {
                int researchIndex = i;
                if (researchButtons[i] == null)
                {
                    continue;
                }

                researchButtons[i].onClick.RemoveAllListeners();
                researchButtons[i].onClick.AddListener(() => SelectResearchUpgrade((ResearchId)researchIndex));
            }

            if (researchDetailActionButton != null)
            {
                researchDetailActionButton.onClick.RemoveAllListeners();
                researchDetailActionButton.onClick.AddListener(BuySelectedResearchUpgrade);
            }
        }

        private void SelectTab(int tabIndex)
        {
            historyOpen = false;
            achievementsOpen = false;
            solverTuneOpen = false;
            gameplayInfoOpen = false;
            int requestedTab = Mathf.Clamp(tabIndex, 0, 6);
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

            if (requestedTab == 5 && progression != null && !IsResearchMenuUnlocked())
            {
                SetText(feedbackText, progression.IsSolverUnlocked(SolverId.MachineLearning) ? "Finish PPO Training first" : "Unlock PPO to open Research");
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
            SetActive(researchPanel, selectedTabIndex == 5);
            SetActive(settingsPanel, selectedTabIndex == 6);
            SetActive(solverTunePanel, false);
            SetActive(gameplayInfoOverlay, false);
            RefreshTabButtons();

            // Update the board / training-overlay visibility immediately on tab change so the
            // PPO matrix view appears/disappears at once instead of lagging until the next move.
            RefreshGameView();
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
            SetActive(researchPanel, false);
            SetActive(settingsPanel, false);
            SetActive(historyPanel, true);
            SetActive(solverTunePanel, false);
            SetActive(gameplayInfoOverlay, false);
            RefreshHistory();
            RefreshTabButtons();
            RefreshGameView();
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
            SetActive(researchPanel, false);
            SetActive(settingsPanel, false);
            SetActive(achievementsPanel, true);
            SetActive(solverTunePanel, false);
            SetActive(gameplayInfoOverlay, false);
            RefreshAchievements();
            RefreshTabButtons();
            RefreshGameView();
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

        private bool IsResearchMenuUnlocked()
        {
            return progression != null
                && (progression.PrestigeAvailable || progression.PrestigeCount > 0 || progression.ResearchInsight > 0);
        }

        private void OpenSolverTunePanel()
        {
            if (progression == null)
            {
                return;
            }

            SolverTuningDefinition tuningDefinition = StackMergeSolverCatalog.GetTuningDefinition(selectedSolverId);
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
            string[] labels = { "Jatek", "Algoritmus", "Upgrade", "Modifiers", "Agent", "Research", "Settings" };
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
                bool lockedResearchTab = i == 5 && progression != null && !IsResearchMenuUnlocked();
                bool locked = lockedModifierTab || lockedAgentTab || lockedResearchTab;

                if (label != null)
                {
                    label.text = lockedModifierTab ? "Modifiers\nLocked"
                        : lockedAgentTab ? "Agent\nLocked"
                        : lockedResearchTab ? "Research\nLocked"
                        : i < labels.Length ? labels[i] : label.text;
                    label.color = locked ? HexColor("#64748B") : selected ? HexColor("#FDE68A") : Color.white;
                }

                // Tab background colour is owned by the Button component (Color Tint) in the
                // Inspector; the code only marks locked tabs non-interactable so Unity shows
                // your disabled colour.
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
                // Training: a short artificial "result evaluation" pause between runs so the very
                // fast PPO advances at a watchable pace, then auto-restart for free (no tokens, no
                // game-over menu).
                if (progression.IsMachineLearningTrainingActive)
                {
                    trainingEvalTimer += Time.deltaTime;
                    float percent = Mathf.Clamp01(trainingEvalTimer / TrainingEvalDuration) * 100f;
                    string status = $"Result evaluation {percent:0}%";
                    SetText(runStatusText, status);
                    if (selectedTabIndex == 0 && !historyOpen && !achievementsOpen)
                    {
                        UpdateTrainingOverlay(status);
                    }

                    if (trainingEvalTimer >= TrainingEvalDuration)
                    {
                        trainingEvalTimer = 0f;
                        StartNewGame();
                    }

                    return;
                }

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
            trainingEvalTimer = 0f;
            // Training always auto-runs even if the player hasn't enabled auto-solve.
            if (!progression.AutoSolveEnabled && !progression.IsMachineLearningTrainingActive)
            {
                return;
            }

            if (solverDeselected && !progression.IsMachineLearningTrainingActive)
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
                    // Real-time play uses the lightweight solver path so the two compute-heavy
                    // solvers (MOCA+, MCTS) stay at a smooth frame rate. The benchmark window runs
                    // the full-strength path, so measured strength is unaffected.
                    lightweightMode: true,
                    tuning: progression.SolverTuningUnlocked
                        ? progression.GetSolverTuning(progression.SelectedSolver)
                        : SolverTuningSettings.Neutral(progression.SelectedSolver),
                    highTierSpeedTuningAccelerator: progression.NeuralAcceleratorActive,
                    machineLearningAgent: progression.MachineLearningAgent,
                    machineLearningTrainingMode: progression.IsMachineLearningTrainingActive));

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
            // Trigger block drop animation.
            blockDropStack = result.StackIndex;
            blockDropTimer = BlockDropDuration;
            if (result.MergeCount > 0)
            {
                mergePulseStack = result.StackIndex;
                mergePulseTimer = MergePulseDuration;
            }

            bool machineLearningTraining = progression.IsMachineLearningTrainingActive;
            long chipsGained = progression.AwardMove(result, machineLearningTraining);
            if (progression.SelectedSolver == SolverId.MachineLearning && autoSolverMove)
            {
                progression.ObserveMachineLearningMove(result, gameState, machineLearningTraining);
            }

            if (autoSolverMove)
            {
                currentRunUsedAutoSolve = true;
            }
            else
            {
                currentRunManualMoves++;
            }

            long runBonus = 0;
            if (!wasGameOver && result.IsGameOver)
            {
                bool manualRun = currentRunManualMoves > 0 && !currentRunUsedAutoSolve;
                SolverId histSolverId = manualRun ? (SolverId)(-1) : progression.SelectedSolver;
                runBonus = progression.AwardRunCompleted(
                    gameState.Score,
                    histSolverId,
                    gameState.BlocksDropped,
                    gameState.TotalMerges,
                    gameState.HighestMergedBlock,
                    manualRun,
                    currentRunElapsed,
                    machineLearningTraining);
                if (progression.SelectedSolver == SolverId.MachineLearning)
                {
                    progression.AwardMachineLearningRun(
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
            string learningText = progression.SelectedSolver == SolverId.MachineLearning
                ? $" | PPO U{progression.MachineLearningAgent.Metrics.Updates}"
                : string.Empty;
            SetText(feedbackText, $"{moveText} | {chipText}{learningText} | {resultReason}");

            // Per-move: only refresh the cheap game view. When a run actually ends, do a
            // single full refresh so the history / achievements / upgrade panels pick up the
            // run-completion rewards (this happens once per run, not once per move).
            if (!wasGameOver && result.IsGameOver)
            {
                RefreshEverything();
            }
            else
            {
                RefreshGameView();
            }
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

            // PPO: unlock if needed, then let the player pick Training / Normal mode in a popup.
            if (selectedSolverId == SolverId.MachineLearning)
            {
                if (!progression.IsSolverUnlocked(SolverId.MachineLearning))
                {
                    if (!progression.SelectOrUnlockSolver(SolverId.MachineLearning))
                    {
                        SetText(feedbackText, progression.CanUnlockMachineLearning ? "Not enough chips" : "PPO requires every Modifier maxed");
                        RefreshEverything();
                        return;
                    }

                    progression.Save();
                }

                ShowPpoModeModal();
                return;
            }

            // If the solver is already active, toggle manual deselect mode.
            if (progression.SelectedSolver == selectedSolverId && progression.IsSolverUnlocked(selectedSolverId))
            {
                solverDeselected = !solverDeselected;
                SetText(feedbackText, solverDeselected ? "Manual mode: solver paused" : $"Solver: {definition.DisplayName}");
                RefreshEverything();
                return;
            }

            bool changed = progression.SelectOrUnlockSolver(selectedSolverId);
            if (changed) solverDeselected = false;
            SetText(feedbackText, changed ? $"Solver: {definition.DisplayName}" : "Not enough chips");
            progression.Save();
            RefreshEverything();
        }

        private void ChoosePpoMode(bool training)
        {
            if (progression == null)
            {
                return;
            }

            if (!training && !progression.MachineLearningPlayingModeUnlocked)
            {
                SetText(feedbackText, "Normal mode is still locked.");
                return;
            }

            progression.SetMachineLearningTrainingMode(training);
            progression.SelectOrUnlockSolver(SolverId.MachineLearning);
            progression.Save();
            HidePpoModeModal();
            SetText(feedbackText, training ? "PPO: Training mode" : "PPO: Normal mode");
            RefreshEverything();
        }

        private void HidePpoModeModal()
        {
            if (ppoModeModal != null)
            {
                SetActive(ppoModeModal.gameObject, false);
            }
        }

        private void ShowPpoModeModal()
        {
            EnsurePpoModeModal();
            bool playingUnlocked = progression.MachineLearningPlayingModeUnlocked;
            long frames = progression.MachineLearningFrames;

            if (ppoNormalButton != null)
            {
                ppoNormalButton.interactable = playingUnlocked;
                SetButtonText(ppoNormalButton, playingUnlocked ? "Normal Mode" : "Normal Mode\n(Locked)");
                SetButtonColor(ppoNormalButton, playingUnlocked ? HexColor("#2563EB") : HexColor("#334155"));
            }

            if (ppoModeHintText != null)
            {
                SetText(ppoModeHintText, playingUnlocked
                    ? "Training: keeps learning, earns no chips.\nNormal: plays for chips like other solvers."
                    : $"Normal mode unlocks after {FormatNumber(progression.MachineLearningPlayingModeFrameRequirement)} trained frames.\n{FormatNumber(frames)} / {FormatNumber(progression.MachineLearningPlayingModeFrameRequirement)}");
            }

            SetActive(ppoModeModal.gameObject, true);
        }

        private void EnsurePpoModeModal()
        {
            if (ppoModeModal != null || canvas == null)
            {
                return;
            }

            ppoModeModal = CreateRuntimePanel("PPO Mode Modal", canvas.transform, HexColor("#020617", 0.78f));
            Stretch(ppoModeModal, 0f, 0f, 0f, 0f);
            Button backdrop = ppoModeModal.gameObject.AddComponent<Button>();
            backdrop.transition = Selectable.Transition.None;
            backdrop.targetGraphic = ppoModeModal.GetComponent<Image>();
            backdrop.onClick.AddListener(HidePpoModeModal);

            RectTransform card = CreateRuntimePanel("Card", ppoModeModal, HexColor("#111A2E", 1f));
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = new Vector2(440f, 340f);
            Image cardImage = card.GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.sprite = GetRoundedSprite(Color.white, Color.white, 28);
                cardImage.type = Image.Type.Sliced;
                cardImage.color = HexColor("#111A2E");
            }

            CreateCardChildText("Title", card, "PPO Mode", 30, new Vector2(0f, 128f), new Vector2(400f, 52f), HexColor("#F8FAFC"));

            ppoTrainingButton = CreateRuntimeButton("Training Btn", card, "Training Mode", HexColor("#0F766E"), new Vector2(0f, 56f), new Vector2(360f, 64f));
            ppoTrainingButton.onClick.AddListener(() => ChoosePpoMode(true));

            ppoNormalButton = CreateRuntimeButton("Normal Btn", card, "Normal Mode", HexColor("#2563EB"), new Vector2(0f, -24f), new Vector2(360f, 64f));
            ppoNormalButton.onClick.AddListener(() => ChoosePpoMode(false));

            ppoModeHintText = CreateCardChildText("Hint", card, string.Empty, 16, new Vector2(0f, -110f), new Vector2(400f, 80f), HexColor("#94A3B8"));

            ppoModeModal.gameObject.SetActive(false);
        }

        private void EnsureResearchUi()
        {
            HideLegacyPrestigeResearchSection();
            EnsureResearchTabButton();

            if (researchPanel != null && prestigeButton != null && researchDetailActionButton != null && researchButtons.Length > 0)
            {
                return;
            }

            RectTransform tabRoot = researchPanel != null
                ? researchPanel.transform.parent as RectTransform
                : upgradesPanel != null ? upgradesPanel.transform.parent as RectTransform : null;
            if (tabRoot == null)
            {
                return;
            }

            RectTransform panel = researchPanel != null
                ? researchPanel.GetComponent<RectTransform>()
                : CreateRuntimePanel("Research Panel", tabRoot, HexColor("#000000", 0f));
            researchPanel = panel.gameObject;
            Stretch(panel);
            researchPanel.SetActive(false);

            TMP_Text title = CreateRuntimeText("Research Title", panel, "Research", 38, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#F8FAFC"));
            SetTopStretchRuntime(title.rectTransform, 0f, 0f, 280f, 58f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 20;
            title.fontSizeMax = 38;

            RectTransform wallet = CreateRuntimePanel("Research Wallet", panel, HexColor("#141C2B"));
            SetTopRightRuntime(wallet, 0f, 0f, 360f, 58f);
            TMP_Text walletText = CreateRuntimeText("Research Wallet Text", wallet, "Insight: 0", 20, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#FDE68A"));
            Stretch(walletText.rectTransform, 12f, 0f, 12f, 0f);
            walletText.enableAutoSizing = true;
            walletText.fontSizeMin = 12;
            walletText.fontSizeMax = 20;
            chipsTexts = AppendTextTarget(chipsTexts, walletText);

            TMP_Text subtitle = CreateRuntimeText(
                "Research Subtitle",
                panel,
                "Late-game permanent upgrades. Prestige converts trained PPO performance into Insight, then this tree speeds up every future cycle.",
                20,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                HexColor("#CBD5E1"));
            SetTopStretchRuntime(subtitle.rectTransform, 0f, 78f, 0f, 46f);
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 12;
            subtitle.fontSizeMax = 20;

            RectTransform console = CreateRuntimeCategoryPanel(panel, "Prestige Console", 136f, 146f);
            prestigeSummaryText = CreateRuntimeText("Prestige Summary", console, "Research locked.", 18, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#CBD5E1"));
            Stretch(prestigeSummaryText.rectTransform, 0f, 0f, 250f, 0f);
            prestigeSummaryText.enableAutoSizing = true;
            prestigeSummaryText.fontSizeMin = 10;
            prestigeSummaryText.fontSizeMax = 18;

            prestigeButton = CreateRuntimeButton("Prestige Button", console, "Prestige", HexColor("#7C3AED"), Vector2.zero, new Vector2(230f, 74f));
            SetTopRightRuntime(prestigeButton.GetComponent<RectTransform>(), 0f, 0f, 230f, 74f);

            RectTransform tree = CreateRuntimeCategoryPanel(panel, "Research Tree", 304f, 640f, 420f);
            RectTransform connectorLayer = CreateRuntimePanel("Research Connectors", tree, HexColor("#000000", 0f));
            Stretch(connectorLayer);
            researchConnectorImages = CreateRuntimeResearchConnectors(connectorLayer);
            researchButtons = new Button[StackMergeProgression.Research.Length];
            for (int i = 0; i < researchButtons.Length; i++)
            {
                ResearchDefinition definition = StackMergeProgression.Research[i];
                Button button = CreateRuntimeButton($"Research {i}", tree, definition.DisplayName, HexColor("#334155"), Vector2.zero, ResearchNodeSize);
                SetTopLeftRuntime(button.GetComponent<RectTransform>(), GetResearchNodePosition(definition), ResearchNodeSize);
                researchButtons[i] = button;
            }

            RectTransform detail = CreateRuntimeCategoryPanelRight(panel, "Selected Research", 304f, 400f, 640f);
            researchDetailNameText = CreateRuntimeText("Research Detail Name", detail, "Insight Amplifier", 30, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#F8FAFC"));
            SetTopStretchRuntime(researchDetailNameText.rectTransform, 0f, 0f, 0f, 42f);
            researchDetailNameText.enableAutoSizing = true;
            researchDetailNameText.fontSizeMin = 16;
            researchDetailNameText.fontSizeMax = 30;

            researchDetailStatusText = CreateRuntimeText("Research Detail Status", detail, "Locked", 18, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#FDE68A"));
            SetTopStretchRuntime(researchDetailStatusText.rectTransform, 0f, 52f, 0f, 34f);
            researchDetailStatusText.enableAutoSizing = true;
            researchDetailStatusText.fontSizeMin = 11;
            researchDetailStatusText.fontSizeMax = 18;

            researchDetailInfoText = CreateRuntimeText("Research Detail Info", detail, "Select a node.", 19, FontStyles.Normal, TextAlignmentOptions.TopLeft, HexColor("#CBD5E1"));
            Stretch(researchDetailInfoText.rectTransform, 0f, 98f, 0f, 86f);
            researchDetailInfoText.enableAutoSizing = true;
            researchDetailInfoText.fontSizeMin = 12;
            researchDetailInfoText.fontSizeMax = 19;

            researchDetailActionButton = CreateRuntimeButton("Research Buy Button", detail, "Buy", HexColor("#7C3AED"), Vector2.zero, new Vector2(240f, 64f));
            SetBottomCenterRuntime(researchDetailActionButton.GetComponent<RectTransform>(), 240f, 64f, 0f);
        }

        private void HideLegacyPrestigeResearchSection()
        {
            if (upgradesPanel == null)
            {
                return;
            }

            foreach (Transform child in upgradesPanel.transform)
            {
                if (child.name.IndexOf("Prestige & Research", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        private void EnsureResearchTabButton()
        {
            if (tabButtons == null || tabButtons.Length >= 7 || tabButtons.Length == 0)
            {
                return;
            }

            RectTransform tabRoot = tabButtons[0] != null ? tabButtons[0].transform.parent as RectTransform : null;
            if (tabRoot == null)
            {
                return;
            }

            var buttons = new Button[7];
            Array.Copy(tabButtons, buttons, tabButtons.Length);
            Button researchTab = CreateRuntimeButton("Research Tab", tabRoot, "Research", HexColor("#18212F"), Vector2.zero, Vector2.zero);
            int researchIndex = 5;
            researchTab.onClick.AddListener(() => SelectTab(researchIndex));

            for (int i = 0; i < buttons.Length; i++)
            {
                if (i < 5)
                {
                    buttons[i] = tabButtons[i];
                }
                else if (i == 5)
                {
                    buttons[i] = researchTab;
                }
                else
                {
                    buttons[i] = tabButtons.Length > 5 ? tabButtons[5] : null;
                }
            }

            tabButtons = buttons;
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] != null)
                {
                    int tabIndex = i;
                    tabButtons[i].onClick.RemoveAllListeners();
                    tabButtons[i].onClick.AddListener(() => SelectTab(tabIndex));
                    SetGridCellRuntime(tabButtons[i].GetComponent<RectTransform>(), i, tabButtons.Length, 0, 1, 8f);
                }
            }
        }

        private RectTransform CreateRuntimeCategoryPanel(RectTransform parent, string titleText, float top, float height, float right = 0f)
        {
            RectTransform panel = CreateRuntimePanel($"{titleText} Category", parent, HexColor("#18212F"));
            SetTopStretchRuntime(panel, 0f, top, right, height);

            TMP_Text title = CreateRuntimeText("Title", panel, titleText, 20, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#E5E7EB"));
            SetTopStretchRuntime(title.rectTransform, 18f, 8f, 18f, 28f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 12;
            title.fontSizeMax = 20;

            RectTransform content = CreateRuntimePanel($"{titleText} Content", panel, HexColor("#000000", 0f));
            Stretch(content, 18f, 44f, 18f, 14f);
            return content;
        }

        private RectTransform CreateRuntimeCategoryPanelRight(RectTransform parent, string titleText, float top, float width, float height)
        {
            RectTransform panel = CreateRuntimePanel($"{titleText} Category", parent, HexColor("#18212F"));
            SetTopRightRuntime(panel, top, 0f, width, height);

            TMP_Text title = CreateRuntimeText("Title", panel, titleText, 20, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#E5E7EB"));
            SetTopStretchRuntime(title.rectTransform, 18f, 8f, 18f, 28f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 12;
            title.fontSizeMax = 20;

            RectTransform content = CreateRuntimePanel($"{titleText} Content", panel, HexColor("#000000", 0f));
            Stretch(content, 18f, 44f, 18f, 14f);
            return content;
        }

        private static TMP_Text[] AppendTextTarget(TMP_Text[] targets, TMP_Text target)
        {
            if (target == null)
            {
                return targets ?? Array.Empty<TMP_Text>();
            }

            targets ??= Array.Empty<TMP_Text>();
            if (targets.Contains(target))
            {
                return targets;
            }

            var resized = new TMP_Text[targets.Length + 1];
            Array.Copy(targets, resized, targets.Length);
            resized[^1] = target;
            return resized;
        }

        private static Image[] CreateRuntimeResearchConnectors(RectTransform parent)
        {
            var images = new List<Image>();
            foreach ((ResearchId from, ResearchId to) in ResearchConnections)
            {
                ResearchDefinition fromDefinition = GetStaticResearchDefinition(from);
                ResearchDefinition toDefinition = GetStaticResearchDefinition(to);
                Vector2 fromPosition = GetResearchNodePosition(fromDefinition) + new Vector2(ResearchNodeSize.x * 0.5f, ResearchNodeSize.y);
                Vector2 toPosition = GetResearchNodePosition(toDefinition) + new Vector2(ResearchNodeSize.x * 0.5f, 0f);
                AddRuntimeResearchArrow(parent, fromPosition, toPosition, images);
            }

            return images.ToArray();
        }

        private static void AddRuntimeResearchArrow(RectTransform parent, Vector2 from, Vector2 to, List<Image> images)
        {
            Image main = AddRuntimeLine(parent, "Research Arrow", from, to, 4f);
            images.Add(main);

            Vector2 delta = to - from;
            if (delta.sqrMagnitude < 0.01f)
            {
                return;
            }

            Vector2 direction = delta.normalized;
            Vector2 normal = new(-direction.y, direction.x);
            Vector2 left = to - direction * 18f + normal * 8f;
            Vector2 right = to - direction * 18f - normal * 8f;
            images.Add(AddRuntimeLine(parent, "Research Arrow Head", to, left, 3.2f));
            images.Add(AddRuntimeLine(parent, "Research Arrow Head", to, right, 3.2f));
        }

        private static Image AddRuntimeLine(RectTransform parent, string name, Vector2 from, Vector2 to, float thickness)
        {
            RectTransform line = CreateRuntimePanel(name, parent, HexColor("#334155", 0.9f));
            Image image = line.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }

            Vector2 delta = to - from;
            Vector2 middle = (from + to) * 0.5f;
            line.anchorMin = new Vector2(0f, 1f);
            line.anchorMax = new Vector2(0f, 1f);
            line.pivot = new Vector2(0.5f, 0.5f);
            line.anchoredPosition = new Vector2(middle.x, -middle.y);
            line.sizeDelta = new Vector2(Mathf.Max(1f, delta.magnitude), thickness);
            line.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(-delta.y, delta.x) * Mathf.Rad2Deg);
            return image;
        }

        private static ResearchDefinition GetStaticResearchDefinition(ResearchId researchId)
        {
            int index = (int)researchId;
            return index >= 0 && index < StackMergeProgression.Research.Length ? StackMergeProgression.Research[index] : StackMergeProgression.Research[0];
        }

        private static Vector2 GetResearchNodePosition(ResearchDefinition definition)
        {
            return new Vector2(
                ResearchNodeLeft + definition.BranchColumn * ResearchNodeColumnSpacing,
                ResearchNodeTop + definition.Tier * ResearchNodeTierSpacing);
        }

        private TMP_Text CreateCardChildText(string name, Transform parent, string label, int fontSize, Vector2 anchoredPosition, Vector2 size, Color color)
        {
            TMP_Text text = CreateRuntimeText(name, parent, label, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, color);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = 12;
            text.fontSizeMax = fontSize;
            return text;
        }

        private Button CreateRuntimeButton(string name, Transform parent, string label, Color color, Vector2 anchoredPosition, Vector2 size)
        {
            RectTransform rect = CreateRuntimePanel(name, parent, color);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = GetRoundedSprite(Color.white, Color.white, 18);
                image.type = Image.Type.Sliced;
                image.color = color;
            }

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text text = CreateRuntimeText(name + " Label", rect, label, 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform, 12f, 6f, 12f, 6f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 12;
            text.fontSizeMax = 24;
            return button;
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

        private void ExecutePrestige()
        {
            if (progression == null)
            {
                return;
            }

            long gained = progression.ExecutePrestige();
            if (gained <= 0)
            {
                SetText(feedbackText, progression.PrestigeAvailable ? "Run PPO in Normal mode first" : "Finish PPO Training first");
                RefreshEverything();
                return;
            }

            highScore = 0;
            PlayerPrefs.SetInt(HighScoreKey, 0);
            PlayerPrefs.Save();
            selectedSolverId = progression.SelectedSolver;
            selectedAgentId = AgentId.MergeBroker;
            selectedModifierId = ModifierId.UnstableStack;
            solverTuneOpen = false;
            historyOpen = false;
            achievementsOpen = false;
            gameplayInfoOpen = false;
            HidePpoModeModal();
            CreateFreshGame();
            SetText(feedbackText, $"Prestige complete: +{FormatNumber(gained)} Insight");
            progression.SaveImmediate();
            SelectTab(5);
            RefreshEverything();
        }

        private void SelectResearchUpgrade(ResearchId researchId)
        {
            selectedResearchId = researchId;
            RefreshResearchMenu();
        }

        private void BuySelectedResearchUpgrade()
        {
            BuyResearchUpgrade(selectedResearchId);
        }

        private void BuyResearchUpgrade(ResearchId researchId)
        {
            if (progression == null)
            {
                return;
            }

            ResearchDefinition definition = progression.GetResearchDefinition(researchId);
            bool bought = progression.BuyResearch(researchId);
            SetText(feedbackText, bought
                ? $"{definition.DisplayName} L{progression.GetResearchLevel(researchId)}"
                : progression.GetResearchUnavailableReason(researchId));
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
            RefreshGameView();
            RefreshProgressionUi();
        }

        /// <summary>
        /// Cheap per-move refresh: only the board, the queue, the game-over overlay and the
        /// text-only HUD. The heavy panels (solver/agent/modifier/upgrade buttons, history
        /// charts, achievement lists) are NOT rebuilt here — they only change on user actions
        /// and are refreshed by <see cref="RefreshProgressionUi"/>. Rebuilding everything on
        /// every solver move was a major frame-rate sink.
        /// </summary>
        private void RefreshGameView()
        {
            if (gameState == null)
            {
                return;
            }

            SetText(scoreText, FormatNumber(gameState.Score));
            SetText(droppedText, $"Dobasok: {FormatNumber(gameState.BlocksDropped)}");

            bool trainingView = progression != null
                && progression.IsMachineLearningTrainingActive
                && selectedTabIndex == 0
                && !historyOpen
                && !achievementsOpen;

            SetTrainingView(trainingView);
            if (trainingView)
            {
                UpdateTrainingOverlay(gameState.IsGameOver ? null : "Auto solving");
            }
            else
            {
                RefreshNextBlocks();
                RefreshColumns();
            }

            RefreshGameOver();
            RefreshHud();
        }

        private void SetTrainingView(bool active)
        {
            EnsureTrainingOverlay();
            if (trainingOverlay != null)
            {
                SetActive(trainingOverlay.gameObject, active);
            }

            // While the simplified matrix view is up, hide the graphical board and queue.
            if (boardRoot != null)
            {
                SetActive(boardRoot.gameObject, !active);
            }

            if (nextBlocksRoot != null && nextBlocksRoot.parent is RectTransform nextPanel)
            {
                SetActive(nextPanel.gameObject, !active);
            }
        }

        private void EnsureTrainingOverlay()
        {
            if (trainingOverlay != null || canvas == null)
            {
                return;
            }

            trainingOverlay = CreateRuntimePanel("Training Overlay", canvas.transform, HexColor("#0E1626", 0.98f));
            trainingOverlay.anchorMin = new Vector2(0.04f, 0.12f);
            trainingOverlay.anchorMax = new Vector2(0.96f, 0.82f);
            trainingOverlay.offsetMin = Vector2.zero;
            trainingOverlay.offsetMax = Vector2.zero;

            Image background = trainingOverlay.GetComponent<Image>();
            if (background != null)
            {
                background.sprite = GetRoundedSprite(Color.white, Color.white, 28);
                background.type = Image.Type.Sliced;
                background.color = HexColor("#0E1626", 0.98f);
            }

            trainingOverlayText = CreateRuntimeText(
                "Training Text",
                trainingOverlay,
                string.Empty,
                26,
                FontStyles.Bold,
                TextAlignmentOptions.Top,
                HexColor("#E2E8F0"));
            Stretch(trainingOverlayText.rectTransform, 30f, 26f, 30f, 26f);
            trainingOverlayText.richText = true;
            trainingOverlayText.enableAutoSizing = true;
            trainingOverlayText.fontSizeMin = 12;
            trainingOverlayText.fontSizeMax = 30;

            trainingOverlay.gameObject.SetActive(false);
        }

        private void UpdateTrainingOverlay(string statusLine)
        {
            EnsureTrainingOverlay();
            if (trainingOverlayText == null || gameState == null || progression == null)
            {
                return;
            }

            StackMergePpoMetrics metrics = progression.MachineLearningAgent.Metrics;
            var builder = new StringBuilder();
            builder.AppendLine("<b>PPO TRAINING</b>");
            builder.AppendLine();
            builder.Append("<mspace=0.62em>");
            builder.Append(BuildTrainingMatrix());
            builder.Append("</mspace>");
            builder.AppendLine();

            builder.Append("Next  ");
            builder.Append("<mspace=0.62em>");
            for (int i = 0; i < gameState.NextBlocks.Count; i++)
            {
                builder.Append(FormatMatrixCell(gameState.NextBlocks[i]).PadLeft(6));
            }
            builder.Append("</mspace>");
            builder.AppendLine();
            builder.AppendLine();

            builder.AppendLine($"Iteration  {metrics.Updates}");
            builder.AppendLine($"Frames  {metrics.Steps}   (run {gameState.BlocksDropped})");
            builder.AppendLine($"Score  {FormatNumber(gameState.Score)}   High  {FormatNumber(Math.Max(1, gameState.HighestMergedBlock))}");
            builder.AppendLine($"Policy loss  {metrics.LastPolicyLoss:0.000}   Entropy  {metrics.LastEntropy:0.000}");
            if (!string.IsNullOrEmpty(statusLine))
            {
                builder.AppendLine();
                builder.AppendLine($"<color=#F0ABFC>{statusLine}</color>");
            }

            trainingOverlayText.text = builder.ToString();
        }

        private string BuildTrainingMatrix()
        {
            if (gameState == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            int capacity = gameState.StackCapacity;
            for (int row = capacity - 1; row >= 0; row--)
            {
                for (int stackIndex = 0; stackIndex < gameState.StackCount; stackIndex++)
                {
                    IReadOnlyList<int> stack = gameState.Stacks[stackIndex];
                    string cell = row < stack.Count ? FormatMatrixCell(stack[row]) : ".";
                    builder.Append(cell.PadLeft(6));
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string FormatMatrixCell(int value)
        {
            return value == StackMergeGameState.JokerBlockValue ? "J" : value.ToString();
        }

        private void RefreshHud()
        {
            if (progression == null)
            {
                return;
            }

            bool machineLearningTraining = progression.IsMachineLearningTrainingActive;
            SetText(chipsTexts, $"Chips: {FormatNumber(progression.Chips)} | Tokens: {FormatNumber(progression.Tokens)} | Insight: {FormatNumber(progression.ResearchInsight)}");
            if (solverDeselected)
            {
                SetText(solverText, $"Solver: Manual  ({GetSelectedSolver().DisplayName} paused)");
            }
            else if (progression.SelectedSolver == SolverId.MachineLearning)
            {
                SetText(solverText, $"Solver: {GetSelectedSolver().DisplayName} Lv {progression.MachineLearningLevel}");
            }
            else
            {
                SetText(solverText, $"Solver: {GetSelectedSolver().DisplayName}");
            }
            SetText(speedText, machineLearningTraining
                ? $"Speed L{progression.SpeedLevel} | {progression.MoveInterval:0.000}s | training"
                : $"Speed L{progression.SpeedLevel} | {progression.MoveInterval:0.00}s");

            if (gameState != null && !gameState.IsGameOver)
            {
                SetText(runStatusText, machineLearningTraining
                    ? "ML TRAINING - chips paused"
                    : (solverDeselected || !progression.AutoSolveEnabled) ? "Manual mode" : "Auto solving");
                if (runStatusText != null)
                {
                    runStatusText.color = machineLearningTraining ? HexColor("#F0ABFC") : HexColor("#D1D5DB");
                }
            }

            if (feedbackText != null)
            {
                feedbackText.color = machineLearningTraining ? HexColor("#F0ABFC") : HexColor("#5EEAD4");
            }
        }

        private void RefreshProgressionUi()
        {
            if (progression == null)
            {
                return;
            }

            RefreshHud();

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
            RefreshResearchMenu();
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
                !unlocked ? "Tune\nLocked" : !progression.SolverTuningUnlocked ? "Tune\nUpgrade" : canTune ? "Tune" : "No tuning");
            if (solverDetailTuneButton != null)
            {
                solverDetailTuneButton.interactable = canTune;
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

            string statusLabel = isMachineLearning
                ? active ? $"Active | Lv {progression.MachineLearningLevel}" : $"Unlocked | Lv {progression.MachineLearningLevel}"
                : active ? (solverDeselected ? "Paused — manual mode" : "Active") : "Unlocked";
            SetText(solverDetailStatusText, statusLabel);
            if (!isMachineLearning && active)
            {
                SetButtonText(solverDetailActionButton, solverDeselected ? "Resume solver" : "Deselect");
                if (solverDetailActionButton != null) solverDetailActionButton.interactable = true;
            }
            else
            {
                SetButtonText(solverDetailActionButton, active ? "Selected" : "Select");
                if (solverDetailActionButton != null) solverDetailActionButton.interactable = !active;
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

                // Small whole-number parameters become a row of buttons (one per value) showing the
                // real resolved value, which is far clearer than a slider snapping to "-3".
                bool useSegments = parameter.WholeNumbers
                    && parameter.MaxValue > parameter.MinValue
                    && (parameter.MaxValue - parameter.MinValue) <= 6;

                if (i < solverTuneValueTexts.Length)
                {
                    SetText(solverTuneValueTexts[i], useSegments
                        ? FormatWholeParamValue(selectedSolverId, parameter.Id, value)
                        : parameter.FormatValue(value));
                }

                Slider slider = i < solverTuneSliders.Length ? solverTuneSliders[i] : null;
                if (useSegments && slider != null)
                {
                    SetActive(slider.gameObject, false);
                    BuildTuneSegments(i, slider.GetComponent<RectTransform>(), parameter, value);
                }
                else
                {
                    HideTuneSegments(i);
                    if (slider != null)
                    {
                        SetActive(slider.gameObject, true);
                        slider.minValue = parameter.MinDisplayValue;
                        slider.maxValue = parameter.MaxDisplayValue;
                        slider.wholeNumbers = parameter.WholeNumbers;
                        slider.SetValueWithoutNotify(parameter.ToDisplayValue(value));
                    }
                }
            }

            if (solverTuneResetButton != null)
            {
                solverTuneResetButton.interactable = !tuning.IsNeutral;
            }
        }

        private string FormatWholeParamValue(SolverId solverId, SolverTuneParameterId param, int offset)
        {
            int real = ResolveWholeParamValue(solverId, param, offset);
            return offset == 0 ? $"{real} (Default)" : real.ToString();
        }

        // Maps an offset-style whole-number tuning value to the real quantity the player gets in
        // real-time (lightweight) play, so the UI can show "60" iterations instead of "-3".
        private int ResolveWholeParamValue(SolverId solverId, SolverTuneParameterId param, int offset)
        {
            int sims = progression != null ? progression.MonteCarloSimulationCount : 5;
            int depth = progression != null ? progression.MonteCarloRolloutDepth : 4;
            switch (param)
            {
                case SolverTuneParameterId.TreeVisits:
                    return Math.Max(2, Math.Max(24, sims * 3) + offset * 2);
                case SolverTuneParameterId.SimulationRounds:
                    return Math.Max(1, sims + offset);
                case SolverTuneParameterId.RolloutMoves:
                    return Math.Max(1, depth + offset);
                case SolverTuneParameterId.PlanningDepth:
                    return Math.Max(1, (solverId == SolverId.Plan5 ? 5 : 3) + offset);
                case SolverTuneParameterId.RolloutPlanning:
                    return Math.Max(1, 3 + offset);
                case SolverTuneParameterId.FutureDepth:
                    return Math.Max(1, 3 + offset);
                default:
                    return offset;
            }
        }

        private void BuildTuneSegments(int rowIndex, RectTransform sliderRect, SolverTuningParameterDefinition parameter, int currentValue)
        {
            if (sliderRect == null)
            {
                return;
            }

            solverTuneSegmentContainers ??= new RectTransform[solverTuneRows.Length];
            RectTransform container = rowIndex < solverTuneSegmentContainers.Length ? solverTuneSegmentContainers[rowIndex] : null;
            if (container == null)
            {
                container = CreateRuntimePanel($"Tune Segments {rowIndex}", sliderRect.parent, new Color(0f, 0f, 0f, 0f));
                Image containerImage = container.GetComponent<Image>();
                if (containerImage != null)
                {
                    containerImage.raycastTarget = false;
                }

                HorizontalLayoutGroup layout = container.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 4f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
                layout.childAlignment = TextAnchor.MiddleCenter;
                if (rowIndex < solverTuneSegmentContainers.Length)
                {
                    solverTuneSegmentContainers[rowIndex] = container;
                }
            }

            // Occupy exactly the slider's slot.
            container.anchorMin = sliderRect.anchorMin;
            container.anchorMax = sliderRect.anchorMax;
            container.pivot = sliderRect.pivot;
            container.offsetMin = sliderRect.offsetMin;
            container.offsetMax = sliderRect.offsetMax;
            SetActive(container.gameObject, true);

            for (int c = container.childCount - 1; c >= 0; c--)
            {
                Destroy(container.GetChild(c).gameObject);
            }

            Sprite rounded = GetRoundedSprite(Color.white, Color.white, 12);
            for (int raw = parameter.MinValue; raw <= parameter.MaxValue; raw++)
            {
                int captured = raw;
                bool selected = raw == currentValue;
                string label = ResolveWholeParamValue(selectedSolverId, parameter.Id, raw).ToString();
                CreateSegmentButton(container, label, selected, rounded, () => SetSelectedSolverTuningRaw(rowIndex, captured));
            }
        }

        private void HideTuneSegments(int rowIndex)
        {
            if (solverTuneSegmentContainers == null || rowIndex >= solverTuneSegmentContainers.Length)
            {
                return;
            }

            RectTransform container = solverTuneSegmentContainers[rowIndex];
            if (container != null)
            {
                SetActive(container.gameObject, false);
            }
        }

        private void CreateSegmentButton(RectTransform container, string label, bool selected, Sprite rounded, UnityEngine.Events.UnityAction onClick)
        {
            Color color = selected ? HexColor("#2563EB") : HexColor("#1E293B");
            RectTransform rect = CreateRuntimePanel("Seg", container, color);
            Image image = rect.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = rounded;
                image.type = Image.Type.Sliced;
                image.color = color;
            }

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;
            layoutElement.minWidth = 24f;

            TMP_Text text = CreateRuntimeText("Label", rect, label, 15, FontStyles.Bold, TextAlignmentOptions.Center, selected ? Color.white : HexColor("#CBD5E1"));
            Stretch(text.rectTransform, 1f, 1f, 1f, 1f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 9;
            text.fontSizeMax = 15;
        }

        private void SetSelectedSolverTuningRaw(int slotIndex, int rawValue)
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

            progression.SetSolverTuningValue(selectedSolverId, slotIndex, rawValue);
            progression.Save();

            SolverDefinition definition = StackMergeSolverCatalog.GetDefinition(selectedSolverId);
            SetText(feedbackText, $"{definition.DisplayName} tuning updated");
            RefreshSolverTunePanel();
            RefreshSolverDetails();
            RefreshGameplayInfo();
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
                    : "Endgame - PPO training";
                string nextGoal = progression.IsSolverUnlocked(SolverId.MachineLearning)
                    ? progression.GetMachineLearningStatus()
                    : progression.ModifiersMenuUnlocked
                    ? progression.AllModifiersMaxed ? "PPO is ready to unlock in Algorithms." : "Max every Modifier to open the Machine Learning layer."
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

        private void RefreshResearchMenu()
        {
            if (progression == null)
            {
                return;
            }

            if (prestigeSummaryText != null)
            {
                SetText(prestigeSummaryText, progression.GetPrestigeSummary());
            }

            if (prestigeButton != null)
            {
                long gain = progression.PreviewPrestigeInsightGain();
                if (gain > 0)
                {
                    SetButtonText(prestigeButton, $"Prestige\n+{FormatNumber(gain)} Insight");
                    prestigeButton.interactable = true;
                    SetButtonColor(prestigeButton, HexColor("#7C3AED"));
                }
                else
                {
                    SetButtonText(prestigeButton, progression.PrestigeAvailable ? "Prestige\nRun Normal PPO" : "Prestige\nNeeds Training");
                    prestigeButton.interactable = false;
                    SetButtonColor(prestigeButton, HexColor("#334155"));
                }
            }

            for (int i = 0; i < researchButtons.Length && i < StackMergeProgression.Research.Length; i++)
            {
                Button button = researchButtons[i];
                if (button == null)
                {
                    continue;
                }

                ResearchDefinition definition = StackMergeProgression.Research[i];
                ResearchId researchId = definition.Id;
                int level = progression.GetResearchLevel(researchId);
                bool maxed = progression.IsResearchMaxed(researchId);
                bool canBuy = progression.CanBuyResearch(researchId);
                string effect = progression.GetResearchEffectSummary(researchId);
                bool selected = researchId == selectedResearchId;
                string prefix = selected ? "> " : string.Empty;
                string label = maxed
                    ? $"{prefix}{definition.DisplayName}\nL{level}/{definition.MaxLevel}\nMaxed"
                    : $"{prefix}{definition.DisplayName}\nL{level}/{definition.MaxLevel}\n{FormatNumber(progression.GetResearchCost(researchId))} Insight";

                string reason = progression.GetResearchUnavailableReason(researchId);
                if (!maxed && !string.IsNullOrEmpty(reason) && reason != "Not enough Insight.")
                {
                    label = $"{prefix}{definition.DisplayName}\nL{level}/{definition.MaxLevel}\nLocked";
                }

                SetButtonText(button, label);
                button.interactable = IsResearchMenuUnlocked();
                SetButtonColor(button, selected ? HexColor("#1D4ED8") : maxed ? HexColor("#0F766E") : level > 0 ? HexColor("#115E59") : canBuy ? HexColor("#7C3AED") : HexColor("#334155"));
            }

            RefreshResearchConnectors();
            RefreshSelectedResearchDetails();
        }

        private void RefreshResearchConnectors()
        {
            if (researchConnectorImages == null || researchConnectorImages.Length == 0 || progression == null)
            {
                return;
            }

            for (int connectionIndex = 0; connectionIndex < ResearchConnections.Length; connectionIndex++)
            {
                (ResearchId from, ResearchId to) = ResearchConnections[connectionIndex];
                Color color = progression.GetResearchLevel(to) > 0
                    ? HexColor("#0F766E", 0.95f)
                    : progression.GetResearchLevel(from) > 0
                        ? HexColor("#7C3AED", 0.9f)
                        : HexColor("#334155", 0.72f);

                int firstSegment = connectionIndex * 3;
                for (int offset = 0; offset < 3; offset++)
                {
                    int imageIndex = firstSegment + offset;
                    if (imageIndex < researchConnectorImages.Length && researchConnectorImages[imageIndex] != null)
                    {
                        researchConnectorImages[imageIndex].color = color;
                    }
                }
            }
        }

        private void RefreshSelectedResearchDetails()
        {
            if (progression == null)
            {
                return;
            }

            ResearchDefinition definition = progression.GetResearchDefinition(selectedResearchId);
            int level = progression.GetResearchLevel(selectedResearchId);
            bool maxed = progression.IsResearchMaxed(selectedResearchId);
            bool canBuy = progression.CanBuyResearch(selectedResearchId);
            string effect = progression.GetResearchEffectSummary(selectedResearchId);
            string reason = progression.GetResearchUnavailableReason(selectedResearchId);

            SetText(researchDetailNameText, definition.DisplayName);
            SetText(researchDetailStatusText, $"Level {level}/{definition.MaxLevel} | {effect}");

            string availability = maxed
                ? "This research is maxed."
                : string.IsNullOrEmpty(reason)
                    ? $"Ready to buy for {FormatNumber(progression.GetResearchCost(selectedResearchId))} Insight."
                    : reason;

            SetText(researchDetailInfoText,
                $"{definition.Description}\n\nCurrent effect: {effect}\n\n{availability}\n\nInsight: {FormatNumber(progression.ResearchInsight)}");

            if (researchDetailActionButton != null)
            {
                if (maxed)
                {
                    SetButtonText(researchDetailActionButton, "Maxed");
                    researchDetailActionButton.interactable = false;
                    SetButtonColor(researchDetailActionButton, HexColor("#0F766E"));
                }
                else
                {
                    SetButtonText(researchDetailActionButton, canBuy ? $"Buy\n{FormatNumber(progression.GetResearchCost(selectedResearchId))}" : "Locked");
                    researchDetailActionButton.interactable = canBuy;
                    SetButtonColor(researchDetailActionButton, canBuy ? HexColor("#7C3AED") : HexColor("#334155"));
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
                DrawTrendChart(history);
                BuildSolverList(Array.Empty<HistorySolverStats>());
                CreateTable(historyRecentRunsRoot, new[] { "Run", "Solver", "Score", "Moves", "Merges", "High", "Risk" }, Array.Empty<string[]>(), "Recent runs will appear here.");

                return;
            }

            RunHistoryEntry latest = history[0];
            RunHistoryEntry best = history.OrderByDescending(entry => entry.score).First();
            HistorySolverStats[] solverStats = BuildHistorySolverStats(history);
            HistorySolverStats bestMedian = solverStats.OrderByDescending(stats => stats.MedianScore).First();
            HistorySolverStats bestPeak = solverStats.OrderByDescending(stats => stats.MaxScore).First();
            int trendCount = Math.Min(history.Length, 40);

            SetText(
                historySummaryText,
                $"Stored runs: {history.Length}/250 | Latest: {FormatNumber(latest.score)} ({SolverName(latest.solverId)}) | Best: {FormatNumber(best.score)} ({SolverName(best.solverId)})  —  tap a solver's  i  for its detail & score history");

            SetText(
                historyInsightText,
                $"Best median: {bestMedian.SolverName} ({FormatNumber(bestMedian.MedianScore)}) | Best peak: {bestPeak.SolverName} ({FormatNumber(bestPeak.MaxScore)}) | Trend = last {trendCount} runs (chronological), more honest than a single aggregate median");

            DrawTrendChart(history);
            BuildSolverList(solverStats);
            RefreshRecentRunsTable(history);
        }

        // Recent score trend: the last runs in chronological order. This reflects how the player's
        // setup is actually performing right now, instead of an all-time aggregate that jumps every
        // time an upgrade is bought.
        private void DrawTrendChart(RunHistoryEntry[] history)
        {
            int take = Math.Min(history.Length, 40);
            var values = new List<double>(take);
            for (int i = take - 1; i >= 0; i--)
            {
                values.Add(history[i].score);
            }

            DrawLineChart(historyChartRoot, values, HexColor("#38BDF8"), "No runs yet");
        }

        private void RefreshRecentRunsTable(RunHistoryEntry[] history)
        {
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

        // Per-solver list with an info button on each row that opens a detail window.
        private void BuildSolverList(HistorySolverStats[] stats)
        {
            RectTransform root = historySolverTableRoot;
            if (root == null)
            {
                return;
            }

            ClearChildren(root);
            float width = Mathf.Max(620f, root.rect.width);
            float rowHeight = 40f;
            float y = 0f;

            CreateSolverListRow(root, width, y, rowHeight, "Solver", "Runs", "Median", "Best", "High", true, SolverId.Rand);
            y += rowHeight + 4f;

            if (stats.Length == 0)
            {
                TMP_Text empty = CreateRuntimeText("Empty", root, "No solver data yet.", 18, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#64748B"));
                RectTransform emptyRect = empty.rectTransform;
                emptyRect.anchorMin = new Vector2(0f, 1f);
                emptyRect.anchorMax = new Vector2(0f, 1f);
                emptyRect.pivot = new Vector2(0f, 1f);
                emptyRect.anchoredPosition = new Vector2(0f, -y - 18f);
                emptyRect.sizeDelta = new Vector2(width, 44f);
                return;
            }

            foreach (HistorySolverStats stat in stats.OrderByDescending(s => s.MedianScore))
            {
                CreateSolverListRow(
                    root, width, y, rowHeight,
                    stat.SolverName,
                    stat.SolverId < 0 ? FormatNumber(stat.Runs) : FormatNumber(progression.GetSolverLifetimeRuns((SolverId)stat.SolverId)),
                    FormatNumber(stat.MedianScore),
                    FormatNumber(stat.MaxScore),
                    FormatNumber(stat.BestHighestMerged),
                    false,
                    (SolverId)stat.SolverId);
                y += rowHeight + 3f;
            }
        }

        private void CreateSolverListRow(RectTransform root, float width, float y, float height, string c0, string c1, string c2, string c3, string c4, bool header, SolverId solverId)
        {
            RectTransform row = CreateRuntimePanel("Solver Row", root, header ? HexColor("#0B1322") : HexColor("#141C2B"));
            row.anchorMin = new Vector2(0f, 1f);
            row.anchorMax = new Vector2(0f, 1f);
            row.pivot = new Vector2(0f, 1f);
            row.anchoredPosition = new Vector2(0f, -y);
            row.sizeDelta = new Vector2(width, height);

            float[] frac = { 0.30f, 0.15f, 0.18f, 0.18f, 0.11f };
            string[] cells = { c0, c1, c2, c3, c4 };
            float x = 10f;
            for (int i = 0; i < cells.Length; i++)
            {
                float columnWidth = width * frac[i];
                TMP_Text text = CreateRuntimeText($"Cell{i}", row, cells[i], header ? 15 : 14, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, header ? HexColor("#FDE68A") : HexColor("#E5E7EB"));
                text.enableAutoSizing = true;
                text.fontSizeMin = 9;
                text.fontSizeMax = header ? 15 : 14;
                RectTransform rect = text.rectTransform;
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 0.5f);
                rect.offsetMin = new Vector2(x, 0f);
                rect.offsetMax = new Vector2(x + columnWidth - 6f, 0f);
                x += columnWidth;
            }

            if (!header)
            {
                RectTransform infoRect = CreateRuntimePanel("Info", row, HexColor("#2563EB"));
                infoRect.anchorMin = new Vector2(1f, 0.5f);
                infoRect.anchorMax = new Vector2(1f, 0.5f);
                infoRect.pivot = new Vector2(1f, 0.5f);
                infoRect.anchoredPosition = new Vector2(-10f, 0f);
                infoRect.sizeDelta = new Vector2(40f, height - 12f);
                Image infoImage = infoRect.GetComponent<Image>();
                if (infoImage != null)
                {
                    infoImage.sprite = GetRoundedSprite(Color.white, Color.white, 12);
                    infoImage.type = Image.Type.Sliced;
                    infoImage.color = HexColor("#2563EB");
                }

                Button infoButton = infoRect.gameObject.AddComponent<Button>();
                infoButton.targetGraphic = infoImage;
                SolverId captured = solverId;
                infoButton.onClick.AddListener(() => ShowSolverInfoModal(captured));

                TMP_Text infoLabel = CreateRuntimeText("i", infoRect, "i", 18, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
                Stretch(infoLabel.rectTransform, 2f, 2f, 2f, 2f);
            }
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

        // Simple runtime line chart: connects the values with rotated thin segments, draws dots for
        // small series, and labels the min / max. Used both for the History trend and the per-solver
        // detail window.
        private void DrawLineChart(RectTransform root, IReadOnlyList<double> values, Color lineColor, string emptyText)
        {
            if (root == null)
            {
                return;
            }

            ClearChildren(root);
            if (values == null || values.Count == 0)
            {
                TMP_Text empty = CreateRuntimeText("Empty Chart", root, emptyText, 18, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#64748B"));
                Stretch(empty.rectTransform, 0f, 0f, 0f, 0f);
                return;
            }

            float width = Mathf.Max(320f, root.rect.width);
            float height = Mathf.Max(150f, root.rect.height);
            float padLeft = 10f;
            float padRight = 12f;
            float padTop = 26f;
            float padBottom = 26f;
            float plotWidth = Mathf.Max(1f, width - padLeft - padRight);
            float plotHeight = Mathf.Max(1f, height - padTop - padBottom);

            double min = values.Min();
            double max = values.Max();
            double range = max - min;
            if (range < 1e-6)
            {
                range = Math.Max(1.0, Math.Abs(max));
            }

            int count = values.Count;

            Vector2 PointAt(int index)
            {
                float px = padLeft + (count == 1 ? plotWidth * 0.5f : plotWidth * index / (count - 1));
                float norm = (float)((values[index] - min) / range);
                float py = padBottom + norm * plotHeight;
                return new Vector2(px, py);
            }

            // baseline
            RectTransform baseline = CreateRuntimePanel("Baseline", root, HexColor("#1E293B"));
            baseline.anchorMin = new Vector2(0f, 0f);
            baseline.anchorMax = new Vector2(0f, 0f);
            baseline.pivot = new Vector2(0f, 0.5f);
            baseline.anchoredPosition = new Vector2(padLeft, padBottom);
            baseline.sizeDelta = new Vector2(plotWidth, 2f);
            Image baselineImage = baseline.GetComponent<Image>();
            if (baselineImage != null)
            {
                baselineImage.raycastTarget = false;
            }

            for (int i = 1; i < count; i++)
            {
                CreateLineSegment(root, PointAt(i - 1), PointAt(i), lineColor, 3f);
            }

            if (count <= 30)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2 point = PointAt(i);
                    RectTransform dot = CreateRuntimePanel("Dot", root, lineColor);
                    dot.anchorMin = new Vector2(0f, 0f);
                    dot.anchorMax = new Vector2(0f, 0f);
                    dot.pivot = new Vector2(0.5f, 0.5f);
                    dot.anchoredPosition = point;
                    dot.sizeDelta = new Vector2(7f, 7f);
                    Image dotImage = dot.GetComponent<Image>();
                    if (dotImage != null)
                    {
                        dotImage.sprite = GetRoundedSprite(Color.white, Color.white, 3);
                        dotImage.type = Image.Type.Sliced;
                        dotImage.color = lineColor;
                        dotImage.raycastTarget = false;
                    }
                }
            }

            CreateChartLabel(root, $"max {FormatNumber((long)Math.Round(max))}", new Vector2(padLeft, height - padTop + 2f), 220f, TextAlignmentOptions.Left);
            CreateChartLabel(root, $"min {FormatNumber((long)Math.Round(min))}", new Vector2(padLeft, 2f), 220f, TextAlignmentOptions.Left);
            CreateChartLabel(root, $"{count} runs", new Vector2(width - padRight - 140f, height - padTop + 2f), 140f, TextAlignmentOptions.Right);
        }

        private void CreateLineSegment(RectTransform root, Vector2 from, Vector2 to, Color color, float thickness)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

            RectTransform segment = CreateRuntimePanel("Seg", root, color);
            segment.anchorMin = new Vector2(0f, 0f);
            segment.anchorMax = new Vector2(0f, 0f);
            segment.pivot = new Vector2(0f, 0.5f);
            segment.anchoredPosition = from;
            segment.sizeDelta = new Vector2(length, thickness);
            segment.localRotation = Quaternion.Euler(0f, 0f, angle);
            Image image = segment.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
                image.raycastTarget = false;
            }
        }

        private void CreateChartLabel(RectTransform root, string text, Vector2 anchoredPosition, float width, TextAlignmentOptions alignment)
        {
            TMP_Text label = CreateRuntimeText("Chart Label", root, text, 13, FontStyles.Bold, alignment, HexColor("#94A3B8"));
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(width, 22f);
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

        private void ShowSolverInfoModal(SolverId solverId)
        {
            if (progression == null || (int)solverId < 0)
            {
                return;
            }

            EnsureSolverInfoModal();
            SolverDefinition definition = StackMergeSolverCatalog.GetDefinition(solverId);
            RunHistoryEntry[] solverRuns = progression.RunHistory.Where(entry => entry.solverId == (int)solverId).ToArray();
            int lifetime = progression.GetSolverLifetimeRuns(solverId);

            SetText(solverInfoTitle, $"{definition.DisplayName}  •  detail");

            var stats = new StringBuilder();
            stats.AppendLine(definition.Description);
            stats.AppendLine();
            stats.AppendLine($"Lifetime runs: {FormatNumber(lifetime)}    (stored history: {solverRuns.Length})");
            if (solverRuns.Length > 0)
            {
                long[] scores = solverRuns.Select(entry => entry.score).OrderBy(value => value).ToArray();
                stats.AppendLine($"Best {FormatNumber(scores[^1])}   Median {FormatNumber(Median(scores))}   Avg {FormatNumber((long)scores.Average())}");
                stats.AppendLine($"Range {FormatNumber(scores[0])} – {FormatNumber(scores[^1])}");
                stats.AppendLine($"Best high tile {FormatNumber(solverRuns.Max(entry => entry.highestMergedBlock))}   Avg moves {solverRuns.Average(entry => entry.moves):0}");
            }
            else
            {
                stats.AppendLine("No stored runs for this solver yet.");
            }

            if (solverId == SolverId.MachineLearning)
            {
                StackMergePpoMetrics metrics = progression.MachineLearningAgent.Metrics;
                stats.AppendLine();
                stats.AppendLine("<b>PPO training</b>");
                stats.AppendLine($"Updates {metrics.Updates}   Frames {metrics.Steps}   Lv {progression.MachineLearningLevel}");
                stats.AppendLine($"Avg high {metrics.RecentAverageHigh:0}   Avg score {metrics.RecentAverageScore:0}");
                stats.AppendLine($"Policy loss {metrics.LastPolicyLoss:0.000}   Entropy {metrics.LastEntropy:0.000}");
                stats.AppendLine(progression.MachineLearningPlayingModeUnlocked
                    ? "Playing mode: unlocked"
                    : $"Playing mode unlocks at {FormatNumber(progression.MachineLearningPlayingModeFrameRequirement)} frames ({FormatNumber(progression.MachineLearningFrames)} so far)");
            }

            SetText(solverInfoStatsText, stats.ToString());
            SetText(solverInfoTuningText, BuildTuningSummary(solverId));

            int take = Math.Min(solverRuns.Length, 50);
            var values = new List<double>(take);
            for (int i = take - 1; i >= 0; i--)
            {
                values.Add(solverRuns[i].score);
            }

            DrawLineChart(solverInfoChartRoot, values, HexColor("#34D399"), "No score history for this solver yet");

            SetActive(solverInfoModal.gameObject, true);
        }

        private string BuildTuningSummary(SolverId solverId)
        {
            SolverTuningDefinition tuningDefinition = StackMergeSolverCatalog.GetTuningDefinition(solverId);
            if (!tuningDefinition.HasParameters)
            {
                return "<b>Tuning</b>\nThis solver has no tunable parameters.";
            }

            if (!progression.SolverTuningUnlocked)
            {
                return "<b>Tuning</b>\nLocked — unlock Solver Tuning in Upgrades.";
            }

            SolverTuningSettings tuning = progression.GetSolverTuning(solverId);
            var builder = new StringBuilder();
            builder.AppendLine("<b>Tuning</b>");
            for (int i = 0; i < tuningDefinition.Parameters.Length; i++)
            {
                SolverTuningParameterDefinition parameter = tuningDefinition.Parameters[i];
                int raw = tuning.GetSlotValue(i);
                bool useSegments = parameter.WholeNumbers && parameter.MaxValue > parameter.MinValue && (parameter.MaxValue - parameter.MinValue) <= 6;
                string value = useSegments ? FormatWholeParamValue(solverId, parameter.Id, raw) : parameter.FormatValue(raw);
                builder.AppendLine($"{parameter.DisplayName}: {value}");
            }

            return builder.ToString();
        }

        private void EnsureSolverInfoModal()
        {
            if (solverInfoModal != null || canvas == null)
            {
                return;
            }

            solverInfoModal = CreateRuntimePanel("Solver Info Modal", canvas.transform, HexColor("#020617", 0.8f));
            Stretch(solverInfoModal, 0f, 0f, 0f, 0f);
            Button backdrop = solverInfoModal.gameObject.AddComponent<Button>();
            backdrop.transition = Selectable.Transition.None;
            backdrop.targetGraphic = solverInfoModal.GetComponent<Image>();
            backdrop.onClick.AddListener(HideSolverInfoModal);

            RectTransform card = CreateRuntimePanel("Card", solverInfoModal, HexColor("#111A2E", 1f));
            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.anchoredPosition = Vector2.zero;
            card.sizeDelta = new Vector2(460f, 700f);
            Image cardImage = card.GetComponent<Image>();
            if (cardImage != null)
            {
                cardImage.sprite = GetRoundedSprite(Color.white, Color.white, 28);
                cardImage.type = Image.Type.Sliced;
                cardImage.color = HexColor("#111A2E");
            }

            solverInfoTitle = CreateCardChildText("Title", card, "Solver", 26, new Vector2(0f, 308f), new Vector2(420f, 40f), HexColor("#F8FAFC"));

            solverInfoStatsText = CreateInfoBlockText("Stats", card, new Vector2(0f, 138f), new Vector2(420f, 280f), 17);
            solverInfoTuningText = CreateInfoBlockText("Tuning", card, new Vector2(0f, -78f), new Vector2(420f, 148f), 16);

            RectTransform chartHolder = CreateRuntimePanel("Chart", card, HexColor("#0B1322", 1f));
            chartHolder.anchorMin = new Vector2(0.5f, 0.5f);
            chartHolder.anchorMax = new Vector2(0.5f, 0.5f);
            chartHolder.pivot = new Vector2(0.5f, 0.5f);
            chartHolder.anchoredPosition = new Vector2(0f, -232f);
            chartHolder.sizeDelta = new Vector2(420f, 180f);
            Image chartImage = chartHolder.GetComponent<Image>();
            if (chartImage != null)
            {
                chartImage.sprite = GetRoundedSprite(Color.white, Color.white, 14);
                chartImage.type = Image.Type.Sliced;
                chartImage.color = HexColor("#0B1322");
            }

            solverInfoChartRoot = chartHolder;

            Button close = CreateRuntimeButton("Close", card, "Close", HexColor("#334155"), new Vector2(0f, -322f), new Vector2(200f, 44f));
            close.onClick.AddListener(HideSolverInfoModal);

            solverInfoModal.gameObject.SetActive(false);
        }

        private TMP_Text CreateInfoBlockText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize)
        {
            TMP_Text text = CreateRuntimeText(name, parent, string.Empty, fontSize, FontStyles.Normal, TextAlignmentOptions.TopLeft, HexColor("#CBD5E1"));
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            text.richText = true;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private void HideSolverInfoModal()
        {
            if (solverInfoModal != null)
            {
                SetActive(solverInfoModal.gameObject, false);
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
            if (solverId < 0) return "Manual";
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

            int blockCount = gameState.NextBlocks.Count;
            float spacing = 16f;
            float availableWidth = Mathf.Max(360f, nextBlocksRoot.rect.width);
            float blockWidth = Mathf.Clamp((availableWidth - spacing * Mathf.Max(0, blockCount - 1)) / Mathf.Max(1, blockCount), 96f, 144f);
            float blockHeight = blockWidth >= 132f ? 78f : 70f;
            int fontSize = Mathf.RoundToInt(blockHeight * 0.44f);

            for (int i = 0; i < blockCount; i++)
            {
                RectTransform block = i < nextBlocksRoot.childCount
                    ? (RectTransform)nextBlocksRoot.GetChild(i)
                    : CreateBlockInstance(nextBlocksRoot);
                ConfigureBlock(block, gameState.NextBlocks[i], blockWidth, blockHeight, fontSize);
                LayoutElement layout = EnsureComponent<LayoutElement>(block.gameObject);
                layout.preferredWidth = blockWidth;
                layout.preferredHeight = blockHeight;
            }

            for (int i = nextBlocksRoot.childCount - 1; i >= blockCount; i--)
            {
                GameObject extra = nextBlocksRoot.GetChild(i).gameObject;
                if (extra.activeSelf)
                {
                    extra.SetActive(false);
                }
            }
        }

        private void RefreshColumns()
        {
            if (stackBlockLayers == null || stackButtons == null || gameState == null)
            {
                return;
            }

            int capacity = gameState.StackCapacity;
            if (capacity != lastRenderedCapacity)
            {
                lastRenderedCapacity = capacity;
                boardLayoutDirty = true;
            }

            // Canvas.ForceUpdateCanvases() is expensive; only run it (and re-anchor the board)
            // when the layout actually changed (capacity upgrade or screen resize), not on
            // every move. Between layout changes, layer.rect already holds valid dimensions.
            if (boardLayoutDirty)
            {
                ResizeBoardToCapacity();
                Canvas.ForceUpdateCanvases();
                boardLayoutDirty = false;
            }

            for (int stackIndex = 0; stackIndex < stackBlockLayers.Length; stackIndex++)
            {
                RectTransform layer = stackBlockLayers[stackIndex];
                if (layer == null)
                {
                    continue;
                }

                float layerWidth = Mathf.Max(128f, layer.rect.width);
                float layerHeight = Mathf.Max(420f, layer.rect.height);
                float padding = 12f;
                float spacing = 7f;
                float blockHeight = Mathf.Min(74f, (layerHeight - padding * 2f - spacing * (capacity - 1)) / capacity);
                float blockWidth = Mathf.Max(110f, layerWidth - padding * 2f);
                int fontSize = Mathf.RoundToInt(blockHeight * 0.44f);

                IReadOnlyList<int> stack = stackIndex < gameState.StackCount ? gameState.Stacks[stackIndex] : Array.Empty<int>();
                int count = stack.Count;

                // Slot outlines: render ghost placeholder behind blocks.
                EnsureSlotLayer(stackIndex, layer);
                RefreshSlotOutlines(stackIndex, capacity, blockWidth, blockHeight, padding, spacing, count);

                // Pool block objects: reuse the layer's existing children instead of
                // destroying and re-instantiating every block on every move.
                // Child 0 is reserved for the slot outline layer, so blocks start at child 1.
                int poolOffset = stackSlotLayers != null && stackIndex < stackSlotLayers.Length && stackSlotLayers[stackIndex] != null ? 1 : 0;
                for (int i = 0; i < count; i++)
                {
                    int childIdx = poolOffset + i;
                    RectTransform block = childIdx < layer.childCount
                        ? (RectTransform)layer.GetChild(childIdx)
                        : CreateBlockInstance(layer);
                    ConfigureBlock(block, stack[i], blockWidth, blockHeight, fontSize);
                    block.anchorMin = new Vector2(0.5f, 0f);
                    block.anchorMax = new Vector2(0.5f, 0f);
                    block.pivot = new Vector2(0.5f, 0f);

                    float finalY = padding + i * (blockHeight + spacing);
                    bool isTopBlock = i == count - 1;

                    // Block drop animation: slide the freshly placed top block from above.
                    if (stackIndex == blockDropStack && isTopBlock && blockDropTimer > 0f)
                    {
                        float t = Mathf.SmoothStep(0f, 1f, 1f - blockDropTimer / BlockDropDuration);
                        float startY = padding + capacity * (blockHeight + spacing);
                        finalY = Mathf.Lerp(startY, finalY, t);
                    }

                    block.anchoredPosition = new Vector2(0f, finalY);

                    // Merge pulse: bounce scale on the resulting top block.
                    if (stackIndex == mergePulseStack && isTopBlock && mergePulseTimer > 0f)
                    {
                        float pt = mergePulseTimer / MergePulseDuration;
                        float scale = 1f + 0.14f * Mathf.Sin(pt * Mathf.PI);
                        block.localScale = new Vector3(scale, scale, 1f);
                    }
                    else
                    {
                        block.localScale = Vector3.one;
                    }
                }

                for (int i = layer.childCount - 1; i >= count + poolOffset; i--)
                {
                    GameObject extra = layer.GetChild(i).gameObject;
                    if (extra.activeSelf)
                    {
                        extra.SetActive(false);
                    }
                }

                if (stackIndex < stackButtons.Length && stackButtons[stackIndex] != null)
                {
                    stackButtons[stackIndex].interactable = gameState.CanPlace(stackIndex);
                }
            }
        }

        private void EnsureSlotLayer(int stackIndex, RectTransform layer)
        {
            if (stackSlotLayers == null || stackSlotLayers.Length != stackBlockLayers.Length)
            {
                stackSlotLayers = new RectTransform[stackBlockLayers.Length];
            }

            if (stackIndex >= stackSlotLayers.Length || stackSlotLayers[stackIndex] != null)
            {
                return;
            }

            GameObject go = new GameObject("SlotLayer");
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(layer, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.SetAsFirstSibling();
            stackSlotLayers[stackIndex] = rt;
        }

        private void RefreshSlotOutlines(int stackIndex, int capacity, float blockWidth, float blockHeight, float padding, float spacing, int filledCount)
        {
            if (stackSlotLayers == null || stackIndex >= stackSlotLayers.Length)
            {
                return;
            }

            RectTransform slotLayer = stackSlotLayers[stackIndex];
            if (slotLayer == null)
            {
                return;
            }

            while (slotLayer.childCount < capacity)
            {
                GameObject go = new GameObject("Slot");
                RectTransform slotRt = go.AddComponent<RectTransform>();
                slotRt.SetParent(slotLayer, false);
                UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
                img.raycastTarget = false;
                img.color = HexColor("#FFFFFF", 0.07f);
            }

            for (int s = 0; s < slotLayer.childCount; s++)
            {
                RectTransform slot = slotLayer.GetChild(s) as RectTransform;
                if (slot == null) continue;

                if (s < capacity)
                {
                    slot.gameObject.SetActive(true);
                    slot.anchorMin = new Vector2(0.5f, 0f);
                    slot.anchorMax = new Vector2(0.5f, 0f);
                    slot.pivot = new Vector2(0.5f, 0f);
                    slot.sizeDelta = new Vector2(blockWidth, blockHeight);
                    slot.anchoredPosition = new Vector2(0f, padding + s * (blockHeight + spacing));
                    UnityEngine.UI.Image img = slot.GetComponent<UnityEngine.UI.Image>();
                    if (img != null)
                    {
                        img.color = s < filledCount ? HexColor("#FFFFFF", 0.03f) : HexColor("#FFFFFF", 0.09f);
                    }
                }
                else
                {
                    slot.gameObject.SetActive(false);
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

            bool trainingActive = progression != null && progression.IsMachineLearningTrainingActive;
            gameOverOverlay.SetActive(gameState.IsGameOver && selectedTabIndex == 0 && !historyOpen && !achievementsOpen && !trainingActive);
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

        private RectTransform CreateBlockInstance(Transform parent)
        {
            return blockTemplate != null
                ? Instantiate(blockTemplate, parent)
                : CreateFallbackBlock(parent);
        }

        private void ConfigureBlock(RectTransform block, int value, float width, float height, int fontSize)
        {
            if (!block.gameObject.activeSelf)
            {
                block.gameObject.SetActive(true);
            }

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

        // Intentionally a no-op. Button background colours are owned by each Button's
        // Color Tint settings in the Inspector — the code only toggles `interactable`,
        // so Unity shows your normal / disabled colours automatically. Kept as a stub so
        // the many call sites don't need to be removed individually.
        private static void SetButtonColor(Button button, Color color)
        {
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

        private static void SetTopStretchRuntime(RectTransform rectTransform, float left, float top, float right, float height)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.offsetMin = new Vector2(left, -top - height);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static void SetTopRightRuntime(RectTransform rectTransform, float top, float right, float width, float height)
        {
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-right, -top);
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        private static void SetTopLeftRuntime(RectTransform rectTransform, Vector2 topLeft, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(topLeft.x, -topLeft.y);
            rectTransform.sizeDelta = size;
        }

        private static void SetBottomCenterRuntime(RectTransform rectTransform, float width, float height, float bottom)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, bottom);
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        private static void SetGridCellRuntime(RectTransform rectTransform, int column, int columns, int row, int rows, float spacing)
        {
            float columnWidth = 1f / Math.Max(1, columns);
            float rowHeight = 1f / Math.Max(1, rows);
            rectTransform.anchorMin = new Vector2(column * columnWidth, 1f - (row + 1) * rowHeight);
            rectTransform.anchorMax = new Vector2((column + 1) * columnWidth, 1f - row * rowHeight);
            rectTransform.offsetMin = new Vector2(spacing * 0.5f, spacing * 0.5f);
            rectTransform.offsetMax = new Vector2(-spacing * 0.5f, -spacing * 0.5f);
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

        // Modern, cohesive tile ramp keyed by the block's power-of-two tier: cool neutrals climb
        // through teal/blue/violet into warm amber/rose as the value grows, so higher tiles read
        // as "hotter" at a glance while staying readable.
        private static readonly string[] BlockPalette =
        {
            "#E2E8F0", // 1
            "#C7D2FE", // 2
            "#93C5FD", // 4
            "#5EEAD4", // 8
            "#6EE7B7", // 16
            "#FCD34D", // 32
            "#FB923C", // 64
            "#F87171", // 128
            "#F472B6", // 256
            "#C084FC", // 512
            "#818CF8", // 1024
            "#22D3EE", // 2048
            "#2DD4BF", // 4096
            "#A3E635", // 8192
            "#FACC15"  // 16384
        };

        private static Color GetBlockColor(int value)
        {
            if (value == StackMergeGameState.JokerBlockValue)
            {
                return HexColor("#FFFFFF");
            }

            int exponent = SolverScoring.FloorLog2(Math.Max(1, value));
            if (exponent < BlockPalette.Length)
            {
                return HexColor(BlockPalette[exponent]);
            }

            float t = Mathf.PingPong((exponent - BlockPalette.Length) * 0.22f, 1f);
            return Color.Lerp(HexColor("#FBBF24"), HexColor("#E879F9"), t);
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
