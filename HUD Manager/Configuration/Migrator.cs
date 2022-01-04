using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using Dalamud.Plugin;
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

                var positions = entry.Value.Positions.ToDictionary(
                    pos => pos.Key,
                    pos => new Window(
                        WindowComponent.X | WindowComponent.Y,
                        pos.Value
                    )
                );
                var saved = new SavedLayout(entry.Value.Name, layout, positions);
                config.Layouts[entry.Key] = saved;
            }

            return config;
        }

        private static void WithEachLayout(JObject old, Action<JObject> action) {
            foreach (var property in old["Layouts"].Children<JProperty>()) {
                if (property.Name == "$type") {
                    continue;
                }

                var layout = (JObject) property.Value;

                action(layout);
            }
        }

        private static void WithEachElement(JObject old, Action<JObject> action) {
            WithEachLayout(old, layout => {
                var elements = (JObject) layout["Elements"];

                foreach (var elementProp in elements.Children<JProperty>()) {
                    if (elementProp.Name == "$type") {
                        continue;
                    }

                    var element = (JObject) elementProp.Value;

                    action(element);
                }
            });
        }

        private static void MigrateV2(JObject old) {
            WithEachElement(old, element => {
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
            });

            old["Version"] = 3;
        }

        private static void MigrateV3(JObject old) {
            WithEachElement(old, element => {
                var measuredFrom = element["Unknown4"].ToObject<byte>();
                element.Remove("Unknown4");
                element["MeasuredFrom"] = measuredFrom;
            });

            old["Version"] = 4;
        }

        private static void MigrateV4(JObject old) {
            WithEachLayout(old, layout => {
                var oldPositions = (JObject) layout["Positions"];
                var windows = new Dictionary<string, Window>();

                foreach (var elementProp in oldPositions.Children<JProperty>()) {
                    if (elementProp.Name == "$type") {
                        continue;
                    }

                    var position = (JObject) elementProp.Value;
                    windows[elementProp.Name] = new Window(
                        WindowComponent.X | WindowComponent.Y,
                        new Vector2<short>(
                            position["X"].ToObject<short>(),
                            position["Y"].ToObject<short>()
                        )
                    );
                }

                layout["Windows"] = JObject.FromObject(windows);

                layout.Remove("Positions");
            });

            old.Remove("ImportPositions");
            old["Version"] = 5;
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

            int GetVersion() {
                if (config.TryGetValue("Version", out var token)) {
                    return token.Value<int>();
                }

                return -1;
            }

            var version = GetVersion();
            if (version < 1) {
                goto DefaultConfig;
            }

            // v1 is a special case - this is an old HudSwap config that we can interpret as a memory chunk
            // it does not need to go through migration steps after doing this, since it will be interpreted
            // as the layout would be in memory, so the existing code can deal with it normally
            if (version == 1) {
                var v1 = config.ToObject<ConfigV1>(new JsonSerializer {
                    TypeNameHandling = TypeNameHandling.None,
                });

                return Migrate(v1);
            }

            // otherwise, run migrations until done
            while (version < Config.LatestVersion) {
                switch (version) {
                    case 2:
                        MigrateV2(config);
                        break;
                    case 3:
                        MigrateV3(config);
                        break;
                    case 4:
                        MigrateV4(config);
                        break;
                    default:
                        PluginLog.Warning($"Tried to migrate from an unknown version: {version}");
                        goto DefaultConfig;
                }

                version = GetVersion();
            }

            if (version == Config.LatestVersion) {
                return config.ToObject<Config>();
            }

            DefaultConfig:
            return plugin.Interface.GetPluginConfig() as Config ?? new Config();
        }
    }
}
