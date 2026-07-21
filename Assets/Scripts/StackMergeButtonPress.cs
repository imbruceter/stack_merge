using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace StackMerge
{
    /// <summary>
    /// Subtle press feedback for a UI Button: the button dips slightly while held and springs back on
    /// release. Deliberately understated — the goal is only that buttons stop feeling dead, not that they
    /// draw attention.
    ///
    /// Attached automatically to every Button by the bootstrap's feedback sweep, so nothing has to be
    /// wired per button. It composes with the Button's own colour transition rather than replacing it,
    /// and it only touches localScale, which UI layout groups ignore.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class StackMergeButtonPress : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("Scale while held. 0.96 is a dip you feel more than see — that is the intent.")]
        [Range(0.8f, 1f)]
        [SerializeField] private float pressedScale = 0.96f;

        [Tooltip("Seconds to reach the pressed scale. Must be fast, or taps end before the dip is visible.")]
        [Min(0.01f)]
        [SerializeField] private float pressSeconds = 0.06f;

        [Tooltip("Seconds to spring back to rest.")]
        [Min(0.01f)]
        [SerializeField] private float releaseSeconds = 0.13f;

        private Button button;
        private RectTransform rect;
        private Vector3 restScale = Vector3.one;
        private bool restScaleCaptured;
        private bool held;
        private float progress;

        private void Awake()
        {
            button = GetComponent<Button>();
            rect = (RectTransform)transform;
            CaptureRestScale();
        }

        private void OnEnable()
        {
            // Panels are shown and hidden constantly; a button re-enabled mid-press must start at rest.
            held = false;
            progress = 0f;
            if (restScaleCaptured && rect != null)
            {
                rect.localScale = restScale;
            }
        }

        private void OnDisable()
        {
            // Never leave a button parked at its pressed size — that scale would then be captured as the
            // new "rest" the next time this component initialises.
            held = false;
            progress = 0f;
            if (restScaleCaptured && rect != null)
            {
                rect.localScale = restScale;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button == null || !button.IsInteractable())
            {
                return;
            }

            CaptureRestScale();
            held = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            held = false;
        }

        private void Update()
        {
            if (rect == null)
            {
                return;
            }

            float target = held ? 1f : 0f;
            if (Mathf.Approximately(progress, target))
            {
                return;
            }

            float duration = held ? pressSeconds : releaseSeconds;
            progress = Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime / duration);

            // Release overshoots a touch past rest before settling, which is what makes the button feel
            // sprung rather than merely resized. The press direction stays linear — it should feel firm.
            float eased = held
                ? progress
                : progress * progress * (3f - 2f * progress);
            float scale = Mathf.Lerp(1f, pressedScale, eased);
            if (!held && progress > 0f && progress < 0.5f)
            {
                scale += (1f - pressedScale) * 0.35f * Mathf.Sin(progress * Mathf.PI * 2f);
            }

            rect.localScale = restScale * scale;

            if (Mathf.Approximately(progress, 0f))
            {
                rect.localScale = restScale;
            }
        }

        private void CaptureRestScale()
        {
            if (restScaleCaptured || rect == null)
            {
                return;
            }

            restScale = rect.localScale;
            restScaleCaptured = true;
        }
    }
}
