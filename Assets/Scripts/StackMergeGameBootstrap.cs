using System;
using System.Collections.Generic;
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
            new HeuristicStackMergeSolver(),
            new MonteCarloStackMergeSolver()
        };

        [Header("Scene UI")]
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Canvas canvas;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text bestText;
        [SerializeField] private Text highestText;
        [SerializeField] private Text droppedText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private RectTransform nextBlocksRoot;
        [SerializeField] private Button[] stackButtons = Array.Empty<Button>();
        [SerializeField] private RectTransform[] stackBlockLayers = Array.Empty<RectTransform>();
        [SerializeField] private Button[] newGameButtons = Array.Empty<Button>();

        [Header("AI UI")]
        [SerializeField] private Text chipsText;
        [SerializeField] private Text solverText;
        [SerializeField] private Text speedText;
        [SerializeField] private Text capacityText;
        [SerializeField] private Text runStatusText;
        [SerializeField] private Toggle autoSolveToggle;
        [SerializeField] private Button[] solverButtons = Array.Empty<Button>();
        [SerializeField] private Button speedUpgradeButton;
        [SerializeField] private Button autoRestartButton;
        [SerializeField] private Button stackCapacityButton;

        [Header("Templates")]
        [SerializeField] private RectTransform blockTemplate;
        [SerializeField] private bool useGeneratedBlockSprites = true;

        [Header("Game Over")]
        [SerializeField] private GameObject gameOverOverlay;
        [SerializeField] private Text gameOverScoreText;
        [SerializeField] private Text gameOverBestText;

        private StackMergeGameState gameState;
        private StackMergeProgression progression;
        private readonly System.Random solverRandom = new();
        private long highScore;
        private float autoSolveTimer;
        private float autoRestartTimer;
        private int lastScreenWidth;
        private int lastScreenHeight;

        private void Awake()
        {
            ConfigureCamera();
            EnsureEventSystem();
            WireButtons();
            HideTemplate();
        }

        private void Start()
        {
            highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            progression = StackMergeProgression.Load();
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
            Text score,
            Text best,
            Text highest,
            Text dropped,
            Text feedback,
            RectTransform nextRoot,
            Button[] columns,
            RectTransform[] blockLayers,
            Button[] resetButtons,
            Text chips,
            Text solver,
            Text speed,
            Text capacity,
            Text runStatus,
            Toggle autoSolve,
            Button[] solverSelectionButtons,
            Button speedButton,
            Button restartButton,
            Button capacityButton,
            RectTransform blockTemplateReference,
            GameObject gameOver,
            Text gameOverScore,
            Text gameOverBest)
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
            chipsText = chips;
            solverText = solver;
            speedText = speed;
            capacityText = capacity;
            runStatusText = runStatus;
            autoSolveToggle = autoSolve;
            solverButtons = solverSelectionButtons;
            speedUpgradeButton = speedButton;
            autoRestartButton = restartButton;
            stackCapacityButton = capacityButton;
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

            for (int i = 0; i < solverButtons.Length; i++)
            {
                int solverIndex = i;
                if (solverButtons[i] == null)
                {
                    continue;
                }

                solverButtons[i].onClick.RemoveAllListeners();
                solverButtons[i].onClick.AddListener(() => SelectOrUnlockSolver((SolverId)solverIndex));
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

            if (stackCapacityButton != null)
            {
                stackCapacityButton.onClick.RemoveAllListeners();
                stackCapacityButton.onClick.AddListener(BuyStackCapacityUpgrade);
            }

            if (autoSolveToggle != null)
            {
                autoSolveToggle.onValueChanged.RemoveAllListeners();
                autoSolveToggle.onValueChanged.AddListener(SetAutoSolveEnabled);
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
            gameState = new StackMergeGameState(stackCapacity: capacity, seed: Environment.TickCount);
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

        private void SelectOrUnlockSolver(SolverId solverId)
        {
            if (progression == null)
            {
                return;
            }

            bool changed = progression.SelectOrUnlockSolver(solverId);
            SetText(feedbackText, changed ? $"Solver: {solverId.ToString().ToUpperInvariant()}" : "Not enough chips");
            progression.Save();
            RefreshEverything();
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
            SetText(feedbackText, changed ? "Auto restart updated" : "Not enough chips");
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
                ApplyCurrentCapacityToGameState();
            }

            progression.Save();
            RefreshEverything();
        }

        private void ApplyCurrentCapacityToGameState()
        {
            if (gameState == null || gameState.StackCapacity == progression.StackCapacity)
            {
                return;
            }

            StackMergeSnapshot snapshot = gameState.CreateSnapshot();
            var resizedGame = new StackMergeGameState(stackCapacity: progression.StackCapacity, seed: Environment.TickCount);
            resizedGame.RestoreSnapshot(snapshot);
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

            SetText(chipsText, $"Chips: {FormatNumber(progression.Chips)}");
            SetText(solverText, $"Solver: {GetSelectedSolver().DisplayName}");
            SetText(speedText, $"Speed L{progression.SpeedLevel} | {progression.MoveInterval:0.00}s");
            SetText(capacityText, $"Stack cap: {progression.StackCapacity}/{StackMergeGameState.MaxStackCapacity}");

            if (gameState != null && !gameState.IsGameOver)
            {
                SetText(runStatusText, progression.AutoSolveEnabled ? "Auto solving" : "Manual mode");
            }

            if (autoSolveToggle != null)
            {
                autoSolveToggle.SetIsOnWithoutNotify(progression.AutoSolveEnabled);
            }

            RefreshSolverButtons();
            RefreshUpgradeButtons();
        }

        private void RefreshSolverButtons()
        {
            for (int i = 0; i < solverButtons.Length && i < solvers.Length; i++)
            {
                Button button = solverButtons[i];
                if (button == null)
                {
                    continue;
                }

                SolverId solverId = solvers[i].Id;
                bool unlocked = progression.IsSolverUnlocked(solverId);
                bool selected = progression.SelectedSolver == solverId;
                long cost = progression.GetSolverUnlockCost(solverId);

                string label = selected ? $"> {solvers[i].DisplayName}" : solvers[i].DisplayName;
                if (!unlocked)
                {
                    label = $"{solvers[i].DisplayName}\n{FormatNumber(cost)}";
                }

                SetButtonText(button, label);
                button.interactable = unlocked || progression.Chips >= cost;
            }
        }

        private void RefreshUpgradeButtons()
        {
            if (speedUpgradeButton != null)
            {
                if (progression.IsMaxSpeed)
                {
                    SetButtonText(speedUpgradeButton, "Speed\nMAX");
                    speedUpgradeButton.interactable = false;
                }
                else
                {
                    long cost = progression.GetSpeedUpgradeCost();
                    SetButtonText(speedUpgradeButton, $"Speed +\n{FormatNumber(cost)}");
                    speedUpgradeButton.interactable = progression.Chips >= cost;
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

            if (stackCapacityButton != null)
            {
                if (progression.IsMaxStackCapacity)
                {
                    SetButtonText(stackCapacityButton, "Stack cap\nMAX");
                    stackCapacityButton.interactable = false;
                }
                else
                {
                    long cost = progression.GetStackCapacityUpgradeCost();
                    SetButtonText(stackCapacityButton, $"Stack +1\n{FormatNumber(cost)}");
                    stackCapacityButton.interactable = progression.Chips >= cost;
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

            for (int i = 0; i < gameState.NextBlocks.Count; i++)
            {
                RectTransform block = CreateBlock(nextBlocksRoot, gameState.NextBlocks[i], 144f, 78f, 34);
                LayoutElement layout = EnsureComponent<LayoutElement>(block.gameObject);
                layout.preferredWidth = 144f;
                layout.preferredHeight = 78f;
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

            gameOverOverlay.SetActive(gameState.IsGameOver);
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

            Text text = block.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = FormatNumber(value);
                text.fontSize = fontSize;
                text.color = GetReadableTextColor(color);
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 12;
                text.resizeTextMaxSize = Mathf.Max(14, fontSize);
            }

            return block;
        }

        private static RectTransform CreateFallbackBlock(Transform parent)
        {
            GameObject gameObject = new GameObject("Block", typeof(RectTransform), typeof(Image));
            gameObject.transform.SetParent(parent, false);

            GameObject labelObject = new GameObject("Value", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(gameObject.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            Stretch(labelRect, 6f, 4f, 6f, 4f);

            Text text = labelObject.GetComponent<Text>();
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ??
                        Resources.GetBuiltinResource<Font>("Arial.ttf");

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

        private static void SetText(Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        private static void SetButtonText(Button button, string value)
        {
            Text text = button != null ? button.GetComponentInChildren<Text>(true) : null;
            SetText(text, value);
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
