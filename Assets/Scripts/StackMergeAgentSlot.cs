using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackMerge
{
    /// <summary>
    /// One equipped-agent slot display in the Agents menu. Not instantiated at runtime — one
    /// already exists per slot in the Hierarchy. Assign the 0-based slot index (0 = Slot 1, 1 =
    /// Slot 2, ...) and drag its two texts in; the Bootstrap fills SlotText with the slot's own
    /// name and NameText with whichever agent is currently equipped there (or empty/locked state).
    /// </summary>
    [AddComponentMenu("Stack Merge/Agent Slot")]
    public sealed class StackMergeAgentSlot : MonoBehaviour
    {
        [Tooltip("0-based slot index: 0 = Slot 1, 1 = Slot 2, etc.")]
        public int slotIndex;

        [Tooltip("Shows the slot's own label, e.g. \"Slot 1\".")]
        public TMP_Text slotText;

        [Tooltip("Shows the equipped agent's name, or \"Empty\" / \"Locked\".")]
        public TMP_Text nameText;

        [Tooltip("Optional. Shows the equipped agent's icon. The sprite comes from the Bootstrap's Agent " +
                 "Icons list. The Bootstrap hides this Image entirely while the slot is empty or locked, " +
                 "so an empty slot never shows a stale or placeholder graphic.")]
        public Image iconImage;

        [Tooltip("Optional. The transform the equip/unequip animation plays on. Leave empty to animate " +
                 "the slot root itself; assign an inner card if the root is a layout cell that should " +
                 "not move.")]
        public RectTransform animationTarget;

        /// <summary>The transform the Bootstrap animates when this slot's agent changes.</summary>
        public RectTransform AnimationTarget => animationTarget != null ? animationTarget : (RectTransform)transform;
    }
}
