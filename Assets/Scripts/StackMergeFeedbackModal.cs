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
            Vector2 home = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
            Vector2 start = home + Vector2.down * 12f;
            Vector2 end = home + Vector2.up * Mathf.Max(0f, riseDistance);
            Vector3 baseScale = transform.localScale;
            float duration = Mathf.Max(0.05f, totalSeconds);
            float fadeIn = Mathf.Min(fadeInSeconds, duration * 0.35f);
            float fadeOut = Mathf.Min(fadeOutSeconds, duration * 0.45f);

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, end, EaseOutCubic(progress));
                }

                transform.localScale = baseScale * GetPopScale(progress);

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

            transform.localScale = baseScale;
            Finished?.Invoke(this);
            Destroy(gameObject);
        }

        private static float CalculateAlpha(float elapsed, float duration, float fadeIn, float fadeOut)
        {
            if (elapsed < fadeIn)
            {
                return EaseOutCubic(elapsed / Mathf.Max(0.001f, fadeIn));
            }

            float fadeOutStart = Mathf.Max(fadeIn, duration - fadeOut);
            if (elapsed > fadeOutStart)
            {
                return 1f - EaseInCubic((elapsed - fadeOutStart) / Mathf.Max(0.001f, fadeOut));
            }

            return 1f;
        }

        private static float GetPopScale(float progress)
        {
            if (progress < 0.18f)
            {
                return Mathf.LerpUnclamped(0.96f, 1.035f, EaseOutBack(progress / 0.18f));
            }

            if (progress < 0.34f)
            {
                return Mathf.Lerp(1.035f, 1f, EaseOutCubic((progress - 0.18f) / 0.16f));
            }

            return 1f;
        }

        private static float EaseOutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            float inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseInCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value;
        }

        private static float EaseOutBack(float value)
        {
            value = Mathf.Clamp01(value);
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float shifted = value - 1f;
            return 1f + c3 * shifted * shifted * shifted + c1 * shifted * shifted;
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrEmpty(value)
                ? string.Empty
                : value.Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        }
    }
}
