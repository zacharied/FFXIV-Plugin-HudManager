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

        private static Config MigrateV2(JObject old) {
            foreach (var property in old["Layouts"].Children<JProperty>()) {
                if (property.Name == "$type") {
                    continue;
                }

                var layout = (JObject) property.Value;
                var elements = (JObject) layout["Elements"];

                foreach (var elementProp in elements.Children<JProperty>()) {
                    if (elementProp.Name == "$type") {
                        continue;
                    }

                    var element = (JObject) elementProp.Value;
                    var bytes = element["Unknown4"].ToObject<byte[]>();

                    var options = new byte[4];
                    Buffer.BlockCopy(bytes, 0, options, 0, 4);

                    var width = BitConverter.ToUInt16(bytes, 4);
                    var height = BitConverter.ToUInt16(bytes, 6);
                    var unknown4 = bytes[8];

                    element.Remove("Unknown4");
                    element["Options"] = options;
                    element["Width"] = width;
                    element["Height"] = height;
                    element["Unknown4"] = unknown4;
                }
            }

            old["Version"] = 3;

            return old.ToObject<Config>();
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

            string? text = null;
            if (File.Exists(managerPath)) {
                text = File.ReadAllText(managerPath);
                goto CheckVersion;
            }

            var hudSwapPath = PluginConfig("HudSwap");

            if (File.Exists(hudSwapPath)) {
                text = File.ReadAllText(hudSwapPath);
            }

            CheckVersion:
            if (text == null) {
                goto DefaultConfig;
            }

            var config = JsonConvert.DeserializeObject<JObject>(text);
            uint version = 1;
            if (config.TryGetValue("Version", out var token)) {
                version = token.Value<uint>();
            }

            switch (version) {
                case 1: {
                    var v1 = config.ToObject<ConfigV1>(new JsonSerializer {
                        TypeNameHandling = TypeNameHandling.None,
                    });

                    return Migrate(v1);
                }
                case 2: {
                    return MigrateV2(config);
                }
            }

            DefaultConfig:
            return plugin.Interface.GetPluginConfig() as Config ?? new Config();
        }
    }
}
