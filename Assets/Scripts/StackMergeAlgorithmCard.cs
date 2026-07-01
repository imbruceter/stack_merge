using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackMerge
{
    /// <summary>
    /// One static algorithm card in the Algorithms menu (an "AlgorithmItem" prefab instance placed
    /// by hand into its category row). Unlike the row prefabs elsewhere, these are NOT instantiated
    /// at runtime — one already exists per solver in the Hierarchy. Assign the SolverId this
    /// specific card represents, drag its Name/Description texts and its two buttons into the
    /// slots, and the Bootstrap drives all of them from progression state every refresh.
    /// </summary>
    [AddComponentMenu("Stack Merge/Algorithm Card")]
    public sealed class StackMergeAlgorithmCard : MonoBehaviour
    {
        [Tooltip("Which solver this card represents.")]
        public SolverId solverId;

        public TMP_Text nameText;
        public TMP_Text descriptionText;

        [Tooltip("The card's single action button — shows Buy / Select / Deselect depending on state.")]
        public Button actionButton;

        [Tooltip("Opens this solver's tuning panel. Automatically hidden for solvers with no tunable parameters (e.g. RAND).")]
        public Button tuneButton;
    }
}
