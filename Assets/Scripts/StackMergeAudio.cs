using System;
using System.Collections.Generic;
using UnityEngine;

namespace StackMerge
{
    /// <summary>
    /// Every sound effect the game can fire. Each entry maps to a named serialized
    /// <see cref="StackMergeSoundEntry"/> field on <see cref="StackMergeAudio"/> — deliberately NOT to an
    /// index-aligned array, because reordering such an array in the Inspector silently remaps every clip
    /// (the same trap the Agents/Modifiers definition arrays carry). Named fields cannot be mis-ordered.
    /// </summary>
    public enum StackMergeSfx
    {
        // --- Gameplay --------------------------------------------------------------------
        BlockPlace,
        Merge,
        MergeChain,
        NewHigh,
        RunStart,
        GameOver,
        Pickaxe,
        QueueScrub,
        UnstableSave,
        JokerMerge,

        // --- UI --------------------------------------------------------------------------
        UiClick,
        UiTab,
        UiPanelOpen,
        UiPanelClose,
        Purchase,
        PurchaseDenied,

        // --- Progression -----------------------------------------------------------------
        UpgradeLevel,
        SolverSelect,
        AchievementUnlocked,
        Prestige,
        ResearchUnlocked,
        OfflineReward,
        TokenSpend,
        PpoUnlocked
    }

    /// <summary>
    /// One configurable sound. Drop one or more clips into <see cref="clips"/>; a random one is picked per
    /// play so repeated sounds (block placement, merges) do not become fatiguing. Leaving the list empty
    /// makes the sound a silent no-op — the game runs perfectly fine with no audio assigned at all.
    /// </summary>
    [Serializable]
    public sealed class StackMergeSoundEntry
    {
        [Tooltip("One or more interchangeable clips. A random one plays each time. Empty = silent.")]
        public AudioClip[] clips = Array.Empty<AudioClip>();

        [Tooltip("Per-sound volume, multiplied by the SFX and Master sliders.")]
        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Random pitch range. Small variation (0.95-1.05) keeps repeated sounds alive.")]
        [Range(0.1f, 3f)] public float pitchMin = 1f;

        [Range(0.1f, 3f)] public float pitchMax = 1f;

        [Tooltip("Minimum seconds between two plays of THIS sound. Critical for gameplay sounds: at max " +
                 "Solver Speed the game fires ~45 moves/second, which would machine-gun the audio.")]
        [Min(0f)] public float minInterval;

        [NonSerialized] public float lastPlayTime = -999f;

        public bool HasClip
        {
            get
            {
                if (clips == null)
                {
                    return false;
                }

                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] != null)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            if (clips.Length == 1)
            {
                return clips[0];
            }

