using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

// TODO: Zone swaps?

namespace HudSwap {
    public class PluginUI {
        private readonly HudSwapPlugin plugin;
        private readonly DalamudPluginInterface pi;
        private readonly Statuses statuses;

        private bool _settingsVisible = false;
        public bool SettingsVisible { get => this._settingsVisible; set => this._settingsVisible = value; }

        public PluginUI(HudSwapPlugin plugin, DalamudPluginInterface pi) {
            this.plugin = plugin;
            this.pi = pi;
            this.statuses = new Statuses(this.plugin, this.pi);
        }

        public void ConfigUI(object sender, EventArgs args) {
            this.SettingsVisible = true;
        }

        private string importName = "";
        private string renameName = "";
        private Guid selectedLayout = Guid.Empty;

        private string jobFilter = "";

        private static bool configErrorOpen = true;
        public static void ConfigError() {
            if (ImGui.Begin("HudSwap error", ref configErrorOpen)) {
                ImGui.Text("Could not load HudSwap configuration.");
                ImGui.Spacing();
                ImGui.Text("If you are updating from a previous version, please\ndelete your configuration file and restart the game.");

                ImGui.End();
            }
        }

        public void DrawSettings() {
            if (!this.SettingsVisible) {
                return;
            }

            PlayerCharacter player = this.pi.ClientState.LocalPlayer;

            if (ImGui.Begin("HudSwap", ref this._settingsVisible, ImGuiWindowFlags.AlwaysAutoResize)) {
                if (ImGui.BeginTabBar("##hudswap-tabs")) {
                    if (!this.plugin.Config.UnderstandsRisks) {
                        if (ImGui.BeginTabItem("About")) {
                            ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "Read this first");
                            ImGui.Separator();
                            ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                            ImGui.Text("HudSwap will use the configured staging slot as its own slot to make changes to. This means the staging slot will be overwritten whenever any swap happens.");
                            ImGui.Spacing();
                            ImGui.Text("Any HUD layout changes you make while HudSwap is enabled may potentially be lost, no matter what slot. If you want to make changes to your HUD layout, TURN OFF HudSwap first.");
                            ImGui.Spacing();
                            ImGui.Text("When editing or making a new layout, to be completely safe, turn off swaps, set up your layout, import the layout into HudSwap, then turn on swaps.");
                            ImGui.Spacing();
                            ImGui.Text("If you are a new user, HudSwap auto-imported your existing layouts on startup.");
                            ImGui.Spacing();
                            ImGui.Text("Finally, HudSwap is beta software. Back up your character data before using this plugin. You may lose some to all of your HUD layouts while testing this plugin.");
                            ImGui.Separator();
                            ImGui.Text("If you have read all of the above and are okay with continuing, check the box below to enable HudSwap. You only need to do this once.");
                            ImGui.PopTextWrapPos();
                            bool understandsRisks = this.plugin.Config.UnderstandsRisks;
                            if (ImGui.Checkbox("I understand", ref understandsRisks)) {
                                this.plugin.Config.UnderstandsRisks = understandsRisks;
                                this.plugin.Config.Save();
                            }

                            ImGui.EndTabItem();
                        }

                        ImGui.EndTabBar();
                        ImGui.End();
                        return;
                    }

                    if (ImGui.BeginTabItem("Layouts")) {
                        ImGui.Text("Saved layouts");
                        if (this.plugin.Config.Layouts.Keys.Count == 0) {
                            ImGui.Text("None saved!");
                        } else {
                            if (ImGui.ListBoxHeader("##saved-layouts")) {
                                foreach (KeyValuePair<Guid, Tuple<string, byte[]>> entry in this.plugin.Config.Layouts) {
                                    if (ImGui.Selectable(entry.Value.Item1, this.selectedLayout == entry.Key)) {
                                        this.selectedLayout = entry.Key;
                                        this.renameName = entry.Value.Item1;
                                    }
                                }
                                ImGui.ListBoxFooter();
                            }

                            ImGui.Text("Copy onto slot...");
                            foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                                string buttonName = $"{(int)slot + 1}##copy";
                                if (ImGui.Button(buttonName) && this.selectedLayout != null) {
                                    byte[] layout = this.plugin.Config.Layouts[this.selectedLayout].Item2;
                                    this.plugin.Hud.WriteLayout(slot, layout);
                                }
                                ImGui.SameLine();
                            }

                            if (ImGui.Button("Delete") && this.selectedLayout != null) {
                                this.plugin.Config.Layouts.Remove(this.selectedLayout);
                                this.selectedLayout = Guid.Empty;
                                this.renameName = "";
                                this.plugin.Config.Save();
                            }
                            ImGui.SameLine();

                            if (ImGui.Button("Copy to clipboard") && this.selectedLayout != null) {
                                if (this.plugin.Config.Layouts.TryGetValue(this.selectedLayout, out Tuple<string, byte[]> layout)) {
                                    SharedLayout shared = new SharedLayout(layout.Item2);
                                    string json = JsonConvert.SerializeObject(shared);
                                    ImGui.SetClipboardText(json);
                                }
                            }

                            ImGui.InputText("##rename-input", ref this.renameName, 100);
                            ImGui.SameLine();
                            if (ImGui.Button("Rename") && this.renameName.Length != 0 && this.selectedLayout != null) {
                                Tuple<string, byte[]> entry = this.plugin.Config.Layouts[this.selectedLayout]; ;
                                this.plugin.Config.Layouts[this.selectedLayout] = new Tuple<string, byte[]>(this.renameName, entry.Item2);
                                this.plugin.Config.Save();
                            }
                        }

                        ImGui.Separator();

                        ImGui.Text("Import");

                        ImGui.InputText("Imported layout name", ref this.importName, 100);

                        foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                            string buttonName = $"{(int)slot + 1}##import";
                            if (ImGui.Button(buttonName) && this.importName.Length != 0) {
                                this.ImportSlot(slot, this.importName);
                                this.importName = "";
                            }
                            ImGui.SameLine();
                        }

                        if (ImGui.Button("Clipboard") && this.importName.Length != 0) {
                            SharedLayout shared = null;
                            try {
                                shared = (SharedLayout)JsonConvert.DeserializeObject(ImGui.GetClipboardText(), typeof(SharedLayout));
#pragma warning disable CA1031 // Do not catch general exception types
                            } catch (Exception) {
#pragma warning restore CA1031 // Do not catch general exception types
                            }
                            if (shared != null) {
                                byte[] layout = shared.Layout();
                                if (layout != null) {
                                    this.Import(layout, this.importName);
                                    this.importName = "";
                                }
                            }
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Swaps")) {
                        bool enabled = this.plugin.Config.SwapsEnabled;
                        if (ImGui.Checkbox("Enable swaps", ref enabled)) {
                            this.plugin.Config.SwapsEnabled = enabled;
                            this.plugin.Config.Save();
                        }
                        ImGui.Text("Note: Disable swaps when editing your HUD.");

                        ImGui.Spacing();
                        string staging = ((int)this.plugin.Config.StagingSlot + 1).ToString();
                        if (ImGui.BeginCombo("Staging slot", staging)) {
                            foreach (HudSlot slot in Enum.GetValues(typeof(HudSlot))) {
                                if (ImGui.Selectable(((int)slot + 1).ToString())) {
                                    this.plugin.Config.StagingSlot = slot;
                                    this.plugin.Config.Save();
                                }
                            }
                            ImGui.EndCombo();
                        }
                        ImGui.SameLine();
                        HelpMarker("The staging slot is the HUD layout slot that will be used as your HUD layout. All changes will be written to this slot when swaps are enabled.");

                        ImGui.Separator();

                        ImGui.Text("This is the default layout. If none of the below conditions are\nsatisfied, this layout will be enabled.");

                        if (ImGui.BeginCombo("##default-layout", this.LayoutNameOrDefault(this.plugin.Config.DefaultLayout))) {
                            foreach (KeyValuePair<Guid, Tuple<string, byte[]>> entry in this.plugin.Config.Layouts) {
                                if (ImGui.Selectable(entry.Value.Item1)) {
                                    this.plugin.Config.DefaultLayout = entry.Key;
                                    this.plugin.Config.Save();
                                }
                            }
                            ImGui.EndCombo();
                        }

                        ImGui.Spacing();
                        ImGui.Text("These settings are ordered from highest priority to lowest priority.\nHigher priorities overwrite lower priorities when enabled.");
                        ImGui.Spacing();

                        if (ImGui.CollapsingHeader("Status layouts", ImGuiTreeNodeFlags.DefaultOpen)) {
                            if (ImGui.BeginChild("##layout-selections", new Vector2(0, 125))) {
                                ImGui.Columns(2);

                                float maxSize = 0f;

                                foreach (Status status in Statuses.ORDER.Reverse()) {
                                    maxSize = Math.Max(maxSize, ImGui.CalcTextSize(status.Name()).X);

                                    this.plugin.Config.StatusLayouts.TryGetValue(status, out Guid layout);

                                    if (this.LayoutBox(status.Name(), layout, out Guid newLayout)) {
                                        this.plugin.Config.StatusLayouts[status] = newLayout;
                                        this.plugin.Config.Save();
                                        if (this.plugin.Config.SwapsEnabled) {
                                            this.statuses.SetHudLayout(player, true);
                                        }
                                    }
                                }

                                ImGui.SetColumnWidth(0, maxSize + ImGui.GetStyle().ItemSpacing.X * 2);

                                ImGui.Columns(1);
                                ImGui.EndChild();
                            }
                        }

                        if (ImGui.CollapsingHeader("Job layouts")) {
                            if (ImGui.InputText("Filter", ref this.jobFilter, 50, ImGuiInputTextFlags.AutoSelectAll)) {
                                this.jobFilter = this.jobFilter.ToLower();
                            }

                            if (ImGui.BeginChild("##job-layout-selections", new Vector2(0, 125))) {
                                ImGui.Columns(2);

                                float maxSize = 0f;

                                var acceptableJobs = this.pi.Data.GetExcelSheet<ClassJob>()
                                    .Where(job => job.NameEnglish != "Adventurer")
                                    .Where(job => this.jobFilter.Length == 0 || (job.NameEnglish.ToLower().Contains(this.jobFilter) || job.Abbreviation.ToLower().Contains(this.jobFilter)));

                                foreach (ClassJob job in acceptableJobs) {
                                    maxSize = Math.Max(maxSize, ImGui.CalcTextSize(job.NameEnglish).X);

                                    this.plugin.Config.JobLayouts.TryGetValue(job.Abbreviation, out Guid layout);

                                    if (this.LayoutBox(job.NameEnglish, layout, out Guid newLayout)) {
                                        this.plugin.Config.JobLayouts[job.Abbreviation] = newLayout;
                                        this.plugin.Config.Save();
                                        if (this.plugin.Config.SwapsEnabled) {
                                            this.statuses.SetHudLayout(player, true);
                                        }
                                    }
                                }

                                ImGui.SetColumnWidth(0, maxSize + ImGui.GetStyle().ItemSpacing.X * 2);

                                ImGui.Columns(1);
                                ImGui.EndChild();
                            }

                            bool combatOnlyJobs = this.plugin.Config.JobsCombatOnly;
                            if (ImGui.Checkbox("Jobs only in combat/weapon drawn", ref combatOnlyJobs)) {
                                this.plugin.Config.JobsCombatOnly = combatOnlyJobs;
                                this.plugin.Config.Save();
                                if (this.plugin.Config.SwapsEnabled) {
                                    this.statuses.SetHudLayout(player, true);
                                }
                            }
                            ImGui.SameLine();
                            HelpMarker("Selecting this will make the HUD layout change for a job only when in combat or when your weapon is drawn.");

                            bool highPriorityJobs = this.plugin.Config.HighPriorityJobs;
                            if (ImGui.Checkbox("Jobs take priority over status", ref highPriorityJobs)) {
                                this.plugin.Config.HighPriorityJobs = highPriorityJobs;
                                this.plugin.Config.Save();
                                if (this.plugin.Config.SwapsEnabled) {
                                    this.statuses.SetHudLayout(player, true);
                                }
                            }
                            ImGui.SameLine();
                            HelpMarker("Selecting this will make job layouts always apply when on that job. If this is unselected, job layouts will only apply if the default layout was going to be used (or only in combat if the above checkbox is selected).");
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }

                ImGui.End();
            }
        }

