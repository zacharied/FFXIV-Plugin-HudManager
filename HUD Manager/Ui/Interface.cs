using Dalamud.Interface;
using HUD_Manager.Ui.Editor;
using HUDManager.Ui;
using HUDManager.Ui.Editor;
using ImGuiNET;
using System;
using System.Numerics;

namespace HUD_Manager.Ui
{
    public class Interface : IDisposable
    {
        private Plugin Plugin { get; }

        private LayoutEditor LayoutEditor { get; }
        private Swaps Swaps { get; }
        private Help Help { get; }
        private FirstUseWarning FirstUseWarning { get; }
#if DEBUG
        private Debug Debug { get; }
#endif

        internal Guid SelectedLayout { get; set; } = Guid.Empty;

        private bool _settingsVisible;

        private bool SettingsVisible
        {
            get => this._settingsVisible;
            set => this._settingsVisible = value;
        }

        public Interface(Plugin plugin)
        {
            this.Plugin = plugin;

            this.LayoutEditor = new LayoutEditor(plugin, this);
            this.Swaps = new Swaps(plugin);
            this.Help = new Help(plugin);
            this.FirstUseWarning = new FirstUseWarning(plugin);
#if DEBUG
            this.Debug = new Debug(plugin);
#endif

            this.Plugin.Interface.UiBuilder.Draw += this.Draw;
            this.Plugin.Interface.UiBuilder.OpenConfigUi += this.OpenConfig;
        }

        public void Dispose()
        {
            this.Plugin.Interface.UiBuilder.OpenConfigUi -= this.OpenConfig;
            this.Plugin.Interface.UiBuilder.Draw -= this.Draw;
        }

        internal void OpenConfig()
        {
            this.SettingsVisible = true;
        }

        private void OpenConfig(object sender, EventArgs e)
        {
            this.OpenConfig();
        }

        private void Draw()
        {
            if (!this.SettingsVisible) {
                if (Plugin.Swapper is not null)
                    Plugin.Swapper.SwapsTemporarilyDisabled = false;
                return;
            }

            bool update = false;

            ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(530, 530), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(ImGuiHelpers.ScaledVector2(530, 530), new Vector2(int.MaxValue, int.MaxValue));

            if (!ImGui.Begin(this.Plugin.Name, ref this._settingsVisible)) {
                return;
            }

            if (ImGui.BeginTabBar("##hudmanager-tabs")) {
                if (!Plugin.Config.UnderstandsRisks) {
                    this.FirstUseWarning.Draw(ref update);
                    goto End;
                }

                this.LayoutEditor.Draw();

                this.Swaps.Draw();

                this.Help.Draw(ref update);

#if DEBUG
                this.Debug.Draw();
#endif

                End:
                ImGui.EndTabBar();
            }

            if (update) {
                Plugin.Config.Save();
            }

            ImGui.End();
        }
    }
}
