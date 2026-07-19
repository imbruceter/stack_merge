using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackMerge
{
    [AddComponentMenu("Stack Merge/How To Play Element")]
    public sealed class StackMergeHowToPlayElement : MonoBehaviour
    {
        [Tooltip("Title text. Auto-found from a child named Name, NameText or Title when left empty.")]
        public TMP_Text nameText;

        [Tooltip("Description text. Auto-found from a child named Desc, DescText or Description when left empty.")]
        public TMP_Text descText;

        [Tooltip("Optional fallback for legacy Unity UI Text title fields.")]
        public Text legacyNameText;

        [Tooltip("Optional fallback for legacy Unity UI Text description fields.")]
        public Text legacyDescText;

        [Tooltip("Hide the title Text GameObject when the title is empty.")]
        [SerializeField] private bool hideTitleWhenEmpty = true;

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        public void SetContent(string title, string description)
        {
            ResolveReferences();
            SetText(nameText, legacyNameText, title, hideTitleWhenEmpty);
            SetText(descText, legacyDescText, description);
        }

        private void ResolveReferences()
        {
            nameText ??= FindTmpText("Name", "NameText", "Title", "TitleText");
            descText ??= FindTmpText("Desc", "DescText", "Description", "DescriptionText");
            legacyNameText ??= FindLegacyText("Name", "NameText", "Title", "TitleText");
            legacyDescText ??= FindLegacyText("Desc", "DescText", "Description", "DescriptionText");
        }

        private TMP_Text FindTmpText(params string[] names)
        {
            foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
            {
                if (HasName(text.gameObject.name, names))
                {
                    return text;
                }
            }

            return null;
        }

        private Text FindLegacyText(params string[] names)
        {
            foreach (Text text in GetComponentsInChildren<Text>(true))
            {
                if (HasName(text.gameObject.name, names))
                {
                    return text;
                }
            }

            return null;
        }

        private static bool HasName(string candidate, params string[] names)
        {
            string normalized = Normalize(candidate);
            foreach (string name in names)
            {
                if (normalized == Normalize(name))
                {
                    return true;
                }
            }

            return false;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static void SetText(TMP_Text tmpText, Text legacyText, string value, bool hideWhenEmpty = false)
        {
            value = StackMergeSpriteTags.ApplyTint(value ?? string.Empty);
            bool show = !hideWhenEmpty || !string.IsNullOrWhiteSpace(value);

            if (tmpText != null)
            {
                tmpText.gameObject.SetActive(show);
                tmpText.text = value;
            }

            if (legacyText != null)
            {
                legacyText.gameObject.SetActive(show);
                legacyText.text = value;
            }
        }
    }
}
