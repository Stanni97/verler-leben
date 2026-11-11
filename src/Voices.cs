using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Rendering;

namespace VerlerLeben
{
    /// <summary>The voices of cow, calf and bull (Assets/*.wav, made by tools/make_sounds.py), as clips for the game's own SFX objects.</summary>
    internal static class Voices
    {
        private static readonly Dictionary<string, AudioClip[]> Clips = new Dictionary<string, AudioClip[]>();

        private static readonly Dictionary<string, string[]> Files = new Dictionary<string, string[]>
        {
            { "cow|idle", new[] { "cow_idle1", "cow_idle2" } },
            { "cow|alert", new[] { "cow_alert" } },
            { "cow|hit", new[] { "cow_hit1", "cow_hit2" } },
            { "cow|death", new[] { "cow_death" } },
            { "calf|idle", new[] { "calf_idle1", "calf_idle2" } },
            { "calf|alert", new[] { "calf_idle2" } },
            { "calf|hit", new[] { "calf_hit" } },
            { "calf|death", new[] { "calf_death" } },
            { "bull|idle", new[] { "bull_idle1", "bull_idle2" } },
            { "bull|alert", new[] { "bull_alert" } },
            { "bull|hit", new[] { "bull_hit" } },
            { "bull|death", new[] { "bull_death" } },
        };

        /// <summary>The clips of a voice and role (idle, alert, hit or death), or null.</summary>
        public static AudioClip[] Get(string voice, string role, ManualLogSource log)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return null;   // dedicated server, nothing is played there
            }

            string key = voice + "|" + role;
            if (Clips.TryGetValue(key, out AudioClip[] known))
            {
                return known;
            }

            AudioClip[] clips = null;
            if (Files.TryGetValue(key, out string[] names))
            {
                try
                {
                    clips = Array.ConvertAll(names, name => Load(name));
                }
                catch (Exception e)
                {
                    log?.LogWarning("Could not load the " + key + " sounds: " + e.Message);
                }
            }

            Clips[key] = clips;
            return clips;
        }

        private static AudioClip Load(string name)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("VerlerLeben." + name + ".wav"))
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                return FromWav(name, buffer.ToArray());
            }
        }

        private static AudioClip FromWav(string name, byte[] data)
        {
            int channels = 1, rate = 22050, bits = 16, offset = 12, dataStart = -1, dataLength = 0;
            while (offset + 8 <= data.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(data, offset, 4);
                int size = BitConverter.ToInt32(data, offset + 4);
                if (id == "fmt ")
                {
                    channels = BitConverter.ToInt16(data, offset + 10);
                    rate = BitConverter.ToInt32(data, offset + 12);
                    bits = BitConverter.ToInt16(data, offset + 22);
                }
                else if (id == "data")
                {
                    dataStart = offset + 8;
                    dataLength = Math.Min(size, data.Length - dataStart);
                    break;
                }

                offset += 8 + size + (size & 1);
            }

            if (dataStart < 0 || bits != 16)
            {
                throw new InvalidDataException(name + ".wav is not 16 bit PCM");
            }

            int count = dataLength / 2;
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                samples[i] = BitConverter.ToInt16(data, dataStart + i * 2) / 32768f;
            }

            AudioClip clip = AudioClip.Create(name, count / channels, channels, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