            // Retry a few times so a single null hole in the array does not swallow the sound.
            for (int attempt = 0; attempt < 4; attempt++)
            {
                AudioClip candidate = clips[UnityEngine.Random.Range(0, clips.Length)];
                if (candidate != null)
                {
                    return candidate;
                }
            }

            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                {
                    return clips[i];
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Central audio service. Add this component to the same GameObject as
    /// <see cref="StackMergeGameBootstrap"/> and drag clips into the Inspector — nothing else is needed;
    /// the bootstrap finds it automatically and creates a silent one if it is missing.
    ///
    /// Design notes:
    ///  * A small pool of AudioSources plays the effects, so overlapping sounds never cut each other off.
    ///  * Per-sound cooldowns plus a per-frame voice budget keep the fast solver speeds (down to a 0.022s
    ///    move interval) and PPO Training bursts from turning into noise.
    ///  * Music and ambience get their own looping sources with independent volume.
    ///  * Volumes persist in PlayerPrefs and are exposed through <see cref="MasterVolume"/> etc. so the
    ///    Settings panel can drive them.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StackMergeAudio : MonoBehaviour
    {
        public const string MasterVolumeKey = "StackMerge.Settings.MasterVolume";
        public const string SfxVolumeKey = "StackMerge.Settings.SfxVolume";
        public const string MusicVolumeKey = "StackMerge.Settings.MusicVolume";

        private const int VoiceCount = 8;
        private const int MaxSfxPerFrame = 3;
        private const float MusicFadeSeconds = 1.2f;

        public static StackMergeAudio Instance { get; private set; }

        // --------------------------------------------------------------------------------------
        // Gameplay sounds. These fire from the solver loop, so their cooldowns matter most.
        // --------------------------------------------------------------------------------------
        [Header("Gameplay")]
        [Tooltip("A block lands without merging. The most frequent sound in the game — keep it short and soft.")]
        [SerializeField] private StackMergeSoundEntry blockPlace = new() { volume = 0.5f, pitchMin = 0.94f, pitchMax = 1.06f, minInterval = 0.05f };

        [Tooltip("A merge resolves. Pitch is raised by block tier automatically (see mergePitchSemitonesPerTier), " +
                 "so a single clip already produces a rising musical scale as the stack climbs.")]
        [SerializeField] private StackMergeSoundEntry merge = new() { volume = 0.8f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.04f };

        [Tooltip("Two or more merges resolved from one placement (a cascade). Layered on top of the merge sound.")]
        [SerializeField] private StackMergeSoundEntry mergeChain = new() { volume = 0.85f, pitchMin = 0.98f, pitchMax = 1.02f, minInterval = 0.08f };

        [Tooltip("A new highest block for this run. The run's payoff moment — this one may be loud.")]
        [SerializeField] private StackMergeSoundEntry newHigh = new() { volume = 1f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.15f };

        [SerializeField] private StackMergeSoundEntry runStart = new() { volume = 0.7f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.2f };

        [SerializeField] private StackMergeSoundEntry gameOver = new() { volume = 0.9f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.2f };

        [Header("Gameplay — Modifiers")]
        [SerializeField] private StackMergeSoundEntry pickaxe = new() { volume = 0.85f, pitchMin = 0.97f, pitchMax = 1.03f, minInterval = 0.08f };

        [SerializeField] private StackMergeSoundEntry queueScrub = new() { volume = 0.75f, pitchMin = 0.97f, pitchMax = 1.03f, minInterval = 0.08f };

        [Tooltip("Unstable Stack rescued the run by deleting a bottom block.")]
        [SerializeField] private StackMergeSoundEntry unstableSave = new() { volume = 0.9f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.1f };

        [SerializeField] private StackMergeSoundEntry jokerMerge = new() { volume = 0.9f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.08f };

        // --------------------------------------------------------------------------------------
        // UI sounds.
        // --------------------------------------------------------------------------------------
        [Header("UI")]
        [Tooltip("Generic button press. Auto-attached to every Button in the scene, including runtime-built cards.")]
        [SerializeField] private StackMergeSoundEntry uiClick = new() { volume = 0.45f, pitchMin = 0.97f, pitchMax = 1.03f, minInterval = 0.04f };

        [SerializeField] private StackMergeSoundEntry uiTab = new() { volume = 0.55f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.05f };

        [SerializeField] private StackMergeSoundEntry uiPanelOpen = new() { volume = 0.55f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.05f };

        [SerializeField] private StackMergeSoundEntry uiPanelClose = new() { volume = 0.5f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.05f };

        [Tooltip("Any successful purchase (upgrade, agent, modifier, research, rack...).")]
        [SerializeField] private StackMergeSoundEntry purchase = new() { volume = 0.8f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.05f };

        [Tooltip("Rejected purchase: not enough chips, or a locked prerequisite.")]
        [SerializeField] private StackMergeSoundEntry purchaseDenied = new() { volume = 0.6f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.1f };

        // --------------------------------------------------------------------------------------
        // Progression sounds — the milestone moments.
        // --------------------------------------------------------------------------------------
        [Header("Progression")]
        [Tooltip("An upgrade gained a level. Pitch rises with the level automatically (see upgradePitchSemitonesPerLevel).")]
        [SerializeField] private StackMergeSoundEntry upgradeLevel = new() { volume = 0.8f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.05f };

        [SerializeField] private StackMergeSoundEntry solverSelect = new() { volume = 0.7f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.08f };

        [SerializeField] private StackMergeSoundEntry achievementUnlocked = new() { volume = 1f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.25f };

        [Tooltip("Prestige reset executed. The biggest moment in the game — a long clip is appropriate.")]
        [SerializeField] private StackMergeSoundEntry prestige = new() { volume = 1f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.5f };

        [SerializeField] private StackMergeSoundEntry researchUnlocked = new() { volume = 0.85f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.1f };

        [SerializeField] private StackMergeSoundEntry offlineReward = new() { volume = 0.9f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.5f };

        [Tooltip("Tokens consumed by an auto restart.")]
        [SerializeField] private StackMergeSoundEntry tokenSpend = new() { volume = 0.4f, pitchMin = 0.96f, pitchMax = 1.04f, minInterval = 0.12f };

        [Tooltip("PPO finished training and Normal Mode became available.")]
        [SerializeField] private StackMergeSoundEntry ppoUnlocked = new() { volume = 1f, pitchMin = 1f, pitchMax = 1f, minInterval = 0.5f };

        // --------------------------------------------------------------------------------------
        // Loops.
        // --------------------------------------------------------------------------------------
        [Header("Music & Ambience")]
        [Tooltip("Background music loop. Left empty = no music.")]
        [SerializeField] private AudioClip musicLoop;

        [Range(0f, 1f)]
        [SerializeField] private float musicClipVolume = 0.5f;

        [Tooltip("Constant room tone (server-room hum fits the theme). Plays under everything, independent of music.")]
        [SerializeField] private AudioClip ambienceLoop;

        [Range(0f, 1f)]
        [SerializeField] private float ambienceClipVolume = 0.25f;

        [Tooltip("If set, ambience volume scales with Datacenter compute — the server room literally gets louder " +
                 "as the farm grows. Driven by the bootstrap via SetAmbienceIntensity.")]
        [SerializeField] private bool ambienceFollowsDatacenter = true;

        // --------------------------------------------------------------------------------------
        // Tuning.
        // --------------------------------------------------------------------------------------
        [Header("Musical Tuning")]
        [Tooltip("Semitones of pitch added per block tier on merges. 1.0 walks up a chromatic scale as the stack " +
                 "climbs; 0 disables the effect. This is what makes a single merge clip feel hand-authored.")]
        [Range(0f, 3f)]
        [SerializeField] private float mergePitchSemitonesPerTier = 1f;

        [Tooltip("Merge pitch wraps back down after this many tiers so it never becomes a dog whistle.")]
        [Range(2, 24)]
        [SerializeField] private int mergePitchWrapTiers = 12;

        [Tooltip("Semitones of pitch added per upgrade level, so buying up a ladder sounds like climbing one.")]
        [Range(0f, 2f)]
        [SerializeField] private float upgradePitchSemitonesPerLevel = 0.5f;

        [Header("Behaviour")]
        [Tooltip("Silence everything while the app is in the background (recommended on mobile).")]
        [SerializeField] private bool muteWhenUnfocused = true;

        private AudioSource[] voices = Array.Empty<AudioSource>();
        private AudioSource musicSource;
        private AudioSource ambienceSource;
        private int nextVoice;
        private int sfxPlayedThisFrame;
        private int lastSfxFrame = -1;
        private float masterVolume = 1f;
        private float sfxVolume = 1f;
        private float musicVolume = 1f;
        private float ambienceIntensity = 1f;
        private float musicFade = 1f;
        private float musicFadeTarget = 1f;
        private bool applicationFocused = true;
        private bool volumesLoaded;
        private readonly HashSet<int> hookedButtons = new();

        /// <summary>Master volume, 0-1. Scales every sound including music.</summary>
        public float MasterVolume
        {
            get { EnsureVolumesLoaded(); return masterVolume; }
            set => SetVolume(ref masterVolume, value, MasterVolumeKey);
        }

        /// <summary>Sound-effect volume, 0-1.</summary>
        public float SfxVolume
        {
            get { EnsureVolumesLoaded(); return sfxVolume; }
            set => SetVolume(ref sfxVolume, value, SfxVolumeKey);
        }

        /// <summary>Music and ambience volume, 0-1.</summary>
        public float MusicVolume
        {
            get { EnsureVolumesLoaded(); return musicVolume; }
            set => SetVolume(ref musicVolume, value, MusicVolumeKey);
        }

        /// <summary>True when at least one clip is assigned anywhere — lets callers skip audio work entirely.</summary>
        public bool HasAnyAudio { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            EnsureVolumesLoaded();
            BuildSources();
            EnsureListener();
            HasAnyAudio = ComputeHasAnyAudio();
            StartLoops();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (musicSource == null && ambienceSource == null)
            {
                return;
            }

            if (!Mathf.Approximately(musicFade, musicFadeTarget))
            {
                musicFade = Mathf.MoveTowards(musicFade, musicFadeTarget, Time.unscaledDeltaTime / Mathf.Max(0.01f, MusicFadeSeconds));
            }

            ApplyLoopVolumes();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            applicationFocused = hasFocus;
            ApplyLoopVolumes();
        }

        private void OnApplicationPause(bool paused)
        {
            applicationFocused = !paused;
            ApplyLoopVolumes();
        }

        // ------------------------------------------------------------------------------------------
        // Playback API
        // ------------------------------------------------------------------------------------------

        /// <summary>Plays a sound by id. Safe to call from anywhere; unassigned clips are silent no-ops.</summary>
        public static void Play(StackMergeSfx sfx)
        {
            Instance?.PlayInternal(sfx, 1f);
        }

        /// <summary>Plays a sound with an extra pitch multiplier (1 = unchanged, 2 = one octave up).</summary>
        public static void Play(StackMergeSfx sfx, float pitchScale)
        {
            Instance?.PlayInternal(sfx, pitchScale);
        }

        /// <summary>
        /// Plays the merge sound tuned to the resulting block. The pitch climbs with the tier, so a 512 merge
        /// sounds objectively "higher" than a 4 merge without needing a clip per tier. Cascades (mergeCount &gt; 1)
        /// additionally fire the chain sound.
        /// </summary>
        public static void PlayMerge(int resultingValue, int mergeCount)
        {
            Instance?.PlayMergeInternal(resultingValue, mergeCount);
        }

        /// <summary>Plays the upgrade sound, pitched up by the level reached.</summary>
        public static void PlayUpgrade(int level)
        {
            StackMergeAudio instance = Instance;
            if (instance == null)
            {
                return;
            }

            float semitones = instance.upgradePitchSemitonesPerLevel * Mathf.Max(0, level - 1);
            instance.PlayInternal(StackMergeSfx.UpgradeLevel, SemitonesToPitch(semitones));
        }

        /// <summary>
        /// Drives the ambience loudness from an arbitrary 0-1 "how big is the operation" signal. The bootstrap
        /// feeds Datacenter compute into this, so the server-room hum grows with the farm.
        /// </summary>
        public static void SetAmbienceIntensity(float intensity01)
        {
            StackMergeAudio instance = Instance;
            if (instance == null)
            {
                return;
            }

            instance.ambienceIntensity = Mathf.Clamp01(intensity01);
        }

        /// <summary>Fades music out (e.g. while a modal owns the screen) and back in.</summary>
        public static void SetMusicDucked(bool ducked)
        {
            StackMergeAudio instance = Instance;
            if (instance == null)
            {
                return;
            }

            instance.musicFadeTarget = ducked ? 0.35f : 1f;
        }

        /// <summary>
        /// Attaches the generic click sound to every Button under <paramref name="root"/> exactly once.
        /// Safe to call repeatedly after rebuilding card lists — already-hooked buttons are skipped by
        /// instance id, so runtime-instantiated rows get sound without any per-button wiring.
        /// Unlike the press animation this is skipped entirely when no click clip is assigned.
        /// </summary>
        public void HookButtonSounds(Component root)
        {
            if (root == null || !uiClick.HasClip)
            {
                return;
            }

            UnityEngine.UI.Button[] buttons = root.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                UnityEngine.UI.Button button = buttons[i];
                if (button == null || !hookedButtons.Add(button.GetInstanceID()))
                {
                    continue;
                }

                button.onClick.AddListener(PlayUiClick);
            }

            // Buttons destroyed since the last sweep would otherwise leak ids forever. The set is tiny
            // (a few hundred entries at most), but this keeps it honest across panel rebuilds.
            if (hookedButtons.Count > 2048)
            {
                hookedButtons.Clear();
            }
        }

        private void PlayUiClick()
        {
            PlayInternal(StackMergeSfx.UiClick, 1f);
        }

        // ------------------------------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------------------------------

        private void PlayMergeInternal(int resultingValue, int mergeCount)
        {
            int tier = Mathf.Max(0, FloorLog2(Mathf.Max(1, resultingValue)) - 1);
            float wrappedTier = mergePitchWrapTiers > 0 ? tier % mergePitchWrapTiers : tier;
            float pitchScale = SemitonesToPitch(mergePitchSemitonesPerTier * wrappedTier);
            PlayInternal(StackMergeSfx.Merge, pitchScale);

            if (mergeCount > 1)
            {
                PlayInternal(StackMergeSfx.MergeChain, SemitonesToPitch(Mathf.Min(mergeCount - 1, 6)));
            }
        }

        private void PlayInternal(StackMergeSfx sfx, float pitchScale)
        {
            if (!HasAnyAudio || (muteWhenUnfocused && !applicationFocused))
            {
                return;
            }

            float effective = masterVolume * sfxVolume;
            if (effective <= 0.001f)
            {
                return;
            }

            StackMergeSoundEntry entry = GetEntry(sfx);
            if (entry == null || !entry.HasClip)
            {
                return;
            }

            // Per-sound cooldown: the solver can fire 45 moves/second at max speed.
            float now = Time.unscaledTime;
            if (now - entry.lastPlayTime < entry.minInterval)
            {
                return;
            }

            // Per-frame voice budget: a cascade resolving many merges must not eat the whole pool.
            if (lastSfxFrame != Time.frameCount)
            {
                lastSfxFrame = Time.frameCount;
                sfxPlayedThisFrame = 0;
            }

            if (sfxPlayedThisFrame >= MaxSfxPerFrame)
            {
                return;
            }

            AudioClip clip = entry.PickClip();
            if (clip == null)
            {
                return;
            }

            AudioSource voice = NextVoice();
            if (voice == null)
            {
                return;
            }

            entry.lastPlayTime = now;
            sfxPlayedThisFrame++;

            voice.pitch = Mathf.Clamp(UnityEngine.Random.Range(entry.pitchMin, entry.pitchMax) * pitchScale, 0.05f, 3f);
            voice.PlayOneShot(clip, Mathf.Clamp01(entry.volume) * effective);
        }

        private AudioSource NextVoice()
        {
            if (voices.Length == 0)
            {
                return null;
            }

            // Prefer a free source so overlapping sounds do not cut each other; fall back to round-robin
            // (stealing the oldest voice) when everything is busy.
            for (int i = 0; i < voices.Length; i++)
            {
                int index = (nextVoice + i) % voices.Length;
                if (voices[index] != null && !voices[index].isPlaying)
                {
                    nextVoice = (index + 1) % voices.Length;
                    return voices[index];
                }
            }

            AudioSource stolen = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            return stolen;
        }

        private StackMergeSoundEntry GetEntry(StackMergeSfx sfx)
        {
            return sfx switch
            {
                StackMergeSfx.BlockPlace => blockPlace,
                StackMergeSfx.Merge => merge,
                StackMergeSfx.MergeChain => mergeChain,
                StackMergeSfx.NewHigh => newHigh,
                StackMergeSfx.RunStart => runStart,
                StackMergeSfx.GameOver => gameOver,
                StackMergeSfx.Pickaxe => pickaxe,
                StackMergeSfx.QueueScrub => queueScrub,
                StackMergeSfx.UnstableSave => unstableSave,
                StackMergeSfx.JokerMerge => jokerMerge,
                StackMergeSfx.UiClick => uiClick,
                StackMergeSfx.UiTab => uiTab,
                StackMergeSfx.UiPanelOpen => uiPanelOpen,
                StackMergeSfx.UiPanelClose => uiPanelClose,
                StackMergeSfx.Purchase => purchase,
                StackMergeSfx.PurchaseDenied => purchaseDenied,
                StackMergeSfx.UpgradeLevel => upgradeLevel,
                StackMergeSfx.SolverSelect => solverSelect,
                StackMergeSfx.AchievementUnlocked => achievementUnlocked,
                StackMergeSfx.Prestige => prestige,
                StackMergeSfx.ResearchUnlocked => researchUnlocked,
                StackMergeSfx.OfflineReward => offlineReward,
                StackMergeSfx.TokenSpend => tokenSpend,
                StackMergeSfx.PpoUnlocked => ppoUnlocked,
                _ => null
            };
        }

        private void BuildSources()
        {
            voices = new AudioSource[VoiceCount];
            for (int i = 0; i < VoiceCount; i++)
            {
                AudioSource source = gameObject.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
                source.bypassReverbZones = true;
                source.priority = 128;
                voices[i] = source;
            }

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
            musicSource.priority = 64;

            ambienceSource = gameObject.AddComponent<AudioSource>();
            ambienceSource.playOnAwake = false;
            ambienceSource.loop = true;
            ambienceSource.spatialBlend = 0f;
            ambienceSource.priority = 64;
        }

        // A scene with no AudioListener plays nothing and logs a warning every frame in some Unity
        // versions. The camera normally carries one; add it if the scene was built without.
        private void EnsureListener()
        {
#if UNITY_2023_1_OR_NEWER
            AudioListener listener = FindFirstObjectByType<AudioListener>(FindObjectsInactive.Include);
#else
            AudioListener listener = FindObjectOfType<AudioListener>(true);
#endif
            if (listener != null)
            {
                return;
            }

            Camera target = Camera.main;
            GameObject host = target != null ? target.gameObject : gameObject;
            host.AddComponent<AudioListener>();
        }

        private void StartLoops()
        {
            if (musicSource != null && musicLoop != null)
            {
                musicSource.clip = musicLoop;
                musicSource.Play();
            }

            if (ambienceSource != null && ambienceLoop != null)
            {
                ambienceSource.clip = ambienceLoop;
                ambienceSource.Play();
            }

            ApplyLoopVolumes();
        }

        private void ApplyLoopVolumes()
        {
            float gate = muteWhenUnfocused && !applicationFocused ? 0f : 1f;
            float baseVolume = masterVolume * musicVolume * gate;

            if (musicSource != null)
            {
                musicSource.volume = baseVolume * musicClipVolume * musicFade;
            }

            if (ambienceSource != null)
            {
                // Ambience keeps a floor so the room never goes fully silent once assigned.
                float intensity = ambienceFollowsDatacenter
                    ? Mathf.Lerp(0.35f, 1f, ambienceIntensity)
                    : 1f;
                ambienceSource.volume = baseVolume * ambienceClipVolume * intensity;
            }
        }

        private bool ComputeHasAnyAudio()
        {
            if (musicLoop != null || ambienceLoop != null)
            {
                return true;
            }

            foreach (StackMergeSfx sfx in (StackMergeSfx[])Enum.GetValues(typeof(StackMergeSfx)))
            {
                StackMergeSoundEntry entry = GetEntry(sfx);
                if (entry != null && entry.HasClip)
                {
                    return true;
                }
            }

            return false;
        }

        // The bootstrap reads the volumes while syncing the Settings sliders, which can happen before
        // this component's own Awake depending on script execution order — so loading is lazy.
        private void EnsureVolumesLoaded()
        {
            if (volumesLoaded)
            {
                return;
            }

            LoadVolumes();
        }

        private void LoadVolumes()
        {
            volumesLoaded = true;
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 0.7f));
        }

        private void SetVolume(ref float field, float value, string key)
        {
            EnsureVolumesLoaded();
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(field, clamped))
            {
                return;
            }

            field = clamped;
            PlayerPrefs.SetFloat(key, clamped);
            ApplyLoopVolumes();
        }

        /// <summary>Re-scans the assigned clips. Call after changing clips at runtime in the Editor.</summary>
        public void RefreshClipState()
        {
            HasAnyAudio = ComputeHasAnyAudio();
            StartLoops();
        }

        private static float SemitonesToPitch(float semitones)
        {
            return Mathf.Pow(2f, semitones / 12f);
        }

        private static int FloorLog2(int value)
        {
            int result = 0;
            while (value > 1)
            {
                value >>= 1;
                result++;
            }

            return result;
        }
    }
}
