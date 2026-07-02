using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackMerge
{
    /// <summary>
    /// One static research node in the Research tree grid (not instantiated at runtime — one
    /// already exists per research in the Hierarchy). The card itself is the Button: clicking it
    /// opens the Selected Research popup instead of buying directly. Assign the ResearchId this
    /// card represents and drag its Name/Level/Cost-Info texts and its Button into the slots.
    /// </summary>
    [AddComponentMenu("Stack Merge/Research Card")]
    public sealed class StackMergeResearchCard : MonoBehaviour
    {
        [Tooltip("Which research this card represents.")]
        public ResearchId researchId;

        public TMP_Text nameText;

        [Tooltip("Shows the research's current level, e.g. \"2/5\".")]
        public TMP_Text levelText;

        [Tooltip("Shows the cost to buy the next level, or \"Maxed\"/\"Locked\".")]
        public TMP_Text costText;

        [Tooltip("The card itself acts as the button — clicking it opens the Selected Research popup.")]
        public Button button;
    }
}
