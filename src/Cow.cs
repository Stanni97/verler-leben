using System;
using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.Rendering;

namespace VerlerLeben
{
    /// <summary>Marks the cow prefab, so the milking patches know which Tameable they are looking at.</summary>
    internal class DfCow : MonoBehaviour
    {
    }

    /// <summary>
    /// The cow: a Lox that cannot attack anything. Without attack items MonsterAI only idles, so she wanders,
    /// eats Barley and Flax, gets tame, breeds and can be milked, and never breaks a fence.
    /// </summary>
    internal static class Cows
    {
        public const string CowPrefab = "DF_Kuh";
        public const string CalfPrefab = "DF_Kalb";
        public const string MilkPrefab = "DF_Milch";
        public const string BeefPrefab = "DF_Rindfleisch";

        internal static CowSettings Settings;

        public static void Add(CowSettings settings, ManualLogSource log)
        {
            Settings = settings;

            var calfConfig = new CreatureConfig
            {
                DropConfigs = new[]
                {
                    new DropConfig { Item = BeefPrefab, MinAmount = 1, MaxAmount = 1, Chance = 100f, LevelMultiplier = false },
                },
            };
            var calf = new CustomCreature(CalfPrefab, "Lox_Calf", calfConfig);
            if (calf.Prefab == null)
            {
                log.LogError("Lox_Calf not found, the cow is not added.");
                return;
            }

            var cowConfig = new CreatureConfig
            {
                Consumables = new[] { "Barley", "Flax" },
                DropConfigs = new[]
                {
                    new DropConfig { Item = BeefPrefab, MinAmount = 2, MaxAmount = 3, Chance = 100f, LevelMultiplier = false },
                    new DropConfig { Item = "LeatherScraps", MinAmount = 1, MaxAmount = 2, Chance = 100f, LevelMultiplier = false },
                },
                SpawnConfigs = new[]
                {
                    new SpawnConfig
                    {
                        Name = "DF_Kuh_Meadows",
                        Biome = Heightmap.Biome.Meadows,
                        MaxSpawned = Mathf.Max(1, settings.SpawnMax),
                        SpawnInterval = 30f,
                        SpawnChance = Mathf.Clamp(settings.SpawnChance, 1f, 100f),
                        MinGroupSize = Mathf.Max(1, settings.GroupMin),
                        MaxGroupSize = Mathf.Max(settings.GroupMin, settings.GroupMax),
                        GroupRadius = 6f,
                        MinAltitude = 1f,
                    },
                },
            };
            var cow = new CustomCreature(CowPrefab, "Lox", cowConfig);
            if (cow.Prefab == null)
            {
                log.LogError("Lox not found, the cow is not added.");
                return;
            }

            ConfigureCalf(calf.Prefab, cow.Prefab, settings);
            ConfigureCow(cow.Prefab, calf.Prefab, settings);

            try
            {
                // The real cow model; the repainted Lox is the fallback if the model does not fit.
                if (!CowModel.Apply(cow.Prefab) || !CowModel.Apply(calf.Prefab))
                {
                    log.LogWarning("Cow model could not be applied, using the painted Lox.");
                    ApplyCowPattern(calf.Prefab, 17);
                    ApplyCowPattern(cow.Prefab, 17);
                }
            }
            catch (Exception e)
            {
                log.LogWarning("Could not paint the cow, she keeps the Lox fur: " + e.Message);
            }

            CreatureManager.Instance.AddCreature(calf);
            CreatureManager.Instance.AddCreature(cow);
            log.LogInfo("Added " + CowPrefab + " and " + CalfPrefab + ".");
        }

        private static void ConfigureCow(GameObject cow, GameObject calf, CowSettings settings)
        {
            cow.AddComponent<DfCow>();
            cow.transform.localScale = Vector3.one * settings.Scale;

            var humanoid = cow.GetComponent<Humanoid>();
            humanoid.m_name = "$df_kuh";
            humanoid.m_group = "df_cow";
            humanoid.m_health = settings.Health;
            // No bite and no stomp: nothing a cow does can damage a player or a building.
            humanoid.m_defaultItems = new GameObject[0];
            humanoid.m_randomWeapon = new GameObject[0];

            var ai = cow.GetComponent<MonsterAI>();
            ai.m_attackPlayerObjects = false;
            ai.m_enableHuntPlayer = false;

            var tameable = cow.GetComponent<Tameable>();
            tameable.m_tamingTime = settings.TamingSeconds;
            tameable.m_fedDuration = settings.FedSeconds;
            tameable.m_commandable = false;

            var procreation = cow.GetComponent<Procreation>();
            procreation.m_offspring = calf;
            procreation.m_maxCreatures = 4;
            procreation.m_pregnancyChance = 0.5f;
        }

        private static void ConfigureCalf(GameObject calf, GameObject cow, CowSettings settings)
        {
            calf.transform.localScale = Vector3.one * (settings.Scale * 0.75f);

            var character = calf.GetComponent<Character>();
            character.m_name = "$df_kalb";
            character.m_group = "df_cow";

            var growup = calf.GetComponent<Growup>();
            growup.m_grownPrefab = cow;
            growup.m_altGrownPrefabs = new List<Growup.GrownEntry>();
            growup.m_growTime = settings.GrowSeconds;
        }

