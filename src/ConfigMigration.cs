using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace VerlerLeben
{
    /// <summary>
    /// A config file keeps the values of the version it was created with, so a changed default never reaches
    /// players who played an older version. When the config version is older, every value that still equals one
    /// of the old defaults is set to the new default. Values the player changed on purpose stay.
    /// </summary>
    internal static class ConfigMigration
    {
        /// <summary>Raise this when a default changes and add the old default to <see cref="OldDefaults"/>.</summary>
        public const int CurrentVersion = 2;

        private const string VersionKey = "ConfigVersion";

        // section, key, values that used to be the default in earlier versions
        private static readonly (string section, string key, string[] old)[] OldDefaults =
        {
            ("Doener", "Ingredients", new[]
            {
                "CookedLoxMeat:2,SpicePlains:1",
                "CookedLoxMeat:2,CookedWolfMeat:2,SpiceMistlands:1",
                "CookedLoxMeat:1,CookedWolfMeat:2,SpiceMountains:1",
                "CookedLoxMeat:1,CookedWolfMeat:1,Onion:1",
                "DF_Rindfleisch:1,CookedWolfMeat:2,SpiceMountains:1",
            }),
            ("Doener", "Amount", new[] { "2" }),
            ("Kaese", "Ingredients", new[] { "LoxMeat:1", "ChickenEgg:2", "Barley:2" }),
            ("Kaese", "Amount", new[] { "2" }),
            ("Teller", "Ingredients", new[]
            {
                "DF_Doener:4,DF_Kaese:3,Carrot:4",
                "DF_Doener:6,DF_Kaese:4,Carrot:5,Onion:3,MushroomJotunPuffs:2",
                "DF_Doener:4,DF_Kaese:3,Carrot:4,MushroomJotunPuffs:1,SpiceMountains:1",
            }),
            ("Teller", "Health", new[] { "80" }),
            ("Teller", "Stamina", new[] { "40" }),
            ("Teller", "HealthRegen", new[] { "6" }),
            ("Teller", "DurationSeconds", new[] { "3000" }),
            ("Pizza", "Ingredients", new[]
            {
                "BreadDough:3,DF_Doener:6,DF_Kaese:4,Raspberry:3",
                "BreadDough:4,DF_Doener:8,DF_Kaese:6,Cloudberry:4,MushroomMagecap:2,Onion:2",
                "BreadDough:3,DF_Doener:6,DF_Kaese:4,Onion:2,MushroomMagecap:1,SpiceMountains:1",
            }),
            ("Pizza", "Health", new[] { "65" }),
            ("Pizza", "Stamina", new[] { "65" }),
            ("Pizza", "HealthRegen", new[] { "4" }),
            ("Pizza", "DurationSeconds", new[] { "4200" }),
        };

        /// <summary>The config version stored in the file: Current for a new file, 0 for a file from before versions.</summary>
        public static int ReadStoredVersion(ConfigFile config)
        {
            if (!File.Exists(config.ConfigFilePath))
            {
                return CurrentVersion;
            }

            foreach (string line in File.ReadAllLines(config.ConfigFilePath))
            {
                string[] parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2 && parts[0].Trim() == VersionKey && int.TryParse(parts[1].Trim(), out int version))
                {
                    return version;
                }
            }

            return 0;
        }

        /// <summary>Call after all entries are bound.</summary>
        public static void Apply(ConfigFile config, int storedVersion, ManualLogSource log)
        {
            ConfigEntry<int> version = config.Bind("General", VersionKey, CurrentVersion,
                "Internal. When it is lower than the mod's version, old default values in this file are replaced by the new defaults.");
            if (storedVersion < CurrentVersion)
            {
                int changed = 0;
                foreach (var (section, key, old) in OldDefaults)
                {
                    if (!config.ContainsKey(new ConfigDefinition(section, key)))
                    {
                        continue;
                    }

                    ConfigEntryBase entry = config[section, key];
                    string current = Convert.ToString(entry.BoxedValue, CultureInfo.InvariantCulture);
                    if (Array.IndexOf(old, current) >= 0 && !Equals(entry.BoxedValue, entry.DefaultValue))
                    {
                        entry.BoxedValue = entry.DefaultValue;
                        changed++;
                    }
                }

                if (changed > 0)
                {
                    log.LogInfo($"Config updated: {changed} old default value(s) replaced by the new ones. Your own changes were kept.");
                }
            }

            version.Value = CurrentVersion;
            config.Save();
        }
    }
}
