using System;
using UnityEngine;

namespace StackMerge
{
    [DefaultExecutionOrder(-1000)]
    public sealed class StackMergeLocalizationSource : MonoBehaviour
    {
        [SerializeField] private StackMergeLocalizationTable[] tables = Array.Empty<StackMergeLocalizationTable>();

        private void Awake()
        {
            RegisterTables();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                RegisterTables();
            }
        }
#endif

        public void RegisterTables()
        {
            StackMergeLocalization.ClearRegisteredTranslations();

            if (tables == null)
            {
                return;
            }

            foreach (StackMergeLocalizationTable table in tables)
            {
                table?.Register();
            }
        }
    }
}
