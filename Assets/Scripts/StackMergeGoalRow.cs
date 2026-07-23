using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackMerge
{
    [AddComponentMenu("Stack Merge/Goal Row")]
    public sealed class StackMergeGoalRow : MonoBehaviour
    {
        [Tooltip("Shows the goal description.")]
        public TMP_Text goalText;

        [Tooltip("Shows progress or \"Completed\" when the goal is done.")]
        public TMP_Text progressText;

        [Tooltip("Optional. Used by secret goals to reveal the full description only after completion.")]
        public TMP_Text descText;

        [Tooltip("Optional. Shows the goal's unlock reward; stays hidden for goals without one.")]
        public TMP_Text rewardText;

        [Tooltip("Optional. The coloured plate behind the icon. The Bootstrap tints it green once the " +
                 "goal is complete and grey while it is not. Auto-found by the name 'IconBackground'.")]
        public Image iconBackground;

        [Tooltip("Optional. The goal's icon. Auto-found by the name 'Icon'. Hidden when no sprite is " +
                 "mapped for this goal.")]
        public Image iconImage;
    }
}
