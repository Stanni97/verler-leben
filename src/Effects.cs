using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Jotunn.Managers;
using UnityEngine;

namespace VerlerLeben
{
    /// <summary>
    /// The Lox loses brown hair flakes (particles with the material lox_hair_particle) when it is hit and when it dies.
    /// On a black and white cow that looks wrong, so the cow, the calf and the bull get copies of the effects without them.
    /// </summary>
    internal static class Effects
    {
        private const string HairMaterial = "lox_hair_particle";

        private static readonly Dictionary<string, GameObject> Cleaned = new Dictionary<string, GameObject>();

        /// <summary>Hit, death and every other effect list of the creature, without the hair.</summary>
        public static void RemoveHair(GameObject creature, ManualLogSource log)
        {
            var character = creature.GetComponent<Character>();
            if (character == null)
            {
                return;
            }

            foreach (FieldInfo field in typeof(Character).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(EffectList))
                {
                    Clean((EffectList)field.GetValue(character), log);
                }
            }
        }

        public static void Clean(EffectList list, ManualLogSource log)
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

                GameObject clean = Hairless(effect.m_prefab, log);
                if (clean != null)
                {
                    effect.m_prefab = clean;
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

        /// <summary>A copy of the effect without the hair, or null if it has none.</summary>
        private static GameObject Hairless(GameObject original, ManualLogSource log)
        {
            if (Cleaned.TryGetValue(original.name, out GameObject known))
            {
                return known;
            }

            if (HairRenderers(original).Count == 0)
            {
                Cleaned[original.name] = null;
                return null;
            }

            GameObject copy = PrefabManager.Instance.CreateClonedPrefab("DF_" + original.name, original);
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

            PrefabManager.Instance.AddPrefab(copy);
            Cleaned[original.name] = copy;
            log?.LogInfo("Effect " + original.name + " copied without the Lox hair.");
            return copy;
        }
    }
}
