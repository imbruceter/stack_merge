using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackMerge
{
    /// <summary>
    /// One slider-style parameter row in the Solver Tuning panel. Add this to your
    /// "Tune Parameter Slider" prefab's root and drag the name/value/description texts
    /// and the Slider into the slots in the Inspector. The code fills it per parameter.
    /// </summary>
    [AddComponentMenu("Stack Merge/Tune Slider Row")]
    public sealed class StackMergeTuneSliderRow : MonoBehaviour
    {
        [Tooltip("Parameter name.")]
        public TMP_Text nameText;
        [Tooltip("Current value, formatted.")]
        public TMP_Text valueText;
        [Tooltip("Parameter description.")]
        public TMP_Text descriptionText;
        public Slider slider;
    }
}