        // ---- looks: smooth short coat in white with black patches instead of the shaggy Lox fur

        private static void ApplyCowPattern(GameObject prefab, int seed)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                return;   // dedicated server, nothing is drawn there
            }

            // The shaggy fur is a second mesh with fur cards (Furr1). Without it the body shows its smooth coat.
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.gameObject.name.IndexOf("fur", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    renderer.gameObject.SetActive(false);
                }
            }

            var painted = new Dictionary<Texture, Texture2D>();
            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is ParticleSystemRenderer || !renderer.gameObject.activeSelf)
                {
                    continue;
                }

                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || source.mainTexture == null || !source.HasProperty("_MainTex"))
                    {
                        continue;
                    }

                    if (!painted.TryGetValue(source.mainTexture, out Texture2D texture))
                    {
                        texture = Paint(source.mainTexture, seed);
                        painted[source.mainTexture] = texture;
                    }

                    // A copy: the shared material belongs to the real Lox.
                    var copy = new Material(source) { mainTexture = texture };
                    if (copy.HasProperty("_EmissionColor"))
                    {
                        copy.SetColor("_EmissionColor", Color.black);   // the Lox eye glows orange, a cow's does not
                    }

                    materials[i] = copy;
                }

                renderer.sharedMaterials = materials;
            }
        }

        /// <summary>
        /// Repaints the texture atlas of the Lox by colour: brown fur becomes a white coat with black patches,
        /// grey parts (hooves, horns) become dark, pink skin gets lighter and the glowing blue eye turns dark brown.
        /// </summary>
        private static Texture2D Paint(Texture source, int seed)
        {
            int width = Mathf.Min(source.width * 2, 1024), height = Mathf.Min(source.height * 2, 1024);
            RenderTexture target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                var result = new Texture2D(width, height, TextureFormat.RGBA32, true);
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                Color32[] pixels = result.GetPixels32();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = y * width + x;
                        pixels[index] = Recolor(pixels[index], x / (float)width, y / (float)height, seed);
                    }
                }

                result.SetPixels32(pixels);
                result.Apply(true);
                result.wrapMode = source.wrapMode;
                return result;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(target);
            }
        }

        private static Color32 Recolor(Color32 c, float u, float v, int seed)
        {
            int lum = (c.r * 3 + c.g * 6 + c.b) / 10;
            if (c.a < 8 || lum < 14)
            {
                return c;   // background and deep shadows stay
            }

            bool blue = c.b > c.r + 8 && c.b > c.g - 4;
            bool pink = c.r > c.g + 28 && c.b > c.g + 8;
            bool grey = Mathf.Abs(c.r - c.g) < 22 && Mathf.Abs(c.g - c.b) < 22;
            if (blue)
            {
                return new Color32(34, 24, 20, c.a);                                          // eye
            }

            if (pink)
            {
                float k = 0.8f + lum / 400f;                                                  // nose and skin
                return new Color32((byte)Mathf.Min(255, 236 * k), (byte)Mathf.Min(255, 178 * k), (byte)Mathf.Min(255, 176 * k), c.a);
            }

            if (grey)
            {
                byte dark = (byte)Mathf.Clamp(lum * 0.45f + 14f, 0f, 255f);                   // hooves and horns
                return new Color32(dark, dark, (byte)(dark + 3), c.a);
            }

            // coat: black patches from two noise layers, white everywhere else, the fur detail shines through
            float noise = Mathf.PerlinNoise(u * 7f + seed, v * 7f) * 0.75f + Mathf.PerlinNoise(u * 19f + seed * 1.7f, v * 19f) * 0.25f;
            float detail = Mathf.Clamp(0.8f + lum / 500f, 0.7f, 1.05f);
            if (noise > 0.56f)
            {
                float k = 0.65f + lum / 330f;
                return new Color32((byte)Mathf.Min(255, 30 * k), (byte)Mathf.Min(255, 28 * k), (byte)Mathf.Min(255, 32 * k), c.a);
            }

            return new Color32((byte)Mathf.Min(255, 240 * detail), (byte)Mathf.Min(255, 234 * detail), (byte)Mathf.Min(255, 224 * detail), c.a);
        }
    }

    /// <summary>Settings of the cow, from the config file.</summary>
    internal sealed class CowSettings
    {
        public bool Enabled = true;
        public int SpawnMax = 3;
        public float SpawnChance = 50f;
        public int GroupMin = 2;
        public int GroupMax = 3;
        public float Scale = 0.65f;
        public float Health = 150f;
        public float TamingSeconds = 300f;
        public float FedSeconds = 900f;
        public float GrowSeconds = 1200f;
        public float MilkCooldownSeconds = 1800f;
        public int MilkMin = 1;
        public int MilkMax = 2;

        public void Bind(BepInEx.Configuration.ConfigFile config)
        {
            const string s = "Cow";
            Enabled = config.Bind(s, "Enabled", Enabled, "Adds the cow, her calf, milk and beef to the game.").Value;
            SpawnMax = config.Bind(s, "SpawnMax", SpawnMax, "How many cows spawn around you in the Meadows at most.").Value;
            SpawnChance = config.Bind(s, "SpawnChancePercent", SpawnChance, "Chance per spawn attempt, in percent.").Value;
            GroupMin = config.Bind(s, "GroupMin", GroupMin, "Smallest herd.").Value;
            GroupMax = config.Bind(s, "GroupMax", GroupMax, "Largest herd.").Value;
            Scale = config.Bind(s, "Scale", Scale, "Size compared to a Lox. The calf is three quarters of this.").Value;
            Health = config.Bind(s, "Health", Health, "Health of a cow.").Value;
            TamingSeconds = config.Bind(s, "TamingSeconds", TamingSeconds, "How long a fed cow needs to get tame (a Lox: 1800).").Value;
            FedSeconds = config.Bind(s, "FedSeconds", FedSeconds, "How long a meal of Barley or Flax keeps a cow fed.").Value;
            GrowSeconds = config.Bind(s, "CalfGrowSeconds", GrowSeconds, "How long a calf needs to grow up.").Value;
            MilkCooldownSeconds = config.Bind(s, "MilkCooldownSeconds", MilkCooldownSeconds, "Time between two milkings of the same cow (one game day is 1800 s).").Value;
            MilkMin = config.Bind(s, "MilkMin", MilkMin, "Least milk per milking.").Value;
            MilkMax = config.Bind(s, "MilkMax", MilkMax, "Most milk per milking.").Value;
        }
    }

    /// <summary>Milking: petting a tame, fed cow gives milk, once per cooldown.</summary>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
    internal static class MilkInteract
    {
        private const string LastMilkKey = "DF_lastMilk";

        [HarmonyPostfix]
        private static void Postfix(Tameable __instance, Humanoid user, bool hold, bool alt, bool __result)
        {
            if (!__result || hold || alt || Cows.Settings == null || __instance.GetComponent<DfCow>() == null)
            {
                return;
            }

            var nview = __instance.GetComponent<ZNetView>();
            var character = __instance.GetComponent<Character>();
            if (nview == null || !nview.IsValid() || character == null || !character.IsTamed())
            {
                return;
            }

            if (__instance.IsHungry())
            {
                user.Message(MessageHud.MessageType.Center, "$df_cow_hungry");
                return;
            }

            if (!MilkReady(nview))
            {
                user.Message(MessageHud.MessageType.Center, "$df_cow_done");
                return;
            }

            int amount = UnityEngine.Random.Range(Cows.Settings.MilkMin, Cows.Settings.MilkMax + 1);
            if (amount < 1)
            {
                return;
            }

            nview.ClaimOwnership();
            nview.GetZDO().Set(LastMilkKey, ZNet.instance.GetTime().Ticks);
            Give(user, amount);
        }

        internal static bool MilkReady(ZNetView nview)
        {
            long last = nview.GetZDO().GetLong(LastMilkKey, 0L);
            if (last == 0L)
            {
                return true;
            }

            double seconds = (ZNet.instance.GetTime() - new DateTime(last)).TotalSeconds;
            return seconds >= Cows.Settings.MilkCooldownSeconds;
        }

        private static void Give(Humanoid user, int amount)
        {
            GameObject prefab = ObjectDB.instance.GetItemPrefab(Cows.MilkPrefab);
            if (prefab == null)
            {
                return;
            }

            if (!user.GetInventory().AddItem(prefab, amount))
            {
                // Inventory full: the milk lands at the player's feet.
                GameObject drop = UnityEngine.Object.Instantiate(prefab, user.transform.position + user.transform.forward + Vector3.up, Quaternion.identity);
                var item = drop.GetComponent<ItemDrop>();
                if (item != null)
                {
                    item.m_itemData.m_stack = amount;
                }
            }

            user.Message(MessageHud.MessageType.Center, Localization.instance.Localize("$item_df_milch") + " x" + amount);
        }
    }

    /// <summary>Hover text of a tame cow: "Milk" instead of "Pet", and whether she can be milked.</summary>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.GetHoverText))]
    internal static class MilkHover
    {
        [HarmonyPostfix]
        private static void Postfix(Tameable __instance, ref string __result)
        {
            if (Cows.Settings == null || __instance.GetComponent<DfCow>() == null)
            {
                return;
            }

            var nview = __instance.GetComponent<ZNetView>();
            var character = __instance.GetComponent<Character>();
            if (nview == null || !nview.IsValid() || character == null || !character.IsTamed())
            {
                return;
            }

            Localization loc = Localization.instance;
            __result = __result.Replace(loc.Localize("$hud_pet"), loc.Localize("$df_milk_action"));
            string status = MilkInteract.MilkReady(nview) ? "$df_cow_ready" : "$df_cow_done_short";
            __result += loc.Localize("\n<color=#bbbbbb>" + status + "</color>");
        }
    }
}
