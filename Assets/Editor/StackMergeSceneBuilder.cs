using StackMerge;
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
            rootLayout.padding = new RectOffset(64, 64, 42, 42);
            rootLayout.spacing = 22f;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = true;
            rootLayout.childForceExpandWidth = false;
            rootLayout.childForceExpandHeight = false;

            BuildHeader(appRoot);
            Text scoreText;
            Text bestText;
            Text highestText;
            BuildStats(appRoot, out scoreText, out bestText, out highestText);

            RectTransform nextBlocksRoot = BuildNextBlocks(appRoot);
            Text chipsText;
            Text solverText;
            Text speedText;
            Text capacityText;
            Text runStatusText;
            Toggle autoSolveToggle;
            Button[] solverButtons;
            Button speedUpgradeButton;
            Button autoRestartButton;
            Button stackCapacityButton;
            BuildAiPanel(
                appRoot,
                out chipsText,
                out solverText,
                out speedText,
                out capacityText,
                out runStatusText,
                out autoSolveToggle,
                out solverButtons,
                out speedUpgradeButton,
                out autoRestartButton,
                out stackCapacityButton);

            RectTransform[] stackLayers;
            Button[] stackButtons;
            BuildBoard(appRoot, out stackButtons, out stackLayers);

            Text droppedText;
            Text feedbackText;
            Button footerNewGameButton;
            BuildFooter(appRoot, out droppedText, out feedbackText, out footerNewGameButton);

            Button modalNewGameButton;
            Text gameOverScoreText;
            Text gameOverBestText;
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
                chipsText,
                solverText,
                speedText,
                capacityText,
                runStatusText,
                autoSolveToggle,
                solverButtons,
                speedUpgradeButton,
                autoRestartButton,
                stackCapacityButton,
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
            EventSystem eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemObject = new GameObject("EventSystem");
                eventSystem = eventSystemObject.AddComponent<EventSystem>();
            }

            StandaloneInputModule legacyInput = eventSystem.GetComponent<StandaloneInputModule>();
            if (legacyInput != null)
            {
                Object.DestroyImmediate(legacyInput);
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
            layout.preferredHeight = 132f;

            HorizontalLayoutGroup horizontal = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 18f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;

            CreateLogoBlock(header, "Stack", HexColor("#F59E0B"));
            CreateLogoBlock(header, "Merge", HexColor("#7C3AED"));
        }

        private static void BuildStats(RectTransform parent, out Text scoreText, out Text bestText, out Text highestText)
        {
            RectTransform stats = CreateRect("Stats", parent);
            LayoutElement layout = stats.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 920f;
            layout.preferredHeight = 112f;

            HorizontalLayoutGroup horizontal = stats.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 14f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = true;

            scoreText = CreateStatPanel(stats, "Pont", "0", HexColor("#14B8A6"));
            bestText = CreateStatPanel(stats, "Rekord", "0", HexColor("#F97316"));
            highestText = CreateStatPanel(stats, "Legnagyobb", "2", HexColor("#8B5CF6"));
        }

        private static RectTransform BuildNextBlocks(RectTransform parent)
        {
            RectTransform panel = CreatePanel("Next Blocks Panel", parent, HexColor("#1F2937"));
            LayoutElement layout = panel.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 920f;
            layout.preferredHeight = 178f;

            VerticalLayoutGroup vertical = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(18, 18, 14, 18);
            vertical.spacing = 12f;
            vertical.childAlignment = TextAnchor.MiddleCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = false;
            vertical.childForceExpandHeight = false;

            Text title = CreateText("Kovetkezo", panel, 28, FontStyle.Bold, TextAnchor.MiddleCenter, HexColor("#E5E7EB"));
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

            RectTransform nextBlocksRoot = CreateRect("Next Blocks", panel);
            LayoutElement nextLayout = nextBlocksRoot.gameObject.AddComponent<LayoutElement>();
            nextLayout.preferredHeight = 94f;
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

        private static void BuildAiPanel(
            RectTransform parent,
            out Text chipsText,
            out Text solverText,
            out Text speedText,
            out Text capacityText,
            out Text runStatusText,
            out Toggle autoSolveToggle,
            out Button[] solverButtons,
            out Button speedUpgradeButton,
            out Button autoRestartButton,
            out Button stackCapacityButton)
        {
            RectTransform panel = CreatePanel("AI Panel", parent, HexColor("#172033"));
            LayoutElement layout = panel.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 920f;
            layout.preferredHeight = 270f;

            VerticalLayoutGroup vertical = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(18, 18, 14, 18);
            vertical.spacing = 12f;
            vertical.childAlignment = TextAnchor.MiddleCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = true;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            RectTransform titleRow = CreateRect("AI Status", panel);
            titleRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;
            HorizontalLayoutGroup titleLayout = titleRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            titleLayout.spacing = 12f;
            titleLayout.childAlignment = TextAnchor.MiddleCenter;
            titleLayout.childControlWidth = true;
            titleLayout.childControlHeight = true;
            titleLayout.childForceExpandWidth = true;
            titleLayout.childForceExpandHeight = true;

            chipsText = CreateStatusText(titleRow, "Chips: 0", HexColor("#FDE68A"));
            solverText = CreateStatusText(titleRow, "Solver: RAND", HexColor("#93C5FD"));
            speedText = CreateStatusText(titleRow, "Speed L0 | 1.40s", HexColor("#5EEAD4"));
            capacityText = CreateStatusText(titleRow, $"Stack cap: {StackMergeGameState.DefaultStackCapacity}/{StackMergeGameState.MaxStackCapacity}", HexColor("#C4B5FD"));

            RectTransform solverRow = CreateRect("Solver Buttons", panel);
            solverRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
            HorizontalLayoutGroup solverLayout = solverRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            solverLayout.spacing = 12f;
            solverLayout.childAlignment = TextAnchor.MiddleCenter;
            solverLayout.childControlWidth = true;
            solverLayout.childControlHeight = true;
            solverLayout.childForceExpandWidth = true;
            solverLayout.childForceExpandHeight = true;

            solverButtons = new[]
            {
                CreateButton(solverRow, "> RAND", HexColor("#2563EB")),
                CreateButton(solverRow, "HEUR\n90", HexColor("#7C3AED")),
                CreateButton(solverRow, "MOCA\n420", HexColor("#BE123C"))
            };

            RectTransform upgradeRow = CreateRect("Upgrade Controls", panel);
            upgradeRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;
            HorizontalLayoutGroup upgradeLayout = upgradeRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            upgradeLayout.spacing = 12f;
            upgradeLayout.childAlignment = TextAnchor.MiddleCenter;
            upgradeLayout.childControlWidth = true;
            upgradeLayout.childControlHeight = true;
            upgradeLayout.childForceExpandWidth = true;
            upgradeLayout.childForceExpandHeight = true;

            autoSolveToggle = CreateToggle(upgradeRow, "Auto solve", HexColor("#0F766E"));
            speedUpgradeButton = CreateButton(upgradeRow, "Speed +\n20", HexColor("#0891B2"));
            autoRestartButton = CreateButton(upgradeRow, "Auto restart\n180", HexColor("#C2410C"));
            stackCapacityButton = CreateButton(upgradeRow, "Stack +1\n60", HexColor("#4F46E5"));

            RectTransform statusRow = CreateRect("Run Status", panel);
            statusRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
            runStatusText = CreateText("Auto solving", statusRow, 22, FontStyle.Bold, TextAnchor.MiddleCenter, HexColor("#D1D5DB"));
            Stretch(runStatusText.rectTransform);
        }

        private static void BuildBoard(RectTransform parent, out Button[] stackButtons, out RectTransform[] stackLayers)
        {
            RectTransform board = CreateRect("Board", parent);
            LayoutElement layout = board.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 920f;
            layout.preferredHeight = 780f;
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

        private static void BuildFooter(RectTransform parent, out Text droppedText, out Text feedbackText, out Button newGameButton)
        {
            RectTransform footer = CreateRect("Footer", parent);
            LayoutElement layout = footer.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 920f;
            layout.preferredHeight = 104f;

            HorizontalLayoutGroup horizontal = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 18f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = false;

            RectTransform infoPanel = CreatePanel("Run Info", footer, HexColor("#1F2937"));
            infoPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            HorizontalLayoutGroup infoLayout = infoPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
            infoLayout.padding = new RectOffset(20, 20, 8, 8);
            infoLayout.spacing = 18f;
            infoLayout.childAlignment = TextAnchor.MiddleCenter;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = true;

            droppedText = CreateText("Dobasok: 0", infoPanel, 25, FontStyle.Bold, TextAnchor.MiddleLeft, HexColor("#D1D5DB"));
            feedbackText = CreateText(string.Empty, infoPanel, 25, FontStyle.Bold, TextAnchor.MiddleRight, HexColor("#5EEAD4"));

            newGameButton = CreateButton(footer, "Uj jatek", HexColor("#DC2626"));
            LayoutElement resetLayout = newGameButton.gameObject.AddComponent<LayoutElement>();
            resetLayout.preferredWidth = 220f;
            resetLayout.preferredHeight = 76f;
        }

        private static GameObject BuildGameOver(RectTransform parent, out Text scoreText, out Text bestText, out Button newGameButton)
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

            Text title = CreateText("Jatek vege", modal, 54, FontStyle.Bold, TextAnchor.MiddleCenter, HexColor("#F9FAFB"));
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;

            scoreText = CreateText("Pont: 0", modal, 34, FontStyle.Bold, TextAnchor.MiddleCenter, HexColor("#FDBA74"));
            scoreText.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

            bestText = CreateText("Rekord: 0", modal, 28, FontStyle.Bold, TextAnchor.MiddleCenter, HexColor("#C4B5FD"));
            bestText.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

            newGameButton = CreateButton(modal, "Uj jatek", HexColor("#14B8A6"));
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

            Text value = CreateText("4", blockTemplate, 34, FontStyle.Bold, TextAnchor.MiddleCenter, HexColor("#111827"));
            Stretch(value.rectTransform, 6f, 4f, 6f, 4f);
            value.resizeTextForBestFit = true;
            value.resizeTextMinSize = 12;
            value.resizeTextMaxSize = 34;

            blockTemplate.gameObject.SetActive(false);
            return blockTemplate;
        }

        private static RectTransform CreateLogoBlock(Transform parent, string label, Color color)
        {
            RectTransform block = CreatePanel(label, parent, color);
            LayoutElement layout = block.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 230f;
            layout.preferredHeight = 90f;

            Text text = CreateText(label, block, 42, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 20;
            text.resizeTextMaxSize = 42;
            return block;
        }

        private static Text CreateStatPanel(Transform parent, string label, string value, Color accent)
        {
            RectTransform panel = CreatePanel(label, parent, HexColor("#1F2937"));

            VerticalLayoutGroup vertical = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(12, 12, 8, 10);
            vertical.spacing = 2f;
            vertical.childAlignment = TextAnchor.MiddleCenter;
            vertical.childControlWidth = true;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = true;
            vertical.childForceExpandHeight = false;

            Text labelText = CreateText(label, panel, 20, FontStyle.Bold, TextAnchor.MiddleCenter, HexColor("#9CA3AF"));
            labelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

            Text valueText = CreateText(value, panel, 38, FontStyle.Bold, TextAnchor.MiddleCenter, accent);
            valueText.resizeTextForBestFit = true;
            valueText.resizeTextMinSize = 22;
            valueText.resizeTextMaxSize = 38;
            valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;
            return valueText;
        }

        private static Text CreateStatusText(Transform parent, string value, Color color)
        {
            RectTransform panel = CreatePanel(value, parent, HexColor("#111827", 0.72f));
            Text text = CreateText(value, panel, 22, FontStyle.Bold, TextAnchor.MiddleCenter, color);
            Stretch(text.rectTransform, 6f, 0f, 6f, 0f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 22;
            return text;
        }

        private static Button CreateButton(Transform parent, string label, Color color)
        {
            RectTransform rectTransform = CreatePanel(label, parent, color);
            rectTransform.gameObject.AddComponent<LayoutElement>().minWidth = 130f;
            Button button = rectTransform.gameObject.AddComponent<Button>();
            button.targetGraphic = rectTransform.GetComponent<Image>();
            button.colors = ButtonColors(color, Color.Lerp(color, Color.white, 0.18f), Color.Lerp(color, Color.black, 0.5f));

            Text text = CreateText(label, rectTransform, 28, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform, 8f, 0f, 8f, 0f);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = 28;
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

            Text text = CreateText(label, root, 24, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = 24;
            return toggle;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform panel = CreateRect(name, parent);
            Image image = panel.gameObject.AddComponent<Image>();
            image.color = color;
            return panel;
        }

        private static Text CreateText(string text, Transform parent, int fontSize, FontStyle style, TextAnchor alignment, Color color)
        {
            RectTransform textRect = CreateRect("Text", parent);
            Text uiText = textRect.gameObject.AddComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                          Resources.GetBuiltinResource<Font>("Arial.ttf");
            uiText.fontSize = fontSize;
            uiText.fontStyle = style;
            uiText.alignment = alignment;
            uiText.color = color;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Truncate;
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
                Object.DestroyImmediate(existing);
            }
        }
    }
}
