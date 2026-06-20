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
            RectTransform agentsPanel;
            RectTransform settingsPanel;
            BuildTabPanels(contentRoot, out gameplayPanel, out algorithmsPanel, out upgradesPanel, out agentsPanel, out settingsPanel);

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
            RectTransform[] stackLayers;
            Button[] stackButtons;
            TMP_Text droppedText;
            TMP_Text feedbackText;
            Button footerNewGameButton;
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
                out stackButtons,
                out stackLayers,
                out droppedText,
                out feedbackText,
                out footerNewGameButton);

            TMP_Text algorithmsChipsText;
            TMP_Text solverDetailNameText;
            TMP_Text solverDetailInfoText;
            TMP_Text solverDetailStatusText;
            Button solverDetailActionButton;
            Button[] solverButtons = BuildAlgorithmsPanel(
                algorithmsPanel,
                out algorithmsChipsText,
                out solverDetailNameText,
                out solverDetailInfoText,
                out solverDetailStatusText,
                out solverDetailActionButton);

            TMP_Text upgradesChipsText;
            Toggle autoSolveToggle;
            Button[] speedUpgradeButtons;
            Button autoRestartButton;
            Button[] stackCapacityUpgradeButtons;
            Button[] queuePreviewUpgradeButtons;
            Button[] incomeUpgradeButtons;
            BuildUpgradesPanel(upgradesPanel, out upgradesChipsText, out autoSolveToggle, out speedUpgradeButtons, out autoRestartButton, out stackCapacityUpgradeButtons, out queuePreviewUpgradeButtons, out incomeUpgradeButtons);

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
                stackButtons,
                stackLayers,
                new[] { footerNewGameButton, modalNewGameButton },
                gameplayPanel.gameObject,
                algorithmsPanel.gameObject,
                upgradesPanel.gameObject,
                agentsPanel.gameObject,
                settingsPanel.gameObject,
                tabButtons,
                new[] { gameplayChipsText, algorithmsChipsText, upgradesChipsText, agentsChipsText, settingsChipsText },
                solverText,
                speedText,
                capacityText,
                queueText,
                runStatusText,
                agentSlotsText,
                autoSolveToggle,
                solverButtons,
                solverDetailNameText,
                solverDetailInfoText,
                solverDetailStatusText,
                solverDetailActionButton,
                speedUpgradeButtons,
                autoRestartButton,
                stackCapacityUpgradeButtons,
                queuePreviewUpgradeButtons,
                incomeUpgradeButtons,
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
            camera.backgroundColor = HexColor("#111827");
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
            background.gameObject.AddComponent<Image>().color = HexColor("#111827");
        }

        private static void BuildHeader(RectTransform parent)
        {
            RectTransform header = CreatePanel("Header", parent, HexColor("#0F172A"));
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
            out RectTransform agentsPanel,
            out RectTransform settingsPanel)
        {
            gameplayPanel = CreateTabPanel("Gameplay Panel", parent);
            algorithmsPanel = CreateTabPanel("Algorithms Panel", parent);
            upgradesPanel = CreateTabPanel("Upgrades Panel", parent);
            agentsPanel = CreateTabPanel("Agents Panel", parent);
            settingsPanel = CreateTabPanel("Settings Panel", parent);

            algorithmsPanel.gameObject.SetActive(false);
            upgradesPanel.gameObject.SetActive(false);
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
            out Button[] stackButtons,
            out RectTransform[] stackLayers,
            out TMP_Text droppedText,
            out TMP_Text feedbackText,
            out Button newGameButton)
        {
            BuildStats(panel, out scoreText, out bestText, out highestText);
            BuildStatusBar(panel, out chipsText, out solverText, out speedText, out capacityText, out queueText, out runStatusText, out agentSlotsText);
            nextBlocksRoot = BuildNextBlocks(panel);
            BuildBoard(panel, out stackButtons, out stackLayers);
            BuildFooter(panel, out droppedText, out feedbackText, out newGameButton);
        }

        private static void BuildStats(RectTransform parent, out TMP_Text scoreText, out TMP_Text bestText, out TMP_Text highestText)
        {
            RectTransform stats = CreateRect("Stats", parent);
            SetTopStretch(stats, 0f, 0f, 0f, 92f);

            scoreText = CreateStatBox(stats, "Score Stat", "Pont", "0", HexColor("#14B8A6"), 0);
            bestText = CreateStatBox(stats, "Best Stat", "Rekord", "0", HexColor("#F97316"), 1);
            highestText = CreateStatBox(stats, "Highest Stat", "Legnagyobb", "2", HexColor("#8B5CF6"), 2);
        }

        private static TMP_Text CreateStatBox(RectTransform parent, string name, string label, string value, Color accent, int column)
        {
            RectTransform box = CreatePanel(name, parent, HexColor("#1F2937"));
            SetGridCell(box, column, 3, 0, 1, 12f);

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
            RectTransform status = CreatePanel("Status Bar", parent, HexColor("#172033"));
            SetTopStretch(status, 0f, 104f, 0f, 96f);

            RectTransform topRow = CreateRect("Economy Status", status);
            SetTopStretch(topRow, 12f, 10f, 12f, 34f);

            chipsText = CreateStatusPill(topRow, "Chips", "Chips: 0", HexColor("#FDE68A"), 0, 3);
            solverText = CreateStatusPill(topRow, "Solver", "Solver: RAND", HexColor("#93C5FD"), 1, 3);
            speedText = CreateStatusPill(topRow, "Speed", "Speed L0 | 1.40s", HexColor("#5EEAD4"), 2, 3);

            RectTransform bottomRow = CreateRect("Run Status", status);
            SetTopStretch(bottomRow, 12f, 52f, 12f, 34f);

            capacityText = CreateStatusPill(bottomRow, "Capacity", $"Stack cap: {StackMergeGameState.DefaultStackCapacity}/{StackMergeGameState.MaxStackCapacity}", HexColor("#C4B5FD"), 0, 4);
            queueText = CreateStatusPill(bottomRow, "Next Queue", $"Next: {StackMergeGameState.DefaultQueueLength}", HexColor("#DDD6FE"), 1, 4);
            agentSlotsText = CreateStatusPill(bottomRow, "Agent Slots", "Active agents: 0/2", HexColor("#F0ABFC"), 2, 4);
            runStatusText = CreateStatusPill(bottomRow, "Run State", "Auto solving", HexColor("#D1D5DB"), 3, 4);
        }

        private static TMP_Text CreateStatusPill(RectTransform parent, string name, string value, Color color, int column, int columns)
        {
            RectTransform pill = CreatePanel(name, parent, HexColor("#111827", 0.72f));
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
            RectTransform panel = CreatePanel("Next Blocks Panel", parent, HexColor("#1F2937"));
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

        private static void BuildBoard(RectTransform parent, out Button[] stackButtons, out RectTransform[] stackLayers)
        {
            RectTransform board = CreateRect("Board", parent);
            SetStretch(board, 0f, 360f, 0f, 102f);

            stackButtons = new Button[StackMergeGameState.DefaultStackCount];
            stackLayers = new RectTransform[StackMergeGameState.DefaultStackCount];

            for (int i = 0; i < StackMergeGameState.DefaultStackCount; i++)
            {
                RectTransform column = CreatePanel($"Stack {i + 1}", board, HexColor("#182033"));
                SetGridCell(column, i, StackMergeGameState.DefaultStackCount, 0, 1, 18f);

                Button button = column.gameObject.AddComponent<Button>();
                button.targetGraphic = column.GetComponent<Image>();
                button.colors = ButtonColors(HexColor("#253046"), HexColor("#31415D"), HexColor("#111827"));
                stackButtons[i] = button;

                RectTransform fill = CreateRect("Column Fill", column);
                SetStretch(fill, 10f, 10f, 10f, 10f);
                Image fillImage = fill.gameObject.AddComponent<Image>();
                fillImage.color = HexColor("#0F172A", 0.52f);
                fill.gameObject.AddComponent<RectMask2D>();
                stackLayers[i] = fill;
            }
        }

        private static void BuildFooter(RectTransform parent, out TMP_Text droppedText, out TMP_Text feedbackText, out Button newGameButton)
        {
            RectTransform footer = CreateRect("Footer", parent);
            SetBottomStretch(footer, 0f, 8f, 0f, 80f);

            RectTransform infoPanel = CreatePanel("Run Info", footer, HexColor("#1F2937"));
            SetStretch(infoPanel, 0f, 0f, 206f, 0f);

            droppedText = CreateText("Dobasok: 0", infoPanel, 22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#D1D5DB"));
            SetStretch(droppedText.rectTransform, 18f, 0f, 420f, 0f);
            droppedText.enableAutoSizing = true;
            droppedText.fontSizeMin = 13;
            droppedText.fontSizeMax = 22;

            feedbackText = CreateText(string.Empty, infoPanel, 20, FontStyles.Bold, TextAlignmentOptions.MidlineRight, HexColor("#5EEAD4"));
            SetStretch(feedbackText.rectTransform, 230f, 0f, 18f, 0f);
            feedbackText.enableAutoSizing = true;
            feedbackText.fontSizeMin = 12;
            feedbackText.fontSizeMax = 20;

            newGameButton = CreateButton(footer, "Uj jatek", HexColor("#DC2626"), 22);
            SetRightStretch(newGameButton.GetComponent<RectTransform>(), 0f, 0f, 0f, 190f);
        }

        private static Button[] BuildAlgorithmsPanel(
            RectTransform panel,
            out TMP_Text chipsText,
            out TMP_Text detailNameText,
            out TMP_Text detailInfoText,
            out TMP_Text detailStatusText,
            out Button detailActionButton)
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
            SetRightStretch(detailActionButton.GetComponent<RectTransform>(), 44f, 0f, 0f, 190f);

            Button[] buttons = new Button[StackMergeSolverCatalog.Definitions.Length];

            RectTransform basic = CreateCategoryPanel(panel, "Basic", 306f, 112f);
            buttons[(int)SolverId.Rand] = CreateSolverButton(basic, SolverId.Rand, 0, 1);

            RectTransform heuristics = CreateCategoryPanel(panel, "Heuristics", 434f, 126f);
            buttons[(int)SolverId.Merge] = CreateSolverButton(heuristics, SolverId.Merge, 0, 4);
            buttons[(int)SolverId.Balance] = CreateSolverButton(heuristics, SolverId.Balance, 1, 4);
            buttons[(int)SolverId.Heur] = CreateSolverButton(heuristics, SolverId.Heur, 2, 4);
            buttons[(int)SolverId.Look] = CreateSolverButton(heuristics, SolverId.Look, 3, 4);

            RectTransform planning = CreateCategoryPanel(panel, "Planning", 576f, 112f);
            buttons[(int)SolverId.Plan3] = CreateSolverButton(planning, SolverId.Plan3, 0, 2);
            buttons[(int)SolverId.Plan5] = CreateSolverButton(planning, SolverId.Plan5, 1, 2);

            RectTransform monteCarlo = CreateCategoryPanel(panel, "Monte Carlo", 704f, 112f);
            buttons[(int)SolverId.Moca] = CreateSolverButton(monteCarlo, SolverId.Moca, 0, 1);

            return buttons;
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
            out Toggle autoSolveToggle,
            out Button[] speedUpgradeButtons,
            out Button autoRestartButton,
            out Button[] stackCapacityUpgradeButtons,
            out Button[] queuePreviewUpgradeButtons,
            out Button[] incomeUpgradeButtons)
        {
            BuildMenuHeader(panel, "Fejlesztesek", out chipsText);

            TMP_Text subtitle = CreateText("Unlock each row from left to right. Speed and stack upgrades have five steps; next preview has two.", panel, 20, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#CBD5E1"));
            SetTopStretch(subtitle.rectTransform, 0f, 78f, 0f, 46f);
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 12;
            subtitle.fontSizeMax = 20;

            RectTransform automation = CreateCategoryPanel(panel, "Automatization", 150f, 128f);
            autoSolveToggle = CreateToggle(automation, "Auto solve", HexColor("#0F766E"));
            SetGridCell(autoSolveToggle.GetComponent<RectTransform>(), 0, 2, 0, 1, 14f);
            autoRestartButton = CreateButton(automation, "Auto restart\n180", HexColor("#C2410C"), 22);
            SetGridCell(autoRestartButton.GetComponent<RectTransform>(), 1, 2, 0, 1, 14f);

            RectTransform speed = CreateCategoryPanel(panel, "Solver Speed", 294f, 126f);
            speedUpgradeButtons = CreateUpgradeRow(speed, "Speed", HexColor("#0891B2"));

            RectTransform stack = CreateCategoryPanel(panel, "Stack Capacity", 436f, 126f);
            stackCapacityUpgradeButtons = CreateUpgradeRow(stack, "Cap", HexColor("#4F46E5"));

            RectTransform queue = CreateCategoryPanel(panel, "Next Preview", 578f, 126f);
            queuePreviewUpgradeButtons = CreateUpgradeRow(queue, "Next", HexColor("#7C3AED"), 2);

            RectTransform income = CreateCategoryPanel(panel, "Chip Yield", 720f, 126f);
            incomeUpgradeButtons = CreateUpgradeRow(income, "Yield", HexColor("#CA8A04"));
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
                RectTransform slot = CreatePanel($"Agent Slot {i + 1}", slots, HexColor("#172033"));
                SetGridCell(slot, i, 3, 0, 1, 12f);

                TMP_Text slotText = CreateText(i == 2 ? "Bonus slot\nNeeds Coordinator" : $"Slot {i + 1}\nEmpty", slot, 18, FontStyles.Bold, TextAlignmentOptions.Center, i == 2 ? HexColor("#64748B") : HexColor("#CBD5E1"));
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

            RectTransform collection = CreateCategoryPanel(panel, "Collection", 406f, 290f);

            Button[] buttons = new Button[StackMergeProgression.Agents.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                AgentDefinition definition = StackMergeProgression.Agents[i];
                buttons[i] = CreateButton(collection, $"{definition.DisplayName}\nLocked", HexColor("#9333EA"), 17);
                SetGridCell(buttons[i].GetComponent<RectTransform>(), i % 3, 3, i / 3, 2, 14f);
            }

            return buttons;
        }

        private static void BuildSettingsPanel(RectTransform panel, out TMP_Text chipsText)
        {
            BuildMenuHeader(panel, "Settings", out chipsText);

            RectTransform placeholderPanel = CreatePanel("Settings Placeholder", panel, HexColor("#1F2937"));
            SetTopStretch(placeholderPanel, 0f, 138f, 0f, 260f);

            TMP_Text placeholder = CreateText("Placeholder\n\nSound, animation, number format, save reset, and accessibility options can live here later.", placeholderPanel, 26, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#E5E7EB"));
            SetStretch(placeholder.rectTransform, 24f, 24f, 24f, 24f);
            placeholder.enableAutoSizing = true;
            placeholder.fontSizeMin = 14;
            placeholder.fontSizeMax = 26;
        }

        private static RectTransform CreateCategoryPanel(RectTransform parent, string titleText, float top, float height)
        {
            RectTransform panel = CreatePanel($"{titleText} Category", parent, HexColor("#1F2937"));
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

            RectTransform wallet = CreatePanel("Wallet", panel, HexColor("#172033"));
            SetTopRight(wallet, 0f, 0f, 240f, 58f);

            chipsText = CreateText("Chips: 0", wallet, 20, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#FDE68A"));
            SetStretch(chipsText.rectTransform, 12f, 0f, 12f, 0f);
            chipsText.enableAutoSizing = true;
            chipsText.fontSizeMin = 12;
            chipsText.fontSizeMax = 20;
        }

        private static Button[] BuildBottomTabs(RectTransform parent)
        {
            RectTransform tabs = CreatePanel("Bottom Menu Bar", parent, HexColor("#0F172A"));
            SetBottomStretch(tabs, 40f, 24f, 40f, 86f);

            string[] labels = { "Jatek", "Algoritmus", "Upgrade", "Agent", "Settings" };
            Button[] buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                buttons[i] = CreateButton(tabs, labels[i], i == 0 ? HexColor("#1D4ED8") : HexColor("#1F2937"), 18);
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

            RectTransform modal = CreatePanel("Game Over Modal", overlay, HexColor("#1F2937"));
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

            TMP_Text value = CreateText("4", blockTemplate, 34, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#111827"));
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
