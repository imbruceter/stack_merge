using TMPro;
using UnityEngine;

namespace StackMerge
{
    /// <summary>
    /// One row in the Goals (Achievements) list. Add this to your Goals row prefab's root
    /// and drag the two TMP texts into the slots in the Inspector. The code fills them in
    /// per goal — no hardcoded table, no rebuild needed.
    /// </summary>
    [AddComponentMenu("Stack Merge/Goal Row")]
    public sealed class StackMergeGoalRow : MonoBehaviour
    {
        [Tooltip("Shows the goal description.")]
        public TMP_Text goalText;

        [Tooltip("Shows progress (e.g. \"3 / 10\"), or \"Completed\" when the goal is done.")]
        public TMP_Text progressText;
    }
}
