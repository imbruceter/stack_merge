using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackMerge
{
    /// <summary>
    /// One static agent card in the Agents menu (not instantiated at runtime — one already exists
    /// per agent in the Hierarchy, same idea as the Algorithms cards). Assign the AgentId this card
    /// represents and drag its Name/Description texts and its Button into the slots. The button's
    /// own child TMP text reads Buy price → "Select" → "Deselect" depending on ownership/equip state.
    /// </summary>
    [AddComponentMenu("Stack Merge/Agent Card")]
    public sealed class StackMergeAgentCard : MonoBehaviour
    {
        [Tooltip("Which agent this card represents.")]
        public AgentId agentId;

        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public Button button;
    }
}
