using TMPro;
using UnityEngine;

namespace StackMerge
{
    [AddComponentMenu("Stack Merge/Goal Row")]
    public sealed class StackMergeGoalRow : MonoBehaviour
    {
        [Tooltip("Shows the goal description.")]
        public TMP_Text goalText;

        [Tooltip("Shows progress or \"Completed\" when the goal is done.")]
        public TMP_Text progressText;

        [Tooltip("Optional. Shows the goal's unlock reward; stays hidden for goals without one.")]
        public TMP_Text rewardText;
    }
}