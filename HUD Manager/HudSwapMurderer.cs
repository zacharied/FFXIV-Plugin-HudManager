using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Dalamud.Plugin;

namespace HUD_Manager {
    public class HudSwapMurderer {
        private Plugin Plugin { get; }

        public HudSwapMurderer(Plugin plugin) {
            this.Plugin = plugin;

            this.Murder();
            Oppress();
        }

        /// <summary>
        /// Kill any existing HudSwap instance.
        /// </summary>
        private void Murder() {
            // get dalamud
            var dalamudField = this.Plugin.Interface.GetType().GetField("dalamud", BindingFlags.Instance | BindingFlags.NonPublic);
            var dalamud = (Dalamud.Dalamud?) dalamudField?.GetValue(this.Plugin.Interface);
            if (dalamud == null) {
                PluginLog.LogWarning("Could not kill HudSwap since Dalamud field was null");
                return;
            }

            // get the plugin manager
            var managerProp = dalamud.GetType().GetProperty("PluginManager", BindingFlags.Instance | BindingFlags.NonPublic);
            var manager = managerProp?.GetValue(dalamud);
            if (manager == null) {
                PluginLog.LogWarning("Could not kill HudSwap since PluginManager property was null");
                return;
            }

            // get the method to disable plugins
            var disablePluginMethod = manager.GetType().GetMethod("DisablePlugin", BindingFlags.Instance | BindingFlags.Public);
            if (disablePluginMethod == null) {
                PluginLog.LogWarning("Could not kill HudSwap since DisablePlugin method was null");
                return;
            }

            // get the list of plugins
            var pluginsField = manager.GetType().GetProperty("Plugins", BindingFlags.Instance | BindingFlags.Public);
            var plugins = (List<(IDalamudPlugin plugin, PluginDefinition def, DalamudPluginInterface PluginInterface, bool IsRaw)>?) pluginsField?.GetValue(manager);
            if (plugins == null) {
                PluginLog.LogWarning("Could not kill HudSwap since Plugins property was null");
                return;
            }

            var hudSwapDefs = plugins
                .Select(info => info.def)
                .Where(def => def.InternalName == "HudSwap")
                .ToArray();

            foreach (var def in hudSwapDefs) {
                disablePluginMethod.Invoke(manager, new object[] {def});
            }
        }

        /// <summary>
        /// Prevent HudSwap from ever rising again.
        /// </summary>
        private static void Oppress() {
            var hudSwapInstallPath = Path.Combine(new[] {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XIVLauncher",
                "installedPlugins",
                "HudSwap",
            });

            if (!Directory.Exists(hudSwapInstallPath)) {
                return;
            }

            foreach (var version in Directory.EnumerateDirectories(hudSwapInstallPath)) {
                var disabledPath = Path.Combine(new[] {
                    hudSwapInstallPath,
                    version,
                    ".disabled",
                });

                try {
                    File.Create(disabledPath).Dispose();
                } catch (IOException ex) {
                    PluginLog.LogWarning($"Failed to oppress HudSwap {version}:\n{ex}");
                }
            }
        }
    }
}
