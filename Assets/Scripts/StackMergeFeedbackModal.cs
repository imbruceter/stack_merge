using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackMerge
{
    [AddComponentMenu("Stack Merge/Feedback Modal")]
    public sealed class StackMergeFeedbackModal : MonoBehaviour
    {
        [Tooltip("Text shown in the toast. Auto-found from a child named StatusText when left empty.")]
        [SerializeField] private TMP_Text statusText;

        [Tooltip("Optional CanvasGroup used for fade in/out. Added at runtime when missing.")]
        [SerializeField] private CanvasGroup canvasGroup;

        [SerializeField, Min(0.01f)] private float fadeInSeconds = 0.12f;
        [SerializeField, Min(0.01f)] private float fadeOutSeconds = 0.22f;

        private Coroutine animationCoroutine;

        public event Action<StackMergeFeedbackModal> Finished;

        private void Reset()
        {
            ResolveReferences();
        }

        private void OnValidate()
        {
            ResolveReferences();
        }

        public void Play(string status, float totalSeconds, float riseDistance, bool anchorBottom, Vector2 bottomOffset)
        {
            ResolveReferences();
            SetStatusText(status);

            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform != null && anchorBottom)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rectTransform.anchorMax = new Vector2(0.5f, 0f);
                rectTransform.pivot = new Vector2(0.5f, 0f);
                rectTransform.anchoredPosition = bottomOffset;
            }

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            if (animationCoroutine != null)
            {
                StopCoroutine(animationCoroutine);
            }

            animationCoroutine = StartCoroutine(Animate(totalSeconds, riseDistance));
        }

        private void ResolveReferences()
        {
            canvasGroup ??= GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (statusText == null)
            {
                foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
                {
                    if (Normalize(text.gameObject.name) == "statustext")
                    {
                        statusText = text;
                        break;
                    }
                }
            }
        }

        private void SetStatusText(string status)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = StackMergeSpriteTags.ApplyTint(StackMergeLocalization.Translate(status ?? string.Empty));
            statusText.SetVerticesDirty();
            statusText.SetLayoutDirty();

            RectTransform textRect = statusText.rectTransform;
            LayoutRebuilder.MarkLayoutForRebuild(textRect);
            if (textRect.parent is RectTransform parent)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
            }
        }

        private IEnumerator Animate(float totalSeconds, float riseDistance)
        {
            RectTransform rectTransform = transform as RectTransform;
            Vector2 start = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
            Vector2 end = start + Vector2.up * Mathf.Max(0f, riseDistance);
            float duration = Mathf.Max(0.05f, totalSeconds);
            float fadeIn = Mathf.Min(fadeInSeconds, duration * 0.35f);
            float fadeOut = Mathf.Min(fadeOutSeconds, duration * 0.45f);

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, end, Smooth(progress));
                }

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = CalculateAlpha(elapsed, duration, fadeIn, fadeOut);
                }

                yield return null;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            Finished?.Invoke(this);
            Destroy(gameObject);
        }

        private static float CalculateAlpha(float elapsed, float duration, float fadeIn, float fadeOut)
        {
            if (elapsed < fadeIn)
            {
                return Mathf.Clamp01(elapsed / Mathf.Max(0.001f, fadeIn));
            }

            float fadeOutStart = Mathf.Max(fadeIn, duration - fadeOut);
            if (elapsed > fadeOutStart)
            {
                return 1f - Mathf.Clamp01((elapsed - fadeOutStart) / Mathf.Max(0.001f, fadeOut));
            }

            return 1f;
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
