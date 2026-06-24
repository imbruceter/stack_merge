using StackMerge;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StackMerge.Editor
{
    public static class StackMergeSceneBuilder
    {
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

        [MenuItem("Tools/Stack Merge/Rebuild Editable UI Scene")]
        public static void RebuildOpenScene()
        {
            BuildInCurrentScene();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        public static void RebuildSampleScene()
        {
            Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            BuildInCurrentScene();
            EditorSceneManager.SaveScene(scene);
        }

        private static void BuildInCurrentScene()
        {
            RemoveExisting("Stack Merge Game");
            Camera camera = EnsureMainCamera();
            EnsureEventSystem();

            GameObject root = new GameObject("Stack Merge Game");
            StackMergeGameBootstrap controller = root.AddComponent<StackMergeGameBootstrap>();

            Canvas canvas = CreateCanvas(root.transform);
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            CreateBackground(canvasRect);

            RectTransform appRoot = CreateRect("App Root", canvasRect);
            Stretch(appRoot);

            BuildHeader(appRoot);

            RectTransform contentRoot = CreateRect("Tab Content", appRoot);
            SetStretch(contentRoot, 40f, 126f, 40f, 124f);

            RectTransform gameplayPanel;
            RectTransform algorithmsPanel;
            RectTransform upgradesPanel;
            RectTransform modifiersPanel;
            RectTransform historyPanel;
            RectTransform achievementsPanel;
            RectTransform agentsPanel;
            RectTransform settingsPanel;
            BuildTabPanels(contentRoot, out gameplayPanel, out algorithmsPanel, out upgradesPanel, out modifiersPanel, out historyPanel, out achievementsPanel, out agentsPanel, out settingsPanel);

            TMP_Text scoreText;
            TMP_Text bestText;
            TMP_Text highestText;
            TMP_Text gameplayChipsText;
            TMP_Text solverText;
            TMP_Text speedText;
            TMP_Text capacityText;
            TMP_Text queueText;
            TMP_Text runStatusText;
            TMP_Text agentSlotsText;
            RectTransform nextBlocksRoot;
            RectTransform boardRoot;
            RectTransform[] stackLayers;
            Button[] stackButtons;
            TMP_Text droppedText;
            TMP_Text feedbackText;
            Button footerNewGameButton;
            Button historyButton;
            Button achievementsButton;
            Button gameplayInfoButton;
            GameObject gameplayInfoOverlay;
            TMP_Text gameplayInfoText;
            Button gameplayInfoCloseButton;
            BuildGameplayPanel(
                gameplayPanel,
                out scoreText,
                out bestText,
                out highestText,
                out gameplayChipsText,
                out solverText,
                out speedText,
                out capacityText,
                out queueText,
                out runStatusText,
                out agentSlotsText,
                out nextBlocksRoot,
                out boardRoot,
                out stackButtons,
                out stackLayers,
                out droppedText,
                out feedbackText,
                out footerNewGameButton,
                out historyButton,
                out achievementsButton,
                out gameplayInfoButton,
                out gameplayInfoOverlay,
                out gameplayInfoText,
                out gameplayInfoCloseButton);

            TMP_Text algorithmsChipsText;
            TMP_Text solverDetailNameText;
            TMP_Text solverDetailInfoText;
            TMP_Text solverDetailStatusText;
            Button solverDetailActionButton;
            Button solverDetailTuneButton;
            GameObject solverTunePanel;
            TMP_Text solverTuneTitleText;
            TMP_Text solverTuneSummaryText;
            GameObject[] solverTuneRows;
            TMP_Text[] solverTuneNameTexts;
            TMP_Text[] solverTuneValueTexts;
            TMP_Text[] solverTuneDescriptionTexts;
            Slider[] solverTuneSliders;
            Button solverTuneBackButton;
            Button solverTuneResetButton;
            Button[] solverButtons = BuildAlgorithmsPanel(
                algorithmsPanel,
                out algorithmsChipsText,
                out solverDetailNameText,
                out solverDetailInfoText,
                out solverDetailStatusText,
                out solverDetailActionButton,
                out solverDetailTuneButton,
                out solverTunePanel,
                out solverTuneTitleText,
                out solverTuneSummaryText,
                out solverTuneRows,
                out solverTuneNameTexts,
                out solverTuneValueTexts,
                out solverTuneDescriptionTexts,
                out solverTuneSliders,
                out solverTuneBackButton,
                out solverTuneResetButton);

            TMP_Text upgradesChipsText;
            Button autoSolveButton;
            Button[] speedUpgradeButtons;
            Button autoRestartButton;
            Button tokenPackButton;
            Button solverTuningUnlockButton;
            Button extraAgentSlotUpgradeButton;
            Button[] stackCapacityUpgradeButtons;
            Button[] queuePreviewUpgradeButtons;
            Button[] incomeUpgradeButtons;
            Button[] difficultyUpgradeButtons;
            TMP_Text progressionStageText;
            Button modifiersMenuUnlockButton;
            Button agentsMenuUnlockButton;
            BuildUpgradesPanel(
                upgradesPanel,
                out upgradesChipsText,
                out autoSolveButton,
                out speedUpgradeButtons,
                out autoRestartButton,
                out tokenPackButton,
                out solverTuningUnlockButton,
                out extraAgentSlotUpgradeButton,
                out stackCapacityUpgradeButtons,
                out queuePreviewUpgradeButtons,
                out incomeUpgradeButtons,
                out difficultyUpgradeButtons,
                out progressionStageText,
                out modifiersMenuUnlockButton,
                out agentsMenuUnlockButton);

            TMP_Text modifiersChipsText;
            TMP_Text modifierSummaryText;
            TMP_Text modifierDetailNameText;
            TMP_Text modifierDetailInfoText;
            TMP_Text modifierDetailStatusText;
            Button modifierDetailActionButton;
            Button[] modifierButtons = BuildModifiersPanel(
                modifiersPanel,
                out modifiersChipsText,
                out modifierSummaryText,
                out modifierDetailNameText,
                out modifierDetailInfoText,
                out modifierDetailStatusText,
                out modifierDetailActionButton);

            TMP_Text historySummaryText;
            RectTransform historyChartRoot;
            RectTransform historySolverTableRoot;
            RectTransform historyRecentRunsRoot;
            TMP_Text historyInsightText;
            Button historyBackButton;
            BuildHistoryPanel(
                historyPanel,
                out historySummaryText,
                out historyChartRoot,
                out historySolverTableRoot,
                out historyRecentRunsRoot,
                out historyInsightText,
                out historyBackButton);

            TMP_Text achievementStatsText;
            RectTransform achievementListRoot;
            Button achievementBackButton;
            BuildAchievementsPanel(
                achievementsPanel,
                out achievementStatsText,
                out achievementListRoot,
                out achievementBackButton);

            TMP_Text agentsChipsText;
            TMP_Text[] agentSlotTexts;
            TMP_Text agentDetailNameText;
            TMP_Text agentDetailInfoText;
            TMP_Text agentDetailStatusText;
            Button agentDetailActionButton;
            Button[] agentButtons = BuildAgentsPanel(
                agentsPanel,
                out agentsChipsText,
                out agentSlotTexts,
                out agentDetailNameText,
                out agentDetailInfoText,
                out agentDetailStatusText,
                out agentDetailActionButton);

            TMP_Text settingsChipsText;
            BuildSettingsPanel(settingsPanel, out settingsChipsText);

            Button[] tabButtons = BuildBottomTabs(appRoot);

            Button modalNewGameButton;
            TMP_Text gameOverScoreText;
            TMP_Text gameOverBestText;
            GameObject gameOverOverlay = BuildGameOver(canvasRect, out gameOverScoreText, out gameOverBestText, out modalNewGameButton);

            RectTransform blockTemplate = BuildTemplates(canvasRect);

            controller.ConfigureSceneReferences(
                camera,
                canvas,
                scoreText,
                bestText,
                highestText,
                droppedText,
                feedbackText,
                nextBlocksRoot,
                boardRoot,
                stackButtons,
                stackLayers,
                new[] { footerNewGameButton, modalNewGameButton },
                historyButton,
                achievementsButton,
                gameplayInfoButton,
                gameplayInfoOverlay,
                gameplayInfoText,
                gameplayInfoCloseButton,
                gameplayPanel.gameObject,
                algorithmsPanel.gameObject,
                upgradesPanel.gameObject,
                modifiersPanel.gameObject,
                historyPanel.gameObject,
                achievementsPanel.gameObject,
                agentsPanel.gameObject,
                settingsPanel.gameObject,
                tabButtons,
                new[] { gameplayChipsText, algorithmsChipsText, upgradesChipsText, modifiersChipsText, agentsChipsText, settingsChipsText },
                solverText,
                speedText,
                capacityText,
                queueText,
                runStatusText,
                agentSlotsText,
                autoSolveButton,
                solverButtons,
                solverDetailNameText,
                solverDetailInfoText,
                solverDetailStatusText,
                solverDetailActionButton,
                solverDetailTuneButton,
                solverTunePanel,
                solverTuneTitleText,
                solverTuneSummaryText,
                solverTuneRows,
                solverTuneNameTexts,
                solverTuneValueTexts,
                solverTuneDescriptionTexts,
                solverTuneSliders,
                solverTuneBackButton,
                solverTuneResetButton,
                speedUpgradeButtons,
                autoRestartButton,
                tokenPackButton,
                solverTuningUnlockButton,
                extraAgentSlotUpgradeButton,
                stackCapacityUpgradeButtons,
                queuePreviewUpgradeButtons,
                incomeUpgradeButtons,
                difficultyUpgradeButtons,
                progressionStageText,
                modifiersMenuUnlockButton,
                agentsMenuUnlockButton,
                modifierButtons,
                modifierSummaryText,
                modifierDetailNameText,
                modifierDetailInfoText,
                modifierDetailStatusText,
                modifierDetailActionButton,
                historySummaryText,
                historyChartRoot,
                historySolverTableRoot,
                historyRecentRunsRoot,
                historyInsightText,
                historyBackButton,
                achievementStatsText,
                achievementListRoot,
                achievementBackButton,
                agentButtons,
                agentSlotTexts,
                agentDetailNameText,
                agentDetailInfoText,
                agentDetailStatusText,
                agentDetailActionButton,
                blockTemplate,
                gameOverOverlay,
                gameOverScoreText,
                gameOverBestText);

            Selection.activeObject = root;
        }

        private static Camera EnsureMainCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = HexColor("#0B1220");
            camera.transform.position = new Vector3(0f, 0f, -10f);
            return camera;
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            StandaloneInputModule legacyInput = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyInput != null)
            {
                UnityEngine.Object.DestroyImmediate(legacyInput);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static Canvas CreateCanvas(Transform parent)
        {
            GameObject canvasObject = new GameObject("Stack Merge Canvas", typeof(RectTransform));
            canvasObject.transform.SetParent(parent, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            canvasObject.AddComponent<GraphicRaycaster>();

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Stretch(canvasObject.GetComponent<RectTransform>());
            return canvas;
        }

        private static void CreateBackground(RectTransform parent)
        {
            RectTransform background = CreateRect("Background", parent);
            Stretch(background);
            background.gameObject.AddComponent<Image>().color = HexColor("#0B1220");
        }

        private static void BuildHeader(RectTransform parent)
        {
            RectTransform header = CreatePanel("Header", parent, HexColor("#0B1322"));
            SetTopStretch(header, 40f, 24f, 40f, 82f);

            TMP_Text title = CreateText("Stack Merge", header, 38, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#F8FAFC"));
            SetStretch(title.rectTransform, 26f, 0f, 340f, 0f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 18;
            title.fontSizeMax = 38;

            TMP_Text mode = CreateText("Idle Lab", header, 22, FontStyles.Bold, TextAlignmentOptions.MidlineRight, HexColor("#93C5FD"));
            SetStretch(mode.rectTransform, 700f, 0f, 26f, 0f);
            mode.enableAutoSizing = true;
            mode.fontSizeMin = 13;
            mode.fontSizeMax = 22;
        }

        private static void BuildTabPanels(
            RectTransform parent,
            out RectTransform gameplayPanel,
            out RectTransform algorithmsPanel,
            out RectTransform upgradesPanel,
            out RectTransform modifiersPanel,
            out RectTransform historyPanel,
            out RectTransform achievementsPanel,
            out RectTransform agentsPanel,
            out RectTransform settingsPanel)
        {
            gameplayPanel = CreateTabPanel("Gameplay Panel", parent);
            algorithmsPanel = CreateTabPanel("Algorithms Panel", parent);
            upgradesPanel = CreateTabPanel("Upgrades Panel", parent);
            modifiersPanel = CreateTabPanel("Modifiers Panel", parent);
            historyPanel = CreateTabPanel("History Panel", parent);
            achievementsPanel = CreateTabPanel("Achievements Panel", parent);
            agentsPanel = CreateTabPanel("Agents Panel", parent);
            settingsPanel = CreateTabPanel("Settings Panel", parent);

            algorithmsPanel.gameObject.SetActive(false);
            upgradesPanel.gameObject.SetActive(false);
            modifiersPanel.gameObject.SetActive(false);
            historyPanel.gameObject.SetActive(false);
            achievementsPanel.gameObject.SetActive(false);
            agentsPanel.gameObject.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
        }

        private static RectTransform CreateTabPanel(string name, RectTransform parent)
        {
            RectTransform panel = CreateRect(name, parent);
            Stretch(panel);
            return panel;
        }

        private static void BuildGameplayPanel(
            RectTransform panel,
            out TMP_Text scoreText,
            out TMP_Text bestText,
            out TMP_Text highestText,
            out TMP_Text chipsText,
            out TMP_Text solverText,
            out TMP_Text speedText,
            out TMP_Text capacityText,
            out TMP_Text queueText,
            out TMP_Text runStatusText,
            out TMP_Text agentSlotsText,
            out RectTransform nextBlocksRoot,
            out RectTransform boardRoot,
            out Button[] stackButtons,
            out RectTransform[] stackLayers,
            out TMP_Text droppedText,
            out TMP_Text feedbackText,
            out Button newGameButton,
            out Button historyButton,
            out Button achievementsButton,
            out Button infoButton,
            out GameObject infoOverlay,
            out TMP_Text infoText,
            out Button infoCloseButton)
        {
            BuildStats(panel, out scoreText, out bestText, out highestText);
            BuildStatusBar(panel, out chipsText, out solverText, out speedText, out capacityText, out queueText, out runStatusText, out agentSlotsText);
            nextBlocksRoot = BuildNextBlocks(panel);
            BuildBoard(panel, out boardRoot, out stackButtons, out stackLayers);
            BuildFooter(panel, out droppedText, out feedbackText, out newGameButton, out historyButton, out achievementsButton, out infoButton);
            infoOverlay = BuildGameplayInfoOverlay(panel, out infoText, out infoCloseButton);
        }

        private static void BuildStats(RectTransform parent, out TMP_Text scoreText, out TMP_Text bestText, out TMP_Text highestText)
        {
            RectTransform stats = CreateRect("Stats", parent);
            SetTopStretch(stats, 0f, 0f, 0f, 92f);

            scoreText = CreateStatBox(stats, "Score Stat", "Pont", "0", HexColor("#14B8A6"), 0, 1);
            bestText = null;
            highestText = null;
        }

        private static TMP_Text CreateStatBox(RectTransform parent, string name, string label, string value, Color accent, int column, int columns = 3)
        {
            RectTransform box = CreatePanel(name, parent, HexColor("#18212F"));
            SetGridCell(box, column, columns, 0, 1, 12f);

            TMP_Text labelText = CreateText(label, box, 16, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#9CA3AF"));
            SetTopStretch(labelText.rectTransform, 10f, 7f, 10f, 24f);

            TMP_Text valueText = CreateText(value, box, 34, FontStyles.Bold, TextAlignmentOptions.Center, accent);
            SetStretch(valueText.rectTransform, 10f, 28f, 10f, 8f);
            valueText.enableAutoSizing = true;
            valueText.fontSizeMin = 18;
            valueText.fontSizeMax = 34;
            return valueText;
        }

        private static void BuildStatusBar(
            RectTransform parent,
            out TMP_Text chipsText,
            out TMP_Text solverText,
            out TMP_Text speedText,
            out TMP_Text capacityText,
            out TMP_Text queueText,
            out TMP_Text runStatusText,
            out TMP_Text agentSlotsText)
        {
            RectTransform status = CreatePanel("Status Bar", parent, HexColor("#141C2B"));
            SetTopStretch(status, 0f, 104f, 0f, 96f);

            RectTransform topRow = CreateRect("Economy Status", status);
            SetTopStretch(topRow, 12f, 10f, 12f, 34f);

            chipsText = CreateStatusPill(topRow, "Chips", "Chips: 0", HexColor("#FDE68A"), 0, 3);
            solverText = CreateStatusPill(topRow, "Solver", "Solver: RAND", HexColor("#93C5FD"), 1, 3);
            speedText = CreateStatusPill(topRow, "Speed", "Speed L0 | 1.40s", HexColor("#5EEAD4"), 2, 3);

            RectTransform bottomRow = CreateRect("Run Status", status);
            SetTopStretch(bottomRow, 12f, 52f, 12f, 34f);

            runStatusText = CreateStatusPill(bottomRow, "Run State", "Auto solving", HexColor("#D1D5DB"), 0, 1);
            capacityText = null;
            queueText = null;
            agentSlotsText = null;
        }

        private static TMP_Text CreateStatusPill(RectTransform parent, string name, string value, Color color, int column, int columns)
        {
            RectTransform pill = CreatePanel(name, parent, HexColor("#0B1220", 0.72f));
            SetGridCell(pill, column, columns, 0, 1, 8f);

            TMP_Text text = CreateText(value, pill, 18, FontStyles.Bold, TextAlignmentOptions.Center, color);
            SetStretch(text.rectTransform, 6f, 0f, 6f, 0f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 11;
            text.fontSizeMax = 18;
            return text;
        }

        private static RectTransform BuildNextBlocks(RectTransform parent)
        {
            RectTransform panel = CreatePanel("Next Blocks Panel", parent, HexColor("#18212F"));
            SetTopStretch(panel, 0f, 214f, 0f, 132f);

            TMP_Text title = CreateText("Kovetkezo", panel, 22, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#E5E7EB"));
            SetTopStretch(title.rectTransform, 16f, 8f, 16f, 28f);

            RectTransform nextBlocksRoot = CreateRect("Next Blocks", panel);
            SetStretch(nextBlocksRoot, 112f, 44f, 112f, 10f);

            HorizontalLayoutGroup horizontal = nextBlocksRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 16f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            return nextBlocksRoot;
        }

        private static void BuildBoard(RectTransform parent, out RectTransform board, out Button[] stackButtons, out RectTransform[] stackLayers)
        {
            board = CreateRect("Board", parent);
            SetTopStretch(board, 0f, 360f, 0f, BoardHeightForCapacity(StackMergeGameState.DefaultStackCapacity));

            stackButtons = new Button[StackMergeGameState.DefaultStackCount];
            stackLayers = new RectTransform[StackMergeGameState.DefaultStackCount];

            for (int i = 0; i < StackMergeGameState.DefaultStackCount; i++)
            {
                RectTransform column = CreatePanel($"Stack {i + 1}", board, HexColor("#141C2B"));
                SetGridCell(column, i, StackMergeGameState.DefaultStackCount, 0, 1, 18f);

                Button button = column.gameObject.AddComponent<Button>();
                button.targetGraphic = column.GetComponent<Image>();
                button.colors = ButtonColors(HexColor("#253046"), HexColor("#31415D"), HexColor("#0B1220"));
                stackButtons[i] = button;

                RectTransform fill = CreateRect("Column Fill", column);
                SetStretch(fill, 10f, 10f, 10f, 10f);
                Image fillImage = fill.gameObject.AddComponent<Image>();
                fillImage.color = HexColor("#0B1322", 0.52f);
                fill.gameObject.AddComponent<RectMask2D>();
                stackLayers[i] = fill;
            }
        }

        private static void BuildFooter(RectTransform parent, out TMP_Text droppedText, out TMP_Text feedbackText, out Button newGameButton, out Button historyButton, out Button achievementsButton, out Button infoButton)
        {
            RectTransform footer = CreateRect("Footer", parent);
            SetBottomStretch(footer, 0f, 8f, 0f, 80f);

            RectTransform infoPanel = CreatePanel("Run Info", footer, HexColor("#18212F"));
            SetStretch(infoPanel, 0f, 0f, 660f, 0f);

            droppedText = CreateText("Dobasok: 0", infoPanel, 22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#D1D5DB"));
            SetStretch(droppedText.rectTransform, 18f, 0f, 286f, 0f);
            droppedText.enableAutoSizing = true;
            droppedText.fontSizeMin = 13;
            droppedText.fontSizeMax = 22;

            feedbackText = CreateText(string.Empty, infoPanel, 20, FontStyles.Bold, TextAlignmentOptions.MidlineRight, HexColor("#5EEAD4"));
            SetStretch(feedbackText.rectTransform, 150f, 0f, 18f, 0f);
            feedbackText.enableAutoSizing = true;
            feedbackText.fontSizeMin = 12;
            feedbackText.fontSizeMax = 20;

            infoButton = CreateButton(footer, "i", HexColor("#334155"), 26);
            SetRightStretch(infoButton.GetComponent<RectTransform>(), 0f, 576f, 0f, 70f);

            historyButton = CreateButton(footer, "History", HexColor("#2563EB"), 22);
            SetRightStretch(historyButton.GetComponent<RectTransform>(), 0f, 396f, 0f, 166f);

            achievementsButton = CreateButton(footer, "Goals", HexColor("#7C3AED"), 22);
            SetRightStretch(achievementsButton.GetComponent<RectTransform>(), 0f, 204f, 0f, 178f);

            newGameButton = CreateButton(footer, "Uj jatek", HexColor("#DC2626"), 22);
            SetRightStretch(newGameButton.GetComponent<RectTransform>(), 0f, 0f, 0f, 190f);
        }

        private static GameObject BuildGameplayInfoOverlay(RectTransform parent, out TMP_Text infoText, out Button closeButton)
        {
            RectTransform overlay = CreatePanel("Gameplay Info Overlay", parent, HexColor("#020617", 0.74f));
            Stretch(overlay);

            RectTransform modal = CreatePanel("Gameplay Info Modal", overlay, HexColor("#18212F"));
            SetCenter(modal, 760f, 560f);

            TMP_Text title = CreateText("Run Info", modal, 34, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#F8FAFC"));
            SetTopStretch(title.rectTransform, 32f, 26f, 180f, 48f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 18;
            title.fontSizeMax = 34;

            closeButton = CreateButton(modal, "Close", HexColor("#334155"), 20);
            SetTopRight(closeButton.GetComponent<RectTransform>(), 24f, 28f, 138f, 48f);

            infoText = CreateText("Stack cap: 5", modal, 22, FontStyles.Bold, TextAlignmentOptions.TopLeft, HexColor("#CBD5E1"));
            SetStretch(infoText.rectTransform, 34f, 98f, 34f, 34f);
            infoText.enableAutoSizing = true;
            infoText.fontSizeMin = 14;
            infoText.fontSizeMax = 22;

            overlay.gameObject.SetActive(false);
            return overlay.gameObject;
        }

        private static Button[] BuildAlgorithmsPanel(
            RectTransform panel,
            out TMP_Text chipsText,
            out TMP_Text detailNameText,
            out TMP_Text detailInfoText,
            out TMP_Text detailStatusText,
            out Button detailActionButton,
            out Button detailTuneButton,
            out GameObject tunePanel,
            out TMP_Text tuneTitleText,
            out TMP_Text tuneSummaryText,
            out GameObject[] tuneRows,
            out TMP_Text[] tuneNameTexts,
            out TMP_Text[] tuneValueTexts,
            out TMP_Text[] tuneDescriptionTexts,
            out Slider[] tuneSliders,
            out Button tuneBackButton,
            out Button tuneResetButton)
        {
            BuildMenuHeader(panel, "Algoritmusok", out chipsText);

            TMP_Text subtitle = CreateText("Select an algorithm to inspect it. Locked algorithms hide detailed behavior until unlocked.", panel, 20, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#CBD5E1"));
            SetTopStretch(subtitle.rectTransform, 0f, 78f, 0f, 42f);
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 12;
            subtitle.fontSizeMax = 20;

            RectTransform details = CreateCategoryPanel(panel, "Selected Algorithm", 136f, 154f);
            detailNameText = CreateText("RAND", details, 28, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#F8FAFC"));
            SetTopStretch(detailNameText.rectTransform, 0f, 0f, 230f, 36f);
            detailNameText.enableAutoSizing = true;
            detailNameText.fontSizeMin = 16;
            detailNameText.fontSizeMax = 28;

            detailStatusText = CreateText("Active", details, 18, FontStyles.Bold, TextAlignmentOptions.MidlineRight, HexColor("#FDE68A"));
            SetTopStretch(detailStatusText.rectTransform, 650f, 0f, 0f, 32f);
            detailStatusText.enableAutoSizing = true;
            detailStatusText.fontSizeMin = 11;
            detailStatusText.fontSizeMax = 18;

            detailInfoText = CreateText("Randomly chooses any valid stack. Weak, chaotic, but fast.", details, 20, FontStyles.Normal, TextAlignmentOptions.TopLeft, HexColor("#CBD5E1"));
            SetStretch(detailInfoText.rectTransform, 0f, 44f, 220f, 0f);
            detailInfoText.enableAutoSizing = true;
            detailInfoText.fontSizeMin = 12;
            detailInfoText.fontSizeMax = 20;

            detailActionButton = CreateButton(details, "Selected", HexColor("#0F766E"), 22);
            SetTopRight(detailActionButton.GetComponent<RectTransform>(), 44f, 0f, 190f, 44f);

            detailTuneButton = CreateButton(details, "Tune", HexColor("#7C3AED"), 20);
            SetTopRight(detailTuneButton.GetComponent<RectTransform>(), 96f, 0f, 190f, 44f);

            Button[] buttons = new Button[StackMergeSolverCatalog.Definitions.Length];

            RectTransform basic = CreateCategoryPanel(panel, "Basic", 306f, 112f);
            buttons[(int)SolverId.Rand] = CreateSolverButton(basic, SolverId.Rand, 0, 1);

            RectTransform direct = CreateCategoryPanel(panel, "Direct", 434f, 112f);
            buttons[(int)SolverId.Merge] = CreateSolverButton(direct, SolverId.Merge, 0, 2);
            buttons[(int)SolverId.Combo] = CreateSolverButton(direct, SolverId.Combo, 1, 2);

            RectTransform risk = CreateCategoryPanel(panel, "Risk Control", 562f, 112f);
            buttons[(int)SolverId.Balance] = CreateSolverButton(risk, SolverId.Balance, 0, 2);
            buttons[(int)SolverId.AntiStall] = CreateSolverButton(risk, SolverId.AntiStall, 1, 2);

            RectTransform heuristics = CreateCategoryPanel(panel, "Heuristics", 690f, 112f);
            buttons[(int)SolverId.Heur] = CreateSolverButton(heuristics, SolverId.Heur, 0, 2);
            buttons[(int)SolverId.Look] = CreateSolverButton(heuristics, SolverId.Look, 1, 2);

            RectTransform planning = CreateCategoryPanel(panel, "Planning", 818f, 112f);
            buttons[(int)SolverId.Plan3] = CreateSolverButton(planning, SolverId.Plan3, 0, 2);
            buttons[(int)SolverId.Plan5] = CreateSolverButton(planning, SolverId.Plan5, 1, 2);

            RectTransform monteCarlo = CreateCategoryPanel(panel, "Monte Carlo", 946f, 112f);
            buttons[(int)SolverId.Moca] = CreateSolverButton(monteCarlo, SolverId.Moca, 0, 2);
            buttons[(int)SolverId.MocaPlus] = CreateSolverButton(monteCarlo, SolverId.MocaPlus, 1, 2);

            RectTransform treeSearch = CreateCategoryPanel(panel, "Tree / Learning", 1074f, 112f);
            buttons[(int)SolverId.Mcts] = CreateSolverButton(treeSearch, SolverId.Mcts, 0, 2);
            buttons[(int)SolverId.MachineLearning] = CreateSolverButton(treeSearch, SolverId.MachineLearning, 1, 2);

            tunePanel = BuildSolverTunePanel(
                panel,
                out tuneTitleText,
                out tuneSummaryText,
                out tuneRows,
                out tuneNameTexts,
                out tuneValueTexts,
                out tuneDescriptionTexts,
                out tuneSliders,
                out tuneBackButton,
                out tuneResetButton);

            return buttons;
        }

        private static GameObject BuildSolverTunePanel(
            RectTransform parent,
            out TMP_Text titleText,
            out TMP_Text summaryText,
            out GameObject[] rows,
            out TMP_Text[] nameTexts,
            out TMP_Text[] valueTexts,
            out TMP_Text[] descriptionTexts,
            out Slider[] sliders,
            out Button backButton,
            out Button resetButton)
        {
            RectTransform panel = CreatePanel("Solver Tune Panel", parent, HexColor("#0B1220"));
            Stretch(panel);

            titleText = CreateText("HEUR tuning", panel, 38, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#F8FAFC"));
            SetTopStretch(titleText.rectTransform, 0f, 0f, 340f, 58f);
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 20;
            titleText.fontSizeMax = 38;

            backButton = CreateButton(panel, "Back", HexColor("#334155"), 20);
            SetTopRight(backButton.GetComponent<RectTransform>(), 0f, 0f, 146f, 58f);

            resetButton = CreateButton(panel, "Reset", HexColor("#7C2D12"), 20);
            SetTopRight(resetButton.GetComponent<RectTransform>(), 0f, 160f, 146f, 58f);

            RectTransform summary = CreateCategoryPanel(panel, "Tuning Notes", 78f, 120f);
            summaryText = CreateText("Tune this solver after unlocking it.", summary, 18, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#CBD5E1"));
            SetStretch(summaryText.rectTransform, 0f, 0f, 0f, 0f);
            summaryText.enableAutoSizing = true;
            summaryText.fontSizeMin = 11;
            summaryText.fontSizeMax = 18;

            RectTransform options = CreateCategoryPanel(panel, "Parameters", 214f, 724f);
            rows = new GameObject[SolverTuningSettings.MaxSlots];
            nameTexts = new TMP_Text[SolverTuningSettings.MaxSlots];
            valueTexts = new TMP_Text[SolverTuningSettings.MaxSlots];
            descriptionTexts = new TMP_Text[SolverTuningSettings.MaxSlots];
            sliders = new Slider[SolverTuningSettings.MaxSlots];

            for (int i = 0; i < SolverTuningSettings.MaxSlots; i++)
            {
                CreateTuneRow(options, i, rows, nameTexts, valueTexts, descriptionTexts, sliders);
            }

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private static void CreateTuneRow(
            RectTransform parent,
            int rowIndex,
            GameObject[] rows,
            TMP_Text[] nameTexts,
            TMP_Text[] valueTexts,
            TMP_Text[] descriptionTexts,
            Slider[] sliders)
        {
            RectTransform row = CreatePanel($"Tune Parameter {rowIndex + 1}", parent, rowIndex % 2 == 0 ? HexColor("#141C2B") : HexColor("#0B1220", 0.86f));
            SetTopStretch(row, 0f, rowIndex * 106f, 0f, 92f);

            TMP_Text nameText = CreateText("Parameter", row, 20, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#F8FAFC"));
            SetTopStretch(nameText.rectTransform, 18f, 8f, 190f, 26f);
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = 12;
            nameText.fontSizeMax = 20;

            TMP_Text valueText = CreateText("0", row, 18, FontStyles.Bold, TextAlignmentOptions.MidlineRight, HexColor("#FDE68A"));
            SetTopStretch(valueText.rectTransform, 640f, 8f, 18f, 24f);
            valueText.enableAutoSizing = true;
            valueText.fontSizeMin = 11;
            valueText.fontSizeMax = 18;

            TMP_Text descriptionText = CreateText("Description", row, 15, FontStyles.Bold, TextAlignmentOptions.TopLeft, HexColor("#94A3B8"));
            SetTopStretch(descriptionText.rectTransform, 18f, 38f, 330f, 42f);
            descriptionText.enableAutoSizing = true;
            descriptionText.fontSizeMin = 9;
            descriptionText.fontSizeMax = 15;

            Slider slider = CreateSlider(row, HexColor("#60A5FA"));
            SetRightStretch(slider.GetComponent<RectTransform>(), 40f, 18f, 26f, 286f);

            rows[rowIndex] = row.gameObject;
            nameTexts[rowIndex] = nameText;
            valueTexts[rowIndex] = valueText;
            descriptionTexts[rowIndex] = descriptionText;
            sliders[rowIndex] = slider;
        }

        private static Button CreateSolverButton(RectTransform category, SolverId solverId, int column, int columns)
        {
            SolverDefinition definition = StackMergeSolverCatalog.GetDefinition(solverId);
            Button button = CreateButton(category, $"{definition.DisplayName}\nLocked", HexColor("#2563EB"), 18);
            SetGridCell(button.GetComponent<RectTransform>(), column, columns, 0, 1, 12f);
            return button;
        }

        private static void BuildUpgradesPanel(
            RectTransform panel,
            out TMP_Text chipsText,
            out Button autoSolveButton,
            out Button[] speedUpgradeButtons,
            out Button autoRestartButton,
            out Button tokenPackButton,
            out Button solverTuningUnlockButton,
            out Button extraAgentSlotUpgradeButton,
            out Button[] stackCapacityUpgradeButtons,
            out Button[] queuePreviewUpgradeButtons,
            out Button[] incomeUpgradeButtons,
            out Button[] difficultyUpgradeButtons,
            out TMP_Text progressionStageText,
            out Button modifiersMenuUnlockButton,
            out Button agentsMenuUnlockButton)
        {
            BuildMenuHeader(panel, "Fejlesztesek", out chipsText);

            TMP_Text subtitle = CreateText("Buy automation, unlock new layers, then push into riskier systems when the run history proves you are ready.", panel, 20, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#CBD5E1"));
            SetTopStretch(subtitle.rectTransform, 0f, 78f, 0f, 46f);
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 12;
            subtitle.fontSizeMax = 20;

            RectTransform stage = CreateCategoryPanel(panel, "Stage Progression", 150f, 154f);
            progressionStageText = CreateText("Stage 1 - Core automation", stage, 18, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#E2E8F0"));
            SetStretch(progressionStageText.rectTransform, 0f, 0f, 260f, 0f);
            progressionStageText.enableAutoSizing = true;
            progressionStageText.fontSizeMin = 11;
            progressionStageText.fontSizeMax = 18;

            modifiersMenuUnlockButton = CreateButton(stage, "Modifier Lab\nStage locked", HexColor("#334155"), 20);
            SetRightStretch(modifiersMenuUnlockButton.GetComponent<RectTransform>(), 0f, 0f, 0f, 236f);

            RectTransform automation = CreateCategoryPanel(panel, "Automatization", 322f, 128f);
            autoSolveButton = CreateButton(automation, "Auto solve\nNeeds algorithm", HexColor("#0F766E"), 22);
            SetGridCell(autoSolveButton.GetComponent<RectTransform>(), 0, 3, 0, 1, 14f);
            autoRestartButton = CreateButton(automation, "Auto restart\nNeeds algorithm", HexColor("#C2410C"), 22);
            SetGridCell(autoRestartButton.GetComponent<RectTransform>(), 1, 3, 0, 1, 14f);
            tokenPackButton = CreateButton(automation, "+50 tokens\n300 chips", HexColor("#0369A1"), 22);
            SetGridCell(tokenPackButton.GetComponent<RectTransform>(), 2, 3, 0, 1, 14f);

            RectTransform lab = CreateCategoryPanel(panel, "Lab Unlocks", 466f, 128f);
            agentsMenuUnlockButton = CreateButton(lab, "Unlock Agents\n650", HexColor("#9333EA"), 22);
            SetGridCell(agentsMenuUnlockButton.GetComponent<RectTransform>(), 0, 3, 0, 1, 14f);
            solverTuningUnlockButton = CreateButton(lab, "Solver tuning\n700", HexColor("#2563EB"), 22);
            SetGridCell(solverTuningUnlockButton.GetComponent<RectTransform>(), 1, 3, 0, 1, 14f);
            extraAgentSlotUpgradeButton = CreateButton(lab, "+1 Agent slot\n1800", HexColor("#7C3AED"), 22);
            SetGridCell(extraAgentSlotUpgradeButton.GetComponent<RectTransform>(), 2, 3, 0, 1, 14f);

            RectTransform speed = CreateCategoryPanel(panel, "Solver Speed", 610f, 126f);
            speedUpgradeButtons = CreateUpgradeRow(speed, "Speed", HexColor("#0891B2"));

            RectTransform stack = CreateCategoryPanel(panel, "Stack Capacity", 752f, 126f);
            stackCapacityUpgradeButtons = CreateUpgradeRow(stack, "Cap", HexColor("#4F46E5"));

            RectTransform difficulty = CreateCategoryPanel(panel, "Difficulty Scaling", 894f, 126f);
            difficultyUpgradeButtons = CreateUpgradeRow(difficulty, "Risk", HexColor("#DB2777"), 3);

            RectTransform queue = CreateCategoryPanel(panel, "Next Preview", 1036f, 126f);
            queuePreviewUpgradeButtons = CreateUpgradeRow(queue, "Next", HexColor("#7C3AED"), 2);

            RectTransform income = CreateCategoryPanel(panel, "Chip Yield", 1178f, 126f);
            incomeUpgradeButtons = CreateUpgradeRow(income, "Yield", HexColor("#CA8A04"));
        }

        private static Button[] BuildModifiersPanel(
            RectTransform panel,
            out TMP_Text chipsText,
            out TMP_Text summaryText,
            out TMP_Text detailNameText,
            out TMP_Text detailInfoText,
            out TMP_Text detailStatusText,
            out Button detailActionButton)
        {
            BuildMenuHeader(panel, "Modifiers", out chipsText);

            TMP_Text subtitle = CreateText("Late-game rule modules. They raise the ceiling, create rescue tools, and make solver choice matter more than raw price.", panel, 20, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#CBD5E1"));
            SetTopStretch(subtitle.rectTransform, 0f, 78f, 0f, 46f);
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 12;
            subtitle.fontSizeMax = 20;

            RectTransform summary = CreateCategoryPanel(panel, "Lab Status", 150f, 112f);
            summaryText = CreateText("Modifier Lab locked.", summary, 19, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#E2E8F0"));
            SetStretch(summaryText.rectTransform, 0f, 0f, 0f, 0f);
            summaryText.enableAutoSizing = true;
            summaryText.fontSizeMin = 11;
            summaryText.fontSizeMax = 19;

            RectTransform details = CreateCategoryPanel(panel, "Selected Modifier", 278f, 188f);
            detailNameText = CreateText("Unstable Stack", details, 28, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#F8FAFC"));
            SetTopStretch(detailNameText.rectTransform, 0f, 0f, 230f, 38f);
            detailNameText.enableAutoSizing = true;
            detailNameText.fontSizeMin = 16;
            detailNameText.fontSizeMax = 28;

            detailStatusText = CreateText("Locked", details, 18, FontStyles.Bold, TextAlignmentOptions.MidlineRight, HexColor("#FDE68A"));
            SetTopStretch(detailStatusText.rectTransform, 650f, 0f, 0f, 34f);
            detailStatusText.enableAutoSizing = true;
            detailStatusText.fontSizeMin = 11;
            detailStatusText.fontSizeMax = 18;

            detailInfoText = CreateText("Unlock Modifier Lab from Upgrades first.", details, 19, FontStyles.Normal, TextAlignmentOptions.TopLeft, HexColor("#CBD5E1"));
            SetStretch(detailInfoText.rectTransform, 0f, 46f, 220f, 0f);
            detailInfoText.enableAutoSizing = true;
            detailInfoText.fontSizeMin = 11;
            detailInfoText.fontSizeMax = 19;

            detailActionButton = CreateButton(details, "Unlock in\nUpgrades", HexColor("#334155"), 22);
            SetRightStretch(detailActionButton.GetComponent<RectTransform>(), 46f, 0f, 0f, 190f);

            RectTransform collection = CreateCategoryPanel(panel, "Modifier Collection", 482f, 318f);
            return CreateModifierGrid(collection);
        }

        private static Button[] CreateModifierGrid(RectTransform category)
        {
            Button[] buttons = new Button[StackMergeProgression.Modifiers.Length];
            int rows = Mathf.CeilToInt(buttons.Length / 3f);
            for (int i = 0; i < buttons.Length; i++)
            {
                ModifierDefinition definition = StackMergeProgression.Modifiers[i];
                buttons[i] = CreateButton(category, $"{definition.DisplayName}\nLocked", HexColor("#92400E"), 16);
                SetGridCell(buttons[i].GetComponent<RectTransform>(), i % 3, 3, i / 3, rows, 12f);
            }

            return buttons;
        }

        private static Button[] CreateUpgradeRow(RectTransform category, string prefix, Color color, int count = 5)
        {
            Button[] buttons = new Button[count];
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i] = CreateButton(category, $"{prefix} {i + 1}", color, 18);
                SetGridCell(buttons[i].GetComponent<RectTransform>(), i, buttons.Length, 0, 1, 10f);
            }

            return buttons;
        }

        private static void BuildHistoryPanel(
            RectTransform panel,
            out TMP_Text summaryText,
            out RectTransform chartRoot,
            out RectTransform solverTableRoot,
            out RectTransform recentRunsRoot,
            out TMP_Text insightText,
            out Button backButton)
        {
            TMP_Text title = CreateText("History", panel, 38, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#F8FAFC"));
            SetTopStretch(title.rectTransform, 0f, 0f, 300f, 58f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 20;
            title.fontSizeMax = 38;

            backButton = CreateButton(panel, "Back", HexColor("#334155"), 20);
            SetTopRight(backButton.GetComponent<RectTransform>(), 0f, 0f, 170f, 58f);

            RectTransform summary = CreateCategoryPanel(panel, "Run Summary", 78f, 94f);
            summaryText = CreateText("No completed runs yet.", summary, 19, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#CBD5E1"));
            SetStretch(summaryText.rectTransform, 0f, 0f, 0f, 0f);
            summaryText.enableAutoSizing = true;
            summaryText.fontSizeMin = 12;
            summaryText.fontSizeMax = 19;

            RectTransform insight = CreateCategoryPanel(panel, "Algorithm Readout", 188f, 92f);
            insightText = CreateText("Use median and range together: high median means reliable value, narrow range means stable behavior.", insight, 18, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#A7F3D0"));
            SetStretch(insightText.rectTransform, 0f, 0f, 0f, 0f);
            insightText.enableAutoSizing = true;
            insightText.fontSizeMin = 11;
            insightText.fontSizeMax = 18;

            RectTransform solverTable = CreateCategoryPanel(panel, "Algorithm Comparison", 296f, 282f);
            solverTableRoot = CreateRect("Algorithm Table", solverTable);
            Stretch(solverTableRoot);

            RectTransform chart = CreateCategoryPanel(panel, "Median Score Chart", 594f, 220f);
            chartRoot = CreateRect("History Chart", chart);
            Stretch(chartRoot);
            Image chartBackground = chartRoot.gameObject.AddComponent<Image>();
            chartBackground.color = HexColor("#0B1220", 0.58f);

            RectTransform recent = CreateCategoryPanel(panel, "Recent Runs", 830f, 276f);
            recentRunsRoot = CreateRect("Recent Runs Table", recent);
            Stretch(recentRunsRoot);
        }

        private static void BuildAchievementsPanel(
            RectTransform panel,
            out TMP_Text statsText,
            out RectTransform listRoot,
            out Button backButton)
        {
            TMP_Text title = CreateText("Achievements", panel, 38, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#F8FAFC"));
            SetTopStretch(title.rectTransform, 0f, 0f, 300f, 58f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 20;
            title.fontSizeMax = 38;

            backButton = CreateButton(panel, "Back", HexColor("#334155"), 20);
            SetTopRight(backButton.GetComponent<RectTransform>(), 0f, 0f, 170f, 58f);

            RectTransform stats = CreateCategoryPanel(panel, "Lifetime Stats", 78f, 126f);
            statsText = CreateText("Runs: 0 | Merges: 0 | Highest: 2 | Earned: 0 | Spent: 0", stats, 18, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#CBD5E1"));
            SetStretch(statsText.rectTransform, 0f, 0f, 0f, 0f);
            statsText.enableAutoSizing = true;
            statsText.fontSizeMin = 11;
            statsText.fontSizeMax = 18;

            RectTransform goals = CreateCategoryPanel(panel, "Goals", 220f, 730f);
            listRoot = CreateRect("Achievement Goals Table", goals);
            Stretch(listRoot);
        }

        private static Button[] BuildAgentsPanel(
            RectTransform panel,
            out TMP_Text chipsText,
            out TMP_Text[] agentSlotTexts,
            out TMP_Text detailNameText,
            out TMP_Text detailInfoText,
            out TMP_Text detailStatusText,
            out Button detailActionButton)
        {
            BuildMenuHeader(panel, "Agentek", out chipsText);

            RectTransform slots = CreateCategoryPanel(panel, "Active Slots 2 (+1)", 78f, 130f);
            agentSlotTexts = new TMP_Text[3];
            for (int i = 0; i < agentSlotTexts.Length; i++)
            {
                RectTransform slot = CreatePanel($"Agent Slot {i + 1}", slots, HexColor("#141C2B"));
                SetGridCell(slot, i, 3, 0, 1, 12f);

                TMP_Text slotText = CreateText(i == 2 ? "Bonus slot\nNeeds upgrade" : $"Slot {i + 1}\nEmpty", slot, 18, FontStyles.Bold, TextAlignmentOptions.Center, i == 2 ? HexColor("#64748B") : HexColor("#CBD5E1"));
                SetStretch(slotText.rectTransform, 10f, 0f, 10f, 0f);
                slotText.enableAutoSizing = true;
                slotText.fontSizeMin = 11;
                slotText.fontSizeMax = 18;
                agentSlotTexts[i] = slotText;
            }

            RectTransform details = CreateCategoryPanel(panel, "Selected Agent", 224f, 166f);
            detailNameText = CreateText("Merge Broker", details, 28, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#F8FAFC"));
            SetTopStretch(detailNameText.rectTransform, 0f, 0f, 230f, 38f);
            detailNameText.enableAutoSizing = true;
            detailNameText.fontSizeMin = 16;
            detailNameText.fontSizeMax = 28;

            detailStatusText = CreateText("Locked", details, 18, FontStyles.Bold, TextAlignmentOptions.MidlineRight, HexColor("#FDE68A"));
            SetTopStretch(detailStatusText.rectTransform, 650f, 0f, 0f, 34f);
            detailStatusText.enableAutoSizing = true;
            detailStatusText.fontSizeMin = 11;
            detailStatusText.fontSizeMax = 18;

            detailInfoText = CreateText("Boosts merge income.\nCost: 120 chips", details, 20, FontStyles.Normal, TextAlignmentOptions.TopLeft, HexColor("#CBD5E1"));
            SetStretch(detailInfoText.rectTransform, 0f, 46f, 220f, 0f);
            detailInfoText.enableAutoSizing = true;
            detailInfoText.fontSizeMin = 12;
            detailInfoText.fontSizeMax = 20;

            detailActionButton = CreateButton(details, "Buy\n120", HexColor("#0F766E"), 22);
            SetRightStretch(detailActionButton.GetComponent<RectTransform>(), 46f, 0f, 0f, 190f);

            RectTransform collection = CreateCategoryPanel(panel, "Collection", 406f, 390f);

            Button[] buttons = new Button[StackMergeProgression.Agents.Length];
            int rows = Mathf.CeilToInt(buttons.Length / 3f);
            for (int i = 0; i < buttons.Length; i++)
            {
                AgentDefinition definition = StackMergeProgression.Agents[i];
                buttons[i] = CreateButton(collection, $"{definition.DisplayName}\nLocked", HexColor("#9333EA"), 17);
                SetGridCell(buttons[i].GetComponent<RectTransform>(), i % 3, 3, i / 3, rows, 14f);
            }

            return buttons;
        }

        private static void BuildSettingsPanel(RectTransform panel, out TMP_Text chipsText)
        {
            BuildMenuHeader(panel, "Settings", out chipsText);

            RectTransform placeholderPanel = CreatePanel("Settings Placeholder", panel, HexColor("#18212F"));
            SetTopStretch(placeholderPanel, 0f, 138f, 0f, 260f);

            TMP_Text placeholder = CreateText("Placeholder\n\nSound, animation, number format, save reset, and accessibility options can live here later.", placeholderPanel, 26, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#E5E7EB"));
            SetStretch(placeholder.rectTransform, 24f, 24f, 24f, 24f);
            placeholder.enableAutoSizing = true;
            placeholder.fontSizeMin = 14;
            placeholder.fontSizeMax = 26;
        }

        private static RectTransform CreateCategoryPanel(RectTransform parent, string titleText, float top, float height)
        {
            RectTransform panel = CreatePanel($"{titleText} Category", parent, HexColor("#18212F"));
            SetTopStretch(panel, 0f, top, 0f, height);

            TMP_Text title = CreateText(titleText, panel, 20, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#E5E7EB"));
            SetTopStretch(title.rectTransform, 18f, 8f, 18f, 28f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 12;
            title.fontSizeMax = 20;

            RectTransform content = CreateRect($"{titleText} Content", panel);
            SetStretch(content, 18f, 44f, 18f, 14f);
            return content;
        }

        private static void BuildMenuHeader(RectTransform panel, string titleText, out TMP_Text chipsText)
        {
            TMP_Text title = CreateText(titleText, panel, 38, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#F8FAFC"));
            SetTopStretch(title.rectTransform, 0f, 0f, 280f, 58f);
            title.enableAutoSizing = true;
            title.fontSizeMin = 20;
            title.fontSizeMax = 38;

            RectTransform wallet = CreatePanel("Wallet", panel, HexColor("#141C2B"));
            SetTopRight(wallet, 0f, 0f, 340f, 58f);

            chipsText = CreateText("Chips: 0", wallet, 20, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#FDE68A"));
            SetStretch(chipsText.rectTransform, 12f, 0f, 12f, 0f);
            chipsText.enableAutoSizing = true;
            chipsText.fontSizeMin = 12;
            chipsText.fontSizeMax = 20;
        }

        private static Button[] BuildBottomTabs(RectTransform parent)
        {
            RectTransform tabs = CreatePanel("Bottom Menu Bar", parent, HexColor("#0B1322"));
            SetBottomStretch(tabs, 40f, 24f, 40f, 86f);

            string[] labels = { "Jatek", "Algoritmus", "Upgrade", "Modifiers", "Agent", "Settings" };
            Button[] buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                buttons[i] = CreateButton(tabs, labels[i], i == 0 ? HexColor("#1D4ED8") : HexColor("#18212F"), 18);
                SetGridCell(buttons[i].GetComponent<RectTransform>(), i, labels.Length, 0, 1, 8f);
            }

            return buttons;
        }

        private static GameObject BuildGameOver(RectTransform parent, out TMP_Text scoreText, out TMP_Text bestText, out Button newGameButton)
        {
            RectTransform overlay = CreateRect("Game Over Overlay", parent);
            Stretch(overlay);
            Image scrim = overlay.gameObject.AddComponent<Image>();
            scrim.color = HexColor("#030712", 0.78f);

            RectTransform modal = CreatePanel("Game Over Modal", overlay, HexColor("#18212F"));
            SetCenter(modal, 720f, 440f);

            TMP_Text title = CreateText("Jatek vege", modal, 54, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#F9FAFB"));
            SetTopStretch(title.rectTransform, 42f, 36f, 42f, 76f);

            scoreText = CreateText("Pont: 0", modal, 34, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#FDBA74"));
            SetTopStretch(scoreText.rectTransform, 42f, 136f, 42f, 54f);

            bestText = CreateText("Rekord: 0", modal, 28, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#C4B5FD"));
            SetTopStretch(bestText.rectTransform, 42f, 200f, 42f, 48f);

            newGameButton = CreateButton(modal, "Uj jatek", HexColor("#14B8A6"), 28);
            SetBottomCenter(newGameButton.GetComponent<RectTransform>(), 300f, 82f, 42f);

            overlay.gameObject.SetActive(false);
            return overlay.gameObject;
        }

        private static RectTransform BuildTemplates(RectTransform parent)
        {
            RectTransform templates = CreateRect("Templates", parent);
            templates.anchorMin = new Vector2(1f, 0f);
            templates.anchorMax = new Vector2(1f, 0f);
            templates.pivot = new Vector2(1f, 0f);
            templates.anchoredPosition = new Vector2(-32f, 32f);
            templates.sizeDelta = new Vector2(180f, 120f);

            RectTransform blockTemplate = CreateRect("Block Template", templates);
            blockTemplate.sizeDelta = new Vector2(144f, 78f);
            blockTemplate.gameObject.AddComponent<Image>().color = HexColor("#5EEAD4");

            TMP_Text value = CreateText("4", blockTemplate, 34, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#0B1220"));
            SetStretch(value.rectTransform, 6f, 4f, 6f, 4f);
            value.enableAutoSizing = true;
            value.fontSizeMin = 12;
            value.fontSizeMax = 34;

            blockTemplate.gameObject.SetActive(false);
            return blockTemplate;
        }

        private static Button CreateButton(Transform parent, string label, Color color, int fontSize)
        {
            RectTransform rectTransform = CreatePanel(label, parent, color);
            Button button = rectTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = rectTransform.GetComponent<Image>();
            button.colors = ButtonColors(color, Color.Lerp(color, Color.white, 0.18f), Color.Lerp(color, Color.black, 0.5f));

            TMP_Text text = CreateText(label, rectTransform, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetStretch(text.rectTransform, 10f, 0f, 10f, 0f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 10;
            text.fontSizeMax = fontSize;
            return button;
        }

        private static Toggle CreateToggle(Transform parent, string label, Color color)
        {
            RectTransform root = CreatePanel(label, parent, color);
            Toggle toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = root.GetComponent<Image>();
            toggle.isOn = true;

            RectTransform box = CreatePanel("Box", root, HexColor("#ECFEFF"));
            box.anchorMin = new Vector2(0f, 0.5f);
            box.anchorMax = new Vector2(0f, 0.5f);
            box.pivot = new Vector2(0f, 0.5f);
            box.anchoredPosition = new Vector2(24f, 0f);
            box.sizeDelta = new Vector2(38f, 38f);

            Image check = CreatePanel("Checkmark", box, HexColor("#14B8A6")).GetComponent<Image>();
            SetStretch(check.rectTransform, 8f, 8f, 8f, 8f);
            toggle.graphic = check;

            TMP_Text text = CreateText(label, root, 24, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);
            SetStretch(text.rectTransform, 78f, 0f, 18f, 0f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 13;
            text.fontSizeMax = 24;
            return toggle;
        }

        private static Slider CreateSlider(Transform parent, Color color)
        {
            RectTransform root = CreateRect("Slider", parent);
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.minValue = SolverTuningSettings.MinValue;
            slider.maxValue = SolverTuningSettings.MaxValue;
            slider.wholeNumbers = true;
            slider.value = 0;

            RectTransform background = CreatePanel("Background", root, HexColor("#0B1322"));
            SetStretch(background, 0f, 7f, 0f, 7f);

            RectTransform fillArea = CreateRect("Fill Area", root);
            SetStretch(fillArea, 7f, 7f, 7f, 7f);

            RectTransform fill = CreatePanel("Fill", fillArea, color);
            SetStretch(fill, 0f, 0f, 0f, 0f);

            RectTransform handleArea = CreateRect("Handle Slide Area", root);
            SetStretch(handleArea, 9f, 0f, 9f, 0f);

            RectTransform handle = CreatePanel("Handle", handleArea, HexColor("#F8FAFC"));
            handle.sizeDelta = new Vector2(22f, 22f);

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.colors = ButtonColors(HexColor("#F8FAFC"), HexColor("#FFFFFF"), HexColor("#64748B"));
            return slider;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform panel = CreateRect(name, parent);
            panel.gameObject.AddComponent<Image>().color = color;
            return panel;
        }

        private static TMP_Text CreateText(string text, Transform parent, int fontSize, FontStyles style, TextAlignmentOptions alignment, Color color)
        {
            RectTransform textRect = CreateRect("Text", parent);
            TextMeshProUGUI uiText = textRect.gameObject.AddComponent<TextMeshProUGUI>();
            uiText.text = text;
            uiText.fontSize = fontSize;
            uiText.fontStyle = style;
            uiText.alignment = alignment;
            uiText.color = color;
            uiText.overflowMode = TextOverflowModes.Ellipsis;
            uiText.raycastTarget = false;
            return uiText;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rectTransform)
        {
            SetStretch(rectTransform, 0f, 0f, 0f, 0f);
        }

        private static void SetStretch(RectTransform rectTransform, float left, float top, float right, float bottom)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static void SetTopStretch(RectTransform rectTransform, float left, float top, float right, float height)
        {
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.offsetMin = new Vector2(left, -top - height);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static void SetBottomStretch(RectTransform rectTransform, float left, float bottom, float right, float height)
        {
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, bottom + height);
        }

        private static void SetRightStretch(RectTransform rectTransform, float top, float right, float bottom, float width)
        {
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 0.5f);
            rectTransform.offsetMin = new Vector2(-right - width, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static void SetTopRight(RectTransform rectTransform, float top, float right, float width, float height)
        {
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-right, -top);
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        private static void SetCenter(RectTransform rectTransform, float width, float height)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        private static void SetBottomCenter(RectTransform rectTransform, float width, float height, float bottom)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, bottom);
            rectTransform.sizeDelta = new Vector2(width, height);
        }

        private static float BoardHeightForCapacity(int stackCapacity)
        {
            const float blockHeight = 74f;
            const float spacing = 7f;
            const float internalPadding = 44f;
            int capacity = Mathf.Max(1, stackCapacity);
            return internalPadding + capacity * blockHeight + Mathf.Max(0, capacity - 1) * spacing;
        }

        private static void SetGridCell(RectTransform rectTransform, int column, int columns, int row, int rows, float gap)
        {
            float xMin = column / (float)columns;
            float xMax = (column + 1) / (float)columns;
            float yMax = 1f - row / (float)rows;
            float yMin = 1f - (row + 1) / (float)rows;

            float left = column == 0 ? 0f : gap * 0.5f;
            float right = column == columns - 1 ? 0f : gap * 0.5f;
            float top = row == 0 ? 0f : gap * 0.5f;
            float bottom = row == rows - 1 ? 0f : gap * 0.5f;

            rectTransform.anchorMin = new Vector2(xMin, yMin);
            rectTransform.anchorMax = new Vector2(xMax, yMax);
            rectTransform.offsetMin = new Vector2(left, bottom);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static void SetGridCellTop(RectTransform rectTransform, int column, int columns, float top, float height, float gap)
        {
            float xMin = column / (float)columns;
            float xMax = (column + 1) / (float)columns;
            float left = column == 0 ? 0f : gap * 0.5f;
            float right = column == columns - 1 ? 0f : gap * 0.5f;

            rectTransform.anchorMin = new Vector2(xMin, 1f);
            rectTransform.anchorMax = new Vector2(xMax, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.offsetMin = new Vector2(left, -top - height);
            rectTransform.offsetMax = new Vector2(-right, -top);
        }

        private static ColorBlock ButtonColors(Color normal, Color highlighted, Color disabled)
        {
            return new ColorBlock
            {
                normalColor = normal,
                highlightedColor = highlighted,
                pressedColor = Color.Lerp(normal, Color.black, 0.18f),
                selectedColor = highlighted,
                disabledColor = disabled,
                colorMultiplier = 1f,
                fadeDuration = 0.08f
            };
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

        private static void RemoveExisting(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }
        }
    }
}
