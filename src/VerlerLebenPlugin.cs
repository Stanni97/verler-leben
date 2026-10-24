using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using UnityEngine;

namespace VerlerLeben
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class VerlerLebenPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "felix.verlerleben";
        public const string PluginName = "Verler Leben";
        // Keep in sync with <Version> in VerlerLeben.csproj.
        public const string PluginVersion = "1.2.2";

        // The piece table of the Feaster tool, the one the vanilla biome feasts are in.
        private const string FeasterTable = "_FeasterPieceTable";

        private readonly Ingredient[] _ingredients =
        {
            new Ingredient
            {
                Key = "Doener", PrefabName = "DF_Doener", BasePrefab = "LoxMeat",
                // Beef of the cow, a little lox from the Plains and the Mountains spice.
                Ingredients = "DF_Rindfleisch:1,CookedLoxMeat:1,SpiceMountains:1", Amount = 3,
            },
            new Ingredient
            {
                Key = "Kaese", PrefabName = "DF_Kaese", BasePrefab = "BreadDough",
                // Milk of the cow.
                Ingredients = "DF_Milch:2", Amount = 3,
            },
        };

        // Dropped by the cow or made from her: milk and beef.
        private readonly DropItem[] _drops =
        {
            new DropItem { Key = "Milch", PrefabName = "DF_Milch", BasePrefab = "MeadStaminaMinor", Weight = 0.5f, StackSize = 20 },
            new DropItem { Key = "Rindfleisch", PrefabName = "DF_Rindfleisch", BasePrefab = "LoxMeat", Weight = 0.5f, StackSize = 30 },
        };

        private readonly CowSettings _cow = new CowSettings();
        private readonly Drink _boonekamp = new Drink();

        private readonly Dish[] _dishes =
        {
            // Health food: Döner and cheese baked onto fries, in an aluminium tray.
            new Dish
            {
                Key = "Teller", RawPrefab = "DF_TellerRaw", MaterialPrefab = "DF_Teller_Material", FeastPrefab = "DF_FeastTeller",
                BaseMaterial = "FeastMeadows_Material", BaseFeast = "FeastMeadows",
                Ingredients = "DF_Doener:6,DF_Kaese:4,Carrot:5,Onion:3,MushroomJotunPuffs:2,SpiceMountains:1",
                Health = 100f, Stamina = 50f, Regen = 7f, BurnSeconds = 3600f,
            },
            // Balanced and very filling: a big pizza with plenty of Döner meat.
            new Dish
            {
                Key = "Pizza", RawPrefab = "DF_PizzaRaw", MaterialPrefab = "DF_Pizza_Material", FeastPrefab = "DF_FeastPizza",
                BaseMaterial = "FeastBlackforest_Material", BaseFeast = "FeastBlackforest",
                Ingredients = "BreadDough:4,DF_Doener:8,DF_Kaese:6,Cloudberry:4,MushroomMagecap:2,Onion:2,SpiceMountains:1",
                Health = 80f, Stamina = 80f, Regen = 6f, BurnSeconds = 4800f,
            },
        };

        private void Awake()
        {
            foreach (Ingredient ingredient in _ingredients)
            {
                ingredient.Bind(Config);
            }

            foreach (Dish dish in _dishes)
            {
                dish.Bind(Config);
            }

            _cow.Bind(Config);
            _boonekamp.Bind(Config);

            SE_Boonekamp.FeastFoods.AddRange(new[] { "$item_df_teller", "$item_df_pizza" });
            new Harmony(PluginGUID).PatchAll(typeof(VerlerLebenPlugin).Assembly);

            AddLocalizations();
            PrefabManager.OnVanillaPrefabsAvailable += AddContent;
        }

        private Dish Get(string key) => _dishes.First(d => d.Key == key);

        private void AddLocalizations()
        {
            Dish teller = Get("Teller"), pizza = Get("Pizza");
            CustomLocalization loc = LocalizationManager.Instance.GetLocalization();

            loc.AddTranslation("English", new Dictionary<string, string>
            {
                { "item_df_milch", "Milk" },
                { "item_df_milch_desc", "Fresh from the cow. Becomes cheese at the preparation table." },
                { "item_df_rindfleisch", "Beef" },
                { "item_df_rindfleisch_desc", "Raw beef from a cow. The base of Döner meat." },
                { "df_kuh", "Cow" },
                { "df_kalb", "Calf" },
                { "df_milk_action", "Milk" },
                { "df_cow_ready", "Milk ready" },
                { "df_cow_done_short", "Milked for today" },
                { "df_cow_done", "She has been milked already. Come back later." },
                { "df_cow_hungry", "The cow is hungry. Feed her Barley or Flax first." },
                { "item_df_boonekamp", "Schroeders Boonekamp" },
                { "item_df_boonekamp_desc", "Herbal bitter from Verl: 34 herbs, roots and fruits, 42 % vol. \"For cold beer and good food.\" More regeneration, even more with a feast, mead or barley wine." },
                { "se_df_boonekamp", "Verler Heimatwasser" },
                { "se_df_boonekamp_tooltip", "Herbal, bitter and warming. Your stomach thanks you. Stronger with a hearty meal, a mead or barley wine." },
                { "se_df_boonekamp_start", "Prost!" },
                { "se_df_boonekamp_stop", "The warmth fades." },
                { "item_df_doener", "Döner Meat" },
                { "item_df_doener_desc", "Thin slices of beef and lox meat from the spit. The base of every good feast." },
                { "item_df_kaese", "Cheese" },
                { "item_df_kaese_desc", "Barley cheese, made from barley milk like the oat milk of the north. Melts beautifully." },

                { "item_df_teller_raw", "Kaunitzer Plate (raw)" },
                { "item_df_teller_raw_desc", "Döner and fries in an aluminium tray, covered in cheese. Needs to be baked in the stone oven." },
                { "item_df_teller", "Kaunitzer Plate" },
                { "item_df_teller_desc", $"Döner with fries, baked under a thick layer of cheese until the edges are almost black. Lots of health and some stamina for {Minutes(teller)} minutes." },

                { "item_df_pizza_raw", "Spexarder Pizza (raw)" },
                { "item_df_pizza_raw_desc", "Pizza dough with sauce, cheese and a mountain of Döner meat. Needs to be baked in the stone oven." },
                { "item_df_pizza", "Spexarder Pizza" },
                { "item_df_pizza_desc", $"Greasy, hearty and very filling. Plenty of Döner meat under golden cheese. Health and stamina for {Minutes(pizza)} minutes." },
            });

            loc.AddTranslation("German", new Dictionary<string, string>
            {
                { "item_df_milch", "Milch" },
                { "item_df_milch_desc", "Frisch von der Kuh. Wird am Zubereitungstisch zu Käse." },
                { "item_df_rindfleisch", "Rindfleisch" },
                { "item_df_rindfleisch_desc", "Rohes Rindfleisch von der Kuh. Die Grundlage fürs Dönerfleisch." },
                { "df_kuh", "Kuh" },
                { "df_kalb", "Kalb" },
                { "df_milk_action", "Melken" },
                { "df_cow_ready", "Milch bereit" },
                { "df_cow_done_short", "Heute schon gemolken" },
                { "df_cow_done", "Sie wurde schon gemolken. Komm später wieder." },
                { "df_cow_hungry", "Die Kuh hat Hunger. Füttere sie zuerst mit Gerste oder Flachs." },
                { "item_df_boonekamp", "Schroeders Boonekamp" },
                { "item_df_boonekamp_desc", "Kräuterbitter aus Verl: 34 Kräuter, Wurzeln und Früchte, 42 % Vol. „Zu kühlem Bier und gutem Essen.“ Mehr Regeneration, noch mehr mit Feast, Met oder Gerstenwein." },
                { "se_df_boonekamp", "Verler Heimatwasser" },
                { "se_df_boonekamp_tooltip", "Herb, kräuterig und wärmend. Der Magen dankt es dir. Stärker mit deftigem Essen, Met oder Gerstenwein." },
                { "se_df_boonekamp_start", "Prost!" },
                { "se_df_boonekamp_stop", "Die Wärme lässt nach." },
                { "item_df_doener", "Dönerfleisch" },
                { "item_df_doener_desc", "Dünne Scheiben Rind- und Loxfleisch vom Spieß. Die Grundlage jedes guten Festmahls." },
                { "item_df_kaese", "Käse" },
                { "item_df_kaese_desc", "Gerstenkäse aus Gerstenmilch, so wie die Hafermilch im Norden. Schmilzt wunderbar." },

                { "item_df_teller_raw", "Kaunitzer Teller (roh)" },
                { "item_df_teller_raw_desc", "Döner und Pommes in der Aluschale, dick mit Käse bedeckt. Muss im Steinofen gebacken werden." },
                { "item_df_teller", "Kaunitzer Teller" },
                { "item_df_teller_desc", $"Döner mit Pommes, unter einer dicken Käseschicht überbacken, bis die Ränder fast schwarz sind. Viel Leben und etwas Ausdauer für {Minutes(teller)} Minuten." },

                { "item_df_pizza_raw", "Spexarder Pizza (roh)" },
                { "item_df_pizza_raw_desc", "Pizzateig mit Soße, Käse und einem Berg Dönerfleisch. Muss im Steinofen gebacken werden." },
                { "item_df_pizza", "Spexarder Pizza" },
                { "item_df_pizza_desc", $"Deftig, fettig und sehr sättigend. Ordentlich Dönerfleisch unter goldenem Käse. Leben und Ausdauer für {Minutes(pizza)} Minuten." },
            });
        }

        private static string Minutes(Dish dish) => Mathf.Max(1, Mathf.RoundToInt(dish.BurnSeconds / 60f)).ToString();

        private void AddContent()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddContent;

            foreach (Ingredient ingredient in _ingredients.Where(i => i.Enabled))
            {
                try
                {
                    AddIngredient(ingredient);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Failed to add {ingredient.PrefabName}: {e}");
                }
            }

            if (_cow.Enabled)
            {
                try
                {
                    foreach (DropItem drop in _drops)
                    {
                        AddDropItem(drop);
                    }

                    Cows.Add(_cow, Logger);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Failed to add the cow: {e}");
                }
            }

            if (_boonekamp.Enabled)
            {
                try
                {
                    AddDrink(_boonekamp);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Failed to add {_boonekamp.PrefabName}: {e}");
                }
            }

            foreach (Dish dish in _dishes.Where(d => d.Enabled))
            {
                try
                {
                    AddDish(dish);
                }
                catch (Exception e)
                {
                    Logger.LogError($"Failed to add {dish.FeastPrefab}: {e}");
                }
            }
        }

        private void AddDropItem(DropItem drop)
        {
            var item = new CustomItem(drop.PrefabName, drop.BasePrefab);
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_name = $"$item_{drop.Token}";
            shared.m_description = $"$item_{drop.Token}_desc";
            shared.m_icons = new[] { LoadIcon(drop.IconResource) };
            shared.m_weight = drop.Weight;
            shared.m_maxStackSize = drop.StackSize;
            MakePlainMaterial(item.ItemDrop);
            ItemManager.Instance.AddItem(item);
        }

        private void AddDrink(Drink drink)
        {
            Sprite icon = LoadIcon(drink.IconResource);
            StatusEffect baseEffect = PrefabManager.Instance.GetPrefab("MeadStaminaMinor")
                .GetComponent<ItemDrop>().m_itemData.m_shared.m_consumeStatusEffect;

            var effect = ScriptableObject.CreateInstance<SE_Boonekamp>();
            effect.name = "SE_" + drink.PrefabName;
            effect.m_name = $"$se_{drink.Token}";
            effect.m_tooltip = $"$se_{drink.Token}_tooltip";
            effect.m_category = "df_boonekamp";   // its own category, so it stacks with the meads
            effect.m_icon = icon;
            effect.m_ttl = drink.Duration;
            effect.m_startMessageType = MessageHud.MessageType.Center;
            effect.m_startMessage = $"$se_{drink.Token}_start";
            effect.m_stopMessageType = MessageHud.MessageType.TopLeft;
            effect.m_stopMessage = $"$se_{drink.Token}_stop";
            effect.m_healthRegenMultiplier = 1f + drink.HealthRegenBonus / 100f;
            effect.m_staminaRegenMultiplier = 1f + drink.StaminaRegenBonus / 100f;
            effect.FeastHealth = drink.FeastHealthRegenBonus / 100f;
            effect.FeastStamina = drink.FeastStaminaRegenBonus / 100f;
            effect.MeadHealth = drink.MeadHealthRegenBonus / 100f;
            effect.MeadStamina = drink.MeadStaminaRegenBonus / 100f;
            effect.BeerHealth = drink.BeerHealthRegenBonus / 100f;
            effect.BeerStamina = drink.BeerStaminaRegenBonus / 100f;
            if (baseEffect != null)
            {
                effect.m_startEffects = baseEffect.m_startEffects;
                effect.m_stopEffects = baseEffect.m_stopEffects;
            }

            ItemManager.Instance.AddStatusEffect(new CustomStatusEffect(effect, false));

            var config = new ItemConfig
            {
                Name = $"$item_{drink.Token}",
                Description = $"$item_{drink.Token}_desc",
                CraftingStation = CraftingStations.GetInternalName(drink.CraftingStation),
                Amount = Mathf.Max(1, drink.Amount),
                Requirements = ParseRequirements(drink.PrefabName, drink.Ingredients),
                Icon = icon,
                Weight = 0.5f,
                StackSize = 20,
            };
            var item = new CustomItem(drink.PrefabName, "MeadStaminaMinor", config);
            item.ItemDrop.m_itemData.m_shared.m_consumeStatusEffect = effect;
            item.ItemPrefab.transform.localScale *= 0.85f;
            ItemManager.Instance.AddItem(item);
        }

        private void AddIngredient(Ingredient ingredient)
        {
            var config = new ItemConfig
            {
                Name = $"$item_{ingredient.Token}",
                Description = $"$item_{ingredient.Token}_desc",
                CraftingStation = CraftingStations.GetInternalName(ingredient.CraftingStation),
                Amount = Mathf.Max(1, ingredient.Amount),
                Requirements = ParseRequirements(ingredient.PrefabName, ingredient.Ingredients),
                Icon = LoadIcon(ingredient.IconResource),
                Weight = ingredient.Weight,
                StackSize = ingredient.StackSize,
            };

            var item = new CustomItem(ingredient.PrefabName, ingredient.BasePrefab, config);
            MakePlainMaterial(item.ItemDrop);
            ItemManager.Instance.AddItem(item);
        }

        private void AddDish(Dish dish)
        {
            Sprite icon = LoadIcon(dish.IconResource);
            string name = $"$item_{dish.Token}";
            string description = $"$item_{dish.Token}_desc";

            // 1. Raw dish, crafted at the preparation table.
            var rawConfig = new ItemConfig
            {
                Name = $"$item_{dish.Token}_raw",
                Description = $"$item_{dish.Token}_raw_desc",
                CraftingStation = CraftingStations.GetInternalName(dish.CraftingStation),
                Amount = Mathf.Max(1, dish.Amount),
                Requirements = ParseRequirements(dish.RawPrefab, dish.Ingredients),
                Icon = LoadIcon(dish.RawIconResource),
                Weight = 5f,
                StackSize = 5,
            };
            var raw = new CustomItem(dish.RawPrefab, dish.BaseRaw, rawConfig);
            MakePlainMaterial(raw.ItemDrop);
            ItemManager.Instance.AddItem(raw);

            // 2. Baked dish, the item the Feaster tool consumes to put the feast on the table.
            var baked = new CustomItem(dish.MaterialPrefab, dish.BaseMaterial);
            ItemDrop.ItemData.SharedData bakedShared = baked.ItemDrop.m_itemData.m_shared;
            bakedShared.m_name = name;
            bakedShared.m_description = description;
            bakedShared.m_icons = new[] { icon };
            ItemManager.Instance.AddItem(baked);

            // 3. The feast itself: a piece with a Feast component that is also the food item.
            GameObject feast = PrefabManager.Instance.CreateClonedPrefab(dish.FeastPrefab, dish.BaseFeast);

            var piece = feast.GetComponent<Piece>();
            piece.m_name = name;
            piece.m_description = description;
            piece.m_icon = icon;
            piece.m_resources = new[]
            {
                new Piece.Requirement
                {
                    m_resItem = baked.ItemDrop, m_amount = 1, m_amountPerLevel = 1, m_recover = true,
                },
            };

            var food = feast.GetComponent<ItemDrop>().m_itemData.m_shared;
            food.m_name = name;
            food.m_description = description;
            food.m_icons = new[] { icon };
            food.m_food = dish.Health;
            food.m_foodStamina = dish.Stamina;
            food.m_foodEitr = 0f;
            food.m_foodRegen = dish.Regen;
            food.m_foodBurnTime = dish.BurnSeconds;

            feast.GetComponent<Feast>().m_eatStacks = Mathf.Max(1, dish.Servings);

            // The food is saved by prefab name, so it has to be in the ObjectDB like the vanilla feasts.
            ItemManager.Instance.AddItem(new CustomItem(feast, false));
            PieceManager.Instance.AddPiece(new CustomPiece(feast, FeasterTable, false));

            // 4. Baking in the stone oven turns the raw dish into the baked one.
            ItemManager.Instance.AddItemConversion(new CustomItemConversion(new CookingConversionConfig
            {
                FromItem = dish.RawPrefab,
                ToItem = dish.MaterialPrefab,
                Station = CookingStations.StoneOven,
                CookTime = Mathf.Max(1f, dish.BakeSeconds),
            }));

            Logger.LogInfo($"Added {dish.FeastPrefab} (model of {dish.BaseFeast}).");
        }

        /// <summary>Some base items are food; the clone must only be an ingredient.</summary>
        private static void MakePlainMaterial(ItemDrop drop)
        {
            ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
            shared.m_itemType = ItemDrop.ItemData.ItemType.Material;
            shared.m_food = 0f;
            shared.m_foodStamina = 0f;
            shared.m_foodEitr = 0f;
            shared.m_foodRegen = 0f;
            shared.m_foodBurnTime = 0f;
            shared.m_consumeStatusEffect = null;
        }

        private RequirementConfig[] ParseRequirements(string owner, string ingredients)
        {
            var requirements = new List<RequirementConfig>();
            foreach (string entry in ingredients.Split(','))
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
                    Logger.LogWarning($"Invalid amount in ingredient '{entry}', using 1.");
                    amount = 1;
                }

                // Our own ingredients are registered by now or soon; only vanilla names can be checked.
                if (!prefab.StartsWith("DF_") && PrefabManager.Instance.GetPrefab(prefab) == null)
                {
                    Logger.LogWarning($"{owner}: ingredient '{prefab}' was not found, check the prefab name.");
                }

                requirements.Add(new RequirementConfig(prefab, Mathf.Max(1, amount), 0, true));
            }

            return requirements.ToArray();
        }

        private static Sprite LoadIcon(string resource)
        {
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            using (var buffer = new MemoryStream())
            {
                stream.CopyTo(buffer);
                Texture2D texture = AssetUtils.LoadImage(buffer.ToArray());
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            }
        }
    }
}
