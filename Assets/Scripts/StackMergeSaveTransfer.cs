using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace StackMerge
{
    /// <summary>
    /// Outcome of an <see cref="StackMergeSaveTransfer.Import"/> attempt.
    /// </summary>
    public enum StackMergeSaveImportResult
    {
        Success,
        Empty,
        BadFormat,
        ChecksumMismatch,
        UnsupportedVersion
    }

    /// <summary>
    /// Local save export/import. Turns the whole PlayerPrefs save into a single text code the player can
    /// copy anywhere (chat, notes, e-mail) and paste back on another device or after a reinstall.
    ///
    /// NO server, no account, no cloud service is involved — the code IS the save. A full backup file is
    /// additionally written next to the game's own data so a mistyped import can still be undone.
    ///
    /// The PPO network weights are deliberately excluded: they are tens of thousands of floats (hundreds of
    /// kilobytes even compressed), which would make the code unpasteable, and PPO knowledge is the one part
    /// of the save the game can regenerate by itself. Everything that cost the player real time — chips,
    /// upgrades, agents, modifiers, prestige count, Insight, research, the Datacenter, achievements,
    /// lifetime metrics and the frame counters — is included.
    /// </summary>
    public static class StackMergeSaveTransfer
    {
        private const string Prefix = "SM1";
        private const string Separator = "|";
        private const string BackupFileName = "stackmerge_backup.txt";

        // Every PlayerPrefs key that carries player-owned state. The PPO policy keys are intentionally
        // absent — see the class summary.
        private static readonly string[] ExportedKeys =
        {
            "StackMerge.Progression.v2",
            "StackMerge.HighScore",
            "StackMerge.Settings.Language",
            "StackMerge.Settings.BlockNumerals",
            "StackMerge.Settings.ShowFps",
            "StackMerge.Settings.SuppressAchievementNotification",
            StackMergeAudio.MasterVolumeKey,
            StackMergeAudio.SfxVolumeKey,
            StackMergeAudio.MusicVolumeKey
        };

        // Keys stored as ints rather than strings. PlayerPrefs is typed, so the exporter has to know.
        private static readonly string[] IntKeys =
        {
            "StackMerge.HighScore",
            "StackMerge.Settings.Language",
            "StackMerge.Settings.BlockNumerals",
            "StackMerge.Settings.ShowFps",
            "StackMerge.Settings.SuppressAchievementNotification"
        };

        private static readonly string[] FloatKeys =
        {
            StackMergeAudio.MasterVolumeKey,
            StackMergeAudio.SfxVolumeKey,
            StackMergeAudio.MusicVolumeKey
        };

        [Serializable]
        private sealed class Envelope
        {
            public int format = 1;
            public string game = "StackMerge";
            public long createdUnix;
            public string[] keys = Array.Empty<string>();
            public string[] values = Array.Empty<string>();
            public string[] kinds = Array.Empty<string>();
        }

        /// <summary>Full path of the local backup file written on every export.</summary>
        public static string BackupFilePath => Path.Combine(Application.persistentDataPath, BackupFileName);

        /// <summary>
        /// Builds the transfer code for the current save. The caller is responsible for flushing any
        /// pending in-memory progression to PlayerPrefs first (see StackMergeProgression.SaveImmediate).
        /// </summary>
        public static string Export()
        {
            Envelope envelope = new()
            {
                createdUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            int count = ExportedKeys.Length;
            envelope.keys = new string[count];
            envelope.values = new string[count];
            envelope.kinds = new string[count];

            int written = 0;
            for (int i = 0; i < count; i++)
            {
                string key = ExportedKeys[i];
                if (!PlayerPrefs.HasKey(key))
                {
                    continue;
                }

                string kind = KindOf(key);
                envelope.keys[written] = key;
                envelope.kinds[written] = kind;
                envelope.values[written] = kind switch
                {
                    "i" => PlayerPrefs.GetInt(key, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    "f" => PlayerPrefs.GetFloat(key, 0f).ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                    _ => PlayerPrefs.GetString(key, string.Empty)
                };
                written++;
            }

            Array.Resize(ref envelope.keys, written);
            Array.Resize(ref envelope.values, written);
            Array.Resize(ref envelope.kinds, written);

            string json = JsonUtility.ToJson(envelope);
            string payload = Convert.ToBase64String(Compress(Encoding.UTF8.GetBytes(json)));
            string code = $"{Prefix}{Separator}{Checksum(payload):X8}{Separator}{payload}";

            WriteBackupFile(code);
            return code;
        }

        /// <summary>
        /// Restores a save from a transfer code. On success the PlayerPrefs are overwritten and flushed —
        /// the caller must then reload the progression (the simplest correct way is a scene reload, which
        /// is what the bootstrap does).
        /// </summary>
        public static StackMergeSaveImportResult Import(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return StackMergeSaveImportResult.Empty;
            }

            // Pasting through chat apps routinely introduces whitespace and line breaks.
            string cleaned = code.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty).Replace(" ", string.Empty);
            string[] parts = cleaned.Split(Separator[0]);
            if (parts.Length != 3)
            {
                return StackMergeSaveImportResult.BadFormat;
            }

            if (parts[0] != Prefix)
            {
                return StackMergeSaveImportResult.UnsupportedVersion;
            }

            string payload = parts[2];
            if (!uint.TryParse(parts[1], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out uint expected)
                || Checksum(payload) != expected)
            {
                return StackMergeSaveImportResult.ChecksumMismatch;
            }

            Envelope envelope;
            try
            {
                byte[] raw = Decompress(Convert.FromBase64String(payload));
                envelope = JsonUtility.FromJson<Envelope>(Encoding.UTF8.GetString(raw));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"StackMerge: save import failed to decode — {exception.Message}");
                return StackMergeSaveImportResult.BadFormat;
            }

            if (envelope == null || envelope.keys == null || envelope.values == null || envelope.kinds == null)
            {
                return StackMergeSaveImportResult.BadFormat;
            }

            if (envelope.format != 1 || envelope.game != "StackMerge")
            {
                return StackMergeSaveImportResult.UnsupportedVersion;
            }

            if (envelope.keys.Length != envelope.values.Length || envelope.keys.Length != envelope.kinds.Length)
            {
                return StackMergeSaveImportResult.BadFormat;
            }

            // Back up whatever is currently installed before overwriting it, so an accidental import of
            // someone else's code is recoverable.
            WriteBackupFile(Export(), "stackmerge_pre_import.txt");

            // The imported save has no PPO weights, so the existing ones must go: keeping a network
            // trained against a different progression state would be worse than starting fresh.
            PlayerPrefs.DeleteKey("StackMerge.Progression.v2.PpoPolicy");
            PlayerPrefs.DeleteKey("StackMerge.Progression.v2.PpoPermanentPolicy");
            PlayerPrefs.DeleteKey("StackMerge.Progression.v2.PpoPrestigeMemoryPolicy");

            for (int i = 0; i < envelope.keys.Length; i++)
            {
                string key = envelope.keys[i];
                if (string.IsNullOrEmpty(key) || Array.IndexOf(ExportedKeys, key) < 0)
                {
                    // Ignore anything the current build does not know about rather than trusting it.
                    continue;
                }

                string value = envelope.values[i] ?? string.Empty;
                switch (envelope.kinds[i])
                {
                    case "i":
                        if (int.TryParse(value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int intValue))
                        {
                            PlayerPrefs.SetInt(key, intValue);
                        }

                        break;
                    case "f":
                        if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatValue))
                        {
                            PlayerPrefs.SetFloat(key, floatValue);
                        }

                        break;
                    default:
                        PlayerPrefs.SetString(key, value);
                        break;
                }
            }

            PlayerPrefs.Save();
            return StackMergeSaveImportResult.Success;
        }

        /// <summary>Human-readable message for an import result, in English (localized at display time).</summary>
        public static string DescribeResult(StackMergeSaveImportResult result)
        {
            return result switch
            {
                StackMergeSaveImportResult.Success => "Save imported. Restarting...",
                StackMergeSaveImportResult.Empty => "Paste a save code first",
                StackMergeSaveImportResult.BadFormat => "That is not a valid save code",
                StackMergeSaveImportResult.ChecksumMismatch => "Save code is damaged or incomplete",
                StackMergeSaveImportResult.UnsupportedVersion => "Save code is from a different version",
                _ => "Import failed"
            };
        }

        private static string KindOf(string key)
        {
            if (Array.IndexOf(IntKeys, key) >= 0)
            {
                return "i";
            }

            return Array.IndexOf(FloatKeys, key) >= 0 ? "f" : "s";
        }

        private static void WriteBackupFile(string code, string fileName = BackupFileName)
        {
            try
            {
                File.WriteAllText(Path.Combine(Application.persistentDataPath, fileName), code);
            }
            catch (Exception exception)
            {
                // A read-only or full data directory must never break the in-memory export.
                Debug.LogWarning($"StackMerge: could not write save backup — {exception.Message}");
            }
        }

        private static byte[] Compress(byte[] data)
        {
            using MemoryStream output = new();
            // Fully qualified: UnityEngine also declares a CompressionLevel enum.
            using (GZipStream gzip = new(output, System.IO.Compression.CompressionLevel.Optimal))
            {
                gzip.Write(data, 0, data.Length);
            }

            return output.ToArray();
        }

        private static byte[] Decompress(byte[] data)
        {
            using MemoryStream input = new(data);
            using GZipStream gzip = new(input, CompressionMode.Decompress);
            using MemoryStream output = new();
            gzip.CopyTo(output);
            return output.ToArray();
        }

        // FNV-1a. Not security — it only catches a truncated or mangled paste, which is the realistic
        // failure mode when a code travels through a chat app.
        private static uint Checksum(string text)
        {
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619u;
                }

                return hash;
            }
        }
    }
}
