using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StackMerge
{
    [AddComponentMenu("Stack Merge/Help Overlay Trigger")]
    public sealed class StackMergeHelpOverlayTrigger : MonoBehaviour
    {
        private const string DefaultFirstLaunchPrefsKey = "StackMerge.HelpOverlay.FirstLaunchSeen";

        [Header("Scene References")]
        [SerializeField] private StackMergeHelpOverlay overlay;
        [SerializeField] private Button triggerButton;

        [Header("Content")]
        [SerializeField] private string title = "Welcome to the game!";
        [SerializeField, TextArea(3, 8)] private string body = "Before you start playing, it is highly recommended to visit How To Play (Settings), as it describes all the gameplay elements.";
        [SerializeField] private string actionButtonLabel;
        [SerializeField] private UnityEvent actionButtonAction;

        [Header("Magyar Overrides")]
        [SerializeField] private string magyarTitle;
        [SerializeField, TextArea(3, 8)] private string magyarBody;
        [SerializeField] private string magyarActionButtonLabel;

        [Header("Trigger")]
        [SerializeField] private bool showOnStart;
        [SerializeField] private bool showOnce;
        [SerializeField] private string playerPrefsKey = DefaultFirstLaunchPrefsKey;

        private void Awake()
        {
            ResolveReferences();
            WireButton();
        }

        private void Start()
        {
            if (showOnStart)
            {
                Show();
            }
        }

        private void OnDestroy()
        {
            if (triggerButton != null)
            {
                triggerButton.onClick.RemoveListener(Show);
            }
        }

        public void Show()
        {
            ResolveReferences();
            if (overlay == null || HasAlreadyShown())
            {
                return;
            }

            UnityAction action = HasActionButton() ? actionButtonAction.Invoke : null;
            overlay.Show(
                Localize(title, magyarTitle),
                Localize(body, magyarBody),
                Localize(actionButtonLabel, magyarActionButtonLabel),
                action);
            MarkShown();
        }

        public void ResetShownFlag()
        {
            if (!string.IsNullOrWhiteSpace(playerPrefsKey))
            {
                PlayerPrefs.DeleteKey(playerPrefsKey);
            }
        }

        private void ResolveReferences()
        {
            triggerButton ??= GetComponent<Button>();
            overlay ??= FindLoadedOverlay();
        }

        private void WireButton()
        {
            if (triggerButton == null)
            {
                return;
            }

            triggerButton.onClick.RemoveListener(Show);
            triggerButton.onClick.AddListener(Show);
        }

        private bool HasAlreadyShown()
        {
            return showOnce
                && !string.IsNullOrWhiteSpace(playerPrefsKey)
                && PlayerPrefs.GetInt(playerPrefsKey, 0) == 1;
        }

        private void MarkShown()
        {
            if (!showOnce || string.IsNullOrWhiteSpace(playerPrefsKey))
            {
                return;
            }

            PlayerPrefs.SetInt(playerPrefsKey, 1);
            PlayerPrefs.Save();
        }

        private bool HasActionButton()
        {
            return !string.IsNullOrWhiteSpace(Localize(actionButtonLabel, magyarActionButtonLabel))
                && actionButtonAction != null
                && actionButtonAction.GetPersistentEventCount() > 0;
        }

        private static string Localize(string english, string magyar)
        {
            if (StackMergeLocalization.CurrentLanguage == StackMergeLanguage.Magyar && !string.IsNullOrWhiteSpace(magyar))
            {
                return magyar;
            }

            return StackMergeLocalization.Translate(english ?? string.Empty);
        }

        private static StackMergeHelpOverlay FindLoadedOverlay()
        {
            foreach (StackMergeHelpOverlay candidate in Resources.FindObjectsOfTypeAll<StackMergeHelpOverlay>())
            {
                if (candidate != null && candidate.gameObject.scene.IsValid())
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
