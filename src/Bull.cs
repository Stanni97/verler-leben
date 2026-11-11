using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace VerlerLeben
{
    /// <summary>
    /// Momo, the bull. A calf of the herd grows into him now and then. He is a Lox in strength, tame from the start, and
    /// leaves players alone unless one hits him first. Monsters that come near the herd are his business.
    /// </summary>
    internal class DfBull : DfFeeder
    {
        private Character _character;
        private ZNetView _nview;
        private float _tameTimer;

        internal Character AngryAt;
        internal float AngryUntil;

        protected override void Awake()
        {
            base.Awake();
            _character = GetComponent<Character>();
            _nview = GetComponent<ZNetView>();
            Bulls.Living.Add(_character);
        }

        private void Start()
        {
            if (_character != null)
            {
                _character.m_onDamaged += OnDamaged;
            }
        }

        private void OnDestroy()
        {
            Bulls.Living.Remove(_character);
            if (_character != null)
            {
                _character.m_onDamaged -= OnDamaged;
            }
        }

        protected override void Update()
        {
            base.Update();
            _tameTimer -= Time.deltaTime;
            if (_tameTimer > 0f)
            {
                return;
            }

            // Tame from birth: a tame animal never counts a player as an enemy.
            _tameTimer = 2f;
            if (_character != null && _nview != null && _nview.IsValid() && _nview.IsOwner() && !_character.IsTamed())
            {
                _character.SetTamed(true);
            }
        }

        internal bool IsAngryAt(Character other)
        {
            return other != null && other == AngryAt && Time.time < AngryUntil;
        }

        /// <summary>A player who hits Momo is his enemy for the next half minute, longer with every hit.</summary>
        private void OnDamaged(float damage, Character attacker)
        {
            if (attacker == null || !attacker.IsPlayer())
            {
                return;
            }

            AngryAt = attacker;
            AngryUntil = Time.time + 30f;
            var ai = GetComponent<MonsterAI>();
            if (ai != null)
            {
                Traverse t = Traverse.Create(ai);
                t.Field("m_targetCreature").SetValue(attacker);
                t.Field("m_targetStatic").SetValue(null);
                t.Method("SetAlerted", new object[] { true }).GetValue();
            }
        }
    }

    internal static class Bulls
    {
        public const string BullPrefab = "DF_Bulle";
        public const string BeefPrefab = "DF_Rindfleisch";

        internal static readonly HashSet<Character> Living = new HashSet<Character>();
        internal static CowSettings Settings;

        private static GameObject _prefab, _ragdoll;

        /// <summary>Builds the bull from the Lox. Returns the prefab, or null if it could not be made.</summary>
        public static GameObject Add(CowSettings settings, ManualLogSource log)
        {
            Settings = settings;
            var config = new CreatureConfig
            {
                Consumables = new[] { "Barley", "Flax" },
                DropConfigs = new[]
                {
                    new DropConfig { Item = BeefPrefab, MinAmount = 4, MaxAmount = 6, Chance = 100f, LevelMultiplier = false },
                    new DropConfig { Item = "LeatherScraps", MinAmount = 2, MaxAmount = 3, Chance = 100f, LevelMultiplier = false },
                },
            };
            var bull = new CustomCreature(BullPrefab, "Lox", config);
            if (bull.Prefab == null)
            {
                log.LogError("Lox not found, Momo is not added.");
                return null;
            }

            GameObject prefab = bull.Prefab;
            prefab.AddComponent<DfBull>();

            var humanoid = prefab.GetComponent<Humanoid>();
            humanoid.m_name = "$df_bulle";
            humanoid.m_group = "df_cow";          // the herd: cows and Momo never fight each other
            // His attacks are the Lox's (default items stay).

            var procreation = prefab.GetComponent<Procreation>();
            if (procreation != null)
            {
                Object.DestroyImmediate(procreation);   // Momo does not breed, the cows do
            }

            var ai = prefab.GetComponent<MonsterAI>();
            ai.m_attackPlayerObjects = false;
            ai.m_enableHuntPlayer = false;
            ai.m_consumeSearchRange = 15f;
            ai.m_consumeSearchInterval = 3f;
            ai.m_consumeRange = 4f;
            ai.m_pathAgentType = Pathfinding.AgentType.HorseSize;
            Configure(prefab, settings);
            Effects.RemoveHair(prefab, log);

            try
            {
                if (CowModel.Apply(prefab, momo: true))
                {
                    _ragdoll = Cows.ReplaceRagdoll(prefab, "lox_ragdoll", BullPrefab + "_ragdoll", settings.BullScale, log, momo: true);
                }
                else
                {
                    log.LogWarning("Momo's model could not be applied, he keeps the Lox look.");
                }
            }
            catch (System.Exception e)
            {
                log.LogWarning("Could not dress Momo, he keeps the Lox look: " + e.Message);
            }

            CreatureManager.Instance.AddCreature(bull);
            _prefab = prefab;
            log.LogInfo("Added " + BullPrefab + ".");
            return prefab;
        }

        public static void Apply(CowSettings settings)
        {
            Settings = settings;
            if (_prefab == null)
            {
                return;
            }

            Configure(_prefab, settings);
            if (_ragdoll != null)
            {
                _ragdoll.transform.localScale = Vector3.one * settings.BullScale;
            }

            foreach (Character character in Living)
            {
                if (character != null)
                {
                    Configure(character.gameObject, settings);
                }
            }
        }

        private static void Configure(GameObject bull, CowSettings settings)
        {
            bull.transform.localScale = Vector3.one * settings.BullScale;
            bull.GetComponent<Humanoid>().m_health = settings.BullHealth;

            float guard = Mathf.Max(10f, settings.BullGuardRange);
            var ai = bull.GetComponent<MonsterAI>();
            ai.m_viewRange = guard;
            ai.m_hearRange = guard * 0.75f;
            ai.m_alertRange = guard * 0.6f;

            var tameable = bull.GetComponent<Tameable>();
            if (tameable != null)
            {
                tameable.m_fedDuration = settings.FedSeconds;
                tameable.m_commandable = true;
            }
        }
    }

    /// <summary>
    /// Who counts as an enemy of Momo: monsters and a player who hit him, but not harmless animals (deer, boars, hares)
    /// and, while tame, no other player.
    /// </summary>
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.IsEnemy), typeof(Character), typeof(Character))]
    internal static class BullEnemies
    {
        [HarmonyPostfix]
        private static void Postfix(Character a, Character b, ref bool __result)
        {
            if (Bulls.Living.Count == 0 || a == null || b == null)
            {
                return;
            }

            Character other = b;
            DfBull bull = Bulls.Living.Contains(a) ? a.GetComponent<DfBull>() : null;
            if (bull == null && Bulls.Living.Contains(b))
            {
                bull = b.GetComponent<DfBull>();
                other = a;
            }

            if (bull == null)
            {
                return;
            }

            if (bull.IsAngryAt(other))
            {
                __result = true;
            }
            else if (__result && other.GetFaction() == Character.Faction.AnimalsVeg)
            {
                __result = false;
            }
        }
    }

    /// <summary>Everything Momo hits is hit harder by the damage multiplier of the config.</summary>
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    internal static class BullDamage
    {
        [HarmonyPrefix]
        private static void Prefix(HitData hit)
        {
            if (Bulls.Living.Count == 0 || Bulls.Settings == null)
            {
                return;
            }

            Character attacker = hit?.GetAttacker();
            if (attacker != null && Bulls.Living.Contains(attacker))
            {
                hit.ApplyModifier(Mathf.Max(0f, Bulls.Settings.BullDamage));
            }
        }
    }
}