        private static void HelpMarker(string text) {
            ImGui.TextDisabled("(?)");
            if (ImGui.IsItemHovered()) {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20f);
                ImGui.TextUnformatted(text);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }
        }

        private string LayoutNameOrDefault(Guid key) {
            if (this.plugin.Config.Layouts.TryGetValue(key, out Tuple<string, byte[]> tuple)) {
                return tuple.Item1;
            } else {
                return "";
            }
        }

        public void Draw() {
            this.DrawSettings();

            if (!(this.plugin.Config.SwapsEnabled && this.plugin.Config.UnderstandsRisks)) {
                return;
            }

            PlayerCharacter player = this.pi.ClientState.LocalPlayer;
            if (player == null) {
                return;
            }

            if (this.statuses.Update(player)) {
                this.statuses.SetHudLayout(null);
            }
        }

        private bool LayoutBox(string name, Guid currentLayout, out Guid newLayout) {
            newLayout = Guid.Empty;
            bool updated = false;
            ImGui.Text(name);
            ImGui.NextColumn();
            if (ImGui.BeginCombo($"##{name}-layout", this.LayoutNameOrDefault(currentLayout))) {
                if (ImGui.Selectable("Not set")) {
                    updated = true;
                }
                ImGui.Separator();
                foreach (KeyValuePair<Guid, Tuple<string, byte[]>> entry in this.plugin.Config.Layouts) {
                    if (ImGui.Selectable(entry.Value.Item1)) {
                        updated = true;
                        newLayout = entry.Key;
                    }
                }
                ImGui.EndCombo();
            }
            ImGui.NextColumn();

            return updated;
        }

        public void ImportSlot(HudSlot slot, string name, bool save = true) {
            this.Import(this.plugin.Hud.ReadLayout(slot), name, save);
        }

        public void Import(byte[] layout, string name, bool save = true) {
            this.plugin.Config.Layouts[Guid.NewGuid()] = new Tuple<string, byte[]>(name, layout);
            if (save) {
                this.plugin.Config.Save();
            }
        }
    }
}
