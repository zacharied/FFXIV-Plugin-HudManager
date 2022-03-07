using Dalamud.Interface;
using HUD_Manager.Ui.Editor;
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
                return;
            }

            bool update = false;

            ImGui.SetNextWindowSize(ImGuiHelpers.ScaledVector2(530, 530), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(ImGuiHelpers.ScaledVector2(530, 530), new Vector2(int.MaxValue, int.MaxValue));

            if (!ImGui.Begin(this.Plugin.Name, ref this._settingsVisible)) {
                return;
            }

            if (ImGui.BeginTabBar("##hudmanager-tabs")) {
                if (!this.Plugin.Config.UnderstandsRisks) {
                    if (ImGui.BeginTabItem("About")) {
                        ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "Read this first");
                        ImGui.Separator();
                        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                        ImGui.TextUnformatted("HUD Manager will use the configured staging slot as its own slot to make changes to. This means the staging slot will be overwritten whenever any swap happens.");
                        ImGui.Spacing();
                        ImGui.TextUnformatted("Any HUD layout changes you make while HUD Manager is enabled may potentially be lost, no matter what slot. If you want to make changes to your HUD layout, TURN OFF HUD Manager first.");
                        ImGui.Spacing();
                        ImGui.TextUnformatted("When editing or making a new layout, to be completely safe, turn off swaps, set up your layout, import the layout into HUD Manager, then turn on swaps.");
                        ImGui.Spacing();
                        ImGui.TextUnformatted("If you are a new user, HUD Manager auto-imported your existing layouts on startup.");
                        ImGui.Spacing();
                        ImGui.TextUnformatted("Finally, HUD Manager is beta software. Back up your character data before using this plugin. You may lose some to all of your HUD layouts while testing this plugin.");
                        ImGui.Separator();
                        ImGui.TextUnformatted("If you have read all of the above and are okay with continuing, check the box below to enable HUD Manager. You only need to do this once.");
                        ImGui.PopTextWrapPos();
                        var understandsRisks = this.Plugin.Config.UnderstandsRisks;
                        if (ImGui.Checkbox("I understand", ref understandsRisks)) {
                            this.Plugin.Config.UnderstandsRisks = understandsRisks;
                            this.Plugin.Config.Save();
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                    ImGui.End();
                    return;
                }

                this.LayoutEditor.Draw();

                this.Swaps.Draw();

                this.Help.Draw(ref update);

#if DEBUG
                this.Debug.Draw();
#endif

                ImGui.EndTabBar();
            }

            if (update) {
                Plugin.Config.Save();
            }

            ImGui.End();
        }
    }
}
