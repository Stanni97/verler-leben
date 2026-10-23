using System.Collections.Generic;
using UnityEngine;

namespace VerlerLeben
{
    /// <summary>
    /// Schroeders Boonekamp, the digestif: more health and stamina regeneration, and much more while a
    /// hearty meal of this mod (Teller, Pizza, ...) is still active. SEMan clones the effect with MemberwiseClone,
    /// so the fields set when it is created survive.
    /// </summary>
    internal class SE_Boonekamp : SE_Stats
    {
        /// <summary>Item names ($item_...) of the foods that trigger the bonus.</summary>
        internal static readonly List<string> FeastFoods = new List<string>();

        public float FeastHealthRegenMultiplier = 1.8f;
        public float FeastStaminaRegenMultiplier = 1.5f;

        public override void ModifyHealthRegen(ref float regenMultiplier)
        {
            float normal = m_healthRegenMultiplier;
            if (FeastActive())
            {
                m_healthRegenMultiplier = FeastHealthRegenMultiplier;
            }

            base.ModifyHealthRegen(ref regenMultiplier);
            m_healthRegenMultiplier = normal;
        }

        public override void ModifyStaminaRegen(ref float staminaRegen)
        {
            float normal = m_staminaRegenMultiplier;
            if (FeastActive())
            {
                m_staminaRegenMultiplier = FeastStaminaRegenMultiplier;
            }

            base.ModifyStaminaRegen(ref staminaRegen);
            m_staminaRegenMultiplier = normal;
        }

        private bool FeastActive()
        {
            var player = m_character as Player;
            if (player == null)
            {
                return false;
            }

            foreach (Player.Food food in player.GetFoods())
            {
                if (food.m_item != null && FeastFoods.Contains(food.m_item.m_shared.m_name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
