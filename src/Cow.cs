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
    /// <summary>Marks the cow prefab, so the milking patches know which Tameable they are looking at. A bull is no DfCow.</summary>
    internal class DfCow : DfFeeder
    {
    }

    /// <summary>A calf close to Momo grows up faster: its birth time moves back a little every few seconds.</summary>
    internal class DfCalf : MonoBehaviour
    {
        private const float Interval = 10f;
        private ZNetView _nview;

        private void Start()
        {
            _nview = GetComponent<ZNetView>();
            InvokeRepeating(nameof(Tick), UnityEngine.Random.Range(5f, 10f), Interval);
        }

        private void Tick()
        {
            CowSettings settings = Cows.Settings;
            if (settings == null || settings.CalfGrowthBonus <= 0f || _nview == null || !_nview.IsValid() || !_nview.IsOwner()
                || Bulls.Living.Count == 0)
            {
                return;
            }

            float range = Mathf.Max(1f, settings.CalfGrowthRange);
            bool near = false;
            foreach (Character bull in Bulls.Living)
            {
                if (bull != null && !bull.IsDead() && Vector3.Distance(bull.transform.position, transform.position) <= range)
                {
                    near = true;
                    break;
                }
            }

            if (!near)
            {
                return;
            }

            ZDO zdo = _nview.GetZDO();
            long born = zdo.GetLong(ZDOVars.s_spawnTime, 0L);
            if (born > 0L)
            {
                zdo.Set(ZDOVars.s_spawnTime, born - (long)(Interval * settings.CalfGrowthBonus / 100f * TimeSpan.TicksPerSecond));
            }
        }
    }

    /// <summary>Feeding behaviour shared by cows and bulls.</summary>
    internal class DfFeeder : MonoBehaviour
    {
        private MonsterAI _ai;
        private Character _character;
        private Tameable _tameable;
        private ZNetView _nview;
        private float _timer;
        private float _lastLog = -1000f;

        protected virtual void Awake()
        {
            _ai = GetComponent<MonsterAI>();
            _character = GetComponent<Character>();
            _tameable = GetComponent<Tameable>();
            _nview = GetComponent<ZNetView>();
            _timer = UnityEngine.Random.Range(0f, 3f);
        }

        /// <summary>
        /// A hungry tame cow eats food lying around, but the game skips that while she is alerted or has a target
        /// (a monster that wandered by, a stray animal). She cannot fight, so she lets it go and goes for the food.
        /// </summary>
        protected virtual void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f)
            {
                return;
            }

            _timer = 3f;
            if (_ai == null || _character == null || _tameable == null || _nview == null || !_nview.IsValid() || !_nview.IsOwner()
                || !_character.IsTamed() || !_tameable.IsHungry() || _ai.m_consumeItems == null)
            {
                return;
            }

            ItemDrop food = FindFood();
            if (food == null)
            {
                return;
            }

            Traverse ai = Traverse.Create(_ai);
            bool blocked = _ai.IsAlerted() || ai.Field("m_targetCreature").GetValue() != null || ai.Field("m_targetStatic").GetValue() != null;
            if (blocked)
            {
                ai.Method("SetAlerted", new object[] { false }).GetValue();
                ai.Field("m_targetCreature").SetValue(null);
                ai.Field("m_targetStatic").SetValue(null);
            }

            if (Time.time - _lastLog > 60f)
            {
                _lastLog = Time.time;
                ServerSync.Log?.LogInfo($"Cow is hungry and {food.m_itemData.m_shared.m_name} lies {Vector3.Distance(food.transform.position, transform.position):0.#} m away" +
                                               (blocked ? " (she was alerted or had a target, reset)." : "."));
            }
        }

        private ItemDrop FindFood()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, Mathf.Max(_ai.m_consumeSearchRange, 5f), LayerMask.GetMask("item"));
            ItemDrop best = null;
            float distance = float.MaxValue;
            foreach (Collider hit in hits)
            {
                if (hit.attachedRigidbody == null)
                {
                    continue;
                }

                var item = hit.attachedRigidbody.GetComponent<ItemDrop>();
                if (item == null || item.m_itemData == null)
                {
                    continue;
                }

                foreach (ItemDrop eats in _ai.m_consumeItems)
                {
                    if (eats != null && eats.m_itemData.m_shared.m_name == item.m_itemData.m_shared.m_name)
                    {
                        float d = Vector3.Distance(item.transform.position, transform.position);
                        if (d < distance)
                        {
                            distance = d;
                            best = item;
                        }

                        break;
                    }
                }
            }

            return best;
        }
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

        private static CustomCreature _cow;
        private static GameObject _cowPrefab, _calfPrefab, _bullPrefab, _cowRagdoll, _calfRagdoll;

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
                        Name = "DF_Kuh_Spawn",
                        Biome = settings.SpawnBiome,
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

            ConfigureCow(cow.Prefab, calf.Prefab, settings);
            Effects.Prepare(cow.Prefab, "cow", settings.Sounds, log);
            Effects.Prepare(calf.Prefab, "calf", settings.Sounds, log);
            GameObject bull = Bulls.Add(settings, log);
            ConfigureCalf(calf.Prefab, cow.Prefab, bull, settings);
            _bullPrefab = bull;

            try
            {
                // The real cow model; the repainted Lox is the fallback if the model does not fit.
                if (!CowModel.Apply(cow.Prefab) || !CowModel.Apply(calf.Prefab))
                {
                    log.LogWarning("Cow model could not be applied, using the painted Lox.");
                    ApplyCowPattern(calf.Prefab, 17);
                    ApplyCowPattern(cow.Prefab, 17);
                }
                else
                {
                    // On death the game puts a ragdoll in place of the creature. Without our own, a Lox would fall over.
                    _cowRagdoll = ReplaceRagdoll(cow.Prefab, "lox_ragdoll", CowPrefab + "_ragdoll", settings.Scale, log);
                    _calfRagdoll = ReplaceRagdoll(calf.Prefab, "loxcalf_ragdoll", CalfPrefab + "_ragdoll", settings.Scale * 0.75f, log);
                }
            }
            catch (Exception e)
            {
                log.LogWarning("Could not paint the cow, she keeps the Lox fur: " + e.Message);
            }

            CreatureManager.Instance.AddCreature(calf);
            CreatureManager.Instance.AddCreature(cow);
            _cow = cow;
            _cowPrefab = cow.Prefab;
            _calfPrefab = calf.Prefab;
            log.LogInfo("Added " + CowPrefab + " and " + CalfPrefab + ".");
        }

        /// <summary>
        /// Applies changed settings (e.g. from the server): the spawn, the prefabs for new cows and the cows already loaded.
        /// Turned off, cows no longer spawn; tame ones stay.
        /// </summary>
        public static void Apply(CowSettings settings)
        {
            Settings = settings;
            if (_cow == null)
            {
                return;
            }

            foreach (SpawnSystem.SpawnData spawn in _cow.Spawns)
            {
                spawn.m_enabled = settings.Enabled;
                spawn.m_biome = settings.SpawnBiome;
                spawn.m_maxSpawned = Mathf.Max(1, settings.SpawnMax);
                spawn.m_spawnChance = Mathf.Clamp(settings.SpawnChance, 1f, 100f);
                spawn.m_groupSizeMin = Mathf.Max(1, settings.GroupMin);
                spawn.m_groupSizeMax = Mathf.Max(settings.GroupMin, settings.GroupMax);
            }

            _cowPrefab.transform.localScale = Vector3.one * settings.Scale;
            _calfPrefab.transform.localScale = Vector3.one * (settings.Scale * 0.75f);
            if (_cowRagdoll != null)
            {
                _cowRagdoll.transform.localScale = Vector3.one * settings.Scale;
            }

            if (_calfRagdoll != null)
            {
                _calfRagdoll.transform.localScale = Vector3.one * (settings.Scale * 0.75f);
            }

            _cowPrefab.GetComponent<Humanoid>().m_health = settings.Health;
            SetTaming(_cowPrefab.GetComponent<Tameable>(), settings);
            SetBreeding(_cowPrefab.GetComponent<Procreation>(), settings);
            SetGrowup(_calfPrefab.GetComponent<Growup>(), _cowPrefab, _bullPrefab, settings);
            Bulls.Apply(settings);

            foreach (Character character in Character.GetAllCharacters())
            {
                if (character == null)
                {
                    continue;
                }

                if (character.GetComponent<DfCow>() != null)
                {
                    SetTaming(character.GetComponent<Tameable>(), settings);
                    SetBreeding(character.GetComponent<Procreation>(), settings);
                }
                else if (character.GetComponent<Growup>() is Growup growup && growup.m_grownPrefab == _cowPrefab)
                {
                    SetGrowup(growup, _cowPrefab, _bullPrefab, settings);
                }
            }
        }

        private static void SetBreeding(Procreation procreation, CowSettings settings)
        {
            if (procreation != null)
            {
                procreation.m_maxCreatures = settings.BreedLimit <= 0 ? int.MaxValue : settings.BreedLimit;
                procreation.m_pregnancyChance = Mathf.Clamp01(settings.BreedChance / 100f);
            }
        }

        private static void SetTaming(Tameable tameable, CowSettings settings)
        {
            if (tameable != null)
            {
                tameable.m_tamingTime = settings.TamingSeconds;
                tameable.m_fedDuration = settings.FedSeconds;
            }
        }

        /// <summary>A copy of the Lox ragdoll with the cow model, in the death effects of the cow instead of the original.</summary>
        internal static GameObject ReplaceRagdoll(GameObject creature, string originalName, string name, float scale, ManualLogSource log, bool momo = false)
        {
            GameObject original = PrefabManager.Instance.GetPrefab(originalName);
            var character = creature.GetComponent<Character>();
            if (original == null || character == null)
            {
                log.LogWarning("Ragdoll " + originalName + " not found, the " + creature.name + " falls over as a Lox.");
                return null;
            }

            GameObject ragdoll = PrefabManager.Instance.CreateClonedPrefab(name, original);
            if (ragdoll == null || !CowModel.Apply(ragdoll, ragdoll: true, momo: momo))
            {
                log.LogWarning("Could not build the ragdoll " + name + ".");
                return null;
            }

            ragdoll.transform.localScale = Vector3.one * scale;
            Effects.Clean(ragdoll.GetComponent<Ragdoll>()?.m_removeEffect, null, null, log);
            PrefabManager.Instance.AddPrefab(ragdoll);
            foreach (EffectList.EffectData effect in character.m_deathEffects.m_effectPrefabs)
            {
                if (effect.m_prefab != null && effect.m_prefab.name == originalName)
                {
                    effect.m_prefab = ragdoll;
                }
            }

            return ragdoll;
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
            ai.m_consumeSearchRange = 15f;      // food lying within 15 m, looked for every 3 s (a Lox: 10 m, every 10 s)
            ai.m_consumeSearchInterval = 3f;
            ai.m_consumeRange = 4f;
            // A Lox needs a path 2 m wide and 7 m high; a pen with a roof, a gate or a fence close by has none, so tame cows never
            // walked to food lying in it. A horse-sized path (1.6 m wide, 2.5 m high) fits a cow.
            ai.m_pathAgentType = Pathfinding.AgentType.HorseSize;

            var tameable = cow.GetComponent<Tameable>();
            tameable.m_tamingTime = settings.TamingSeconds;
            tameable.m_fedDuration = settings.FedSeconds;
            tameable.m_commandable = true;   // E makes her follow or wait, like a wolf; milking has its own key

            var procreation = cow.GetComponent<Procreation>();
            procreation.m_offspring = calf;
            SetBreeding(procreation, settings);
        }

        /// <summary>A calf grows into a cow, or with the bull chance into Momo.</summary>
        private static void SetGrowup(Growup growup, GameObject cow, GameObject bull, CowSettings settings)
        {
            growup.m_grownPrefab = cow;
            growup.m_growTime = settings.GrowSeconds;
            float chance = Mathf.Clamp(settings.BullChance, 0f, 100f);
            growup.m_altGrownPrefabs = bull != null && chance > 0f
                ? new List<Growup.GrownEntry>
                {
                    new Growup.GrownEntry { m_prefab = cow, m_weight = 100f - chance },
                    new Growup.GrownEntry { m_prefab = bull, m_weight = chance },
                }
                : new List<Growup.GrownEntry>();
        }

        private static void ConfigureCalf(GameObject calf, GameObject cow, GameObject bull, CowSettings settings)
        {
            calf.transform.localScale = Vector3.one * (settings.Scale * 0.75f);

            var character = calf.GetComponent<Character>();
            character.m_name = "$df_kalb";
            character.m_group = "df_cow";

            SetGrowup(calf.GetComponent<Growup>(), cow, bull, settings);
            if (calf.GetComponent<DfCalf>() == null)
            {
                calf.AddComponent<DfCalf>();
            }
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
        public Heightmap.Biome SpawnBiome = Heightmap.Biome.Plains;
        public int SpawnMax = 2;
        public float SpawnChance = 25f;
        public int GroupMin = 1;
        public int GroupMax = 2;
        public int BreedLimit;                // cows and calves within 10 m before breeding stops; 0 = no limit
        public float BreedChance = 50f;       // percent per attempt, one attempt every 10 s
        public float Scale = 0.65f;
        public float Health = 250f;
        public float TamingSeconds = 300f;
        public float FedSeconds = 900f;
        public float GrowSeconds = 1200f;
        public float MilkCooldownSeconds = 1800f;
        public KeyCode MilkKey = KeyCode.H;
        public int MilkMin = 1;
        public int MilkMax = 2;
        public int PetMilkBonus = 1;           // extra milk when the cow was petted lately
        public float PetMemorySeconds = 900f;  // how long a petting counts
        public int WellFedMilkBonus = 1;       // extra milk when she was fed lately
        public float WellFedPercent = 50f;     // "lately" = within this share of FedSeconds
        public int RipenYield = 2;             // aged cheese per cheese in the fermenter
        public float CalfGrowthBonus = 50f;    // percent faster growth of calves close to Momo
        public float CalfGrowthRange = 20f;    // how close, in metres
        public bool Sounds = true;             // moos instead of the Lox's grunts; always on, there is no option
        public float BullChance = 25f;         // percent of the calves that grow into Momo
        public float BullHealth = 1200f;       // a Lox: 1000
        public float BullScale = 0.9f;
        public float BullDamage = 1.5f;        // times what a Lox hits for
        public float BullGuardRange = 40f;     // how far Momo sees a monster coming (a Lox: 25)

        public void Bind(BepInEx.Configuration.ConfigFile config)
        {
            const string s = "Cow";
            Enabled = config.Bind(s, "Enabled", Enabled, ServerSync.Desc("Cows spawn in the wild. Off: no new cows spawn; tame cows, milk and beef stay.")).Value;
            SpawnBiome = config.Bind(s, "SpawnBiome", SpawnBiome,
                ServerSync.Desc("Where the cows live. Plains is where Barley and Flax, their food, grow. Meadows, BlackForest, Swamp, Mountain or Mistlands work too.")).Value;
            SpawnMax = config.Bind(s, "SpawnMax", SpawnMax, ServerSync.Desc("How many cows spawn around you in that biome at most.")).Value;
            SpawnChance = config.Bind(s, "SpawnChancePercent", SpawnChance, ServerSync.Desc("Chance per spawn attempt, in percent.")).Value;
            GroupMin = config.Bind(s, "GroupMin", GroupMin, ServerSync.Desc("Smallest herd.")).Value;
            GroupMax = config.Bind(s, "GroupMax", GroupMax, ServerSync.Desc("Largest herd.")).Value;
            Scale = config.Bind(s, "Scale", Scale, ServerSync.Desc("Size compared to a Lox. The calf is three quarters of this.")).Value;
            Health = config.Bind(s, "Health", Health, ServerSync.Desc("Health of a cow.")).Value;
            TamingSeconds = config.Bind(s, "TamingSeconds", TamingSeconds, ServerSync.Desc("How long a fed cow needs to get tame (a Lox: 1800).")).Value;
            FedSeconds = config.Bind(s, "FedSeconds", FedSeconds, ServerSync.Desc("How long a meal of Barley or Flax keeps a cow fed.")).Value;
            GrowSeconds = config.Bind(s, "CalfGrowSeconds", GrowSeconds, ServerSync.Desc("How long a calf needs to grow up.")).Value;
            MilkCooldownSeconds = config.Bind(s, "MilkCooldownSeconds", MilkCooldownSeconds, ServerSync.Desc("Time between two milkings of the same cow (one game day is 1800 s).")).Value;
            BreedLimit = config.Bind(s, "BreedLimit", BreedLimit,
                ServerSync.Desc("Cows stop breeding when this many cows and calves stand within 20 m of each other. 0 = no limit (a Lox: 4, a wolf: 4, a boar: 5, a hen: 10).")).Value;
            BreedChance = config.Bind(s, "BreedChancePercent", BreedChance,
                ServerSync.Desc("Chance per attempt (every 30 s) that two fed, tame cows close to each other (within 8 m) start a pregnancy, in percent (a Lox: 33).")).Value;
            MilkKey = config.Bind(s, "MilkKey", MilkKey,
                "Key to milk the cow you look at. E makes her follow you or wait, like a wolf. Not used by the game: H, Y, U, J, N.").Value;
            MilkMin = config.Bind(s, "MilkMin", MilkMin, ServerSync.Desc("Least milk per milking.")).Value;
            MilkMax = config.Bind(s, "MilkMax", MilkMax, ServerSync.Desc("Most milk per milking.")).Value;

            PetMilkBonus = config.Bind(s, "PetMilkBonus", PetMilkBonus,
                ServerSync.Desc("Extra milk when you petted the cow (E) shortly before milking. 0 = no bonus.")).Value;
            PetMemorySeconds = config.Bind(s, "PetMemorySeconds", PetMemorySeconds,
                ServerSync.Desc("How long after petting the cow counts as content, in seconds.")).Value;
            WellFedMilkBonus = config.Bind(s, "WellFedMilkBonus", WellFedMilkBonus,
                ServerSync.Desc("Extra milk when the cow was fed recently. 0 = no bonus.")).Value;
            WellFedPercent = config.Bind(s, "WellFedPercent", WellFedPercent,
                ServerSync.Desc("A cow counts as well fed within this share of FedSeconds after her last meal, in percent.")).Value;
            RipenYield = config.Bind("Kaese", "RipenedPerCheese", RipenYield,
                ServerSync.Desc("Aged cheese that one cheese turns into in the fermenter (it ripens as long as mead does).")).Value;

            const string b = "Bull";
            BullChance = config.Bind(b, "BullChancePercent", BullChance,
                ServerSync.Desc("Chance that a calf grows into Momo, the bull, instead of a cow, in percent. 0 = no bulls.")).Value;
            BullHealth = config.Bind(b, "Health", BullHealth, ServerSync.Desc("Health of Momo (a Lox: 1000).")).Value;
            BullScale = config.Bind(b, "Scale", BullScale, ServerSync.Desc("Size of Momo compared to a Lox.")).Value;
            BullDamage = config.Bind(b, "DamageMultiplier", BullDamage,
                ServerSync.Desc("Momo hits like a Lox times this. He only fights monsters, or a player who attacked him first.")).Value;
            CalfGrowthBonus = config.Bind(b, "CalfGrowthBonusPercent", CalfGrowthBonus,
                ServerSync.Desc("Calves close to Momo grow up this much faster, in percent. 0 = no effect.")).Value;
            CalfGrowthRange = config.Bind(b, "CalfGrowthRangeMetres", CalfGrowthRange,
                ServerSync.Desc("How close a calf has to be to Momo for that.")).Value;
            BullGuardRange = config.Bind(b, "GuardRangeMetres", BullGuardRange,
                ServerSync.Desc("How far away Momo notices a monster that comes near the herd (a Lox: 25).")).Value;
        }
    }

    /// <summary>Milking a tame, fed cow with the milk key gives milk, once per cooldown.</summary>
    internal static class Milking
    {
        private const string LastMilkKey = "DF_lastMilk";

        private static readonly AccessTools.FieldRef<Player, GameObject> Hovering =
            AccessTools.FieldRefAccess<Player, GameObject>("m_hovering");

        private static readonly System.Reflection.MethodInfo TakeInput = AccessTools.Method(typeof(Player), "TakeInput");

        /// <summary>The tame cow the player looks at, or null.</summary>
        internal static Tameable HoveredCow(Player player)
        {
            GameObject hovering = Hovering(player);
            Tameable tameable = hovering != null ? hovering.GetComponentInParent<Tameable>() : null;
            if (tameable == null || tameable.GetComponent<DfCow>() == null)
            {
                return null;
            }

            var character = tameable.GetComponent<Character>();
            return character != null && character.IsTamed() ? tameable : null;
        }

        internal static bool InputAllowed(Player player)
        {
            return TakeInput == null || (bool)TakeInput.Invoke(player, null);
        }

        internal static void TryMilk(Tameable cow, Humanoid user)
        {
            var nview = cow.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return;
            }

            if (cow.IsHungry())
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

            amount += Math.Max(0, Cows.Settings.PetMilkBonus) * (Petted(nview) ? 1 : 0);
            amount += Math.Max(0, Cows.Settings.WellFedMilkBonus) * (WellFed(nview) ? 1 : 0);

            nview.ClaimOwnership();
            nview.GetZDO().Set(LastMilkKey, ZNet.instance.GetTime().Ticks);
            Give(user, amount);
        }

        private const string LastPetKey = "DF_lastPet";

        /// <summary>The cow was petted (E) within the last PetMemorySeconds.</summary>
        internal static bool Petted(ZNetView nview)
        {
            long last = nview.GetZDO().GetLong(LastPetKey, 0L);
            return last != 0L && (ZNet.instance.GetTime() - new DateTime(last)).TotalSeconds <= Cows.Settings.PetMemorySeconds;
        }

        /// <summary>The cow ate within the first WellFedPercent of the time a meal lasts.</summary>
        internal static bool WellFed(ZNetView nview)
        {
            long last = nview.GetZDO().GetLong(ZDOVars.s_tameLastFeeding, 0L);
            double limit = Cows.Settings.FedSeconds * Mathf.Clamp(Cows.Settings.WellFedPercent, 0f, 100f) / 100f;
            return last != 0L && (ZNet.instance.GetTime() - new DateTime(last)).TotalSeconds <= limit;
        }

        internal static void RememberPet(ZNetView nview)
        {
            if (nview != null && nview.IsValid())
            {
                nview.ClaimOwnership();
                nview.GetZDO().Set(LastPetKey, ZNet.instance.GetTime().Ticks);
            }
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

    /// <summary>Pressing the milk key while looking at a tame cow milks her.</summary>
    [HarmonyPatch(typeof(Player), "Update")]
    internal static class MilkKeyPress
    {
        [HarmonyPostfix]
        private static void Postfix(Player __instance)
        {
            if (__instance != Player.m_localPlayer || Cows.Settings == null || !ZInput.GetKeyDown(Cows.Settings.MilkKey)
                || !Milking.InputAllowed(__instance))
            {
                return;
            }

            Tameable cow = Milking.HoveredCow(__instance);
            if (cow != null)
            {
                Milking.TryMilk(cow, __instance);
            }
        }
    }

    /// <summary>Hover text of a tame cow: E follows or waits, the milk key milks, and whether she can be milked.</summary>
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
            __result = __result.Replace(loc.Localize("$hud_pet"), loc.Localize("$df_cow_follow"));
            string status = Milking.MilkReady(nview) ? "$df_cow_ready" : "$df_cow_done_short";
            if (Milking.Petted(nview))
            {
                status += ", $df_cow_happy";
            }
            __result += loc.Localize("\n[<color=yellow><b>" + Cows.Settings.MilkKey + "</b></color>] $df_milk_action <color=#bbbbbb>(" + status + ")</color>");
        }
    }

    /// <summary>Petting a tame cow (E) makes her content for a while, which gives a little more milk.</summary>
    [HarmonyPatch(typeof(Tameable), nameof(Tameable.Interact))]
    internal static class CowPetting
    {
        [HarmonyPostfix]
        private static void Postfix(Tameable __instance, bool hold, bool alt, bool __result)
        {
            if (!__result || hold || alt || Cows.Settings == null || __instance.GetComponent<DfCow>() == null)
            {
                return;
            }

            var character = __instance.GetComponent<Character>();
            if (character != null && character.IsTamed())
            {
                Milking.RememberPet(__instance.GetComponent<ZNetView>());
            }
        }
    }
}
