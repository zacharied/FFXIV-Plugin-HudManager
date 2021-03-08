using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using HUD_Manager.Structs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HUD_Manager.Configuration {
    public static class Migrator {
        private static Config Migrate(ConfigV1 old) {
            var config = new Config {
                FirstRun = old.FirstRun,
                StagingSlot = old.StagingSlot,
                SwapsEnabled = old.SwapsEnabled,
                UnderstandsRisks = old.UnderstandsRisks,
            };

            foreach (var entry in old.Layouts2) {
                Layout layout;
                unsafe {
                    fixed (byte* ptr = entry.Value.Hud) {
                        layout = Marshal.PtrToStructure<Layout>((IntPtr) ptr);
                    }
                }

                var saved = new SavedLayout(entry.Value.Name, layout, entry.Value.Positions);
                config.Layouts[entry.Key] = saved;
            }

            return config;
        }

        private static string PluginConfig(string? pluginName = null) {
            pluginName ??= Assembly.GetAssembly(typeof(Plugin)).GetName().Name;
            return Path.Combine(new[] {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XIVLauncher",
                "pluginConfigs",
                $"{pluginName}.json",
            });
        }

        public static Config LoadConfig(Plugin plugin) {
            var managerPath = PluginConfig();

            if (File.Exists(managerPath)) {
                goto DefaultConfig;
            }

            var hudSwapPath = PluginConfig("HudSwap");

            if (File.Exists(hudSwapPath)) {
                var oldText = File.ReadAllText(hudSwapPath);
                var config = JsonConvert.DeserializeObject<JObject>(oldText);
                uint version = 1;
                if (config.TryGetValue("Version", out var token)) {
                    version = token.Value<uint>();
                }

                if (version == 1) {
                    var v1 = config.ToObject<ConfigV1>(new JsonSerializer {
                        TypeNameHandling = TypeNameHandling.None,
                    });

                    return Migrate(v1);
                }
            }

            DefaultConfig:
            return plugin.Interface.GetPluginConfig() as Config ?? new Config();
        }
    }
}
