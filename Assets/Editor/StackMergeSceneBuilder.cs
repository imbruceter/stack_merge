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
            VerticalLayoutGroup rootLayout = appRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(52, 52, 34, 28);
            rootLayout.spacing = 16f;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;

            BuildHeader(appRoot);

            TMP_Text scoreText;
            TMP_Text bestText;
            TMP_Text highestText;
            BuildStats(appRoot, out scoreText, out bestText, out highestText);

            TMP_Text chipsText;
            TMP_Text solverText;
            TMP_Text speedText;
            TMP_Text capacityText;
            TMP_Text runStatusText;
            TMP_Text agentSlotsText;
            BuildStatusBar(appRoot, out chipsText, out solverText, out speedText, out capacityText, out runStatusText, out agentSlotsText);

            RectTransform contentRoot = CreateRect("Tab Content", appRoot);
            LayoutElement contentLayout = contentRoot.gameObject.AddComponent<LayoutElement>();
            contentLayout.preferredWidth = 920f;
            contentLayout.flexibleHeight = 1f;
            Stretch(contentRoot);

            RectTransform gameplayPanel;
            RectTransform algorithmsPanel;
            RectTransform upgradesPanel;
            RectTransform agentsPanel;
            RectTransform settingsPanel;
            BuildTabPanels(contentRoot, out gameplayPanel, out algorithmsPanel, out upgradesPanel, out agentsPanel, out settingsPanel);

            RectTransform nextBlocksRoot;
            RectTransform[] stackLayers;
            Button[] stackButtons;
            TMP_Text droppedText;
            TMP_Text feedbackText;
            Button footerNewGameButton;
            BuildGameplayPanel(gameplayPanel, out nextBlocksRoot, out stackButtons, out stackLayers, out droppedText, out feedbackText, out footerNewGameButton);

            Button[] solverButtons = BuildAlgorithmsPanel(algorithmsPanel);

            Toggle autoSolveToggle;
            Button speedUpgradeButton;
            Button autoRestartButton;
            Button stackCapacityButton;
            BuildUpgradesPanel(upgradesPanel, out autoSolveToggle, out speedUpgradeButton, out autoRestartButton, out stackCapacityButton);

            Button[] agentButtons = BuildAgentsPanel(agentsPanel);
            BuildSettingsPanel(settingsPanel);

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
                chipsText,
                solverText,
                speedText,
                capacityText,
                runStatusText,
                agentSlotsText,
                autoSolveToggle,
                solverButtons,
                speedUpgradeButton,
                autoRestartButton,
                stackCapacityButton,
                agentButtons,
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
            Image image = background.gameObject.AddComponent<Image>();
            image.color = HexColor("#111827");
        }

        private static void BuildHeader(RectTransform parent)
        {
            RectTransform header = CreateRect("Header", parent);
            LayoutElement layout = header.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 920f;
            layout.preferredHeight = 98f;

            HorizontalLayoutGroup horizontal = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 14f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            CreateLogoBlock(header, "Stack", HexColor("#F59E0B"));
            CreateLogoBlock(header, "Merge", HexColor("#7C3AED"));
        }

        private static void BuildStats(RectTransform parent, out TMP_Text scoreText, out TMP_Text bestText, out TMP_Text highestText)
        {
            RectTransform stats = CreateRect("Stats", parent);
            LayoutElement layout = stats.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 920f;
            layout.preferredHeight = 92f;

            HorizontalLayoutGroup horizontal = stats.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 12f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = true;

            scoreText = CreateStatPanel(stats, "Pont", "0", HexColor("#14B8A6"));
            bestText = CreateStatPanel(stats, "Rekord", "0", HexColor("#F97316"));
            highestText = CreateStatPanel(stats, "Legnagyobb", "2", HexColor("#8B5CF6"));
        }

        private static void BuildStatusBar(
            RectTransform parent,
            out TMP_Text chipsText,
            out TMP_Text solverText,
            out TMP_Text speedText,
            out TMP_Text capacityText,
            out TMP_Text runStatusText,
            out TMP_Text agentSlotsText)
        {
            RectTransform status = CreatePanel("Status Bar", parent, HexColor("#172033"));
            LayoutElement layout = status.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 920f;
            layout.preferredHeight = 104f;

            VerticalLayoutGroup vertical = status.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(14, 14, 10, 10);
            vertical.spacing = 8f;
            vertical.childAlignment = TextAnchor.MiddleCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = true;

            RectTransform top = CreateRect("Economy Status", status);
            top.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
            HorizontalLayoutGroup topLayout = top.gameObject.AddComponent<HorizontalLayoutGroup>();
            topLayout.spacing = 8f;
            topLayout.childAlignment = TextAnchor.MiddleCenter;
            topLayout.childControlWidth = true;
            topLayout.childControlHeight = true;
            topLayout.childForceExpandWidth = true;
            topLayout.childForceExpandHeight = true;

            chipsText = CreateStatusText(top, "Chips: 0", HexColor("#FDE68A"));
            solverText = CreateStatusText(top, "Solver: RAND", HexColor("#93C5FD"));
            speedText = CreateStatusText(top, "Speed L0 | 1.40s", HexColor("#5EEAD4"));

            RectTransform bottom = CreateRect("Run Status", status);
            bottom.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;
            HorizontalLayoutGroup bottomLayout = bottom.gameObject.AddComponent<HorizontalLayoutGroup>();
            bottomLayout.spacing = 8f;
            bottomLayout.childAlignment = TextAnchor.MiddleCenter;
            bottomLayout.childControlWidth = true;
            bottomLayout.childControlHeight = true;
            bottomLayout.childForceExpandWidth = true;
            bottomLayout.childForceExpandHeight = true;

            capacityText = CreateStatusText(bottom, $"Stack cap: {StackMergeGameState.DefaultStackCapacity}/{StackMergeGameState.MaxStackCapacity}", HexColor("#C4B5FD"));
            agentSlotsText = CreateStatusText(bottom, "Active agents: 0/2", HexColor("#F0ABFC"));
            runStatusText = CreateStatusText(bottom, "Auto solving", HexColor("#D1D5DB"));
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
            RectTransform panel = CreatePanel(name, parent, HexColor("#111827", 0.01f));
            Stretch(panel);
            return panel;
        }

        private static void BuildGameplayPanel(
            RectTransform panel,
            out RectTransform nextBlocksRoot,
            out Button[] stackButtons,
            out RectTransform[] stackLayers,
            out TMP_Text droppedText,
            out TMP_Text feedbackText,
            out Button newGameButton)
        {
            VerticalLayoutGroup vertical = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.spacing = 14f;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = false;
            vertical.childForceExpandHeight = false;

            nextBlocksRoot = BuildNextBlocks(panel);
            BuildBoard(panel, out stackButtons, out stackLayers);
            BuildFooter(panel, out droppedText, out feedbackText, out newGameButton);
        }

        private static RectTransform BuildNextBlocks(RectTransform parent)
        {
            RectTransform panel = CreatePanel("Next Blocks Panel", parent, HexColor("#1F2937"));
            LayoutElement layout = panel.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 920f;
            layout.preferredHeight = 144f;

            VerticalLayoutGroup vertical = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(18, 18, 10, 14);
            vertical.spacing = 8f;
            vertical.childAlignment = TextAnchor.MiddleCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = false;
            vertical.childForceExpandHeight = false;

            TMP_Text title = CreateText("Kovetkezo", panel, 24, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#E5E7EB"));
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

            RectTransform nextBlocksRoot = CreateRect("Next Blocks", panel);
            LayoutElement nextLayout = nextBlocksRoot.gameObject.AddComponent<LayoutElement>();
            nextLayout.preferredHeight = 82f;
            nextLayout.preferredWidth = 540f;

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
            LayoutElement layout = board.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 920f;
            layout.preferredHeight = 660f;
            layout.flexibleHeight = 1f;

            HorizontalLayoutGroup horizontal = board.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 18f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = true;

            stackButtons = new Button[StackMergeGameState.DefaultStackCount];
            stackLayers = new RectTransform[StackMergeGameState.DefaultStackCount];

            for (int i = 0; i < StackMergeGameState.DefaultStackCount; i++)
            {
                RectTransform column = CreatePanel($"Stack {i + 1}", board, HexColor("#182033"));
                LayoutElement columnLayout = column.gameObject.AddComponent<LayoutElement>();
                columnLayout.minWidth = 160f;
                columnLayout.flexibleWidth = 1f;
                columnLayout.flexibleHeight = 1f;

                Button button = column.gameObject.AddComponent<Button>();
                button.targetGraphic = column.GetComponent<Image>();
                button.colors = ButtonColors(HexColor("#253046"), HexColor("#31415D"), HexColor("#111827"));
                stackButtons[i] = button;

                RectTransform fill = CreateRect("Column Fill", column);
                Stretch(fill, 10f, 10f, 10f, 10f);
                Image fillImage = fill.gameObject.AddComponent<Image>();
                fillImage.color = HexColor("#0F172A", 0.52f);
                fill.gameObject.AddComponent<RectMask2D>();
                stackLayers[i] = fill;
            }
        }

        private static void BuildFooter(RectTransform parent, out TMP_Text droppedText, out TMP_Text feedbackText, out Button newGameButton)
        {
            RectTransform footer = CreateRect("Footer", parent);
            LayoutElement layout = footer.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 920f;
            layout.preferredHeight = 86f;

            HorizontalLayoutGroup horizontal = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 14f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = false;

            RectTransform infoPanel = CreatePanel("Run Info", footer, HexColor("#1F2937"));
            infoPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            HorizontalLayoutGroup infoLayout = infoPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            infoLayout.padding = new RectOffset(16, 16, 8, 8);
            infoLayout.spacing = 14f;
            infoLayout.childAlignment = TextAnchor.MiddleCenter;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = true;

            droppedText = CreateText("Dobasok: 0", infoPanel, 22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, HexColor("#D1D5DB"));
            feedbackText = CreateText(string.Empty, infoPanel, 20, FontStyles.Bold, TextAlignmentOptions.MidlineRight, HexColor("#5EEAD4"));

            newGameButton = CreateButton(footer, "Uj jatek", HexColor("#DC2626"), 22);
            LayoutElement resetLayout = newGameButton.gameObject.AddComponent<LayoutElement>();
            resetLayout.preferredWidth = 190f;
            resetLayout.preferredHeight = 68f;
        }

        private static Button[] BuildAlgorithmsPanel(RectTransform panel)
        {
            VerticalLayoutGroup vertical = ConfigureTabList(panel);
            TMP_Text title = CreateSectionTitle(panel, "Algorithms");
            title.text = "Algorithms";

            TMP_Text subtitle = CreateText("Unlock solvers to reveal how they think. Locked cards only show price.", panel, 20, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#CBD5E1"));
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

            RectTransform grid = CreateRect("Algorithm Cards", panel);
            grid.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            GridLayoutGroup gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(285f, 150f);
            gridLayout.spacing = new Vector2(14f, 14f);
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;

            Button[] buttons = new Button[StackMergeSolverCatalog.Definitions.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                SolverDefinition definition = StackMergeSolverCatalog.Definitions[i];
                buttons[i] = CreateButton(grid, $"{definition.DisplayName}\nLocked", HexColor("#2563EB"), 18);
            }

            return buttons;
        }

        private static void BuildUpgradesPanel(RectTransform panel, out Toggle autoSolveToggle, out Button speedUpgradeButton, out Button autoRestartButton, out Button stackCapacityButton)
        {
            ConfigureTabList(panel);
            CreateSectionTitle(panel, "Upgrades");

            TMP_Text subtitle = CreateText("Speed makes every algorithm act faster. Stack capacity makes longer, richer runs possible.", panel, 20, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#CBD5E1"));
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            RectTransform row = CreateRect("Upgrade Cards", panel);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 170f;
            HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 14f;
            rowLayout.childAlignment = TextAnchor.MiddleCenter;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;

            autoSolveToggle = CreateToggle(row, "Auto solve", HexColor("#0F766E"));
            speedUpgradeButton = CreateButton(row, "Speed +\n20", HexColor("#0891B2"), 22);
            autoRestartButton = CreateButton(row, "Auto restart\n180", HexColor("#C2410C"), 22);
            stackCapacityButton = CreateButton(row, "Stack +1\n60", HexColor("#4F46E5"), 22);

            TMP_Text footer = CreateText("Auto restart starts a fresh run after game over. Stack capacity starts at 5 and can grow to 10.", panel, 18, FontStyles.Normal, TextAlignmentOptions.Center, HexColor("#94A3B8"));
            footer.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        }

        private static Button[] BuildAgentsPanel(RectTransform panel)
        {
            ConfigureTabList(panel);
            CreateSectionTitle(panel, "Agents");

            TMP_Text subtitle = CreateText("Agents are managers with passive effects. Buy them to reveal their exact bonus, then equip a small active team.", panel, 20, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#CBD5E1"));
            subtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;

            RectTransform grid = CreateRect("Agent Cards", panel);
            grid.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
            GridLayoutGroup gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(285f, 150f);
            gridLayout.spacing = new Vector2(14f, 14f);
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;

            Button[] buttons = new Button[StackMergeProgression.Agents.Length];
            for (int i = 0; i < buttons.Length; i++)
            {
                AgentDefinition definition = StackMergeProgression.Agents[i];
                buttons[i] = CreateButton(grid, $"{definition.DisplayName}\nLocked", HexColor("#9333EA"), 17);
            }

            return buttons;
        }

        private static void BuildSettingsPanel(RectTransform panel)
        {
            ConfigureTabList(panel);
            CreateSectionTitle(panel, "Settings");

            TMP_Text placeholder = CreateText("Settings placeholder\n\nFuture options can live here: sound, animations, number format, reset save, and accessibility controls.", panel, 26, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#E5E7EB"));
            placeholder.gameObject.AddComponent<LayoutElement>().preferredHeight = 260f;
        }

        private static VerticalLayoutGroup ConfigureTabList(RectTransform panel)
        {
            VerticalLayoutGroup vertical = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(18, 18, 18, 18);
            vertical.spacing = 14f;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = false;
            vertical.childForceExpandHeight = false;
            return vertical;
        }

        private static TMP_Text CreateSectionTitle(Transform parent, string text)
        {
            TMP_Text title = CreateText(text, parent, 38, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#F8FAFC"));
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
            return title;
        }

        private static Button[] BuildBottomTabs(RectTransform parent)
        {
            RectTransform tabs = CreatePanel("Bottom Menu Bar", parent, HexColor("#0F172A"));
            LayoutElement layout = tabs.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 920f;
            layout.preferredHeight = 86f;

            HorizontalLayoutGroup horizontal = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.padding = new RectOffset(10, 10, 10, 10);
            horizontal.spacing = 8f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = true;

            string[] labels = { "Gameplay", "Algorithms", "Upgrades", "Agents", "Settings" };
            Button[] buttons = new Button[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                buttons[i] = CreateButton(tabs, labels[i], i == 0 ? HexColor("#1D4ED8") : HexColor("#1F2937"), 20);
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
            modal.anchorMin = new Vector2(0.5f, 0.5f);
            modal.anchorMax = new Vector2(0.5f, 0.5f);
            modal.pivot = new Vector2(0.5f, 0.5f);
            modal.sizeDelta = new Vector2(720f, 440f);

            VerticalLayoutGroup vertical = modal.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(42, 42, 36, 36);
            vertical.spacing = 22f;
            vertical.childAlignment = TextAnchor.MiddleCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            TMP_Text title = CreateText("Jatek vege", modal, 54, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#F9FAFB"));
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

            scoreText = CreateText("Pont: 0", modal, 34, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#FDBA74"));
            scoreText.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

            bestText = CreateText("Rekord: 0", modal, 28, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#C4B5FD"));
            bestText.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

            newGameButton = CreateButton(modal, "Uj jatek", HexColor("#14B8A6"), 28);
            LayoutElement restartLayout = newGameButton.gameObject.AddComponent<LayoutElement>();
            restartLayout.preferredWidth = 300f;
            restartLayout.preferredHeight = 82f;

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
            Image image = blockTemplate.gameObject.AddComponent<Image>();
            image.color = HexColor("#5EEAD4");

            TMP_Text value = CreateText("4", blockTemplate, 34, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#111827"));
            Stretch(value.rectTransform, 6f, 4f, 6f, 4f);
            value.enableAutoSizing = true;
            value.fontSizeMin = 12;
            value.fontSizeMax = 34;

            blockTemplate.gameObject.SetActive(false);
            return blockTemplate;
        }

        private static RectTransform CreateLogoBlock(Transform parent, string label, Color color)
        {
            RectTransform block = CreatePanel(label, parent, color);
            LayoutElement layout = block.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 210f;
            layout.preferredHeight = 74f;

            TMP_Text text = CreateText(label, block, 38, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform);
            text.enableAutoSizing = true;
            text.fontSizeMin = 18;
            text.fontSizeMax = 38;
            return block;
        }

        private static TMP_Text CreateStatPanel(Transform parent, string label, string value, Color accent)
        {
            RectTransform panel = CreatePanel(label, parent, HexColor("#1F2937"));

            VerticalLayoutGroup vertical = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(10, 10, 6, 8);
            vertical.spacing = 1f;
            vertical.childAlignment = TextAnchor.MiddleCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            TMP_Text labelText = CreateText(label, panel, 18, FontStyles.Bold, TextAlignmentOptions.Center, HexColor("#9CA3AF"));
            labelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            TMP_Text valueText = CreateText(value, panel, 34, FontStyles.Bold, TextAlignmentOptions.Center, accent);
            valueText.enableAutoSizing = true;
            valueText.fontSizeMin = 20;
            valueText.fontSizeMax = 34;
            valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 46f;
            return valueText;
        }

        private static TMP_Text CreateStatusText(Transform parent, string value, Color color)
        {
            RectTransform panel = CreatePanel(value, parent, HexColor("#111827", 0.72f));
            TMP_Text text = CreateText(value, panel, 18, FontStyles.Bold, TextAlignmentOptions.Center, color);
            Stretch(text.rectTransform, 6f, 0f, 6f, 0f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 12;
            text.fontSizeMax = 18;
            return text;
        }

        private static Button CreateButton(Transform parent, string label, Color color, int fontSize)
        {
            RectTransform rectTransform = CreatePanel(label, parent, color);
            rectTransform.gameObject.AddComponent<LayoutElement>().minWidth = 120f;
            Button button = rectTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = rectTransform.GetComponent<Image>();
            button.colors = ButtonColors(color, Color.Lerp(color, Color.white, 0.18f), Color.Lerp(color, Color.black, 0.5f));

            TMP_Text text = CreateText(label, rectTransform, fontSize, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform, 8f, 0f, 8f, 0f);
            text.enableAutoSizing = true;
            text.fontSizeMin = 10;
            text.fontSizeMax = fontSize;
            return button;
        }

        private static Toggle CreateToggle(Transform parent, string label, Color color)
        {
            RectTransform root = CreatePanel(label, parent, color);
            root.gameObject.AddComponent<LayoutElement>().minWidth = 150f;

            Toggle toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = root.GetComponent<Image>();
            toggle.isOn = true;

            HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            RectTransform box = CreatePanel("Box", root, HexColor("#ECFEFF"));
            LayoutElement boxLayout = box.gameObject.AddComponent<LayoutElement>();
            boxLayout.preferredWidth = 34f;
            boxLayout.preferredHeight = 34f;
            Image check = CreatePanel("Checkmark", box, HexColor("#14B8A6")).GetComponent<Image>();
            Stretch(check.rectTransform, 7f, 7f, 7f, 7f);
            toggle.graphic = check;

            TMP_Text text = CreateText(label, root, 22, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, Color.white);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            text.enableAutoSizing = true;
            text.fontSizeMin = 13;
            text.fontSizeMax = 22;
            return toggle;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform panel = CreateRect(name, parent);
            Image image = panel.gameObject.AddComponent<Image>();
            image.color = color;
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
            return uiText;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject.GetComponent<RectTransform>();
        }

        private static void Stretch(RectTransform rectTransform, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
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
