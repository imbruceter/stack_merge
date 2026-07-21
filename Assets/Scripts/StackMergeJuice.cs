using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StackMerge
{
    /// <summary>
    /// Runtime "juice" service: punch/shake/stamp tweens and screen flashes.
    ///
    /// Everything is built procedurally, so this needs no prefabs, no sprites and no scene rebuild — add
    /// the component (or let the bootstrap add it) and it works.
    ///
    /// Design rule learned the hard way: the late game runs at 60-100 solver moves per SECOND, so nothing
    /// here may be driven per move. Every effect below belongs to a rare, discrete event (a run ending, an
    /// all-time record, a prestige). Continuous feedback is handled by the bootstrap instead, as a decaying
    /// colour envelope on the feedback line under the board — that degrades gracefully at any move rate.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StackMergeJuice : MonoBehaviour
    {
        public static StackMergeJuice Instance { get; private set; }

        [Tooltip("Master switch. Turning this off makes every effect a no-op without touching call sites.")]
        [SerializeField] private bool effectsEnabled = true;

        private Canvas hostCanvas;
        private readonly List<PunchTween> punches = new();
        private readonly List<ShakeTween> shakes = new();
        private readonly List<StampTween> stamps = new();
        private Image flashOverlay;
        private float flashTimer;
        private float flashDuration;
        private Color flashColor;

        private sealed class StampTween
        {
            public RectTransform target;
            public Vector3 baseScale;
            public float baseRotation;
            public float startScale;
            public float startRotation;
            public float timer;
            public float duration;
        }

        private sealed class PunchTween
        {
            public RectTransform target;
            public Vector3 baseScale;
            public float timer;
            public float duration;
            public float strength;
        }

        private sealed class ShakeTween
        {
            public RectTransform target;
            public Vector2 basePosition;
            public float timer;
            public float duration;
            public float strength;
            public float seed;
        }

        /// <summary>True once a canvas is known, i.e. effects can actually be drawn.</summary>
        public bool Ready => hostCanvas != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Points the service at the UI canvas. Call once from the bootstrap after the scene references
        /// resolve; safe to call again if the canvas changes.
        /// </summary>
        public void Initialize(Canvas canvas)
        {
            hostCanvas = canvas;
            if (hostCanvas == null)
            {
                return;
            }

            EnsureFlashOverlay();
        }

        // ------------------------------------------------------------------------------------------
        // Effects
        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// Slams a panel onto the screen like a rubber stamp: it drops in oversized and slightly rotated,
        /// overshoots past its resting size and settles. Used for the Game Over modal, where the impact
        /// should read as the run being stamped "finished" rather than as the board being disturbed.
        /// </summary>
        public void Stamp(RectTransform target, float startScale = 2.1f, float startRotation = -7f, float duration = 0.32f)
        {
            if (!effectsEnabled || target == null)
            {
                return;
            }

            for (int i = 0; i < stamps.Count; i++)
            {
                if (stamps[i].target == target)
                {
                    // Restart in place, keeping the ORIGINAL resting transform: re-reading it mid-tween
                    // would bake the animated scale in as the new resting size.
                    stamps[i].timer = 0f;
                    stamps[i].duration = duration;
                    stamps[i].startScale = startScale;
                    stamps[i].startRotation = startRotation;
                    return;
                }
            }

            stamps.Add(new StampTween
            {
                target = target,
                baseScale = target.localScale,
                baseRotation = target.localEulerAngles.z,
                startScale = startScale,
                startRotation = startRotation,
                timer = 0f,
                duration = duration
            });
        }

        /// <summary>Scale-punches a transform (overshoot then settle). Re-punching restarts the tween.</summary>
        public void Punch(RectTransform target, float strength = 0.18f, float duration = 0.26f)
        {
            if (!effectsEnabled || target == null)
            {
                return;
            }

            for (int i = 0; i < punches.Count; i++)
            {
                if (punches[i].target == target)
                {
                    punches[i].timer = 0f;
                    punches[i].duration = duration;
                    punches[i].strength = strength;
                    return;
                }
            }

            punches.Add(new PunchTween
            {
                target = target,
                baseScale = target.localScale,
                timer = 0f,
                duration = duration,
                strength = strength
            });
        }

        /// <summary>Shakes a transform around its current anchored position (game over, big events).</summary>
        public void Shake(RectTransform target, float strength = 10f, float duration = 0.32f)
        {
            if (!effectsEnabled || target == null)
            {
                return;
            }

            for (int i = 0; i < shakes.Count; i++)
            {
                if (shakes[i].target == target)
                {
                    shakes[i].timer = 0f;
                    shakes[i].duration = duration;
                    shakes[i].strength = strength;
                    return;
                }
            }

            shakes.Add(new ShakeTween
            {
                target = target,
                basePosition = target.anchoredPosition,
                timer = 0f,
                duration = duration,
                strength = strength,
                seed = UnityEngine.Random.value * 100f
            });
        }

        /// <summary>Full-screen colour flash that fades out. Used for new highest blocks and prestige.</summary>
        public void Flash(Color color, float duration = 0.35f)
        {
            if (!effectsEnabled || !Ready || flashOverlay == null)
            {
                return;
            }

            flashColor = color;
            flashDuration = Mathf.Max(0.01f, duration);
            flashTimer = flashDuration;
            flashOverlay.gameObject.SetActive(true);
            flashOverlay.rectTransform.SetAsLastSibling();
        }

        // ------------------------------------------------------------------------------------------
        // Tween ticking
        // ------------------------------------------------------------------------------------------

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            TickStamps(dt);
            TickPunches(dt);
            TickShakes(dt);
            TickFlash(dt);
        }

        private void TickStamps(float dt)
        {
            for (int i = stamps.Count - 1; i >= 0; i--)
            {
                StampTween tween = stamps[i];
                if (tween.target == null)
                {
                    stamps.RemoveAt(i);
                    continue;
                }

                tween.timer += dt;
                float t = tween.timer / tween.duration;
                if (t >= 1f)
                {
                    tween.target.localScale = tween.baseScale;
                    tween.target.localEulerAngles = new Vector3(0f, 0f, tween.baseRotation);
                    stamps.RemoveAt(i);
                    continue;
                }

                // Two-phase curve. Phase 1 (first 55%): a hard quartic ease-IN from oversized down to
                // slightly UNDER the resting size — accelerating downward is what sells the weight of a
                // stamp being pressed. Phase 2: a damped bounce back up to rest.
                float scaleFactor;
                float rotation;
                if (t < 0.55f)
                {
                    float p = t / 0.55f;
                    float eased = p * p * p * p;
                    scaleFactor = Mathf.Lerp(tween.startScale, 0.94f, eased);
                    rotation = Mathf.Lerp(tween.startRotation, 0f, eased);
                }
                else
                {
                    float p = (t - 0.55f) / 0.45f;
                    // Damped sine settle: 0.94 -> slight overshoot -> 1.
                    scaleFactor = 1f - 0.06f * Mathf.Cos(p * Mathf.PI * 1.5f) * (1f - p);
                    rotation = 0f;
                }

                tween.target.localScale = tween.baseScale * scaleFactor;
                tween.target.localEulerAngles = new Vector3(0f, 0f, tween.baseRotation + rotation);
            }
        }

        private void TickPunches(float dt)
        {
            for (int i = punches.Count - 1; i >= 0; i--)
            {
                PunchTween tween = punches[i];
                if (tween.target == null)
                {
                    punches.RemoveAt(i);
                    continue;
                }

                tween.timer += dt;
                float t = tween.timer / tween.duration;
                if (t >= 1f)
                {
                    tween.target.localScale = tween.baseScale;
                    punches.RemoveAt(i);
                    continue;
                }

                // Damped sine: a sharp overshoot that settles, i.e. the classic "punch" curve.
                float amount = tween.strength * Mathf.Sin(t * Mathf.PI * 1.5f) * (1f - t);
                tween.target.localScale = tween.baseScale * (1f + amount);
            }
        }

        private void TickShakes(float dt)
        {
            for (int i = shakes.Count - 1; i >= 0; i--)
            {
                ShakeTween tween = shakes[i];
                if (tween.target == null)
                {
                    shakes.RemoveAt(i);
                    continue;
                }

                tween.timer += dt;
                float t = tween.timer / tween.duration;
                if (t >= 1f)
                {
                    tween.target.anchoredPosition = tween.basePosition;
                    shakes.RemoveAt(i);
                    continue;
                }

                float falloff = 1f - t;
                float time = tween.seed + tween.timer * 34f;
                float x = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f;
                float y = (Mathf.PerlinNoise(0f, time) - 0.5f) * 2f;
                tween.target.anchoredPosition = tween.basePosition + new Vector2(x, y) * (tween.strength * falloff);
            }
        }

        private void TickFlash(float dt)
        {
            if (flashOverlay == null || flashTimer <= 0f)
            {
                return;
            }

            flashTimer -= dt;
            if (flashTimer <= 0f)
            {
                flashOverlay.gameObject.SetActive(false);
                return;
            }

            Color c = flashColor;
            // Squared falloff: bright instantly, gone quickly — a flash, not a fade.
            float t = flashTimer / flashDuration;
            c.a = flashColor.a * t * t;
            flashOverlay.color = c;
        }

        // ------------------------------------------------------------------------------------------
        // Construction
        // ------------------------------------------------------------------------------------------

        private void EnsureFlashOverlay()
        {
            if (flashOverlay != null || hostCanvas == null)
            {
                return;
            }

            GameObject go = new("JuiceFlash", typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(hostCanvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            flashOverlay = go.AddComponent<Image>();
            flashOverlay.raycastTarget = false;
            flashOverlay.color = new Color(1f, 1f, 1f, 0f);
            go.SetActive(false);
        }
    }
}
