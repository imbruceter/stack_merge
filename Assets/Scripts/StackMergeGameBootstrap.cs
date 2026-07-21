using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StackMerge
{
    public sealed class StackMergeGameBootstrap : MonoBehaviour
    {
        private const string HighScoreKey = "StackMerge.HighScore";
        private const float AutoRestartDelay = 1.2f;
        private const float StackBlockMinHeight = 74f;
        private const float StackBlockMaxHeight = 90f;
        private const float StackBlockPadding = 10f;
        private const float StackBlockSpacing = 10f;
        private const float StackInternalPadding = StackBlockPadding * 2f;
        private const float NextPanelChromeHeight = 64f;
        private const float SmallestEmergencyBlockHeight = 58f;
        private const float EmergencyFitBlockMinHeight = 1f;
        private const float GameplayModifiersSectionHeight = 80f;
        private const float GameOverOverlayDelay = 0.24f;
        private const int StartupGameplayLayoutWarmupFrames = 90;
        private const int TabGameplayLayoutWarmupFrames = 18;
        private const int NoArmedGameplayModifier = -1;
        private const string ShowFpsSettingKey = "StackMerge.Settings.ShowFps";
        private const string SuppressAchievementNotificationSettingKey = "StackMerge.Settings.SuppressAchievementNotification";
        private const string TestingPpoFramesSettingKey = "StackMerge.Settings.TestingPpoFrames";
        private const string TestingTripleIncomeSettingKey = "StackMerge.Settings.TestingTripleIncome";
        private const string LanguageSettingKey = "StackMerge.Settings.Language";
        private const string BlockNumeralSettingKey = "StackMerge.Settings.BlockNumerals";
        private const string PrestigeResetInfoBody =
            "In case of a Prestige reset, the game will restart. All <sprite name=\"chips\" tint=1>, <sprite name=\"token\" tint=1>, upgrades, Agents, and Modifiers will be lost.\n\n" +
            "However, existing <sprite name=\"insight\" tint=1> and purchased research will remain, which will make further resets progressively more valuable.\n\n" +
            "<b>It is recommended to use PPO in Normal Mode before resetting to gain <sprite name=\"insight\" tint=1> multiplier.</b>";
        private static readonly Color DropdownSelectedBackgroundColor = HexColor("#F2EEE6");

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
        [Header("Feedback Toasts")]
        [Tooltip("Scene-built/prefab Feedback Modal. Expected child: StatusText. Instantiated for global purchase/lock/status feedback.")]
        [SerializeField] private GameObject feedbackModalPrefab;
        [Tooltip("Optional parent for spawned feedback modals. If empty, the Canvas is used.")]
        [SerializeField] private RectTransform feedbackModalRoot;
        [SerializeField, Min(0.1f)] private float feedbackModalLifetime = 2f;
        [SerializeField, Min(0f)] private float feedbackModalRiseDistance = 88f;
        [SerializeField] private bool feedbackModalAnchorBottom = true;
        [SerializeField] private Vector2 feedbackModalBottomOffset = new(0f, 110f);
        [SerializeField, Min(0f)] private float feedbackModalStackSpacing = 44f;
        [SerializeField] private RectTransform nextBlocksRoot;
        [Tooltip("Root of the full Next Blocks Panel. Drag the panel itself here, not only its Content child.")]
        [SerializeField] private RectTransform nextBlocksPanelRoot;
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

        [Header("Gameplay Modifiers")]
        [SerializeField] private GameObject gameplayModifiersSection;
        [SerializeField] private Button gameplayMinersPickaxeButton;
        [SerializeField] private TMP_Text gameplayMinersPickaxeAmountText;
        [SerializeField] private Button gameplayQueueScrubberButton;
        [SerializeField] private TMP_Text gameplayQueueScrubberAmountText;

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
        [Header("Settings UI")]
        [SerializeField] private Toggle showFpsToggle;
        [SerializeField] private TMP_Text fpsText;
        [SerializeField] private Toggle suppressAchievementNotificationToggle;
        [Tooltip("Testing only: lowers the PPO Normal Mode frame requirement so late-game flow can be checked quickly.")]
        [SerializeField] private Toggle testingPpoFramesToggle;
        [Tooltip("Testing only: multiplies chip income by 3. Default off; intended for local balance testing.")]
        [SerializeField] private Toggle x3IncomeToggle;
        [SerializeField] private TMP_Dropdown languageDropdown;
        [Tooltip("Block numeral style dropdown in Settings. Auto-found by a name containing 'Numeral'. Locked styles show '(Locked)' and snap back to the previous choice.")]
        [SerializeField] private TMP_Dropdown blockNumeralDropdown;
        [Tooltip("Optional How To Play panel controller. Auto-found if left empty.")]
        [SerializeField] private StackMergeHowToPlayPanel howToPlayPanel;
        [Tooltip("Reusable Help Overlay used for one-time menu introductions. Auto-found if left empty.")]
        [SerializeField] private StackMergeHelpOverlay menuHelpOverlay;
        [Tooltip("Testing helper: when enabled, menu intro overlays appear every time a menu is opened instead of only once.")]
        [SerializeField] private bool showMenuHelpEveryOpen;
        [Tooltip("Optional editable menu intro copy. If empty, built-in concise defaults are used.")]
        [SerializeField] private MenuHelpOverlayEntry[] menuHelpEntries = Array.Empty<MenuHelpOverlayEntry>();
        [SerializeField] private Button[] tabButtons = Array.Empty<Button>();
        [Tooltip("Optional lock icon used by locked bottom-menu tabs. Drag your lock sprite here in the Inspector.")]
        [SerializeField] private Sprite lockedTabIcon;
        [SerializeField] private float bottomMenuHighlightSlideSeconds = 0.18f;

        [Header("Button Visuals")]
        [Tooltip("How much to reduce an Image button's HSV Value when Button.interactable is false. 0.40 means V 100 becomes V 60.")]
        [SerializeField, Range(0f, 1f)] private float disabledButtonValueDrop = 0.4f;

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
        [Header("PPO Scene UI")]
        [Tooltip("Scene-built PPO mode overlay root. Expected hierarchy: PPO Mode Overlay (Image, Button) > PPO Mode Modal > InfoText + Buttons.")]
        [SerializeField] private GameObject ppoModeOverlay;
        [SerializeField] private TMP_Text ppoModeHintText;
        [SerializeField] private Button ppoTrainingButton;
        [SerializeField] private Button ppoNormalButton;
        [Tooltip("Scene-built PPO training overlay. It is positioned over only the Gameplay Next + Board area.")]
        [SerializeField] private RectTransform trainingOverlay;
        [SerializeField] private TMP_Text trainingOverlayText;
        [SerializeField, Min(80f)] private float ppoTrainingOverlayMinHeight = 150f;
        [SerializeField, Min(0f)] private float ppoTrainingOverlayVerticalPadding = 36f;
        [SerializeField, Range(20f, 100f)] private float ppoTrainingMatrixBoardWidthPercent = 88f;
        [SerializeField, Range(20f, 100f)] private float ppoTrainingMatrixNextWidthPercent = 64f;
        [Tooltip("TMP <mspace> width used for PPO Training numbers. Tune slightly if your Training Text font looks off-center.")]
        [SerializeField, Range(0.35f, 1.2f)] private float ppoTrainingMatrixMspaceEm = 0.62f;
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
        [Tooltip("Single Compute Speed upgrade button. Optional: if left empty, the Bootstrap searches the scene for a Button whose name/label contains \"Compute Speed\".")]
        [SerializeField] private Button computeSpeedUpgradeButton;
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
        [Tooltip("Single Scaling Frequency upgrade button. Optional: if left empty, the Bootstrap searches the scene for a Button whose name/label contains \"Scaling Frequency\".")]
        [SerializeField] private Button scalingFrequencyUpgradeButton;
        [Tooltip("Single Profitable Ending upgrade button. Optional: if left empty, the Bootstrap searches the scene for a Button whose name/label contains \"Profitable Ending\".")]
        [SerializeField] private Button profitableEndingUpgradeButton;
        [Tooltip("Single Passive Yield upgrade button. Optional: if left empty, the Bootstrap searches the scene for a Button whose name/label contains \"Passive Yield\".")]
        [SerializeField] private Button passiveYieldUpgradeButton;
        [Tooltip("Single Combo Engine upgrade button. Optional: auto-found by a name/label containing \"Combo Engine\".")]
        [SerializeField] private Button comboEngineUpgradeButton;
        [Tooltip("Single Salvage Protocol upgrade button. Optional: auto-found by a name/label containing \"Salvage Protocol\".")]
        [SerializeField] private Button salvageProtocolUpgradeButton;
        [Tooltip("Single Token Dividend upgrade button. Optional: auto-found by a name/label containing \"Token Dividend\".")]
        [SerializeField] private Button tokenDividendUpgradeButton;
        [Tooltip("Single Passive Tick Rate upgrade button. Optional: if left empty, the Bootstrap searches the scene for a Button whose name/label contains \"Passive Tick Rate\".")]
        [SerializeField] private Button passiveTickRateUpgradeButton;
        [Tooltip("Single Active Multiplier upgrade button. Optional: if left empty, the Bootstrap searches the scene for a Button whose name/label contains \"Active Multiplier\".")]
        [SerializeField] private Button activeMultiplierUpgradeButton;
        [Tooltip("PPO Curriculum amount upgrade. Optional: auto-found by a name/label containing \"Curriculum\" (but not \"Rate\").")]
        [SerializeField] private Button curriculumAmountUpgradeButton;
        [Tooltip("PPO Curriculum rate upgrade. Optional: auto-found by a name/label containing \"Curriculum Rate\".")]
        [SerializeField] private Button curriculumRateUpgradeButton;
        [SerializeField] private TMP_Text progressionStageText;
        [Tooltip("Optional separate text for the current progression stage name (for example: Stage 2 - Agent acceleration).")]
        [SerializeField] private TMP_Text progressionStageNameText;
        [SerializeField] private Button modifiersMenuUnlockButton;
        [SerializeField] private Button agentsMenuUnlockButton;
        [SerializeField] private TMP_Text prestigeSummaryText;
        [SerializeField] private Button prestigeButton;
        [Tooltip("Optional hand-built Prestige Reset Modal root. Auto-found by name ('Prestige Reset Modal') when left empty; without it the Prestige button falls back to resetting immediately.")]
        [SerializeField] private GameObject prestigeResetModal;
        [SerializeField] private Button prestigeResetBackButton;
        [Tooltip("Text under 'PPO Training Progress' showing frames done / required.")]
        [SerializeField] private TMP_Text prestigeResetTrainingText;
        [Tooltip("Explains what Prestige reset keeps and removes. Optional: auto-found by name ('ResetInfoText').")]
        [SerializeField] private TMP_Text prestigeResetInfoText;
        [SerializeField] private Slider prestigeResetSlider;
        [Tooltip("Legacy separate amount label. Hidden when present; the Buy button now shows the reset reward.")]
        [SerializeField] private TMP_Text prestigeResetAmountText;
        [SerializeField] private Button prestigeResetBuyButton;
        [Tooltip("Optional hand-built Offline Progress Overlay root. Auto-found by name ('Offline Progress Overlay') when left empty. Shown on startup once Offline Engine research is bought and offline time accrued.")]
        [SerializeField] private GameObject offlineProgressOverlay;
        [SerializeField] private TMP_Text offlineGainText;
        [Tooltip("Text under 'Offline Time' showing rewarded time vs the cap.")]
        [SerializeField] private TMP_Text offlineTimeText;
        [SerializeField] private Slider offlineProgressSlider;
        [SerializeField] private Button offlineBackButton;
        [SerializeField] private Button offlineCollectButton;
        [Tooltip("Optional hand-built Datacenter Panel (tab content, shown by the Datacenter bottom-bar tab at index 7). Auto-found by name ('Datacenter Panel').")]
        [SerializeField] private GameObject datacenterPanel;
        [Tooltip("Testing: treat the Datacenter as unlocked in the Editor regardless of prestige count. Has no effect in builds.")]
        [SerializeField] private bool unlockDatacenterInEditor;
        [Tooltip("Shows 'Total compute: 412.6 GF/s'.")]
        [SerializeField] private TMP_Text datacenterComputeText;
        [Tooltip("Shows 'NN% allocated' in the Compute Allocation title.")]
        [SerializeField] private TMP_Text datacenterAllocationTotalText;
        [Tooltip("Read-only bar showing the total allocated fraction.")]
        [SerializeField] private Slider datacenterAllocationTotalSlider;
        [Tooltip("Training Cluster / Analysis Node / Market Bots rows, in this order.")]
        [SerializeField] private DatacenterAllocationRow[] datacenterAllocationRows = { new(), new(), new() };
        [Tooltip("Shows 'NN units' in the Server Racks title.")]
        [SerializeField] private TMP_Text datacenterRackUnitsText;
        [Tooltip("CPU Rack / GPU Rack / TPU Pod / Neural Fabric rows, in this order.")]
        [SerializeField] private DatacenterItemRow[] datacenterRackRows = { new(), new(), new(), new() };
        [Tooltip("Power Grid / Cooling Loop / Fabric Interconnect rows, in this order.")]
        [SerializeField] private DatacenterItemRow[] datacenterFacilityRows = { new(), new(), new() };

        [Serializable]
        private sealed class DatacenterAllocationRow
        {
            public TMP_Text percentText;
            public TMP_Text prodText;
            public TMP_Text descText;
            public Slider slider;
        }

        // Runtime-built stacked bar inside the AllocatedSlider node: three colored segments whose
        // widths show the Training/Analysis/Market split on a 0-100% scale.
        private Image[] datacenterAllocationSegments;

        private static readonly Color[] DatacenterAllocationFallbackColors =
        {
            new(0.486f, 0.227f, 0.929f), // Training Cluster — purple (#7C3AED)
            new(0.133f, 0.827f, 0.933f), // Analysis Node — cyan (#22D3EE)
            new(0.961f, 0.620f, 0.043f)  // Market Bots — amber (#F59E0B)
        };

        [Serializable]
        private sealed class DatacenterItemRow
        {
            public TMP_Text prodText;
            public TMP_Text unitsText;
            public Button buyButton;
        }
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
        [SerializeField] private TMP_Text historyHighestText;
        [SerializeField] private TMP_Text historyHighestMedianText;
        [SerializeField] private TMP_Text historyAverageRunTimeText;
        [SerializeField] private TMP_Text historyPlaytimeText;
        [SerializeField] private RectTransform historyChartRoot;
        [SerializeField] private RectTransform historySolverTableRoot;
        [SerializeField] private RectTransform historyRecentRunsRoot;
        [SerializeField] private TMP_Text historyInsightText;
        [SerializeField] private Button historyBackButton;
        [SerializeField] private TMP_Text achievementStatsText;
        [SerializeField] private TMP_Text achievementRunsText;
        [SerializeField] private TMP_Text achievementBestRunText;
        [SerializeField] private TMP_Text achievementMovesText;
        [SerializeField] private TMP_Text achievementMergesText;
        [SerializeField] private TMP_Text achievementHighestText;
        [SerializeField] private TMP_Text achievementEarnedText;
        [SerializeField] private TMP_Text achievementCompletedGoalsText;
        [SerializeField] private TMP_Text achievementResetsText;
        [SerializeField] private TMP_Text achievementPlaytimeText;
        [SerializeField] private RectTransform achievementListRoot;
        [SerializeField] private RectTransform secretAchievementListRoot;
        [SerializeField] private TMP_Text secretAchievementCompletedGoalsText;
        [SerializeField] private GameObject achievementGoalsListCategory;
        [SerializeField] private GameObject achievementSecretGoalsListCategory;
        [SerializeField] private GameObject achievementLifetimeStatsCategory;
        [SerializeField] private GameObject achievementPpoProgressCategory;
        [SerializeField] private AchievementPanelTab[] achievementTabs = Array.Empty<AchievementPanelTab>();
        [SerializeField] private AchievementPanelTabKind defaultAchievementTab = AchievementPanelTabKind.Achievements;
        [SerializeField] private TMP_Text ppoProgressTotalFramesText;
        [SerializeField] private TMP_Text ppoProgressAverageFramesPerPrestigeText;
        [SerializeField] private TMP_Text ppoProgressTotalRunsText;
        [SerializeField] private TMP_Text ppoProgressBestRunText;
        [SerializeField] private TMP_Text ppoProgressTotalMovesText;
        [SerializeField] private TMP_Text ppoProgressTotalMergesText;
        [SerializeField] private TMP_Text ppoProgressKnowledgeText;
        [SerializeField] private TMP_Text ppoProgressPlaytimeText;
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

        [Header("Row Prefabs")]
        [Tooltip("One row of the Goals list. Needs a StackMergeGoalRow component on its root.")]
        [SerializeField] private StackMergeGoalRow goalRowPrefab;
        [Tooltip("One row of the Secrets list. Needs a StackMergeGoalRow component on its root, with optional DescText.")]
        [SerializeField] private StackMergeGoalRow secretGoalRowPrefab;
        [Tooltip("One row of History > Solvers. Needs a StackMergeSolverStatRow component on its root.")]
        [SerializeField] private StackMergeSolverStatRow solverStatRowPrefab;
        [Tooltip("One row of History > Recent Runs. Needs a StackMergeRecentRunRow component on its root.")]
        [SerializeField] private StackMergeRecentRunRow recentRunRowPrefab;

        [Header("Solver Info Modal")]
        [Tooltip("Root overlay GameObject of the solver info modal — shown/hidden like the Gameplay Info Overlay.")]
        [SerializeField] private GameObject solverInfoOverlay;
        [SerializeField] private TMP_Text solverInfoTitle;
        [SerializeField] private TMP_Text solverInfoStatsText;
        [SerializeField] private TMP_Text solverInfoTuningText;
        [Tooltip("Optional. A RectTransform where the score-history line chart is drawn. Leave empty to skip the chart.")]
        [SerializeField] private RectTransform solverInfoChartRoot;
        [SerializeField] private Button solverInfoCloseButton;

        [Header("Templates")]
        [Tooltip("Prefab used for every visible block in both stacks and the next queue. Expected hierarchy: Block (Image) > Text (TMP).")]
        [SerializeField] private RectTransform blockTemplate;
        [Tooltip("Fallback only: when the block prefab/Image has no sprite, generate a rounded sprite in code.")]
        [SerializeField] private bool useGeneratedBlockSprites = false;
        [Tooltip("Legacy runtime polish. Off by default so scene-built menus keep their Inspector-assigned Image backgrounds/sprites.")]
        [SerializeField] private bool applyGeneratedRoundedTheme = false;

        [Header("Game Over")]
        [SerializeField] private GameObject gameOverOverlay;
        [SerializeField] private TMP_Text gameOverScoreText;
        [SerializeField] private TMP_Text gameOverBestText;
        [Tooltip("Optional separate text for the just-finished run duration. Auto-found by name if left empty.")]
        [SerializeField] private TMP_Text gameOverRunTimeText;
        [SerializeField] private Slider gameOverAutoRestartSlider;

        [Header("Achievement Notification")]
        [SerializeField] private GameObject achievementNotificationPanel;
        [SerializeField] private TMP_Text achievementNotificationGoalText;
        [Tooltip("Optional. Shows the completed goal's unlock reward; stays hidden for goals without one. Auto-found by name ('RewardText').")]
        [SerializeField] private TMP_Text achievementNotificationRewardText;
        [SerializeField] private Button achievementNotificationCloseButton;
        [Tooltip("Auto Buy toggle buttons in the Algorithms / Upgrades / Agents / Modifiers menus, in this order. Auto-found by name ('AutoBuyButton') under each panel. Hidden until the First Prestige goal is completed.")]
        [SerializeField] private Button[] autoBuyButtons = new Button[4];
        [SerializeField] private float achievementNotificationVisibleSeconds = 10f;
        [SerializeField] private float achievementNotificationSlideSeconds = 0.28f;

        private sealed class BottomTabVisual
        {
            public Image iconBackground;
            public RectTransform iconBackgroundRect;
            public Image icon;
            public TMP_Text label;
            public Sprite unlockedIcon;
            public string unlockedLabel;
            public Color unlockedLabelColor;
            public Vector2 iconBackgroundHomePosition;
        }

        [Serializable]
        private sealed class MenuHelpOverlayEntry
        {
            public StackMergeHowToPlayLayer layer;
            public string playerPrefsKey;
            public string title;
            [TextArea(3, 8)] public string body;
            public string magyarTitle;
            [TextArea(3, 8)] public string magyarBody;
        }

        private StackMergeGameState gameState;
        private StackMergeProgression progression;
        private readonly System.Random solverRandom = new();
        private long highScore;
        private float autoSolveTimer;
        private float autoRestartTimer;
        private float saveFlushTimer;
        private const float SaveFlushInterval = 10f;
        private float trainingEvalTimer;
        private float datacenterUiRefreshTimer;
        private float autoBuyTimer;
        private float playtimeTickAccumulator;
        private BlockNumeralStyle blockNumeralStyle = BlockNumeralStyle.Standard;
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
        private bool menuHelpReady;
        private StackMergeHowToPlayLayer? pendingMenuHelpLayer;
        private bool currentRunUsedAutoSolve;
        private int currentRunManualMoves;
        private float currentRunElapsed;
        private long currentRunChipsEarned;
        private long lastObservedChips = long.MinValue;
        private long lastObservedInsight = long.MinValue;
        private long lastObservedTokens = long.MinValue;
        // Drives the Active Multiplier upgrade: "actively playing" means a move (manual or
        // solver) happened recently, not literally this frame — otherwise the bonus would flicker
        // off between individual solver moves at slow Speed levels.
        private float timeSinceLastAcceptedMove = float.MaxValue;
        private const float ActivePlayWindowSeconds = 4f;
        private const float SolverImmediateDeselectSecretWindowSeconds = 4f;
        private SolverId selectedSolverId = SolverId.Rand;
        private AgentId selectedAgentId = AgentId.MergeBroker;
        private ModifierId selectedModifierId = ModifierId.UnstableStack;
        private ResearchId selectedResearchId = ResearchId.InsightAmplifier;
        private bool solverDeselected = false;
        private SolverId recentlySelectedSolverForSecret = SolverId.Rand;
        private float recentSolverSelectedAtTime;
        private bool hasRecentSolverSelectionForSecret;
        private double currentSessionPlaytimeSeconds;
        private bool exitRecordedThisSession;
        private int armedGameplayModifier = NoArmedGameplayModifier;
        private bool showFps;
        private bool suppressAchievementNotifications;
        private bool testingPpoFrames;
        private bool testingTripleIncome;
        private StackMergeLanguage currentLanguage = StackMergeLanguage.English;
        private bool syncingSettingsControls;
        private int lastLanguageDropdownValue = -1;
        private float fpsSampleTimer;
        private int fpsSampleFrames;
        private readonly Dictionary<TMP_Text, string> settingsStaticTextDefaults = new();
        private readonly Dictionary<TMP_Text, string> staticLocalizedTextDefaults = new();
        private readonly Dictionary<Image, Color> dropdownItemBackgroundDefaults = new();
        private BottomTabVisual[] bottomTabVisuals = Array.Empty<BottomTabVisual>();
        private readonly Dictionary<Button, Color> buttonNormalColors = new();
        private readonly List<StackMergeFeedbackModal> activeFeedbackModals = new();
        private int bottomMenuHighlightIndex = -1;
        private bool bottomMenuHighlightAnimating;
        private RectTransform bottomMenuHighlightRect;
        private int bottomMenuHighlightPendingIconIndex = -1;
        private Vector3 bottomMenuHighlightStartWorld;
        private Vector3 bottomMenuHighlightEndWorld;
        private float bottomMenuHighlightTimer;
        private int blockDropStack = -1;
        private float blockDropTimer = 0f;
        private const float BlockDropDuration = 0.14f;
        private float mergePulseTimer = 0f;
        private const float MergePulseDuration = 0.22f;
        private int mergePulseStack = -1;
        private RectTransform[] stackSlotLayers;
        private Coroutine gameplayLayoutWarmupCoroutine;
        private float gameOverOverlayTimer;
        private float nextTrainingOverlayRefreshTime;
        private float nextTrainingRunInfoRefreshTime;
        private bool initialGameplayLayoutFinalized;
        private const float TrainingOverlayRefreshInterval = 0.12f;
        private readonly HashSet<int> completedAchievementIds = new();
        private readonly HashSet<SecretAchievementId> completedSecretAchievementIds = new();
        private readonly Dictionary<int, StackMergeGoalRow> achievementRowsById = new();
        private readonly Dictionary<SecretAchievementId, StackMergeGoalRow> secretAchievementRowsById = new();
        private StackMergeLanguage achievementRowsLanguage = (StackMergeLanguage)(-1);
        private StackMergeLanguage secretAchievementRowsLanguage = (StackMergeLanguage)(-1);
        private AchievementPanelTabKind selectedAchievementTab;
        private readonly Queue<(string Description, string Reward)> achievementNotificationQueue = new();
        private bool achievementCompletionStateInitialized;
        private RectTransform achievementNotificationRect;
        private Vector2 achievementNotificationHomePosition;
        private Vector2 achievementNotificationEnterPosition;
        private Vector2 achievementNotificationExitPosition;
        private Vector2 achievementNotificationSlideOutStartPosition;
        private float achievementNotificationTimer;
        private AchievementNotificationState achievementNotificationState = AchievementNotificationState.Hidden;

        private enum AchievementNotificationState
        {
            Hidden,
            SlidingIn,
            Showing,
            SlidingOut
        }

        public enum AchievementPanelTabKind
        {
            Achievements,
            Secrets,
            Statistics,
            PpoProgress
        }

        [Serializable]
        private sealed class AchievementPanelTab
        {
            public AchievementPanelTabKind tab;
            public Button button;
            public Image icon;
            public TMP_Text text;

            [NonSerialized] public bool defaultsCached;
            [NonSerialized] public Color buttonColor;
            [NonSerialized] public Color iconColor;
            [NonSerialized] public Color textColor;
        }

        private static readonly (ResearchId From, ResearchId To)[] ResearchConnections =
        {
            (ResearchId.SeedCapital, ResearchId.AutomationMemory),
            (ResearchId.SeedCapital, ResearchId.PpoBootcamp),
            (ResearchId.SeedCapital, ResearchId.InsightAmplifier),
            (ResearchId.AutomationMemory, ResearchId.AlgorithmArchive),
            (ResearchId.AlgorithmArchive, ResearchId.AgentSynergy),
            (ResearchId.AgentSynergy, ResearchId.BulkDiscount),
            (ResearchId.BulkDiscount, ResearchId.YieldTheory),
            (ResearchId.PpoBootcamp, ResearchId.EvaluationEfficiency),
            (ResearchId.EvaluationEfficiency, ResearchId.PpoMemory),
            (ResearchId.PpoMemory, ResearchId.PpoHighFocus),
            (ResearchId.PpoHighFocus, ResearchId.PpoStability),
            (ResearchId.PpoStability, ResearchId.InsightExtractor),
            (ResearchId.InsightAmplifier, ResearchId.PassiveInsight),
            (ResearchId.PassiveInsight, ResearchId.OfflineEfficiency),
            (ResearchId.OfflineEfficiency, ResearchId.OfflineTime)
        };

        private void Awake()
        {
            StackMergeProgression.DebugUnlockDatacenter = unlockDatacenterInEditor && Application.isEditor;
            ConfigureCamera();
            EnsureEventSystem();
            EnsureOptionalUpgradeButtonReferences();
            EnsureGameplayModifierReferences();
            EnsureSettingsReferences();
            EnsureAchievementPanelReferences();
            selectedAchievementTab = defaultAchievementTab;
            LoadPlayerSettings();
            WireButtons();
            SyncSettingsControls();
            ApplyPlayerSettings();
            HideTemplate();
            HideFeedbackModalTemplate();
            EnsurePpoSceneUiReferences();
            EnsurePrestigeResetModalReferences();
            EnsureOfflineProgressOverlayReferences();
            EnsureDatacenterReferences();
            EnsureGameOverReferences();
            EnsureAutoBuyButtons();
            HidePpoModeModal();
            SetActive(trainingOverlay != null ? trainingOverlay.gameObject : null, false);
            SetActive(gameOverAutoRestartSlider != null ? gameOverAutoRestartSlider.gameObject : null, false);
            EnsureAchievementNotificationReferences();
            WireAchievementNotificationButton();
            WireAchievementPanelTabs();
            PrepareGlobalUiLayering();
            SelectTab(0);
        }

        private void Start()
        {
            Application.targetFrameRate = 120;

            highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            progression = StackMergeProgression.Load();
            selectedSolverId = progression.SelectedSolver;
            solverDeselected = progression.SolverDeselected;
            // The numeral preference loads before the save exists — re-validate it against unlocks.
            if (!progression.IsBlockNumeralUnlocked(blockNumeralStyle))
            {
                blockNumeralStyle = BlockNumeralStyle.Standard;
            }
            //ApplyModernTheme();
            EnsureResearchUi();
            WirePrestigeResearchButtons();
            EnsureAchievementNotificationReferences();
            WireAchievementNotificationButton();
            SyncCompletedAchievements();
            CreateFreshGame();
            PrimeGameplayPanelForInitialLayout();
            RefreshEverything();
            ForceGameplayLayoutPass();
            ScheduleGameplayLayoutWarmup(StartupGameplayLayoutWarmupFrames);
            if (progression.LastOfflineChips > 0 || progression.LastOfflineInsight > 0)
            {
                ShowFeedbackModal($"Offline gain: +{FormatNumber(progression.LastOfflineChips)} <sprite name=\"chips\" tint=1>, +{FormatNumber(progression.LastOfflineInsight)} <sprite name=\"insight\" tint=1>");
            }

            ShowOfflineProgressOverlayIfEarned();
            menuHelpReady = true;
            TryShowMenuHelpForSelectedTab();
            StartCoroutine(FinalizeInitialGameplayLayout());
        }

        /// <summary>
        /// Runtime visual polish that only rounds the corners of buttons and card panels.
        /// It NEVER sets a colour — every colour comes from the Image / Button components you
        /// edit in the Inspector. Images that already have a sprite assigned are left untouched,
        /// so any custom sprite you set in the Hierarchy is preserved.
        /// </summary>
        private void ApplyModernTheme()
        {
            if (!applyGeneratedRoundedTheme || canvas == null)
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
                RefreshNextBlocks();
            }

            TickAutomation();
            TickBottomMenuHighlight();
            MaybeEndStuckRun();
            TickBlockAnimations();
            TickGameOverOverlayDelay();
            TickAchievementNotification();
            TickLanguageDropdownSelection();
            TickDropdownSelectionVisuals();
            TickFpsDisplay();
            TickPassiveProduction();
            TickDatacenterProduction();
            TickAutoBuy();
            TickPlaytime();
            RefreshEconomyUiIfChanged();
            if (gameState != null && !gameState.IsGameOver)
            {
                currentRunElapsed += Time.deltaTime;
            }

            // Persistence is throttled: the per-move path only marks the progression dirty, and
            // autosave flushes periodically. Pause/quit paths force the heavier PPO policy save.
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
                RecordGameExitOnceForSecret();
                currentSessionPlaytimeSeconds = 0.0;
                FlushPlaytime();
                progression?.FlushIfDirty(forceMachineLearningPolicySave: true);
            }
        }

        private void OnApplicationQuit()
        {
            RecordGameExitOnceForSecret();
            FlushPlaytime();
            progression?.FlushIfDirty(forceMachineLearningPolicySave: true);
        }

        private void OnDisable()
        {
            if (gameplayLayoutWarmupCoroutine != null)
            {
                StopCoroutine(gameplayLayoutWarmupCoroutine);
                gameplayLayoutWarmupCoroutine = null;
            }

            if (menuHelpOverlay != null)
            {
                menuHelpOverlay.Hidden -= OnMenuHelpOverlayHidden;
            }

            FlushPlaytime();
            progression?.FlushIfDirty(forceMachineLearningPolicySave: true);
        }

        private void TickPlaytime()
        {
            if (progression == null)
            {
                playtimeTickAccumulator = 0f;
                return;
            }

            playtimeTickAccumulator += Time.unscaledDeltaTime;
            if (playtimeTickAccumulator < 1f)
            {
                return;
            }

            float elapsed = playtimeTickAccumulator;
            progression.TickPlaytime(elapsed);
            TickSessionPlaytimeSecret(elapsed);
            playtimeTickAccumulator = 0f;
        }

        private void FlushPlaytime()
        {
            if (progression == null || playtimeTickAccumulator <= 0f)
            {
                return;
            }

            float elapsed = playtimeTickAccumulator;
            progression.TickPlaytime(elapsed);
            TickSessionPlaytimeSecret(elapsed);
            playtimeTickAccumulator = 0f;
        }

        private void TickSessionPlaytimeSecret(float elapsedSeconds)
        {
            if (progression == null || elapsedSeconds <= 0f)
            {
                return;
            }

            currentSessionPlaytimeSeconds += elapsedSeconds;
            if (currentSessionPlaytimeSeconds >= 2.0 * 60.0 * 60.0)
            {
                progression.MarkSecretAchievementComplete(SecretAchievementId.OneMoreRun);
            }
        }

        private void RecordGameExitOnceForSecret()
        {
            if (progression == null || exitRecordedThisSession)
            {
                return;
            }

            exitRecordedThisSession = true;
            progression.RecordGameExit();
        }

        private void TickBlockAnimations()
        {
            if (blockDropTimer <= 0f && mergePulseTimer <= 0f)
            {
                return;
            }

            blockDropTimer = Mathf.Max(0f, blockDropTimer - Time.deltaTime);
            mergePulseTimer = Mathf.Max(0f, mergePulseTimer - Time.deltaTime);

            // Deliberately no IsGameOver gate: the run-ending block still needs its drop animation
            // to play out (the Game Over overlay waits for it via StartGameOverOverlayDelay).
            if (gameState != null && selectedTabIndex == 0 && !historyOpen && !achievementsOpen)
            {
                RefreshColumns();
            }
        }

        private void ScheduleGameplayLayoutWarmup(int frames = 5)
        {
            if (!isActiveAndEnabled || gameState == null)
            {
                return;
            }

            if (gameplayLayoutWarmupCoroutine != null)
            {
                StopCoroutine(gameplayLayoutWarmupCoroutine);
            }

            gameplayLayoutWarmupCoroutine = StartCoroutine(GameplayLayoutWarmup(frames));
        }

        private IEnumerator GameplayLayoutWarmup(int frames)
        {
            int safeFrames = Mathf.Clamp(frames, 1, 120);
            int stableFrames = 0;
            for (int i = 0; i < safeFrames; i++)
            {
                yield return null;
                yield return new WaitForEndOfFrame();
                ForceGameplayLayoutPass();
                if (IsGameplayLayoutReady())
                {
                    stableFrames++;
                    if (stableFrames >= 3 && i >= 5)
                    {
                        break;
                    }
                }
                else
                {
                    stableFrames = 0;
                }
            }

            gameplayLayoutWarmupCoroutine = null;
        }

        private void ForceGameplayLayoutPass()
        {
            if (gameState == null || selectedTabIndex != 0 || historyOpen || achievementsOpen)
            {
                return;
            }

            SetActive(gameplayPanel, true);
            Canvas.ForceUpdateCanvases();
            RebuildLayout(gameplayPanel != null ? gameplayPanel.transform as RectTransform : null);
            RebuildLayout(GetGameplaySectionsRoot());
            RebuildLayout(canvas != null ? canvas.transform as RectTransform : null);
            RebuildLayout(boardRoot != null ? boardRoot.parent as RectTransform : null);
            RebuildLayout(GetNextBlocksPanel());
            RebuildLayout(nextBlocksRoot);

            boardLayoutDirty = true;
            RefreshGameplayModifiers();
            RefreshColumns();
            Canvas.ForceUpdateCanvases();
            RefreshNextBlocks();
            RebuildLayout(nextBlocksRoot);
            RebuildLayout(GetNextBlocksPanel());
            RebuildLayout(GetGameplaySectionsRoot());
            Canvas.ForceUpdateCanvases();
        }

        private void PrimeGameplayPanelForInitialLayout()
        {
            if (selectedTabIndex != 0 || gameplayPanel == null)
            {
                return;
            }

            if (gameplayPanel.activeSelf)
            {
                gameplayPanel.SetActive(false);
            }

            gameplayPanel.SetActive(true);
        }

        private IEnumerator FinalizeInitialGameplayLayout()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            if (selectedTabIndex != 0 || gameplayPanel == null || gameState == null)
            {
                yield break;
            }

            gameplayPanel.SetActive(false);
            Canvas.ForceUpdateCanvases();
            gameplayPanel.SetActive(true);
            Canvas.ForceUpdateCanvases();

            boardLayoutDirty = true;
            lastRenderedCapacity = -1;
            RefreshGameplayModifiers();
            RefreshColumns();
            RefreshNextBlocks();
            ForceGameplayLayoutPass();
            ScheduleGameplayLayoutWarmup(TabGameplayLayoutWarmupFrames);
            initialGameplayLayoutFinalized = true;
        }

        private bool IsGameplayLayoutReady()
        {
            if (selectedTabIndex != 0 || gameplayPanel == null || !gameplayPanel.activeInHierarchy || boardRoot == null || nextBlocksRoot == null)
            {
                return false;
            }

            RectTransform sectionsRoot = GetGameplaySectionsRoot();
            if (sectionsRoot != null && (sectionsRoot.rect.width < 100f || sectionsRoot.rect.height < 240f))
            {
                return false;
            }

            if (nextBlocksRoot.rect.width < 100f || nextBlocksRoot.rect.height < 24f)
            {
                return false;
            }

            if (stackBlockLayers == null || stackBlockLayers.Length == 0)
            {
                return false;
            }

            foreach (RectTransform layer in stackBlockLayers)
            {
                if (layer == null || !layer.gameObject.activeInHierarchy || layer.rect.width < 60f || layer.rect.height < 180f)
                {
                    return false;
                }
            }

            int expectedNextBlocks = gameState != null ? gameState.NextBlocks.Count : 0;
            if (expectedNextBlocks > 0 && nextBlocksRoot.childCount < expectedNextBlocks)
            {
                return false;
            }

            if (expectedNextBlocks > 0 && nextBlocksRoot.GetChild(0) is RectTransform firstNextBlock)
            {
                LayoutElement layout = firstNextBlock.GetComponent<LayoutElement>();
                float width = layout != null ? layout.preferredWidth : firstNextBlock.rect.width;
                float height = layout != null ? layout.preferredHeight : firstNextBlock.rect.height;
                if (width < 60f || height < SmallestEmergencyBlockHeight)
                {
                    return false;
                }
            }

            return true;
        }

        private RectTransform GetGameplaySectionsRoot()
        {
            if (boardRoot == null)
            {
                return null;
            }

            return boardRoot.parent as RectTransform;
        }

        private static void RebuildLayout(RectTransform rectTransform)
        {
            if (rectTransform != null && rectTransform.gameObject.activeInHierarchy)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            }
        }

        private void FlushDynamicUiLayout()
        {
            if (canvas == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            RebuildLayout(canvas.transform as RectTransform);
            RebuildLayout(achievementListRoot);
            RebuildLayout(historyRecentRunsRoot);
            RebuildLayout(historySolverTableRoot);
            Canvas.ForceUpdateCanvases();
        }

        private void StartGameOverOverlayDelay()
        {
            gameOverOverlayTimer = Mathf.Max(GameOverOverlayDelay, blockDropTimer, mergePulseTimer);
            RefreshGameOver();
        }

        private void TickGameOverOverlayDelay()
        {
            if (gameOverOverlayTimer <= 0f)
            {
                return;
            }

            gameOverOverlayTimer = Mathf.Max(0f, gameOverOverlayTimer - Time.deltaTime);
            if (gameOverOverlayTimer <= 0f)
            {
                RefreshGameOver();
            }
        }

        private void EnsureAchievementNotificationReferences()
        {
            if (canvas == null)
            {
                return;
            }

            if (achievementNotificationPanel == null)
            {
                Transform panel = FindNamedDescendant(canvas.transform, "Achievement Notification Panel");
                if (panel != null)
                {
                    achievementNotificationPanel = panel.gameObject;
                }
            }

            if (achievementNotificationPanel == null)
            {
                return;
            }

            if (achievementNotificationRewardText == null)
            {
                Transform rewardText = FindNamedDescendant(achievementNotificationPanel.transform, "RewardText");
                achievementNotificationRewardText = rewardText != null ? rewardText.GetComponent<TMP_Text>() : null;
            }

            if (achievementNotificationGoalText == null)
            {
                Transform goalText = FindNamedDescendant(achievementNotificationPanel.transform, "GoalText");
                achievementNotificationGoalText = goalText != null
                    ? goalText.GetComponent<TMP_Text>()
                    : achievementNotificationPanel.GetComponentInChildren<TMP_Text>(true);
            }

            if (achievementNotificationCloseButton == null)
            {
                achievementNotificationCloseButton = achievementNotificationPanel.GetComponentInChildren<Button>(true);
            }

            RectTransform rect = achievementNotificationPanel.transform as RectTransform;
            if (rect != null && rect != achievementNotificationRect)
            {
                achievementNotificationRect = rect;
                achievementNotificationHomePosition = rect.anchoredPosition;
                RecalculateAchievementNotificationPositions();
            }

            if (achievementNotificationState == AchievementNotificationState.Hidden)
            {
                SetActive(achievementNotificationPanel, false);
            }
        }

        private void RecalculateAchievementNotificationPositions()
        {
            if (achievementNotificationRect == null)
            {
                return;
            }

            float travel = 520f;
            if (achievementNotificationRect.parent is RectTransform parent)
            {
                float parentWidth = parent.rect.width > 1f ? parent.rect.width : 0f;
                float panelWidth = achievementNotificationRect.rect.width > 1f
                    ? achievementNotificationRect.rect.width
                    : Mathf.Abs(achievementNotificationRect.sizeDelta.x);
                travel = Mathf.Max(420f, parentWidth + panelWidth + 80f);
            }

            achievementNotificationEnterPosition = achievementNotificationHomePosition + new Vector2(travel, 0f);
            achievementNotificationExitPosition = achievementNotificationHomePosition - new Vector2(travel, 0f);
        }

        private void WireAchievementNotificationButton()
        {
            if (achievementNotificationCloseButton == null)
            {
                return;
            }

            achievementNotificationCloseButton.onClick.RemoveAllListeners();
            achievementNotificationCloseButton.onClick.AddListener(DismissAchievementNotification);
        }

        /// <summary>
        /// Unlock rewards tied to specific goals. Shown on the goal row's RewardText and in the
        /// completion notification; the unlocks themselves are gated by the same conditions the
        /// goals measure (Auto Buy → PrestigeCount >= 1, Datacenter → PrestigeCount >= 5).
        /// </summary>
        private static string GetAchievementRewardText(int achievementId)
        {
            return achievementId switch
            {
                2 => "Unlocks Byte numeral for blocks",                        // "Chip Billion" (earn 1B)
                5 => "Unlocks Power numeral for blocks",                       // "Mega Budget" (spend 100M)
                10 => "Unlocks Roman numeral for blocks",                      // "Move Singularity" (1M moves)
                13 => "Unlocks Hexadecimal numeral for blocks",                // "Merge Singularity" (1M merges)
                16 => "Unlocks Scientific numeral for blocks",                 // "High 32768"
                20 => "Merge streak upgrade at max level is 5% stronger",
                23 => "Unlocks Auto Buy for Algos, Upgrades, Agents and Mods", // "First Prestige"
                24 => "Unlocks Datacenter",                                    // "Prestige Loop" (5×)
                32 => "PPO learns from your Manual runs",
                37 => "Higher frequency upgrade at max level is 2% stronger",
                _ => null
            };
        }

        private void SyncCompletedAchievements()
        {
            completedAchievementIds.Clear();
            completedSecretAchievementIds.Clear();
            if (progression == null)
            {
                achievementCompletionStateInitialized = false;
                return;
            }

            foreach (AchievementDefinition achievement in StackMergeProgression.Achievements)
            {
                if (progression.IsAchievementComplete(achievement))
                {
                    completedAchievementIds.Add(achievement.Id);
                }
            }

            foreach (SecretAchievementDefinition achievement in StackMergeProgression.SecretAchievements)
            {
                if (progression.IsSecretAchievementComplete(achievement))
                {
                    completedSecretAchievementIds.Add(achievement.Id);
                }
            }

            achievementCompletionStateInitialized = true;
        }

        private void QueueAchievementNotificationsForNewCompletions()
        {
            if (progression == null)
            {
                return;
            }

            if (!achievementCompletionStateInitialized)
            {
                SyncCompletedAchievements();
                return;
            }

            foreach (AchievementDefinition achievement in StackMergeProgression.Achievements)
            {
                if (!completedAchievementIds.Contains(achievement.Id) && progression.IsAchievementComplete(achievement))
                {
                    completedAchievementIds.Add(achievement.Id);
                    if (achievement.Id == 37)
                    {
                        ApplyCurrentBoardSettingsToGameState();
                    }

                    achievementNotificationQueue.Enqueue((achievement.Description, GetAchievementRewardText(achievement.Id)));
                }
            }

            foreach (SecretAchievementDefinition achievement in StackMergeProgression.SecretAchievements)
            {
                if (!completedSecretAchievementIds.Contains(achievement.Id) && progression.IsSecretAchievementComplete(achievement))
                {
                    completedSecretAchievementIds.Add(achievement.Id);
                    achievementNotificationQueue.Enqueue((achievement.DisplayName, achievement.Description));
                }
            }

            TryShowNextAchievementNotification();
        }

        private void TryShowNextAchievementNotification()
        {
            if (suppressAchievementNotifications)
            {
                HideAchievementNotificationImmediate(clearQueue: true);
                return;
            }

            if (achievementNotificationState != AchievementNotificationState.Hidden || achievementNotificationQueue.Count == 0)
            {
                return;
            }

            EnsureAchievementNotificationReferences();
            WireAchievementNotificationButton();
            if (achievementNotificationPanel == null)
            {
                return;
            }

            (string description, string reward) = achievementNotificationQueue.Dequeue();
            SetText(achievementNotificationGoalText, description);
            if (achievementNotificationRewardText != null)
            {
                bool hasReward = !string.IsNullOrEmpty(reward);
                SetActive(achievementNotificationRewardText.gameObject, hasReward);
                if (hasReward)
                {
                    SetText(achievementNotificationRewardText, reward);
                }
            }

            SetActive(achievementNotificationPanel, true);
            achievementNotificationPanel.transform.SetAsLastSibling();

            if (achievementNotificationRect == null)
            {
                achievementNotificationTimer = achievementNotificationVisibleSeconds;
                achievementNotificationState = AchievementNotificationState.Showing;
                return;
            }

            RecalculateAchievementNotificationPositions();
            achievementNotificationRect.anchoredPosition = achievementNotificationEnterPosition;
            achievementNotificationTimer = 0f;
            achievementNotificationState = AchievementNotificationState.SlidingIn;
        }

        private void TickAchievementNotification()
        {
            if (suppressAchievementNotifications)
            {
                HideAchievementNotificationImmediate(clearQueue: true);
                return;
            }

            if (achievementNotificationState == AchievementNotificationState.Hidden)
            {
                TryShowNextAchievementNotification();
                return;
            }

            if (achievementNotificationPanel == null)
            {
                achievementNotificationState = AchievementNotificationState.Hidden;
                return;
            }

            float slideSeconds = Mathf.Max(0.01f, achievementNotificationSlideSeconds);
            switch (achievementNotificationState)
            {
                case AchievementNotificationState.SlidingIn:
                    achievementNotificationTimer += Time.deltaTime;
                    SetAchievementNotificationPosition(achievementNotificationEnterPosition, achievementNotificationHomePosition, achievementNotificationTimer / slideSeconds);
                    if (achievementNotificationTimer >= slideSeconds)
                    {
                        achievementNotificationTimer = achievementNotificationVisibleSeconds;
                        achievementNotificationState = AchievementNotificationState.Showing;
                    }

                    break;
                case AchievementNotificationState.Showing:
                    achievementNotificationTimer -= Time.deltaTime;
                    if (achievementNotificationTimer <= 0f)
                    {
                        BeginAchievementNotificationSlideOut();
                    }

                    break;
                case AchievementNotificationState.SlidingOut:
                    achievementNotificationTimer += Time.deltaTime;
                    SetAchievementNotificationPosition(achievementNotificationSlideOutStartPosition, achievementNotificationExitPosition, achievementNotificationTimer / slideSeconds);
                    if (achievementNotificationTimer >= slideSeconds)
                    {
                        SetActive(achievementNotificationPanel, false);
                        achievementNotificationState = AchievementNotificationState.Hidden;
                        TryShowNextAchievementNotification();
                    }

                    break;
            }
        }

        private void HideAchievementNotificationImmediate(bool clearQueue)
        {
            if (clearQueue)
            {
                achievementNotificationQueue.Clear();
            }

            achievementNotificationTimer = 0f;
            achievementNotificationState = AchievementNotificationState.Hidden;
            SetActive(achievementNotificationPanel, false);
        }

        private void SetAchievementNotificationPosition(Vector2 from, Vector2 to, float normalized)
        {
            if (achievementNotificationRect == null)
            {
                return;
            }

            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalized));
            achievementNotificationRect.anchoredPosition = Vector2.Lerp(from, to, t);
        }

        private void DismissAchievementNotification()
        {
            if (achievementNotificationState == AchievementNotificationState.Hidden)
            {
                return;
            }

            BeginAchievementNotificationSlideOut();
        }

        private void BeginAchievementNotificationSlideOut()
        {
            if (achievementNotificationPanel == null)
            {
                achievementNotificationState = AchievementNotificationState.Hidden;
                return;
            }

            achievementNotificationSlideOutStartPosition = achievementNotificationRect != null
                ? achievementNotificationRect.anchoredPosition
                : achievementNotificationHomePosition;
            achievementNotificationTimer = 0f;
            achievementNotificationState = AchievementNotificationState.SlidingOut;
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
            Button scalingFrequencyButton,
            Button profitableEndingButton,
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
            if (nextBlocksPanelRoot == null && nextRoot != null)
            {
                nextBlocksPanelRoot = nextRoot.parent as RectTransform;
            }

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
            scalingFrequencyUpgradeButton = scalingFrequencyButton;
            profitableEndingUpgradeButton = profitableEndingButton;
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

        private void PrepareGlobalUiLayering()
        {
            // Full-screen overlays are reparented under the Canvas root so they always draw above
            // the bottom bar / status bar, no matter which panel they were authored under. The Game
            // Over overlay intentionally stays where it is: during game over the bottom bar must
            // remain usable (menu browsing, auto-restart UI).
            ReparentOverlayToCanvas(gameplayInfoOverlay);
            ReparentOverlayToCanvas(solverInfoOverlay);
            ReparentOverlayToCanvas(researchDetailModal);
            ReparentOverlayToCanvas(ppoModeOverlay);
            ReparentOverlayToCanvas(prestigeResetModal);
            ReparentOverlayToCanvas(offlineProgressOverlay);

            // Clicking the dimmed backdrop closes these. The Offline Progress and Game Over
            // overlays deliberately get NO backdrop close; the PPO mode overlay and the research
            // popup already ship their own backdrop buttons.
            AddOverlayBackdropClose(gameplayInfoOverlay, CloseGameplayInfo);
            AddOverlayBackdropClose(solverInfoOverlay, HideSolverInfoModal);
            AddOverlayBackdropClose(prestigeResetModal, ClosePrestigeResetModal);
        }

        // Adds (or rewires) a full-stretch invisible button as the FIRST child of the overlay, so
        // clicks on the dimmed area close it while clicks on the content card (a later sibling with
        // its own raycast target) never reach it.
        private static void AddOverlayBackdropClose(GameObject overlayRoot, UnityEngine.Events.UnityAction close)
        {
            if (overlayRoot == null)
            {
                return;
            }

            Transform existing = overlayRoot.transform.Find("Runtime Backdrop Close");
            GameObject backdropObject = existing != null ? existing.gameObject : null;
            if (backdropObject == null)
            {
                backdropObject = new GameObject("Runtime Backdrop Close", typeof(RectTransform), typeof(Image), typeof(Button));
                var rect = backdropObject.GetComponent<RectTransform>();
                rect.SetParent(overlayRoot.transform, false);
                rect.SetAsFirstSibling();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                Image image = backdropObject.GetComponent<Image>();
                image.color = Color.clear; // Invisible but still a raycast target.
                backdropObject.GetComponent<Button>().transition = Selectable.Transition.None;
            }

            Button button = backdropObject.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(close);
        }

        private void ReparentOverlayToCanvas(GameObject overlay)
        {
            if (overlay == null || canvas == null || overlay.transform.parent == canvas.transform)
            {
                return;
            }

            RectTransform rect = overlay.transform as RectTransform;
            // Full-stretch roots (dim + card containers) become full-screen under the canvas; a
            // fixed-size root keeps its on-screen placement instead of being blown up.
            bool fullStretch = rect != null && rect.anchorMin == Vector2.zero && rect.anchorMax == Vector2.one;
            if (fullStretch)
            {
                overlay.transform.SetParent(canvas.transform, false);
                Stretch(rect);
            }
            else
            {
                overlay.transform.SetParent(canvas.transform, true);
            }
        }

        private void EnsureOptionalUpgradeButtonReferences()
        {
            if (canvas == null)
            {
                return;
            }

            if (scalingFrequencyUpgradeButton == null)
            {
                scalingFrequencyUpgradeButton = FindButtonByLooseName("Scaling Frequency", "Higher Frequency");
            }

            if (profitableEndingUpgradeButton == null)
            {
                profitableEndingUpgradeButton = FindButtonByLooseName("Profitable Ending", "Ending Profit");
            }

            if (passiveYieldUpgradeButton == null)
            {
                passiveYieldUpgradeButton = FindButtonByLooseName("Passive Yield", "Passive Chips Tick");
            }

            if (passiveTickRateUpgradeButton == null)
            {
                passiveTickRateUpgradeButton = FindButtonByLooseName("Passive Tick Rate");
            }

            if (activeMultiplierUpgradeButton == null)
            {
                activeMultiplierUpgradeButton = FindButtonByLooseName("Active Multiplier");
            }

            if (computeSpeedUpgradeButton == null)
            {
                computeSpeedUpgradeButton = FindButtonByLooseName("Compute Speed", "Solver Delay");
            }

            if (comboEngineUpgradeButton == null)
            {
                comboEngineUpgradeButton = FindButtonByLooseName("Combo Engine", "Merge Streak");
            }

            if (salvageProtocolUpgradeButton == null)
            {
                salvageProtocolUpgradeButton = FindButtonByLooseName("Salvage Protocol", "Score Salvage");
            }

            if (tokenDividendUpgradeButton == null)
            {
                tokenDividendUpgradeButton = FindButtonByLooseName("Token Dividend", "Sqrt Chips");
            }

            if (curriculumRateUpgradeButton == null)
            {
                curriculumRateUpgradeButton = FindButtonByLooseName("Curriculum Rate");
            }

            if (curriculumAmountUpgradeButton == null)
            {
                // Match the amount button by "Curriculum" but not the rate button we just resolved.
                Button curriculum = FindButtonByLooseName("Curriculum");
                if (curriculum != curriculumRateUpgradeButton)
                {
                    curriculumAmountUpgradeButton = curriculum;
                }
            }
        }

        private Button FindButtonByLooseName(params string[] expectedNames)
        {
            if (expectedNames == null || expectedNames.Length == 0)
            {
                return null;
            }

            string[] normalizedExpectedNames = expectedNames
                .Select(NormalizeLookupName)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToArray();
            foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            {
                if (button == null)
                {
                    continue;
                }

                string normalizedButtonName = NormalizeLookupName(button.name);
                if (normalizedExpectedNames.Any(expected => normalizedButtonName == expected || normalizedButtonName.Contains(expected)))
                {
                    return button;
                }

                StackMergeButtonLabelPair labels = button.GetComponent<StackMergeButtonLabelPair>();
                string normalizedLabel = labels?.nameText != null ? NormalizeLookupName(labels.nameText.text) : string.Empty;
                if (normalizedExpectedNames.Any(expected => normalizedLabel == expected || normalizedLabel.Contains(expected)))
                {
                    return button;
                }
            }

            return null;
        }

        private static string NormalizeLookupName(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : new string(value.Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
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
            EnsureGameplayModifierReferences();
            for (int i = 0; i < stackButtons.Length; i++)
            {
                int stackIndex = i;
                if (stackButtons[i] == null)
                {
                    continue;
                }

                stackButtons[i].onClick.RemoveAllListeners();
                stackButtons[i].onClick.AddListener(() => HandleStackButtonClick(stackIndex));
            }

            WireGameplayModifierButton(gameplayMinersPickaxeButton, ModifierId.MinersPickaxe);
            WireGameplayModifierButton(gameplayQueueScrubberButton, ModifierId.QueueScrubber);

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

            WireAchievementPanelTabs();

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

            WireSettingsControls();

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

            if (computeSpeedUpgradeButton != null)
            {
                computeSpeedUpgradeButton.onClick.RemoveAllListeners();
                computeSpeedUpgradeButton.onClick.AddListener(BuyComputeSpeedUpgrade);
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

            if (scalingFrequencyUpgradeButton != null)
            {
                scalingFrequencyUpgradeButton.onClick.RemoveAllListeners();
                scalingFrequencyUpgradeButton.onClick.AddListener(BuyScalingFrequencyUpgrade);
            }

            if (comboEngineUpgradeButton != null)
            {
                comboEngineUpgradeButton.onClick.RemoveAllListeners();
                comboEngineUpgradeButton.onClick.AddListener(BuyComboEngineUpgrade);
            }

            if (salvageProtocolUpgradeButton != null)
            {
                salvageProtocolUpgradeButton.onClick.RemoveAllListeners();
                salvageProtocolUpgradeButton.onClick.AddListener(BuySalvageProtocolUpgrade);
            }

            if (tokenDividendUpgradeButton != null)
            {
                tokenDividendUpgradeButton.onClick.RemoveAllListeners();
                tokenDividendUpgradeButton.onClick.AddListener(BuyTokenDividendUpgrade);
            }

            if (curriculumAmountUpgradeButton != null)
            {
                curriculumAmountUpgradeButton.onClick.RemoveAllListeners();
                curriculumAmountUpgradeButton.onClick.AddListener(BuyCurriculumAmountUpgrade);
            }

            if (curriculumRateUpgradeButton != null)
            {
                curriculumRateUpgradeButton.onClick.RemoveAllListeners();
                curriculumRateUpgradeButton.onClick.AddListener(BuyCurriculumRateUpgrade);
            }

            if (profitableEndingUpgradeButton != null)
            {
                profitableEndingUpgradeButton.onClick.RemoveAllListeners();
                profitableEndingUpgradeButton.onClick.AddListener(BuyProfitableEndingUpgrade);
            }

            if (passiveYieldUpgradeButton != null)
            {
                passiveYieldUpgradeButton.onClick.RemoveAllListeners();
                passiveYieldUpgradeButton.onClick.AddListener(BuyPassiveYieldUpgrade);
            }

            if (passiveTickRateUpgradeButton != null)
            {
                passiveTickRateUpgradeButton.onClick.RemoveAllListeners();
                passiveTickRateUpgradeButton.onClick.AddListener(BuyPassiveTickRateUpgrade);
            }

            if (activeMultiplierUpgradeButton != null)
            {
                activeMultiplierUpgradeButton.onClick.RemoveAllListeners();
                activeMultiplierUpgradeButton.onClick.AddListener(BuyActiveMultiplierUpgrade);
            }

            if (modifiersMenuUnlockButton != null)
            {
                modifiersMenuUnlockButton.onClick.RemoveAllListeners();
                modifiersMenuUnlockButton.onClick.AddListener(BuyStageProgressionUnlock);
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
                SetActive(agentsMenuUnlockButton.gameObject, false);
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
                prestigeButton.onClick.AddListener(OpenPrestigeResetModal);
            }

            // The hand-built Research grid uses StackMergeResearchCard.button references wired in
            // WireButtons(). The legacy researchButtons array often points at those same Button
            // components, so rewiring it here would remove OpenResearchDetail and leave clicks only
            // refreshing hidden detail text.
            if (!HasAssignedResearchCardButtons())
            {
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
            }

            if (researchDetailActionButton != null)
            {
                researchDetailActionButton.onClick.RemoveAllListeners();
                researchDetailActionButton.onClick.AddListener(BuySelectedResearchUpgrade);
            }
        }

        private void EnsureGameplayModifierReferences()
        {
            if (gameplayPanel == null)
            {
                return;
            }

            Transform modifiersRoot = gameplayModifiersSection != null
                ? gameplayModifiersSection.transform
                : FindGameplaySection("Modifiers");
            if (modifiersRoot == null)
            {
                return;
            }

            gameplayModifiersSection = modifiersRoot.gameObject;
            EnsureGameplayModifierButtonReferences(
                modifiersRoot,
                "MinersPickaxe",
                ref gameplayMinersPickaxeButton,
                ref gameplayMinersPickaxeAmountText);
            EnsureGameplayModifierButtonReferences(
                modifiersRoot,
                "QueueScrubber",
                ref gameplayQueueScrubberButton,
                ref gameplayQueueScrubberAmountText);
        }

        private Transform FindGameplaySection(string sectionName)
        {
            if (gameplayPanel == null)
            {
                return null;
            }

            Transform sections = FindNamedDescendant(gameplayPanel.transform, "Sections");
            if (sections != null)
            {
                foreach (Transform child in sections)
                {
                    if (child.name == sectionName)
                    {
                        return child;
                    }
                }
            }

            return FindNamedDescendant(gameplayPanel.transform, sectionName);
        }

        private static void EnsureGameplayModifierButtonReferences(
            Transform root,
            string buttonName,
            ref Button button,
            ref TMP_Text amountText)
        {
            Transform buttonTransform = button != null ? button.transform : FindNamedDescendant(root, buttonName);
            if (buttonTransform == null)
            {
                return;
            }

            if (button == null)
            {
                button = buttonTransform.GetComponent<Button>();
            }

            if (amountText == null)
            {
                Transform amountTransform = FindNamedDescendant(buttonTransform, "AmountText");
                amountText = amountTransform != null
                    ? amountTransform.GetComponent<TMP_Text>()
                    : buttonTransform.GetComponentInChildren<TMP_Text>(true);
            }
        }

        private void WireGameplayModifierButton(Button button, ModifierId modifierId)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ToggleGameplayModifier(modifierId));
        }

        private void EnsureSettingsReferences()
        {
            Transform settingsRoot = settingsPanel != null ? settingsPanel.transform : canvas != null ? canvas.transform : null;
            Transform canvasRoot = canvas != null ? canvas.transform : settingsRoot;

            showFpsToggle ??= FindComponentByNormalizedName<Toggle>(settingsRoot, "ShowFps", "ShowFPSToggle", "FpsToggle");
            suppressAchievementNotificationToggle ??= FindComponentByNormalizedName<Toggle>(
                settingsRoot,
                "AchievementPopup",
                "AchievementPopip",
                "GoalNotification",
                "AchievementNotification",
                "HideAchievementPopup",
                "DisableAchievementPopup");
            testingPpoFramesToggle ??= FindComponentByNormalizedName<Toggle>(
                settingsRoot,
                "TestingPpoFrames",
                "TestingPPOFrames",
                "PpoFramesTesting",
                "PPOFramesTesting");
            x3IncomeToggle ??= FindComponentByNormalizedName<Toggle>(
                settingsRoot,
                "X3Income",
                "TripleIncome",
                "TestingIncome",
                "TestingTripleIncome");
            languageDropdown ??= FindComponentByNormalizedName<TMP_Dropdown>(settingsRoot, "Language", "LanguageDropdown");
            blockNumeralDropdown ??= FindComponentByNormalizedName<TMP_Dropdown>(settingsRoot, "Numeral", "BlockNumeral", "NumeralDropdown");
            fpsText ??= FindComponentByNormalizedName<TMP_Text>(canvasRoot, "FPSText", "FpsText", "FPS");
            howToPlayPanel ??= FindAnyObjectByType<StackMergeHowToPlayPanel>(FindObjectsInactive.Include);
            menuHelpOverlay ??= FindAnyObjectByType<StackMergeHelpOverlay>(FindObjectsInactive.Include);
        }

        private static T FindComponentByNormalizedName<T>(Transform root, params string[] names) where T : Component
        {
            if (root == null || names == null || names.Length == 0)
            {
                return null;
            }

            string[] normalizedNames = names
                .Select(NormalizeLookupName)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToArray();
            if (normalizedNames.Length == 0)
            {
                return null;
            }

            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                string normalizedObjectName = NormalizeLookupName(component.gameObject.name);
                if (normalizedNames.Any(name => normalizedObjectName == name || normalizedObjectName.Contains(name)))
                {
                    return component;
                }
            }

            return null;
        }

        private void LoadPlayerSettings()
        {
            showFps = PlayerPrefs.GetInt(ShowFpsSettingKey, 0) == 1;
            suppressAchievementNotifications = PlayerPrefs.GetInt(SuppressAchievementNotificationSettingKey, 0) == 1;
            testingPpoFrames = PlayerPrefs.GetInt(TestingPpoFramesSettingKey, 0) == 1;
            testingTripleIncome = PlayerPrefs.GetInt(TestingTripleIncomeSettingKey, 0) == 1;
            StackMergeProgression.DebugFastPpoFrames = testingPpoFrames;
            StackMergeProgression.DebugTripleIncome = testingTripleIncome;
            currentLanguage = PlayerPrefs.GetInt(LanguageSettingKey, 0) == 1
                ? StackMergeLanguage.Magyar
                : StackMergeLanguage.English;
            StackMergeLocalization.CurrentLanguage = currentLanguage;
            blockNumeralStyle = (BlockNumeralStyle)Mathf.Clamp(PlayerPrefs.GetInt(BlockNumeralSettingKey, 0), 0, 5);
        }

        private void WireSettingsControls()
        {
            EnsureSettingsReferences();
            ConfigureLanguageDropdownFromScene();

            if (showFpsToggle != null)
            {
                showFpsToggle.onValueChanged.RemoveAllListeners();
                showFpsToggle.onValueChanged.AddListener(SetShowFps);
            }

            if (suppressAchievementNotificationToggle != null)
            {
                suppressAchievementNotificationToggle.onValueChanged.RemoveAllListeners();
                suppressAchievementNotificationToggle.onValueChanged.AddListener(SetSuppressAchievementNotifications);
            }

            if (testingPpoFramesToggle != null)
            {
                testingPpoFramesToggle.onValueChanged.RemoveAllListeners();
                testingPpoFramesToggle.onValueChanged.AddListener(SetTestingPpoFrames);
            }

            if (x3IncomeToggle != null)
            {
                x3IncomeToggle.onValueChanged.RemoveAllListeners();
                x3IncomeToggle.onValueChanged.AddListener(SetTestingTripleIncome);
            }

            if (languageDropdown != null)
            {
                languageDropdown.onValueChanged.RemoveAllListeners();
                languageDropdown.onValueChanged.AddListener(SetLanguageFromDropdown);
            }

            if (blockNumeralDropdown != null)
            {
                blockNumeralDropdown.onValueChanged.RemoveAllListeners();
                blockNumeralDropdown.onValueChanged.AddListener(SetBlockNumeralFromDropdown);
            }
        }

        private void SyncSettingsControls()
        {
            EnsureSettingsReferences();
            syncingSettingsControls = true;

            if (showFpsToggle != null)
            {
                showFpsToggle.SetIsOnWithoutNotify(showFps);
            }

            if (suppressAchievementNotificationToggle != null)
            {
                suppressAchievementNotificationToggle.SetIsOnWithoutNotify(suppressAchievementNotifications);
            }

            if (testingPpoFramesToggle != null)
            {
                testingPpoFramesToggle.SetIsOnWithoutNotify(testingPpoFrames);
            }

            if (x3IncomeToggle != null)
            {
                x3IncomeToggle.SetIsOnWithoutNotify(testingTripleIncome);
            }

            RefreshBlockNumeralDropdown();

            if (languageDropdown != null)
            {
                ConfigureLanguageDropdownFromScene();
                int languageIndex = GetLanguageDropdownIndex(currentLanguage);
                languageDropdown.SetValueWithoutNotify(languageIndex);
                lastLanguageDropdownValue = languageIndex;
                languageDropdown.RefreshShownValue();
                ApplyDropdownSelectionBackground(languageDropdown);
            }

            syncingSettingsControls = false;
        }

        private void ConfigureLanguageDropdownFromScene()
        {
            if (languageDropdown == null)
            {
                return;
            }

            ConfigureDropdownTextReferences(languageDropdown);
            if (languageDropdown.options.Count != 2
                || !IsLanguageOption(languageDropdown.options[0].text, StackMergeLanguage.English)
                || !IsLanguageOption(languageDropdown.options[1].text, StackMergeLanguage.Magyar))
            {
                languageDropdown.options = new List<TMP_Dropdown.OptionData>
                {
                    new("English"),
                    new("Magyar")
                };
            }

            languageDropdown.RefreshShownValue();
            ApplyDropdownSelectionBackground(languageDropdown);
        }

        private void ConfigureDropdownTextReferences(TMP_Dropdown dropdown)
        {
            if (dropdown == null)
            {
                return;
            }

            if (dropdown.captionText == null || !dropdown.captionText.transform.IsChildOf(dropdown.transform))
            {
                Transform label = FindNamedDescendant(dropdown.transform, "Label");
                dropdown.captionText = label != null ? label.GetComponent<TMP_Text>() : null;
            }

            if (dropdown.template == null)
            {
                return;
            }

            bool itemTextInvalid = dropdown.itemText == null || !dropdown.itemText.transform.IsChildOf(dropdown.template);
            if (!itemTextInvalid)
            {
                return;
            }

            Transform itemLabel = FindNamedDescendant(dropdown.template, "Item Label");
            if (itemLabel != null)
            {
                dropdown.itemText = itemLabel.GetComponent<TMP_Text>();
                return;
            }

            Toggle itemToggle = dropdown.template.GetComponentInChildren<Toggle>(true);
            dropdown.itemText = itemToggle != null ? itemToggle.GetComponentInChildren<TMP_Text>(true) : null;
        }

        private int GetLanguageDropdownIndex(StackMergeLanguage language)
        {
            if (languageDropdown != null)
            {
                for (int i = 0; i < languageDropdown.options.Count; i++)
                {
                    if (IsLanguageOption(languageDropdown.options[i].text, language))
                    {
                        return i;
                    }
                }

                if (languageDropdown.options.Count > 0)
                {
                    return language == StackMergeLanguage.Magyar
                        ? Mathf.Min(1, languageDropdown.options.Count - 1)
                        : 0;
                }
            }

            return 0;
        }

        private StackMergeLanguage ResolveLanguageDropdownSelection(int index)
        {
            if (languageDropdown != null && index >= 0 && index < languageDropdown.options.Count)
            {
                if (IsLanguageOption(languageDropdown.options[index].text, StackMergeLanguage.Magyar))
                {
                    return StackMergeLanguage.Magyar;
                }

                if (IsLanguageOption(languageDropdown.options[index].text, StackMergeLanguage.English))
                {
                    return StackMergeLanguage.English;
                }
            }

            return index == GetLanguageDropdownIndex(StackMergeLanguage.Magyar)
                ? StackMergeLanguage.Magyar
                : StackMergeLanguage.English;
        }

        private static bool IsLanguageOption(string optionText, StackMergeLanguage language)
        {
            string optionName = NormalizeLookupName(optionText);
            return language == StackMergeLanguage.Magyar
                ? optionName == "magyar"
                : optionName == "english" || optionName == "angol";
        }

        private void TickDropdownSelectionVisuals()
        {
            ApplyDropdownSelectionBackground(languageDropdown);
            ApplyDropdownSelectionBackground(blockNumeralDropdown);
        }

        private void ApplyDropdownSelectionBackground(TMP_Dropdown dropdown)
        {
            if (dropdown == null)
            {
                return;
            }

            foreach (Toggle item in dropdown.GetComponentsInChildren<Toggle>(true))
            {
                if (dropdown.template != null && item.transform.IsChildOf(dropdown.template))
                {
                    continue;
                }

                Image background = GetDropdownItemBackground(item);
                if (background == null)
                {
                    continue;
                }

                if (!dropdownItemBackgroundDefaults.ContainsKey(background))
                {
                    dropdownItemBackgroundDefaults[background] = background.color;
                }

                background.color = item.isOn
                    ? DropdownSelectedBackgroundColor
                    : dropdownItemBackgroundDefaults[background];
            }
        }

        private static Image GetDropdownItemBackground(Toggle item)
        {
            if (item == null)
            {
                return null;
            }

            Transform background = FindNamedDescendant(item.transform, "Item Background");
            if (background != null && background.TryGetComponent(out Image backgroundImage))
            {
                return backgroundImage;
            }

            return item.targetGraphic as Image ?? item.GetComponent<Image>();
        }

        private void TickLanguageDropdownSelection()
        {
            if (languageDropdown == null || syncingSettingsControls)
            {
                return;
            }

            int value = languageDropdown.value;
            if (value == lastLanguageDropdownValue)
            {
                return;
            }

            SetLanguageFromDropdown(value);
        }

        private void ApplyPlayerSettings()
        {
            StackMergeLocalization.CurrentLanguage = currentLanguage;
            StackMergeProgression.DebugFastPpoFrames = testingPpoFrames;
            StackMergeProgression.DebugTripleIncome = testingTripleIncome;
            ApplyFpsVisibility();
            ApplySettingsPanelLocalization();
            ApplyStaticSceneLocalization();
            if (suppressAchievementNotifications)
            {
                HideAchievementNotificationImmediate(clearQueue: true);
            }
        }

        private void SetShowFps(bool value)
        {
            if (syncingSettingsControls)
            {
                return;
            }

            showFps = value;
            PlayerPrefs.SetInt(ShowFpsSettingKey, showFps ? 1 : 0);
            PlayerPrefs.Save();
            ApplyFpsVisibility();
        }

        private void SetSuppressAchievementNotifications(bool value)
        {
            if (syncingSettingsControls)
            {
                return;
            }

            suppressAchievementNotifications = value;
            PlayerPrefs.SetInt(SuppressAchievementNotificationSettingKey, suppressAchievementNotifications ? 1 : 0);
            PlayerPrefs.Save();
            if (suppressAchievementNotifications)
            {
                HideAchievementNotificationImmediate(clearQueue: true);
            }
        }

        private void SetTestingPpoFrames(bool value)
        {
            if (syncingSettingsControls)
            {
                return;
            }

            testingPpoFrames = value;
            StackMergeProgression.DebugFastPpoFrames = testingPpoFrames;
            PlayerPrefs.SetInt(TestingPpoFramesSettingKey, testingPpoFrames ? 1 : 0);
            PlayerPrefs.Save();
            RefreshEverything();
        }

        private void SetTestingTripleIncome(bool value)
        {
            if (syncingSettingsControls)
            {
                return;
            }

            testingTripleIncome = value;
            StackMergeProgression.DebugTripleIncome = testingTripleIncome;
            PlayerPrefs.SetInt(TestingTripleIncomeSettingKey, testingTripleIncome ? 1 : 0);
            PlayerPrefs.Save();
            RefreshHud();
            RefreshProgressionUi();
        }

        private void SetLanguageFromDropdown(int index)
        {
            if (syncingSettingsControls)
            {
                return;
            }

            currentLanguage = ResolveLanguageDropdownSelection(index);
            lastLanguageDropdownValue = index;
            StackMergeLocalization.CurrentLanguage = currentLanguage;
            PlayerPrefs.SetInt(LanguageSettingKey, currentLanguage == StackMergeLanguage.Magyar ? 1 : 0);
            PlayerPrefs.Save();
            ApplyLanguageToUi();
            RefreshBlockNumeralDropdown();
            ApplyDropdownSelectionBackground(languageDropdown);
        }

        private void ApplyLanguageToUi()
        {
            if (currentLanguage == StackMergeLanguage.English)
            {
                ApplyStaticSceneLocalization();
            }

            if (progression != null)
            {
                RefreshEverything();
            }
            else
            {
                RefreshHud();
            }

            ApplySettingsPanelLocalization();
            ApplyStaticSceneLocalization();
            howToPlayPanel?.RefreshContent();
            RefreshTabButtons();
            RefreshBottomMenuIconStates();
            SyncSettingsControls();
        }

        private void ApplySettingsPanelLocalization()
        {
            if (settingsPanel == null)
            {
                return;
            }

            foreach (TMP_Text text in settingsPanel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text == null
                    || text == fpsText
                    || (languageDropdown != null && text.transform.IsChildOf(languageDropdown.transform))
                    || (blockNumeralDropdown != null && text.transform.IsChildOf(blockNumeralDropdown.transform)))
                {
                    continue;
                }

                if (!settingsStaticTextDefaults.ContainsKey(text))
                {
                    settingsStaticTextDefaults[text] = text.text;
                }

                SetText(text, settingsStaticTextDefaults[text]);
            }
        }

        private void ApplyStaticSceneLocalization()
        {
            if (canvas == null)
            {
                return;
            }

            foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                if (ShouldSkipStaticSceneLocalization(text))
                {
                    continue;
                }

                if (!staticLocalizedTextDefaults.TryGetValue(text, out string original))
                {
                    original = text.text;
                    staticLocalizedTextDefaults[text] = original;
                }

                text.text = StackMergeLocalization.Translate(original);
            }
        }

        private bool ShouldSkipStaticSceneLocalization(TMP_Text text)
        {
            if (text == null
                || text == fpsText
                || text == scoreText
                || text == bestText
                || text == highestText
                || text == droppedText
                || text == feedbackText
                || text == tokensText
                || text == solverText
                || text == speedText
                || text == capacityText
                || text == queueText
                || text == runStatusText
                || text == agentSlotsText
                || text == solverDetailNameText
                || text == solverDetailInfoText
                || text == solverDetailStatusText
                || text == solverTuneTitleText
                || text == solverTuneSummaryText
                || text == modifierSummaryText
                || text == modifierDetailNameText
                || text == modifierDetailInfoText
                || text == modifierDetailStatusText
                || text == agentDetailNameText
                || text == agentDetailInfoText
                || text == agentDetailStatusText
                || text == researchDetailNameText
                || text == researchDetailInfoText
                || text == researchDetailStatusText
                || text == prestigeSummaryText
                || text == prestigeResetTrainingText
                || text == prestigeResetInfoText
                || text == prestigeResetAmountText
                || text == offlineGainText
                || text == offlineTimeText
                || text == historySummaryText
                || text == historyHighestText
                || text == historyHighestMedianText
                || text == historyAverageRunTimeText
                || text == historyPlaytimeText
                || text == historyInsightText
                || text == achievementStatsText
                || text == achievementRunsText
                || text == achievementBestRunText
                || text == achievementMovesText
                || text == achievementMergesText
                || text == achievementHighestText
                || text == achievementEarnedText
                || text == achievementCompletedGoalsText
                || text == secretAchievementCompletedGoalsText
                || text == achievementResetsText
                || text == achievementPlaytimeText
                || text == ppoProgressTotalFramesText
                || text == ppoProgressAverageFramesPerPrestigeText
                || text == ppoProgressTotalRunsText
                || text == ppoProgressBestRunText
                || text == ppoProgressTotalMovesText
                || text == ppoProgressTotalMergesText
                || text == ppoProgressKnowledgeText
                || text == ppoProgressPlaytimeText
                || text == gameplayInfoText
                || text == achievementNotificationGoalText
                || text == gameOverScoreText
                || text == gameOverBestText
                || text == gameOverRunTimeText)
            {
                return true;
            }

            if (languageDropdown != null && text.transform.IsChildOf(languageDropdown.transform))
            {
                return true;
            }

            if (blockNumeralDropdown != null && text.transform.IsChildOf(blockNumeralDropdown.transform))
            {
                return true;
            }

            if (settingsPanel != null && text.transform.IsChildOf(settingsPanel.transform))
            {
                return true;
            }

            if (ContainsText(chipsTexts, text)
                || ContainsText(insightsTexts, text)
                || ContainsText(solverTuneNameTexts, text)
                || ContainsText(solverTuneValueTexts, text)
                || ContainsText(solverTuneDescriptionTexts, text)
                || ContainsText(agentSlotTexts, text))
            {
                return true;
            }

            return text.GetComponentInParent<StackMergeGoalRow>(true) != null
                || text.GetComponentInParent<StackMergeRecentRunRow>(true) != null
                || text.GetComponentInParent<StackMergeSolverStatRow>(true) != null
                || text.GetComponentInParent<StackMergeAlgorithmCard>(true) != null
                || text.GetComponentInParent<StackMergeAgentCard>(true) != null
                || text.GetComponentInParent<StackMergeAgentSlot>(true) != null
                || text.GetComponentInParent<StackMergeModifierCard>(true) != null
                || text.GetComponentInParent<StackMergeResearchCard>(true) != null
                || text.GetComponentInParent<StackMergeTuneButtonRow>(true) != null
                || text.GetComponentInParent<StackMergeTuneSliderRow>(true) != null
                || text.GetComponentInParent<StackMergeButtonLabelPair>(true) != null;
        }

        private static bool ContainsText(TMP_Text[] texts, TMP_Text text)
        {
            return texts != null && Array.IndexOf(texts, text) >= 0;
        }

        private void ApplyFpsVisibility()
        {
            if (fpsText == null)
            {
                return;
            }

            SetActive(fpsText.gameObject, showFps);
            if (showFps)
            {
                fpsSampleTimer = 0f;
                fpsSampleFrames = 0;
                SetText(fpsText, "FPS --");
            }
        }

        private void TickFpsDisplay()
        {
            if (!showFps || fpsText == null)
            {
                return;
            }

            if (!fpsText.gameObject.activeSelf)
            {
                fpsText.gameObject.SetActive(true);
            }

            fpsSampleFrames++;
            fpsSampleTimer += Time.unscaledDeltaTime;
            if (fpsSampleTimer < 0.25f)
            {
                return;
            }

            float fps = fpsSampleTimer > 0f ? fpsSampleFrames / fpsSampleTimer : 0f;
            SetText(fpsText, $"{Mathf.RoundToInt(fps)} FPS");
            fpsSampleTimer = 0f;
            fpsSampleFrames = 0;
        }

        // Passive Production ticks independent of moves — it's the point of the upgrade family.
        // "Actively playing" only gates the Active Multiplier bonus, not the base tick itself.
        private void TickPassiveProduction()
        {
            if (progression == null)
            {
                return;
            }

            timeSinceLastAcceptedMove += Time.deltaTime;
            bool isActivelyPlaying = gameState != null
                && !gameState.IsGameOver
                && timeSinceLastAcceptedMove <= ActivePlayWindowSeconds;

            long gained = progression.TickPassiveProduction(Time.deltaTime, isActivelyPlaying);
            if (gained > 0)
            {
                RefreshHud();
            }

            // PPO Curriculum only accrues while actively in PPO Training mode (guarded inside).
            progression.TickCurriculum(Time.deltaTime);
        }

        private bool HasAssignedResearchCardButtons()
        {
            if (researchCards == null)
            {
                return false;
            }

            foreach (StackMergeResearchCard card in researchCards)
            {
                if (card != null && card.button != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void SelectTab(int tabIndex)
        {
            CloseHowToPlayPanel();
            historyOpen = false;
            achievementsOpen = false;
            solverTuneOpen = false;
            gameplayInfoOpen = false;
            int requestedTab = Mathf.Clamp(tabIndex, 0, 7);
            if (requestedTab == 3 && progression != null && !progression.AgentsMenuUnlocked)
            {
                ShowFeedbackModal("Unlock Agents in Upgrades first");
                requestedTab = selectedTabIndex;
            }

            if (requestedTab == 4 && progression != null && !progression.ModifiersMenuUnlocked)
            {
                ShowFeedbackModal("Unlock Modifier Lab in Upgrades first");
                requestedTab = selectedTabIndex;
            }

            if (requestedTab == 5 && progression != null && !IsResearchMenuUnlocked())
            {
                ShowFeedbackModal(progression.IsSolverUnlocked(SolverId.MachineLearning) ? "Finish PPO Training first" : "Unlock PPO to open Research");
                requestedTab = selectedTabIndex;
            }

            if (requestedTab == 7 && progression != null && !progression.DatacenterUnlocked)
            {
                ShowFeedbackModal($"Prestige {StackMergeProgression.DatacenterUnlockPrestigeCount} times to unlock the Datacenter");
                requestedTab = selectedTabIndex;
            }

            selectedTabIndex = requestedTab;
            if (selectedTabIndex != 0)
            {
                ClearArmedGameplayModifier();
                if (gameplayLayoutWarmupCoroutine != null)
                {
                    StopCoroutine(gameplayLayoutWarmupCoroutine);
                    gameplayLayoutWarmupCoroutine = null;
                }
            }

            SetActive(gameplayPanel, selectedTabIndex == 0);
            SetActive(algorithmsPanel, selectedTabIndex == 1);
            SetActive(upgradesPanel, selectedTabIndex == 2);
            SetActive(agentsPanel, selectedTabIndex == 3);
            SetActive(modifiersPanel, selectedTabIndex == 4);
            SetActive(historyPanel, false);
            SetActive(achievementsPanel, false);
            SetActive(researchPanel, selectedTabIndex == 5);
            SetActive(settingsPanel, selectedTabIndex == 6);
            if (selectedTabIndex == 6)
            {
                // Unlock states may have changed since the last visit — rebuild the numeral options.
                RefreshBlockNumeralDropdown();
            }

            SetActive(datacenterPanel, selectedTabIndex == 7);
            if (selectedTabIndex == 7)
            {
                RefreshDatacenterPanel();
            }
            SetActive(solverTunePanel, false);
            SetActive(gameplayInfoOverlay, false);
            SetActive(researchDetailModal, false);
            // These overlays live at canvas level (PrepareGlobalUiLayering), so panel switches no
            // longer hide them implicitly — close them explicitly.
            SetActive(solverInfoOverlay, false);
            ClosePrestigeResetModal();
            HidePpoModeModal();
            RefreshTabButtons();

            // Update the board / training-overlay visibility immediately on tab change so the
            // PPO matrix view appears/disappears at once instead of lagging until the next move.
            RefreshGameView();
            RefreshProgressionUi();
            if (selectedTabIndex == 0)
            {
                ForceGameplayLayoutPass();
                ScheduleGameplayLayoutWarmup(TabGameplayLayoutWarmupFrames);
            }

            TryShowMenuHelpForSelectedTab();
        }

        private void OpenHistoryPanel()
        {
            CloseHowToPlayPanel();
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
            SetActive(datacenterPanel, false);
            SetActive(settingsPanel, false);
            SetActive(historyPanel, true);
            SetActive(solverTunePanel, false);
            SetActive(gameplayInfoOverlay, false);
            SetActive(researchDetailModal, false);
            SetActive(solverInfoOverlay, false);
            ClosePrestigeResetModal();
            HidePpoModeModal();
            FlushPlaytime();
            RefreshHistory();
            RefreshTabButtons();
            RefreshGameView();
            TryShowMenuHelp(StackMergeHowToPlayLayer.History);
        }

        private void CloseHistoryPanel()
        {
            SelectTab(0);
        }

        private void OpenAchievementsPanel()
        {
            CloseHowToPlayPanel();
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
            SetActive(datacenterPanel, false);
            SetActive(settingsPanel, false);
            SetActive(achievementsPanel, true);
            SetActive(solverTunePanel, false);
            SetActive(gameplayInfoOverlay, false);
            SetActive(researchDetailModal, false);
            selectedAchievementTab = defaultAchievementTab;
            WireAchievementPanelTabs();
            RefreshAchievements();
            RefreshTabButtons();
            RefreshGameView();
            TryShowMenuHelp(StackMergeHowToPlayLayer.Achievements);
        }

        private void CloseHowToPlayPanel()
        {
            if (howToPlayPanel == null)
            {
                howToPlayPanel = FindAnyObjectByType<StackMergeHowToPlayPanel>(FindObjectsInactive.Include);
            }

            howToPlayPanel?.Close();
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
            gameplayInfoOverlay?.transform.SetAsLastSibling();
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
            // Permanent once earned: buying PPO opens it, and after the first prestige (which requires
            // PPO) it STAYS open — prestige resets the solver unlocks, but Research is not stage-gated
            // and spends persistent Insight, so re-locking it would strand the player's Insight.
            return progression != null
                && (progression.PrestigeCount >= 1 || progression.IsSolverUnlocked(SolverId.MachineLearning));
        }

        public bool IsHowToPlayLayerUnlocked(StackMergeHowToPlayLayer layer)
        {
            if (progression == null)
            {
                return layer == StackMergeHowToPlayLayer.Gameplay
                    || layer == StackMergeHowToPlayLayer.Algorithms
                    || layer == StackMergeHowToPlayLayer.Upgrades
                    || layer == StackMergeHowToPlayLayer.History
                    || layer == StackMergeHowToPlayLayer.Achievements;
            }

            return layer switch
            {
                StackMergeHowToPlayLayer.Gameplay => true,
                StackMergeHowToPlayLayer.Algorithms => true,
                StackMergeHowToPlayLayer.Upgrades => true,
                StackMergeHowToPlayLayer.Agents => progression.LifetimeAgentsUnlocked,
                StackMergeHowToPlayLayer.Modifiers => progression.LifetimeModifiersUnlocked,
                StackMergeHowToPlayLayer.Research => IsResearchMenuUnlocked(),
                StackMergeHowToPlayLayer.Datacenter => progression.DatacenterUnlocked,
                StackMergeHowToPlayLayer.History => true,
                StackMergeHowToPlayLayer.Achievements => true,
                _ => true
            };
        }

        private void TryShowMenuHelpForSelectedTab()
        {
            if (!menuHelpReady || historyOpen || achievementsOpen)
            {
                return;
            }

            if (!TryGetHowToPlayLayerForTab(selectedTabIndex, out StackMergeHowToPlayLayer layer))
            {
                return;
            }

            TryShowMenuHelp(layer);
        }

        private bool TryGetHowToPlayLayerForTab(int tabIndex, out StackMergeHowToPlayLayer layer)
        {
            switch (tabIndex)
            {
                case 0:
                    layer = StackMergeHowToPlayLayer.Gameplay;
                    return true;
                case 1:
                    layer = StackMergeHowToPlayLayer.Algorithms;
                    return true;
                case 2:
                    layer = StackMergeHowToPlayLayer.Upgrades;
                    return true;
                case 3:
                    layer = StackMergeHowToPlayLayer.Agents;
                    return true;
                case 4:
                    layer = StackMergeHowToPlayLayer.Modifiers;
                    return true;
                case 5:
                    layer = StackMergeHowToPlayLayer.Research;
                    return true;
                case 7:
                    layer = StackMergeHowToPlayLayer.Datacenter;
                    return true;
                default:
                    layer = StackMergeHowToPlayLayer.Gameplay;
                    return false;
            }
        }

        private void TryShowMenuHelp(StackMergeHowToPlayLayer layer)
        {
            if (!IsHowToPlayLayerUnlocked(layer))
            {
                return;
            }

            EnsureSettingsReferences();
            if (menuHelpOverlay == null)
            {
                return;
            }

            if (menuHelpOverlay.IsVisible)
            {
                pendingMenuHelpLayer = layer;
                menuHelpOverlay.Hidden -= OnMenuHelpOverlayHidden;
                menuHelpOverlay.Hidden += OnMenuHelpOverlayHidden;
                return;
            }

            MenuHelpOverlayEntry entry = GetMenuHelpEntry(layer);
            string prefsKey = GetMenuHelpPrefsKey(layer, entry);
            if (!showMenuHelpEveryOpen && PlayerPrefs.GetInt(prefsKey, 0) == 1)
            {
                return;
            }

            menuHelpOverlay.Hidden -= OnMenuHelpOverlayHidden;
            menuHelpOverlay.Show(
                GetMenuHelpTitle(layer, entry),
                GetMenuHelpBody(layer, entry));

            PlayerPrefs.SetInt(prefsKey, 1);
            PlayerPrefs.Save();
        }

        private void OnMenuHelpOverlayHidden()
        {
            if (menuHelpOverlay != null)
            {
                menuHelpOverlay.Hidden -= OnMenuHelpOverlayHidden;
            }

            if (!pendingMenuHelpLayer.HasValue)
            {
                return;
            }

            StackMergeHowToPlayLayer layer = pendingMenuHelpLayer.Value;
            pendingMenuHelpLayer = null;
            TryShowMenuHelp(layer);
        }

        private MenuHelpOverlayEntry GetMenuHelpEntry(StackMergeHowToPlayLayer layer)
        {
            return menuHelpEntries == null
                ? null
                : menuHelpEntries.FirstOrDefault(entry => entry != null && entry.layer == layer);
        }

        private static string GetMenuHelpPrefsKey(StackMergeHowToPlayLayer layer, MenuHelpOverlayEntry entry)
        {
            return entry != null && !string.IsNullOrWhiteSpace(entry.playerPrefsKey)
                ? entry.playerPrefsKey
                : $"StackMerge.HelpOverlay.Menu.{layer}.Seen";
        }

        private static string GetMenuHelpTitle(StackMergeHowToPlayLayer layer, MenuHelpOverlayEntry entry)
        {
            if (StackMergeLocalization.CurrentLanguage == StackMergeLanguage.Magyar)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.magyarTitle))
                {
                    return entry.magyarTitle;
                }

                return layer switch
                {
                    StackMergeHowToPlayLayer.Gameplay => "Ez itt a kert!",
                    StackMergeHowToPlayLayer.Algorithms => "Algoritmusok",
                    StackMergeHowToPlayLayer.Upgrades => "Fejlesztések",
                    StackMergeHowToPlayLayer.Agents => "Ügynökök",
                    StackMergeHowToPlayLayer.Modifiers => "Módosítók",
                    StackMergeHowToPlayLayer.Research => "Kutatás",
                    StackMergeHowToPlayLayer.Datacenter => "Adatközpont",
                    StackMergeHowToPlayLayer.History => "Előzmények",
                    StackMergeHowToPlayLayer.Achievements => "Mérföldkövek",
                    _ => "Segítség"
                };
            }

            if (entry != null && !string.IsNullOrWhiteSpace(entry.title))
            {
                return entry.title;
            }

            return layer switch
            {
                StackMergeHowToPlayLayer.Gameplay => "Welcome!",
                StackMergeHowToPlayLayer.Algorithms => "Algorithms",
                StackMergeHowToPlayLayer.Upgrades => "Upgrades",
                StackMergeHowToPlayLayer.Agents => "Agents",
                StackMergeHowToPlayLayer.Modifiers => "Modifiers",
                StackMergeHowToPlayLayer.Research => "Research",
                StackMergeHowToPlayLayer.Datacenter => "Datacenter",
                StackMergeHowToPlayLayer.History => "History",
                StackMergeHowToPlayLayer.Achievements => "Achievements",
                _ => "Help"
            };
        }

        private static string GetMenuHelpBody(StackMergeHowToPlayLayer layer, MenuHelpOverlayEntry entry)
        {
            if (StackMergeLocalization.CurrentLanguage == StackMergeLanguage.Magyar)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.magyarBody))
                {
                    return entry.magyarBody;
                }

                return layer switch
                {
                    StackMergeHowToPlayLayer.Gameplay => "Helyezd a blokkokat a négy kupac egyikébe.\n\n" +
                    "Két ugyanolyan értékű blokk egymásra helyezésével egyesítheted (merge) azokat, így több pontszámra tehetsz szert és nagyobb játékbeli valuta <sprite name=\"chips\" tint=1> jutalomban részesülsz.\n\n" +
                    "Ügyelj arra, hogy mindig legyen legalább egy szabad mozgástér, és húzd a lehető legtovább a runokat.\n\n" +
                    "A játékmenet korán megváltozik, amely során már nem te, hanem algoritmusok játszanak helyetted. Onnantól a te feladatod a különféle fejlesztések megvásárlása és menedzselése lesz.\n\n" +
                    "<b>A játék minden lényeges eleméről részletesen a Beállítások > Hogyan kell játszani menüben olvashatsz.</b>",

                    StackMergeHowToPlayLayer.Algorithms => "Az algoritmusok feladata, hogy automatikusan játszanak helyetted.\n\n" +
                    "Minden solver más értékeket priorizálva dönt, ezért érdemes összehasonlítani őket. Működésük a Tuning feloldásával hangolható.\n\n" +
                    "A végső cél az, hogy feloldd a PPO-t, ami egy tanuló algoritmus. A feloldás feltételei csak a játék későbbi szakaszában válnak teljesíthetővé.",

                    StackMergeHowToPlayLayer.Upgrades => "A fejlesztések a játék korai szakaszának <sprite name=\"chips\" tint=1> alapú fejlődési rétege.\n\n" +
                    "Itt oldható fel az automatizálás, itt lesz gyorsítható és bővíthető a játékmenet, valamint itt növelhető a bevétel termelése.\n\n" +
                    "Elsőként célszerű az automatizációs fejlesztéseket megvásárolni, hogy mihamarabb a menedzseléssel foglalkozhass.\n\n" +
                    "<b>Fontos, hogy az Auto újraindítás <sprite name=\"token\" tint=1>-t használ.</b>",

                    StackMergeHowToPlayLayer.Agents => "Az Ügynökök aktív loadout bónuszokat adnak.\n\n" +
                    "Nem minden birtokolt Ügynök aktív: slotokba felszerelt kombináció határozza meg, milyen irányba válik hatékonyabbá a run.",

                    StackMergeHowToPlayLayer.Modifiers => "A Módosítók a jövőbeli runok szabályait változtatják meg. Több lehetőséget és nagyobb bevételt adnak, de a board is összetettebb lesz.\n\n" +
                    "Egyes Módosítók több szinten is fejleszthetőek.\n\n" +
                    "<b>Az összes Módosító megvásárlásával elérhetővé válik a PPO algoritmus, ami már a végjáték kapuja.</b>",

                    StackMergeHowToPlayLayer.Research => "A Kutatás az a réteg, amely által elérhetővé válik a Prestige Reset.\n\n" +
                    "Itt tudsz játékmenetet könnyítő és gyorsító permanens fejlesztéseket vásárolni, amelyhez azonban <sprite name=\"insight\" tint=1>-ra van szükséged." +
                    "A <sprite name=\"insight\" tint=1> a PPO Normál Módban történő játszatással termelődik.\n\n" +
                    "Ahhoz, hogy a <sprite name=\"insight\" tint=1>-ot megkapd, Prestige Resetelned kell. Erről részletesen a Console-ban olvashatsz.",

                    StackMergeHowToPlayLayer.Datacenter => "Az Adatközpont egy permanens compute réteg, amely a Prestige-ek felgyorsításáért felelnek.\n\n" +
                    "Itt ismét <sprite name=\"chips\" tint=1> a valuta, így a PPO Normál Módban felhalmozott termelés Prestige Reset esetén innentől kezdve nem vész kárba.\n\n" +
                    "<b>Fontos, hogy a felhalmozott <sprite name=\"chips\" tint=1>-eket még Prestige Reset előtt költsd el, különben nullázódnak.</b>\n\n" +
                    "Szerverállványokat és Létesítményfejlesztéseket vásárolhatsz, az így szerzett kapacitást pedig tetszőleges elosztással passzív PPO tanulásra, passzív <sprite name=\"insight\" tint=1> termelésre vagy <sprite name=\"chips\" tint=1> szorzóra allokálhatod.",

                    StackMergeHowToPlayLayer.History => "Itt látsz részletes adatokat arról, hogyan teljesítenek a runjaid ebben a Prestige ciklusban.\n\n" +
                    "Itt látod a legjobb elért pontszámot, a legjobb medián teljesítményt, az átlagos run időt és a solverenkénti eredményeket.\n\n" +
                    "<b>Mérheted, hogy egy új algoritmus, tuning, vagy bármilyen módosítás miképp hat a teljesítményedre.</b>",

                    StackMergeHowToPlayLayer.Achievements => "Itt követheted nyomon a játékban töltött idő alatt elért legjobb eredményeket, összesítve, illetve itt láthatod, mely célokat teljesítetted már.\n\n" +
                    "<b>Egyes mérföldkövek permanens jutalmat vagy kozmetikai feloldást adnak.</b>",
                    _ => string.Empty
                };
            }

            if (entry != null && !string.IsNullOrWhiteSpace(entry.body))
            {
                return entry.body;
            }

            return layer switch
            {
                StackMergeHowToPlayLayer.Gameplay => "Place the blocks into one of the four stacks.\n\n" +
                "By placing two blocks with the same value on top of each other, you can merge them, earning more score and receiving a larger in-game currency <sprite name=\"chips\" tint=1> reward.\n\n" +
                "Make sure to always keep at least one free space available, and try to make your runs last as long as possible.\n\n" +
                "The gameplay changes early on, when algorithms start playing instead of you. From that point onward, your role is to purchase various upgrades and manage them.\n\n" +
                "<b>You can find details about every important game feature in Settings > How to Play menu.</b>",

                StackMergeHowToPlayLayer.Algorithms => "Algorithms are responsible for playing the game automatically for you.\n\n" +
                "Each solver prioritizes different values when making decisions, so it's worth comparing them. Their behavior can be tuned once Tuning is unlocked.\n\n" +
                "The ultimate goal is to unlock PPO, which is a learning algorithm. The requirements for unlocking it can only be met later in the game.",

                StackMergeHowToPlayLayer.Upgrades => "Upgrades are the early-game progression layer based on <sprite name=\"chips\" tint=1>.\n\n" +
                "This is where you unlock automation, speed up and expand gameplay, and increase your income generation.\n\n" +
                "It's generally best to prioritize automation upgrades so you can focus on managing your run as early as possible.\n\n" +
                "<b>Keep in mind that Auto Restart consumes <sprite name=\"token\" tint=1>.</b>",

                StackMergeHowToPlayLayer.Agents => "Agents provide active loadout bonuses.\n\n" +
                "Not every owned Agent is active: the combination equipped in your slots determines which aspects of your run become more effective.",

                StackMergeHowToPlayLayer.Modifiers => "Modifiers change the rules of future runs. They unlock new possibilities and increase your income, but they also make the board more complex.\n\n" +
                "Some Modifiers can be upgraded multiple times.\n\n" +
                "<b>Purchasing every Modifier unlocks the PPO algorithm, marking the gateway to the endgame.</b>",

                StackMergeHowToPlayLayer.Research => "Research is the progression layer that unlocks the ability to Prestige Reset.\n\n" +
                "Here, you can purchase permanent upgrades that make gameplay easier and faster, but they require <sprite name=\"insight\" tint=1>.\n\n" +
                "<sprite name=\"insight\" tint=1> is generated by running the PPO algorithm in Normal Mode.\n\n" +
                "To claim your <sprite name=\"insight\" tint=1>, you must perform a Prestige Reset. You can find details in the Console.",

                StackMergeHowToPlayLayer.Datacenter => "Datacenter is a persistent compute layer that speeds up your Prestiges.\n\n" +
                "Here, <sprite name=\"chips\" tint=1> becomes the currency again, meaning the production accumulated in PPO Normal Mode is no longer wasted when you perform a Prestige Reset.\n\n" +
                "<b>Be sure to spend your accumulated <sprite name=\"chips\" tint=1> before resetting, as any unspent amount will be lost.</b>\n\n" +
                "You can purchase Server Racks and Facility Upgrades, then allocate the resulting compute capacity however you like: to passive PPO training, passive <sprite name=\"insight\" tint=1> generation, or a <sprite name=\"chips\" tint=1> production multiplier.",

                StackMergeHowToPlayLayer.History => "Here you can see detailed data on how your runs perform during the current Prestige cycle.\n\n" +
                "You can view your highest score, best median performance, average run duration, and results for each solver.\n\n" +
                "<b>Measure how a new algorithm, Tuning, or any other modification affects your performance.</b>",

                StackMergeHowToPlayLayer.Achievements => "Here you can track your best results achieved throughout your playtime, along with a summary of your overall progress. You can also see which goals you have already completed.\n\n" +
                "<b>Some milestones grant permanent rewards or cosmetic unlocks.</b>",
                _ => string.Empty
            };
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
            RefreshHud();
        }

        private void CloseSolverTunePanel()
        {
            solverTuneOpen = false;
            SetActive(solverTunePanel, false);
            RefreshSolverDetails();
            RefreshHud();
        }

        private void RefreshTabButtons()
        {
            EnsureBottomTabVisualCache();
            int activeIndex = GetBottomMenuActiveTabIndex();

            for (int i = 0; i < tabButtons.Length; i++)
            {
                Button button = tabButtons[i];
                if (button == null)
                {
                    continue;
                }

                // Settings is no longer a bottom-bar tab — it opens from the Gameplay footer.
                // Index 7 is the Datacenter tab; anything past that is a leftover and stays hidden.
                if (i == 6 || i > 7)
                {
                    SetActive(button.gameObject, false);
                    continue;
                }

                SetActive(button.gameObject, true);

                bool locked = IsBottomTabLocked(i);
                button.interactable = !locked;
            }

            UpdateBottomMenuHighlightTarget(activeIndex);
            RefreshBottomMenuIconStates();
        }

        private void EnsureBottomTabVisualCache()
        {
            if (bottomTabVisuals.Length == tabButtons.Length)
            {
                RefreshBottomTabCanonicalLabels();
                return;
            }

            bottomTabVisuals = new BottomTabVisual[tabButtons.Length];
            for (int i = 0; i < tabButtons.Length; i++)
            {
                Button button = tabButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.transition = Selectable.Transition.None;

                Transform iconBackgroundTransform = FindNamedDescendant(button.transform, "IconBackground");
                Image iconBackground = iconBackgroundTransform != null ? iconBackgroundTransform.GetComponent<Image>() : null;
                Transform iconTransform = iconBackgroundTransform != null ? FindNamedDescendant(iconBackgroundTransform, "Icon") : null;
                if (iconTransform == null)
                {
                    iconTransform = FindNamedDescendant(button.transform, "Icon");
                }

                Transform textTransform = FindNamedDescendant(button.transform, "Text");
                TMP_Text label = textTransform != null ? textTransform.GetComponent<TMP_Text>() : button.GetComponentInChildren<TMP_Text>(true);
                Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
                RectTransform iconBackgroundRect = iconBackground != null ? iconBackground.rectTransform : null;

                bottomTabVisuals[i] = new BottomTabVisual
                {
                    iconBackground = iconBackground,
                    iconBackgroundRect = iconBackgroundRect,
                    icon = icon,
                    label = label,
                    unlockedIcon = icon != null ? icon.sprite : null,
                    unlockedLabel = GetDefaultBottomTabLabel(i),
                    unlockedLabelColor = label != null ? label.color : Color.white,
                    iconBackgroundHomePosition = iconBackgroundRect != null ? iconBackgroundRect.anchoredPosition : Vector2.zero
                };
            }

            RefreshBottomTabCanonicalLabels();
        }

        private void RefreshBottomTabCanonicalLabels()
        {
            for (int i = 0; i < bottomTabVisuals.Length; i++)
            {
                BottomTabVisual visual = bottomTabVisuals[i];
                if (visual != null)
                {
                    visual.unlockedLabel = GetDefaultBottomTabLabel(i);
                }
            }
        }

        private void ApplyBottomTabVisual(BottomTabVisual visual, bool locked, bool selected)
        {
            if (visual == null)
            {
                return;
            }

            if (visual.label != null)
            {
                visual.label.text = StackMergeLocalization.Translate(locked ? "Locked" : visual.unlockedLabel);
                visual.label.color = locked ? HexColor("#808080") : visual.unlockedLabelColor;
            }

            if (visual.icon != null)
            {
                if (locked)
                {
                    if (lockedTabIcon != null)
                    {
                        visual.icon.sprite = lockedTabIcon;
                    }

                    visual.icon.color = HexColor("#808080");
                }
                else
                {
                    if (visual.unlockedIcon != null)
                    {
                        visual.icon.sprite = visual.unlockedIcon;
                    }

                    visual.icon.color = selected ? Color.white : Color.black;
                }
            }
        }

        private int GetBottomMenuActiveTabIndex()
        {
            // Settings (6) opens from the Gameplay footer, so it has no bar highlight; the
            // Datacenter tab (7) is a regular bar tab.
            if (historyOpen || achievementsOpen || selectedTabIndex < 0 || selectedTabIndex == 6 || selectedTabIndex >= tabButtons.Length)
            {
                return -1;
            }

            return IsBottomTabLocked(selectedTabIndex) ? -1 : selectedTabIndex;
        }

        private bool CanSlideBottomMenuHighlight(int fromIndex, int toIndex)
        {
            if (fromIndex < 0
                || toIndex < 0
                || fromIndex == toIndex
                || bottomMenuHighlightSlideSeconds <= 0f
                || fromIndex >= bottomTabVisuals.Length
                || toIndex >= bottomTabVisuals.Length)
            {
                return false;
            }

            BottomTabVisual from = bottomTabVisuals[fromIndex];
            BottomTabVisual to = bottomTabVisuals[toIndex];
            return from?.iconBackgroundRect != null
                && to?.iconBackground != null
                && to.iconBackgroundRect != null;
        }

        private bool IsBottomTabLocked(int tabIndex)
        {
            return (tabIndex == 3 && progression != null && !progression.AgentsMenuUnlocked)
                || (tabIndex == 4 && progression != null && !progression.ModifiersMenuUnlocked)
                || (tabIndex == 5 && progression != null && !IsResearchMenuUnlocked())
                || (tabIndex == 7 && progression != null && !progression.DatacenterUnlocked);
        }

        private static string GetDefaultBottomTabLabel(int tabIndex)
        {
            string[] labels = { "Game", "Algos", "Upgrades", "Agents", "Mods", "Research", string.Empty, "DC" };
            return tabIndex >= 0 && tabIndex < labels.Length ? labels[tabIndex] : string.Empty;
        }

        private static Transform FindNamedDescendant(Transform root, string targetName)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root)
            {
                if (child.name == targetName)
                {
                    return child;
                }

                Transform match = FindNamedDescendant(child, targetName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Transform FindFirstNamedDescendant(Transform root, params string[] targetNames)
        {
            if (root == null || targetNames == null)
            {
                return null;
            }

            foreach (string targetName in targetNames)
            {
                Transform match = FindNamedDescendant(root, targetName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private void UpdateBottomMenuHighlightTarget(int activeIndex)
        {
            if (activeIndex < 0 || activeIndex >= bottomTabVisuals.Length)
            {
                bottomMenuHighlightIndex = -1;
                bottomMenuHighlightAnimating = false;
                bottomMenuHighlightRect = null;
                bottomMenuHighlightPendingIconIndex = -1;
                ApplyBottomMenuHighlightVisibility(-1);
                return;
            }

            BottomTabVisual target = bottomTabVisuals[activeIndex];
            if (target == null || target.iconBackground == null || target.iconBackgroundRect == null)
            {
                bottomMenuHighlightIndex = activeIndex;
                bottomMenuHighlightPendingIconIndex = -1;
                ApplyBottomMenuHighlightVisibility(activeIndex);
                return;
            }

            if (activeIndex == bottomMenuHighlightIndex)
            {
                ApplyBottomMenuHighlightVisibility(activeIndex);
                return;
            }

            int previousIndex = bottomMenuHighlightIndex;
            bool shouldAnimate = CanSlideBottomMenuHighlight(previousIndex, activeIndex);
            Vector3 startWorld = GetBottomTabBackgroundHomeWorld(target);
            if (shouldAnimate)
            {
                BottomTabVisual previous = bottomTabVisuals[previousIndex];
                startWorld = GetBottomTabBackgroundHomeWorld(previous);
            }

            bottomMenuHighlightIndex = activeIndex;
            bottomMenuHighlightPendingIconIndex = shouldAnimate ? activeIndex : -1;
            ApplyBottomMenuHighlightVisibility(activeIndex);

            if (!shouldAnimate)
            {
                RestoreBottomTabBackgroundHome(target);
                return;
            }

            bottomMenuHighlightRect = target.iconBackgroundRect;
            bottomMenuHighlightStartWorld = startWorld;
            bottomMenuHighlightEndWorld = GetBottomTabBackgroundHomeWorld(target);
            bottomMenuHighlightTimer = 0f;
            bottomMenuHighlightAnimating = true;
            bottomMenuHighlightRect.position = bottomMenuHighlightStartWorld;
        }

        private void TickBottomMenuHighlight()
        {
            if (!bottomMenuHighlightAnimating || bottomMenuHighlightRect == null)
            {
                return;
            }

            bottomMenuHighlightTimer += Time.unscaledDeltaTime;
            float duration = Mathf.Max(0.01f, bottomMenuHighlightSlideSeconds);
            float t = Mathf.Clamp01(bottomMenuHighlightTimer / duration);
            float eased = t * t * (3f - 2f * t);
            bottomMenuHighlightRect.position = Vector3.LerpUnclamped(bottomMenuHighlightStartWorld, bottomMenuHighlightEndWorld, eased);

            if (bottomMenuHighlightPendingIconIndex >= 0 && IsMovingHighlightTouchingPendingIcon())
            {
                bottomMenuHighlightPendingIconIndex = -1;
                RefreshBottomMenuIconStates();
            }

            if (t < 1f)
            {
                return;
            }

            bottomMenuHighlightAnimating = false;
            if (bottomMenuHighlightIndex >= 0 && bottomMenuHighlightIndex < bottomTabVisuals.Length)
            {
                RestoreBottomTabBackgroundHome(bottomTabVisuals[bottomMenuHighlightIndex]);
                SetBottomTabBackgroundAlpha(bottomTabVisuals[bottomMenuHighlightIndex], 1f);
            }

            bottomMenuHighlightPendingIconIndex = -1;
            RefreshBottomMenuIconStates();
        }

        private void ApplyBottomMenuHighlightVisibility(int activeIndex)
        {
            for (int i = 0; i < bottomTabVisuals.Length; i++)
            {
                BottomTabVisual visual = bottomTabVisuals[i];
                if (visual == null)
                {
                    continue;
                }

                bool active = i == activeIndex;
                SetBottomTabBackgroundAlpha(visual, active ? 1f : 0f);
                if (!active || !bottomMenuHighlightAnimating || visual.iconBackgroundRect != bottomMenuHighlightRect)
                {
                    RestoreBottomTabBackgroundHome(visual);
                }
            }
        }

        private void RefreshBottomMenuIconStates()
        {
            int activeIndex = GetBottomMenuActiveTabIndex();
            for (int i = 0; i < tabButtons.Length && i <= 7; i++)
            {
                if (i == 6)
                {
                    continue; // Settings is not on the bar.
                }

                BottomTabVisual visual = i < bottomTabVisuals.Length ? bottomTabVisuals[i] : null;
                ApplyBottomTabVisual(visual, IsBottomTabLocked(i), i == activeIndex && i != bottomMenuHighlightPendingIconIndex);
            }
        }

        private bool IsMovingHighlightTouchingPendingIcon()
        {
            if (bottomMenuHighlightPendingIconIndex < 0
                || bottomMenuHighlightPendingIconIndex >= bottomTabVisuals.Length
                || bottomMenuHighlightRect == null)
            {
                return false;
            }

            BottomTabVisual pending = bottomTabVisuals[bottomMenuHighlightPendingIconIndex];
            RectTransform iconRect = pending?.icon != null ? pending.icon.rectTransform : null;
            return iconRect == null || WorldRectsOverlap(bottomMenuHighlightRect, iconRect);
        }

        private static void SetBottomTabBackgroundAlpha(BottomTabVisual visual, float alpha)
        {
            if (visual?.iconBackground == null)
            {
                return;
            }

            Color color = HexColor("#00BFFF", alpha);
            visual.iconBackground.color = color;
        }

        private static void RestoreBottomTabBackgroundHome(BottomTabVisual visual)
        {
            if (visual?.iconBackgroundRect != null)
            {
                visual.iconBackgroundRect.anchoredPosition = visual.iconBackgroundHomePosition;
            }
        }

        private static Vector3 GetBottomTabBackgroundHomeWorld(BottomTabVisual visual)
        {
            if (visual?.iconBackgroundRect == null)
            {
                return Vector3.zero;
            }

            Vector2 current = visual.iconBackgroundRect.anchoredPosition;
            visual.iconBackgroundRect.anchoredPosition = visual.iconBackgroundHomePosition;
            Vector3 world = visual.iconBackgroundRect.position;
            visual.iconBackgroundRect.anchoredPosition = current;
            return world;
        }

        private static bool WorldRectsOverlap(RectTransform a, RectTransform b)
        {
            if (a == null || b == null)
            {
                return false;
            }

            Vector3[] aCorners = new Vector3[4];
            Vector3[] bCorners = new Vector3[4];
            a.GetWorldCorners(aCorners);
            b.GetWorldCorners(bCorners);

            GetWorldBounds(aCorners, out float aMinX, out float aMaxX, out float aMinY, out float aMaxY);
            GetWorldBounds(bCorners, out float bMinX, out float bMaxX, out float bMinY, out float bMaxY);

            return aMinX <= bMaxX
                && aMaxX >= bMinX
                && aMinY <= bMaxY
                && aMaxY >= bMinY;
        }

        private static void GetWorldBounds(Vector3[] corners, out float minX, out float maxX, out float minY, out float maxY)
        {
            minX = corners[0].x;
            maxX = corners[0].x;
            minY = corners[0].y;
            maxY = corners[0].y;

            for (int i = 1; i < corners.Length; i++)
            {
                Vector3 corner = corners[i];
                minX = Mathf.Min(minX, corner.x);
                maxX = Mathf.Max(maxX, corner.x);
                minY = Mathf.Min(minY, corner.y);
                maxY = Mathf.Max(maxY, corner.y);
            }
        }

        private void HideTemplate()
        {
            if (blockTemplate != null)
            {
                Scene templateScene = blockTemplate.gameObject.scene;
                if (templateScene.IsValid() && templateScene.isLoaded)
                {
                    blockTemplate.gameObject.SetActive(false);
                }
            }
        }

        private void HideFeedbackModalTemplate()
        {
            EnsureFeedbackModalReferences();
            if (feedbackModalPrefab == null)
            {
                return;
            }

            Scene templateScene = feedbackModalPrefab.scene;
            if (templateScene.IsValid() && templateScene.isLoaded)
            {
                feedbackModalPrefab.SetActive(false);
            }
        }

        private void EnsureFeedbackModalReferences()
        {
            if (canvas == null)
            {
                canvas = FindAnyObjectByType<Canvas>();
            }

            if (feedbackModalRoot == null && canvas != null)
            {
                Transform root = FindNamedDescendant(canvas.transform, "Feedback Modal Root")
                    ?? FindNamedDescendant(canvas.transform, "FeedbackModalRoot")
                    ?? FindNamedDescendant(canvas.transform, "Feedback Toast Root")
                    ?? FindNamedDescendant(canvas.transform, "ToastRoot");
                feedbackModalRoot = root as RectTransform;
            }

            if (feedbackModalPrefab == null && canvas != null)
            {
                Transform modal = FindNamedDescendant(canvas.transform, "Feedback Modal")
                    ?? FindNamedDescendant(canvas.transform, "FeedbackModal")
                    ?? FindNamedDescendant(canvas.transform, "Feedback Toast")
                    ?? FindNamedDescendant(canvas.transform, "Toast");
                feedbackModalPrefab = modal != null ? modal.gameObject : null;
            }
        }

        private void ShowFeedbackModal(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            EnsureFeedbackModalReferences();
            if (feedbackModalPrefab == null)
            {
                SetText(feedbackText, message);
                return;
            }

            Transform parent = feedbackModalRoot != null
                ? feedbackModalRoot
                : canvas != null
                    ? canvas.transform
                    : transform;

            activeFeedbackModals.RemoveAll(modal => modal == null);
            int stackIndex = activeFeedbackModals.Count;
            GameObject instance = Instantiate(feedbackModalPrefab, parent);
            instance.name = feedbackModalPrefab.name;
            instance.SetActive(true);
            StackMergeFeedbackModal modalComponent = instance.GetComponent<StackMergeFeedbackModal>();
            if (modalComponent == null)
            {
                modalComponent = instance.AddComponent<StackMergeFeedbackModal>();
            }

            activeFeedbackModals.Add(modalComponent);
            modalComponent.Finished += OnFeedbackModalFinished;
            modalComponent.Play(
                message,
                feedbackModalLifetime,
                feedbackModalRiseDistance,
                feedbackModalAnchorBottom,
                feedbackModalBottomOffset + Vector2.up * feedbackModalStackSpacing * stackIndex);
        }

        private void OnFeedbackModalFinished(StackMergeFeedbackModal modal)
        {
            if (modal != null)
            {
                modal.Finished -= OnFeedbackModalFinished;
            }

            activeFeedbackModals.Remove(modal);
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
                    // Pause length comes from progression so the Evaluation Efficiency research can
                    // shorten it (base 2.5s, -15%/level).
                    float evalDuration = progression.MachineLearningEvaluationSeconds;
                    trainingEvalTimer += Time.deltaTime;
                    float percent = Mathf.Clamp01(trainingEvalTimer / Mathf.Max(0.01f, evalDuration)) * 100f;
                    string status = $"Evaluating {percent:0}%";
                    UpdatePpoTrainingRunInfo(status);
                    if (selectedTabIndex == 0 && !historyOpen && !achievementsOpen)
                    {
                        UpdateTrainingOverlay(status);
                    }

                    if (trainingEvalTimer >= evalDuration)
                    {
                        trainingEvalTimer = 0f;
                        StartNewGame();
                    }

                    return;
                }

                if (progression.AutoRestartUnlocked && progression.AutoRestartEnabled)
                {
                    if (!CanAutoRestartCurrentRun())
                    {
                        autoRestartTimer = 0f;
                        SetText(runStatusText, "Run ended");
                        RebuildTextLayout(runStatusText);
                        UpdateGameOverAutoRestartSlider(false);
                        return;
                    }

                    autoRestartTimer += Time.deltaTime;
                    SetText(runStatusText, $"Restart in {Mathf.Max(0f, AutoRestartDelay - autoRestartTimer):0.0}s");
                    RebuildTextLayout(runStatusText);
                    UpdateGameOverAutoRestartSlider();
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
                            RebuildTextLayout(runStatusText);
                            RefreshProgressionUi();
                        }
                    }
                }

                return;
            }

            autoRestartTimer = 0f;
            UpdateGameOverAutoRestartSlider(false);
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

        private bool CanAutoRestartCurrentRun()
        {
            return progression != null
                && progression.AutoRestartUnlocked
                && progression.AutoRestartEnabled
                && (progression.AutoRestartIsTokenFree || progression.Tokens > 0);
        }

        private IStackMergeSolver GetSelectedSolver()
        {
            int solverIndex = Mathf.Clamp((int)progression.SelectedSolver, 0, solvers.Length - 1);
            return solvers[solverIndex];
        }

        private void HandleStackButtonClick(int stackIndex)
        {
            if (IsGameplayModifierArmed(ModifierId.MinersPickaxe))
            {
                SetText(feedbackText, "Tap a block to remove it with Miner's Pickaxe");
                RefreshGameView();
                return;
            }

            if (IsGameplayModifierArmed(ModifierId.QueueScrubber))
            {
                SetText(feedbackText, "Tap the first next block to scrub it");
                RefreshGameView();
                return;
            }

            PlaceOnStack(stackIndex, "Manual", false);
        }

        private void PlaceOnStack(int stackIndex, string reason, bool autoSolverMove)
        {
            if (gameState == null || progression == null)
            {
                return;
            }

            bool wasGameOver = gameState.IsGameOver;
            StackMergeGameState manualLessonState = CaptureManualPpoLessonState(autoSolverMove);
            MoveResult result = gameState.PlaceNext(stackIndex);
            if (!result.Accepted)
            {
                SetText(feedbackText, result.Reason);
                RefreshEverything();
                return;
            }

            TeachPpoFromManualMove(manualLessonState, result, autoSolverMove);
            HandleAcceptedMove(result, reason, autoSolverMove, wasGameOver);
        }

        private void ToggleGameplayModifier(ModifierId modifierId)
        {
            if (gameState == null || progression == null)
            {
                return;
            }

            if (IsGameplayModifierArmed(modifierId))
            {
                ClearArmedGameplayModifier();
                SetText(feedbackText, $"{GetGameplayModifierDisplayName(modifierId)} unequipped");
                RefreshGameView();
                return;
            }

            if (!CanArmGameplayModifier(modifierId))
            {
                ClearArmedGameplayModifier();
                SetText(feedbackText, GetGameplayModifierUnavailableMessage(modifierId));
                RefreshGameView();
                return;
            }

            armedGameplayModifier = (int)modifierId;
            SetText(feedbackText, $"{GetGameplayModifierDisplayName(modifierId)} equipped");
            RefreshGameView();
        }

        private void UsePickaxeOnBlock(int stackIndex, int blockIndex)
        {
            if (gameState == null || progression == null || !IsGameplayModifierArmed(ModifierId.MinersPickaxe))
            {
                return;
            }

            bool wasGameOver = gameState.IsGameOver;
            StackMergeGameState manualLessonState = CaptureManualPpoLessonState(autoSolverMove: false);
            MoveResult result = gameState.UsePickaxe(stackIndex, blockIndex);
            if (!result.Accepted)
            {
                SetText(feedbackText, result.Reason);
                ClearArmedGameplayModifier();
                RefreshEverything();
                return;
            }

            ClearArmedGameplayModifier();
            TeachPpoFromManualMove(manualLessonState, result, autoSolverMove: false);
            HandleAcceptedMove(result, "Manual pickaxe", false, wasGameOver);
        }

        private void UseQueueScrubber()
        {
            if (gameState == null || progression == null || !IsGameplayModifierArmed(ModifierId.QueueScrubber))
            {
                return;
            }

            bool wasGameOver = gameState.IsGameOver;
            StackMergeGameState manualLessonState = CaptureManualPpoLessonState(autoSolverMove: false);
            MoveResult result = gameState.SkipNextBlock();
            if (!result.Accepted)
            {
                SetText(feedbackText, result.Reason);
                ClearArmedGameplayModifier();
                RefreshEverything();
                return;
            }

            ClearArmedGameplayModifier();
            TeachPpoFromManualMove(manualLessonState, result, autoSolverMove: false);
            HandleAcceptedMove(result, "Manual queue scrubber", false, wasGameOver);
        }

        private StackMergeGameState CaptureManualPpoLessonState(bool autoSolverMove)
        {
            if (autoSolverMove || progression == null || gameState == null || !progression.ManualRunsTeachPpo)
            {
                return null;
            }

            return gameState.CreateSimulationCopy();
        }

        private void TeachPpoFromManualMove(StackMergeGameState stateBeforeMove, MoveResult result, bool autoSolverMove)
        {
            if (autoSolverMove || stateBeforeMove == null || progression == null || gameState == null)
            {
                return;
            }

            progression.ObserveManualMoveForMachineLearning(stateBeforeMove, result, gameState);
        }

        private bool IsGameplayModifierArmed(ModifierId modifierId)
        {
            return armedGameplayModifier == (int)modifierId;
        }

        private bool IsManualModeActive()
        {
            return progression == null
                || !progression.HasPurchasedSolver
                || (!progression.IsMachineLearningTrainingActive && (solverDeselected || !progression.AutoSolveEnabled));
        }

        private void ClearArmedGameplayModifier()
        {
            armedGameplayModifier = NoArmedGameplayModifier;
        }

        private bool IsGameplayModifierUnlocked(ModifierId modifierId)
        {
            return progression != null
                && progression.ModifiersMenuUnlocked
                && progression.GetModifierLevel(modifierId) > 0;
        }

        private int GetGameplayModifierRemaining(ModifierId modifierId)
        {
            if (gameState == null)
            {
                return 0;
            }

            return modifierId switch
            {
                ModifierId.MinersPickaxe => gameState.PickaxeUsesRemaining,
                ModifierId.QueueScrubber => gameState.QueueSkipsRemaining,
                _ => 0
            };
        }

        private bool CanArmGameplayModifier(ModifierId modifierId)
        {
            if (gameState == null || gameState.IsGameOver || !IsGameplayModifierUnlocked(modifierId) || GetGameplayModifierRemaining(modifierId) <= 0)
            {
                return false;
            }

            return modifierId switch
            {
                ModifierId.MinersPickaxe => HasPickaxeTarget(),
                ModifierId.QueueScrubber => gameState.NextBlocks.Count > 0,
                _ => false
            };
        }

        private bool HasManualGameplayModifierAction()
        {
            return gameState != null
                && ((IsGameplayModifierUnlocked(ModifierId.MinersPickaxe) && gameState.PickaxeUsesRemaining > 0 && HasPickaxeTarget())
                    || (IsGameplayModifierUnlocked(ModifierId.QueueScrubber) && gameState.QueueSkipsRemaining > 0 && gameState.NextBlocks.Count > 0));
        }

        private bool HasPickaxeTarget()
        {
            if (gameState == null)
            {
                return false;
            }

            for (int i = 0; i < gameState.Stacks.Count; i++)
            {
                if (gameState.Stacks[i].Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetGameplayModifierDisplayName(ModifierId modifierId)
        {
            return modifierId switch
            {
                ModifierId.MinersPickaxe => "Miner's Pickaxe",
                ModifierId.QueueScrubber => "Queue Scrubber",
                _ => "Modifier"
            };
        }

        private string GetGameplayModifierUnavailableMessage(ModifierId modifierId)
        {
            if (!IsGameplayModifierUnlocked(modifierId))
            {
                return $"{GetGameplayModifierDisplayName(modifierId)} is locked";
            }

            if (GetGameplayModifierRemaining(modifierId) <= 0)
            {
                return $"{GetGameplayModifierDisplayName(modifierId)} has no uses left";
            }

            return modifierId switch
            {
                ModifierId.MinersPickaxe => "No block available for Miner's Pickaxe",
                ModifierId.QueueScrubber => "No next block available for Queue Scrubber",
                _ => "Modifier unavailable"
            };
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
            timeSinceLastAcceptedMove = 0f;

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
            if (result.ActionKind == SolverActionKind.Pickaxe && result.RemovedValue >= 1024)
            {
                progression.MarkSecretAchievementComplete(SecretAchievementId.SurgicalMove);
            }

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
                StartGameOverOverlayDelay();
            }

            progression.Save();
            UpdateHighScore();
            QueueAchievementNotificationsForNewCompletions();

            string chipText = machineLearningTraining
                ? "training: +0 chips"
                : $"+{FormatNumber(chipsGained + runBonus)} <sprite name=\"chips\" tint=1>";
            string moveText = result.ActionKind switch
            {
                SolverActionKind.Pickaxe => $"Pickaxe -{FormatBlockValue(result.RemovedValue)}",
                SolverActionKind.QueueSkip => $"Scrubbed {FormatBlockValue(result.RemovedValue)}",
                _ => result.MergeCount > 0
                    ? $"Merge x{result.MergeCount}: {FormatBlockValue(result.ResultingTopValue)}"
                    : $"+{FormatBlockValue(result.PlacedValue)}"
            };

            // Combo Engine: surface the running streak so the multiplier is visible in play.
            if (progression.ComboEngineLevel > 0 && progression.CurrentComboStreak >= 2)
            {
                moveText += $" | Combo x{progression.CurrentComboStreak}";
            }
            string resultReason = string.IsNullOrWhiteSpace(result.Reason)
                ? reason
                : string.IsNullOrWhiteSpace(reason)
                    ? result.Reason
                    : $"{reason}; {result.Reason}";
            string learningText = progression.SelectedSolver == SolverId.MachineLearning
                ? $" | PPO knowledge {progression.MachineLearningSkill * 100f:0}%"
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

            // Salvage Protocol: convert a share of the run's final score into chips.
            long salvaged = progression.AwardSalvage(gameState.Score, machineLearningTraining);
            if (salvaged > 0)
            {
                SetText(feedbackText, $"Salvage: +{FormatNumber(salvaged)} <sprite name=\"chips\" tint=1>");
            }

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
                    machineLearningTraining,
                    currentRunElapsed);
            }

            currentRunChipsEarned += runBonus;
            return runBonus;
        }

        // Manual play can use pickaxe / queue-skip directly, so only end the run when there is
        // no legal placement and no manually usable modifier action left.
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

            if (gameState.HasLegalMove() || HasManualGameplayModifierAction())
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
            StartGameOverOverlayDelay();
            progression.Save();
            UpdateHighScore();
            SetText(feedbackText, "No moves left — run over");
            RefreshEverything();
        }

        public void StartNewGame()
        {
            bool preserveGameplayLayout = CanPreserveGameplayLayoutForNewRun();
            CreateFreshGame(preserveGameplayLayout);
            if (preserveGameplayLayout && gameplayLayoutWarmupCoroutine != null)
            {
                StopCoroutine(gameplayLayoutWarmupCoroutine);
                gameplayLayoutWarmupCoroutine = null;
            }

            SetText(feedbackText, string.Empty);
            RefreshEverything();
            if (!preserveGameplayLayout)
            {
                ForceGameplayLayoutPass();
                ScheduleGameplayLayoutWarmup(TabGameplayLayoutWarmupFrames);
            }
        }

        private bool CanPreserveGameplayLayoutForNewRun()
        {
            int nextCapacity = progression != null ? progression.StackCapacity : StackMergeGameState.DefaultStackCapacity;
            return initialGameplayLayoutFinalized
                && selectedTabIndex == 0
                && gameplayPanel != null
                && gameplayPanel.activeInHierarchy
                && !historyOpen
                && !achievementsOpen
                && progression != null
                && !progression.IsMachineLearningTrainingActive
                && gameState != null
                && gameState.StackCapacity == nextCapacity;
        }

        private void CreateFreshGame(bool preserveGameplayLayout = false)
        {
            int capacity = progression != null ? progression.StackCapacity : StackMergeGameState.DefaultStackCapacity;
            int queueLength = progression != null ? progression.QueueLength : StackMergeGameState.DefaultQueueLength;
            int difficulty = progression != null ? progression.DifficultyLevel : 0;
            int scalingFrequency = progression != null ? progression.ScalingFrequencyLevel : 0;
            StackMergeRunModifiers modifiers = progression != null ? progression.BuildRunModifiers() : default;
            gameState = new StackMergeGameState(stackCapacity: capacity, queueLength: queueLength, difficultyLevel: difficulty, scalingFrequencyLevel: scalingFrequency, modifiers: modifiers, seed: Environment.TickCount, scalingFrequencyRewardBonus: progression?.ScalingFrequencyRewardBonus ?? 0.0);
            autoSolveTimer = 0f;
            autoRestartTimer = 0f;
            currentRunUsedAutoSolve = false;
            currentRunManualMoves = 0;
            currentRunElapsed = 0f;
            currentRunChipsEarned = 0L;
            timeSinceLastAcceptedMove = float.MaxValue;
            gameOverOverlayTimer = 0f;
            nextTrainingOverlayRefreshTime = 0f;
            nextTrainingRunInfoRefreshTime = 0f;
            if (!preserveGameplayLayout)
            {
                boardLayoutDirty = true;
                lastRenderedCapacity = -1;
            }
            else
            {
                boardLayoutDirty = false;
                lastRenderedCapacity = capacity;
            }

            ClearArmedGameplayModifier();
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

        private void SetSolverDeselected(bool deselected)
        {
            if (progression == null)
            {
                solverDeselected = deselected;
                return;
            }

            progression.SetSolverDeselected(deselected);
            solverDeselected = progression.SolverDeselected;
            MaybeCompleteImmediateDeselectSecret();
        }

        private void RememberSolverSelectionForSecret(SolverId solverId)
        {
            recentlySelectedSolverForSecret = solverId;
            recentSolverSelectedAtTime = Time.unscaledTime;
            hasRecentSolverSelectionForSecret = true;
        }

        private void MaybeCompleteImmediateDeselectSecret()
        {
            if (progression == null || !solverDeselected || !hasRecentSolverSelectionForSecret)
            {
                return;
            }

            bool sameSolver = progression.SelectedSolver == recentlySelectedSolverForSecret;
            bool immediate = Time.unscaledTime - recentSolverSelectedAtTime <= SolverImmediateDeselectSecretWindowSeconds;
            hasRecentSolverSelectionForSecret = false;
            if (sameSolver && immediate)
            {
                progression.MarkSecretAchievementComplete(SecretAchievementId.ImmediateDeselect);
            }
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
                        ShowFeedbackModal(progression.CanUnlockMachineLearning ? "Not enough <sprite name=\"chips\" tint=1>" : "PPO requires every Modifier maxed");
                        RefreshEverything();
                        return;
                    }

                    solverDeselected = progression.SolverDeselected;
                    progression.Save();
                }

                ShowPpoModeModal();
                return;
            }

            // If the solver is already selected, toggle it off into manual play.
            if (progression.SelectedSolver == id && progression.IsSolverUnlocked(id))
            {
                SetSolverDeselected(!solverDeselected);
                if (!solverDeselected)
                {
                    RememberSolverSelectionForSecret(id);
                }

                ShowFeedbackModal(solverDeselected ? $"{definition.DisplayName} deselected" : $"{definition.DisplayName} selected");
                progression.Save();
                RefreshEverything();
                return;
            }

            bool changed = progression.SelectOrUnlockSolver(id);
            if (changed)
            {
                solverDeselected = progression.SolverDeselected;
                RememberSolverSelectionForSecret(id);
            }
            ShowFeedbackModal(changed ? $"{definition.DisplayName} selected" : "Not enough <sprite name=\"chips\" tint=1>");
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
                ShowFeedbackModal("Normal mode is still locked.");
                return;
            }

            progression.SetMachineLearningTrainingMode(training);
            progression.SelectOrUnlockSolver(SolverId.MachineLearning);
            solverDeselected = progression.SolverDeselected;
            RememberSolverSelectionForSecret(SolverId.MachineLearning);
            progression.Save();
            HidePpoModeModal();
            ShowFeedbackModal(training ? "PPO: Training mode" : "PPO: Normal mode");
            RefreshEverything();
        }

        private void HidePpoModeModal()
        {
            if (ppoModeOverlay != null)
            {
                SetActive(ppoModeOverlay, false);
            }
        }

        private void ShowPpoModeModal()
        {
            EnsurePpoSceneUiReferences();
            if (ppoModeOverlay == null)
            {
                ShowFeedbackModal("PPO Mode Overlay is not assigned.");
                return;
            }

            bool playingUnlocked = progression.MachineLearningPlayingModeUnlocked;
            long frames = progression.MachineLearningCycleFrames;

            SetButtonText(ppoTrainingButton, "Training Mode");
            if (ppoNormalButton != null)
            {
                ppoNormalButton.interactable = playingUnlocked;
                SetButtonText(ppoNormalButton, playingUnlocked ? "Normal Mode" : "Normal Mode\nLocked");
            }

            if (ppoModeHintText != null)
            {
                SetText(ppoModeHintText, playingUnlocked
                    ? "Training: keeps learning, earns no chips.\nNormal: plays for chips like other solvers."
                    : $"Normal mode unlocks after {FormatNumber(progression.EffectivePlayingModeFrameRequirement)} cycle frames.\n{FormatNumber(frames)} / {FormatNumber(progression.EffectivePlayingModeFrameRequirement)}");
            }

            SetActive(ppoModeOverlay, true);
            ppoModeOverlay.transform.SetAsLastSibling();
            ApplyButtonVisualState(ppoNormalButton);
            ApplyButtonVisualState(ppoTrainingButton);
        }

        private void EnsurePpoSceneUiReferences()
        {
            if (canvas == null)
            {
                return;
            }

            if (ppoModeOverlay == null)
            {
                Transform overlay = FindNamedDescendant(canvas.transform, "PPO Mode Overlay");
                ppoModeOverlay = overlay != null ? overlay.gameObject : null;
            }

            if (ppoModeOverlay != null)
            {
                ppoModeHintText ??= FindNamedDescendant(ppoModeOverlay.transform, "InfoText")?.GetComponent<TMP_Text>();

                Button[] modeButtons = ppoModeOverlay.GetComponentsInChildren<Button>(true)
                    .Where(button => button != null && button.gameObject != ppoModeOverlay)
                    .ToArray();
                ppoTrainingButton ??= FindPpoModeButton(modeButtons, "TrainingModeButton", "Training");
                ppoNormalButton ??= FindPpoModeButton(modeButtons, "NormalModeButton", "Normal", "PlayingModeButton");
                if (ppoTrainingButton == null && modeButtons.Length > 0)
                {
                    ppoTrainingButton = modeButtons[0];
                }

                if (ppoNormalButton == null && modeButtons.Length > 1)
                {
                    ppoNormalButton = modeButtons.FirstOrDefault(button => button != ppoTrainingButton);
                }

                Button backdrop = ppoModeOverlay.GetComponent<Button>();
                if (backdrop != null)
                {
                    backdrop.onClick.RemoveAllListeners();
                    backdrop.onClick.AddListener(HidePpoModeModal);
                }
            }

            if (ppoTrainingButton != null)
            {
                ppoTrainingButton.onClick.RemoveAllListeners();
                ppoTrainingButton.onClick.AddListener(() => ChoosePpoMode(true));
            }

            if (ppoNormalButton != null)
            {
                ppoNormalButton.onClick.RemoveAllListeners();
                ppoNormalButton.onClick.AddListener(() => ChoosePpoMode(false));
            }

            EnsureTrainingOverlay();
        }

        // ------------------------------------------------------------------------------------
        // Prestige Reset Modal — a hand-built scene object (see the Inspector fields). The
        // Prestige button opens it; it shows PPO Training progress, the exact Insight payout of a
        // reset right now, and the Buy button performs the actual prestige. References are found
        // by name so nothing needs manual dragging, but the serialized fields win when assigned.
        // ------------------------------------------------------------------------------------
        private void EnsurePrestigeResetModalReferences()
        {
            if (canvas == null)
            {
                return;
            }

            if (prestigeResetModal == null)
            {
                Transform modal = FindNamedDescendant(canvas.transform, "Prestige Reset Modal");
                prestigeResetModal = modal != null ? modal.gameObject : null;
            }

            if (prestigeResetModal == null)
            {
                return;
            }

            Transform root = prestigeResetModal.transform;
            if (prestigeResetTrainingText == null)
            {
                Transform progress = FindNamedDescendant(root, "PPO Training Progress");
                prestigeResetTrainingText = progress != null ? FindNamedDescendant(progress, "Text")?.GetComponent<TMP_Text>() : null;
            }

            if (prestigeResetInfoText == null)
            {
                prestigeResetInfoText = FindNamedDescendant(root, "ResetInfoText")?.GetComponent<TMP_Text>();
            }

            if (prestigeResetSlider == null)
            {
                prestigeResetSlider = prestigeResetModal.GetComponentInChildren<Slider>(true);
            }

            if (prestigeResetAmountText == null)
            {
                prestigeResetAmountText = FindNamedDescendant(root, "AmountText")?.GetComponent<TMP_Text>();
            }

            SetActive(prestigeResetAmountText != null ? prestigeResetAmountText.gameObject : null, false);

            if (prestigeResetBackButton == null)
            {
                Transform header = FindNamedDescendant(root, "Header");
                Transform back = header != null ? FindNamedDescendant(header, "Back") : null;
                back ??= FindNamedDescendant(root, "Back");
                prestigeResetBackButton = back != null ? back.GetComponent<Button>() : null;
            }

            if (prestigeResetBuyButton == null)
            {
                prestigeResetBuyButton = FindNamedDescendant(root, "Buy")?.GetComponent<Button>();
            }

            if (prestigeResetBackButton != null)
            {
                prestigeResetBackButton.onClick.RemoveAllListeners();
                prestigeResetBackButton.onClick.AddListener(ClosePrestigeResetModal);
            }

            if (prestigeResetBuyButton != null)
            {
                prestigeResetBuyButton.onClick.RemoveAllListeners();
                prestigeResetBuyButton.onClick.AddListener(ConfirmPrestigeReset);
            }

            SetActive(prestigeResetModal, false);
        }

        // ------------------------------------------------------------------------------------
        // Auto Buy — reward of the "First Prestige" goal. One toggle button per menu (Algorithms,
        // Upgrades, Agents, Modifiers); while a toggle is on, this ticker buys everything in that
        // menu the moment it becomes affordable. PPO is deliberately excluded: buying it flips the
        // game into the training flow and must stay a conscious player decision.
        // ------------------------------------------------------------------------------------
        private void EnsureAutoBuyButtons()
        {
            GameObject[] panels = { algorithmsPanel, upgradesPanel, agentsPanel, modifiersPanel };
            for (int i = 0; i < autoBuyButtons.Length && i < panels.Length; i++)
            {
                if (autoBuyButtons[i] == null && panels[i] != null)
                {
                    autoBuyButtons[i] = FindNamedDescendant(panels[i].transform, "AutoBuyButton")?.GetComponent<Button>();
                }

                if (autoBuyButtons[i] == null)
                {
                    continue;
                }

                var category = (AutoBuyCategory)i;
                autoBuyButtons[i].onClick.RemoveAllListeners();
                autoBuyButtons[i].onClick.AddListener(() => ToggleAutoBuy(category));
            }
        }

        private void ToggleAutoBuy(AutoBuyCategory category)
        {
            if (progression == null || !progression.AutoBuyUnlocked)
            {
                return;
            }

            progression.SetAutoBuyEnabled(category, !progression.GetAutoBuyEnabled(category));
            RefreshAutoBuyButtons();
        }

        private void RefreshAutoBuyButtons()
        {
            if (progression == null)
            {
                return;
            }

            bool unlocked = progression.AutoBuyUnlocked;
            for (int i = 0; i < autoBuyButtons.Length; i++)
            {
                Button button = autoBuyButtons[i];
                if (button == null)
                {
                    continue;
                }

                SetActive(button.gameObject, unlocked);
                if (unlocked)
                {
                    bool enabled = progression.GetAutoBuyEnabled((AutoBuyCategory)i);
                    SetButtonText(button, $"<b>Auto Buy</b>\n{(enabled ? "Enabled" : "Disabled")}");
                }
            }
        }

        private void TickAutoBuy()
        {
            if (progression == null || !progression.AutoBuyUnlocked)
            {
                return;
            }

            autoBuyTimer += Time.deltaTime;
            if (autoBuyTimer < 0.75f)
            {
                return;
            }

            autoBuyTimer = 0f;
            bool boughtAny = false;
            if (progression.GetAutoBuyEnabled(AutoBuyCategory.Algorithms))
            {
                boughtAny |= AutoBuySolversTick();
            }

            if (progression.GetAutoBuyEnabled(AutoBuyCategory.Upgrades))
            {
                boughtAny |= AutoBuyUpgradesTick();
            }

            if (progression.GetAutoBuyEnabled(AutoBuyCategory.Agents))
            {
                boughtAny |= AutoBuyAgentsTick();
            }

            if (progression.GetAutoBuyEnabled(AutoBuyCategory.Modifiers))
            {
                boughtAny |= AutoBuyModifiersTick();
            }

            if (boughtAny)
            {
                // Buying a solver also selects it (same as the manual buy path) — mirror that into
                // the bootstrap's local selection state before the UI refresh.
                selectedSolverId = progression.SelectedSolver;
                solverDeselected = progression.SolverDeselected;
                progression.Save();
                QueueAchievementNotificationsForNewCompletions();
                RefreshEverything();
            }
        }

        private bool AutoBuySolversTick()
        {
            // Buying a solver selects it — never yank the board out of a running PPO session.
            if (progression.SelectedSolver == SolverId.MachineLearning && progression.IsSolverUnlocked(SolverId.MachineLearning))
            {
                return false;
            }

            bool bought = false;
            foreach (SolverDefinition definition in StackMergeSolverCatalog.Definitions)
            {
                if (!definition.Available || definition.Id == SolverId.MachineLearning || progression.IsSolverUnlocked(definition.Id))
                {
                    continue;
                }

                if (progression.Chips >= progression.GetSolverUnlockCost(definition.Id) && progression.SelectOrUnlockSolver(definition.Id))
                {
                    bought = true;
                }
            }

            return bought;
        }

        private bool AutoBuyUpgradesTick()
        {
            bool TryBuy(bool available, long cost, Func<bool> buy)
            {
                return available && cost > 0 && progression.Chips >= cost && buy();
            }

            bool boughtAny = false;
            bool boughtThisPass = true;
            int guard = 0;
            while (boughtThisPass && guard++ < 50)
            {
                boughtThisPass = false;
                boughtThisPass |= TryBuy(!progression.AutoSolveUnlocked && progression.HasPurchasedSolver, progression.GetAutoSolveCost(), progression.ToggleOrBuyAutoSolve);
                boughtThisPass |= TryBuy(!progression.AutoRestartUnlocked && progression.HasPurchasedSolver, progression.GetAutoRestartCost(), progression.ToggleOrBuyAutoRestart);
                boughtThisPass |= TryBuy(!progression.SolverTuningUnlocked, progression.GetSolverTuningUnlockCost(), progression.BuySolverTuningUnlock);
                boughtThisPass |= TryBuy(progression.CanUnlockAgentsMenu && !progression.AgentsMenuUnlocked, progression.GetAgentsMenuUnlockCost(), progression.BuyAgentsMenuUnlock);
                boughtThisPass |= TryBuy(progression.AgentsMenuUnlocked && !progression.ExtraAgentSlotUnlocked, progression.GetExtraAgentSlotUpgradeCost(), progression.BuyExtraAgentSlotUpgrade);
                boughtThisPass |= TryBuy(progression.CanUnlockModifiersMenu && !progression.ModifiersMenuUnlocked, progression.GetModifiersMenuUnlockCost(), progression.BuyModifiersMenuUnlock);
                boughtThisPass |= TryBuy(!progression.IsMaxSpeed, progression.GetSpeedUpgradeCost(), progression.BuySpeedUpgrade);
                boughtThisPass |= TryBuy(!progression.IsMaxComputeSpeed, progression.GetComputeSpeedUpgradeCost(), progression.BuyComputeSpeedUpgrade);
                boughtThisPass |= TryBuy(!progression.IsMaxStackCapacity, progression.GetStackCapacityUpgradeCost(), progression.BuyStackCapacityUpgrade);
                boughtThisPass |= TryBuy(!progression.IsMaxQueuePreview, progression.GetQueuePreviewUpgradeCost(), progression.BuyQueuePreviewUpgrade);
                boughtThisPass |= TryBuy(!progression.IsMaxIncome, progression.GetIncomeUpgradeCost(), progression.BuyIncomeUpgrade);
                boughtThisPass |= TryBuy(!progression.IsMaxDifficulty, progression.GetDifficultyUpgradeCost(), progression.BuyDifficultyUpgrade);
                boughtThisPass |= TryBuy(progression.ScalingFrequencyPurchasable && !progression.IsMaxScalingFrequency, progression.GetScalingFrequencyUpgradeCost(), progression.BuyScalingFrequencyUpgrade);
                boughtThisPass |= TryBuy(!progression.IsMaxProfitableEnding, progression.GetProfitableEndingUpgradeCost(), progression.BuyProfitableEndingUpgrade);
                boughtThisPass |= TryBuy(!progression.IsMaxPassiveYield, progression.GetPassiveYieldUpgradeCost(), progression.BuyPassiveYieldUpgrade);
                boughtThisPass |= TryBuy(progression.PassiveSupportUpgradesUnlocked && !progression.IsMaxPassiveTickRate, progression.GetPassiveTickRateUpgradeCost(), progression.BuyPassiveTickRateUpgrade);
                boughtThisPass |= TryBuy(progression.PassiveSupportUpgradesUnlocked && !progression.IsMaxActiveMultiplier, progression.GetActiveMultiplierUpgradeCost(), progression.BuyActiveMultiplierUpgrade);
                boughtThisPass |= TryBuy(!progression.IsMaxComboEngine, progression.GetComboEngineUpgradeCost(), progression.BuyComboEngineUpgrade);
                boughtThisPass |= TryBuy(!progression.IsMaxSalvageProtocol, progression.GetSalvageProtocolUpgradeCost(), progression.BuySalvageProtocolUpgrade);
                boughtThisPass |= TryBuy(!progression.IsMaxTokenDividend, progression.GetTokenDividendUpgradeCost(), progression.BuyTokenDividendUpgrade);
                boughtAny |= boughtThisPass;
            }

            return boughtAny;
        }

        private bool AutoBuyAgentsTick()
        {
            if (!progression.AgentsMenuUnlocked)
            {
                return false;
            }

            bool bought = false;
            foreach (AgentDefinition agent in StackMergeProgression.Agents)
            {
                if (progression.IsAgentUnlocked(agent.Id))
                {
                    continue;
                }

                if (progression.Chips >= progression.GetAgentCost(agent.Id) && progression.BuyOrToggleAgent(agent.Id))
                {
                    bought = true;
                }
            }

            return bought;
        }

        private bool AutoBuyModifiersTick()
        {
            if (!progression.ModifiersMenuUnlocked)
            {
                return false;
            }

            bool boughtAny = false;
            bool boughtThisPass = true;
            int guard = 0;
            while (boughtThisPass && guard++ < 40)
            {
                boughtThisPass = false;
                foreach (ModifierDefinition modifier in StackMergeProgression.Modifiers)
                {
                    if (progression.IsModifierMaxed(modifier.Id))
                    {
                        continue;
                    }

                    if (progression.Chips >= progression.GetModifierUpgradeCost(modifier.Id) && progression.BuyModifierUpgrade(modifier.Id))
                    {
                        boughtThisPass = true;
                        boughtAny = true;
                    }
                }
            }

            return boughtAny;
        }

        // ------------------------------------------------------------------------------------
        // Datacenter layer UI — hand-built "Datacenter Panel" in the scene. References are found
        // by row-root names (CPU Rack, Training Cluster, Power Grid, ...) so nothing needs manual
        // dragging, but every serialized field wins when assigned in the Inspector. The panel also
        // hosts the Research tree and the Prestige Console; those keep their own existing wiring
        // (researchCards / prestigeSummaryText / prestigeButton fields).
        // ------------------------------------------------------------------------------------
        private static readonly string[] DatacenterAllocationRowNames = { "Training Cluster", "Analysis Node", "Market Bots" };
        private static readonly string[] DatacenterRackRowNames = { "CPU Rack", "GPU Rack", "TPU Pod", "Neural Fabric" };
        private static readonly string[] DatacenterFacilityRowNames = { "Power Grid", "Cooling Loop", "Fabric Interconnect" };

        private void EnsureDatacenterReferences()
        {
            if (canvas == null)
            {
                return;
            }

            if (datacenterPanel == null)
            {
                Transform panel = FindNamedDescendant(canvas.transform, "Datacenter Panel");
                datacenterPanel = panel != null ? panel.gameObject : null;
            }

            if (datacenterPanel == null)
            {
                return;
            }

            Transform root = datacenterPanel.transform;
            if (datacenterComputeText == null)
            {
                datacenterComputeText = FindNamedDescendant(root, "ComputeAmountText")?.GetComponent<TMP_Text>();
            }

            Transform allocationSection = FindNamedDescendant(root, "Compute Allocation");
            if (allocationSection != null)
            {
                if (datacenterAllocationTotalText == null)
                {
                    Transform title = FindNamedDescendant(allocationSection, "Title");
                    datacenterAllocationTotalText = title != null ? FindNamedDescendant(title, "%Text")?.GetComponent<TMP_Text>() : null;
                }

                if (datacenterAllocationTotalSlider == null)
                {
                    Transform totalSlider = FindNamedDescendant(allocationSection, "AllocatedSlider");
                    datacenterAllocationTotalSlider = totalSlider != null ? totalSlider.GetComponentInChildren<Slider>(true) : null;
                }

                for (int i = 0; i < datacenterAllocationRows.Length && i < DatacenterAllocationRowNames.Length; i++)
                {
                    datacenterAllocationRows[i] ??= new DatacenterAllocationRow();
                    Transform row = FindNamedDescendant(allocationSection, DatacenterAllocationRowNames[i]);
                    if (row == null)
                    {
                        continue;
                    }

                    datacenterAllocationRows[i].percentText ??= FindNamedDescendant(row, "%Text")?.GetComponent<TMP_Text>();
                    datacenterAllocationRows[i].prodText ??= FindNamedDescendant(row, "ProdText")?.GetComponent<TMP_Text>();
                    datacenterAllocationRows[i].descText ??= FindNamedDescendant(row, "DescText")?.GetComponent<TMP_Text>();
                    if (datacenterAllocationRows[i].slider == null)
                    {
                        datacenterAllocationRows[i].slider = row.GetComponentInChildren<Slider>(true);
                    }
                }
            }

            Transform rackSection = FindNamedDescendant(root, "Server Racks");
            if (rackSection != null && datacenterRackUnitsText == null)
            {
                Transform title = FindNamedDescendant(rackSection, "Title");
                datacenterRackUnitsText = title != null ? FindNamedDescendant(title, "UnitText")?.GetComponent<TMP_Text>() : null;
            }

            EnsureDatacenterItemRows(rackSection ?? root, datacenterRackRows, DatacenterRackRowNames);
            Transform facilitySection = FindNamedDescendant(root, "Facility Upgrades");
            EnsureDatacenterItemRows(facilitySection ?? root, datacenterFacilityRows, DatacenterFacilityRowNames);

            WireDatacenterControls();
        }

        private static void EnsureDatacenterItemRows(Transform sectionRoot, DatacenterItemRow[] rows, string[] rowNames)
        {
            if (sectionRoot == null)
            {
                return;
            }

            for (int i = 0; i < rows.Length && i < rowNames.Length; i++)
            {
                rows[i] ??= new DatacenterItemRow();
                Transform row = FindNamedDescendant(sectionRoot, rowNames[i]);
                if (row == null)
                {
                    continue;
                }

                rows[i].prodText ??= FindNamedDescendant(row, "ProdText")?.GetComponent<TMP_Text>();
                rows[i].unitsText ??= FindNamedDescendant(row, "UnitsText")?.GetComponent<TMP_Text>();
                rows[i].buyButton ??= FindNamedDescendant(row, "Buy")?.GetComponent<Button>();
            }
        }

        private void WireDatacenterControls()
        {
            for (int i = 0; i < datacenterAllocationRows.Length; i++)
            {
                Slider slider = datacenterAllocationRows[i]?.slider;
                if (slider == null)
                {
                    continue;
                }

                var allocationId = (DatacenterAllocationId)i;
                slider.minValue = 0f;
                slider.maxValue = 100f;
                slider.onValueChanged.RemoveAllListeners();
                slider.onValueChanged.AddListener(value => OnDatacenterAllocationChanged(allocationId, value));
            }

            EnsureDatacenterAllocationBar();

            for (int i = 0; i < datacenterRackRows.Length; i++)
            {
                Button buy = datacenterRackRows[i]?.buyButton;
                if (buy == null)
                {
                    continue;
                }

                var rackId = (DatacenterRackId)i;
                buy.onClick.RemoveAllListeners();
                buy.onClick.AddListener(() => BuyDatacenterRack(rackId));
            }

            for (int i = 0; i < datacenterFacilityRows.Length; i++)
            {
                Button buy = datacenterFacilityRows[i]?.buyButton;
                if (buy == null)
                {
                    continue;
                }

                var facilityId = (DatacenterFacilityId)i;
                buy.onClick.RemoveAllListeners();
                buy.onClick.AddListener(() => BuyDatacenterFacility(facilityId));
            }
        }

        // Converts the AllocatedSlider node into a three-segment stacked bar: the Slider component
        // is disabled (its plain fill hidden) and three colored Images are created inside its Fill
        // Area (which already has a RectMask2D, so segments clip to the bar's rounded shape). The
        // segment colors are taken from each allocation row's own slider fill, so recoloring the
        // rows in the scene recolors the bar too.
        private void EnsureDatacenterAllocationBar()
        {
            if (datacenterAllocationTotalSlider == null)
            {
                return;
            }

            Transform fillArea = FindNamedDescendant(datacenterAllocationTotalSlider.transform, "Fill Area");
            RectTransform container = fillArea as RectTransform ?? datacenterAllocationTotalSlider.transform as RectTransform;
            if (container == null)
            {
                return;
            }

            datacenterAllocationTotalSlider.interactable = false;
            datacenterAllocationTotalSlider.enabled = false;
            if (datacenterAllocationTotalSlider.fillRect != null)
            {
                SetActive(datacenterAllocationTotalSlider.fillRect.gameObject, false);
            }

            datacenterAllocationSegments = new Image[DatacenterAllocationRowNames.Length];
            for (int i = 0; i < datacenterAllocationSegments.Length; i++)
            {
                string segmentName = $"Allocation Segment {i}";
                Transform existing = container.Find(segmentName);
                GameObject segmentObject = existing != null ? existing.gameObject : new GameObject(segmentName, typeof(RectTransform), typeof(Image));
                RectTransform rect = segmentObject.GetComponent<RectTransform>();
                if (existing == null)
                {
                    rect.SetParent(container, false);
                }

                Image image = segmentObject.GetComponent<Image>();
                image.raycastTarget = false;
                image.color = GetDatacenterAllocationColor(i);
                datacenterAllocationSegments[i] = image;
            }
        }

        private Color GetDatacenterAllocationColor(int index)
        {
            Slider rowSlider = index >= 0 && index < datacenterAllocationRows.Length ? datacenterAllocationRows[index]?.slider : null;
            Image fill = rowSlider != null && rowSlider.fillRect != null ? rowSlider.fillRect.GetComponent<Image>() : null;
            return fill != null ? fill.color : DatacenterAllocationFallbackColors[Mathf.Clamp(index, 0, DatacenterAllocationFallbackColors.Length - 1)];
        }

        private void OnDatacenterAllocationChanged(DatacenterAllocationId allocationId, float sliderValue)
        {
            if (progression == null || !progression.DatacenterUnlocked)
            {
                RefreshDatacenterPanel();
                return;
            }

            progression.SetDatacenterAllocation(allocationId, sliderValue / 100f);
            // Refresh snaps the slider back if the requested share was clamped by the 100% budget.
            RefreshDatacenterPanel();
        }

        private void BuyDatacenterRack(DatacenterRackId rackId)
        {
            if (progression == null || !progression.BuyDatacenterRack(rackId))
            {
                return;
            }

            progression.Save();
            RefreshDatacenterPanel();
            RefreshProgressionUi();
        }

        private void BuyDatacenterFacility(DatacenterFacilityId facilityId)
        {
            if (progression == null || !progression.BuyDatacenterFacility(facilityId))
            {
                return;
            }

            progression.Save();
            RefreshDatacenterPanel();
            RefreshProgressionUi();
        }

        private void TickDatacenterProduction()
        {
            if (progression == null)
            {
                return;
            }

            progression.TickDatacenter(Time.deltaTime);

            bool panelVisible = datacenterPanel != null ? datacenterPanel.activeInHierarchy : selectedTabIndex == 7;
            if (!panelVisible)
            {
                return;
            }

            datacenterUiRefreshTimer += Time.deltaTime;
            if (datacenterUiRefreshTimer >= 0.5f)
            {
                datacenterUiRefreshTimer = 0f;
                RefreshDatacenterPanel();
            }
        }

        private void RefreshEconomyUiIfChanged()
        {
            if (progression == null)
            {
                return;
            }

            long chips = progression.Chips;
            long insight = progression.ResearchInsight;
            long tokens = progression.Tokens;
            if (chips == lastObservedChips && insight == lastObservedInsight && tokens == lastObservedTokens)
            {
                return;
            }

            lastObservedChips = chips;
            lastObservedInsight = insight;
            lastObservedTokens = tokens;

            RefreshHud();
            RefreshCurrencyDependentUi();
        }

        private void RefreshCurrencyDependentUi()
        {
            switch (selectedTabIndex)
            {
                case 1:
                    RefreshSolverButtons();
                    RefreshSolverDetails();
                    RefreshAlgorithmCards();
                    break;
                case 2:
                    RefreshUpgradeButtons();
                    RefreshAutoBuyButtons();
                    break;
                case 3:
                    RefreshAgentButtons();
                    RefreshAgentDetails();
                    RefreshAgentCards();
                    RefreshAgentSlotCards();
                    break;
                case 4:
                    RefreshModifierButtons();
                    RefreshModifierDetails();
                    RefreshModifierCards();
                    break;
                case 5:
                    RefreshResearchMenu();
                    break;
                case 7:
                    RefreshDatacenterPanel();
                    break;
            }

            RefreshTabButtons();
            RefreshGameplayModifiers();
            RefreshButtonVisualStates();
        }

        private void RefreshDatacenterPanel()
        {
            if (progression == null)
            {
                return;
            }

            bool unlocked = progression.DatacenterUnlocked;
            double totalGigaflops = progression.DatacenterTotalGigaflops;

            SetText(datacenterComputeText, unlocked
                ? $"Total compute: {FormatFlops(totalGigaflops)}"
                : $"Locked — reach Prestige {StackMergeProgression.DatacenterUnlockPrestigeCount} ({progression.PrestigeCount}/{StackMergeProgression.DatacenterUnlockPrestigeCount})");

            // --- Allocation ---
            float allocatedTotal = progression.DatacenterAllocatedFraction;
            SetText(datacenterAllocationTotalText, $"{Mathf.RoundToInt(allocatedTotal * 100f)}% allocated");
            if (datacenterAllocationSegments != null)
            {
                // Stacked bar: each segment spans its share of the 0..1 anchor range; whatever is
                // unallocated stays background on the right.
                float segmentStart = 0f;
                for (int i = 0; i < datacenterAllocationSegments.Length; i++)
                {
                    Image segment = datacenterAllocationSegments[i];
                    if (segment == null)
                    {
                        continue;
                    }

                    float share = progression.GetDatacenterAllocation((DatacenterAllocationId)i);
                    RectTransform rect = segment.rectTransform;
                    rect.anchorMin = new Vector2(segmentStart, 0f);
                    rect.anchorMax = new Vector2(Mathf.Clamp01(segmentStart + share), 1f);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    segment.color = GetDatacenterAllocationColor(i);
                    segmentStart = Mathf.Clamp01(segmentStart + share);
                }
            }
            else if (datacenterAllocationTotalSlider != null)
            {
                datacenterAllocationTotalSlider.SetValueWithoutNotify(allocatedTotal * 100f);
            }

            for (int i = 0; i < datacenterAllocationRows.Length; i++)
            {
                DatacenterAllocationRow row = datacenterAllocationRows[i];
                if (row == null)
                {
                    continue;
                }

                var allocationId = (DatacenterAllocationId)i;
                float share = progression.GetDatacenterAllocation(allocationId);
                SetText(row.percentText, $"{Mathf.RoundToInt(share * 100f)}%");
                if (row.slider != null)
                {
                    row.slider.SetValueWithoutNotify(share * 100f);
                    row.slider.interactable = unlocked;
                }

                switch (allocationId)
                {
                    case DatacenterAllocationId.TrainingCluster:
                        SetText(row.prodText, GetDatacenterAllocationDescription(allocationId));
                        break;
                    case DatacenterAllocationId.AnalysisNode:
                        SetText(row.prodText, GetDatacenterAllocationDescription(allocationId));
                        break;
                    case DatacenterAllocationId.MarketBots:
                        SetText(row.prodText, $"×{progression.DatacenterMarketMultiplier:0.00}");
                        break;
                }
                SetText(row.prodText, GetDatacenterAllocationProduction(allocationId));
                SetText(row.descText, GetDatacenterAllocationBody(allocationId));
            }

            // --- Racks ---
            SetText(datacenterRackUnitsText, $"{progression.TotalDatacenterRackUnits} units");
            for (int i = 0; i < datacenterRackRows.Length; i++)
            {
                DatacenterItemRow row = datacenterRackRows[i];
                if (row == null)
                {
                    continue;
                }

                var rackId = (DatacenterRackId)i;
                int count = progression.GetDatacenterRackCount(rackId);
                double unitFlops = progression.GetDatacenterRackUnitGigaflops(rackId);
                long cost = progression.GetDatacenterRackCost(rackId);
                SetText(row.prodText, $"unit: {FormatFlops(unitFlops)} - total: {FormatFlops(count * unitFlops)}");
                SetText(row.unitsText, $"x{count}");
                if (row.buyButton != null)
                {
                    SetButtonText(row.buyButton, unlocked ? $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}" : "Locked");
                    row.buyButton.interactable = unlocked && progression.Chips >= cost;
                }
            }

            // --- Facilities ---
            for (int i = 0; i < datacenterFacilityRows.Length; i++)
            {
                DatacenterItemRow row = datacenterFacilityRows[i];
                if (row == null)
                {
                    continue;
                }

                var facilityId = (DatacenterFacilityId)i;
                int level = progression.GetDatacenterFacilityLevel(facilityId);
                int maxLevel = StackMergeProgression.DatacenterFacilities[i].MaxLevel;
                bool maxed = progression.IsDatacenterFacilityMaxed(facilityId);
                SetText(row.prodText, facilityId switch
                {
                    DatacenterFacilityId.PowerGrid => $"+{level * 5}% rack FLOPS",
                    DatacenterFacilityId.CoolingLoop => $"+{level * 10}% rack FLOPS",
                    _ => $"×{1.0 + level * 0.18:0.00} TPU / Fabric output"
                });
                SetText(row.prodText, GetDatacenterFacilityEffectText(facilityId, level));
                SetText(row.unitsText, $"{level}/{maxLevel}");
                if (row.buyButton != null)
                {
                    long cost = progression.GetDatacenterFacilityCost(facilityId);
                    SetButtonText(row.buyButton, !unlocked ? "Locked" : maxed ? "Maxed" : $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}");
                    row.buyButton.interactable = unlocked && !maxed && progression.Chips >= cost;
                }
            }
        }

        private string GetDatacenterAllocationProduction(DatacenterAllocationId allocationId)
        {
            return allocationId switch
            {
                DatacenterAllocationId.TrainingCluster => $"{progression.DatacenterTrainingFramesPerSecond:N0} frames/s",
                DatacenterAllocationId.AnalysisNode => $"{progression.DatacenterInsightPerSecond:0.00} <sprite name=\"insight\" tint=1> /s",
                DatacenterAllocationId.MarketBots => $"x{progression.DatacenterMarketMultiplier:0.00} <sprite name=\"chips\" tint=1>",
                _ => string.Empty
            };
        }

        private static string GetDatacenterAllocationBody(DatacenterAllocationId allocationId)
        {
            bool magyar = StackMergeLocalization.CurrentLanguage == StackMergeLanguage.Magyar;
            return allocationId switch
            {
                DatacenterAllocationId.TrainingCluster => magyar
                    ? "Passzívan tanítja a PPO-t, ami a Normál Mód frames-be beleszámít. <b>A PPO permanens tanul akkor is, amikor nincs használva.</b>"
                    : "Passively trains PPO, which counts as Training Mode frames. <b>PPO learns permanently even when not in use.</b>",
                DatacenterAllocationId.AnalysisNode => magyar
                    ? "Passzívan termel <sprite name=\"insight\" tint=1>-ot a játék bármelyik szakaszában, így a PPO-t innentől kezdve nem kell ehhez Normál Módban használni."
                    : "Passively generates <sprite name=\"insight\" tint=1> at any stage of the game, so using PPO in Normal Mode is no longer required for that.",
                DatacenterAllocationId.MarketBots => magyar
                    ? "Bevételi szorzód ad mindennek, ami bármilyen formában <sprite name=\"chips\" tint=1>-et termel."
                    : "It gives an income multiplier to everything that produces <sprite name=\"chips\" tint=1> in any form.",
                _ => string.Empty
            };
        }

        private string GetDatacenterAllocationDescription(DatacenterAllocationId allocationId)
        {
            bool magyar = StackMergeLocalization.CurrentLanguage == StackMergeLanguage.Magyar;
            return allocationId switch
            {
                DatacenterAllocationId.TrainingCluster => magyar
                    ? $"{progression.DatacenterTrainingFramesPerSecond:N0} frame/s" // magyar
                    : $"{progression.DatacenterTrainingFramesPerSecond:N0} frames/s", // angol
                DatacenterAllocationId.AnalysisNode => magyar
                    ? $"{progression.DatacenterInsightPerSecond:0.00} <sprite name=\"insight\" tint=1>/s" // magyar
                    : $"{progression.DatacenterInsightPerSecond:0.00} <sprite name=\"insight\" tint=1>/s", // angol
                DatacenterAllocationId.MarketBots => magyar
                    ? $"x{progression.DatacenterMarketMultiplier:0.00} <sprite name=\"chips\" tint=1>" // magyar
                    : $"x{progression.DatacenterMarketMultiplier:0.00} <sprite name=\"chips\" tint=1>", // angol
                _ => string.Empty
            };
        }

        private static string GetDatacenterFacilityEffectText(DatacenterFacilityId facilityId, int level)
        {
            return facilityId switch
            {
                DatacenterFacilityId.PowerGrid => $"+5% rack FLOPS - total: +{level * 5}%",
                DatacenterFacilityId.CoolingLoop => $"+10% rack FLOPS - total: +{level * 10}%",
                _ => $"x1.18 TPU / Fabric output - total: x{1.0 + level * 0.18:0.00}"
            };
        }

        private static string FormatFlops(double gigaflops)
        {
            if (gigaflops >= 1_000_000_000d)
            {
                return $"{gigaflops / 1_000_000_000d:0.#} EF/s";
            }

            if (gigaflops >= 1_000_000d)
            {
                return $"{gigaflops / 1_000_000d:0.#} PF/s";
            }

            if (gigaflops >= 1_000d)
            {
                return $"{gigaflops / 1_000d:0.#} TF/s";
            }

            return $"{gigaflops:0.#} GF/s";
        }

        // ------------------------------------------------------------------------------------
        // Offline Progress Overlay — hand-built scene object. Shown on startup once Offline
        // Engine research is bought and a rewarded offline period was just credited. Deliberately
        // NOT closable by clicking the backdrop; only Back/Collect dismiss it (the reward itself
        // is already banked by ApplyOfflineProgress at load, Collect is an acknowledgement).
        // ------------------------------------------------------------------------------------
        private void EnsureOfflineProgressOverlayReferences()
        {
            if (canvas == null)
            {
                return;
            }

            if (offlineProgressOverlay == null)
            {
                Transform overlay = FindNamedDescendant(canvas.transform, "Offline Progress Overlay");
                offlineProgressOverlay = overlay != null ? overlay.gameObject : null;
            }

            if (offlineProgressOverlay == null)
            {
                return;
            }

            Transform root = offlineProgressOverlay.transform;
            if (offlineGainText == null)
            {
                offlineGainText = FindNamedDescendant(root, "GainText")?.GetComponent<TMP_Text>();
            }

            if (offlineTimeText == null)
            {
                Transform time = FindNamedDescendant(root, "Offline Time");
                Transform timeText = time != null ? FindNamedDescendant(time, "TimeText") : null;
                timeText ??= FindNamedDescendant(root, "TimeText");
                offlineTimeText = timeText != null ? timeText.GetComponent<TMP_Text>() : null;
            }

            if (offlineProgressSlider == null)
            {
                offlineProgressSlider = offlineProgressOverlay.GetComponentInChildren<Slider>(true);
            }

            if (offlineBackButton == null)
            {
                Transform header = FindNamedDescendant(root, "Header");
                Transform back = header != null ? FindNamedDescendant(header, "Back") : null;
                back ??= FindNamedDescendant(root, "Back");
                offlineBackButton = back != null ? back.GetComponent<Button>() : null;
            }

            if (offlineCollectButton == null)
            {
                offlineCollectButton = FindNamedDescendant(root, "CollectButton")?.GetComponent<Button>();
            }

            if (offlineBackButton != null)
            {
                offlineBackButton.onClick.RemoveAllListeners();
                offlineBackButton.onClick.AddListener(CloseOfflineProgressOverlay);
            }

            if (offlineCollectButton != null)
            {
                offlineCollectButton.onClick.RemoveAllListeners();
                offlineCollectButton.onClick.AddListener(CloseOfflineProgressOverlay);
            }

            SetActive(offlineProgressOverlay, false);
        }

        private void ShowOfflineProgressOverlayIfEarned()
        {
            EnsureOfflineProgressOverlayReferences();
            if (offlineProgressOverlay == null || progression == null)
            {
                return;
            }

            // Only once the research exists AND a measurable offline period was rewarded — an
            // empty "You made 0 chips in 0 min" popup on quick restarts would just look broken.
            if (progression.GetResearchLevel(ResearchId.OfflineEfficiency) < 1 || progression.LastOfflineHours <= 0.0)
            {
                return;
            }

            RefreshOfflineProgressOverlay();
            SetActive(offlineProgressOverlay, true);
            offlineProgressOverlay.transform.SetAsLastSibling();
        }

        private void CloseOfflineProgressOverlay()
        {
            SetActive(offlineProgressOverlay, false);
        }

        private void RefreshOfflineProgressOverlay()
        {
            if (progression == null)
            {
                return;
            }

            SetText(offlineGainText, $"You made <sprite name=\"chips\" tint=1> {FormatNumber(progression.LastOfflineChips)} and <sprite name=\"insight\" tint=1> {FormatNumber(progression.LastOfflineInsight)} while you were offline.");

            double hours = progression.LastOfflineHours;
            double cap = Math.Max(0.01, progression.OfflineHourCap);
            SetText(offlineTimeText, $"Offline reward time:\n<b>{FormatHoursShort(hours)} / {FormatHoursShort(cap)}</b>");

            if (offlineProgressSlider != null)
            {
                offlineProgressSlider.minValue = 0f;
                offlineProgressSlider.maxValue = 1f;
                offlineProgressSlider.SetValueWithoutNotify(Mathf.Clamp01((float)(hours / cap)));
            }
        }

        private static string FormatHoursShort(double hours)
        {
            int wholeHours = (int)hours;
            int minutes = (int)Math.Round((hours - wholeHours) * 60.0);
            if (minutes >= 60)
            {
                wholeHours++;
                minutes = 0;
            }

            if (wholeHours <= 0)
            {
                return $"{minutes} min";
            }

            return minutes > 0 ? $"{wholeHours}h {minutes} min" : $"{wholeHours}h";
        }

        private void OpenPrestigeResetModal()
        {
            EnsurePrestigeResetModalReferences();
            if (prestigeResetModal == null)
            {
                // No modal in the scene — keep the old immediate-prestige behaviour working.
                ExecutePrestige();
                return;
            }

            SetActive(prestigeResetModal, true);
            prestigeResetModal.transform.SetAsLastSibling();
            RefreshPrestigeResetModal();
        }

        private void ClosePrestigeResetModal()
        {
            SetActive(prestigeResetModal, false);
        }

        private void ConfirmPrestigeReset()
        {
            ClosePrestigeResetModal();
            ExecutePrestige();
        }

        private void RefreshPrestigeResetModal()
        {
            if (progression == null || prestigeResetModal == null || !prestigeResetModal.activeSelf)
            {
                return;
            }

            long frames = progression.MachineLearningCycleFrames;
            long required = Math.Max(1, progression.EffectivePlayingModeFrameRequirement);
            bool trained = progression.MachineLearningPlayingModeUnlocked;
            long gain = progression.PreviewPrestigeInsightGain();

            SetText(prestigeResetTrainingText, trained
                ? "PPO Training complete — prestige reset unlocked."
                : $"You have to finish PPO Training to unlock prestige.\n<b>{frames:N0} / {required:N0} cycle frames</b>");
            SetText(prestigeResetInfoText, PrestigeResetInfoBody);

            if (prestigeResetSlider != null)
            {
                prestigeResetSlider.minValue = 0f;
                prestigeResetSlider.maxValue = 1f;
                prestigeResetSlider.SetValueWithoutNotify(trained ? 1f : Mathf.Clamp01(frames / (float)required));
            }

            SetActive(prestigeResetAmountText != null ? prestigeResetAmountText.gameObject : null, false);
            if (prestigeResetBuyButton != null)
            {
                SetButtonText(prestigeResetBuyButton, $"Reset for <sprite name=\"insight\" tint=1> {FormatNumber(gain)}");
                prestigeResetBuyButton.interactable = gain > 0;
            }
        }

        private static Button FindPpoModeButton(Button[] buttons, params string[] names)
        {
            if (buttons == null || names == null)
            {
                return null;
            }

            string[] normalizedNames = names.Select(NormalizeLookupName).ToArray();
            return buttons.FirstOrDefault(button =>
            {
                string buttonName = NormalizeLookupName(button.name);
                return normalizedNames.Any(name => !string.IsNullOrEmpty(name) && buttonName.Contains(name));
            });
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
            ShowFeedbackModal($"{definition.DisplayName} tuning updated");
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
            ShowFeedbackModal($"{definition.DisplayName} tuning reset");
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
            ShowFeedbackModal(bought ? $"Speed level {progression.SpeedLevel}" : "Speed upgrade unavailable");
            progression.Save();
            RefreshEverything();
        }

        private void BuyComputeSpeedUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyComputeSpeedUpgrade();
            ShowFeedbackModal(bought ? $"Compute speed level {progression.ComputeSpeedLevel}" : "Compute speed upgrade unavailable");
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
            ShowFeedbackModal(changed ? "Auto restart updated" : progression.HasPurchasedSolver ? "Not enough <sprite name=\"chips\" tint=1>" : "Buy an algorithm first");
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
            ShowFeedbackModal(changed ? "Auto solve updated" : progression.HasPurchasedSolver ? "Not enough <sprite name=\"chips\" tint=1>" : "Buy an algorithm first");
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
            ShowFeedbackModal(bought ? $"+{progression.GetTokenPackSize()} tokens" : "Not enough <sprite name=\"chips\" tint=1>");
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
            ShowFeedbackModal(bought ? "Solver tuning unlocked" : "Not enough <sprite name=\"chips\" tint=1>");
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
            ShowFeedbackModal(bought ? "+1 agent slot unlocked" : !progression.AgentsMenuUnlocked ? "Unlock Agents first" : "Not enough <sprite name=\"chips\" tint=1>");
            progression.Save();
            RefreshEverything();
        }

        private void BuyStageProgressionUnlock()
        {
            if (progression == null)
            {
                return;
            }

            if (!progression.AgentsMenuUnlocked)
            {
                BuyAgentsMenuUnlock();
                return;
            }

            BuyModifiersMenuUnlock();
        }

        private void BuyModifiersMenuUnlock()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyModifiersMenuUnlock();
            ShowFeedbackModal(bought ? "Modifier Lab unlocked" : progression.CanUnlockModifiersMenu ? "Not enough <sprite name=\"chips\" tint=1>" : "Modifier Lab requirements not met");
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
                ShowFeedbackModal(progression.PrestigeAvailable ? "Run PPO in Normal mode first" : "Finish PPO Training first");
                RefreshEverything();
                return;
            }

            highScore = 0;
            PlayerPrefs.SetInt(HighScoreKey, 0);
            PlayerPrefs.Save();
            selectedSolverId = progression.SelectedSolver;
            solverDeselected = progression.SolverDeselected;
            selectedAgentId = AgentId.MergeBroker;
            selectedModifierId = ModifierId.UnstableStack;
            solverTuneOpen = false;
            historyOpen = false;
            achievementsOpen = false;
            gameplayInfoOpen = false;
            HidePpoModeModal();
            CreateFreshGame();
            ShowFeedbackModal($"Prestige complete: +{FormatNumber(gained)} <sprite name=\"insight\" tint=1>");
            progression.SaveImmediate(forceMachineLearningPolicySave: true);
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
            researchDetailModal?.transform.SetAsLastSibling();

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
            ShowFeedbackModal(bought
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
            ShowFeedbackModal(bought ? $"Stack capacity: {progression.StackCapacity}" : "Stack upgrade unavailable");
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
            ShowFeedbackModal(bought ? $"Next preview: {progression.QueueLength} blocks" : "Next preview upgrade unavailable");
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
            ShowFeedbackModal(bought ? $"Chip yield level {progression.IncomeLevel}" : "Income upgrade unavailable");
            progression.Save();
            RefreshEverything();
        }

        private void BuyComboEngineUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyComboEngineUpgrade();
            ShowFeedbackModal(bought ? $"Combo Engine level {progression.ComboEngineLevel}" : "Combo Engine upgrade unavailable");
            progression.Save();
            RefreshEverything();
        }

        private void BuySalvageProtocolUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuySalvageProtocolUpgrade();
            ShowFeedbackModal(bought ? $"Salvage Protocol level {progression.SalvageProtocolLevel}" : "Salvage Protocol upgrade unavailable");
            progression.Save();
            RefreshEverything();
        }

        private void BuyTokenDividendUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyTokenDividendUpgrade();
            ShowFeedbackModal(bought ? $"Token Dividend level {progression.TokenDividendLevel}" : "Token Dividend upgrade unavailable");
            progression.Save();
            RefreshEverything();
        }

        private void BuyCurriculumAmountUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyCurriculumAmountUpgrade();
            ShowFeedbackModal(bought ? $"PPO Curriculum level {progression.CurriculumAmountLevel}" : "Curriculum upgrade unavailable");
            progression.Save();
            RefreshEverything();
        }

        private void BuyCurriculumRateUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyCurriculumRateUpgrade();
            ShowFeedbackModal(bought ? $"Curriculum Rate level {progression.CurriculumRateLevel}" : "Curriculum Rate upgrade unavailable");
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
            ShowFeedbackModal(bought ? $"Risk level {progression.DifficultyLevel}" : "Risk upgrade unavailable");
            if (bought)
            {
                ApplyCurrentBoardSettingsToGameState();
            }

            progression.Save();
            RefreshEverything();
        }

        private void BuyScalingFrequencyUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyScalingFrequencyUpgrade();
            ShowFeedbackModal(bought ? $"Scaling frequency level {progression.ScalingFrequencyLevel}" : "Scaling frequency upgrade unavailable");
            if (bought)
            {
                ApplyCurrentBoardSettingsToGameState();
            }

            progression.Save();
            RefreshEverything();
        }

        private void BuyProfitableEndingUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyProfitableEndingUpgrade();
            ShowFeedbackModal(bought ? $"Profitable ending level {progression.ProfitableEndingLevel}" : "Profitable ending upgrade unavailable");
            progression.Save();
            RefreshEverything();
        }

        private void BuyPassiveYieldUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyPassiveYieldUpgrade();
            ShowFeedbackModal(bought ? $"Passive yield level {progression.PassiveYieldLevel}" : "Passive yield upgrade unavailable");
            progression.Save();
            RefreshEverything();
        }

        private void BuyPassiveTickRateUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyPassiveTickRateUpgrade();
            ShowFeedbackModal(bought ? $"Passive tick rate level {progression.PassiveTickRateLevel}" : "Passive tick rate upgrade unavailable");
            progression.Save();
            RefreshEverything();
        }

        private void BuyActiveMultiplierUpgrade()
        {
            if (progression == null)
            {
                return;
            }

            bool bought = progression.BuyActiveMultiplierUpgrade();
            ShowFeedbackModal(bought ? $"Active multiplier level {progression.ActiveMultiplierLevel}" : "Active multiplier upgrade unavailable");
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
            ShowFeedbackModal(bought ? $"{definition.DisplayName} level {progression.GetModifierLevel(modifierId)}" : "Modifier upgrade unavailable");
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
            string reason = progression.GetAgentsGateStatus();
            ShowFeedbackModal(bought ? "Agents menu unlocked" : !string.IsNullOrEmpty(reason) ? reason : "Not enough <sprite name=\"chips\" tint=1>");
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
                ShowFeedbackModal("Unlock Agents in Upgrades first");
                RefreshEverything();
                return;
            }

            AgentDefinition definition = progression.GetAgentDefinition(id);
            bool changed;
            if (!progression.IsAgentUnlocked(id))
            {
                changed = progression.BuyAgent(id);
                ShowFeedbackModal(changed ? $"Agent bought: {definition.DisplayName}" : "Not enough <sprite name=\"chips\" tint=1>");
            }
            else if (progression.IsAgentActive(id))
            {
                changed = progression.UnequipAgent(id);
                ShowFeedbackModal(changed ? $"Agent unequipped: {definition.DisplayName}" : "Agent unavailable");
            }
            else
            {
                changed = progression.EquipAgent(id);
                ShowFeedbackModal(changed ? $"Agent equipped: {definition.DisplayName}" : "No free agent slot");
            }

            progression.Save();
            RefreshEverything();
        }

        private void ApplyCurrentBoardSettingsToGameState()
        {
            if (gameState == null
                || (gameState.StackCapacity == progression.StackCapacity
                    && gameState.QueueLength == progression.QueueLength
                    && gameState.DifficultyLevel == progression.DifficultyLevel
                    && gameState.ScalingFrequencyLevel == progression.ScalingFrequencyLevel
                    && Math.Abs(gameState.ScalingFrequencyRewardBonus - progression.ScalingFrequencyRewardBonus) < 0.0001))
            {
                return;
            }

            StackMergeSnapshot snapshot = gameState.CreateSnapshot();
            var resizedGame = new StackMergeGameState(
                stackCapacity: progression.StackCapacity,
                queueLength: progression.QueueLength,
                difficultyLevel: progression.DifficultyLevel,
                scalingFrequencyLevel: progression.ScalingFrequencyLevel,
                modifiers: snapshot.Modifiers,
                seed: Environment.TickCount,
                scalingFrequencyRewardBonus: progression.ScalingFrequencyRewardBonus);
            resizedGame.RestoreSnapshotResized(snapshot);
            gameState = resizedGame;
        }

        private void RefreshEverything()
        {
            RefreshGameView();
            RefreshProgressionUi();
            QueueAchievementNotificationsForNewCompletions();
            FlushDynamicUiLayout();
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
            SetText(
                droppedText,
                gameState.BlocksDropped <= 0
                    ? "The run hasn't started yet."
                    : $"{FormatNumber(gameState.BlocksDropped)} moves");

            bool trainingView = progression != null
                && progression.IsMachineLearningTrainingActive
                && selectedTabIndex == 0
                && !historyOpen
                && !achievementsOpen;

            RefreshGameplayModifiers();
            SetTrainingView(trainingView);
            bool trainingOverlayVisible = trainingView && trainingOverlay != null;
            if (trainingOverlayVisible)
            {
                UpdateTrainingOverlay(null);
            }
            else
            {
                RefreshColumns();
                RefreshNextBlocks();
            }

            RefreshGameOver();
            RefreshHud();
        }

        private void SetTrainingView(bool active)
        {
            EnsureTrainingOverlay();
            RectTransform nextPanel = GetNextBlocksPanel();
            bool boardVisible = boardRoot != null && boardRoot.gameObject.activeSelf;
            bool nextVisible = nextPanel != null && nextPanel.gameObject.activeSelf;
            bool viewChanged = boardVisible == active || nextVisible == active || (trainingOverlay != null && trainingOverlay.gameObject.activeSelf != active);

            if (boardRoot != null)
            {
                SetActive(boardRoot.gameObject, !active);
            }

            if (nextPanel != null)
            {
                SetActive(nextPanel.gameObject, !active);
            }

            if (boardVisible == active || nextVisible == active)
            {
                boardLayoutDirty = true;
            }

            if (trainingOverlay != null)
            {
                SetActive(trainingOverlay.gameObject, active);
                if (active && viewChanged)
                {
                    nextTrainingOverlayRefreshTime = 0f;
                    nextTrainingRunInfoRefreshTime = 0f;
                    TryLayoutGameplaySections();
                    Canvas.ForceUpdateCanvases();
                    PositionTrainingOverlay();
                    trainingOverlay.SetAsLastSibling();
                }
            }

        }

        private void EnsureTrainingOverlay()
        {
            if (canvas == null)
            {
                return;
            }

            if (trainingOverlay == null)
            {
                trainingOverlay = FindNamedDescendant(canvas.transform, "PPO Training Overlay") as RectTransform;
            }

            if (trainingOverlay != null)
            {
                trainingOverlayText ??= FindNamedDescendant(trainingOverlay, "Training Text")?.GetComponent<TMP_Text>()
                    ?? trainingOverlay.GetComponentInChildren<TMP_Text>(true);
                if (trainingOverlayText != null)
                {
                    trainingOverlayText.richText = true;
                    trainingOverlayText.alignment = TextAlignmentOptions.TopLeft;
                    trainingOverlayText.textWrappingMode = TextWrappingModes.NoWrap;
                    trainingOverlayText.overflowMode = TextOverflowModes.Overflow;
                }
            }
        }

        private void PositionTrainingOverlay()
        {
            if (trainingOverlay == null || boardRoot == null)
            {
                return;
            }

            RectTransform parent = trainingOverlay.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            RectTransform gameplaySectionsParent = boardRoot.parent as RectTransform;
            if (gameplaySectionsParent == null)
            {
                return;
            }

            GetRectInParent(parent, gameplaySectionsParent, out Vector2 min, out Vector2 max);
            float desiredHeight = GetTrainingOverlayDesiredHeight();
            Vector2 size = new(Mathf.Max(1f, max.x - min.x), Mathf.Max(1f, desiredHeight));
            Vector2 center = new((min.x + max.x) * 0.5f, max.y - size.y * 0.5f);

            trainingOverlay.anchorMin = new Vector2(0.5f, 0.5f);
            trainingOverlay.anchorMax = new Vector2(0.5f, 0.5f);
            trainingOverlay.pivot = new Vector2(0.5f, 0.5f);
            trainingOverlay.anchoredPosition = center;
            trainingOverlay.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
            trainingOverlay.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
        }

        private static void EncapsulateRectInParent(RectTransform parent, RectTransform target, ref Vector2 min, ref Vector2 max)
        {
            if (target == null)
            {
                return;
            }

            GetRectInParent(parent, target, out Vector2 targetMin, out Vector2 targetMax);
            min = Vector2.Min(min, targetMin);
            max = Vector2.Max(max, targetMax);
        }

        private static void GetRectInParent(RectTransform parent, RectTransform target, out Vector2 min, out Vector2 max)
        {
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector3 local = parent.InverseTransformPoint(corners[0]);
            min = local;
            max = local;
            for (int i = 1; i < corners.Length; i++)
            {
                local = parent.InverseTransformPoint(corners[i]);
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
        }

        private void UpdateTrainingOverlay(string statusLine)
        {
            EnsureTrainingOverlay();
            if (gameState == null || progression == null)
            {
                return;
            }

            if (Time.unscaledTime < nextTrainingOverlayRefreshTime)
            {
                UpdatePpoTrainingRunInfo(statusLine);
                return;
            }

            nextTrainingOverlayRefreshTime = Time.unscaledTime + TrainingOverlayRefreshInterval;
            if (trainingOverlayText != null)
            {
                float characterWidthPercent = GetTrainingMatrixCharacterWidthPercent();
                var builder = new StringBuilder();
                builder.AppendLine("<align=\"center\">Next</align>");
                builder.AppendLine(BuildPositionedTrainingLine(
                    gameState.NextBlocks.Select(FormatMatrixCell).ToArray(),
                    ppoTrainingMatrixNextWidthPercent,
                    characterWidthPercent));
                builder.AppendLine();
                builder.Append(BuildTrainingMatrix(characterWidthPercent));
                trainingOverlayText.text = builder.ToString();
            }

            UpdatePpoTrainingRunInfo(statusLine);
            if (trainingOverlay != null && trainingOverlay.gameObject.activeInHierarchy)
            {
                TryLayoutGameplaySections();
                PositionTrainingOverlay();
            }
        }

        private void UpdatePpoTrainingRunInfo(string statusLine)
        {
            if (gameState == null || progression == null)
            {
                return;
            }

            if (Time.unscaledTime < nextTrainingRunInfoRefreshTime)
            {
                return;
            }

            nextTrainingRunInfoRefreshTime = Time.unscaledTime + TrainingOverlayRefreshInterval;
            StackMergePpoMetrics metrics = progression.MachineLearningAgent.Metrics;
            string summary = string.IsNullOrWhiteSpace(statusLine)
                ? $"{FormatNumber(metrics.Steps)} frames"
                : statusLine;

            SetText(runStatusText, summary);
            RebuildTextLayout(runStatusText);
            SetText(feedbackText, BuildPpoTrainingFeedback(metrics));
        }

        private string BuildPpoTrainingFeedback(StackMergePpoMetrics metrics)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"Policy loss: {metrics.LastPolicyLoss:0.000}");
            builder.AppendLine($"Value loss: {metrics.LastValueLoss:0.000}");
            builder.AppendLine($"Entropy: {metrics.LastEntropy:0.000}");
            builder.Append($"Updates: {FormatNumber(metrics.Updates)}");
            return builder.ToString();
        }

        private string BuildTrainingMatrix(float characterWidthPercent)
        {
            if (gameState == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            int capacity = gameState.StackCapacity;
            string[] cells = new string[gameState.StackCount];
            for (int row = capacity - 1; row >= 0; row--)
            {
                for (int stackIndex = 0; stackIndex < gameState.StackCount; stackIndex++)
                {
                    IReadOnlyList<int> stack = gameState.Stacks[stackIndex];
                    cells[stackIndex] = row < stack.Count ? FormatMatrixCell(stack[row]) : ".";
                }

                builder.AppendLine(BuildPositionedTrainingLine(cells, ppoTrainingMatrixBoardWidthPercent, characterWidthPercent));
            }

            return builder.ToString();
        }

        private string BuildPositionedTrainingLine(IReadOnlyList<string> cells, float widthPercent, float characterWidthPercent)
        {
            if (cells == null || cells.Count == 0)
            {
                return string.Empty;
            }

            widthPercent = Mathf.Clamp(widthPercent, 1f, 100f);
            float leftPercent = (100f - widthPercent) * 0.5f;
            float stepPercent = widthPercent / cells.Count;
            string mspace = Mathf.Max(0.01f, ppoTrainingMatrixMspaceEm).ToString("0.###", CultureInfo.InvariantCulture);
            var builder = new StringBuilder();
            builder.Append("<align=\"left\">");
            for (int i = 0; i < cells.Count; i++)
            {
                string cell = string.IsNullOrEmpty(cells[i]) ? "." : cells[i];
                float centerPercent = leftPercent + stepPercent * (i + 0.5f);
                float textWidthPercent = characterWidthPercent * cell.Length;
                float startPercent = Mathf.Clamp(centerPercent - textWidthPercent * 0.5f, 0f, 100f);
                string posText = startPercent.ToString("0.###", CultureInfo.InvariantCulture);
                builder.Append("<pos=");
                builder.Append(posText);
                builder.Append("%><mspace=");
                builder.Append(mspace);
                builder.Append("em>");
                builder.Append(cell);
                builder.Append("</mspace>");
            }

            builder.Append("</align>");
            return builder.ToString();
        }

        private float GetTrainingMatrixCharacterWidthPercent()
        {
            float textWidth = 0f;
            if (trainingOverlayText != null)
            {
                RectTransform rect = trainingOverlayText.rectTransform;
                if (rect != null)
                {
                    textWidth = rect.rect.width;
                }
            }

            if (textWidth <= 1f && trainingOverlay != null)
            {
                textWidth = trainingOverlay.rect.width;
            }

            if (textWidth <= 1f)
            {
                return 1.5f;
            }

            float fontSize = trainingOverlayText != null ? Mathf.Max(1f, trainingOverlayText.fontSize) : 22f;
            return Mathf.Max(0.01f, ppoTrainingMatrixMspaceEm) * fontSize / textWidth * 100f;
        }

        private string FormatMatrixCell(int value)
        {
            // The matrix is monospaced plain text, so rich-text tags (Power's <sup>) are disabled.
            return value == StackMergeGameState.JokerBlockValue ? "J" : FormatBlockValue(value, blockNumeralStyle, allowRichText: false);
        }

        // ------------------------------------------------------------------------------------
        // Block numeral skins (goal rewards). Every style stays numerically readable — block
        // values are exact powers of two, which Byte / Hexadecimal / Power / Roman-tier exploit
        // for compact, ASCII-safe renderings. Only block faces and the PPO training matrix go
        // through this; economy numbers (chips, costs, insight) keep the standard formatting.
        // ------------------------------------------------------------------------------------
        private string FormatBlockValue(long value)
        {
            return FormatBlockValue(value, blockNumeralStyle, allowRichText: true);
        }

        private static string FormatBlockValue(long value, BlockNumeralStyle style, bool allowRichText)
        {
            if (value <= 0)
            {
                return value.ToString();
            }

            switch (style)
            {
                case BlockNumeralStyle.Byte:
                    return FormatByteValue(value);
                case BlockNumeralStyle.Hexadecimal:
                    return $"0x{value:X}";
                case BlockNumeralStyle.Power:
                {
                    int exponent = ExactPowerOfTwoExponent(value);
                    if (exponent < 1)
                    {
                        return value.ToString();
                    }

                    return allowRichText ? $"2<sup>{exponent}</sup>" : $"2^{exponent}";
                }

                case BlockNumeralStyle.Roman:
                {
                    // Roman shows the TIER (the power-of-two exponent): 1024 = X, 32768 = XV.
                    int exponent = ExactPowerOfTwoExponent(value);
                    return exponent >= 1 ? ToRoman(exponent) : value.ToString();
                }

                case BlockNumeralStyle.Scientific:
                {
                    if (value < 1000)
                    {
                        return value.ToString();
                    }

                    int exponent = (int)Math.Floor(Math.Log10(value));
                    double mantissa = value / Math.Pow(10, exponent);
                    return $"{mantissa:0.0}e{exponent}";
                }

                default:
                    return value.ToString();
            }
        }

        // Block values are powers of two, so byte units are always EXACT (1KB = 1024).
        private static string FormatByteValue(long value)
        {
            const long Kilo = 1024L;
            if (value < Kilo)
            {
                return $"{value}B";
            }

            if (value < Kilo * Kilo)
            {
                return $"{value / Kilo}KB";
            }

            if (value < Kilo * Kilo * Kilo)
            {
                return $"{value / (Kilo * Kilo)}MB";
            }

            return $"{value / (Kilo * Kilo * Kilo)}GB";
        }

        /// <summary>Returns n for value == 2^n (n ≥ 1); -1 for anything that isn't a power of two.</summary>
        private static int ExactPowerOfTwoExponent(long value)
        {
            if (value < 2 || (value & (value - 1)) != 0)
            {
                return -1;
            }

            int exponent = 0;
            while (value > 1)
            {
                value >>= 1;
                exponent++;
            }

            return exponent;
        }

        private static string ToRoman(int number)
        {
            if (number <= 0 || number >= 4000)
            {
                return number.ToString();
            }

            (int Value, string Symbol)[] table =
            {
                (1000, "M"), (900, "CM"), (500, "D"), (400, "CD"), (100, "C"), (90, "XC"),
                (50, "L"), (40, "XL"), (10, "X"), (9, "IX"), (5, "V"), (4, "IV"), (1, "I")
            };

            var builder = new StringBuilder();
            foreach ((int value, string symbol) in table)
            {
                while (number >= value)
                {
                    builder.Append(symbol);
                    number -= value;
                }
            }

            return builder.ToString();
        }

        private static readonly string[] BlockNumeralLabels = { "Standard", "Byte", "Hexadecimal", "Power", "Roman", "Scientific" };

        private void RefreshBlockNumeralDropdown()
        {
            if (blockNumeralDropdown == null)
            {
                return;
            }

            ConfigureDropdownTextReferences(blockNumeralDropdown);
            var options = new List<TMP_Dropdown.OptionData>(BlockNumeralLabels.Length);
            for (int i = 0; i < BlockNumeralLabels.Length; i++)
            {
                bool unlocked = i == 0 || (progression != null && progression.IsBlockNumeralUnlocked((BlockNumeralStyle)i));
                string label = StackMergeLocalization.Translate(BlockNumeralLabels[i]);
                options.Add(new TMP_Dropdown.OptionData(unlocked ? label : $"{label} ({StackMergeLocalization.Translate("Locked")})"));
            }

            blockNumeralDropdown.options = options;
            if (progression != null && !progression.IsBlockNumeralUnlocked(blockNumeralStyle))
            {
                blockNumeralStyle = BlockNumeralStyle.Standard;
            }

            blockNumeralDropdown.SetValueWithoutNotify((int)blockNumeralStyle);
            blockNumeralDropdown.RefreshShownValue();
            ApplyDropdownSelectionBackground(blockNumeralDropdown);
        }

        private void SetBlockNumeralFromDropdown(int index)
        {
            if (syncingSettingsControls)
            {
                return;
            }

            var style = (BlockNumeralStyle)Mathf.Clamp(index, 0, BlockNumeralLabels.Length - 1);
            if (progression != null && !progression.IsBlockNumeralUnlocked(style))
            {
                ShowFeedbackModal("Complete its goal to unlock this numeral");
                RefreshBlockNumeralDropdown();
                return;
            }

            blockNumeralStyle = style;
            PlayerPrefs.SetInt(BlockNumeralSettingKey, (int)style);
            PlayerPrefs.Save();
            RefreshColumns();
            RefreshNextBlocks();
            RefreshBlockNumeralDropdown();
        }

        private void RefreshGameplayModifiers()
        {
            EnsureGameplayModifierReferences();
            bool pickaxeUnlocked = IsGameplayModifierUnlocked(ModifierId.MinersPickaxe);
            bool queueScrubberUnlocked = IsGameplayModifierUnlocked(ModifierId.QueueScrubber);
            bool showSection = ShouldShowGameplayModifiersSection();

            if (!showSection && armedGameplayModifier != NoArmedGameplayModifier)
            {
                ClearArmedGameplayModifier();
            }

            if (gameplayModifiersSection != null)
            {
                bool wasActive = gameplayModifiersSection.activeSelf;
                SetActive(gameplayModifiersSection, showSection);
                if (showSection && gameplayModifiersSection.transform is RectTransform modifierRect)
                {
                    modifierRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, GameplayModifiersSectionHeight);
                }

                if (wasActive != showSection)
                {
                    boardLayoutDirty = true;
                }
            }

            RefreshGameplayModifierButton(
                gameplayMinersPickaxeButton,
                gameplayMinersPickaxeAmountText,
                ModifierId.MinersPickaxe,
                pickaxeUnlocked && showSection);
            RefreshGameplayModifierButton(
                gameplayQueueScrubberButton,
                gameplayQueueScrubberAmountText,
                ModifierId.QueueScrubber,
                queueScrubberUnlocked && showSection);
        }

        private bool ShouldShowGameplayModifiersSection()
        {
            return selectedTabIndex == 0
                && (IsGameplayModifierUnlocked(ModifierId.MinersPickaxe)
                    || IsGameplayModifierUnlocked(ModifierId.QueueScrubber));
        }

        private void RefreshGameplayModifierButton(Button button, TMP_Text amountText, ModifierId modifierId, bool visible)
        {
            if (button == null)
            {
                return;
            }

            SetActive(button.gameObject, visible);
            if (!visible)
            {
                return;
            }

            if (IsGameplayModifierArmed(modifierId) && !CanArmGameplayModifier(modifierId))
            {
                ClearArmedGameplayModifier();
            }

            bool armed = IsGameplayModifierArmed(modifierId);
            bool canArm = CanArmGameplayModifier(modifierId);
            button.interactable = armed || canArm;
            SetText(amountText, FormatNumber(GetGameplayModifierRemaining(modifierId)));
            ApplyButtonVisualState(button);
            if (armed)
            {
                ApplyGameplayModifierArmedVisual(button);
            }
        }

        private void ApplyGameplayModifierArmedVisual(Button button)
        {
            if (button == null)
            {
                return;
            }

            Image image = GetButtonVisualImage(button);
            if (image == null)
            {
                return;
            }

            if (!buttonNormalColors.TryGetValue(button, out Color normalColor))
            {
                normalColor = image.color;
                buttonNormalColors[button] = normalColor;
            }

            image.color = DarkenButtonColor(normalColor);
        }

        private void RefreshHud()
        {
            // Global Status Bar lives outside Tab Content (a sibling, not a per-panel child), so its
            // visibility has to be driven explicitly here instead of by the per-tab SetActive calls
            // in SelectTab/OpenHistoryPanel/OpenAchievementsPanel. RefreshHud already runs after
            // every one of those, so this one spot covers all navigation paths.
            bool showGlobalStatusBar = !historyOpen && !achievementsOpen && !solverTuneOpen && selectedTabIndex != 6;
            SetActive(globalStatusBarRoot, showGlobalStatusBar);
            if (showGlobalStatusBar && globalStatusBarRoot != null && !gameplayInfoOpen)
            {
                globalStatusBarRoot.transform.SetAsLastSibling();
            }

            if (progression == null)
            {
                return;
            }

            bool machineLearningTraining = progression.IsMachineLearningTrainingActive;
            SetText(chipsTexts, $"<sprite name=\"chips\" tint=1> {FormatNumber(progression.Chips)}");
            SetText(insightsTexts, $"<sprite name=\"insight\" tint=1> {FormatNumber(progression.ResearchInsight)}");
            SetText(tokensText, $"<sprite name=\"token\" tint=1> {FormatNumber(progression.Tokens)}");

            if (!progression.HasPurchasedSolver || solverDeselected)
            {
                SetText(solverText, "Manual");
            }
            else
            {
                // PPO shows just its name; its "knowledge %" goes in the feedback line (the old
                // "Lv N" read like a separate progression track, which confused players).
                SetText(solverText, $"{GetSelectedSolver().DisplayName}");
            }
            SetText(speedText, machineLearningTraining
                ? $"Speed L{progression.SpeedLevel} | {progression.MoveInterval:0.000}s | training"
                : $"Speed L{progression.SpeedLevel} | {progression.MoveInterval:0.00}s");

            if (gameState != null && !gameState.IsGameOver)
            {
                if (machineLearningTraining)
                {
                    UpdatePpoTrainingRunInfo(null);
                }
                else
                {
                    SetText(runStatusText, IsManualModeActive() ? "Manual mode" : "Auto solving");
                }
            }

            RebuildStatusBarLayout();
        }

        private void RebuildStatusBarLayout()
        {
            // TMP reports stale preferred sizes on the first frame and after dynamic text changes,
            // so ContentSizeFitter + LayoutGroup status pills need an immediate rebuild.
            RebuildCurrencyLayout(chipsTexts);
            RebuildCurrencyLayout(insightsTexts);
            RebuildTextLayout(tokensText);
            RebuildTextLayout(runStatusText);
            RebuildTextLayout(solverText);
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

        // Forces TMP to (re)generate its mesh and rebuilds the layout chain immediately, so
        // ContentSizeFitter status pills resize before the next rendered frame.
        private static void RebuildTextLayout(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            text.ForceMeshUpdate(true, true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(text.rectTransform);

            List<RectTransform> targets = new();
            Transform t = text.transform;
            while (t != null)
            {
                if (t is RectTransform rect
                    && (t.GetComponent<LayoutGroup>() != null
                        || t.GetComponent<ContentSizeFitter>() != null
                        || t.GetComponent<LayoutElement>() != null))
                {
                    targets.Add(rect);
                }

                t = t.parent;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(targets[i]);
            }

            Canvas.ForceUpdateCanvases();

            for (int i = targets.Count - 1; i >= 0; i--)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(targets[i]);
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
            RefreshAutoBuyButtons();
            RefreshResearchMenu();
            RefreshHistory();
            RefreshAchievements();
            RefreshTabButtons();
            RefreshGameplayInfo();
            RefreshGameplayModifiers();
            RefreshButtonVisualStates();
            RefreshGameplayModifiers();
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
                // Removed solvers (Available=false) are hidden from the shop; their button object is
                // disabled so a layout group re-flows around it. Flip Available back to true to restore.
                if (button.gameObject.activeSelf != definition.Available)
                {
                    button.gameObject.SetActive(definition.Available);
                }

                if (!definition.Available)
                {
                    continue;
                }

                bool unlocked = progression.IsSolverUnlocked(definition.Id);
                bool selectedInPanel = selectedSolverId == definition.Id;
                bool active = progression.SelectedSolver == definition.Id;
                bool machineLearningGateReady = definition.Id != SolverId.MachineLearning || progression.CanUnlockMachineLearning;

                string label = selectedInPanel ? $"> {definition.DisplayName}" : definition.DisplayName;
                if (unlocked)
                {
                    label += active
                        ? solverDeselected ? "\nDeselected" : "\nSelected"
                        : "\nUnlocked";
                }
                else if (definition.Id == SolverId.MachineLearning && !machineLearningGateReady)
                {
                    label += "\nNeeds modifiers";
                }
                else
                {
                    label += $"\n<sprite name=\"chips\" tint=1> {FormatNumber(definition.Cost)}";
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
                ? $"{progression.GetMachineLearningGateStatus()}\n<sprite name=\"chips\" tint=1> {FormatNumber(definition.Cost)}"
                : $"Unlock this algorithm to reveal details.";
            // PPO runtime statistics are intentionally not shown here — they live in the History menu.
            SetText(solverDetailInfoText, unlocked ? definition.Description : lockedInfo);
            if (solverDetailTuneButton != null)
            {
                SetActive(solverDetailTuneButton.gameObject, canTune);
                SetButtonText(solverDetailTuneButton, "Tune");
                solverDetailTuneButton.interactable = canTune;
            }

            if (!unlocked)
            {
                SetText(solverDetailStatusText, isMachineLearning && !machineLearningGateReady ? "Stage locked" : "Locked");
                SetButtonText(solverDetailActionButton, isMachineLearning && !machineLearningGateReady ? "Needs all\nModifiers" : $"Unlock\n<sprite name=\"chips\" tint=1> {FormatNumber(definition.Cost)}");
                if (solverDetailActionButton != null)
                {
                    solverDetailActionButton.interactable = machineLearningGateReady && progression.Chips >= definition.Cost;
                }
                return;
            }

            string statusLabel = active
                ? (!isMachineLearning && solverDeselected ? "Deselected" : "Selected")
                : "Unlocked";
            SetText(
                solverDetailStatusText,
                statusLabel);
            if (isMachineLearning && active)
            {
                SetButtonText(solverDetailActionButton, "Change mode");
                if (solverDetailActionButton != null) solverDetailActionButton.interactable = true;
            }
            else if (!isMachineLearning && active)
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
                // Removed solvers (Available=false) hide their whole card so the shop shows only the
                // 7 kept solvers (+ PPO). Flip Available back to true in the catalog to restore.
                if (card.gameObject.activeSelf != definition.Available)
                {
                    card.gameObject.SetActive(definition.Available);
                }

                if (!definition.Available)
                {
                    continue;
                }

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
                        : $"Buy\n<sprite name=\"chips\" tint=1> {FormatNumber(definition.Cost)}");
                    if (card.actionButton != null)
                    {
                        card.actionButton.interactable = machineLearningGateReady && progression.Chips >= definition.Cost;
                    }
                }
                else if (isMachineLearning && active)
                {
                    // PPO stays clickable while selected so you can re-open the Training/Normal mode
                    // popup — otherwise switching mode meant deselecting to another solver and back.
                    SetButtonText(card.actionButton, "Change mode");
                    if (card.actionButton != null) card.actionButton.interactable = true;
                }
                else if (!isMachineLearning && active)
                {
                    SetButtonText(card.actionButton, solverDeselected ? "Select" : "Deselect");
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
                    // Hide tuning until this solver is owned, tuning is unlocked, and it has tunable parameters.
                    bool showTuneButton = unlocked && progression.SolverTuningUnlocked && tuningDefinition.HasParameters;
                    card.tuneButton.gameObject.SetActive(showTuneButton);
                    card.tuneButton.interactable = showTuneButton;
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

            SetManualContentHeight(root, y);
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
            ShowFeedbackModal($"{definition.DisplayName} tuning updated");
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

            var builder = new StringBuilder();
            builder.AppendLine($"Stack capacity: {progression.StackCapacity}/{StackMergeGameState.MaxStackCapacity}");
            builder.AppendLine($"Difficulty: Level {progression.DifficultyLevel}");
            builder.AppendLine($"Speed: Level {progression.SpeedLevel} ({progression.MoveInterval:0.00}s)");
            builder.AppendLine($"Auto solving: {(progression.AutoSolveEnabled ? "ON" : "OFF")}");
            builder.AppendLine($"Auto restart: {(progression.AutoRestartEnabled ? progression.AutoRestartIsTokenFree ? "ON (free)" : $"ON ({progression.Tokens} tokens)" : "OFF")}");

            if (IsManualModeActive())
            {
                builder.AppendLine("Mode: Manual");
                if (gameState != null)
                {
                    builder.AppendLine($"Run score: {FormatNumber(gameState.Score)}");
                    builder.AppendLine($"Moves: {FormatNumber(gameState.BlocksDropped)}");
                    builder.AppendLine($"Merges: {FormatNumber(gameState.TotalMerges)}");
                    builder.AppendLine($"Current next: {(gameState.NextBlocks.Count > 0 ? FormatBlockValue(gameState.NextBlocks[0]) : "-")}");
                    builder.AppendLine($"Available actions: Pickaxe {gameState.PickaxeUsesRemaining}, Queue Scrubber {gameState.QueueSkipsRemaining}");
                }

                builder.AppendLine();
                builder.AppendLine("<b>Manual controls</b>");
                builder.AppendLine("Tap a stack to place the current next block.");
                if (IsGameplayModifierUnlocked(ModifierId.MinersPickaxe))
                {
                    builder.AppendLine("Equip Miner's Pickaxe, then tap a stack block to remove it.");
                }

                if (IsGameplayModifierUnlocked(ModifierId.QueueScrubber))
                {
                    builder.AppendLine("Equip Queue Scrubber, then tap the first next block to remove it.");
                }

                SetText(gameplayInfoText, builder.ToString());
                return;
            }

            SolverId solverId = progression.SelectedSolver;
            SolverDefinition solverDefinition = StackMergeSolverCatalog.GetDefinition(solverId);
            SolverTuningDefinition tuningDefinition = StackMergeSolverCatalog.GetTuningDefinition(solverId);
            SolverTuningSettings tuning = progression.GetSolverTuning(solverId);
            builder.AppendLine($"Solver: {solverDefinition.DisplayName}");
            if (solverId == SolverId.MachineLearning)
            {
                builder.AppendLine(progression.IsMachineLearningTrainingActive ? "Mode: PPO Training" : "Mode: PPO Normal");
            }

            if (progression.NeuralAcceleratorActive)
            {
                builder.AppendLine("Neural Accelerator: MOCA/MOCA+/MCTS speed boost active");
            }

            if (gameState != null)
            {
                builder.AppendLine($"Run modifiers: Unstable {gameState.UnstableSavesRemaining}, Pickaxe {gameState.PickaxeUsesRemaining}, Queue skip {gameState.QueueSkipsRemaining}");
                builder.AppendLine($"Special blocks: {(gameState.JokerBlocksEnabled ? "Joker ON" : "Joker OFF")}, Mirror: {(gameState.MirrorStackEnabled ? "ON" : "OFF")}");
            }
            builder.AppendLine();
            builder.AppendLine("<b>Solver tuning</b>");

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
                    label += $"\n<sprite name=\"chips\" tint=1> {FormatNumber(progression.GetAgentCost(definition.Id))}";
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
                long agentCost = progression.GetAgentCost(selectedAgentId);
                SetText(agentDetailStatusText, "Locked");
                SetButtonText(agentDetailActionButton, $"Buy\n<sprite name=\"chips\" tint=1> {FormatNumber(agentCost)}");
                if (agentDetailActionButton != null)
                {
                    agentDetailActionButton.interactable = progression.Chips >= agentCost;
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
        // Desired shop DISPLAY order (independent of the AgentId/ModifierId enum order the data arrays
        // must keep). Edit these to reorder the UI — never reorder the definition arrays.
        private static readonly AgentId[] AgentDisplayOrder =
        {
            AgentId.RestartSponsor, AgentId.HighwaterAnalyst, AgentId.Quartermaster, AgentId.ScoreAuditor,
            AgentId.Overclocker, AgentId.VelocityTrader, AgentId.MoveDividend, AgentId.MergeBroker, AgentId.TokenProspector
        };

        private static readonly ModifierId[] ModifierDisplayOrder =
        {
            ModifierId.NeuralAccelerator, ModifierId.UnstableStack, ModifierId.MinersPickaxe, ModifierId.QueueScrubber,
            ModifierId.CatalystStack, ModifierId.MirrorStack, ModifierId.Joker
        };

        private void RefreshAgentCards()
        {
            if (progression == null || agentCards == null)
            {
                return;
            }

            for (int order = 0; order < AgentDisplayOrder.Length; order++)
            {
                foreach (StackMergeAgentCard card in agentCards)
                {
                    if (card != null && card.agentId == AgentDisplayOrder[order])
                    {
                        card.transform.SetSiblingIndex(order);
                        break;
                    }
                }
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
                    long cardCost = progression.GetAgentCost(card.agentId);
                    SetButtonText(card.button, $"<sprite name=\"chips\" tint=1> {FormatNumber(cardCost)}");
                    if (card.button != null) card.button.interactable = progression.Chips >= cardCost;
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

            SetText(
                modifierSummaryText,
                $"Modifier Lab online. Active families: {unlockedCount}/{StackMergeProgression.Modifiers.Length} | Total levels: {totalLevels}\n" +
                "They expand the game rules, allowing for further production. Each solver is effective in different ways.");

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

            for (int order = 0; order < ModifierDisplayOrder.Length; order++)
            {
                foreach (StackMergeModifierCard card in modifierCards)
                {
                    if (card != null && card.modifierId == ModifierDisplayOrder[order])
                    {
                        card.transform.SetSiblingIndex(order);
                        break;
                    }
                }
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
                    SetButtonText(card.button, $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}");
                    if (card.button != null) card.button.interactable = progression.Chips >= cost;
                }
            }
        }

        private void RefreshUpgradeButtons()
        {
            string stageName = !progression.AgentsMenuUnlocked ? "Stage 1 - Core automation"
                : !progression.ModifiersMenuUnlocked ? "Stage 2 - Agent acceleration"
                : !progression.AllModifiersMaxed ? "Stage 3 - Modifier Lab"
                : !progression.IsSolverUnlocked(SolverId.MachineLearning) ? "Stage 4 - Machine Learning"
                : "Endgame - PPO training";
            // PPO runtime statistics are intentionally not shown here - they live in the History menu.
            string nextGoal = progression.IsSolverUnlocked(SolverId.MachineLearning)
                ? "Train PPO, then prestige from the Research menu."
                : progression.ModifiersMenuUnlocked
                ? progression.AllModifiersMaxed ? "PPO is ready to unlock in Algorithms." : "Max every Modifier to open the Machine Learning layer."
                : progression.AgentsMenuUnlocked
                ? progression.GetModifiersGateStatus()
                : progression.CanUnlockAgentsMenu ? "Open Agents to start the next stage." : "Unlock COMBO or LOOK solver to open Agents.";
            SetText(progressionStageNameText, stageName);
            SetText(progressionStageText, nextGoal);

            if (modifiersMenuUnlockButton != null)
            {
                if (!progression.AgentsMenuUnlocked)
                {
                    long cost = progression.GetAgentsMenuUnlockCost();
                    bool gateReady = progression.CanUnlockAgentsMenu;
                    SetButtonText(modifiersMenuUnlockButton, gateReady
                        ? $"Agents Lab\n<sprite name=\"chips\" tint=1> {FormatNumber(cost)}"
                        : "Agents Lab\nLocked");
                    modifiersMenuUnlockButton.interactable = gateReady && progression.Chips >= cost;
                }
                else
                {
                    // Single short two-line label so it fits: "Modifiers Lab\n{Locked | <chips> 3M | Unlocked}".
                    if (progression.ModifiersMenuUnlocked)
                    {
                        SetButtonText(modifiersMenuUnlockButton, "Modifiers Lab\nUnlocked");
                        modifiersMenuUnlockButton.interactable = false;
                    }
                    else
                    {
                        long cost = progression.GetModifiersMenuUnlockCost();
                        bool gateReady = progression.CanUnlockModifiersMenu;
                        SetButtonText(modifiersMenuUnlockButton, gateReady
                            ? $"Modifiers Lab\n<sprite name=\"chips\" tint=1> {FormatNumber(cost)}"
                            : "Modifiers Lab\nLocked");
                        modifiersMenuUnlockButton.interactable = gateReady && progression.Chips >= cost;
                    }
                }
            }

            // Solver Speed: collapsed from one button per level to a single button that always
            // targets the next level. Name shows the effect buying grants (not what you already
            // have — otherwise level 0 would misleadingly read "0% faster").
            if (speedUpgradeButton != null)
            {
                int level = progression.SpeedLevel;
                int maxLevel = progression.MaxSpeedLevel;
                float percent = StackMergeProgression.GetSpeedUpgradeEffectPercent(level);
                if (progression.IsMaxSpeed)
                {
                    SetUpgradeButtonLabels(speedUpgradeButton, UpgradeName("Faster solver", level, maxLevel), "Maxed", false, effect: BonusPercent(percent));
                }
                else
                {
                    float nextPercent = StackMergeProgression.GetSpeedUpgradeEffectPercent(level + 1);
                    long cost = progression.GetSpeedUpgradeCost();
                    SetUpgradeButtonLabels(speedUpgradeButton, UpgradeName("Faster solver", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: EffectRange(BonusPercent(percent), BonusPercent(nextPercent), false));
                }
            }

            // Compute Speed: a second, narrower speed lever. Only shrinks the pacing overhead of
            // the compute-heavy search solvers (Plan3/Plan5/MOCA/MOCA+/MCTS) — light solvers get
            // nothing from it, so it matters or not depending on which solver you actually run.
            if (computeSpeedUpgradeButton != null)
            {
                int level = progression.ComputeSpeedLevel;
                int maxLevel = progression.MaxComputeSpeedLevel;
                float percent = StackMergeProgression.GetComputeSpeedEffectPercent(level);
                if (progression.IsMaxComputeSpeed)
                {
                    SetUpgradeButtonLabels(computeSpeedUpgradeButton, UpgradeName("Solver delay", level, maxLevel), "Maxed", false, effect: ReductionPercent(percent));
                }
                else
                {
                    float nextPercent = StackMergeProgression.GetComputeSpeedEffectPercent(level + 1);
                    long cost = progression.GetComputeSpeedUpgradeCost();
                    SetUpgradeButtonLabels(computeSpeedUpgradeButton, UpgradeName("Solver delay", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: EffectRange(ReductionPercent(percent), ReductionPercent(nextPercent), false));
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
                        progression.HasPurchasedSolver ? $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}" : "Needs algorithm",
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
                        progression.HasPurchasedSolver ? $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}" : "Needs algorithm",
                        progression.HasPurchasedSolver && progression.Chips >= cost);
                }
            }

            if (tokenPackButton != null)
            {
                long cost = progression.GetTokenPackCost();
                SetUpgradeButtonLabels(tokenPackButton, $"+{progression.GetTokenPackSize()} tokens", $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost);
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
                    SetUpgradeButtonLabels(solverTuningUnlockButton, "Solver tuning", $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost);
                }
            }

            if (extraAgentSlotUpgradeButton != null)
            {
                if (!progression.AgentsMenuUnlocked)
                {
                    SetUpgradeButtonLabels(extraAgentSlotUpgradeButton, "+1 Agent slot", "Needs Agents", false);
                }
                else if (progression.ExtraAgentSlotUnlocked)
                {
                    SetUpgradeButtonLabels(extraAgentSlotUpgradeButton, "+1 Agent slot", "Unlocked", false);
                }
                else
                {
                    long cost = progression.GetExtraAgentSlotUpgradeCost();
                    SetUpgradeButtonLabels(extraAgentSlotUpgradeButton, "+1 Agent slot", $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost);
                }
            }

            if (agentsMenuUnlockButton != null)
            {
                SetActive(agentsMenuUnlockButton.gameObject, false);
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
                    SetUpgradeButtonLabels(
                        stackCapacityUpgradeButton,
                        UpgradeName("Stack rows", level, maxLevel),
                        "Maxed",
                        false,
                        "Increases the capacity of each stack by 1 per level.",
                        $"{progression.StackCapacity}");
                }
                else
                {
                    int nextCapacity = progression.StackCapacity + 1;
                    long cost = progression.GetStackCapacityUpgradeCost();
                    SetUpgradeButtonLabels(
                        stackCapacityUpgradeButton,
                        UpgradeName("Stack rows", level, maxLevel),
                        $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}",
                        progression.Chips >= cost,
                        "Increases the capacity of each stack by 1 per level.",
                        EffectRange($"{progression.StackCapacity}", $"{nextCapacity}", false));
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
                    SetUpgradeButtonLabels(
                        queuePreviewUpgradeButton,
                        UpgradeName("Visible blocks", level, maxLevel),
                        "Maxed",
                        false,
                        "Shows 1 more upcoming block per level.",
                        $"{progression.QueueLength}");
                }
                else
                {
                    long cost = progression.GetQueuePreviewUpgradeCost();
                    SetUpgradeButtonLabels(
                        queuePreviewUpgradeButton,
                        UpgradeName("Visible blocks", level, maxLevel),
                        $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}",
                        progression.Chips >= cost,
                        "Shows 1 more upcoming block per level.",
                        EffectRange($"{progression.QueueLength}", $"{progression.QueueLength + 1}", false));
                }
            }

            // Difficulty Scaling: single button, always targets the next level. Describes what the
            // upgrade actually does (raises the odds/ceiling of high-value block spawns) instead of
            // a bare "Risk L{level}" label that told the player nothing.
            if (difficultyUpgradeButton != null)
            {
                int level = progression.DifficultyLevel;
                int maxLevel = progression.MaxDifficultyLevel;
                if (progression.IsMaxDifficulty)
                {
                    float current = StackMergeProgression.GetDifficultyMaxTierBonus(level);
                    SetUpgradeButtonLabels(difficultyUpgradeButton, UpgradeName("Higher chance", level, maxLevel), "Maxed", false, effect: $"+{current:0.#} max tier");
                }
                else
                {
                    float current = StackMergeProgression.GetDifficultyMaxTierBonus(level);
                    float next = StackMergeProgression.GetDifficultyMaxTierBonus(level + 1);
                    long cost = progression.GetDifficultyUpgradeCost();
                    SetUpgradeButtonLabels(difficultyUpgradeButton, UpgradeName("Higher chance", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: EffectRange($"+{current:0.#} max tier", $"+{next:0.#}", false));
                }
            }

            if (scalingFrequencyUpgradeButton != null)
            {
                int level = progression.ScalingFrequencyLevel;
                int maxLevel = progression.MaxScalingFrequencyLevel;
                float current = progression.GetEffectiveScalingFrequencyEffectPercent(level);
                if (!progression.ScalingFrequencyPurchasable)
                {
                    float next = progression.GetEffectiveScalingFrequencyEffectPercent(level + 1);
                    SetUpgradeButtonLabels(scalingFrequencyUpgradeButton, UpgradeName("Higher frequency", level, maxLevel), "Needs Difficulty L1", false, effect: EffectRange(BonusPercent(current), BonusPercent(next), false));
                }
                else if (progression.IsMaxScalingFrequency)
                {
                    SetUpgradeButtonLabels(scalingFrequencyUpgradeButton, UpgradeName("Higher frequency", level, maxLevel), "Maxed", false, effect: BonusPercent(current));
                }
                else
                {
                    float next = progression.GetEffectiveScalingFrequencyEffectPercent(level + 1);
                    long cost = progression.GetScalingFrequencyUpgradeCost();
                    SetUpgradeButtonLabels(scalingFrequencyUpgradeButton, UpgradeName("Higher frequency", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: EffectRange(BonusPercent(current), BonusPercent(next), false));
                }
            }

            if (profitableEndingUpgradeButton != null)
            {
                int level = progression.ProfitableEndingLevel;
                int maxLevel = progression.MaxProfitableEndingLevel;
                float current = StackMergeProgression.GetProfitableEndingEffectPercent(level);
                if (progression.IsMaxProfitableEnding)
                {
                    SetUpgradeButtonLabels(profitableEndingUpgradeButton, UpgradeName("Ending profit", level, maxLevel), "Maxed", false, effect: BonusPercent(current));
                }
                else
                {
                    float next = StackMergeProgression.GetProfitableEndingEffectPercent(level + 1);
                    long cost = progression.GetProfitableEndingUpgradeCost();
                    SetUpgradeButtonLabels(profitableEndingUpgradeButton, UpgradeName("Ending profit", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: EffectRange(BonusPercent(current), BonusPercent(next), false));
                }
            }

            // Passive Yield: single button, always targets the next level. Chips/tick this grants —
            // 0 levels means Passive Production hasn't started producing anything yet.
            if (passiveYieldUpgradeButton != null)
            {
                int level = progression.PassiveYieldLevel;
                int maxLevel = progression.MaxPassiveYieldLevel;
                if (progression.IsMaxPassiveYield)
                {
                    long perTick = StackMergeProgression.GetPassiveYieldPerTick(level);
                    SetUpgradeButtonLabels(passiveYieldUpgradeButton, UpgradeName("Passive <sprite name=\"chips\" tint=1> / tick", level, maxLevel), "Maxed", false, effect: $"{FormatNumber(perTick)}");
                }
                else
                {
                    long perTick = StackMergeProgression.GetPassiveYieldPerTick(level);
                    long nextPerTick = StackMergeProgression.GetPassiveYieldPerTick(level + 1);
                    long cost = progression.GetPassiveYieldUpgradeCost();
                    SetUpgradeButtonLabels(passiveYieldUpgradeButton, UpgradeName("Passive <sprite name=\"chips\" tint=1> / tick", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: EffectRange($"{FormatNumber(perTick)}", $"{FormatNumber(nextPerTick)}", false));
                }
            }

            // Passive Tick Rate: single button, always targets the next level. Shows the resulting
            // tick interval buying grants (lower = more ticks per minute).
            if (passiveTickRateUpgradeButton != null)
            {
                int level = progression.PassiveTickRateLevel;
                int maxLevel = progression.MaxPassiveTickRateLevel;
                float interval = StackMergeProgression.GetPassiveTickInterval(level);
                if (!progression.PassiveSupportUpgradesUnlocked)
                {
                    float nextInterval = StackMergeProgression.GetPassiveTickInterval(level + 1);
                    SetUpgradeButtonLabels(passiveTickRateUpgradeButton, UpgradeName("Passive tick rate", level, maxLevel), "Needs Passive Yield L1", false, effect: EffectRange(SecondsValue(interval), SecondsValue(nextInterval), false));
                }
                else if (progression.IsMaxPassiveTickRate)
                {
                    SetUpgradeButtonLabels(passiveTickRateUpgradeButton, UpgradeName("Passive tick rate", level, maxLevel), "Maxed", false, effect: SecondsValue(interval));
                }
                else
                {
                    float nextInterval = StackMergeProgression.GetPassiveTickInterval(level + 1);
                    long cost = progression.GetPassiveTickRateUpgradeCost();
                    SetUpgradeButtonLabels(passiveTickRateUpgradeButton, UpgradeName("Passive tick rate", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: EffectRange(SecondsValue(interval), SecondsValue(nextInterval), false));
                }
            }

            // Active Multiplier: single button, always targets the next level. Bonus only applies
            // while a run is actively being played (see TickPassiveProduction in Update()).
            if (activeMultiplierUpgradeButton != null)
            {
                int level = progression.ActiveMultiplierLevel;
                int maxLevel = progression.MaxActiveMultiplierLevel;
                float current = StackMergeProgression.GetActiveMultiplierEffectPercent(level);
                if (!progression.PassiveSupportUpgradesUnlocked)
                {
                    float next = StackMergeProgression.GetActiveMultiplierEffectPercent(level + 1);
                    SetUpgradeButtonLabels(activeMultiplierUpgradeButton, UpgradeName("Active multiplier", level, maxLevel), "Needs Passive Yield L1", false, effect: EffectRange(BonusPercent(current), BonusPercent(next), false));
                }
                else if (progression.IsMaxActiveMultiplier)
                {
                    SetUpgradeButtonLabels(activeMultiplierUpgradeButton, UpgradeName("Active multiplier", level, maxLevel), "Maxed", false, effect: BonusPercent(current));
                }
                else
                {
                    float next = StackMergeProgression.GetActiveMultiplierEffectPercent(level + 1);
                    long cost = progression.GetActiveMultiplierUpgradeCost();
                    SetUpgradeButtonLabels(activeMultiplierUpgradeButton, UpgradeName("Active multiplier", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: EffectRange(BonusPercent(current), BonusPercent(next), false));
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
                float current = StackMergeProgression.GetIncomeEffectPercent(level);
                if (progression.IsMaxIncome)
                {
                    SetUpgradeButtonLabels(incomeUpgradeButton, UpgradeName("Base <sprite name=\"chips\" tint=1> income", level, maxLevel), "Maxed", false, effect: BonusPercent(current));
                }
                else
                {
                    float next = StackMergeProgression.GetIncomeEffectPercent(level + 1);
                    long cost = progression.GetIncomeUpgradeCost();
                    SetUpgradeButtonLabels(incomeUpgradeButton, UpgradeName("Base <sprite name=\"chips\" tint=1> income", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: EffectRange(BonusPercent(current), BonusPercent(next), false));
                }
            }

            // Combo Engine: shows the per-streak-step bonus buying grants.
            if (comboEngineUpgradeButton != null)
            {
                int level = progression.ComboEngineLevel;
                int maxLevel = progression.MaxComboEngineLevel;
                float current = progression.GetEffectiveComboEffectPercentPerStreak(level);
                if (progression.IsMaxComboEngine)
                {
                    SetUpgradeButtonLabels(comboEngineUpgradeButton, UpgradeName("Merge streak", level, maxLevel), "Maxed", false, effect: $"{BonusPercent(current)} / streak");
                }
                else
                {
                    float next = progression.GetEffectiveComboEffectPercentPerStreak(level + 1);
                    long cost = progression.GetComboEngineUpgradeCost();
                    SetUpgradeButtonLabels(comboEngineUpgradeButton, UpgradeName("Merge streak", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: $"{EffectRange(BonusPercent(current), BonusPercent(next), false)} / streak");
                }
            }

            // Salvage Protocol: shows the game-over score conversion share.
            if (salvageProtocolUpgradeButton != null)
            {
                int level = progression.SalvageProtocolLevel;
                int maxLevel = progression.MaxSalvageProtocolLevel;
                float current = StackMergeProgression.GetSalvageEffectPercent(level);
                if (progression.IsMaxSalvageProtocol)
                {
                    SetUpgradeButtonLabels(salvageProtocolUpgradeButton, UpgradeName("Score salvage", level, maxLevel), "Maxed", false, effect: BonusPercent(current));
                }
                else
                {
                    float next = StackMergeProgression.GetSalvageEffectPercent(level + 1);
                    long cost = progression.GetSalvageProtocolUpgradeCost();
                    SetUpgradeButtonLabels(salvageProtocolUpgradeButton, UpgradeName("Score salvage", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: EffectRange(BonusPercent(current), BonusPercent(next), false));
                }
            }

            // Token Dividend: shows the per-√(held tokens) income bonus.
            if (tokenDividendUpgradeButton != null)
            {
                int level = progression.TokenDividendLevel;
                int maxLevel = progression.MaxTokenDividendLevel;
                float current = StackMergeProgression.GetTokenDividendPercentPerSqrtToken(level);
                if (progression.IsMaxTokenDividend)
                {
                    SetUpgradeButtonLabels(tokenDividendUpgradeButton, UpgradeName("\u221a<sprite name=\"token\" tint=1> = <sprite name=\"chips\" tint=1>", level, maxLevel), "Maxed", false, effect: $"{BonusPercent(current)} / \u221atoken");
                }
                else
                {
                    float next = StackMergeProgression.GetTokenDividendPercentPerSqrtToken(level + 1);
                    long cost = progression.GetTokenDividendUpgradeCost();
                    SetUpgradeButtonLabels(tokenDividendUpgradeButton, UpgradeName("\u221a<sprite name=\"token\" tint=1> = <sprite name=\"chips\" tint=1>", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: $"{EffectRange(BonusPercent(current), BonusPercent(next), false)} / \u221atoken");
                }
            }

            // PPO Curriculum (amount): shaves the training frame requirement per tick. Gated on owning PPO.
            if (curriculumAmountUpgradeButton != null)
            {
                int level = progression.CurriculumAmountLevel;
                int maxLevel = progression.MaxCurriculumAmountLevel;
                long current = StackMergeProgression.GetCurriculumReductionPerTick(level);
                if (!progression.CurriculumUnlocked)
                {
                    long next = StackMergeProgression.GetCurriculumReductionPerTick(level + 1);
                    SetUpgradeButtonLabels(curriculumAmountUpgradeButton, UpgradeName("frames / tick", level, maxLevel), "Needs PPO or Bootcamp L5", false, effect: EffectRange(FrameReduction(current), FrameReduction(next), false));
                }
                else if (progression.IsMaxCurriculumAmount)
                {
                    SetUpgradeButtonLabels(curriculumAmountUpgradeButton, UpgradeName("frames / tick", level, maxLevel), "Maxed", false, effect: FrameReduction(current));
                }
                else
                {
                    long next = StackMergeProgression.GetCurriculumReductionPerTick(level + 1);
                    long cost = progression.GetCurriculumAmountUpgradeCost();
                    SetUpgradeButtonLabels(curriculumAmountUpgradeButton, UpgradeName("frames / tick", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: EffectRange(FrameReduction(current), FrameReduction(next), false));
                }
            }

            // PPO Curriculum (rate): tick interval. Gated on owning at least Curriculum L1.
            if (curriculumRateUpgradeButton != null)
            {
                int level = progression.CurriculumRateLevel;
                int maxLevel = progression.MaxCurriculumRateLevel;
                float interval = StackMergeProgression.GetCurriculumTickInterval(level);
                if (!progression.CurriculumRateUnlocked)
                {
                    float nextInterval = StackMergeProgression.GetCurriculumTickInterval(level + 1);
                    SetUpgradeButtonLabels(curriculumRateUpgradeButton, UpgradeName("Passive tick rate", level, maxLevel), "Needs Curriculum", false, effect: EffectRange(SecondsValue(interval), SecondsValue(nextInterval), false));
                }
                else if (progression.IsMaxCurriculumRate)
                {
                    SetUpgradeButtonLabels(curriculumRateUpgradeButton, UpgradeName("Passive tick rate", level, maxLevel), "Maxed", false, effect: SecondsValue(interval));
                }
                else
                {
                    float nextInterval = StackMergeProgression.GetCurriculumTickInterval(level + 1);
                    long cost = progression.GetCurriculumRateUpgradeCost();
                    SetUpgradeButtonLabels(curriculumRateUpgradeButton, UpgradeName("Passive tick rate", level, maxLevel), $"<sprite name=\"chips\" tint=1> {FormatNumber(cost)}", progression.Chips >= cost, effect: EffectRange(SecondsValue(interval), SecondsValue(nextInterval), false));
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
                // Static label — the adjacent prestigeSummaryText already spells out the gain/status,
                // so the button no longer duplicates it dynamically. Colour still reflects readiness.
                long gain = progression.PreviewPrestigeInsightGain();
                SetButtonText(prestigeButton, "Prestige");
                SetButtonColor(prestigeButton, HexColor(gain > 0 ? "#7C3AED" : "#334155"));

                // Always clickable: the button opens the Prestige Reset Modal, which shows training
                // progress and the exact payout — only the modal's Buy button gates the reset.
                prestigeButton.interactable = true;
            }

            RefreshPrestigeResetModal();

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
                    : $"{prefix}{definition.DisplayName}\nL{level}/{definition.MaxLevel}\n{FormatNumber(progression.GetResearchCost(researchId))} <sprite name=\"insight\" tint=1>";

                string reason = progression.GetResearchUnavailableReason(researchId);
                if (!maxed && !string.IsNullOrEmpty(reason) && reason != "Not enough <sprite name=\"insight\" tint=1>.")
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
            RefreshDatacenterPanel();
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
                    if (!string.IsNullOrEmpty(reason) && reason != "Not enough <sprite name=\"insight\" tint=1>.")
                    {
                        SetText(card.costText, "Locked");
                    }
                    else
                    {
                        long cost = progression.GetResearchCost(card.researchId);
                        SetText(card.costText, $"<sprite name=\"insight\" tint=1> {FormatNumber(cost)}");
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
            string effect = progression.GetResearchEffectSummary(selectedResearchId, level);
            // Show current → next so the value is meaningful even at level 0 (e.g. "x1.00 -> x2.00").
            string effectDisplay = effect;
            if (!maxed)
            {
                string next = progression.GetResearchEffectSummary(selectedResearchId, level + 1);
                if (!string.IsNullOrEmpty(next) && next != effect)
                {
                    effectDisplay = $"{effect} >> {next}";
                }
            }
            string reason = progression.GetResearchUnavailableReason(selectedResearchId);

            SetText(researchDetailNameText, definition.DisplayName);
            SetText(researchDetailStatusText, $"Level {level}/{definition.MaxLevel}");

            //string availability = maxed
            //    ? "This research is maxed."
            //    : canBuy
            //        ? $"Ready to buy for <sprite name=\"insight\" tint=1> {FormatNumber(progression.GetResearchCost(selectedResearchId))}."
            //        : string.Empty;

            string detailText = $"{definition.Description}\n\nEffect: {effectDisplay}";
            //if (!string.IsNullOrEmpty(availability))
            //{
            //    detailText += $"\n\n{availability}";
            //}

            SetText(researchDetailInfoText, detailText);

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
                    SetButtonText(researchDetailActionButton, canBuy
                        ? $"Buy\n<sprite name=\"insight\" tint=1> {FormatNumber(progression.GetResearchCost(selectedResearchId))}"
                        : string.IsNullOrEmpty(reason) ? "Locked" : reason);
                    researchDetailActionButton.interactable = canBuy;
                    SetButtonColor(researchDetailActionButton, canBuy ? HexColor("#7C3AED") : HexColor("#334155"));
                }
            }
        }

        private void EnsureHistoryReferences()
        {
            if (historyPanel == null)
            {
                return;
            }

            Transform root = historyPanel.transform;
            historyAverageRunTimeText ??= FindComponentByNormalizedName<TMP_Text>(root, "AverageRunTimeText", "AverageTimeText", "AverageTimeInRunsText", "AverageTimeInRuns", "Average time in runs");
            historyPlaytimeText ??= FindComponentByNormalizedName<TMP_Text>(root, "PlaytimeText", "PrestigePlaytimeText", "CurrentPrestigePlaytimeText", "Playtime");
        }

        private void EnsureAchievementPanelReferences()
        {
            if (achievementsPanel == null)
            {
                return;
            }

            Transform root = achievementsPanel.transform;
            achievementListRoot ??= FindNamedDescendant(root, "GoalsList") as RectTransform;
            secretAchievementListRoot ??= FindNamedDescendant(root, "SecretGoalsList") as RectTransform;
            achievementGoalsListCategory ??= achievementListRoot != null ? achievementListRoot.gameObject : null;
            achievementSecretGoalsListCategory ??= secretAchievementListRoot != null ? secretAchievementListRoot.gameObject : null;
            achievementLifetimeStatsCategory ??= FindFirstNamedDescendant(root, "New Lifetime Stats Category", "Lifetime Stats Category", "Statistics Category")?.gameObject;
            achievementPpoProgressCategory ??= FindFirstNamedDescendant(root, "PPO Progress Category", "PPOProgressCategory")?.gameObject;

            if (secretAchievementCompletedGoalsText == null && secretAchievementListRoot != null)
            {
                secretAchievementCompletedGoalsText = FindComponentByNormalizedName<TMP_Text>(
                    secretAchievementListRoot,
                    "SecretCompletedGoalsText",
                    "SecretAchievementCompletedGoalsText",
                    "CompletedGoalsText");
            }

            if (secretAchievementCompletedGoalsText == null)
            {
                secretAchievementCompletedGoalsText = FindComponentByNormalizedName<TMP_Text>(
                    root,
                    "SecretCompletedGoalsText",
                    "SecretAchievementCompletedGoalsText");
            }

            if (achievementTabs == null || achievementTabs.Length == 0)
            {
                achievementTabs = FindAchievementTabs(root);
            }
        }

        private AchievementPanelTab[] FindAchievementTabs(Transform root)
        {
            if (root == null)
            {
                return Array.Empty<AchievementPanelTab>();
            }

            var discovered = new List<AchievementPanelTab>();
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (button == null || button == achievementBackButton)
                {
                    continue;
                }

                string lookup = NormalizeLookupName(button.gameObject.name);
                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    lookup += NormalizeLookupName(label.text);
                }

                if (!TryResolveAchievementTabKind(lookup, out AchievementPanelTabKind kind))
                {
                    continue;
                }

                Transform buttonRoot = button.transform;
                discovered.Add(new AchievementPanelTab
                {
                    tab = kind,
                    button = button,
                    icon = FindNamedDescendant(buttonRoot, "Icon")?.GetComponent<Image>(),
                    text = FindFirstNamedDescendant(buttonRoot, "Text", "Label", "Name")?.GetComponent<TMP_Text>()
                });
            }

            return discovered
                .GroupBy(tab => tab.tab)
                .Select(group => group.First())
                .ToArray();
        }

        private static bool TryResolveAchievementTabKind(string lookup, out AchievementPanelTabKind kind)
        {
            if (ContainsInvariant(lookup, "secret"))
            {
                kind = AchievementPanelTabKind.Secrets;
                return true;
            }

            if (ContainsInvariant(lookup, "statistic")
                || ContainsInvariant(lookup, "stats"))
            {
                kind = AchievementPanelTabKind.Statistics;
                return true;
            }

            if (ContainsInvariant(lookup, "ppoprogress")
                || (ContainsInvariant(lookup, "ppo") && ContainsInvariant(lookup, "progress")))
            {
                kind = AchievementPanelTabKind.PpoProgress;
                return true;
            }

            if (ContainsInvariant(lookup, "achievement")
                || ContainsInvariant(lookup, "goal"))
            {
                kind = AchievementPanelTabKind.Achievements;
                return true;
            }

            kind = default;
            return false;
        }

        private static bool ContainsInvariant(string value, string fragment)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void WireAchievementPanelTabs()
        {
            EnsureAchievementPanelReferences();
            if (achievementTabs == null)
            {
                return;
            }

            foreach (AchievementPanelTab tab in achievementTabs)
            {
                if (tab == null || tab.button == null)
                {
                    continue;
                }

                AchievementPanelTabKind kind = tab.tab;
                tab.icon ??= FindNamedDescendant(tab.button.transform, "Icon")?.GetComponent<Image>();
                tab.text ??= FindFirstNamedDescendant(tab.button.transform, "Text", "Label", "Name")?.GetComponent<TMP_Text>();
                CacheAchievementTabDefaults(tab);
                tab.button.onClick.RemoveAllListeners();
                tab.button.onClick.AddListener(() => SelectAchievementTab(kind));
            }
        }

        private void SelectAchievementTab(AchievementPanelTabKind tab)
        {
            selectedAchievementTab = tab;
            RefreshAchievementTabVisibility();
            RefreshAchievements();
        }

        private void RefreshAchievementTabVisibility()
        {
            EnsureAchievementPanelReferences();

            SetActive(achievementGoalsListCategory != null ? achievementGoalsListCategory : achievementListRoot != null ? achievementListRoot.gameObject : null,
                selectedAchievementTab == AchievementPanelTabKind.Achievements);
            SetActive(achievementSecretGoalsListCategory != null ? achievementSecretGoalsListCategory : secretAchievementListRoot != null ? secretAchievementListRoot.gameObject : null,
                selectedAchievementTab == AchievementPanelTabKind.Secrets);
            SetActive(achievementLifetimeStatsCategory, selectedAchievementTab == AchievementPanelTabKind.Statistics);
            SetActive(achievementPpoProgressCategory, selectedAchievementTab == AchievementPanelTabKind.PpoProgress);

            RefreshAchievementTabs();
        }

        private void RefreshAchievementTabs()
        {
            if (achievementTabs == null)
            {
                return;
            }

            foreach (AchievementPanelTab tab in achievementTabs)
            {
                if (tab == null)
                {
                    continue;
                }

                CacheAchievementTabDefaults(tab);
                bool selected = tab.tab == selectedAchievementTab;
                string label = tab.tab switch
                {
                    AchievementPanelTabKind.Achievements => "Achievements",
                    AchievementPanelTabKind.Secrets => "Secrets",
                    AchievementPanelTabKind.Statistics => "Statistics",
                    AchievementPanelTabKind.PpoProgress => "PPO Progress",
                    _ => tab.tab.ToString()
                };

                if (tab.button != null)
                {
                    tab.button.transition = Selectable.Transition.None;
                    tab.button.interactable = !selected;
                    if (tab.button.targetGraphic != null)
                    {
                        tab.button.targetGraphic.color = selected ? DarkenButtonColor(tab.buttonColor) : tab.buttonColor;
                    }
                }

                if (tab.icon != null)
                {
                    tab.icon.color = tab.iconColor;
                }

                if (tab.text != null)
                {
                    SetText(tab.text, label);
                    tab.text.color = tab.textColor;
                }
            }
        }

        private void CacheAchievementTabDefaults(AchievementPanelTab tab)
        {
            if (tab == null || tab.defaultsCached)
            {
                return;
            }

            tab.buttonColor = tab.button != null && tab.button.targetGraphic is Image image
                ? image.color
                : Color.white;
            tab.iconColor = tab.icon != null ? tab.icon.color : Color.white;
            tab.textColor = tab.text != null ? tab.text.color : Color.white;
            if (tab.button != null && !buttonNormalColors.ContainsKey(tab.button))
            {
                buttonNormalColors[tab.button] = tab.buttonColor;
            }

            tab.defaultsCached = true;
        }

        private void RefreshHistory()
        {
            if (progression == null)
            {
                return;
            }

            EnsureHistoryReferences();

            // Same guard as the achievements list: the recent-run / solver-stat rows are re-instantiated,
            // so only rebuild them while the History panel is visible (it's refreshed on open and when a
            // run completes while open). Rebuilding on every action was destroying/recreating the rows.
            if (!historyOpen)
            {
                return;
            }

            RunHistoryEntry[] recentHistory = progression.RunHistory;
            RunHistoryEntry[] playthroughHistory = progression.PlaythroughRunHistory;
            if (playthroughHistory.Length == 0)
            {
                SetText(historySummaryText, "No completed runs yet. Let a run end to start collecting solver stats.");
                SetText(historyHighestText, "-");
                SetText(historyHighestMedianText, "-");
                SetText(historyAverageRunTimeText, "0s");
                SetText(historyPlaytimeText, FormatDurationCompact(progression.PlaythroughPlaytimeSeconds));
                SetText(historyInsightText, "Tip: use the editor benchmark window for large balance samples without touching player progression.");
                DrawTrendChart(recentHistory);
                BuildSolverList(Array.Empty<HistorySolverStats>());
                RefreshRecentRunsTable(recentHistory);

                return;
            }

            RunHistoryEntry latest = recentHistory.Length > 0
                ? recentHistory[0]
                : playthroughHistory.OrderByDescending(entry => entry.runIndex).First();
            RunHistoryEntry best = playthroughHistory.OrderByDescending(entry => entry.score).First();
            HistorySolverStats[] solverStats = BuildHistorySolverStats(playthroughHistory);
            HistorySolverStats bestMedian = solverStats.OrderByDescending(stats => stats.MedianScore).First();
            HistorySolverStats bestPeak = solverStats.OrderByDescending(stats => stats.MaxScore).First();
            int trendCount = Math.Min(recentHistory.Length, 80);

            SetText(historyHighestText, $"{FormatNumber(best.score)} <size=75%>({SolverName(best.solverId)})</size>");
            SetText(historyHighestMedianText, $"{FormatNumber(bestMedian.MedianScore)} <size=75%>({bestMedian.SolverName})</size>");
            SetText(historyAverageRunTimeText, FormatAverageRunDuration(playthroughHistory, progression.PlaythroughPlaytimeSeconds));
            SetText(historyPlaytimeText, FormatDurationCompact(progression.PlaythroughPlaytimeSeconds));
            SetText(
                historySummaryText,
                $"Playthrough runs: {playthroughHistory.Length}\n" +
                $"Recent stored: {recentHistory.Length}/250\n" +
                $"Latest: {FormatNumber(latest.score)} ({SolverName(latest.solverId)})\n" +
                $"Highest: {FormatNumber(best.score)} ({SolverName(best.solverId)})\n" +
                $"Highest median: {bestMedian.SolverName} ({FormatNumber(bestMedian.MedianScore)})");

            //SetText(
            //    historyInsightText,
            //    $"Best median: {bestMedian.SolverName} ({FormatNumber(bestMedian.MedianScore)}) | Best peak: {bestPeak.SolverName} ({FormatNumber(bestPeak.MaxScore)}) | Trend = last {trendCount} runs (chronological), more honest than a single aggregate median");

            DrawTrendChart(recentHistory);
            BuildSolverList(solverStats);
            RefreshRecentRunsTable(recentHistory);
        }

        // Recent score trend: the last runs in chronological order. This reflects how the player's
        // setup is actually performing right now, instead of an all-time aggregate that jumps every
        // time an upgrade is bought.
        private void DrawTrendChart(RunHistoryEntry[] history)
        {
            int take = Math.Min(history.Length, 80);
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
            foreach (RunHistoryEntry entry in history.Take(80))
            {
                StackMergeRecentRunRow row = Instantiate(recentRunRowPrefab, root, false);
                PositionRow((RectTransform)row.transform, y, rowHeight);
                SetText(row.runText, $"{entry.runIndex}");
                SetText(row.solverText, SolverName(entry.solverId));
                SetText(row.scoreText, FormatNumber(entry.score));
                SetText(row.movesText, entry.moves.ToString());
                SetText(row.mergesText, entry.merges.ToString());
                SetText(row.highText, FormatBlockValue(entry.highestMergedBlock));
                y += rowHeight + 3f;
            }

            SetManualContentHeight(root, y);
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
                SetText(row.runsText, FormatNumber(stat.Runs));
                SetText(row.medianText, FormatNumber(stat.MedianScore));
                SetText(row.bestText, FormatNumber(stat.MaxScore));
                SetText(row.highText, FormatBlockValue(stat.BestHighestMerged));

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

            SetManualContentHeight(root, y);
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

        private static void SetManualContentHeight(RectTransform root, float height)
        {
            if (root == null)
            {
                return;
            }

            float contentHeight = Mathf.Max(0f, height);
            root.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, contentHeight);
            LayoutElement layoutElement = EnsureComponent<LayoutElement>(root.gameObject);
            layoutElement.minHeight = contentHeight;
            layoutElement.preferredHeight = -1f;

            LayoutRebuilder.MarkLayoutForRebuild(root);
            RebuildLayout(root);
            if (root.parent is RectTransform parent)
            {
                LayoutRebuilder.MarkLayoutForRebuild(parent);
                RebuildLayout(parent);
            }
        }

        private void RefreshAchievements()
        {
            if (progression == null)
            {
                return;
            }

            // Only touch the (re-instantiated) goal rows while the panel is actually visible. It's
            // rebuilt from scratch when opened (OpenAchievementsPanel calls this after setting the
            // flag), so refreshing it on every gameplay action — which destroyed and re-created every
            // row each time — was the source of the layout flicker and the "prefab keeps reloading"
            // behaviour. Notifications for newly completed goals run separately and are unaffected.
            if (!achievementsOpen)
            {
                return;
            }

            EnsureAchievementPanelReferences();
            RefreshAchievementTabVisibility();

            int completed = StackMergeProgression.Achievements.Count(progression.IsAchievementComplete);
            int goalCount = StackMergeProgression.Achievements.Length;
            int completedSecrets = progression.CompletedSecretAchievementCount;
            int secretGoalCount = StackMergeProgression.SecretAchievements.Length;
            SetText(achievementRunsText, FormatNumber(progression.LifetimeRunsCompleted));
            SetText(achievementBestRunText, FormatNumber(progression.LifetimeBestRunScore));
            SetText(achievementMovesText, FormatNumber(progression.LifetimeMoves));
            SetText(achievementMergesText, FormatNumber(progression.LifetimeMerges));
            SetText(achievementHighestText, progression.LifetimeHighestBlockEver.ToString());
            SetText(achievementEarnedText, FormatNumber(progression.LifetimeChipsEarned));
            SetText(achievementCompletedGoalsText, $"{completed}/{goalCount} {CompletedSuffix()}");
            SetText(secretAchievementCompletedGoalsText, $"{completedSecrets}/{secretGoalCount} {CompletedSuffix()}");
            SetText(achievementResetsText, progression.PrestigeCount.ToString());
            SetText(achievementPlaytimeText, FormatDurationCompact(progression.TotalPlaytimeSeconds));
            SetText(
                achievementStatsText,
                $"Completed goals: {completed}/{goalCount}\n" +
                $"Runs: {FormatNumber(progression.LifetimeRunsCompleted)} ({FormatNumber(progression.LifetimeManualRunsCompleted)} manual)\n" +
                $"Resets: {FormatNumber(progression.PrestigeCount)}\n" +
                $"Playtime: {FormatDurationCompact(progression.TotalPlaytimeSeconds)}\n" +
                $"Moves: {FormatNumber(progression.LifetimeMoves)}\n" +
                $"Merges: {FormatNumber(progression.LifetimeMerges)}\n" +
                $"Highest: {progression.LifetimeHighestBlockEver}\n" +
                $"Earned: {FormatNumber(progression.LifetimeChipsEarned)}\n" +
                $"Spent: {FormatNumber(progression.LifetimeChipsSpent)}\n" +
                $"Best run: {FormatNumber(progression.LifetimeBestRunScore)}");

            RefreshPpoProgressStats();

            if (selectedAchievementTab == AchievementPanelTabKind.Achievements)
            {
                BuildGoalRows();
            }
            else if (selectedAchievementTab == AchievementPanelTabKind.Secrets)
            {
                BuildSecretGoalRows();
            }
        }

        private void RefreshPpoProgressStats()
        {
            if (progression == null)
            {
                return;
            }

            long frames = progression.MachineLearningDisplayFrames;
            int frameCycles = Math.Max(1, progression.PrestigeCount + 1);
            long averageFrames = frames / frameCycles;
            SetText(ppoProgressTotalFramesText, FormatNumber(frames));
            SetText(ppoProgressAverageFramesPerPrestigeText, FormatNumber(averageFrames));
            SetText(ppoProgressTotalRunsText, FormatNumber(progression.MachineLearningRuns));
            SetText(ppoProgressBestRunText, FormatNumber(progression.MachineLearningBestScore));
            SetText(ppoProgressTotalMovesText, FormatNumber(progression.MachineLearningTotalMoves));
            SetText(ppoProgressTotalMergesText, FormatNumber(progression.MachineLearningTotalMerges));
            SetText(ppoProgressKnowledgeText, $"{progression.MachineLearningSkill * 100f:0.0}%");
            SetText(ppoProgressPlaytimeText, FormatDurationCompact(progression.MachineLearningTotalPlaytimeSeconds));
        }

        private static string CompletedSuffix()
        {
            return StackMergeLocalization.Translate("Completed").ToLowerInvariant();
        }

        // Instantiates one goalRowPrefab per achievement under achievementListRoot.
        private void BuildGoalRows()
        {
            RectTransform root = achievementListRoot;
            if (root == null)
            {
                return;
            }

            if (goalRowPrefab == null)
            {
                Debug.LogWarning("StackMerge: Goal row prefab not assigned — assign it on the Bootstrap in the Inspector.");
                return;
            }

            bool needsRebuild = achievementRowsLanguage != currentLanguage
                || achievementRowsById.Count != StackMergeProgression.Achievements.Length
                || achievementRowsById.Values.Any(row => row == null);

            if (needsRebuild)
            {
                ClearInstantiatedRows<StackMergeGoalRow>(root);
                achievementRowsById.Clear();
                achievementRowsLanguage = currentLanguage;

                float rowHeight = RowHeightOf((RectTransform)goalRowPrefab.transform, 50f);
                float y = 0f;
                foreach (AchievementDefinition achievement in StackMergeProgression.Achievements)
                {
                    StackMergeGoalRow row = Instantiate(goalRowPrefab, root, false);
                    PositionRow((RectTransform)row.transform, y, rowHeight);
                    SetText(row.goalText, achievement.Description);
                    achievementRowsById[achievement.Id] = row;

                    TMP_Text rewardLabel = row.rewardText != null
                        ? row.rewardText
                        : FindNamedDescendant(row.transform, "RewardText")?.GetComponent<TMP_Text>();
                    if (rewardLabel != null)
                    {
                        string reward = GetAchievementRewardText(achievement.Id);
                        bool hasReward = !string.IsNullOrEmpty(reward);
                        SetActive(rewardLabel.gameObject, hasReward);
                        if (hasReward)
                        {
                            SetText(rewardLabel, reward);
                        }
                    }

                    y += rowHeight + 4f;
                }

                SetManualContentHeight(root, y);
            }

            foreach (AchievementDefinition achievement in StackMergeProgression.Achievements)
            {
                if (!achievementRowsById.TryGetValue(achievement.Id, out StackMergeGoalRow row) || row == null)
                {
                    continue;
                }

                long progress = progression.GetAchievementProgress(achievement);
                long cappedProgress = Math.Min(progress, achievement.Target);
                bool complete = progression.IsAchievementComplete(achievement);
                SetText(row.progressText, complete
                    ? StackMergeLocalization.Translate("Completed")
                    : $"{FormatNumber(cappedProgress)} / {FormatNumber(achievement.Target)}");
            }
        }

        private void BuildSecretGoalRows()
        {
            RectTransform root = secretAchievementListRoot;
            if (root == null)
            {
                return;
            }

            StackMergeGoalRow prefab = secretGoalRowPrefab != null ? secretGoalRowPrefab : goalRowPrefab;
            if (prefab == null)
            {
                Debug.LogWarning("StackMerge: Secret goal row prefab not assigned - assign SecretGoalsTableContent on the Bootstrap in the Inspector.");
                return;
            }

            bool needsRebuild = secretAchievementRowsLanguage != currentLanguage
                || secretAchievementRowsById.Count != StackMergeProgression.SecretAchievements.Length
                || secretAchievementRowsById.Values.Any(row => row == null);

            if (needsRebuild)
            {
                ClearInstantiatedRows<StackMergeGoalRow>(root);
                secretAchievementRowsById.Clear();
                secretAchievementRowsLanguage = currentLanguage;

                bool layoutDriven = root.GetComponent<HorizontalOrVerticalLayoutGroup>() != null;
                float rowHeight = RowHeightOf((RectTransform)prefab.transform, 60f);
                float y = 0f;
                foreach (SecretAchievementDefinition achievement in StackMergeProgression.SecretAchievements)
                {
                    StackMergeGoalRow row = Instantiate(prefab, root, false);
                    if (!layoutDriven)
                    {
                        PositionRow((RectTransform)row.transform, y, rowHeight);
                    }

                    secretAchievementRowsById[achievement.Id] = row;
                    y += rowHeight + 4f;
                }

                if (layoutDriven)
                {
                    LayoutRebuilder.MarkLayoutForRebuild(root);
                    RebuildLayout(root);
                }
                else
                {
                    SetManualContentHeight(root, y);
                }
            }

            foreach (SecretAchievementDefinition achievement in StackMergeProgression.SecretAchievements)
            {
                if (!secretAchievementRowsById.TryGetValue(achievement.Id, out StackMergeGoalRow row) || row == null)
                {
                    continue;
                }

                bool complete = progression.IsSecretAchievementComplete(achievement);
                TMP_Text descText = row.descText != null
                    ? row.descText
                    : FindFirstNamedDescendant(row.transform, "DescText", "DescriptionText")?.GetComponent<TMP_Text>();
                TMP_Text rewardText = row.rewardText != null
                    ? row.rewardText
                    : FindNamedDescendant(row.transform, "RewardText")?.GetComponent<TMP_Text>();

                SetText(row.goalText, achievement.Hint);
                SetText(row.progressText, complete ? "Completed" : "Locked");
                if (descText != null)
                {
                    SetActive(descText.gameObject, complete);
                    if (complete)
                    {
                        SetText(descText, achievement.Description);
                    }
                }

                if (rewardText != null && rewardText != descText)
                {
                    SetActive(rewardText.gameObject, false);
                }
            }

            LayoutRebuilder.MarkLayoutForRebuild(root);
            RebuildLayout(root);
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
                TMP_Text empty = CreateRuntimeText("Empty Chart", root, emptyText, 26, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#64748B"));
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
                CreateLineSegment(root, PointAt(i - 1), PointAt(i), lineColor, 5f);
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
            TMP_Text label = CreateRuntimeText("Chart Label", root, text, 22, FontStyles.Bold, alignment, HexColor("#94A3B8"));
            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(width, 30f);
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
            RunHistoryEntry[] solverRuns = progression.PlaythroughRunHistory.Where(entry => entry.solverId == (int)solverId).ToArray();
            int lifetime = progression.GetSolverLifetimeRuns(solverId);

            SetText(solverInfoTitle, $"{definition.DisplayName} detail");

            var stats = new StringBuilder();
            stats.AppendLine(definition.Description);
            stats.AppendLine();
            stats.AppendLine($"<b>Lifetime runs</b>\n" +
                $"{FormatNumber(lifetime)} lifetime\n{FormatNumber(solverRuns.Length)} this playthrough");
            if (solverRuns.Length > 0)
            {
                long[] scores = solverRuns.Select(entry => entry.score).OrderBy(value => value).ToArray();
                stats.AppendLine($"Best {FormatNumber(scores[^1])}\n" +
                    $"Median {FormatNumber(Median(scores))}\n" +
                    $"Average {FormatNumber((long)scores.Average())}");
                stats.AppendLine($"Range {FormatNumber(scores[0])} – {FormatNumber(scores[^1])}");
                stats.AppendLine($"Best high tile {FormatBlockValue(solverRuns.Max(entry => entry.highestMergedBlock))}\n" +
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
                    : $"Playing mode unlocks at {FormatNumber(progression.EffectivePlayingModeFrameRequirement)} cycle frames ({FormatNumber(progression.MachineLearningCycleFrames)} so far)");
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
            solverInfoOverlay.transform.SetAsLastSibling();
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
            if (nextBlocksRoot == null || gameState == null)
            {
                return;
            }

            int blockCount = gameState.NextBlocks.Count;
            Vector2 blockSize = CalculateNextBlockSize(blockCount);
            float blockWidth = blockSize.x;
            float blockHeight = blockSize.y;
            int fontSize = Mathf.RoundToInt(blockHeight * 0.44f);

            for (int i = 0; i < blockCount; i++)
            {
                RectTransform block = i < nextBlocksRoot.childCount
                    ? (RectTransform)nextBlocksRoot.GetChild(i)
                    : CreateBlockInstance(nextBlocksRoot);
                ConfigureBlock(block, gameState.NextBlocks[i], blockWidth, blockHeight, fontSize);
                SetCurrentNextOutline(block, i == 0);
                ConfigureNextBlockInteraction(block, i);
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

        private Vector2 CalculateNextBlockSize(int blockCount)
        {
            if (!TryGetStackBlockSize(out float stackBlockWidth, out float stackBlockHeight))
            {
                stackBlockWidth = 144f;
                stackBlockHeight = StackBlockMinHeight;
            }

            if (blockCount <= 1)
            {
                return new Vector2(stackBlockWidth, stackBlockHeight);
            }

            float spacing = GetNextBlocksSpacing(out float horizontalPadding);
            float availableWidth = Mathf.Max(0f, GetNextBlocksAvailableWidth() - horizontalPadding);
            if (availableWidth <= 0f)
            {
                return new Vector2(stackBlockWidth, stackBlockHeight);
            }

            float preferredWidth = stackBlockWidth * blockCount + spacing * (blockCount - 1);
            if (preferredWidth <= availableWidth)
            {
                return new Vector2(stackBlockWidth, stackBlockHeight);
            }

            float fittedWidth = (availableWidth - spacing * (blockCount - 1)) / blockCount;
            fittedWidth = Mathf.Clamp(fittedWidth, 1f, stackBlockWidth);
            return new Vector2(fittedWidth, stackBlockHeight);
        }

        private float GetNextBlocksAvailableWidth()
        {
            if (nextBlocksRoot == null)
            {
                return 0f;
            }

            float width = nextBlocksRoot.rect.width;
            if (width > 1f)
            {
                return width;
            }

            if (nextBlocksRoot.parent is RectTransform parent)
            {
                float parentWidth = parent.rect.width;
                if (parentWidth > 1f)
                {
                    float left = Mathf.Max(0f, nextBlocksRoot.offsetMin.x);
                    float right = Mathf.Max(0f, -nextBlocksRoot.offsetMax.x);
                    return Mathf.Max(0f, parentWidth - left - right);
                }
            }

            return 0f;
        }

        private float GetNextBlocksSpacing(out float horizontalPadding)
        {
            HorizontalLayoutGroup layout = nextBlocksRoot != null ? nextBlocksRoot.GetComponent<HorizontalLayoutGroup>() : null;
            if (layout == null)
            {
                horizontalPadding = 0f;
                return 16f;
            }

            horizontalPadding = Mathf.Max(0f, layout.padding.left + layout.padding.right);
            return Mathf.Max(0f, layout.spacing);
        }

        private bool TryGetStackBlockSize(out float blockWidth, out float blockHeight)
        {
            blockWidth = 144f;
            blockHeight = StackBlockMinHeight;

            if (gameState == null || stackBlockLayers == null)
            {
                return false;
            }

            int capacity = Mathf.Max(1, gameState.StackCapacity);
            foreach (RectTransform layer in stackBlockLayers)
            {
                if (layer == null)
                {
                    continue;
                }

                CalculateStackBlockSize(layer, capacity, out blockWidth, out blockHeight);
                return true;
            }

            return false;
        }

        private static void CalculateStackBlockSize(RectTransform layer, int capacity, out float blockWidth, out float blockHeight)
        {
            float layerWidth = layer.rect.width > 1f ? layer.rect.width : 128f;
            const float padding = StackBlockPadding;
            int safeCapacity = Mathf.Max(1, capacity);
            float fallbackLayerHeight = padding * 2f + StackBlockMinHeight * safeCapacity + StackBlockSpacing * Mathf.Max(0, safeCapacity - 1);
            float layerHeight = layer.rect.height > 1f ? layer.rect.height : fallbackLayerHeight;

            blockHeight = Mathf.Clamp(
                (layerHeight - padding * 2f - StackBlockSpacing * (safeCapacity - 1)) / safeCapacity,
                1f,
                StackBlockMaxHeight);
            blockWidth = Mathf.Max(110f, layerWidth - padding * 2f);
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
                bool sectionLayoutApplied = ResizeBoardToCapacity();
                Canvas.ForceUpdateCanvases();
                boardLayoutDirty = false;
                if (!sectionLayoutApplied)
                {
                    PositionRunInfoPanel();
                }
            }

            for (int stackIndex = 0; stackIndex < stackBlockLayers.Length; stackIndex++)
            {
                RectTransform layer = stackBlockLayers[stackIndex];
                if (layer == null)
                {
                    continue;
                }

                float padding = StackBlockPadding;
                float spacing = StackBlockSpacing;
                CalculateStackBlockSize(layer, capacity, out float blockWidth, out float blockHeight);
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
                    SetCurrentNextOutline(block, false);
                    ConfigureStackBlockInteraction(block, stackIndex, i);
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
                    ApplyButtonVisualState(stackButtons[stackIndex]);
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

        private bool ResizeBoardToCapacity()
        {
            if (gameState == null)
            {
                return false;
            }

            if (TryLayoutGameplaySections())
            {
                return true;
            }

            RectTransform board = boardRoot;
            if (board == null && stackBlockLayers.Length > 0 && stackBlockLayers[0] != null)
            {
                board = stackBlockLayers[0].parent.parent as RectTransform;
            }

            if (board == null)
            {
                return false;
            }

            const float top = 470f;
            float height = CalculateBoardHeight(gameState.StackCapacity);
            board.anchorMin = new Vector2(0f, 1f);
            board.anchorMax = new Vector2(1f, 1f);
            board.pivot = new Vector2(0.5f, 1f);
            board.offsetMin = new Vector2(0f, -top - height);
            board.offsetMax = new Vector2(0f, -top);
            return false;
        }

        private bool TryLayoutGameplaySections()
        {
            if (nextBlocksRoot == null || boardRoot == null || runInfoPanel == null || footerRoot == null || gameState == null)
            {
                return false;
            }

            EnsureGameplayModifierReferences();
            RectTransform parent = boardRoot.parent as RectTransform;
            if (parent == null || runInfoPanel.parent != parent || footerRoot.parent != parent)
            {
                return false;
            }

            RectTransform nextSection = GetNextBlocksPanel(parent);
            if (nextSection == null || nextSection.parent != parent)
            {
                return false;
            }

            float parentHeight = parent.rect.height;
            if (parentHeight <= 1f)
            {
                return false;
            }

            bool trainingLayout = progression != null
                && progression.IsMachineLearningTrainingActive
                && selectedTabIndex == 0
                && !historyOpen
                && !achievementsOpen;
            float gap = GetGameplaySectionGap(parent);
            float footerHeight = GetSectionHeight(footerRoot, 80f);
            float runInfoMaxHeight = Mathf.Clamp(parentHeight * 0.26f, 128f, 220f);
            float runInfoHeight = Mathf.Clamp(GetSectionHeight(runInfoPanel, 96f), 56f, runInfoMaxHeight);
            RectTransform modifierSection = gameplayModifiersSection != null ? gameplayModifiersSection.transform as RectTransform : null;
            bool modifierSectionVisible = modifierSection != null
                && modifierSection.parent == parent
                && ShouldShowGameplayModifiersSection();
            float modifierHeight = modifierSectionVisible ? GameplayModifiersSectionHeight : 0f;
            if (modifierSectionVisible)
            {
                SetActive(gameplayModifiersSection, true);
                modifierSection.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, GameplayModifiersSectionHeight);
            }

            if (trainingLayout)
            {
                EnsureTrainingOverlay();
                float trainingHeight = GetTrainingOverlayLayoutHeight(parentHeight, footerHeight, runInfoHeight, modifierHeight, modifierSectionVisible, gap);

                LayoutGroup trainingParentLayout = parent.GetComponent<LayoutGroup>();
                if (trainingParentLayout != null)
                {
                    trainingParentLayout.enabled = false;
                }

                SetGameplaySectionTop(nextSection, 0f, trainingHeight);
                SetGameplaySectionTop(boardRoot, trainingHeight, 0f);
                SetGameplaySectionBottom(footerRoot, 0f, footerHeight);

                float trainingContentBottomFromTop = trainingHeight;
                if (modifierSectionVisible)
                {
                    float modifierTop = trainingContentBottomFromTop + gap;
                    SetGameplaySectionTop(modifierSection, modifierTop, modifierHeight);
                    trainingContentBottomFromTop = modifierTop + modifierHeight;
                }

                PositionRunInfoBetween(parent, trainingContentBottomFromTop, footerHeight, runInfoHeight, gap);
                return true;
            }

            float gapCount = modifierSectionVisible ? 4f : 3f;

            int capacity = Mathf.Max(1, gameState.StackCapacity);
            float boardChromeHeight = CalculateBoardHeightForBlockHeight(capacity, 0f);
            float availableForBlocks = parentHeight
                - footerHeight
                - runInfoHeight
                - modifierHeight
                - gap * gapCount
                - NextPanelChromeHeight
                - boardChromeHeight;

            float rawBlockHeight = availableForBlocks / (capacity + 1f);
            float blockHeight = rawBlockHeight >= StackBlockMinHeight
                ? Mathf.Clamp(rawBlockHeight, StackBlockMinHeight, StackBlockMaxHeight)
                : Mathf.Max(EmergencyFitBlockMinHeight, rawBlockHeight);

            float nextHeight = NextPanelChromeHeight + blockHeight;
            float boardHeight = CalculateBoardHeightForBlockHeight(capacity, blockHeight);

            LayoutGroup parentLayout = parent.GetComponent<LayoutGroup>();
            if (parentLayout != null)
            {
                parentLayout.enabled = false;
            }

            SetGameplaySectionTop(nextSection, 0f, nextHeight);
            SetGameplaySectionTop(boardRoot, nextHeight + gap, boardHeight);
            SetGameplaySectionBottom(footerRoot, 0f, footerHeight);

            float boardBottomFromTop = nextHeight + gap + boardHeight;
            float contentBottomFromTop = boardBottomFromTop;
            if (modifierSectionVisible)
            {
                float modifierTop = boardBottomFromTop + gap;
                SetGameplaySectionTop(modifierSection, modifierTop, modifierHeight);
                contentBottomFromTop = modifierTop + modifierHeight;
            }

            PositionRunInfoBetween(parent, contentBottomFromTop, footerHeight, runInfoHeight, gap);
            return true;
        }

        private float GetTrainingOverlayLayoutHeight(
            float parentHeight,
            float footerHeight,
            float runInfoHeight,
            float modifierHeight,
            bool modifierSectionVisible,
            float gap)
        {
            float desiredHeight = GetTrainingOverlayDesiredHeight();
            float reservedGaps = gap * (modifierSectionVisible ? 3f : 2f);
            float maxHeight = parentHeight - footerHeight - runInfoHeight - modifierHeight - reservedGaps;
            float minHeight = Mathf.Max(80f, ppoTrainingOverlayMinHeight);
            return Mathf.Clamp(desiredHeight, minHeight, Mathf.Max(minHeight, maxHeight));
        }

        private float GetTrainingOverlayDesiredHeight()
        {
            float textHeight = 0f;
            if (trainingOverlayText != null)
            {
                trainingOverlayText.ForceMeshUpdate();
                textHeight = Mathf.Max(
                    trainingOverlayText.preferredHeight,
                    LayoutUtility.GetPreferredHeight(trainingOverlayText.rectTransform));
            }

            if (textHeight <= 1f)
            {
                float fontSize = trainingOverlayText != null ? Mathf.Max(1f, trainingOverlayText.fontSize) : 22f;
                float lineHeight = Mathf.Max(20f, fontSize * 1.2f);
                int matrixLines = (gameState != null ? Mathf.Max(1, gameState.StackCapacity) : 10) + 2;
                textHeight = matrixLines * lineHeight;
            }

            return Mathf.Max(ppoTrainingOverlayMinHeight, textHeight + GetTrainingOverlayVerticalPadding());
        }

        private float GetTrainingOverlayVerticalPadding()
        {
            if (trainingOverlay != null && trainingOverlayText != null)
            {
                RectTransform textRect = trainingOverlayText.rectTransform;
                if (textRect != null && textRect.parent == trainingOverlay)
                {
                    float padding = Mathf.Abs(textRect.offsetMin.y) + Mathf.Abs(textRect.offsetMax.y);
                    if (padding > 0.5f)
                    {
                        return padding;
                    }
                }
            }

            return ppoTrainingOverlayVerticalPadding;
        }

        private void PositionRunInfoBetween(RectTransform parent, float contentBottomFromTop, float footerHeight, float runInfoHeight, float gap)
        {
            if (runInfoPanel == null || parent == null)
            {
                return;
            }

            float footerTopFromTop = parent.rect.height - footerHeight;
            float freeGap = Mathf.Max(0f, footerTopFromTop - contentBottomFromTop);
            float fittedRunInfoHeight = Mathf.Min(runInfoHeight, Mathf.Max(0f, freeGap - gap * 2f));
            if (fittedRunInfoHeight < 40f && freeGap > 40f)
            {
                fittedRunInfoHeight = Mathf.Min(runInfoHeight, freeGap);
            }

            float runInfoTop = contentBottomFromTop + Mathf.Max(0f, (freeGap - fittedRunInfoHeight) * 0.5f);
            SetGameplaySectionTop(runInfoPanel, runInfoTop, fittedRunInfoHeight);
        }

        private RectTransform GetNextBlocksPanel(RectTransform layoutParent = null)
        {
            if (nextBlocksPanelRoot != null)
            {
                return nextBlocksPanelRoot;
            }

            if (layoutParent != null)
            {
                RectTransform directChild = GetDirectChildUnder(layoutParent, nextBlocksRoot);
                if (directChild != null)
                {
                    return directChild;
                }
            }

            return nextBlocksRoot != null ? nextBlocksRoot.parent as RectTransform : null;
        }

        private static RectTransform GetDirectChildUnder(RectTransform ancestor, RectTransform descendant)
        {
            if (ancestor == null || descendant == null)
            {
                return null;
            }

            Transform current = descendant;
            while (current != null && current.parent != ancestor)
            {
                current = current.parent;
            }

            return current != null && current.parent == ancestor ? current as RectTransform : null;
        }

        private static float GetGameplaySectionGap(RectTransform parent)
        {
            float preferred = 20f;
            if (parent != null && parent.TryGetComponent(out VerticalLayoutGroup layout))
            {
                preferred = layout.spacing;
            }

            float responsiveMax = parent != null && parent.rect.height < 780f ? 14f : 28f;
            return Mathf.Clamp(preferred, 10f, responsiveMax);
        }

        private static float GetSectionHeight(RectTransform rectTransform, float fallback)
        {
            if (rectTransform == null)
            {
                return fallback;
            }

            float height = Mathf.Max(rectTransform.rect.height, Mathf.Abs(rectTransform.sizeDelta.y));
            float preferred = LayoutUtility.GetPreferredHeight(rectTransform);
            if (preferred > 1f)
            {
                height = Mathf.Max(height, preferred);
            }

            return height > 1f ? height : fallback;
        }

        private static void SetGameplaySectionTop(RectTransform rectTransform, float top, float height)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.offsetMin = new Vector2(0f, -top - height);
            rectTransform.offsetMax = new Vector2(0f, -top);
        }

        private static void SetGameplaySectionBottom(RectTransform rectTransform, float bottom, float height)
        {
            if (rectTransform == null)
            {
                return;
            }

            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.offsetMin = new Vector2(0f, bottom);
            rectTransform.offsetMax = new Vector2(0f, bottom + height);
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
            EnsureGameplayModifierReferences();
            if (gameplayModifiersSection != null
                && gameplayModifiersSection.activeSelf
                && gameplayModifiersSection.transform is RectTransform modifierSection
                && modifierSection.parent == parent)
            {
                boardBottomFromTop = Mathf.Max(boardBottomFromTop, -modifierSection.offsetMin.y);
            }

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

        private void EnsureGameOverReferences()
        {
            if (gameOverOverlay == null)
            {
                return;
            }

            Transform root = gameOverOverlay.transform;
            gameOverRunTimeText ??= FindComponentByNormalizedName<TMP_Text>(
                root,
                "GameOverRunTimeText",
                "RunTimeText",
                "Run Time Text",
                "RunTime",
                "Run Time");
        }

        private void RefreshGameOver()
        {
            if (gameOverOverlay == null)
            {
                return;
            }

            EnsureGameOverReferences();
            bool trainingActive = progression != null && progression.IsMachineLearningTrainingActive;
            bool showOverlay = gameState != null
                && gameState.IsGameOver
                && gameOverOverlayTimer <= 0f
                && selectedTabIndex == 0
                && !historyOpen
                && !achievementsOpen
                && !trainingActive;
            gameOverOverlay.SetActive(showOverlay);
            if (gameState == null || !gameState.IsGameOver)
            {
                UpdateGameOverAutoRestartSlider(false);
                return;
            }

            if (trainingActive)
            {
                UpdateGameOverAutoRestartSlider(false);
                return;
            }

            SetText(gameOverScoreText, $"Score: {FormatNumber(gameState.Score)}");
            SetText(gameOverRunTimeText, $"Run time: {FormatDurationCompact(currentRunElapsed)}");
            SetText(gameOverBestText,
                $"Moves: {FormatNumber(gameState.BlocksDropped)}\n" +
                $"Merges: {FormatNumber(gameState.TotalMerges)}\n" +
                $"Highest block: {FormatBlockValue(gameState.HighestMergedBlock)}\n" +
                $"+{FormatNumber(currentRunChipsEarned)} <sprite name=\"chips\" tint=1> earned");
            SetText(runStatusText, CanAutoRestartCurrentRun() ? "Auto restart armed" : "Run ended");
            RebuildTextLayout(runStatusText);
            UpdateGameOverAutoRestartSlider(showOverlay);
        }

        private void UpdateGameOverAutoRestartSlider(bool? overlayVisibleOverride = null)
        {
            if (gameOverAutoRestartSlider == null)
            {
                return;
            }

            bool overlayVisible = overlayVisibleOverride ?? (gameOverOverlay != null && gameOverOverlay.activeSelf);
            bool show = overlayVisible
                && gameState != null
                && gameState.IsGameOver
                && progression != null
                && progression.AutoRestartUnlocked
                && progression.AutoRestartEnabled
                && (progression.AutoRestartIsTokenFree || progression.Tokens > 0);

            SetActive(gameOverAutoRestartSlider.gameObject, show);
            if (!show)
            {
                gameOverAutoRestartSlider.SetValueWithoutNotify(0f);
                return;
            }

            gameOverAutoRestartSlider.minValue = 0f;
            gameOverAutoRestartSlider.maxValue = 1f;
            float remainingFraction = 1f - Mathf.Clamp01(autoRestartTimer / Mathf.Max(0.01f, AutoRestartDelay));
            gameOverAutoRestartSlider.SetValueWithoutNotify(remainingFraction);
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
                if (useGeneratedBlockSprites && image.sprite == null)
                {
                    image.sprite = GetRoundedSprite(color, HexColor("#000000", 0.18f), 18);
                    image.type = Image.Type.Sliced;
                }
            }

            TMP_Text text = block.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = value == StackMergeGameState.JokerBlockValue ? "Joker" : FormatBlockValue(value);
                text.fontSize = fontSize;
                text.color = GetReadableTextColor(color);
                text.enableAutoSizing = true;
                text.fontSizeMin = 12;
                text.fontSizeMax = Mathf.Max(14, fontSize);
            }
        }

        private void ConfigureStackBlockInteraction(RectTransform block, int stackIndex, int blockIndex)
        {
            bool canUse = IsGameplayModifierArmed(ModifierId.MinersPickaxe)
                && gameState != null
                && gameState.CanUsePickaxe(stackIndex, blockIndex);
            ConfigureBlockButton(block, canUse, () => UsePickaxeOnBlock(stackIndex, blockIndex));
        }

        private void ConfigureNextBlockInteraction(RectTransform block, int nextIndex)
        {
            bool canUse = nextIndex == 0
                && IsGameplayModifierArmed(ModifierId.QueueScrubber)
                && gameState != null
                && gameState.CanSkipNextBlock();
            ConfigureBlockButton(block, canUse, UseQueueScrubber);
        }

        private static void ConfigureBlockButton(RectTransform block, bool enabled, UnityEngine.Events.UnityAction action)
        {
            if (block == null)
            {
                return;
            }

            Button button = block.GetComponent<Button>();
            if (!enabled)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                    button.interactable = true;
                    button.enabled = false;
                }

                return;
            }

            if (button == null)
            {
                button = block.gameObject.AddComponent<Button>();
            }

            button.enabled = true;
            button.interactable = true;
            button.transition = Selectable.Transition.None;
            button.targetGraphic = block.GetComponent<Image>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);

            Image image = block.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
            }
        }

        private void SetCurrentNextOutline(RectTransform block, bool current)
        {
            if (block == null)
            {
                return;
            }

            Transform outlineTransform = FindNamedDescendant(block, "Outline");
            if (outlineTransform == null)
            {
                return;
            }

            Image outlineImage = outlineTransform.GetComponent<Image>();
            if (outlineImage != null)
            {
                outlineImage.raycastTarget = false;
            }

            SetActive(outlineTransform.gameObject, current);
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
            if (target == null)
            {
                return;
            }

            string translated = StackMergeLocalization.Translate(value);
            if (target.text == translated)
            {
                return;
            }

            target.text = translated;
            target.SetVerticesDirty();
            target.SetLayoutDirty();
            LayoutRebuilder.MarkLayoutForRebuild(target.rectTransform);
            if (target.rectTransform.parent is RectTransform parent)
            {
                LayoutRebuilder.MarkLayoutForRebuild(parent);
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

        private void RefreshButtonVisualStates()
        {
            if (canvas == null)
            {
                return;
            }

            foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
            {
                ApplyButtonVisualState(button);
            }
        }

        private void ApplyButtonVisualState(Button button)
        {
            if (button == null)
            {
                return;
            }

            if (IsRuntimeBlockButton(button))
            {
                return;
            }

            if (IsHowToPlayPanelButton(button))
            {
                return;
            }

            button.transition = Selectable.Transition.None;
            Image image = GetButtonVisualImage(button);
            if (image == null)
            {
                return;
            }

            if (!buttonNormalColors.TryGetValue(button, out Color normalColor))
            {
                normalColor = image.color;
                buttonNormalColors[button] = normalColor;
            }

            bool subdued = !button.interactable || HasSubduedButtonLabel(button);
            image.color = !subdued
                ? normalColor
                : DarkenButtonColor(normalColor);
        }

        private bool IsRuntimeBlockButton(Button button)
        {
            if (button == null)
            {
                return false;
            }

            if (button.transform.parent == nextBlocksRoot)
            {
                return true;
            }

            if (stackBlockLayers == null)
            {
                return false;
            }

            Transform parent = button.transform.parent;
            foreach (RectTransform layer in stackBlockLayers)
            {
                if (layer != null && parent == layer)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsHowToPlayPanelButton(Button button)
        {
            if (button == null)
            {
                return false;
            }

            howToPlayPanel ??= FindAnyObjectByType<StackMergeHowToPlayPanel>(FindObjectsInactive.Include);
            return howToPlayPanel != null && howToPlayPanel.OwnsButton(button);
        }

        private static Image GetButtonVisualImage(Button button)
        {
            if (button == null)
            {
                return null;
            }

            if (button.targetGraphic is Image targetImage)
            {
                return targetImage;
            }

            return button.image != null ? button.image : button.GetComponent<Image>();
        }

        private Color DarkenButtonColor(Color color)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color darkened = Color.HSVToRGB(h, s, Mathf.Max(0f, v - disabledButtonValueDrop));
            darkened.a = color.a;
            return darkened;
        }

        private static bool HasSubduedButtonLabel(Button button)
        {
            TMP_Text text = button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
            return text != null
                && (text.text.IndexOf("Deselect", StringComparison.OrdinalIgnoreCase) >= 0
                    || text.text.IndexOf("Mellőz", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        // Redesigned Upgrades menu: every upgrade button carries its own NameText + Cost/InfoText
        // children via a StackMergeButtonLabelPair component. Looked up per-call (not cached) since
        // this only runs during UI refreshes, never per-frame. Falls back to the button's own combined
        // text if no label-pair component is present yet, so nothing breaks while wiring is in progress.
        private static void SetUpgradeButtonLabels(Button button, string name, string cost, bool interactable, string description = null, string effect = null)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = interactable;
            description ??= GetUpgradeButtonDescription(button, name);

            StackMergeButtonLabelPair labels = button.GetComponent<StackMergeButtonLabelPair>();
            if (labels != null)
            {
                SetText(labels.nameText, TranslateLeveledUpgradeName(name));
                SetText(labels.descText, description);
                SetText(labels.effectText, effect);
                SetText(labels.costText, cost);
            }
            else
            {
                string effectLine = string.IsNullOrEmpty(effect) ? string.Empty : $"\n{effect}";
                SetButtonText(button, string.IsNullOrEmpty(description) ? $"{name}{effectLine}\n{cost}" : $"{name}\n{description}{effectLine}\n{cost}");
            }
        }

        private static string UpgradeName(string displayName, int level, int maxLevel)
        {
            return $"{displayName} ({level}/{maxLevel})";
        }

        private static string TranslateLeveledUpgradeName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            int suffixStart = name.LastIndexOf(" (", StringComparison.Ordinal);
            if (suffixStart <= 0 || !name.EndsWith(")", StringComparison.Ordinal))
            {
                return TranslateUpgradeBaseName(name);
            }

            string baseName = name[..suffixStart];
            string suffix = name[suffixStart..];
            return $"{TranslateUpgradeBaseName(baseName)}{suffix}";
        }

        private static string TranslateUpgradeBaseName(string baseName)
        {
            if (StackMergeLocalization.CurrentLanguage != StackMergeLanguage.Magyar)
            {
                return StackMergeLocalization.Translate(baseName);
            }

            return baseName switch
            {
                "Base <sprite name=\"chips\" tint=1> income" => "Alap <sprite name=\"chips\" tint=1> bevétel",
                "Ending profit" => "Záró nyereség",
                "Merge streak" => "Merge sorozat",
                "Score salvage" => "Pontmentés",
                "Passive <sprite name=\"chips\" tint=1> / tick" => "Passzív <sprite name=\"chips\" tint=1> / tick",
                "Passive tick rate" => "Passzív tick rate",
                "Active multiplier" => "Aktív szorzó",
                "Faster solver" => "Gyorsabb solver",
                "Solver delay" => "Solver késés",
                "Stack rows" => "Stack sorok",
                "Visible blocks" => "Látható blokkok",
                "Higher chance" => "Nagyobb esély",
                "Higher frequency" => "Nagyobb gyakoriság",
                "Auto solve" => "Auto megoldás",
                "Auto restart" => "Auto újraindítás",
                "Solver tuning" => "Solver tuning",
                "+1 Agent slot" => "+1 Ügynök slot",
                "+20 tokens" => "+20 token",
                "\u221a<sprite name=\"token\" tint=1> = <sprite name=\"chips\" tint=1>" => "\u221a<sprite name=\"token\" tint=1> = <sprite name=\"chips\" tint=1>",
                "frames / tick" => "frame / tick",
                _ => StackMergeLocalization.Translate(baseName)
            };
        }

        private static string EffectRange(string current, string next, bool maxed)
        {
            return maxed ? current : $"{current} >> {next}";
        }

        private static string BonusPercent(float percent)
        {
            return $"+{percent:0.#}%";
        }

        private static string ReductionPercent(float percent)
        {
            return percent <= 0.0001f ? "0%" : $"-{percent:0.#}%";
        }

        private static string SecondsValue(float seconds)
        {
            return $"{seconds:0.##}s";
        }

        private static string FrameReduction(long frames)
        {
            return frames <= 0 ? "0" : $"-{frames}";
        }

        private static string GetUpgradeButtonDescription(Button button, string name)
        {
            string lookup = NormalizeLookupName($"{button.name} {name}");

            if (lookup.Contains("modifierlab") || lookup.Contains("modifiersmenu"))
            {
                return "Unlocks rule-changing modules that apply to new runs.";
            }

            if (lookup.Contains("solverspeed") || lookup.Contains("speedupgrade") || lookup.Contains("faster"))
            {
                return "The solver places blocks faster with each level.";
            }

            if (lookup.Contains("computespeed"))
            {
                return "Shrinks the delay of MOCA and PLAN solvers.";
            }

            if (lookup.Contains("autosolve"))
            {
                return "Solver is automatically playing the game.";
            }

            if (lookup.Contains("autorestart"))
            {
                return "When the run ends, a new one starts automatically.";
            }

            // Must be checked before the generic token-pack match below.
            if (lookup.Contains("tokendividend") || lookup.Contains("dividend"))
            {
                return "Held <sprite name=\"token\" tint=1> pay <sprite name=\"chips\" tint=1> times the √ of the hoard.";
            }

            if (lookup.Contains("tokenpack") || lookup.Contains("tokens"))
            {
                return "The currency for Auto restart. Price rises the more <sprite name=\"token\" tint=1> you hold.";
            }

            if (lookup.Contains("comboengine") || lookup.Contains("combo"))
            {
                return "Consecutive merges multiply move income.";
            }

            if (lookup.Contains("salvageprotocol") || lookup.Contains("salvage"))
            {
                return "Converts part of the run's score into <sprite name=\"chips\" tint=1>.";
            }

            if (lookup.Contains("framestick") || lookup.Contains("framespertick"))
            {
                return "Lowers the frames needed for PPO Normal Mode.";
            }

            // Check the Rate variant before the generic "curriculum" match below.
            if (lookup.Contains("curriculumrate"))
            {
                return "Each level shortens how often Curriculum lowers the PPO requirement.";
            }

            if (lookup.Contains("curriculum"))
            {
                return "Lowers the frames needed for PPO Normal Mode.";
            }

            if (lookup.Contains("solvertuning"))
            {
                return "Allows you to modify the solver parameters.";
            }

            if (lookup.Contains("extraagentslot") || lookup.Contains("agentslot"))
            {
                return "Another slot so you can use 3 Agents at once.";
            }

            if (lookup.Contains("agentsmenu") || lookup == NormalizeLookupName("Agents"))
            {
                return "They give you extra bonuses when you unlock them.";
            }

            if (lookup.Contains("stackcapacity"))
            {
                return "Increases the capacity of each stack by 1 per level.";
            }

            if (lookup.Contains("queuepreview") || lookup.Contains("nextpreview"))
            {
                return "Shows 1 more upcoming block per level.";
            }

            if (lookup.Contains("difficultyscaling") || lookup.Contains("higherchance") || lookup.Contains("maxtier"))
            {
                return "Increases the chance of spawning higher blocks.";
            }

            if (lookup.Contains("scalingfrequency") || lookup.Contains("highodds"))
            {
                return "Slightly increases how often higher blocks appear.";
            }

            if (lookup.Contains("profitableending") || lookup.Contains("ending"))
            {
                return "Boosts the <sprite name=\"chips\" tint=1> bonus at the end of the runs.";
            }

            // Must be checked before the generic "yield" match below (Chip Yield), since
            // "Passive Yield" also contains the substring "yield".
            if (lookup.Contains("passiveyield"))
            {
                return "Chips trickle in on a timer, on top of your normal play.";
            }

            if (lookup.Contains("passivetickrate") || lookup.Contains("tickrate"))
            {
                return "Passive Production ticks more often.";
            }

            if (lookup.Contains("activemultiplier") || lookup.Contains("whileactive"))
            {
                return "Boosts Passive Production while you're actively playing.";
            }

            if (lookup.Contains("chipyield") || lookup.Contains("income") || lookup.Contains("yield"))
            {
                return "Boosts base <sprite name=\"chips\" tint=1> income. Early levels give larger gains.";
            }

            return null;
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
            return CalculateBoardHeightForBlockHeight(stackCapacity, StackBlockMinHeight);
        }

        private static float CalculateBoardHeightForBlockHeight(int stackCapacity, float blockHeight)
        {
            int capacity = Mathf.Max(1, stackCapacity);
            return StackInternalPadding + capacity * blockHeight + Mathf.Max(0, capacity - 1) * StackBlockSpacing;
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

        private static string FormatDurationCompact(double seconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(Math.Max(0.0, seconds));
            if (span.TotalDays >= 1.0)
            {
                return $"{(int)span.TotalDays}d {span.Hours}h {span.Minutes}m";
            }

            if (span.TotalHours >= 1.0)
            {
                return $"{(int)span.TotalHours}h {span.Minutes}m";
            }

            if (span.TotalMinutes < 1.0)
            {
                return $"{Math.Max(0, (int)Math.Round(span.TotalSeconds))}s";
            }

            return $"{Math.Max(0, span.Minutes)}m";
        }

        private static string FormatAverageRunDuration(RunHistoryEntry[] history, double playthroughSeconds)
        {
            if (history == null || history.Length == 0)
            {
                return "0s";
            }

            double average = history
                .Where(entry => entry != null && entry.elapsedSeconds > 0.01f)
                .Select(entry => (double)entry.elapsedSeconds)
                .DefaultIfEmpty(playthroughSeconds > 0.0 ? playthroughSeconds / history.Length : 0.0)
                .Average();

            return $"{Math.Max(0, (int)Math.Round(average))}s";
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
                double rounded = Math.Floor(d * 100.0) / 100.0;

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
