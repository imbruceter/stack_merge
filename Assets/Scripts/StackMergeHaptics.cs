using UnityEngine;

namespace StackMerge
{
    /// <summary>
    /// Haptic (vibration) patterns. Deliberately few and clearly ranked by strength so the whole feel
    /// stays restrained — over-buzzing is the one way haptics reads as cheap.
    /// </summary>
    public enum StackMergeHapticPattern
    {
        /// <summary>Barely-there tick. UI selections.</summary>
        Selection,

        /// <summary>Soft tap. A manual block placement.</summary>
        Light,

        /// <summary>Firmer tap. A manual merge.</summary>
        Medium,

        /// <summary>Strong single buzz. An all-time record.</summary>
        Heavy,

        /// <summary>Two quick pulses. A goal / achievement.</summary>
        DoublePulse,

        /// <summary>A short escalating roll. The prestige moment — the biggest event in the game.</summary>
        Prestige
    }

    /// <summary>
    /// Central haptics service, built to mirror <see cref="StackMergeAudio"/>: add it to any GameObject
    /// (or let the bootstrap create one) and call <see cref="Play"/> from anywhere. It is a silent no-op
    /// on platforms without a vibrator, in the Editor, and whenever the player turns it off.
    ///
    /// On Android it drives the system Vibrator directly (amplitude-controlled on API 26+, so a "light"
    /// tick genuinely feels lighter than a "heavy" one). A global minimum interval keeps rapid events
    /// from turning into a continuous buzz — the deliberate restraint the design calls for.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StackMergeHaptics : MonoBehaviour
    {
        public const string EnabledKey = "StackMerge.Settings.Haptics";

        // No two haptics closer together than this. Vibration motors also physically cannot restart
        // instantly, so this is both a taste choice and a hardware reality.
        private const float MinIntervalSeconds = 0.045f;

        public static StackMergeHaptics Instance { get; private set; }

        [Tooltip("Master switch. Persisted to PlayerPrefs; the Settings toggle drives it.")]
        [SerializeField] private bool hapticsEnabled = true;

        [Tooltip("Global strength scale (0-1) applied to every pattern's amplitude. Lower it if the whole " +
                 "device feels too strong.")]
        [Range(0f, 1f)]
        [SerializeField] private float strength = 1f;

        private bool loaded;
        private float lastPlayTime = -999f;
        private bool supported;
        private bool amplitudeControl;

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject vibrator;
        private AndroidJavaClass vibrationEffectClass;
        private int apiLevel;
#endif

        /// <summary>Whether haptics are turned on (and the device can vibrate).</summary>
        public bool Enabled
        {
            get { EnsureLoaded(); return hapticsEnabled; }
            set
            {
                EnsureLoaded();
                if (hapticsEnabled == value)
                {
                    return;
                }

                hapticsEnabled = value;
                PlayerPrefs.SetInt(EnabledKey, value ? 1 : 0);
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureLoaded();
            InitDevice();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>Fires a haptic pattern. Safe from anywhere; silent when disabled/unsupported.</summary>
        public static void Play(StackMergeHapticPattern pattern)
        {
            Instance?.PlayInternal(pattern);
        }

        private void PlayInternal(StackMergeHapticPattern pattern)
        {
            EnsureLoaded();
            if (!hapticsEnabled || !supported || strength <= 0.01f)
            {
                return;
            }

            float now = Time.unscaledTime;
            if (now - lastPlayTime < MinIntervalSeconds)
            {
                return;
            }

            lastPlayTime = now;

            switch (pattern)
            {
                case StackMergeHapticPattern.Selection:
                    OneShot(8, 45);
                    break;
                case StackMergeHapticPattern.Light:
                    OneShot(12, 90);
                    break;
                case StackMergeHapticPattern.Medium:
                    OneShot(20, 150);
                    break;
                case StackMergeHapticPattern.Heavy:
                    OneShot(40, 230);
                    break;
                case StackMergeHapticPattern.DoublePulse:
                    // ~585ms celebratory "rhythm" for a goal/achievement. A vibration motor can't do
                    // pitch, but it can do RHYTHM and intensity: three quick rising taps, a short breath,
                    // then a strong sustained finish — reads as a little rewarding fanfare rather than a
                    // buzz. (timings alternate wait/vibrate; amplitudes pair with them.)
                    Waveform(
                        new long[] { 0, 45, 55, 45, 55, 45, 100, 240 },
                        new int[] { 0, 140, 0, 180, 0, 215, 0, 245 });
                    break;
                case StackMergeHapticPattern.Prestige:
                    // ~350ms, solid and rewarding. A short-long-short-LONG cadence so the biggest moment
                    // in the game feels deliberate and celebratory without being drawn out.
                    Waveform(
                        new long[] { 0, 50, 40, 110, 50, 100 },
                        new int[] { 0, 150, 0, 210, 0, 245 });
                    break;
            }
        }

        // ------------------------------------------------------------------------------------------
        // Device layer
        // ------------------------------------------------------------------------------------------

        private void InitDevice()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }

                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    apiLevel = version.GetStatic<int>("SDK_INT");
                }

                supported = vibrator != null && vibrator.Call<bool>("hasVibrator");
                if (apiLevel >= 26)
                {
                    vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
                    amplitudeControl = supported && vibrator.Call<bool>("hasAmplitudeControl");
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"StackMerge: haptics unavailable — {exception.Message}");
                supported = false;
            }
#else
            // Editor / desktop: no device. Kept non-supported so everything stays a clean no-op.
            supported = false;
#endif
        }

        private void OneShot(long milliseconds, int amplitude)
        {
            amplitude = Mathf.Clamp(Mathf.RoundToInt(amplitude * strength), 1, 255);

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (apiLevel >= 26 && vibrationEffectClass != null)
                {
                    int amp = amplitudeControl ? amplitude : -1; // -1 = device default amplitude
                    using var effect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, amp);
                    vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", milliseconds);
                }
            }
            catch
            {
                FallbackVibrate();
            }
#endif
        }

        private void Waveform(long[] timings, int[] amplitudes)
        {
            if (strength < 1f)
            {
                for (int i = 0; i < amplitudes.Length; i++)
                {
                    if (amplitudes[i] > 0)
                    {
                        amplitudes[i] = Mathf.Clamp(Mathf.RoundToInt(amplitudes[i] * strength), 1, 255);
                    }
                }
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (apiLevel >= 26 && vibrationEffectClass != null)
                {
                    int[] amps = amplitudeControl ? amplitudes : null;
                    using var effect = amps != null
                        ? vibrationEffectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amps, -1)
                        : vibrationEffectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, -1);
                    vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", timings, -1);
                }
            }
            catch
            {
                FallbackVibrate();
            }
#endif
        }

        // Genuine last resort AND the reason Unity auto-adds the VIBRATE permission at build time:
        // referencing Handheld.Vibrate() anywhere pulls the permission in, so no AndroidManifest edit
        // is needed. It is a coarse ~500ms buzz, so it only runs if the fine-grained path threw.
        private static void FallbackVibrate()
        {
            Handheld.Vibrate();
        }

        private void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            loaded = true;
            hapticsEnabled = PlayerPrefs.GetInt(EnabledKey, 1) == 1;
        }
    }
}
