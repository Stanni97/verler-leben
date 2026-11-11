using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Jotunn.Managers;
using UnityEngine;

namespace VerlerLeben
{
    /// <summary>
    /// Cow, calf and bull get their own copies of the Lox effects: without the brown hair flakes the Lox sheds when it is hit
    /// and when it dies (particles with the material lox_hair_particle), and with moos instead of the Lox's grunts.
    /// </summary>
    internal static class Effects
    {
        private const string HairMaterial = "lox_hair_particle";

        private static readonly Dictionary<string, GameObject> Cleaned = new Dictionary<string, GameObject>();

        /// <summary>Which sound role an effect list of the creature plays, by field name.</summary>
        private static string RoleOf(string field)
        {
            switch (field)
            {
                case "m_hitEffects":
                case "m_critHitEffects":
                case "m_backstabHitEffects":
                    return "hit";
                case "m_deathEffects":
                    return "death";
                case "m_idleSound":
                case "m_petEffect":
                case "m_tamedEffect":
                    return "idle";
                case "m_alertedEffects":
                    return "alert";
                default:
                    return null;
            }
        }

        /// <summary>
        /// Every effect list of the creature, without the hair. With a voice ("cow", "calf" or "bull") the sounds in them
        /// are replaced by that voice.
        /// </summary>
        public static void Prepare(GameObject creature, string voice, bool sounds, ManualLogSource log)
        {
            foreach (Component component in creature.GetComponents<Component>())
            {
                if (component is Character || component is BaseAI || component is Tameable)
                {
                    foreach (FieldInfo field in component.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
                    {
                        if (field.FieldType == typeof(EffectList))
                        {
                            Clean((EffectList)field.GetValue(component), sounds ? voice : null, sounds ? RoleOf(field.Name) : null, log);
                        }
                    }
                }
            }
        }

        public static void Clean(EffectList list, string voice, string role, ManualLogSource log)
        {
            if (list?.m_effectPrefabs == null)
            {
                return;
            }

            foreach (EffectList.EffectData effect in list.m_effectPrefabs)
            {
                if (effect?.m_prefab == null)
                {
                    continue;
                }

                GameObject copy = Variant(effect.m_prefab, voice, role, log);
                if (copy != null)
                {
                    effect.m_prefab = copy;
                }
            }
        }

        private static List<ParticleSystemRenderer> HairRenderers(GameObject prefab)
        {
            var found = new List<ParticleSystemRenderer>();
            foreach (ParticleSystemRenderer renderer in prefab.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (material != null && material.name.StartsWith(HairMaterial))
                {
                    found.Add(renderer);
                }
            }

            return found;
        }

        /// <summary>A copy of the effect without hair and with the voice, or null if it needs none.</summary>
        private static GameObject Variant(GameObject original, string voice, string role, ManualLogSource log)
        {
            string key = original.name + "|" + voice + "|" + role;
            if (Cleaned.TryGetValue(key, out GameObject known))
            {
                return known;
            }

            AudioClip[] clips = voice != null && role != null ? Voices.Get(voice, role, log) : null;
            ZSFX[] sfx = clips != null ? original.GetComponentsInChildren<ZSFX>(true) : new ZSFX[0];
            if (HairRenderers(original).Count == 0 && sfx.Length == 0)
            {
                Cleaned[key] = null;
                return null;
            }

            string name = "DF_" + (voice ?? "x") + "_" + (role ?? "x") + "_" + original.name;
            GameObject copy = PrefabManager.Instance.CreateClonedPrefab(name, original);
            if (copy == null)
            {
                return null;
            }

            foreach (ParticleSystemRenderer renderer in HairRenderers(copy))
            {
                if (renderer.gameObject != copy)
                {
                    Object.DestroyImmediate(renderer.gameObject);
                }
                else
                {
                    renderer.enabled = false;
                }
            }

            if (clips != null)
            {
                foreach (ZSFX z in copy.GetComponentsInChildren<ZSFX>(true))
                {
                    z.m_audioClips = clips;
                    z.m_minPitch = 0.94f;
                    z.m_maxPitch = 1.06f;
                }
            }

            PrefabManager.Instance.AddPrefab(copy);
            Cleaned[key] = copy;
            log?.LogInfo("Effect " + original.name + " copied as " + name + ".");
            return copy;
        }
    }
}
