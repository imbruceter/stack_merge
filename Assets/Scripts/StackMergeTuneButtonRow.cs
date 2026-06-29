using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackMerge
{
    /// <summary>
    /// One button-style parameter row in the Solver Tuning panel (small whole-number
    /// parameters). Add this to your "Tune Parameter Button" prefab's root and drag the
    /// name/value/description texts and the single Button into the slots. The code clones
    /// that one button once per selectable value.
    /// </summary>
    [AddComponentMenu("Stack Merge/Tune Button Row")]
    public sealed class StackMergeTuneButtonRow : MonoBehaviour
    {
        [Tooltip("Parameter name.")]
        public TMP_Text nameText;
        [Tooltip("Current value, formatted.")]
        public TMP_Text valueText;
        [Tooltip("Parameter description.")]
        public TMP_Text descriptionText;

        [Tooltip("The single button in the prefab. It is cloned once per selectable value, " +
                 "inside its own parent. The active value's button is shown non-interactable.")]
        public Button buttonTemplate;
    }
}
