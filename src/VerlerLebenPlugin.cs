using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
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
        public const string PluginVersion = "1.0.0";

        // The piece table of the Feaster tool, the one the vanilla biome feasts are in.
        private const string FeasterTable = "_FeasterPieceTable";

        private readonly Ingredient[] _ingredients =
        {
            new Ingredient
            {
                Key = "Doener", PrefabName = "DF_Doener", BasePrefab = "LoxMeat",
                Ingredients = "CookedLoxMeat:2,SpicePlains:1", Amount = 2,
            },
            new Ingredient
            {
                Key = "Kaese", PrefabName = "DF_Kaese", BasePrefab = "BreadDough",
                Ingredients = "LoxMeat:1", Amount = 2,
            },
        };

        private readonly Dish[] _dishes =
        {
            // Health food: Döner and cheese baked onto fries, in an aluminium tray.
            new Dish
            {
                Key = "Teller", RawPrefab = "DF_TellerRaw", MaterialPrefab = "DF_Teller_Material", FeastPrefab = "DF_FeastTeller",
                BaseMaterial = "FeastMeadows_Material", BaseFeast = "FeastMeadows",
                Ingredients = "DF_Doener:4,DF_Kaese:3,Carrot:4",
                Health = 80f, Stamina = 40f, Regen = 6f, BurnSeconds = 3000f,
            },
            // Balanced and very filling: a big pizza with plenty of Döner meat.
            new Dish
            {
                Key = "Pizza", RawPrefab = "DF_PizzaRaw", MaterialPrefab = "DF_Pizza_Material", FeastPrefab = "DF_FeastPizza",
                BaseMaterial = "FeastBlackforest_Material", BaseFeast = "FeastBlackforest",
                Ingredients = "BreadDough:3,DF_Doener:6,DF_Kaese:4,Raspberry:3",
                Health = 65f, Stamina = 65f, Regen = 4f, BurnSeconds = 4200f,
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
                { "item_df_doener", "Döner Meat" },
                { "item_df_doener_desc", "Thin slices of lox meat from the spit, well seasoned. The base of every good feast." },
                { "item_df_kaese", "Cheese" },
                { "item_df_kaese_desc", "Melts beautifully. Made from lox milk, don't ask." },

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
                { "item_df_doener", "Dönerfleisch" },
                { "item_df_doener_desc", "Dünne, gut gewürzte Scheiben Loxfleisch vom Spieß. Die Grundlage jedes guten Festmahls." },
                { "item_df_kaese", "Käse" },
                { "item_df_kaese_desc", "Schmilzt wunderbar. Aus Loxmilch gemacht, frag nicht." },

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
