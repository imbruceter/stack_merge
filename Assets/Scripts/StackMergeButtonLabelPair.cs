using TMPro;
using UnityEngine;

namespace StackMerge
{
    /// <summary>
    /// Attach to any upgrade Button that has a NameText, DescText, EffectText, and Cost/InfoText child (the redesigned
    /// Upgrades menu layout). The Bootstrap looks this component up on the Button itself
    /// (GetComponent) and writes the name/cost into the two texts separately, instead of the old
    /// single combined "Name\nCost" label on the Button's own text.
    /// </summary>
    [AddComponentMenu("Stack Merge/Button Label Pair")]
    public sealed class StackMergeButtonLabelPair : MonoBehaviour
    {
        [Tooltip("The upgrade's display name (for leveled upgrades this usually includes the current level, e.g. \"Faster solver (3/10)\").")]
        public TMP_Text nameText;

        [Tooltip("Optional description text for the upgrade. Leave empty if this button has no description line.")]
        public TMP_Text descText;

        [Tooltip("Optional current-to-next effect text, e.g. \"+44% >> +66%\". Leave empty if this button has no effect line.")]
        public TMP_Text effectText;

        [Tooltip("The cost to buy the next level, or a requirement/status message (\"Needs algorithm\", \"Maxed\") when it can't be bought.")]
        public TMP_Text costText;
    }
}
