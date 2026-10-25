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
            Enabled = config.Bind(s, "Enabled", Enabled, ServerSync.Desc("Can be crafted. Off: the recipe is gone.")).Value;
            CraftingStation = config.Bind(s, "CraftingStation", CraftingStation,
                ServerSync.Desc("Where the ingredient is crafted.", CraftingStations.GetAcceptableValueList())).Value;
            Ingredients = config.Bind(s, "Ingredients", Ingredients, ServerSync.Desc("Comma separated list of ItemPrefab:Amount.")).Value;
            Amount = config.Bind(s, "Amount", Amount, ServerSync.Desc("Items produced per craft.")).Value;
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
            Enabled = config.Bind(s, "Enabled", Enabled, ServerSync.Desc("Can be prepared and served. Off: the raw dish has no recipe and the feast is not in the Feaster.")).Value;
            CraftingStation = config.Bind(s, "CraftingStation", CraftingStation,
                ServerSync.Desc("Where the raw dish is prepared.", CraftingStations.GetAcceptableValueList())).Value;
            Ingredients = config.Bind(s, "Ingredients", Ingredients, ServerSync.Desc("Comma separated list of ItemPrefab:Amount for the raw dish.")).Value;
            Amount = config.Bind(s, "Amount", Amount, ServerSync.Desc("Raw dishes produced per craft.")).Value;
            BakeSeconds = config.Bind(s, "BakeSeconds", BakeSeconds, ServerSync.Desc("How long the raw dish bakes in the stone oven, in seconds.")).Value;
            Health = config.Bind(s, "Health", Health, ServerSync.Desc("Health per serving.")).Value;
            Stamina = config.Bind(s, "Stamina", Stamina, ServerSync.Desc("Stamina per serving.")).Value;
            Regen = config.Bind(s, "HealthRegen", Regen, ServerSync.Desc("Health regeneration per tick while the food lasts.")).Value;
            BurnSeconds = config.Bind(s, "DurationSeconds", BurnSeconds, ServerSync.Desc("How long the food lasts after eating, in seconds.")).Value;
            Servings = config.Bind(s, "Servings", Servings, ServerSync.Desc("How many times the feast can be eaten from before it is gone.")).Value;
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
        public int HealthRegenBonus = 25;
        public int StaminaRegenBonus = 15;
        public int FeastHealthRegenBonus = 15;
        public int FeastStaminaRegenBonus = 10;
        public int MeadHealthRegenBonus = 10;
        public int MeadStaminaRegenBonus = 8;
        public int BeerHealthRegenBonus = 15;
        public int BeerStaminaRegenBonus = 10;

        public string Token => "df_" + Key.ToLowerInvariant();
        public string IconResource => $"VerlerLeben.{Key.ToLowerInvariant()}.png";

        public void Bind(ConfigFile config)
        {
            string s = Key;
            Enabled = config.Bind(s, "Enabled", Enabled, ServerSync.Desc("Can be brewed. Off: the recipe is gone.")).Value;
            CraftingStation = config.Bind(s, "CraftingStation", CraftingStation,
                ServerSync.Desc("Where the drink is brewed.", CraftingStations.GetAcceptableValueList())).Value;
            Ingredients = config.Bind(s, "Ingredients", Ingredients, ServerSync.Desc("Comma separated list of ItemPrefab:Amount.")).Value;
            Amount = config.Bind(s, "Amount", Amount, ServerSync.Desc("Bottles produced per craft.")).Value;
            Duration = config.Bind(s, "DurationSeconds", Duration, ServerSync.Desc("How long the buff lasts, in seconds.")).Value;
            HealthRegenBonus = config.Bind(s, "HealthRegenBonusPercent", HealthRegenBonus, ServerSync.Desc("Faster health regeneration, in percent.")).Value;
            StaminaRegenBonus = config.Bind(s, "StaminaRegenBonusPercent", StaminaRegenBonus, ServerSync.Desc("Faster stamina regeneration, in percent.")).Value;
            FeastHealthRegenBonus = config.Bind(s, "FeastExtraHealthRegenPercent", FeastHealthRegenBonus,
                ServerSync.Desc("Extra health regeneration while a feast of this mod is active, on top of the base value, in percent.")).Value;
            FeastStaminaRegenBonus = config.Bind(s, "FeastExtraStaminaRegenPercent", FeastStaminaRegenBonus,
                ServerSync.Desc("Extra stamina regeneration while a feast of this mod is active, in percent.")).Value;
            MeadHealthRegenBonus = config.Bind(s, "MeadExtraHealthRegenPercent", MeadHealthRegenBonus,
                ServerSync.Desc("Extra health regeneration while any mead is active, in percent.")).Value;
            MeadStaminaRegenBonus = config.Bind(s, "MeadExtraStaminaRegenPercent", MeadStaminaRegenBonus,
                ServerSync.Desc("Extra stamina regeneration while any mead is active, in percent.")).Value;
            BeerHealthRegenBonus = config.Bind(s, "BeerExtraHealthRegenPercent", BeerHealthRegenBonus,
                ServerSync.Desc("Extra health regeneration while barley wine, the beer of the game, is active, in percent.")).Value;
            BeerStaminaRegenBonus = config.Bind(s, "BeerExtraStaminaRegenPercent", BeerStaminaRegenBonus,
                ServerSync.Desc("Extra stamina regeneration while barley wine is active, in percent.")).Value;
        }
    }

    /// <summary>A quick snack: a food that gives a little health, stamina and Eitr for a short time.</summary>
    internal sealed class Snack
    {
        public string Key;
        public string PrefabName;
        public string BasePrefab;
        public float Weight = 0.3f;
        public int StackSize = 20;

        public bool Enabled = true;
        public string CraftingStation = nameof(CraftingStations.Cauldron);
        public string Ingredients = "";
        public int Amount = 1;
        public float Health, Stamina, Eitr, Regen;
        public float BurnSeconds = 900f;

        public string Token => "df_" + Key.ToLowerInvariant();
        public string IconResource => $"VerlerLeben.{Key.ToLowerInvariant()}.png";

        public void Bind(ConfigFile config)
        {
            string s = Key;
            Enabled = config.Bind(s, "Enabled", Enabled, ServerSync.Desc("Can be made. Off: the recipe is gone.")).Value;
            CraftingStation = config.Bind(s, "CraftingStation", CraftingStation,
                ServerSync.Desc("Where the snack is made.", CraftingStations.GetAcceptableValueList())).Value;
            Ingredients = config.Bind(s, "Ingredients", Ingredients, ServerSync.Desc("Comma separated list of ItemPrefab:Amount.")).Value;
            Amount = config.Bind(s, "Amount", Amount, ServerSync.Desc("Snacks produced per craft.")).Value;
            Health = config.Bind(s, "Health", Health, ServerSync.Desc("Health when eaten.")).Value;
            Stamina = config.Bind(s, "Stamina", Stamina, ServerSync.Desc("Stamina when eaten.")).Value;
            Eitr = config.Bind(s, "Eitr", Eitr, ServerSync.Desc("Eitr when eaten.")).Value;
            Regen = config.Bind(s, "HealthRegen", Regen, ServerSync.Desc("Health regeneration per tick while the food lasts.")).Value;
            BurnSeconds = config.Bind(s, "DurationSeconds", BurnSeconds, ServerSync.Desc("How long the food lasts after eating, in seconds.")).Value;
        }
    }
}
