using System.Collections.Generic;
using UnityEngine;

namespace VerlerLeben
{
    /// <summary>
    /// Schroeders Boonekamp, the digestif: some health and stamina regeneration, more with good company:
    /// a hearty meal of this mod, any mead, and barley wine (the closest thing to beer). The bonuses add up.
    /// SEMan clones the effect with MemberwiseClone, so the fields set when it is created survive.
    /// </summary>
    internal class SE_Boonekamp : SE_Stats
    {
        /// <summary>Item names ($item_...) of the foods that trigger the feast bonus.</summary>
        internal static readonly List<string> FeastFoods = new List<string>();

        /// <summary>Status effect of the barley wine, the "beer" of the game.</summary>
        private const string BeerStatus = "Potion_barleywine";

        // Extra regeneration on top of the base value, as fractions (0.5 = +50 %).
        public float FeastHealth = 0.15f, FeastStamina = 0.10f;
        public float MeadHealth = 0.10f, MeadStamina = 0.08f;
        public float BeerHealth = 0.15f, BeerStamina = 0.10f;

        public override void ModifyHealthRegen(ref float regenMultiplier)
        {
            float normal = m_healthRegenMultiplier;
            m_healthRegenMultiplier = normal + Extra(FeastHealth, MeadHealth, BeerHealth);
            base.ModifyHealthRegen(ref regenMultiplier);
            m_healthRegenMultiplier = normal;
        }

        public override void ModifyStaminaRegen(ref float staminaRegen)
        {
            float normal = m_staminaRegenMultiplier;
            m_staminaRegenMultiplier = normal + Extra(FeastStamina, MeadStamina, BeerStamina);
            base.ModifyStaminaRegen(ref staminaRegen);
            m_staminaRegenMultiplier = normal;
        }

        private float Extra(float feast, float mead, float beer)
        {
            float extra = 0f;
            if (FeastActive())
            {
                extra += feast;
            }

            CountDrinks(out bool anyMead, out bool anyBeer);
            if (anyMead)
            {
                extra += mead;
            }

            if (anyBeer)
            {
                extra += beer;
            }

            return extra;
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

        /// <summary>The vanilla meads all have a status effect called Potion_...; the barley wine counts as beer.</summary>
        private void CountDrinks(out bool anyMead, out bool anyBeer)
        {
            anyMead = false;
            anyBeer = false;
            if (m_character == null)
            {
                return;
            }

            foreach (StatusEffect effect in m_character.GetSEMan().GetStatusEffects())
            {
                if (effect == null || effect.name == null || !effect.name.StartsWith("Potion_"))
                {
                    continue;
                }

                if (effect.name == BeerStatus)
                {
                    anyBeer = true;
                }
                else
                {
                    anyMead = true;
                }
            }
        }
    }
}
