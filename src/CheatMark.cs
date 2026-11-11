using System.Collections.Generic;
using HarmonyLib;

namespace VerlerLeben
{
    /// <summary>
    /// The game marks an item "Erworben durch Cheaten" when a console command made it, but it also passes the mark on:
    /// anything built or crafted from a marked stack (Stone, Wood, a hammer from an old test, ...) and anything made
    /// at a station that was built that way is marked as well. One such stack in a backpack is enough, and the mark
    /// then spreads to every Doener, cheese and feast. The items of this mod are never cleared of the mark by the game,
    /// so they are cleaned whenever an inventory changes.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), "Changed", typeof(bool), typeof(bool))]
    internal static class CheatMark
    {
        private static readonly AccessTools.FieldRef<Inventory, List<ItemDrop.ItemData>> Items =
            AccessTools.FieldRefAccess<Inventory, List<ItemDrop.ItemData>>("m_inventory");

        internal static bool IsOurs(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return false;
            }

            if (item.m_dropPrefab != null && item.m_dropPrefab.name.StartsWith("DF_"))
            {
                return true;
            }

            return item.m_shared != null && item.m_shared.m_name != null && item.m_shared.m_name.StartsWith("$df_");
        }

        [HarmonyPrefix]
        private static void Prefix(Inventory __instance, ref bool cheatedStateChanged)
        {
            List<ItemDrop.ItemData> items = Items(__instance);
            if (items == null)
            {
                return;
            }

            foreach (ItemDrop.ItemData item in items)
            {
                if (item != null && item.m_cheated && IsOurs(item))
                {
                    item.m_cheated = false;
                }
            }

            // No "you picked up a cheated item" message if it was only one of ours.
            if (cheatedStateChanged && !__instance.AnyCheatedItem())
            {
                cheatedStateChanged = false;
            }
        }
    }
}
