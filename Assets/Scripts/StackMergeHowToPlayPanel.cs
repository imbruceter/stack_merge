using System;
using System.Collections.Generic;
using UnityEngine;
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
        Datacenter
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

        [Tooltip("Prefab shown instead of the normal element while the related layer is still locked.")]
        [SerializeField] private GameObject lockedElementPrefab;

        [Tooltip("Optional pre-placed elements. If assigned, these are filled instead of instantiating the prefab.")]
        [SerializeField] private StackMergeHowToPlayElement[] sceneElements = Array.Empty<StackMergeHowToPlayElement>();

        [Header("Behaviour")]
        [SerializeField] private bool hidePanelOnAwake = true;
        [SerializeField] private bool rebuildOnOpen = true;

        [Header("Locked Copy")]
        [SerializeField, TextArea] private string lockedDescriptionEnglish = "This part of the game is still locked. Keep progressing to reveal this help section.";
        [SerializeField, TextArea] private string lockedDescriptionHungarian = "Ez a jatekreteg meg nincs feloldva. Haladj tovabb, hogy megnyiljon ez a sugo szekcio.";

        private readonly List<StackMergeHowToPlayElement> runtimeElements = new();
        private StackMergeLanguage builtForLanguage = (StackMergeLanguage)(-1);
        private bool built;

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
        }

        public void Open()
        {
            ResolveReferences();
            BuildIfNeeded(rebuildOnOpen);
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
        }

        private void BuildIfNeeded(bool force)
        {
            StackMergeLanguage language = StackMergeLocalization.CurrentLanguage;
            if (built && !force && builtForLanguage == language)
            {
                return;
            }

            HowToPlaySection[] sections = GetSections(language);
            if (sceneElements != null && sceneElements.Length > 0)
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
            HideTemplateIfChild(lockedElementPrefab);

            for (int i = 0; i < sections.Count; i++)
            {
                StackMergeHowToPlayElement element = CreateRuntimeElement(IsSectionUnlocked(i));
                element.gameObject.SetActive(true);
                SetElementContent(element, sections[i], i);
                runtimeElements.Add(element);
            }
        }

        private StackMergeHowToPlayElement CreateRuntimeElement(bool unlocked)
        {
            if (unlocked || lockedElementPrefab == null)
            {
                return Instantiate(elementPrefab, contentRoot);
            }

            return CreateLockedElement(contentRoot, -1);
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

            return gameBootstrap == null || gameBootstrap.IsHowToPlayLayerUnlocked(layer);
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
            {
                Title = title;
                Description = description;
            }

            public string Title { get; }
            public string Description { get; }
        }

        private static readonly HowToPlaySection[] EnglishSections =
        {
            new(
                "Gameplay",
                "The gameplay is as simple as it gets. Place the blocks into one of the four stacks. Merge, keep at least one legal action available, and push the run as far as possible.\n\n" +

                "Score is based on placed block value + every created merge value.\n\n" +

                "Producing chips <sprite name=\"chips\" tint=1> start from three parts:\n" +
                "<b>placement = 2</b>\n" +
                "<b>merge = mergeCount x resultingTop x highMultiplier x 0.55</b>\n" +
                "<b>newHigh = highestBlock x highMultiplier x 0.85</b>, only when a merge reaches a new high\n\n" +

                "Final <sprite name=\"chips\" tint=1> formula:\n" +
                "<b>ceil((placement + merge + newHigh) x 0.25) x stage multiplier x income multiplier</b>\n\n" +

                "Difficulty spawns use a safe ceiling and paired opportunities:\n" +
                "<b>regularWeight(tier) = 0.56^tier x (1 + 0.025 x ScalingFrequencyLevel x tier)</b>\n" +
                "<b>highPairChance = min(16%, 1.2% x DifficultyLevel + 0.8% x ScalingFrequencyLevel)</b>"),
            new(
                "Algorithms",
                "Algorithms (solvers) decide how the game is played when Auto Solve is on. It is about choosing a decision style, comparing solvers, and tuning owned solvers once tuning is unlocked.\n\n" +

                "Simple solvers are fast but narrow. Planning and simulation solvers spend more time evaluating future states.\n\n" +

                "<u>PPO is different from the other solvers. It doesn't know the game at first, so it needs to be trained in Training Mode before it can be used in Normal Mode.</u>\n\n" +

                "Solver timing:\n" +
                "<b>moveInterval = max(minInterval, speedInterval x solverPacing x computePacing x agentPacing x trainingPacing)</b>\n\n" +

                "Compute Speed only changes computePacing for heavy search solvers:\n" +
                "<b>computePacing = max(0.35, 1 - 0.11 x ComputeSpeedLevel)</b>\n\n" +

                "Tuning does not directly add income. It changes how a solver scores moves, so it can trade greed, safety, queue planning and combo setup."),
            new(
                "Upgrades",
                "Upgrades are the <sprite name=\"chips\" tint=1> based progression layer. They unlock automation, make runs larger, unlock new layers, and multiply the income earned by normal play.\n\n" +

                "Main income multiplier:\n" +
                "<b>incomeMultiplier = (1 + ChipYieldBonus) x YieldTheoryMultiplier x MarketBotsMultiplier</b>\n\n" +

                "Run-end <sprite name=\"chips\" tint=1> :\n" +
                "<b>runEnd = ceil((scoreBonus + moveBonus + speedBonus) x (1 + 0.15 x ProfitableEndingLevel) x 0.25)</b>, then stage and income multipliers apply.\n\n" +

                "Passive Production:\n" +
                "<b>baseTick = PassiveYield table value</b>\n" +
                "<b>activeTick = ceil(baseTick x (1 + ActiveMultiplierBonus))</b> while actively* playing\n" +
                "*Actively means a placement happened in the last 4 seconds.\n\n" +

                "Tick interval is improved by Passive Tick Rate.\n\n" +

                "Difficulty Scaling raises the maximum paired tier that can appear. Scaling Frequency makes those paired high-tier opportunities appear more often and nudges them toward higher tiers."),
            new(
                "Agents",
                "Agents are a loadout system. You own many, but only the equipped slots are active, so this layer is about choosing which part of your economy or automation you want to emphasize.\n\n" +

                "Most Agent bonuses affect one reward component instead of everything, which makes combinations matter.\n\n" +

                "Agent Synergy from Research scales the bonus part of Agents:\n" +
                "<b>agentSynergy = 1 + 0.25 x AgentSynergyLevel</b>\n\n" +

                "Examples:\n" +
                "Merge income with Merge Broker = <b>baseMerge x (1 + 0.75 x agentSynergy)</b>\n" +
                "New-high reward with Highwater Analyst = <b>baseHigh x (1 + 2.00 x agentSynergy)</b>\n" +
                "Run-end score reward with Score Auditor = <b>baseScore x (1 + 2.00 x agentSynergy)</b>\n" +
                "Overclocker changes solver interval to 75% of normal."),
            new(
                "Modifiers",
                "Modifiers change the rules of future runs. They make the board more flexible, more complex, and more valuable for advanced solvers.\n\n" +

                "Modifier levels are converted into run rules."),
            new(
                "Research",
                "Research is the permanent prestige layer. It makes future resets faster, preserves selected progress, improves PPO, and increases Insight <sprite name=\"chips\" tint=1> generation.\n\n" +

                "PPO Normal Mode requirement:\n" +
                "<b>requiredFrames = max(250000, round(500000 x max(0.5, 1 - 0.08 x PpoBootcampLevel)))</b>\n\n" +

                "Prestige <sprite name=\"chips\" tint=1> is based on PPO Normal Mode performance:\n" +
                "<b>Insight ~= round((1 + performance x 1.25 + cycleCarry) x usage x insightMultiplier)</b>\n\n" +

                "<b>Performance</b> comes from score and highest block. <b>Usage</b> comes from Normal Mode runs and trained frames. <b>CycleCarry</b> rewards <sprite name=\"chips\" tint=1> already earned in the cycle, but fatigue prevents runaway farming."),
            new(
                "Datacenter",
                "Datacenter is the permanent compute layer after multiple prestiges. It turns late-game <sprite name=\"chips\" tint=1> into background PPO progress, passive <sprite name=\"insight\" tint=1> and a <sprite name=\"chips\" tint=1> income multiplier.\n\n" +

                "Rack cost:\n" +
                "<b>cost = baseCost x costGrowth^owned</b>\n\n" +

                "Rack output:\n" +
                "<b>unitGF/s = baseGF/s x facilityBonus x interconnectBonus x prestigeBonus</b>\n" +
                "<b>facilityBonus = 1 + 0.06 x PowerGridLevel + 0.04 x CoolingLoopLevel</b>\n" +
                "<b>interconnectBonus = 1 + 0.05 x FabricInterconnectLevel for TPU Pod and Neural Fabric</b>\n" +
                "<b>prestigeBonus = 1 + 0.05 x PrestigeCount</b>\n\n" +

                "Allocation effects:\n" +
                "<b>Training frames/sec = totalGF/s x trainingAllocation x 0.50</b>\n" +
                "<b>Analysis Insight/sec = totalGF/s x analysisAllocation x 0.002 x fatigue</b>\n" +
                "<b>Market income multiplier = 1 + 0.25 x log10(1 + marketAllocatedGF/s)</b>")
        };

        private static readonly HowToPlaySection[] HungarianSections =
        {
            new(
                "Játékmenet",
                "A játékmenet a lehető legegyszerűbb. Helyezd a blokkokat a négy kupac egyikébe. Egyesítsd (merge) őket, ügyelj arra, hogy mindig legyen legalább szabad mozgástér, és húzd a lehető legtovább a runokat.\n\n" +

                "A pontszám (score) a lehelyezett blokk értéke + minden létrehozott egyesítési érték alapján kerül kiszámításra.\n\n" +

                "A chipek <sprite name=\"chips\" tint=1> termelése három részből áll:\n" +
                "<b>placement = 2</b>\n" +
                "<b>merge = mergeCount x resultingTop x highMultiplier x 0.55</b>\n" +
                "<b>newHigh = highestBlock x highMultiplier x 0.85</b>, csak új high merge esetén\n\n" +


                "Végső <sprite name=\"chips\" tint=1> képlet:\n" +
                "<b>ceil((placement + merge + newHigh) x 0.25) x stage multiplier x income multiplier</b>\n\n" +

                "A Difficulty spawn biztonságos plafont és párosított lehetőségeket használ:\n" +
                "<b>regularWeight(tier) = 0.56^tier x (1 + 0.025 x ScalingFrequencyLevel x tier)</b>\n" +
                "<b>highPairChance = min(16%, 1.2% x DifficultyLevel + 0.8% x ScalingFrequencyLevel)</b>"),
            new(
                "Algoritmusok",
                "Az algoritmusok (solverek) döntik el, hogyan játszik a játék Auto Megoldás mellett. Ez a réteg a döntési stílus kiválasztásáról, solverek összehasonlításáról és a megvett solverek hangolásáról szól.\n\n" +

                "Az egyszerű solverek gyorsak, de szűklátókörűek. A tervező és szimulációs solverek több jövőbeli állapotot értékelnek.\n\n" +

                "<u>A PPO különbözik a többi solvertől. Eleinte nem ismeri a játékot, ezért előbb Tréning Módban kell betanítani, mielőtt Normál Módban használható lenne.</u>\n\n" +

                "Solver időzítés:\n" +
                "<b>moveInterval = max(minInterval, speedInterval x solverPacing x computePacing x agentPacing x trainingPacing)</b>\n\n" +

                "A Compute Speed csak a nehéz kereső solverek computePacing értékét módosítja:\n" +
                "<b>computePacing = max(0.35, 1 - 0.11 x ComputeSpeedLevel)</b>\n\n" +

                "A tuning közvetlenül nem közvetlen bevételt. A solver lépéspontozását módosítja, így állítható a greed (mohóság), safety (biztonság), queue planning (sortervezés) és combo setup aránya."),
            new(
                "Fejlesztések",
                "Az Upgrades a <sprite name=\"chips\" tint=1>-ből vásárolt fejlődési réteg. Automatizációt nyit, nagyobb runokat tesz lehetővé, későbbi rétegeket old fel, és megszorozza a normál játékból szerezhető bevételt.\n\n" +

                "Fő bevétel szorzó:\n" +
                "<b>incomeMultiplier = (1 + ChipYieldBonus) x YieldTheoryMultiplier x MarketBotsMultiplier</b>\n\n" +

                "Run végi <sprite name=\"chips\" tint=1> :\n" +
                "<b>runEnd = ceil((scoreBonus + moveBonus + speedBonus) x (1 + 0.15 x ProfitableEndingLevel) x 0.25)</b>, majd szakasz (stage) és bevétel szorzók jönnek rá.\n\n" +

                "Passive Production:\n" +
                "<b>baseTick = PassiveYield table value</b>\n" +
                "<b>activeTick = ceil(baseTick x (1 + ActiveMultiplierBonus)) aktív* játék közben</b>\n" +
                "*Aktívnak az számít, ha az elmúlt 4 másodpercben volt lerakás.\n\n" +

                "A tick gyakoriságát a Passive Tick Rate javítja.\n\n" +

                "A Difficulty Scaling azt módosítja, mekkora párosított high-tier lehetőség jelenhet meg. A Scaling Frequency gyakoribbá teszi ezeket a high-tier párokat, és magasabb tierek felé tolja őket."),
            new(
                "Ügynökök",
                "Az Ügynökök (Agents) egy loadout rendszer. Több Agentet is birtokolhatsz, de csak az aktív slotok számítanak, ezért itt azt választod ki, melyik gazdasági vagy automatizációs részt akarod erősíteni.\n\n" +

                "A legtöbb Agent nem mindent szoroz, hanem egy konkrét jutalomrészt, emiatt a kombináció számít.\n\n" +

                "A Researchben lévő Agent Synergy az Agentek bónusz részét skálázza:\n" +
                "<b>agentSynergy = 1 + 0.25 x AgentSynergyLevel</b>\n\n" +

                "Példák:\n" +
                "Merge Broker mellett merge bevétel = <b>baseMerge x (1 + 0.75 x agentSynergy)</b>\n" +
                "Highwater Analyst mellett high jutalom = <b>baseHigh x (1 + 2.00 x agentSynergy)</b>\n" +
                "Score Auditor mellett run végi score jutalom = <b>baseScore x (1 + 2.00 x agentSynergy)</b>\n" +
                "Overclocker a solver intervallumot a normál 75%-ára állítja."),
            new(
                "Módifikációk",
                "A Modifierek a jövőbeli runok szabályait változtatják meg. Rugalmasabbá, összetettebbé és a fejlettebb solverek számára értékesebbé teszik a boardot.\n\n" +

                "A Modifier szintek új run indításakor alakulnak run szabályokká."),
            new(
                "Kutatás",
                "A Research egy tartós prestige réteg. Gyorsabbá teszi a későbbi reseteket, megőriz bizonyos progresszt, javítja a PPO-t, és növeli az <sprite name=\"chips\" tint=1> termelést.\n\n" +

                "PPO Normál Mód követelmény:\n" +
                "<b>requiredFrames = max(250000, round(500000 x max(0.5, 1 - 0.08 x PpoBootcampLevel)))</b>\n\n" +

                "A Prestige <sprite name=\"chips\" tint=1> a PPO Normál Mód teljesítményére épül:\n" +
                "<b>Insight ~= round((1 + performance x 1.25 + cycleCarry) x usage x insightMultiplier)</b>\n\n" +

                "A <b>performance</b> score-ból és highest blockból jön. A <b>usage</b> Normál Mód runokból és trained frame-ekből. A <b>cycleCarry</b> jutalmazza a ciklusban már szerzett <sprite name=\"chips\" tint=1>-ot, de fatigue meggátolja a végtelen farmolást."),
            new(
                "Adatközpont",
                "A Datacenter több prestige után nyíló tartós compute réteg. Játék végi <sprite name=\"chips\" tint=1>-et alakít háttér PPO progresszé, passzív <sprite name=\"insight\" tint=1>-tá és <sprite name=\"chips\" tint=1> bevétel szorzóvá.\n\n" +

                "Rack ár:\n" +
                "<b>cost = baseCost x costGrowth^owned</b>\n\n" +

                "Rack output:\n" +
                "<b>unitGF/s = baseGF/s x facilityBonus x interconnectBonus x prestigeBonus</b>\n" +
                "<b>facilityBonus = 1 + 0.06 x PowerGridLevel + 0.04 x CoolingLoopLevel</b>\n" +
                "<b>interconnectBonus = 1 + 0.05 x FabricInterconnectLevel TPU Pod és Neural Fabric esetén</b>\n" +
                "<b>prestigeBonus = 1 + 0.05 x PrestigeCount</b>\n\n" +

                "Allocation hatások:\n" +
                "<b>Training frames/sec = totalGF/s x trainingAllocation x 0.50</b>\n" +
                "<b>Analysis Insight/sec = totalGF/s x analysisAllocation x 0.002 x fatigue</b>\n" +
                "<b>Market income multiplier = 1 + 0.25 x log10(1 + marketAllocatedGF/s)</b>")
        };
    }
}
