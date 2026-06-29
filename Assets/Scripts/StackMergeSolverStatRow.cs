using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackMerge
{
    /// <summary>
    /// One row in the History → Solvers list. Add this to your Solvers row prefab's root
    /// and drag the five TMP texts (and the optional "i" Button) into the slots in the
    /// Inspector. The code fills them in per solver.
    /// </summary>
    [AddComponentMenu("Stack Merge/Solver Stat Row")]
    public sealed class StackMergeSolverStatRow : MonoBehaviour
    {
        public TMP_Text solverText;
        public TMP_Text runsText;
        public TMP_Text medianText;
        public TMP_Text bestText;
        public TMP_Text highText;

        [Tooltip("Optional. The 'i' button that opens the solver info modal. Leave empty if the row has none.")]
        public Button infoButton;
    }
}
