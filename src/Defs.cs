using BepInEx.Configuration;
using Jotunn.Configs;

namespace VerlerLeben
{
    /// <summary>
    /// A plain ingredient (Döner meat, cheese). Field initialisers are the defaults;
    /// <see cref="Bind"/> replaces them with the values from the config file.
    /// </summary>
    internal sealed class Ingredient
    {
        public string Key;                 // config section, localization and icon name
        public string PrefabName;
        public string BasePrefab;          // vanilla item whose model is cloned
        public float Weight = 0.5f;
        public int StackSize = 30;

        public bool Enabled = true;
        public string CraftingStation = nameof(CraftingStations.FoodPreparationTable);
        public string Ingredients = "";
        public int Amount = 1;

        public string Token => "df_" + Key.ToLowerInvariant();
        public string IconResource => $"VerlerLeben.{Key.ToLowerInvariant()}.png";

        public void Bind(ConfigFile config)
        {
            string s = Key;
            Enabled = config.Bind(s, "Enabled", Enabled, "Adds this ingredient to the game.").Value;
            CraftingStation = config.Bind(s, "CraftingStation", CraftingStation,
                new ConfigDescription("Where the ingredient is crafted.", CraftingStations.GetAcceptableValueList())).Value;
            Ingredients = config.Bind(s, "Ingredients", Ingredients, "Comma separated list of ItemPrefab:Amount.").Value;
            Amount = config.Bind(s, "Amount", Amount, "Items produced per craft.").Value;
        }
    }

    /// <summary>
    /// A baked feast. Three items belong to it: the raw dish (crafted), the baked dish (comes out of the
    /// stone oven, this is what the Feaster tool places) and the feast piece standing on the table.
    /// </summary>
    internal sealed class Dish
    {
        public string Key;
        public string RawPrefab, MaterialPrefab, FeastPrefab;
        public string BaseRaw = "LoxPieUncooked";
        public string BaseMaterial, BaseFeast;     // vanilla models to clone

        public bool Enabled = true;
        public string CraftingStation = nameof(CraftingStations.FoodPreparationTable);
        public string Ingredients = "";
        public int Amount = 1;
        public float BakeSeconds = 60f;

        public float Health, Stamina, Regen;
        public float BurnSeconds = 3000f;
        public int Servings = 10;

        public string Token => "df_" + Key.ToLowerInvariant();
        public string RawIconResource => $"VerlerLeben.{Key.ToLowerInvariant()}raw.png";
        public string IconResource => $"VerlerLeben.{Key.ToLowerInvariant()}.png";

        public void Bind(ConfigFile config)
        {
            string s = Key;
            Enabled = config.Bind(s, "Enabled", Enabled, "Adds this feast to the game.").Value;
            CraftingStation = config.Bind(s, "CraftingStation", CraftingStation,
                new ConfigDescription("Where the raw dish is prepared.", CraftingStations.GetAcceptableValueList())).Value;
            Ingredients = config.Bind(s, "Ingredients", Ingredients, "Comma separated list of ItemPrefab:Amount for the raw dish.").Value;
            Amount = config.Bind(s, "Amount", Amount, "Raw dishes produced per craft.").Value;
            BakeSeconds = config.Bind(s, "BakeSeconds", BakeSeconds, "How long the raw dish bakes in the stone oven, in seconds.").Value;
            Health = config.Bind(s, "Health", Health, "Health per serving.").Value;
            Stamina = config.Bind(s, "Stamina", Stamina, "Stamina per serving.").Value;
            Regen = config.Bind(s, "HealthRegen", Regen, "Health regeneration per tick while the food lasts.").Value;
            BurnSeconds = config.Bind(s, "DurationSeconds", BurnSeconds, "How long the food lasts after eating, in seconds.").Value;
            Servings = config.Bind(s, "Servings", Servings, "How many times the feast can be eaten from before it is gone.").Value;
        }
    }

    /// <summary>An item that only drops or is found, without a recipe (milk, beef).</summary>
    internal sealed class DropItem
    {
        public string Key;
        public string PrefabName;
        public string BasePrefab;
        public float Weight = 0.5f;
        public int StackSize = 30;

        public string Token => "df_" + Key.ToLowerInvariant();
        public string IconResource => $"VerlerLeben.{Key.ToLowerInvariant()}.png";
    }

    /// <summary>Schroeders Boonekamp, a drink with a regeneration buff that is stronger after a feast.</summary>
    internal sealed class Drink
    {
        public string Key = "Boonekamp";
        public string PrefabName = "DF_Boonekamp";

        public bool Enabled = true;
        public string CraftingStation = nameof(CraftingStations.Cauldron);
        public string Ingredients = "Barley:3,Thistle:2,Dandelion:2,Honey:1,SpiceMountains:1";
        public int Amount = 2;
        public float Duration = 300f;
        public int HealthRegenBonus = 30;
        public int StaminaRegenBonus = 20;
        public int FeastHealthRegenBonus = 80;
        public int FeastStaminaRegenBonus = 50;

        public string Token => "df_" + Key.ToLowerInvariant();
        public string IconResource => $"VerlerLeben.{Key.ToLowerInvariant()}.png";

        public void Bind(ConfigFile config)
        {
            string s = Key;
            Enabled = config.Bind(s, "Enabled", Enabled, "Adds the drink to the game.").Value;
            CraftingStation = config.Bind(s, "CraftingStation", CraftingStation,
                new ConfigDescription("Where the drink is brewed.", CraftingStations.GetAcceptableValueList())).Value;
            Ingredients = config.Bind(s, "Ingredients", Ingredients, "Comma separated list of ItemPrefab:Amount.").Value;
            Amount = config.Bind(s, "Amount", Amount, "Bottles produced per craft.").Value;
            Duration = config.Bind(s, "DurationSeconds", Duration, "How long the buff lasts, in seconds.").Value;
            HealthRegenBonus = config.Bind(s, "HealthRegenBonusPercent", HealthRegenBonus, "Faster health regeneration, in percent.").Value;
            StaminaRegenBonus = config.Bind(s, "StaminaRegenBonusPercent", StaminaRegenBonus, "Faster stamina regeneration, in percent.").Value;
            FeastHealthRegenBonus = config.Bind(s, "FeastHealthRegenBonusPercent", FeastHealthRegenBonus,
                "Faster health regeneration while a feast of this mod is active, in percent.").Value;
            FeastStaminaRegenBonus = config.Bind(s, "FeastStaminaRegenBonusPercent", FeastStaminaRegenBonus,
                "Faster stamina regeneration while a feast of this mod is active, in percent.").Value;
        }
    }
}
