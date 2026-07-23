using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace StackMerge
{
    [AddComponentMenu("Stack Merge/Help Overlay")]
    public sealed class StackMergeHelpOverlay : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private GameObject overlayRoot;
        [Tooltip("The panel inside the overlay that actually gets animated. Auto-found by a name ending " +
                 "in \"Modal\". The overlay root itself is only a dimming backdrop and must NOT be animated.")]
        [SerializeField] private RectTransform modalRoot;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button actionButton;
        [SerializeField] private TMP_Text actionButtonText;

        [Header("Behaviour")]
        [SerializeField] private bool hideOnAwake = true;
        [SerializeField] private bool bringToFrontOnShow = true;

        private UnityAction currentAction;
        private bool showRequested;

        public event Action Hidden;

        public bool IsVisible => overlayRoot != null && overlayRoot.activeSelf;

        private void Awake()
        {
            ResolveReferences();
            WireButtons();
            ConfigureActionButton(null, null);

            if (hideOnAwake && !showRequested)
            {
                Hide();
            }
        }

        private void OnDestroy()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(Hide);
            }

            if (actionButton != null)
            {
                actionButton.onClick.RemoveListener(InvokeAction);
            }
        }

        public void Show(string title, string body)
        {
            Show(title, body, null, null);
        }

        public void Show(string title, string body, string actionLabel, UnityAction action)
        {
            showRequested = true;
            ResolveReferences();
            WireButtons();

            SetText(titleText, title);
            SetText(bodyText, body);
            ConfigureActionButton(actionLabel, action);

            SetVisible(true);
        }

        public void Hide()
        {
            showRequested = false;
            SetVisible(false);
            Hidden?.Invoke();
        }

        private void ResolveReferences()
        {
            overlayRoot ??= gameObject;

            if (overlayRoot == null)
            {
                return;
            }

            titleText ??= FindTmpText("TitleText", "Title", "HeaderText", "Header");
            bodyText ??= FindTmpText("BodyText", "Body", "InfoText", "DescriptionText", "Description");
            backButton ??= FindButton(null, "BackButton", "CloseButton", "Back", "Close");
            actionButton ??= FindButton(backButton, "ActionButton", "ConfirmButton", "Button");

            if (actionButton != null)
            {
                actionButtonText ??= FindTmpText(actionButton.transform, "Text", "Label", "ButtonText", "ActionText");
            }
        }

        private void WireButtons()
        {
            if (backButton != null)
            {
                backButton.onClick.RemoveListener(Hide);
                backButton.onClick.AddListener(Hide);
            }

            if (actionButton != null)
            {
                actionButton.onClick.RemoveListener(InvokeAction);
                actionButton.onClick.AddListener(InvokeAction);
            }
        }

        private void ConfigureActionButton(string actionLabel, UnityAction action)
        {
            currentAction = action;
            bool hasAction = actionButton != null && action != null && !string.IsNullOrWhiteSpace(actionLabel);
            if (actionButton == null)
            {
                return;
            }

            actionButton.gameObject.SetActive(hasAction);
            actionButton.interactable = hasAction;
            SetText(actionButtonText, hasAction ? actionLabel : string.Empty);
        }

        private void InvokeAction()
        {
            currentAction?.Invoke();
        }

        private void SetVisible(bool visible)
        {
            if (overlayRoot == null)
            {
                return;
            }

            bool wasVisible = overlayRoot.activeSelf;
            overlayRoot.SetActive(visible);
            if (visible && bringToFrontOnShow)
            {
                overlayRoot.transform.SetAsLastSibling();
            }

            if (visible && !wasVisible)
            {
                PlayEntranceAnimation();
            }
        }

        // Animates the inner modal, never the overlay root. The root here is only a dimming backdrop
        // ("Help Overlay" wrapping "Help Modal"), and scaling a full-screen dimmer reads as a glitch.
        private void PlayEntranceAnimation()
        {
            StackMergeJuice juice = StackMergeJuice.Instance;
            if (juice == null)
            {
                return;
            }

            RectTransform target = ResolveModalTarget();
            if (target != null)
            {
                juice.PopIn(target);
            }

            StackMergeAudio.Play(StackMergeSfx.UiPanelOpen);
        }

        private RectTransform ResolveModalTarget()
        {
            if (modalRoot != null)
            {
                return IsFullScreenStretch(modalRoot) ? null : modalRoot;
            }

            foreach (Transform child in overlayRoot.transform)
            {
                if (child is RectTransform rect && child.name.EndsWith("Modal", StringComparison.OrdinalIgnoreCase))
                {
                    modalRoot = rect;
                    return IsFullScreenStretch(rect) ? null : rect;
                }
            }

            // No separate modal child was found. Deliberately animate NOTHING rather than falling back
            // to the root: this overlay root is the full-screen black dimmer, and scaling it makes the
            // dim visibly fail to cover the screen for the duration of the animation.
            return null;
        }

        private static bool IsFullScreenStretch(RectTransform rect)
        {
            return rect != null
                && Mathf.Approximately(rect.anchorMin.x, 0f)
                && Mathf.Approximately(rect.anchorMin.y, 0f)
                && Mathf.Approximately(rect.anchorMax.x, 1f)
                && Mathf.Approximately(rect.anchorMax.y, 1f);
        }

        private TMP_Text FindTmpText(params string[] names)
        {
            return FindTmpText(overlayRoot.transform, names);
        }

        private static TMP_Text FindTmpText(Transform root, params string[] names)
        {
            if (root == null)
            {
                return null;
            }

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (HasName(text.gameObject.name, names))
                {
                    return text;
                }
            }

            return null;
        }

        private Button FindButton(Button excludedButton, params string[] names)
        {
            if (overlayRoot == null)
            {
                return null;
            }

            foreach (Button button in overlayRoot.GetComponentsInChildren<Button>(true))
            {
                if (button == excludedButton)
                {
                    continue;
                }

                if (HasName(button.gameObject.name, names))
                {
                    return button;
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

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = StackMergeLocalization.Translate(value ?? string.Empty);
            }
        }
    }
}
