using System;
using System.Collections.Generic;
using UnityEngine;

namespace StackMerge
{
    [CreateAssetMenu(menuName = "Stack Merge/Localization Table", fileName = "StackMergeLocalizationTable")]
    public sealed class StackMergeLocalizationTable : ScriptableObject
    {
        [SerializeField] private StackMergeLanguage language = StackMergeLanguage.Magyar;
        [SerializeField] private List<Entry> entries = new();

        public StackMergeLanguage Language => language;

        public void Register()
        {
            if (language == StackMergeLanguage.English || entries == null)
            {
                return;
            }

            var translations = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (Entry entry in entries)
            {
                if (string.IsNullOrEmpty(entry.key))
                {
                    continue;
                }

                translations[entry.key] = entry.value ?? string.Empty;
            }

            StackMergeLocalization.RegisterTranslations(language, translations);
        }

        [Serializable]
        public struct Entry
        {
            [TextArea(1, 5)] public string key;
            [TextArea(1, 8)] public string value;
        }
    }
}
