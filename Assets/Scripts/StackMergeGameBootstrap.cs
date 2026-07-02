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
        [Tooltip("Settings button in the Gameplay footer (replaces the old bottom-bar Settings tab). Opens the Settings panel.")]
        [SerializeField] private Button settingsButton;
        [Tooltip("Back button inside the Settings panel — returns to the Gameplay view.")]
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Button gameplayInfoButton;
        [SerializeField] private GameObject gameplayInfoOverlay;
        [SerializeField] private TMP_Text gameplayInfoText;
        [SerializeField] private Button gameplayInfoCloseButton;
        [Tooltip("Footer root (RectTransform) — used to compute where its top edge is, so Run Info can be centered above it.")]
        [SerializeField] private RectTransform footerRoot;
        [Tooltip("Run Info panel, now a sibling of Board/Footer instead of a child of Footer. Positioned automatically, vertically centered between the Board's bottom edge and the Footer's top edge.")]
        [SerializeField] private RectTransform runInfoPanel;

        [Header("Global Status Bar")]
        [Tooltip("Root of the single shared status bar (\"New Status Bar\"), living outside Tab Content. Shown on Gameplay/Algorithms/Upgrades/Modifiers/Agents/Research; hidden on History/Goals/Settings.")]
        [SerializeField] private GameObject globalStatusBarRoot;

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
        [SerializeField] private TMP_Text[] insightsTexts = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text tokensText;
        [SerializeField] private TMP_Text solverText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private TMP_Text queueText;
        [SerializeField] private TMP_Text runStatusText;
        [SerializeField] private TMP_Text agentSlotsText;
        [SerializeField] private Button autoSolveButton;
        [SerializeField] private Button[] solverButtons = Array.Empty<Button>();
        [Tooltip("Static per-solver \"AlgorithmItem\" cards already placed in the Algorithms menu (one per solver, not instantiated at runtime). Each drives its own Buy/Select/Deselect and Tune button.")]
        [SerializeField] private StackMergeAlgorithmCard[] algorithmCards = Array.Empty<StackMergeAlgorithmCard>();
        [SerializeField] private TMP_Text solverDetailNameText;
        [SerializeField] private TMP_Text solverDetailInfoText;
        [SerializeField] private TMP_Text solverDetailStatusText;
        [SerializeField] private Button solverDetailActionButton;
        [SerializeField] private Button solverDetailTuneButton;
        [SerializeField] private GameObject solverTunePanel;
        [SerializeField] private TMP_Text solverTuneTitleText;
        [SerializeField] private TMP_Text solverTuneSummaryText;
        [Tooltip("Container that the tuning parameter rows are instantiated into (one row per parameter).")]
        [SerializeField] private RectTransform solverTuneRowsRoot;
        [Tooltip("Prefab for a slider parameter. Needs a StackMergeTuneSliderRow component on its root.")]
        [SerializeField] private StackMergeTuneSliderRow tuneSliderRowPrefab;
        [Tooltip("Prefab for a button parameter (small whole-number values). Needs a StackMergeTuneButtonRow component on its root.")]
        [SerializeField] private StackMergeTuneButtonRow tuneButtonRowPrefab;
        // Legacy pre-built rows (no longer used — parameters are instantiated from prefabs now).
        // Kept so the editor scene-builder reference wiring still compiles; hidden at runtime.
        [SerializeField] private GameObject[] solverTuneRows = Array.Empty<GameObject>();
        [SerializeField] private TMP_Text[] solverTuneNameTexts = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] solverTuneValueTexts = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] solverTuneDescriptionTexts = Array.Empty<TMP_Text>();
        [SerializeField] private Slider[] solverTuneSliders = Array.Empty<Slider>();
        [SerializeField] private Button solverTuneBackButton;
        [SerializeField] private Button solverTuneResetButton;
        [Tooltip("Single Solver Speed upgrade button (replaces the old one-button-per-level array). Needs a StackMergeButtonLabelPair component for its Name/Cost texts.")]
        [SerializeField] private Button speedUpgradeButton;
        [SerializeField] private Button autoRestartButton;
        [SerializeField] private Button tokenPackButton;
        [SerializeField] private Button solverTuningUnlockButton;
        [SerializeField] private Button extraAgentSlotUpgradeButton;
        [Tooltip("Single Stack Capacity upgrade button. Needs a StackMergeButtonLabelPair component.")]
        [SerializeField] private Button stackCapacityUpgradeButton;
        [Tooltip("Single Next Preview upgrade button. Needs a StackMergeButtonLabelPair component.")]
        [SerializeField] private Button queuePreviewUpgradeButton;
        [Tooltip("Single Chip Yield upgrade button. Needs a StackMergeButtonLabelPair component.")]
        [SerializeField] private Button incomeUpgradeButton;
        [Tooltip("Single Difficulty Scaling upgrade button. Needs a StackMergeButtonLabelPair component.")]
        [SerializeField] private Button difficultyUpgradeButton;
        [SerializeField] private TMP_Text progressionStageText;
        [SerializeField] private Button modifiersMenuUnlockButton;
        [SerializeField] private Button agentsMenuUnlockButton;
        [SerializeField] private TMP_Text prestigeSummaryText;
        [SerializeField] private Button prestigeButton;
        // Legacy pre-redesign tree (dynamic node buttons + drawn connector arrows) — no longer
        // used now that the tree is a static grid of StackMergeResearchCard nodes.
        [SerializeField] private Button[] researchButtons = Array.Empty<Button>();
        [SerializeField] private Image[] researchConnectorImages = Array.Empty<Image>();
        [Tooltip("Static per-research nodes already placed in the Research tree grid (one per research, not instantiated at runtime). Clicking a node opens the Selected Research popup below.")]
        [SerializeField] private StackMergeResearchCard[] researchCards = Array.Empty<StackMergeResearchCard>();
        [Tooltip("Selected Research popup root — hidden by default, shown when a tree node is clicked.")]
        [SerializeField] private GameObject researchDetailModal;
        [SerializeField] private Button researchDetailCloseButton;
        [Tooltip("Optional. A Button covering the dimmed area behind the popup card (NOT the card itself) — clicking it closes the popup, same as the close button.")]
        [SerializeField] private Button researchDetailBackdropButton;
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
        [Tooltip("Static per-modifier cards already placed in the Modifiers menu (one per modifier, not instantiated at runtime).")]
        [SerializeField] private StackMergeModifierCard[] modifierCards = Array.Empty<StackMergeModifierCard>();
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
        // Legacy pre-redesign slot texts (no longer used — slots are StackMergeAgentSlot cards now).
        [SerializeField] private TMP_Text[] agentSlotTexts = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text agentDetailNameText;
        [SerializeField] private TMP_Text agentDetailInfoText;
        [SerializeField] private TMP_Text agentDetailStatusText;
        [SerializeField] private Button agentDetailActionButton;
        [Tooltip("Static per-agent cards already placed in the Agents menu (one per agent, not instantiated at runtime).")]
        [SerializeField] private StackMergeAgentCard[] agentCards = Array.Empty<StackMergeAgentCard>();
        [Tooltip("Static equipped-agent slot displays already placed in the Agents menu (one per slot, not instantiated at runtime).")]
        [SerializeField] private StackMergeAgentSlot[] agentSlotCards = Array.Empty<StackMergeAgentSlot>();

        [Header("Row Prefabs (assign in Inspector)")]
        [Tooltip("One row of the Goals list. Needs a StackMergeGoalRow component on its root.")]
        [SerializeField] private StackMergeGoalRow goalRowPrefab;
        [Tooltip("One row of History > Solvers. Needs a StackMergeSolverStatRow component on its root.")]
        [SerializeField] private StackMergeSolverStatRow solverStatRowPrefab;
        [Tooltip("One row of History > Recent Runs. Needs a StackMergeRecentRunRow component on its root.")]
        [SerializeField] private StackMergeRecentRunRow recentRunRowPrefab;

        [Header("Solver Info Modal (assign in Inspector)")]
        [Tooltip("Root overlay GameObject of the solver info modal — shown/hidden like the Gameplay Info Overlay.")]
        [SerializeField] private GameObject solverInfoOverlay;
        [SerializeField] private TMP_Text solverInfoTitle;
        [SerializeField] private TMP_Text solverInfoStatsText;
        [SerializeField] private TMP_Text solverInfoTuningText;
        [Tooltip("Optional. A RectTransform where the score-history line chart is drawn. Leave empty to skip the chart.")]
        [SerializeField] private RectTransform solverInfoChartRoot;
        [SerializeField] private Button solverInfoCloseButton;

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
        // Captured once from the panel's own designed height, then reused every reposition so the
        // panel doesn't grow/shrink as it gets moved around.
        private float runInfoDesignHeight = -1f;
        // Tuning rows are built once per solver and then updated in place; rebuilding every
        // refresh would destroy/re-instantiate the prefabs each auto-move.
        private SolverId tuneRowsBuiltForSolver = (SolverId)(-999);
        private readonly List<TuneRowBinding> tuneRowBindings = new();
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
        private long currentRunChipsEarned;
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
            Application.targetFrameRate = 120;

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
            MaybeEndStuckRun();
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
            // These upgrades collapsed from one-button-per-level to a single dynamic button; the
            // scene builder still hands us an array, so just take its first slot.
            speedUpgradeButton = speedButtons is { Length: > 0 } ? speedButtons[0] : null;
            autoRestartButton = restartButton;
            tokenPackButton = buyTokensButton;
            solverTuningUnlockButton = unlockSolverTuningButton;
            extraAgentSlotUpgradeButton = unlockExtraAgentSlotButton;
            stackCapacityUpgradeButton = capacityButtons is { Length: > 0 } ? capacityButtons[0] : null;
            queuePreviewUpgradeButton = queueButtons is { Length: > 0 } ? queueButtons[0] : null;
            incomeUpgradeButton = incomeButtons is { Length: > 0 } ? incomeButtons[0] : null;
            difficultyUpgradeButton = difficultyButtons is { Length: > 0 } ? difficultyButtons[0] : null;
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

            if (solverInfoCloseButton != null)
            {
                solverInfoCloseButton.onClick.RemoveAllListeners();
                solverInfoCloseButton.onClick.AddListener(HideSolverInfoModal);
            }

            if (researchDetailCloseButton != null)
            {
                researchDetailCloseButton.onClick.RemoveAllListeners();
                researchDetailCloseButton.onClick.AddListener(CloseResearchDetail);
            }

            if (researchDetailBackdropButton != null)
            {
                researchDetailBackdropButton.onClick.RemoveAllListeners();
                researchDetailBackdropButton.onClick.AddListener(CloseResearchDetail);
            }

            if (researchCards == null || researchCards.Length == 0)
            {
                Debug.LogWarning("StackMerge: Research Cards array is empty on the Bootstrap — no research node click will open the popup. Drag every StackMergeResearchCard tree node into it.");
            }

            int wiredResearchCards = 0;
            foreach (StackMergeResearchCard card in researchCards)
            {
                if (card == null)
                {
                    Debug.LogWarning("StackMerge: Research Cards array has an empty (None) slot.");
                    continue;
                }

                if (card.button == null)
                {
                    Debug.LogWarning($"StackMerge: Research card '{card.name}' (researchId={card.researchId}) has no Button assigned — its click won't open the popup.");
                    continue;
                }

                ResearchId cardResearchId = card.researchId;
                card.button.onClick.RemoveAllListeners();
                card.button.onClick.AddListener(() => OpenResearchDetail(cardResearchId));
                wiredResearchCards++;
            }

            if (researchDetailModal == null)
            {
                Debug.LogWarning("StackMerge: Research Detail Modal is not assigned on the Bootstrap — OpenResearchDetail has nothing to show.");
            }

            Debug.Log($"StackMerge: wired {wiredResearchCards}/{(researchCards?.Length ?? 0)} research cards. researchDetailModal={(researchDetailModal != null ? researchDetailModal.name : "None")}");

            // Start hidden, mirroring the Gameplay Info Overlay / Solver Info Modal.
            SetActive(solverInfoOverlay, false);
            SetActive(researchDetailModal, false);

            if (achievementBackButton != null)
            {
                achievementBackButton.onClick.RemoveAllListeners();
                achievementBackButton.onClick.AddListener(CloseAchievementsPanel);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveAllListeners();
                settingsButton.onClick.AddListener(OpenSettingsPanel);
            }

            if (settingsBackButton != null)
            {
                settingsBackButton.onClick.RemoveAllListeners();
                settingsBackButton.onClick.AddListener(CloseSettingsPanel);
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

            foreach (StackMergeAlgorithmCard card in algorithmCards)
            {
                if (card == null)
                {
                    continue;
                }

                SolverId cardSolverId = card.solverId;

                if (card.actionButton != null)
                {
                    card.actionButton.onClick.RemoveAllListeners();
                    card.actionButton.onClick.AddListener(() => HandleAlgorithmCardAction(cardSolverId));
                }

                if (card.tuneButton != null)
                {
                    card.tuneButton.onClick.RemoveAllListeners();
                    card.tuneButton.onClick.AddListener(() =>
                    {
                        SelectSolver(cardSolverId);
                        OpenSolverTunePanel();
                    });
                }
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

            foreach (StackMergeAgentCard card in agentCards)
            {
                if (card == null || card.button == null)
                {
                    continue;
                }

                AgentId cardAgentId = card.agentId;
                card.button.onClick.RemoveAllListeners();
                card.button.onClick.AddListener(() => HandleAgentCardAction(cardAgentId));
            }

            foreach (StackMergeModifierCard card in modifierCards)
            {
                if (card == null || card.button == null)
                {
                    continue;
                }

                ModifierId cardModifierId = card.modifierId;
                card.button.onClick.RemoveAllListeners();
                card.button.onClick.AddListener(() => BuyModifierUpgrade(cardModifierId));
            }

            if (speedUpgradeButton != null)
            {
                speedUpgradeButton.onClick.RemoveAllListeners();
                speedUpgradeButton.onClick.AddListener(BuySpeedUpgrade);
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

            if (stackCapacityUpgradeButton != null)
            {
                stackCapacityUpgradeButton.onClick.RemoveAllListeners();
                stackCapacityUpgradeButton.onClick.AddListener(BuyStackCapacityUpgrade);
            }

            if (queuePreviewUpgradeButton != null)
            {
                queuePreviewUpgradeButton.onClick.RemoveAllListeners();
                queuePreviewUpgradeButton.onClick.AddListener(BuyQueuePreviewUpgrade);
            }

            if (incomeUpgradeButton != null)
            {
                incomeUpgradeButton.onClick.RemoveAllListeners();
                incomeUpgradeButton.onClick.AddListener(BuyIncomeUpgrade);
            }

            if (difficultyUpgradeButton != null)
            {
                difficultyUpgradeButton.onClick.RemoveAllListeners();
                difficultyUpgradeButton.onClick.AddListener(BuyDifficultyUpgrade);
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

            // Slider rows are instantiated dynamically; their onValueChanged is wired in
            // RefreshSolverTunePanel when each row is created.

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
            SetActive(researchDetailModal, false);
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
            SetActive(researchDetailModal, false);
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
            SetActive(researchDetailModal, false);
            RefreshAchievements();
            RefreshTabButtons();
            RefreshGameView();
        }

        private void CloseAchievementsPanel()
        {
            SelectTab(0);
        }

        // Settings is reached from the Gameplay footer (not the bottom bar). It still lives at
        // tab index 6 internally, so SelectTab handles showing/hiding the panel.
        private void OpenSettingsPanel()
        {
            SelectTab(6);
        }

        private void CloseSettingsPanel()
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

        // The Research tab unlocks as soon as PPO is bought — actually being able to prestige is a
        // separate, later gate (Training Mode's frame requirement, via PrestigeAvailable) that the
        // prestige button/summary text enforces on its own.
        private bool IsResearchMenuUnlocked()
        {
            return progression != null && progression.IsSolverUnlocked(SolverId.MachineLearning);
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
            string[] labels = { "Game", "Algos", "Upgrades", "Modifiers", "Agents", "Research" };
            for (int i = 0; i < tabButtons.Length; i++)
            {
                Button button = tabButtons[i];
                if (button == null)
                {
                    continue;
                }

                // Settings is no longer a bottom-bar tab — it opens from the Gameplay footer.
                // Hide any leftover 7th (or later) tab button so it doesn't show on the bar.
                if (i >= 6)
                {
                    SetActive(button.gameObject, false);
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
                    label.text = lockedModifierTab ? "Modifiers"
                        : lockedAgentTab ? "Agents"
                        : lockedResearchTab ? "Research"
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
            else if (!gameState.IsGameOver && !gameState.HasLegalMove())
            {
                // Solver produced no move and there is no legal placement: it's stuck (it would
                // have returned a pickaxe / queue-skip move if it wanted one). End the run.
                EndStuckRun();
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
            currentRunChipsEarned += chipsGained;
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
                runBonus = CompleteRun(machineLearningTraining);
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
            SetText(feedbackText, $"{moveText}\n{chipText}{learningText}\n{resultReason}");

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

        // Awards the run-completion rewards (chips, history, ML run) for the current game state.
        // Shared by the normal end-of-run path and the stuck-run path.
        private long CompleteRun(bool machineLearningTraining)
        {
            bool manualRun = currentRunManualMoves > 0 && !currentRunUsedAutoSolve;
            SolverId histSolverId = manualRun ? (SolverId)(-1) : progression.SelectedSolver;
            long runBonus = progression.AwardRunCompleted(
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

            currentRunChipsEarned += runBonus;
            return runBonus;
        }

        // Manual play has no pickaxe / queue-skip UI, so HasAvailableAction() can stay true (those
        // tools are "available") while the player has no legal placement — a softlock. Detect that
        // and end the run. The auto-solver handles its own stuck case in TickAutomation.
        private void MaybeEndStuckRun()
        {
            if (gameState == null || progression == null || gameState.IsGameOver)
            {
                return;
            }

            if (progression.IsMachineLearningTrainingActive)
            {
                return;
            }

            // When the auto-solver is actively playing, let it use pickaxe / queue-skip; its tick
            // ends the run if it returns no move.
            bool autoSolverActive = progression.AutoSolveEnabled && !solverDeselected;
            if (autoSolverActive)
            {
                return;
            }

            if (gameState.HasLegalMove())
            {
                return;
            }

            EndStuckRun();
        }

        private void EndStuckRun()
        {
            bool machineLearningTraining = progression.IsMachineLearningTrainingActive;
            gameState.ForceGameOver();
            CompleteRun(machineLearningTraining);
            progression.Save();
            UpdateHighScore();
            SetText(feedbackText, "No moves left — run over");
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
            currentRunChipsEarned = 0L;
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
            HandleAlgorithmCardAction(selectedSolverId);
        }

        // Buy / Select / Deselect for a specific solver — used both by the legacy shared detail
        // panel (via selectedSolverId) and directly by each static AlgorithmItem card's own button.
        private void HandleAlgorithmCardAction(SolverId id)
        {
            if (progression == null)
            {
                return;
            }

            SolverDefinition definition = StackMergeSolverCatalog.GetDefinition(id);

            // PPO: unlock if needed, then let the player pick Training / Normal mode in a popup.
            if (id == SolverId.MachineLearning)
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
            if (progression.SelectedSolver == id && progression.IsSolverUnlocked(id))
            {
                solverDeselected = !solverDeselected;
                SetText(feedbackText, solverDeselected ? "Manual mode: solver paused" : $"Solver: {definition.DisplayName}");
                RefreshEverything();
                return;
            }

            bool changed = progression.SelectOrUnlockSolver(id);
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

        // The Research tree, category layout, and Selected Research popup are now entirely
        // hand-built in the Hierarchy (StackMergeResearchCard grid + researchDetailModal). This
        // used to also rebuild the whole panel at runtime whenever a guard condition on legacy
        // fields (prestigeButton, researchButtons, researchDetailActionButton, etc.) wasn't
        // exactly met — which was fragile: any one unassigned legacy field silently reassigned
        // researchPanel/researchDetailNameText/StatusText/InfoText/ActionButton to freshly-created
        // orphan objects, breaking the hand-wired popup. That runtime rebuild is removed for good;
        // only the housekeeping calls that are still meaningful remain.
        private void EnsureResearchUi()
        {
            HideLegacyPrestigeResearchSection();
            EnsureResearchTabButton();
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
            // A 6+ tab bar already contains Research at index 5, so nothing to inject. (Guard is
            // >= 6, not >= 7, because Settings is no longer a bottom-bar tab.)
            if (tabButtons == null || tabButtons.Length >= 6 || tabButtons.Length == 0)
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

        private void BuySpeedUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuySpeedUpgrade();
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

        // Clicking a tree node opens the Selected Research popup instead of buying directly —
        // the actual purchase happens via researchDetailActionButton inside the popup.
        private void OpenResearchDetail(ResearchId researchId)
        {
            Debug.Log($"StackMerge: OpenResearchDetail({researchId}) called.");

            SelectResearchUpgrade(researchId);
            SetActive(researchDetailModal, true);

            // The modal uses a Vertical Layout Group + Content Size Fitter, which can render at
            // zero size on the very first frame a GameObject is activated (same class of issue as
            // the currency pills). Force an immediate rebuild so it's correctly sized right away.
            if (researchDetailModal != null)
            {
                RectTransform modalRect = researchDetailModal.transform as RectTransform;
                if (modalRect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(modalRect);
                }

                Debug.Log($"StackMerge: researchDetailModal.activeSelf={researchDetailModal.activeSelf} activeInHierarchy={researchDetailModal.activeInHierarchy}");
            }
            else
            {
                Debug.LogWarning("StackMerge: researchDetailModal is null in OpenResearchDetail — nothing to show.");
            }
        }

        private void CloseResearchDetail()
        {
            SetActive(researchDetailModal, false);
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

        private void BuyStackCapacityUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyStackCapacityUpgrade();
            SetText(feedbackText, bought ? $"Stack capacity: {progression.StackCapacity}" : "Stack upgrade unavailable");
            if (bought)
            {
                ApplyCurrentBoardSettingsToGameState();
            }

            progression.Save();
            RefreshEverything();
        }

        private void BuyQueuePreviewUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyQueuePreviewUpgrade();
            SetText(feedbackText, bought ? $"Next preview: {progression.QueueLength} blocks" : "Next preview upgrade unavailable");
            if (bought)
            {
                ApplyCurrentBoardSettingsToGameState();
            }

            progression.Save();
            RefreshEverything();
        }

        private void BuyIncomeUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyIncomeUpgrade();
            SetText(feedbackText, bought ? $"Chip yield level {progression.IncomeLevel}" : "Income upgrade unavailable");
            progression.Save();
            RefreshEverything();
        }

        private void BuyDifficultyUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyDifficultyUpgrade();
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
            HandleAgentCardAction(selectedAgentId);
        }

        // Buy / Equip (Select) / Unequip (Deselect) for a specific agent — used both by the legacy
        // shared detail panel (via selectedAgentId) and directly by each static Agent card's button.
        private void HandleAgentCardAction(AgentId id)
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

            AgentDefinition definition = progression.GetAgentDefinition(id);
            bool changed;
            if (!progression.IsAgentUnlocked(id))
            {
                changed = progression.BuyAgent(id);
                SetText(feedbackText, changed ? $"Agent bought: {definition.DisplayName}" : "Not enough chips");
            }
            else if (progression.IsAgentActive(id))
            {
                changed = progression.UnequipAgent(id);
                SetText(feedbackText, changed ? $"Agent unequipped: {definition.DisplayName}" : "Agent unavailable");
            }
            else
            {
                changed = progression.EquipAgent(id);
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
            SetText(droppedText, $"{FormatNumber(gameState.BlocksDropped)} moves");

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
            // Global Status Bar lives outside Tab Content (a sibling, not a per-panel child), so its
            // visibility has to be driven explicitly here instead of by the per-tab SetActive calls
            // in SelectTab/OpenHistoryPanel/OpenAchievementsPanel. RefreshHud already runs after
            // every one of those, so this one spot covers all navigation paths.
            bool showGlobalStatusBar = !historyOpen && !achievementsOpen && selectedTabIndex != 6;
            SetActive(globalStatusBarRoot, showGlobalStatusBar);

            if (progression == null)
            {
                return;
            }

            bool machineLearningTraining = progression.IsMachineLearningTrainingActive;
            SetText(chipsTexts, $"<sprite name=\"chips\"> {FormatNumber(progression.Chips)}");
            SetText(insightsTexts, $"<sprite name=\"insight\"> {FormatNumber(progression.ResearchInsight)}");
            SetText(tokensText, $"<sprite name=\"token\"> {FormatNumber(progression.Tokens)}");

            // TMP reports a stale preferred width on the first frame (its mesh isn't generated yet),
            // so ContentSizeFitter + Horizontal Layout Group pills mis-size until something forces a
            // relayout. Force it here so the currency pills are correct immediately on start.
            RebuildCurrencyLayout(chipsTexts);
            RebuildCurrencyLayout(insightsTexts);
            RebuildTextLayout(tokensText);
            if (solverDeselected)
            {
                SetText(solverText, $"Manual  ({GetSelectedSolver().DisplayName} paused)");
                SetText(solverText, $"Manual  ({GetSelectedSolver().DisplayName} paused)");
            }
            else if (progression.SelectedSolver == SolverId.MachineLearning)
            {
                SetText(solverText, $"{GetSelectedSolver().DisplayName} Lv {progression.MachineLearningLevel}");
            }
            else
            {
                SetText(solverText, $"{GetSelectedSolver().DisplayName}");
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

        private static void RebuildCurrencyLayout(TMP_Text[] texts)
        {
            if (texts == null)
            {
                return;
            }

            foreach (TMP_Text text in texts)
            {
                RebuildTextLayout(text);
            }
        }

        // Forces TMP to (re)generate its mesh and rebuilds the outermost Layout Group ancestor so
        // ContentSizeFitter pills size to their content immediately, instead of after a stray click.
        private static void RebuildTextLayout(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.ForceMeshUpdate();

            RectTransform target = null;
            Transform t = text.transform;
            while (t != null)
            {
                if (t.GetComponent<LayoutGroup>() != null)
                {
                    target = t as RectTransform;
                }

                t = t.parent;
            }

            if (target != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(target);
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
            RefreshAlgorithmCards();
            if (solverTuneOpen)
            {
                RefreshSolverTunePanel();
            }
            RefreshAgentButtons();
            RefreshAgentSlots();
            RefreshAgentDetails();
            RefreshAgentCards();
            RefreshAgentSlotCards();
            RefreshModifierButtons();
            RefreshModifierDetails();
            RefreshModifierCards();
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
                    label += $"\n<sprite name=\"chips\"> {FormatNumber(definition.Cost)}";
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
                ? $"{progression.GetMachineLearningGateStatus()}\n<sprite name=\"chips\"> {FormatNumber(definition.Cost)}"
                : $"Unlock this algorithm to reveal details.";
            // PPO runtime statistics are intentionally not shown here — they live in the History menu.
            SetText(solverDetailInfoText, unlocked ? definition.Description : lockedInfo);
            SetButtonText(solverDetailTuneButton,
                !unlocked ? "Tune\nLocked" : !progression.SolverTuningUnlocked ? "Tune\nUpgrade" : canTune ? "Tune" : "No tuning");
            if (solverDetailTuneButton != null)
            {
                solverDetailTuneButton.interactable = canTune;
            }

            if (!unlocked)
            {
                SetText(solverDetailStatusText, isMachineLearning && !machineLearningGateReady ? "Stage locked" : "Locked");
                SetButtonText(solverDetailActionButton, isMachineLearning && !machineLearningGateReady ? "Needs all\nModifiers" : $"Unlock\n<sprite name=\"chips_white\"> {FormatNumber(definition.Cost)}");
                if (solverDetailActionButton != null)
                {
                    solverDetailActionButton.interactable = machineLearningGateReady && progression.Chips >= definition.Cost;
                }
                return;
            }

            string statusLabel = active
                ? (!isMachineLearning && solverDeselected ? "Paused — manual mode" : "Active")
                : "Unlocked";
            SetText(solverDetailStatusText, statusLabel);
            if (!isMachineLearning && active)
            {
                SetButtonText(solverDetailActionButton, solverDeselected ? "Select" : "Deselect");
                if (solverDetailActionButton != null) solverDetailActionButton.interactable = true;
            }
            else
            {
                SetButtonText(solverDetailActionButton, active ? "Selected" : "Select");
                if (solverDetailActionButton != null) solverDetailActionButton.interactable = !active;
            }
        }

        // Drives every static AlgorithmItem card (Name/Description/action button/Tune button)
        // straight from progression state. Cards are never instantiated — one already exists per
        // solver in the Hierarchy, so this just updates them in place.
        private void RefreshAlgorithmCards()
        {
            if (progression == null || algorithmCards == null)
            {
                return;
            }

            foreach (StackMergeAlgorithmCard card in algorithmCards)
            {
                if (card == null)
                {
                    continue;
                }

                SolverDefinition definition = StackMergeSolverCatalog.GetDefinition(card.solverId);
                bool unlocked = progression.IsSolverUnlocked(card.solverId);
                bool active = progression.SelectedSolver == card.solverId;
                bool isMachineLearning = card.solverId == SolverId.MachineLearning;
                bool machineLearningGateReady = !isMachineLearning || progression.CanUnlockMachineLearning;

                SetText(card.nameText, definition.DisplayName);
                SetText(card.descriptionText, definition.Description);

                if (!unlocked)
                {
                    SetButtonText(card.actionButton, isMachineLearning && !machineLearningGateReady
                        ? "Needs all\nModifiers"
                        : $"Buy\n<sprite name=\"chips_white\"> {FormatNumber(definition.Cost)}");
                    if (card.actionButton != null)
                    {
                        card.actionButton.interactable = machineLearningGateReady && progression.Chips >= definition.Cost;
                    }
                }
                else if (!isMachineLearning && active)
                {
                    SetButtonText(card.actionButton, solverDeselected ? "Resume solver" : "Deselect");
                    if (card.actionButton != null) card.actionButton.interactable = true;
                }
                else
                {
                    SetButtonText(card.actionButton, active ? "Selected" : "Select");
                    if (card.actionButton != null) card.actionButton.interactable = !active;
                }

                if (card.tuneButton != null)
                {
                    SolverTuningDefinition tuningDefinition = StackMergeSolverCatalog.GetTuningDefinition(card.solverId);
                    // RAND (and any other parameterless solver) has nothing to tune — hide the button entirely.
                    card.tuneButton.gameObject.SetActive(tuningDefinition.HasParameters);
                    card.tuneButton.interactable = unlocked && progression.SolverTuningUnlocked;
                }
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

            HideLegacyTuneRows();

            // Build the prefab rows once per solver, then only update their values. Rebuilding on
            // every refresh would destroy and re-instantiate the prefabs each auto-move, which made
            // the rows overlap (Destroy is deferred to end-of-frame) and dropped editor selection.
            if (TuneRowsNeedRebuild(tuningDefinition))
            {
                RebuildTuneRows(tuningDefinition, tuning);
            }
            else
            {
                UpdateTuneRowValues(tuningDefinition, tuning);
            }

            if (solverTuneResetButton != null)
            {
                solverTuneResetButton.interactable = !tuning.IsNeutral;
            }
        }

        private bool TuneRowsNeedRebuild(SolverTuningDefinition tuningDefinition)
        {
            if (solverTuneRowsRoot == null)
            {
                return false;
            }

            if (tuneRowsBuiltForSolver != selectedSolverId)
            {
                return true;
            }

            if (tuneRowBindings.Count != tuningDefinition.Parameters.Length)
            {
                return true;
            }

            foreach (TuneRowBinding binding in tuneRowBindings)
            {
                if (binding == null || !binding.IsAlive)
                {
                    return true;
                }
            }

            return false;
        }

        // Instantiates one row prefab per tuning parameter into solverTuneRowsRoot: a slider
        // prefab for continuous parameters, a button prefab (one button per value) for the small
        // whole-number ones. Caches each row in tuneRowBindings for later in-place updates.
        private void RebuildTuneRows(SolverTuningDefinition tuningDefinition, SolverTuningSettings tuning)
        {
            RectTransform root = solverTuneRowsRoot;
            if (root == null)
            {
                return;
            }

            ClearInstantiatedRows<StackMergeTuneSliderRow>(root);
            ClearInstantiatedRows<StackMergeTuneButtonRow>(root);
            tuneRowBindings.Clear();
            tuneRowsBuiltForSolver = selectedSolverId;

            float y = 0f;
            for (int i = 0; i < tuningDefinition.Parameters.Length; i++)
            {
                SolverTuningParameterDefinition parameter = tuningDefinition.Parameters[i];
                int value = tuning.GetSlotValue(i);

                // Small whole-number parameters become a row of buttons (one per value) showing the
                // real resolved value, which is far clearer than a slider snapping to "-3".
                bool useSegments = parameter.WholeNumbers
                    && parameter.MaxValue > parameter.MinValue
                    && (parameter.MaxValue - parameter.MinValue) <= 6;

                if (useSegments)
                {
                    if (tuneButtonRowPrefab == null)
                    {
                        Debug.LogWarning("StackMerge: Tune button row prefab not assigned — assign it on the Bootstrap in the Inspector.");
                        continue;
                    }

                    float height = RowHeightOf((RectTransform)tuneButtonRowPrefab.transform, 92f);
                    StackMergeTuneButtonRow row = Instantiate(tuneButtonRowPrefab, root, false);
                    PositionRow((RectTransform)row.transform, y, height);
                    SetText(row.nameText, parameter.DisplayName);
                    SetText(row.descriptionText, parameter.Description);
                    SetText(row.valueText, FormatWholeParamValue(selectedSolverId, parameter.Id, value));
                    Button[] valueButtons = BuildTuneButtons(row, i, parameter, value);
                    tuneRowBindings.Add(new TuneRowBinding
                    {
                        Slot = i,
                        ButtonRow = row,
                        ValueButtons = valueButtons,
                        MinRaw = parameter.MinValue
                    });
                    y += height + 6f;
                }
                else
                {
                    if (tuneSliderRowPrefab == null)
                    {
                        Debug.LogWarning("StackMerge: Tune slider row prefab not assigned — assign it on the Bootstrap in the Inspector.");
                        continue;
                    }

                    float height = RowHeightOf((RectTransform)tuneSliderRowPrefab.transform, 92f);
                    StackMergeTuneSliderRow row = Instantiate(tuneSliderRowPrefab, root, false);
                    PositionRow((RectTransform)row.transform, y, height);
                    SetText(row.nameText, parameter.DisplayName);
                    SetText(row.descriptionText, parameter.Description);
                    SetText(row.valueText, parameter.FormatValue(value));

                    if (row.slider != null)
                    {
                        int slot = i;
                        row.slider.minValue = parameter.MinDisplayValue;
                        row.slider.maxValue = parameter.MaxDisplayValue;
                        row.slider.wholeNumbers = parameter.WholeNumbers;
                        row.slider.SetValueWithoutNotify(parameter.ToDisplayValue(value));
                        row.slider.onValueChanged.RemoveAllListeners();
                        row.slider.onValueChanged.AddListener(v => SetSelectedSolverTuningFromDisplay(slot, v));
                    }

                    tuneRowBindings.Add(new TuneRowBinding { Slot = i, SliderRow = row });
                    y += height + 6f;
                }
            }
        }

        // Refreshes values/labels on already-built rows without destroying anything. Safe to call
        // every frame (e.g. on each auto-move) and after a tuning change.
        private void UpdateTuneRowValues(SolverTuningDefinition tuningDefinition, SolverTuningSettings tuning)
        {
            foreach (TuneRowBinding binding in tuneRowBindings)
            {
                if (binding == null || binding.Slot < 0 || binding.Slot >= tuningDefinition.Parameters.Length)
                {
                    continue;
                }

                SolverTuningParameterDefinition parameter = tuningDefinition.Parameters[binding.Slot];
                int value = tuning.GetSlotValue(binding.Slot);

                if (binding.ButtonRow != null)
                {
                    SetText(binding.ButtonRow.valueText, FormatWholeParamValue(selectedSolverId, parameter.Id, value));
                    if (binding.ValueButtons != null)
                    {
                        for (int raw = parameter.MinValue; raw <= parameter.MaxValue; raw++)
                        {
                            int idx = raw - binding.MinRaw;
                            if (idx < 0 || idx >= binding.ValueButtons.Length)
                            {
                                continue;
                            }

                            Button button = binding.ValueButtons[idx];
                            if (button == null)
                            {
                                continue;
                            }

                            SetButtonText(button, ResolveWholeParamValue(selectedSolverId, parameter.Id, raw).ToString());
                            button.interactable = raw != value;
                        }
                    }
                }
                else if (binding.SliderRow != null)
                {
                    SetText(binding.SliderRow.valueText, parameter.FormatValue(value));
                    if (binding.SliderRow.slider != null)
                    {
                        binding.SliderRow.slider.SetValueWithoutNotify(parameter.ToDisplayValue(value));
                    }
                }
            }
        }

        // Clones the row prefab's single button once per selectable value and returns them in
        // value order. The active value's button is shown non-interactable so the Button's own
        // disabled colour marks it.
        private Button[] BuildTuneButtons(StackMergeTuneButtonRow row, int slotIndex, SolverTuningParameterDefinition parameter, int currentValue)
        {
            Button template = row.buttonTemplate;
            if (template == null)
            {
                return Array.Empty<Button>();
            }

            Transform parent = template.transform.parent;

            // The row is freshly instantiated, so the prefab's single button is the only child.
            // Make sure clones lay out side by side — but only add a layout group if the prefab's
            // button container doesn't already have one (respects whatever you set up).
            if (parent.GetComponent<LayoutGroup>() == null)
            {
                HorizontalLayoutGroup layout = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 4f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
                layout.childAlignment = TextAnchor.MiddleCenter;
            }

            int count = parameter.MaxValue - parameter.MinValue + 1;
            Button[] buttons = new Button[count];
            int index = 0;
            for (int raw = parameter.MinValue; raw <= parameter.MaxValue; raw++)
            {
                Button button = index == 0 ? template : Instantiate(template, parent, false);

                int captured = raw;
                bool selected = raw == currentValue;
                SetButtonText(button, ResolveWholeParamValue(selectedSolverId, parameter.Id, raw).ToString());
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SetSelectedSolverTuningRaw(slotIndex, captured));
                button.interactable = !selected;

                LayoutElement layoutElement = EnsureComponent<LayoutElement>(button.gameObject);
                layoutElement.flexibleWidth = 1f;
                layoutElement.minWidth = 24f;

                buttons[index] = button;
                index++;
            }

            return buttons;
        }

        // Caches one instantiated tuning row so it can be updated in place instead of rebuilt.
        private sealed class TuneRowBinding
        {
            public int Slot;
            public StackMergeTuneSliderRow SliderRow;
            public StackMergeTuneButtonRow ButtonRow;
            public Button[] ValueButtons;
            public int MinRaw;

            // Uses Unity's overloaded == so a destroyed row reads as not-alive.
            public bool IsAlive => SliderRow != null || ButtonRow != null;
        }

        // Defensively hides any legacy pre-built tuning rows still left in the scene.
        private void HideLegacyTuneRows()
        {
            if (solverTuneRows == null)
            {
                return;
            }

            foreach (GameObject row in solverTuneRows)
            {
                if (row != null)
                {
                    row.SetActive(false);
                }
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
                    label += $"\n<sprite name=\"chips_white\"> {FormatNumber(definition.Cost)}";
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
                    //text.color = HexColor("#64748B");
                    continue;
                }

                int activeAgentId = progression.GetActiveAgentIdAtSlot(i);
                if (activeAgentId >= 0)
                {
                    AgentDefinition definition = progression.GetAgentDefinition((AgentId)activeAgentId);
                    SetText(text, $"Slot {i + 1}\n{definition.DisplayName}");
                    //text.color = HexColor("#FFFFFF");
                }
                else if (i >= progression.ActiveAgentSlots)
                {
                    SetText(text, "Bonus slot\nNeeds upgrade");
                    //text.color = HexColor("#64748B");
                }
                else
                {
                    SetText(text, $"Slot {i + 1}\nEmpty");
                    //text.color = HexColor("#CBD5E1");
                }
            }
        }

        private void RefreshAgentDetails()
        {
            AgentDefinition definition = progression.GetAgentDefinition(selectedAgentId);
            if (!progression.AgentsMenuUnlocked)
            {
                SetText(agentDetailNameText, "Agents Locked");
                SetText(agentDetailInfoText, $"Unlock the Agents menu in Upgrades.");
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
            SetText(agentDetailInfoText, unlocked ? definition.Description : $"{definition.LockedHint}");

            if (!unlocked)
            {
                SetText(agentDetailStatusText, "Locked");
                SetButtonText(agentDetailActionButton, $"Buy\n<sprite name=\"chips_white\"> {FormatNumber(definition.Cost)}");
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

        // Drives every static Agent card (Name/Cost-InfoText/Button) straight from progression
        // state. Cards are never instantiated — one already exists per agent in the Hierarchy.
        private void RefreshAgentCards()
        {
            if (progression == null || agentCards == null)
            {
                return;
            }

            foreach (StackMergeAgentCard card in agentCards)
            {
                if (card == null)
                {
                    continue;
                }

                AgentDefinition definition = progression.GetAgentDefinition(card.agentId);
                SetText(card.nameText, definition.DisplayName);
                SetText(card.descriptionText, definition.Description);

                if (!progression.AgentsMenuUnlocked)
                {
                    SetButtonText(card.button, "Locked");
                    if (card.button != null) card.button.interactable = false;
                    continue;
                }

                bool unlocked = progression.IsAgentUnlocked(card.agentId);
                if (!unlocked)
                {
                    SetButtonText(card.button, $"<sprite name=\"chips_white\"> {FormatNumber(definition.Cost)}");
                    if (card.button != null) card.button.interactable = progression.Chips >= definition.Cost;
                    continue;
                }

                bool active = progression.IsAgentActive(card.agentId);
                if (active)
                {
                    SetButtonText(card.button, "Deselect");
                    if (card.button != null) card.button.interactable = true;
                }
                else
                {
                    bool hasFreeSlot = progression.ActiveAgentCount < progression.ActiveAgentSlots;
                    SetButtonText(card.button, "Select");
                    if (card.button != null) card.button.interactable = hasFreeSlot;
                }
            }
        }

        // Drives every static Agent Slot card (SlotText/NameText) from which agent (if any) is
        // equipped at that slot index.
        private void RefreshAgentSlotCards()
        {
            if (progression == null || agentSlotCards == null)
            {
                return;
            }

            foreach (StackMergeAgentSlot slot in agentSlotCards)
            {
                if (slot == null)
                {
                    continue;
                }

                SetText(slot.slotText, $"Slot {slot.slotIndex + 1}");

                if (!progression.AgentsMenuUnlocked)
                {
                    SetText(slot.nameText, "Locked");
                    continue;
                }

                int activeAgentId = progression.GetActiveAgentIdAtSlot(slot.slotIndex);
                if (activeAgentId >= 0)
                {
                    AgentDefinition definition = progression.GetAgentDefinition((AgentId)activeAgentId);
                    SetText(slot.nameText, definition.DisplayName);
                }
                else if (slot.slotIndex >= progression.ActiveAgentSlots)
                {
                    SetText(slot.nameText, "Needs upgrade");
                }
                else
                {
                    SetText(slot.nameText, "Empty");
                }
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

        // Drives every static Modifier card (Name/Cost-InfoText/Button) straight from progression
        // state. Cards are never instantiated — one already exists per modifier in the Hierarchy.
        private void RefreshModifierCards()
        {
            if (progression == null || modifierCards == null)
            {
                return;
            }

            foreach (StackMergeModifierCard card in modifierCards)
            {
                if (card == null)
                {
                    continue;
                }

                ModifierDefinition definition = progression.GetModifierDefinition(card.modifierId);
                SetText(card.nameText, definition.DisplayName);
                SetText(card.descriptionText, definition.Description);

                if (!progression.ModifiersMenuUnlocked)
                {
                    SetButtonText(card.button, "Locked");
                    if (card.button != null) card.button.interactable = false;
                    continue;
                }

                bool maxed = progression.IsModifierMaxed(card.modifierId);
                if (maxed)
                {
                    SetButtonText(card.button, "Maxed");
                    if (card.button != null) card.button.interactable = false;
                }
                else
                {
                    long cost = progression.GetModifierUpgradeCost(card.modifierId);
                    SetButtonText(card.button, $"<sprite name=\"chips\"> {FormatNumber(cost)}");
                    if (card.button != null) card.button.interactable = progression.Chips >= cost;
                }
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
                // PPO runtime statistics are intentionally not shown here — they live in the History menu.
                string nextGoal = progression.IsSolverUnlocked(SolverId.MachineLearning)
                    ? "Train PPO, then prestige from the Research menu."
                    : progression.ModifiersMenuUnlocked
                    ? progression.AllModifiersMaxed ? "PPO is ready to unlock in Algorithms." : "Max every Modifier to open the Machine Learning layer."
                    : progression.GetModifiersGateStatus();
                SetText(progressionStageText, $"{stageName}\n{nextGoal}");
            }

            if (modifiersMenuUnlockButton != null)
            {
                if (progression.ModifiersMenuUnlocked)
                {
                    SetUpgradeButtonLabels(modifiersMenuUnlockButton, "Modifier Lab", "Unlocked", false);
                }
                else
                {
                    long cost = progression.GetModifiersMenuUnlockCost();
                    bool gateReady = progression.CanUnlockModifiersMenu;
                    SetUpgradeButtonLabels(
                        modifiersMenuUnlockButton,
                        "Modifier Lab",
                        gateReady ? $"<sprite name=\"chips\"> {FormatNumber(cost)}" : "Stage locked",
                        gateReady && progression.Chips >= cost);
                }
            }

            // Solver Speed: collapsed from one button per level to a single button that always
            // targets the next level. Name shows the effect buying grants (not what you already
            // have — otherwise level 0 would misleadingly read "0% faster").
            if (speedUpgradeButton != null)
            {
                int level = progression.SpeedLevel;
                int maxLevel = progression.MaxSpeedLevel;
                if (progression.IsMaxSpeed)
                {
                    float percent = StackMergeProgression.GetSpeedUpgradeEffectPercent(level);
                    SetUpgradeButtonLabels(speedUpgradeButton, $"{percent:0}% faster ({level}/{maxLevel})", "Maxed", false);
                }
                else
                {
                    float nextPercent = StackMergeProgression.GetSpeedUpgradeEffectPercent(level + 1);
                    long cost = progression.GetSpeedUpgradeCost();
                    SetUpgradeButtonLabels(speedUpgradeButton, $"{nextPercent:0}% faster ({level}/{maxLevel})", $"<sprite name=\"chips\"> {FormatNumber(cost)}", progression.Chips >= cost);
                }
            }

            if (autoSolveButton != null)
            {
                if (progression.AutoSolveUnlocked)
                {
                    SetUpgradeButtonLabels(autoSolveButton, "Auto solve", progression.AutoSolveEnabled ? "ON" : "OFF", true);
                }
                else
                {
                    long cost = progression.GetAutoSolveCost();
                    SetUpgradeButtonLabels(
                        autoSolveButton,
                        "Auto solve",
                        progression.HasPurchasedSolver ? $"<sprite name=\"chips\"> {FormatNumber(cost)}" : "Needs algorithm",
                        progression.HasPurchasedSolver && progression.Chips >= cost);
                }
            }

            if (autoRestartButton != null)
            {
                if (progression.AutoRestartUnlocked)
                {
                    string tokenMode = progression.AutoRestartIsTokenFree ? "free" : $"{FormatNumber(progression.Tokens)} token";
                    SetUpgradeButtonLabels(autoRestartButton, "Auto restart", progression.AutoRestartEnabled ? $"ON ({tokenMode})" : "OFF", true);
                }
                else
                {
                    long cost = progression.GetAutoRestartCost();
                    SetUpgradeButtonLabels(
                        autoRestartButton,
                        "Auto restart",
                        progression.HasPurchasedSolver ? $"<sprite name=\"chips\"> {FormatNumber(cost)}" : "Needs algorithm",
                        progression.HasPurchasedSolver && progression.Chips >= cost);
                }
            }

            if (tokenPackButton != null)
            {
                long cost = progression.GetTokenPackCost();
                SetUpgradeButtonLabels(tokenPackButton, $"+{progression.GetTokenPackSize()} tokens", $"<sprite name=\"chips\"> {FormatNumber(cost)}", progression.Chips >= cost);
            }

            if (solverTuningUnlockButton != null)
            {
                if (progression.SolverTuningUnlocked)
                {
                    SetUpgradeButtonLabels(solverTuningUnlockButton, "Solver tuning", "Unlocked", false);
                }
                else
                {
                    long cost = progression.GetSolverTuningUnlockCost();
                    SetUpgradeButtonLabels(solverTuningUnlockButton, "Solver tuning", $"<sprite name=\"chips\"> {FormatNumber(cost)}", progression.Chips >= cost);
                }
            }

            if (extraAgentSlotUpgradeButton != null)
            {
                if (progression.ExtraAgentSlotUnlocked)
                {
                    SetUpgradeButtonLabels(extraAgentSlotUpgradeButton, "+1 Agent slot", "Unlocked", false);
                }
                else
                {
                    long cost = progression.GetExtraAgentSlotUpgradeCost();
                    SetUpgradeButtonLabels(extraAgentSlotUpgradeButton, "+1 Agent slot", $"<sprite name=\"chips\"> {FormatNumber(cost)}", progression.Chips >= cost);
                }
            }

            if (agentsMenuUnlockButton != null)
            {
                if (progression.AgentsMenuUnlocked)
                {
                    SetUpgradeButtonLabels(agentsMenuUnlockButton, "Agents", "Unlocked", false);
                }
                else
                {
                    long cost = progression.GetAgentsMenuUnlockCost();
                    SetUpgradeButtonLabels(agentsMenuUnlockButton, "Agents", $"<sprite name=\"chips\"> {FormatNumber(cost)}", progression.Chips >= cost);
                }
            }

            // Stack Capacity: single button, always targets the next level. Name shows the
            // capacity buying grants (not the current one — otherwise it'd repeat the same
            // number you already have instead of telling you what you're paying for).
            if (stackCapacityUpgradeButton != null)
            {
                int level = progression.StackCapacityLevel;
                int maxLevel = progression.MaxStackCapacityLevel;
                if (progression.IsMaxStackCapacity)
                {
                    SetUpgradeButtonLabels(stackCapacityUpgradeButton, $"{progression.StackCapacity} row ({level}/{maxLevel})", "Maxed", false);
                }
                else
                {
                    int nextCapacity = progression.StackCapacity + 1;
                    long cost = progression.GetStackCapacityUpgradeCost();
                    SetUpgradeButtonLabels(stackCapacityUpgradeButton, $"{nextCapacity} row ({level}/{maxLevel})", $"<sprite name=\"chips\"> {FormatNumber(cost)}", progression.Chips >= cost);
                }
            }

            // Next Preview: single button, always targets the next level. Name shows the queue
            // length buying grants.
            if (queuePreviewUpgradeButton != null)
            {
                int level = progression.QueuePreviewLevel;
                int maxLevel = progression.MaxQueuePreviewLevel;
                if (progression.IsMaxQueuePreview)
                {
                    SetUpgradeButtonLabels(queuePreviewUpgradeButton, $"+{level} block ({level}/{maxLevel})", "Maxed", false);
                }
                else
                {
                    long cost = progression.GetQueuePreviewUpgradeCost();
                    SetUpgradeButtonLabels(queuePreviewUpgradeButton, $"+{level + 1} block ({level}/{maxLevel})", $"<sprite name=\"chips\"> {FormatNumber(cost)}", progression.Chips >= cost);
                }
            }

            // Difficulty Scaling: single button, always targets the next level. Describes what the
            // upgrade actually does (raises the odds/ceiling of high-value block spawns) instead of
            // a bare "Risk L{level}" label that told the player nothing.
            if (difficultyUpgradeButton != null)
            {
                int level = progression.DifficultyLevel;
                int maxLevel = progression.MaxDifficultyLevel;
                string name = $"+odds ({level}/{maxLevel})";
                if (progression.IsMaxDifficulty)
                {
                    SetUpgradeButtonLabels(difficultyUpgradeButton, name, "Maxed", false);
                }
                else
                {
                    long cost = progression.GetDifficultyUpgradeCost();
                    SetUpgradeButtonLabels(difficultyUpgradeButton, name, $"<sprite name=\"chips\"> {FormatNumber(cost)}", progression.Chips >= cost);
                }
            }

            // Chip Yield: single button, always targets the next level. Name shows the yield bonus
            // buying grants (not what you already have — at level 0 that showed a misleading
            // "+0% yield"). +35% per level matches ApplyIncomeMultiplier's actual formula (the
            // previous per-level array showed +12%, stale relative to the real balance value).
            if (incomeUpgradeButton != null)
            {
                int level = progression.IncomeLevel;
                int maxLevel = progression.MaxIncomeLevel;
                if (progression.IsMaxIncome)
                {
                    SetUpgradeButtonLabels(incomeUpgradeButton, $"+{level * 35}% yield ({level}/{maxLevel})", "Maxed", false);
                }
                else
                {
                    long cost = progression.GetIncomeUpgradeCost();
                    SetUpgradeButtonLabels(incomeUpgradeButton, $"+{(level + 1) * 35}% yield ({level}/{maxLevel})", $"<sprite name=\"chips\"> {FormatNumber(cost)}", progression.Chips >= cost);
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
            RefreshResearchCards();
            RefreshSelectedResearchDetails();
        }

        // Drives every static Research tree node (Name/Level/Cost-InfoText) straight from
        // progression state. Cards are never instantiated — one already exists per research in the
        // Hierarchy grid. Clicking a card opens the Selected Research popup (OpenResearchDetail);
        // it does not buy directly, so there's no interactable/afford gating here beyond the tab
        // being unlocked at all.
        private void RefreshResearchCards()
        {
            if (progression == null || researchCards == null)
            {
                return;
            }

            foreach (StackMergeResearchCard card in researchCards)
            {
                if (card == null)
                {
                    continue;
                }

                ResearchDefinition definition = progression.GetResearchDefinition(card.researchId);
                int level = progression.GetResearchLevel(card.researchId);
                bool maxed = progression.IsResearchMaxed(card.researchId);

                SetText(card.nameText, definition.DisplayName);
                SetText(card.levelText, $"{level}/{definition.MaxLevel}");

                if (maxed)
                {
                    SetText(card.costText, "Maxed");
                }
                else
                {
                    string reason = progression.GetResearchUnavailableReason(card.researchId);
                    // "Not enough Insight." still shows the price (you could afford it later);
                    // any other non-empty reason (prerequisite unmet, not prestiged yet) is a hard
                    // structural lock.
                    if (!string.IsNullOrEmpty(reason) && reason != "Not enough Insight.")
                    {
                        SetText(card.costText, "Locked");
                    }
                    else
                    {
                        long cost = progression.GetResearchCost(card.researchId);
                        SetText(card.costText, $"<sprite name=\"insight\"> {FormatNumber(cost)}");
                    }
                }

                // Tree nodes are always clickable — even a "Locked" node should open the popup so
                // the player can see what's blocking it. Only the popup's own Buy button gates on
                // affordability/prerequisites.
            }
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
                RefreshRecentRunsTable(history);

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
                $"Stored runs: {history.Length}/250\n" +
                $"Latest: {FormatNumber(latest.score)} ({SolverName(latest.solverId)})\n" +
                $"Highest: {FormatNumber(best.score)} ({SolverName(best.solverId)})\n" +
                $"Highest median: {bestMedian.SolverName} ({FormatNumber(bestMedian.MedianScore)})");

            //SetText(
            //    historyInsightText,
            //    $"Best median: {bestMedian.SolverName} ({FormatNumber(bestMedian.MedianScore)}) | Best peak: {bestPeak.SolverName} ({FormatNumber(bestPeak.MaxScore)}) | Trend = last {trendCount} runs (chronological), more honest than a single aggregate median");

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

        // Instantiates one recentRunRowPrefab per recent run under historyRecentRunsRoot.
        private void RefreshRecentRunsTable(RunHistoryEntry[] history)
        {
            RectTransform root = historyRecentRunsRoot;
            if (root == null)
            {
                return;
            }

            ClearInstantiatedRows<StackMergeRecentRunRow>(root);
            if (recentRunRowPrefab == null)
            {
                Debug.LogWarning("StackMerge: Recent run row prefab not assigned — assign it on the Bootstrap in the Inspector.");
                return;
            }

            float rowHeight = RowHeightOf((RectTransform)recentRunRowPrefab.transform, 44f);
            float y = 0f;
            foreach (RunHistoryEntry entry in history.Take(40))
            {
                StackMergeRecentRunRow row = Instantiate(recentRunRowPrefab, root, false);
                PositionRow((RectTransform)row.transform, y, rowHeight);
                SetText(row.runText, $"{entry.runIndex}");
                SetText(row.solverText, SolverName(entry.solverId));
                SetText(row.scoreText, FormatNumber(entry.score));
                SetText(row.movesText, entry.moves.ToString());
                SetText(row.mergesText, entry.merges.ToString());
                SetText(row.highText, FormatNumber(entry.highestMergedBlock));
                y += rowHeight + 3f;
            }
        }

        // Instantiates one solverStatRowPrefab per solver under historySolverTableRoot.
        // The optional "i" button on each row opens the solver info modal.
        private void BuildSolverList(HistorySolverStats[] stats)
        {
            RectTransform root = historySolverTableRoot;
            if (root == null)
            {
                return;
            }

            ClearInstantiatedRows<StackMergeSolverStatRow>(root);
            if (solverStatRowPrefab == null)
            {
                Debug.LogWarning("StackMerge: Solver stat row prefab not assigned — assign it on the Bootstrap in the Inspector.");
                return;
            }

            float rowHeight = RowHeightOf((RectTransform)solverStatRowPrefab.transform, 44f);
            float y = 0f;
            foreach (HistorySolverStats stat in stats.OrderByDescending(s => s.MedianScore))
            {
                StackMergeSolverStatRow row = Instantiate(solverStatRowPrefab, root, false);
                PositionRow((RectTransform)row.transform, y, rowHeight);
                SetText(row.solverText, stat.SolverName);
                SetText(row.runsText, stat.SolverId < 0
                    ? FormatNumber(stat.Runs)
                    : FormatNumber(progression.GetSolverLifetimeRuns((SolverId)stat.SolverId)));
                SetText(row.medianText, FormatNumber(stat.MedianScore));
                SetText(row.bestText, FormatNumber(stat.MaxScore));
                SetText(row.highText, FormatNumber(stat.BestHighestMerged));

                if (row.infoButton != null)
                {
                    // Manual runs (solverId < 0) have no catalog entry, so hide the info button for them.
                    bool hasInfo = stat.SolverId >= 0;
                    row.infoButton.gameObject.SetActive(hasInfo);
                    if (hasInfo)
                    {
                        SolverId captured = (SolverId)stat.SolverId;
                        row.infoButton.onClick.RemoveAllListeners();
                        row.infoButton.onClick.AddListener(() => ShowSolverInfoModal(captured));
                    }
                }

                y += rowHeight + 3f;
            }
        }

        // Destroys only the rows this code instantiated (children carrying the row component),
        // leaving any static header / decoration you placed in the container untouched.
        private static void ClearInstantiatedRows<T>(Transform root) where T : Component
        {
            if (root == null)
            {
                return;
            }

            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child.GetComponent<T>() != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }

        // Reads the prefab's designed height; falls back if it has none yet.
        private static float RowHeightOf(RectTransform prefabRect, float fallback)
        {
            if (prefabRect == null)
            {
                return fallback;
            }

            float height = prefabRect.sizeDelta.y;
            return height > 1f ? height : fallback;
        }

        // Top-anchored, full-width row stacked downward by `y`. Matches the spacing the old
        // tables used so prefab rows drop straight into the existing containers.
        private static void PositionRow(RectTransform rt, float y, float height)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -y - height);
            rt.offsetMax = new Vector2(0f, -y);
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
                $"Completed goals: {completed}/{StackMergeProgression.Achievements.Length}\n" +
                $"Runs: {FormatNumber(progression.RunsCompleted)} ({FormatNumber(progression.ManualRunsCompleted)} manual)\n" +
                $"Merges: {FormatNumber(progression.TotalMerges)}\n" +
                $"Highest: {FormatNumber(progression.HighestBlockEver)}\n" +
                $"Earned: {FormatNumber(progression.TotalChipsEarned)}\n" +
                $"Spent: {FormatNumber(progression.TotalChipsSpent)}\n" +
                $"Best run: {FormatNumber(progression.BestRunScore)}");

            BuildGoalRows();
        }

        // Instantiates one goalRowPrefab per achievement under achievementListRoot.
        private void BuildGoalRows()
        {
            RectTransform root = achievementListRoot;
            if (root == null)
            {
                return;
            }

            ClearInstantiatedRows<StackMergeGoalRow>(root);
            if (goalRowPrefab == null)
            {
                Debug.LogWarning("StackMerge: Goal row prefab not assigned — assign it on the Bootstrap in the Inspector.");
                return;
            }

            float rowHeight = RowHeightOf((RectTransform)goalRowPrefab.transform, 50f);
            float y = 0f;
            foreach (AchievementDefinition achievement in StackMergeProgression.Achievements)
            {
                long progress = progression.GetAchievementProgress(achievement);
                long cappedProgress = Math.Min(progress, achievement.Target);
                bool complete = progression.IsAchievementComplete(achievement);

                StackMergeGoalRow row = Instantiate(goalRowPrefab, root, false);
                PositionRow((RectTransform)row.transform, y, rowHeight);
                SetText(row.goalText, achievement.Description);
                SetText(row.progressText, complete
                    ? "Completed"
                    : $"{FormatNumber(cappedProgress)} / {FormatNumber(achievement.Target)}");

                y += rowHeight + 4f;
            }
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

            if (solverInfoOverlay == null)
            {
                Debug.LogWarning("StackMerge: Solver Info overlay not assigned — assign it on the Bootstrap in the Inspector.");
                return;
            }

            SolverDefinition definition = StackMergeSolverCatalog.GetDefinition(solverId);
            RunHistoryEntry[] solverRuns = progression.RunHistory.Where(entry => entry.solverId == (int)solverId).ToArray();
            int lifetime = progression.GetSolverLifetimeRuns(solverId);

            SetText(solverInfoTitle, $"{definition.DisplayName} detail");

            var stats = new StringBuilder();
            stats.AppendLine(definition.Description);
            stats.AppendLine();
            stats.AppendLine($"<b>Lifetime runs</b>\n" +
                $"{FormatNumber(lifetime)} (stored history: {solverRuns.Length})");
            if (solverRuns.Length > 0)
            {
                long[] scores = solverRuns.Select(entry => entry.score).OrderBy(value => value).ToArray();
                stats.AppendLine($"Best {FormatNumber(scores[^1])}\n" +
                    $"Median {FormatNumber(Median(scores))}\n" +
                    $"Average {FormatNumber((long)scores.Average())}");
                stats.AppendLine($"Range {FormatNumber(scores[0])} – {FormatNumber(scores[^1])}");
                stats.AppendLine($"Best high tile {FormatNumber(solverRuns.Max(entry => entry.highestMergedBlock))}\n" +
                    $"Average moves {solverRuns.Average(entry => entry.moves):0}");
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

            SetActive(solverInfoOverlay, true);
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

        private void HideSolverInfoModal()
        {
            SetActive(solverInfoOverlay, false);
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
                PositionRunInfoPanel();
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

            const float top = 470f;
            float height = CalculateBoardHeight(gameState.StackCapacity);
            board.anchorMin = new Vector2(0f, 1f);
            board.anchorMax = new Vector2(1f, 1f);
            board.pivot = new Vector2(0.5f, 1f);
            board.offsetMin = new Vector2(0f, -top - height);
            board.offsetMax = new Vector2(0f, -top);
        }

        // Centers runInfoPanel in the vertical gap between the Board's bottom edge and the
        // Footer's top edge, so it sits an equal distance from both — instead of a fixed offset
        // that leaves a huge empty gap on tall screens. Reads Board's and Footer's *current*
        // offsets directly (rather than hardcoding their layout constants), so it keeps working
        // if either is resized by hand later. Horizontal anchoring/margins are left untouched —
        // only the vertical position is taken over.
        private void PositionRunInfoPanel()
        {
            if (runInfoPanel == null || boardRoot == null || footerRoot == null)
            {
                return;
            }

            RectTransform parent = boardRoot.parent as RectTransform;
            if (parent == null || footerRoot.parent != parent || runInfoPanel.parent != parent)
            {
                // Only auto-position when Run Info actually is a sibling of Board/Footer under the
                // same parent — otherwise the maths below wouldn't be meaningful.
                return;
            }

            if (runInfoDesignHeight < 0f)
            {
                runInfoDesignHeight = Mathf.Max(40f, runInfoPanel.rect.height);
            }

            float boardBottomFromTop = -boardRoot.offsetMin.y;
            float footerTopFromBottom = footerRoot.offsetMax.y;
            float footerTopFromTop = parent.rect.height - footerTopFromBottom;

            float gapCenterFromTop = (boardBottomFromTop + footerTopFromTop) * 0.5f;
            float panelTopFromTop = gapCenterFromTop - runInfoDesignHeight * 0.5f;

            runInfoPanel.anchorMin = new Vector2(runInfoPanel.anchorMin.x, 1f);
            runInfoPanel.anchorMax = new Vector2(runInfoPanel.anchorMax.x, 1f);
            runInfoPanel.pivot = new Vector2(runInfoPanel.pivot.x, 1f);
            runInfoPanel.offsetMin = new Vector2(runInfoPanel.offsetMin.x, -(panelTopFromTop + runInfoDesignHeight));
            runInfoPanel.offsetMax = new Vector2(runInfoPanel.offsetMax.x, -panelTopFromTop);
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

            SetText(gameOverScoreText, $"Score {FormatNumber(gameState.Score)}");
            SetText(gameOverBestText,
                $"Moves {FormatNumber(gameState.BlocksDropped)}   Merges {FormatNumber(gameState.TotalMerges)}\n" +
                $"Highest block {FormatNumber(gameState.HighestMergedBlock)}\n" +
                $"+{FormatNumber(currentRunChipsEarned)} chips earned");
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

        // Redesigned Upgrades menu: every upgrade button carries its own NameText + Cost/InfoText
        // children via a StackMergeButtonLabelPair component. Looked up per-call (not cached) since
        // this only runs during UI refreshes, never per-frame. Falls back to the button's own combined
        // text if no label-pair component is present yet, so nothing breaks while wiring is in progress.
        private static void SetUpgradeButtonLabels(Button button, string name, string cost, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = interactable;

            StackMergeButtonLabelPair labels = button.GetComponent<StackMergeButtonLabelPair>();
            if (labels != null)
            {
                SetText(labels.nameText, name);
                SetText(labels.costText, cost);
            }
            else
            {
                SetButtonText(button, $"{name}\n{cost}");
            }
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
            // Negatív számok kezelése (opcionális)
            if (value < 0)
                return "-" + FormatNumber(-value);

            // 100 000 alatt nem rövidítünk
            if (value < 100_000)
                return value.ToString();

            // Egységek: osztó, szöveg, minimális érték (csak a K-nál tér el)
            var units = new (long divisor, string suffix, long minValue)[]
            {
                (1_000_000_000_000_000_000L, "Qi", 1_000_000_000_000_000_000L),
                (1_000_000_000_000_000L, "Qa", 1_000_000_000_000_000L),
                (1_000_000_000_000L, "T", 1_000_000_000_000L),
                (1_000_000_000L, "B", 1_000_000_000L),
                (1_000_000L, "M", 1_000_000L),
                (1000L, "K", 100_000L)
            };

            // Kiválasztjuk a legnagyobb egységet, amelyre value >= minValue
            int selectedIndex = -1;
            for (int i = 0; i < units.Length; i++)
            {
                if (value >= units[i].minValue)
                {
                    selectedIndex = i;
                    break;
                }
            }

            // Biztonsági visszatérés (ide nem juthatunk, mert value >= 100_000)
            if (selectedIndex == -1)
                return value.ToString();

            // Addig lépünk felfelé a nagyobb egységek felé, amíg a kerekített érték < 1000,
            // vagy elérjük a legnagyobb egységet.
            int currentIndex = selectedIndex;
            while (true)
            {
                double d = (double)value / units[currentIndex].divisor;
                double rounded = Math.Round(d, 2, MidpointRounding.AwayFromZero);

                // Ha a kerekített érték kisebb, mint 1000, vagy már a legnagyobb egységnél vagyunk
                if (rounded < 1000 || currentIndex == 0)
                {
                    // `0.##` formátum: levágja a felesleges tizedes nullákat
                    string formatted = rounded.ToString("0.##");
                    return formatted + units[currentIndex].suffix;
                }
                else
                {
                    // Lépünk a következő nagyobb egységre (kisebb index)
                    currentIndex--;
                }
            }
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
