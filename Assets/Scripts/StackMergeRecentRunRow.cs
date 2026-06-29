using TMPro;
using UnityEngine;

namespace StackMerge
{
    /// <summary>
    /// One row in the History → Recent Runs list. Add this to your Recent Runs row prefab's
    /// root and drag the six TMP texts into the slots in the Inspector. The code fills them
    /// in per completed run.
    /// </summary>
    [AddComponentMenu("Stack Merge/Recent Run Row")]
    public sealed class StackMergeRecentRunRow : MonoBehaviour
    {
        [Tooltip("Run index, e.g. \"#42\".")]
        public TMP_Text runText;
        public TMP_Text solverText;
        public TMP_Text scoreText;
        public TMP_Text movesText;
        public TMP_Text mergesText;
        public TMP_Text highText;
    }
}
