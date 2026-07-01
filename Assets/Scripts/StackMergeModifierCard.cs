using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackMerge
{
    /// <summary>
    /// One static modifier card in the Modifiers menu (not instantiated at runtime — one already
    /// exists per modifier in the Hierarchy, same idea as the Algorithms cards). Assign the
    /// ModifierId this card represents and drag its Name/Description texts and its Button into the
    /// slots; the Bootstrap drives all of it from progression state every refresh. The cost/status
    /// ("Maxed", "Locked", price) is written to the Button's own child TMP text.
    /// </summary>
    [AddComponentMenu("Stack Merge/Modifier Card")]
    public sealed class StackMergeModifierCard : MonoBehaviour
    {
        [Tooltip("Which modifier this card represents.")]
        public ModifierId modifierId;

        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public Button button;
    }
}
