using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StackMerge
{
    public enum StackMergeHowToPlayLayer
    {
        Gameplay,
        Algorithms,
        Upgrades,
        Agents,
        Modifiers,
        Research,
        Datacenter,
        History,
        Achievements
    }

    [AddComponentMenu("Stack Merge/How To Play Panel")]
    public sealed class StackMergeHowToPlayPanel : MonoBehaviour
    {
        [Header("Scene References")]
        [Tooltip("Button in Settings that opens the How To Play panel.")]
        [SerializeField] private Button openButton;

        [Tooltip("Optional close/back button inside the How To Play panel.")]
        [SerializeField] private Button closeButton;

        [Tooltip("Root GameObject of the How To Play Panel. If empty, this GameObject is used.")]
        [SerializeField] private GameObject panelRoot;

        [Tooltip("Parent under which HowToPlayElement instances are created.")]
        [SerializeField] private Transform contentRoot;

        [Tooltip("Optional gameplay bootstrap. Used to hide help sections for layers the player has not unlocked yet.")]
        [SerializeField] private StackMergeGameBootstrap gameBootstrap;

        [Tooltip("Prefab with a StackMergeHowToPlayElement component and Name/Desc text children.")]
        [SerializeField] private StackMergeHowToPlayElement elementPrefab;

        [Tooltip("Optional emphasized prefab with the same StackMergeHowToPlayElement API. Used for highlighted How To Play blocks.")]
        [SerializeField] private StackMergeHowToPlayElement elementWithBackgroundPrefab;

        [Tooltip("Optional formula prefab with the same StackMergeHowToPlayElement API. Used for formula How To Play blocks.")]
        [SerializeField] private StackMergeHowToPlayElement elementFormulaPrefab;

        [Tooltip("Prefab shown instead of the normal element while the related layer is still locked.")]
        [SerializeField] private GameObject lockedElementPrefab;

        [Tooltip("Optional pre-placed elements. If assigned, these are filled instead of instantiating the prefab.")]
        [SerializeField] private StackMergeHowToPlayElement[] sceneElements = Array.Empty<StackMergeHowToPlayElement>();

        [Header("Tabs")]
        [Tooltip("Optional tab buttons. If assigned, the panel shows only the selected layer instead of every help section at once.")]
        [SerializeField] private HowToPlayTab[] tabs = Array.Empty<HowToPlayTab>();
        [SerializeField] private StackMergeHowToPlayLayer defaultLayer = StackMergeHowToPlayLayer.Gameplay;
        [SerializeField] private Sprite lockedTabIcon;
        [SerializeField] private Color lockedTabContentColor = new(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private bool activeTabUsesValueDrop = true;
        [SerializeField, Range(0f, 1f)] private float activeTabValueDrop = 0.4f;
        [SerializeField] private Color activeTabColor = new(0.2f, 0.24f, 0.32f, 1f);

        [Header("Behaviour")]
        [SerializeField] private bool hidePanelOnAwake = true;
        [SerializeField] private bool rebuildOnOpen = true;

        [Header("Locked Copy")]
        [SerializeField, TextArea] private string lockedDescriptionEnglish = "This part of the game is still locked. Keep progressing to reveal this help section.";
        [SerializeField, TextArea] private string lockedDescriptionHungarian = "Ez a jatekreteg meg nincs feloldva. Haladj tovabb, hogy megnyiljon ez a sugo szekcio.";

        private readonly List<StackMergeHowToPlayElement> runtimeElements = new();
        private StackMergeLanguage builtForLanguage = (StackMergeLanguage)(-1);
        private StackMergeHowToPlayLayer selectedLayer = StackMergeHowToPlayLayer.Gameplay;
        private bool built;

        [Serializable]
        private sealed class HowToPlayTab
        {
            public StackMergeHowToPlayLayer layer;
            public Button button;
            public Image icon;
            public TMP_Text text;
            public Sprite unlockedIcon;
            public Sprite lockedIcon;

            [NonSerialized] public UnityAction clickAction;
            [NonSerialized] public bool defaultsCached;
            [NonSerialized] public Color buttonColor;
            [NonSerialized] public Color iconColor;
            [NonSerialized] public Color textColor;
            [NonSerialized] public Sprite fallbackUnlockedIcon;
        }

        private static readonly StackMergeHowToPlayLayer[] SectionLayers =
        {
            StackMergeHowToPlayLayer.Gameplay,
            StackMergeHowToPlayLayer.Algorithms,
            StackMergeHowToPlayLayer.Upgrades,
            StackMergeHowToPlayLayer.Agents,
            StackMergeHowToPlayLayer.Modifiers,
            StackMergeHowToPlayLayer.Research,
            StackMergeHowToPlayLayer.Datacenter
        };

        private void Awake()
        {
            selectedLayer = defaultLayer;
            ResolveReferences();
            WireButtons();
            BuildIfNeeded(true);

            if (hidePanelOnAwake)
            {
                SetPanelVisible(false);
            }
        }

        private void OnDestroy()
        {
            if (openButton != null)
            {
                openButton.onClick.RemoveListener(Open);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            UnwireTabs();
        }

        public void Open()
        {
            ResolveReferences();
            selectedLayer = defaultLayer;
            BuildIfNeeded(rebuildOnOpen || HasTabs());
            SetPanelVisible(true);
        }

        public void Close()
        {
            SetPanelVisible(false);
        }

        public void RefreshContent()
        {
            BuildIfNeeded(true);
        }

        public bool OwnsButton(Button button)
        {
            if (button == null)
            {
                return false;
            }

            GameObject rootObject = panelRoot != null ? panelRoot : gameObject;
            if (rootObject != null && button.transform.IsChildOf(rootObject.transform))
            {
                return true;
            }

            if (tabs == null)
            {
                return false;
            }

            return tabs.Any(tab => tab != null && tab.button == button);
        }

        private void ResolveReferences()
        {
            panelRoot ??= gameObject;
            contentRoot ??= FindNamedDescendant(panelRoot.transform, "Content", "ScrollContent", "Elements", "List");
            gameBootstrap ??= FindAnyObjectByType<StackMergeGameBootstrap>();

            if (closeButton == null && panelRoot != null)
            {
                Transform close = FindNamedDescendant(panelRoot.transform, "CloseButton", "BackButton", "Close", "Back");
                closeButton = close != null ? close.GetComponent<Button>() : null;
            }

            ResolveTabReferences();
        }

        private void WireButtons()
        {
            if (openButton != null)
            {
                openButton.onClick.RemoveListener(Open);
                openButton.onClick.AddListener(Open);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
                closeButton.onClick.AddListener(Close);
            }

            WireTabs();
        }

        private void BuildIfNeeded(bool force)
        {
            StackMergeLanguage language = StackMergeLocalization.CurrentLanguage;
            if (built && !force && builtForLanguage == language)
            {
                return;
            }

            HowToPlaySection[] sections = GetSections(language);
            if (HasTabs())
            {
                ValidateSelectedLayer();
                BuildSelectedSection(sections);
                RefreshTabs(sections);
            }
            else if (sceneElements != null && sceneElements.Length > 0)
            {
                FillSceneElements(sections);
            }
            else
            {
                BuildRuntimeElements(sections);
            }

            built = true;
            builtForLanguage = language;
        }

        private void FillSceneElements(IReadOnlyList<HowToPlaySection> sections)
        {
            ClearRuntimeElements();
            HideTemplateIfChild(lockedElementPrefab);

            for (int i = 0; i < sceneElements.Length; i++)
            {
                StackMergeHowToPlayElement element = sceneElements[i];
                if (element == null)
                {
                    continue;
                }

                bool hasSection = i < sections.Count;
                if (!hasSection)
                {
                    element.gameObject.SetActive(false);
                    continue;
                }

                bool unlocked = IsSectionUnlocked(i);
                if (!unlocked && lockedElementPrefab != null)
                {
                    element.gameObject.SetActive(false);
                    StackMergeHowToPlayElement lockedElement = CreateLockedElement(element.transform.parent, element.transform.GetSiblingIndex());
                    lockedElement.gameObject.SetActive(true);
                    SetElementContent(lockedElement, sections[i], i);
                    runtimeElements.Add(lockedElement);
                    continue;
                }

                element.gameObject.SetActive(true);
                SetElementContent(element, sections[i], i);
            }
        }

        private void BuildRuntimeElements(IReadOnlyList<HowToPlaySection> sections)
        {
            if (contentRoot == null || elementPrefab == null)
            {
                return;
            }

            ClearRuntimeElements();

            HideTemplateIfChild(elementPrefab.gameObject);
            HideTemplateIfChild(elementWithBackgroundPrefab != null ? elementWithBackgroundPrefab.gameObject : null);
            HideTemplateIfChild(elementFormulaPrefab != null ? elementFormulaPrefab.gameObject : null);
            HideTemplateIfChild(lockedElementPrefab);

            for (int i = 0; i < sections.Count; i++)
            {
                StackMergeHowToPlayElement element = CreateRuntimeElement(IsSectionUnlocked(i));
                element.gameObject.SetActive(true);
                SetElementContent(element, sections[i], i);
                runtimeElements.Add(element);
            }
        }

        private void BuildSelectedSection(IReadOnlyList<HowToPlaySection> sections)
        {
            int index = LayerToIndex(selectedLayer);
            if (index < 0 || index >= sections.Count)
            {
                return;
            }

            ClearRuntimeElements();
            HideTemplateIfChild(elementPrefab != null ? elementPrefab.gameObject : null);
            HideTemplateIfChild(elementWithBackgroundPrefab != null ? elementWithBackgroundPrefab.gameObject : null);
            HideTemplateIfChild(elementFormulaPrefab != null ? elementFormulaPrefab.gameObject : null);
            HideTemplateIfChild(lockedElementPrefab);

            HowToPlaySection section = sections[index];
            IReadOnlyList<HowToPlayBlock> blocks = section.Blocks;
            bool canInstantiateBlocks = contentRoot != null && elementPrefab != null;
            bool useSceneElements = sceneElements != null && sceneElements.Length > 0 && (!canInstantiateBlocks || blocks.Count <= 1);

            if (useSceneElements)
            {
                for (int i = 0; i < sceneElements.Length; i++)
                {
                    StackMergeHowToPlayElement element = sceneElements[i];
                    if (element == null)
                    {
                        continue;
                    }

                    bool active = i == index;
                    element.gameObject.SetActive(active);
                    if (active)
                    {
                        SetElementContent(element, section, index);
                    }
                }

                return;
            }

            HideSceneElements();

            if (!canInstantiateBlocks)
            {
                return;
            }

            for (int i = 0; i < blocks.Count; i++)
            {
                HowToPlayBlock block = blocks[i];
                StackMergeHowToPlayElement selectedElement = Instantiate(GetElementPrefab(block.Style), contentRoot);
                selectedElement.gameObject.SetActive(true);
                selectedElement.SetContent(block.Title, block.Description);
                runtimeElements.Add(selectedElement);
            }
        }

        private void HideSceneElements()
        {
            if (sceneElements == null)
            {
                return;
            }

            foreach (StackMergeHowToPlayElement element in sceneElements)
            {
                if (element != null)
                {
                    element.gameObject.SetActive(false);
                }
            }
        }

        private void ResolveTabReferences()
        {
            if (tabs == null || tabs.Length == 0 || panelRoot == null)
            {
                return;
            }

            foreach (HowToPlayTab tab in tabs)
            {
                if (tab == null)
                {
                    continue;
                }

                if (tab.button == null)
                {
                    Transform tabTransform = FindNamedDescendant(panelRoot.transform, tab.layer.ToString(), $"{tab.layer}Tab", $"{tab.layer}Button");
                    tab.button = tabTransform != null ? tabTransform.GetComponent<Button>() : null;
                }

                Transform root = tab.button != null ? tab.button.transform : null;
                if (root == null)
                {
                    continue;
                }

                tab.icon ??= FindNamedDescendant(root, "Icon")?.GetComponent<Image>();
                tab.text ??= FindNamedDescendant(root, "Text", "Label", "Name")?.GetComponent<TMP_Text>();
                CacheTabDefaults(tab);
            }
        }

        private void WireTabs()
        {
            if (tabs == null)
            {
                return;
            }

            foreach (HowToPlayTab tab in tabs)
            {
                if (tab == null || tab.button == null)
                {
                    continue;
                }

                if (tab.clickAction != null)
                {
                    tab.button.onClick.RemoveListener(tab.clickAction);
                }

                StackMergeHowToPlayLayer layer = tab.layer;
                tab.clickAction = () => SelectLayer(layer);
                tab.button.onClick.AddListener(tab.clickAction);
            }
        }

        private void UnwireTabs()
        {
            if (tabs == null)
            {
                return;
            }

            foreach (HowToPlayTab tab in tabs)
            {
                if (tab?.button != null && tab.clickAction != null)
                {
                    tab.button.onClick.RemoveListener(tab.clickAction);
                    tab.clickAction = null;
                }
            }
        }

        private void SelectLayer(StackMergeHowToPlayLayer layer)
        {
            if (!IsLayerUnlocked(layer))
            {
                return;
            }

            selectedLayer = layer;
            BuildIfNeeded(true);
        }

        private void ValidateSelectedLayer()
        {
            if (!IsLayerUnlocked(selectedLayer))
            {
                selectedLayer = StackMergeHowToPlayLayer.Gameplay;
            }
        }

        private void RefreshTabs(IReadOnlyList<HowToPlaySection> sections)
        {
            if (tabs == null)
            {
                return;
            }

            foreach (HowToPlayTab tab in tabs)
            {
                if (tab == null)
                {
                    continue;
                }

                CacheTabDefaults(tab);
                bool unlocked = IsLayerUnlocked(tab.layer);
                bool selected = unlocked && tab.layer == selectedLayer;
                int sectionIndex = LayerToIndex(tab.layer);
                string label = unlocked && sectionIndex >= 0 && sectionIndex < sections.Count
                    ? sections[sectionIndex].Title
                    : "Locked";

                if (tab.button != null)
                {
                    tab.button.transition = Selectable.Transition.None;
                    tab.button.interactable = unlocked && !selected;
                    if (tab.button.targetGraphic != null)
                    {
                        tab.button.targetGraphic.color = selected
                            ? GetActiveTabColor(tab)
                            : unlocked
                                ? tab.buttonColor
                                : GetLockedTabColor(tab);
                    }
                }

                if (tab.icon != null)
                {
                    Sprite unlockedSprite = tab.unlockedIcon != null ? tab.unlockedIcon : tab.fallbackUnlockedIcon;
                    Sprite lockedSprite = tab.lockedIcon != null ? tab.lockedIcon : lockedTabIcon != null ? lockedTabIcon : unlockedSprite;
                    tab.icon.sprite = unlocked ? unlockedSprite : lockedSprite;
                    tab.icon.color = unlocked ? tab.iconColor : lockedTabContentColor;
                }

                if (tab.text != null)
                {
                    tab.text.text = StackMergeSpriteTags.ApplyTint(StackMergeLocalization.Translate(label));
                    tab.text.color = unlocked ? tab.textColor : lockedTabContentColor;
                }
            }
        }

        private Color GetActiveTabColor(HowToPlayTab tab)
        {
            if (activeTabUsesValueDrop && tab != null)
            {
                return DarkenByValue(tab.buttonColor, activeTabValueDrop);
            }

            return activeTabColor;
        }

        private Color GetLockedTabColor(HowToPlayTab tab)
        {
            return tab != null ? DarkenByValue(tab.buttonColor, activeTabValueDrop) : activeTabColor;
        }

        private static Color DarkenByValue(Color color, float valueDrop)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color darkened = Color.HSVToRGB(h, s, Mathf.Max(0f, v - Mathf.Clamp01(valueDrop)));
            darkened.a = color.a;
            return darkened;
        }

        private void CacheTabDefaults(HowToPlayTab tab)
        {
            if (tab == null || tab.defaultsCached)
            {
                return;
            }

            tab.buttonColor = tab.button != null && tab.button.targetGraphic != null
                ? tab.button.targetGraphic.color
                : Color.white;
            tab.iconColor = tab.icon != null ? tab.icon.color : Color.white;
            tab.textColor = tab.text != null ? tab.text.color : Color.white;
            tab.fallbackUnlockedIcon = tab.icon != null ? tab.icon.sprite : null;
            tab.defaultsCached = true;
        }

        private bool HasTabs()
        {
            return tabs != null && tabs.Any(tab => tab?.button != null);
        }

        private StackMergeHowToPlayElement CreateRuntimeElement(bool unlocked)
        {
            if (unlocked || lockedElementPrefab == null)
            {
                return Instantiate(elementPrefab, contentRoot);
            }

            return CreateLockedElement(contentRoot, -1);
        }

        private StackMergeHowToPlayElement GetElementPrefab(HowToPlayBlockStyle style)
        {
            return style switch
            {
                HowToPlayBlockStyle.Formula when elementFormulaPrefab != null => elementFormulaPrefab,
                HowToPlayBlockStyle.Highlighted when elementWithBackgroundPrefab != null => elementWithBackgroundPrefab,
                _ => elementPrefab
            };
        }

        private StackMergeHowToPlayElement CreateLockedElement(Transform parent, int siblingIndex)
        {
            GameObject instance = Instantiate(lockedElementPrefab, parent);
            if (siblingIndex >= 0)
            {
                instance.transform.SetSiblingIndex(siblingIndex);
            }

            StackMergeHowToPlayElement element = instance.GetComponent<StackMergeHowToPlayElement>();
            return element != null ? element : instance.AddComponent<StackMergeHowToPlayElement>();
        }

        private void ClearRuntimeElements()
        {
            for (int i = runtimeElements.Count - 1; i >= 0; i--)
            {
                StackMergeHowToPlayElement element = runtimeElements[i];
                if (element != null)
                {
                    Destroy(element.gameObject);
                }
            }

            runtimeElements.Clear();
        }

        private void SetElementContent(StackMergeHowToPlayElement element, HowToPlaySection section, int index)
        {
            bool unlocked = IsSectionUnlocked(index);
            string description = unlocked ? section.Description : GetLockedDescription(StackMergeLocalization.CurrentLanguage);
            element.SetContent(section.Title, description);
        }

        private bool IsSectionUnlocked(int index)
        {
            StackMergeHowToPlayLayer layer = index >= 0 && index < SectionLayers.Length
                ? SectionLayers[index]
                : StackMergeHowToPlayLayer.Gameplay;

            return IsLayerUnlocked(layer);
        }

        private bool IsLayerUnlocked(StackMergeHowToPlayLayer layer)
        {
            return gameBootstrap == null || gameBootstrap.IsHowToPlayLayerUnlocked(layer);
        }

        private static int LayerToIndex(StackMergeHowToPlayLayer layer)
        {
            for (int i = 0; i < SectionLayers.Length; i++)
            {
                if (SectionLayers[i] == layer)
                {
                    return i;
                }
            }

            return -1;
        }

        private string GetLockedDescription(StackMergeLanguage language)
        {
            return language == StackMergeLanguage.Magyar ? lockedDescriptionHungarian : lockedDescriptionEnglish;
        }

        private void HideTemplateIfChild(GameObject template)
        {
            if (template != null && template.transform.parent == contentRoot)
            {
                template.SetActive(false);
            }
        }

        private void SetPanelVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
                if (visible)
                {
                    panelRoot.transform.SetAsLastSibling();
                }
            }
        }

        private static Transform FindNamedDescendant(Transform root, params string[] names)
        {
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (string name in names)
                {
                    if (string.Equals(child.name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private static HowToPlaySection[] GetSections(StackMergeLanguage language)
        {
            return language == StackMergeLanguage.Magyar ? HungarianSections : EnglishSections;
        }

        private readonly struct HowToPlaySection
        {
            public HowToPlaySection(string title, string description)
                : this(title, new HowToPlayBlock(title, description, false))
            {
            }

            public HowToPlaySection(string title, params HowToPlayBlock[] blocks)
            {
                Title = title;
                Blocks = blocks == null || blocks.Length == 0
                    ? new[] { new HowToPlayBlock(title, string.Empty, false) }
                    : blocks;
            }

            public string Title { get; }
            public IReadOnlyList<HowToPlayBlock> Blocks { get; }

            public string Description => string.Join("\n\n", Blocks.Select(block =>
                string.IsNullOrWhiteSpace(block.Title)
                    ? block.Description
                    : $"<b>{block.Title}</b>\n{block.Description}"));
        }

        private readonly struct HowToPlayBlock
        {
            public HowToPlayBlock(string title, string description, bool highlighted = false)
                : this(title, description, highlighted ? HowToPlayBlockStyle.Highlighted : HowToPlayBlockStyle.Normal)
            {
            }

            public HowToPlayBlock(string title, string description, HowToPlayBlockStyle style)
            {
                Title = title;
                Description = description;
                Style = style;
            }

            public string Title { get; }
            public string Description { get; }
            public HowToPlayBlockStyle Style { get; }
        }

        private enum HowToPlayBlockStyle
        {
            Normal,
            Highlighted,
            Formula
        }

        private static readonly HowToPlaySection[] EnglishSections =
        {
            new(
                "Gameplay",
                new HowToPlayBlock(
                    "Basics",
                    "The core gameplay is very simple. Place blocks into one of the four stacks.\n\n" +
                    "You can merge two blocks of the same value by placing one on top of the other, earning more score and a larger in-game currency reward <sprite name=\"chips\" tint=1>.\n\n" +
                    "Always make sure there is at least one free space available, and try to keep your runs going for as long as possible. A run ends when the next block can no longer be placed anywhere."),
                new HowToPlayBlock(
                    "",
                    "The gameplay changes early on, when algorithms begin playing instead of you. From that point forward, your job is to spend your income on upgrades that make future runs faster, longer, and more valuable.",
                    true),
                new HowToPlayBlock(
                    "Income formula",
                    "<b>ceil((placement + merge + newHigh) x combo x 0.25) x stage x globalIncome</b>",
                    HowToPlayBlockStyle.Formula),
                new HowToPlayBlock(
                    "Layer progression",
                    "The stage multiplier starts at 1x, increases to 5x after unlocking Agents, and reaches 24x after unlocking Modifiers. Because of this, unlocking a new layer is usually more valuable than buying another standard income upgrade.")),
            new(
                "Algorithms",
                new HowToPlayBlock(
                    "Purpose",
                    "Algorithms (solvers) automatically play the game for you.\n\n" +
                    "When Auto Solve is enabled, the selected solver places the blocks. If no solver is selected, the run remains Manual."),
                new HowToPlayBlock(
                    "Priorities",
                    "Each solver prioritizes different values when making decisions, so comparing them is worthwhile.\n\n" +
                    "Their behavior can be adjusted after unlocking Tuning. Tuning parameters can be modified using sliders, but their effects may also negatively impact gameplay.\n\n" +
                    "Besides search depth, the biggest difference between solvers is their speed. Solver speed can be upgraded in several places throughout the game."),
                new HowToPlayBlock(
                    "Speed formula",
                    "General:\n" +
                    "<b>moveInterval = max(minInterval, speedInterval x solverPacing x computePacing x agentPacing x trainingPacing)</b>\n\n" +
                    "For heavy search solvers:\n" +
                    "<b>computePacing = max(0.35, 1 - 0.11 x ComputeSpeedLevel)</b>",
                    HowToPlayBlockStyle.Formula),
                new HowToPlayBlock(
                    "PPO",
                    "The ultimate goal is to unlock PPO, which is a learning algorithm. It starts out weak because it doesn't understand the game's rules, so it must first be trained in Training Mode.\n\n" +
                    "Later, it becomes the gateway to Prestige and permanent progression.\n\n" +
                    "<b>The requirements to unlock it only become achievable later in the game.</b>",
                    true)),
            new(
                "Upgrades",
                new HowToPlayBlock(
                    "Purpose",
                    "This is the primary place to spend <sprite name=\"chips\" tint=1> during the early game.\n\n" +
                    "Here you unlock automation, speed up and expand gameplay, and increase your income generation.\n\n" +
                    "<b>Some upgrades have little impact on their own but become much stronger when combined with others.</b>"),
                new HowToPlayBlock(
                    "Tip",
                    "It's recommended to purchase automation upgrades first so you can start focusing on management as soon as possible.",
                    true),
                new HowToPlayBlock(
                    "",
                    "<b>Keep in mind that Auto Restart consumes <sprite name=\"token\" tint=1>, and cannot function without it.</b>",
                    true),
                new HowToPlayBlock(
                    "Main multipliers",
                    "<b>globalIncome = ChipYield x YieldTheory x MarketBots x TokenDividend</b>\n\n" +
                    "<b>runEnd = ceil((scoreBonus + moveBonus + speedBonus) x (1 + 0.15 x ProfitableEndingLevel) x 0.25)</b>\n\n" +
                    "Stage and global income multipliers are applied after the base end-of-run reward has been calculated.",
                    HowToPlayBlockStyle.Formula)),
            new(
                "Agents",
                new HowToPlayBlock(
                    "",
                    "<b>Unlocking Agents increases the game to a 5x stage multiplier.</b>",
                    true),
                new HowToPlayBlock(
                    "Purpose",
                    "Agents provide active loadout bonuses.\n\n" +
                    "Simply purchasing an Agent isn't enough to make it available, it must also be selected. By default, you can equip two Agents at once, and this limit can be increased through upgrades."),
                new HowToPlayBlock(
                    "",
                    "<b>The combination of equipped Agents determines which aspect of a run becomes more efficient, since most Agents improve a specific reward source instead of multiplying the entire economy.</b>",
                    true),
                new HowToPlayBlock(
                    "",
                    "Some Agents have little effect on their own, but become significantly stronger when combined with other Agents and upgrades."),
                new HowToPlayBlock(
                    "Synergy formula",
                    "<b>agentSynergy = 1 + 0.40 x AgentSynergyLevel</b>",
                    HowToPlayBlockStyle.Formula),
                new HowToPlayBlock(
                    "Tip",
                    "Choose Agents based on what's currently limiting your progress. If runs end too quickly, prioritize stability. Once your runs are long enough, score, high-block, or merge rewards become more valuable.",
                    true)),
            new(
                "Modifiers",
                new HowToPlayBlock(
                    "",
                    "<b>Unlocking Modifiers increases the game to a 24x stage multiplier.</b>",
                    true),
                new HowToPlayBlock(
                    "Purpose",
                    "Modifiers change the rules of future runs. They provide a higher ceiling, a more flexible board, and more tools for advanced solvers to take advantage of."),
                new HowToPlayBlock(
                    "Usage",
                    "Some Modifiers grant actions that can be freely used during gameplay. Every Modifier can also be used in Manual Mode."),
                new HowToPlayBlock(
                    "Reward rules",
                    "Catalyst Stack doubles merge rewards.\n\n" +
                    "<b>salvage = ceil(score x salvageShare x 0.25) x stage x globalIncome</b>\n\n" +
                    "Because runs become longer and more flexible, planning solvers and PPO also become more valuable.",
                    HowToPlayBlockStyle.Formula)),
            new(
                "Research",
                new HowToPlayBlock(
                    "Purpose",
                    "Research is responsible for speeding up and deepening future playthroughs, and it is also where Prestige Reset becomes available.\n\n" +
                    "Here you can purchase permanent upgrades that make gameplay easier, but they require <sprite name=\"insight\" tint=1>.\n\n"),
                new HowToPlayBlock(
                    "",
                    "<sprite name=\"insight\" tint=1> is generated by letting PPO play in Normal Mode.\n\n" +
                    "<b>You must perform a Prestige Reset to claim your <sprite name=\"insight\" tint=1>.</b>",
                    true),
                new HowToPlayBlock(
                    "Prestige Reset",
                    "A Prestige Reset removes all accumulated <sprite name=\"chips\" tint=1>, <sprite name=\"token\" tint=1>, Upgrades, Agents, and Modifiers.\n\n" +
                    "However, your <sprite name=\"insight\" tint=1>, purchased Research, permanent PPO knowledge, and Datacenter progress are preserved.",
                    true),
                new HowToPlayBlock(
                    "<sprite name=\"insight\" tint=1> reward formula",
                    "<b><sprite name=\"insight\" tint=1> ~= round((1 + performance x 1.25 + cycleCarry) x usage x insightMultiplier)</b>\n\n" +
                    "Performance is based on PPO score and highest block. Usage comes from Normal Mode runs and frames.",
                    HowToPlayBlockStyle.Formula)),
            new(
                "Datacenter",
                new HowToPlayBlock(
                    "Purpose",
                    "Datacenter is a permanent compute layer that unlocks after your 5th Prestige Reset.\n\n" +
                    "Every upgrade here accelerates different stages of gameplay.\n\n" +
                    "This layer uses <sprite name=\"chips\" tint=1> as its currency again, meaning the production accumulated by PPO in Normal Mode is no longer lost after Prestige Reset."),
                new HowToPlayBlock(
                    "",
                    "<b>Be sure to spend your accumulated <sprite name=\"chips\" tint=1> before performing a Prestige Reset, otherwise they will be lost.</b>",
                    true),
                new HowToPlayBlock(
                    "Allocations",
                    "You can purchase Server Racks and Facility Upgrades, then allocate the resulting compute capacity however you like between passive PPO training, passive <sprite name=\"insight\" tint=1> generation, or a <sprite name=\"chips\" tint=1> income multiplier.",
                    true),
                new HowToPlayBlock(
                    "Main formula",
                    "<b>cost = baseCost x costGrowth^owned</b>\n\n" +
                    "<b>unitGF/s = baseGF/s x facilityBonus x interconnectBonus x prestigeBonus</b>",
                    HowToPlayBlockStyle.Formula),
                new HowToPlayBlock(
                    "Allocation formulas",
                    "<b>Training frames/sec = totalGF/s x trainingAllocation x 0.60</b>\n" +
                    "<b>Analysis Insight/sec = totalGF/s x analysisAllocation x 0.0012 x fatigue</b>\n" +
                    "<b>Market income multiplier = 1 + 0.20 x log10(1 + marketAllocatedGF/s)</b>",
                    HowToPlayBlockStyle.Formula))
        };

        private static readonly HowToPlaySection[] HungarianSections =
        {
            new(
                "Játékmenet",
                new HowToPlayBlock(
                    "Alapok",
                    "A játékmenet alapvetően nagyon egyszerű. Helyezd a blokkokat a négy kupac egyikébe.\n\n" +
                    "Két ugyanolyan értékű blokk egymásra helyezésével egyesítheted (merge) azokat, így több pontszámra tehetsz szert és nagyobb játékbeli valuta <sprite name=\"chips\" tint=1> jutalomban részesülsz.\n\n" +
                    "Ügyelj arra, hogy mindig legyen legalább egy szabad mozgástér, és húzd a lehető legtovább a runokat. A run akkor ér véget, amikor a soron következő blokk már sehova sem rakható le."),
                new HowToPlayBlock(
                    "",
                    "A játékmenet korán megváltozik, amely során már nem te, hanem algoritmusok játszanak helyetted. Onnantól a te feladatod, hogy a bevételt olyan eszközökre költsd, amelyek gyorsabbá, hosszabbá és értékesebbé teszik a következő runokat.",
                    true),
                new HowToPlayBlock(
                    "Bevételi formula",
                    "<b>ceil((placement + merge + newHigh) x combo x 0.25) x stage x globalIncome</b>",
                    HowToPlayBlockStyle.Formula),
                new HowToPlayBlock(
                    "Rétegugrások",
                    "A szakasz (stage) szorzó induláskor 1x, Agents után 5x, Modifiers után 24x. Ezért egy új réteg feloldása fontosabb, mint még egy sima bevételnövelő fejlesztés.")),
            new(
                "Algoritmusok",
                new HowToPlayBlock(
                    "Feladatuk",
                    "Az algoritmusok (solverek) feladata, hogy automatikusan játszanak helyetted.\n\n" +
                    "Ha az Auto megoldás be van kapcsolva, akkor a kiválasztott solver rakja le a blokkokat. Ha nincs kiválasztott solver, a run Manuális marad."),
                new HowToPlayBlock(
                    "Priorizációk",
                    "Minden solver más értékeket priorizálva dönt, ezért érdemes összehasonlítani őket.\n\n" +
                    "Működésük a Tuning feloldásával hangolható. A hangolás paraméterei skálákon állíthatóak, hatásuk azonban a játékmenetre negatív is lehet.\n\n" +
                    "A solverek között a döntési mélység mellett a másik legnagyobb különbség a sebességük. A sebesség a játék több pontján is fejleszthető."),
                new HowToPlayBlock(
                    "Sebességképlet",
                    "Általános:\n" +
                    "<b>moveInterval = max(minInterval, speedInterval x solverPacing x computePacing x agentPacing x trainingPacing)</b>\n\n" +
                    "Nehéz kereső solvereknek:\n" +
                    "<b>computePacing = max(0.35, 1 - 0.11 x ComputeSpeedLevel)</b>",
                    HowToPlayBlockStyle.Formula),
                new HowToPlayBlock(
                    "PPO",
                    "A végső cél az, hogy feloldd a PPO-t, ami egy tanuló algoritmus. Eleinte gyenge, mert nem érti a játékszabályokat, ezért Tréning Módban szükséges tanítani.\n\n" +
                    "Később a Prestige és a permanens fejlődés kapuja lesz.\n\n" +
                    "<b>Feloldásának feltételei csak a játék későbbi szakaszában válnak teljesíthetővé.</b>",
                    true)),
            new(
                "Fejlesztések",
                new HowToPlayBlock(
                    "Feladatuk",
                    "Ez a játék korai szakaszának fő <sprite name=\"chips\" tint=1> költési helye.\n\n" +
                    "Itt oldható fel az automatizálás, itt lesz gyorsítható és bővíthető a játékmenet, valamint itt növelhető a bevétel termelése.\n\n" +
                    "<b>Néhány fejlesztés önmagában nem bír nagy hatással, más fejlesztésekkel kombinálva azonban felerősödnek.</b>"),
                new HowToPlayBlock(
                    "Tipp",
                    "Elsőként célszerű az automatizációs fejlesztéseket megvásárolni, hogy mihamarabb a menedzseléssel foglalkozhass.",
                    true),
                new HowToPlayBlock(
                    "",
                    "<b>Fontos, hogy az Auto újraindítás <sprite name=\"token\" tint=1>-t használ, nélküle nem üzemképes.</b>",
                    true),
                new HowToPlayBlock(
                    "Fő Szorzók",
                    "<b>globalIncome = ChipYield x YieldTheory x MarketBots x TokenDividend</b>\n\n" +
                    "<b>runEnd = ceil((scoreBonus + moveBonus + speedBonus) x (1 + 0.15 x ProfitableEndingLevel) x 0.25)</b>\n\n" +
                    "A szaksz és globális bevételszorzók a run végi alap kiszámítása után kerülnek rá.",
                    HowToPlayBlockStyle.Formula)),
            new(
                "Ügynökök",
                new HowToPlayBlock(
                    "",
                    "<b>Az Ügynökök feloldása 5x stage szorzóra emeli a játékot.</b>",
                    true),
                new HowToPlayBlock(
                    "Feladatuk",
                    "Az Ügynökök aktív loadout bónuszokat adnak.\n\n" +
                    "Egy Ügynök megvásárlása nem elegendő ahhoz, hogy hatása aktív legyen. Ahhoz ki is kell választani. Alapértelmezettként egyszerre 2 Ügynök választható ki, amely később fejlesztéssel növelhető."),
                new HowToPlayBlock(
                    "",
                    "<b>A slotokba felszerelt kombináció határozza meg, milyen irányba válik hatékonyabbá a run, mert a legtöbb Ügynök egy konkrét jutalomforrást javít, nem az egész gazdaságot szorozza.</b>",
                    true),
                new HowToPlayBlock(
                    "",
                    "Néhány Ügynök önmagában nem bír nagy hatással, más Ügynökökkel és fejlesztésekkel kombinálva azonban felerősödnek."),
                new HowToPlayBlock(
                    "Szinergia formula",
                    "<b>agentSynergy = 1 + 0.40 x AgentSynergyLevel</b>",
                    HowToPlayBlockStyle.Formula),
                new HowToPlayBlock(
                    "Tipp",
                    "Az alapján válassz Ügynököt, ami éppen korlátoz. Ha a run túl hamar véget ér, stabilitást keress. Ha már elég hosszú, akkor a pontszám, high-block vagy merge jutalom erősebb lesz.",
                    true)),
            new(
                "Módosítók",
                new HowToPlayBlock(
                    "",
                    "<b>A Módosítók feloldása 24x stage szorzóra emeli a játékot.</b>",
                    true),
                new HowToPlayBlock(
                    "Feladatuk",
                    "A Módosítók a jövőbeli runok szabályait változtatják meg. Magasabb plafont, rugalmasabb boardot és több kihasználható eszközt adnak a fejlettebb solvereknek."),
                new HowToPlayBlock(
                    "Felhasználásuk",
                    "Egyes Módosítók a játékmenet részeként szabadon felhasználható akciókat adnak. Minden Módosító használható manuális módban is."),
                new HowToPlayBlock(
                    "Jutalomszabályok",
                    "A Catalyst Stack duplázza a merge jutalmakat.\n\n" +
                    "<b>salvage = ceil(score x salvageShare x 0.25) x stage x globalIncome</b>\n\n" +
                    "A hosszabb, rugalmasabb runok miatt a tervező solverek és a PPO is értékesebbé válnak.",
                    HowToPlayBlockStyle.Formula)),
            new(
                "Kutatás",
                new HowToPlayBlock(
                    "Feladatuk",
                    "A Kutatások a későbbi végigjátszások gyorsításáért és mélyebbítéséért felelnek, valamint itt válik elérhetővé a Prestige Reset.\n\n" +
                    "Itt tudsz játékmenetet könnyítő permanens fejlesztéseket vásárolni, amelyhez azonban <sprite name=\"insight\" tint=1>-ra van szükséged.\n\n"),
                new HowToPlayBlock(
                    "",
                    "A <sprite name=\"insight\" tint=1> a PPO Normál Módban történő játszatással termelődik.\n\n" +
                    "<b>Ahhoz, hogy a <sprite name=\"insight\" tint=1>-ot megkapd, Prestige Resetelned kell.</b>",
                    true),
                new HowToPlayBlock(
                    "Prestige Reset",
                    "A Prestige Reset során elveszik minden megszerzett <sprite name=\"chips\" tint=1>, <sprite name=\"token\" tint=1>, Fejlesztés, Ügynök és Módosító.\n\n" +
                    "Azonban megmarad az <sprite name=\"insight\" tint=1>, a megvett Kutatás, a permanens PPO tudás és az Adatközpont progressz.",
                    true),
                new HowToPlayBlock(
                    "<sprite name=\"insight\" tint=1> jutalom formula",
                    "<b><sprite name=\"insight\" tint=1> ~= round((1 + performance x 1.25 + cycleCarry) x usage x insightMultiplier)</b>\n\n" +
                    "A performance a PPO pontszámból és highest blockból jön. A usage Normál Mód runokból és frame-ekből.",
                    HowToPlayBlockStyle.Formula)),
            new(
                "Adatközpont",
                new HowToPlayBlock(
                    "Feladatuk",
                    "Az Adatközpont egy permanens compute réteg, amely 5. Prestige Reset után oldódik fel.\n\n" +
                    "Minden itt található fejlesztés a játékmenet különböző szakaszainak felgyorsítását teszi lehetővé.\n\n" +
                    "Itt ismét <sprite name=\"chips\" tint=1> a valuta, így a PPO Normál Módban felhalmozott termelés Prestige Reset esetén innentől kezdve nem vész kárba."),
                new HowToPlayBlock(
                    "",
                    "<b>Fontos, hogy a felhalmozott <sprite name=\"chips\" tint=1>-eket még Prestige Reset előtt költsd el, különben nullázódnak.</b>",
                    true),
                new HowToPlayBlock(
                    "Allokációk",
                    "Szerverállványokat és Létesítményfejlesztéseket vásárolhatsz, az így szerzett kapacitást pedig tetszőleges elosztással passzív PPO tanulásra, passzív <sprite name=\"insight\" tint=1> termelésre vagy <sprite name=\"chips\" tint=1> szorzóra allokálhatod.",
                    true),
                new HowToPlayBlock(
                    "Fő formula",
                    "<b>cost = baseCost x costGrowth^owned</b>\n\n" +
                    "<b>unitGF/s = baseGF/s x facilityBonus x interconnectBonus x prestigeBonus</b>",
                    HowToPlayBlockStyle.Formula),
                new HowToPlayBlock(
                    "Allokációs formulák",
                    "<b>Training frames/sec = totalGF/s x trainingAllocation x 0.60</b>\n" +
                    "<b>Analysis Insight/sec = totalGF/s x analysisAllocation x 0.0012 x fatigue</b>\n" +
                    "<b>Market income multiplier = 1 + 0.20 x log10(1 + marketAllocatedGF/s)</b>",
                    HowToPlayBlockStyle.Formula))
        };
    }
}
