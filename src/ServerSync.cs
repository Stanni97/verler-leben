using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace VerlerLeben
{
    /// <summary>
    /// Gameplay values belong to the server: on a server with this mod its config applies to every player
    /// (Jotunn sends it on join and restores the player's own values on leaving). The values are read again
    /// whenever they change, and recipes, food and effects that already exist are updated in place.
    /// </summary>
    internal static class ServerSync
    {
        internal static ManualLogSource Log;

        private static readonly Dictionary<object, string> Applied = new Dictionary<object, string>();
        private static readonly MethodInfo AddWord = AccessTools.Method(typeof(Localization), "AddWord");

        /// <summary>Description of an entry the server decides.</summary>
        public static ConfigDescription Desc(string text, AcceptableValueBase acceptable = null)
        {
            return new ConfigDescription(text, acceptable, new ConfigurationManagerAttributes { IsAdminOnly = true });
        }

        /// <summary>Notes what a recipe or piece was built with, so unchanged ingredients are left alone.</summary>
        public static void Remember(object owner, string station, string ingredients)
        {
            if (owner != null)
            {
                Applied[owner] = station + "|" + ingredients;
            }
        }

        public static void SetRecipe(Recipe recipe, bool enabled, string station, int amount, string ingredients)
        {
            if (recipe == null)
            {
                return;
            }

            recipe.m_enabled = enabled;
            recipe.m_amount = Mathf.Max(1, amount);
            string key = station + "|" + ingredients;
            if (Applied.TryGetValue(recipe, out string last) && last == key)
            {
                return;
            }

            recipe.m_craftingStation = Station(station);
            recipe.m_resources = Requirements(recipe.name, ingredients);
            Applied[recipe] = key;
        }

        /// <summary>New requirements for a piece, only when the ingredients changed.</summary>
        public static void SetPiece(Piece piece, bool enabled, string ingredients, Func<string, bool> recover = null)
        {
            if (piece == null)
            {
                return;
            }

            piece.m_enabled = enabled;
            if (Applied.TryGetValue(piece, out string last) && last == "|" + ingredients)
            {
                return;
            }

            piece.m_resources = Requirements(piece.name, ingredients, recover);
            Applied[piece] = "|" + ingredients;
        }

        private static CraftingStation Station(string name)
        {
            string internalName = CraftingStations.GetInternalName(name);
            return string.IsNullOrEmpty(internalName) ? null : PrefabManager.Instance.GetPrefab(internalName)?.GetComponent<CraftingStation>();
        }

        public static Piece.Requirement[] Requirements(string owner, string ingredients, Func<string, bool> recover = null)
        {
            var requirements = new List<Piece.Requirement>();
            foreach (string entry in (ingredients ?? "").Split(','))
            {
                string[] parts = entry.Split(':');
                string prefab = parts[0].Trim();
                if (prefab.Length == 0)
                {
                    continue;
                }

                int amount = 1;
                if (parts.Length > 1 && !int.TryParse(parts[1].Trim(), out amount))
                {
                    amount = 1;
                }

                ItemDrop item = PrefabManager.Instance.GetPrefab(prefab)?.GetComponent<ItemDrop>();
                if (item == null)
                {
                    Log?.LogWarning($"{owner}: ingredient '{prefab}' was not found, check the prefab name.");
                    continue;
                }

                requirements.Add(new Piece.Requirement
                {
                    m_resItem = item, m_amount = Mathf.Max(1, amount), m_amountPerLevel = 0, m_recover = recover?.Invoke(prefab) ?? true,
                });
            }

            return requirements.ToArray();
        }

        /// <summary>Replaces a text of the language in use, e.g. a description with a duration in it.</summary>
        public static void SetWord(string key, string text)
        {
            if (Localization.instance != null && AddWord != null)
            {
                AddWord.Invoke(Localization.instance, new object[] { key, text });
            }
        }

        public static bool German => Localization.instance != null && Localization.instance.GetSelectedLanguage() == "German";
    }
}
