using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using HUDManager.Configuration;
using HUDManager.Structs.External;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using System;
using System.Linq;
using static Dalamud.Interface.FontAwesomeIcon;
using static HUDManager.Structs.External.CrossUpConfig;

namespace HUDManager.Ui.Editor.Tabs.External;

public sealed class CrossUp : IExternalElement
{
    private Plugin _plugin;

    private static float Scale => ImGuiHelpers.GlobalScale;

    public CrossUp(Plugin plugin)
    {
        _plugin = plugin;
    }

    public bool Available()
    {
        try {
            return _plugin.Interface.InstalledPlugins.Any(state => state is { Name: "CrossUp", IsLoaded: true }) && _plugin.Interface.GetIpcSubscriber<bool>("CrossUp.Available").InvokeFunc();
        }
        catch {
            return false;
        }
    }
    public void AddButtonToList(SavedLayout layout, ref bool update, bool avail)
    {
        var exists = layout.CrossUpConfig != null;
        var icon = exists ? TrashAlt : Plus;
        var label = $"uimanager-{(exists ? "remove" : "add")}-crossup";

        if (ImGuiExt.IconButton(icon, label)) {
            layout.CrossUpConfig = exists ? null : new();
            update = true;
        }

        ImGui.SameLine();
        if (avail) {
            ImGui.Text("CrossUp");
        } else {
            ImGui.TextDisabled("CrossUp (not installed)");
        }

        ImGui.SameLine();
        ImGuiExt.HelpMarker("CrossUp is a plugin that enables additional customization and features for the Cross Hotbar. If you have the CrossUp plugin installed, you can use HUD Manager layouts to manipulate your CrossUp settings.");
    }

    public void DrawControls(SavedLayout layout, ref bool update)
    {
        var config = layout.CrossUpConfig;
        if (config == null || !ImGui.CollapsingHeader("CrossUp Settings##xup")) { return; }

        using var tabBar = ImRaii.TabBar("CrossUpTabs", ImGuiTabBarFlags.FittingPolicyDefault);
        if (!tabBar) return;

        Tabs.BarLayout(ref config, ref update);
        Tabs.Color(ref config, ref update);
        Tabs.Exhb(ref config, ref update);

        using (ImRaii.PushFont(UiBuilder.IconFont)) {
            ImGui.SetNextItemWidth(40f * Scale);
            if (ImGui.TabItemButton($"{Cog.ToIconString()}{AngleDoubleRight.ToIconString()}##xup-open")) {
                OpenCrossUp(ref _plugin);
            }
        }
        ImGuiExt.HoverTooltip("Open CrossUp");

        using (ImRaii.PushFont(UiBuilder.IconFont)) {
            ImGui.SetNextItemWidth(23f * Scale);
            if (ImGui.TabItemButton($"{TrashAlt.ToIconString()}##xup-overlay-remove")) {
                layout.CrossUpConfig = null;
                update = true;
            }
        }
        ImGuiExt.HoverTooltip("Remove CrossUp settings from this layout");
    }

    private const ImGuiTableFlags TableFlags = ImGuiTableFlags.Borders | ImGuiTableFlags.PadOuterX | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg;
    private const ImGuiColorEditFlags PickerFlags = ImGuiColorEditFlags.PickerMask | ImGuiColorEditFlags.DisplayHex;

    private static class Tabs
    {
        public static void BarLayout(ref CrossUpConfig config, ref bool update)
        {
            using var tabItem = ImRaii.TabItem("Cross Hotbar Layout");
            if (!tabItem) return;

            using var table = ImRaii.Table("CrossUpTable", 2, TableFlags);
            if (!table) return;

            SetUpColumns();

            using (ImRaii.PushIndent(15f * Scale)) {
                Rows.SplitBar(ref config, ref update);
                Rows.Padlock(ref config, ref update);
                Rows.SetNum(ref config, ref update);
                Rows.ChangeSet(ref config, ref update);
                Rows.TriggerText(ref config, ref update);
                Rows.UnassignedSlots(ref config, ref update);
            }
        }
        public static void Color(ref CrossUpConfig config, ref bool update)
        {
            using var tabItem = ImRaii.TabItem("Colors");
            if (!tabItem) return;

            using var table = ImRaii.Table("CrossUpTable", 2, TableFlags);
            if (!table) return;

            SetUpColumns();

            using (ImRaii.PushIndent(15f * Scale)) {
                Rows.SelectBg(ref config, ref update);
                Rows.ButtonColor(ref config, ref update);
                Rows.TextAndBorder(ref config, ref update);
            }
        }

        public static void Exhb(ref CrossUpConfig config, ref bool update)
        {
            using var tabItem = ImRaii.TabItem("Expanded Hold Controls");
            if (!tabItem) return;

            using var table = ImRaii.Table("CrossUpTable", 2, TableFlags);
            if (!table) return;

            SetUpColumns();

            using (ImRaii.PushIndent(15f * Scale)) {
                Rows.SepEx(ref config, ref update);
                Rows.LRpos(ref config, ref update);
                if (!config.OnlyOneEx) Rows.RLpos(ref config, ref update);
            }
        }

        private static class Rows
        {
            public static void SplitBar(ref CrossUpConfig config, ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(ref config, CrossUpComponent.Split, ref update);

                ImGui.TableNextColumn();

                using (ImRaii.Group()) {
                    ImGui.TextColored(ImGuiColors.DalamudGrey3, "BAR SEPARATION");

                    ImGui.Text("Separate Left/Right");
                    ImGui.SameLine(160f * Scale);
                    if (ImGui.Checkbox("##xup-splitOn", ref config.Split.on)) update = true;

                    if (config.Split.on) {
                        ImGui.Text("Separation Distance");
                        ImGui.SameLine(160f * Scale);
                        using (ImRaii.PushId("xup-resetSplit")) {
                            if (ImGuiComponents.IconButton(UndoAlt)) {
                                config.Split.distance = 100;
                                update = true;
                            }
                        }

                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(90 * Scale);
                        if (ImGui.InputInt("##xup-splitDistance", ref config.Split.distance)) {
                            config.Split.distance = Math.Max(config.Split.distance, -142);
                            update = true;
                        }

                        ImGui.Text("Center Point");
                        ImGui.SameLine(160f * Scale);

                        using (ImRaii.PushId("xup-resetCenter")) {
                            if (ImGuiComponents.IconButton(UndoAlt)) {
                                config.Split.center = 0;
                                update = true;
                            }
                        }
                        ImGui.SameLine();
                        ImGui.SetNextItemWidth(90 * Scale);
                        if (ImGui.InputInt("##xup-centerPoint", ref config.Split.center)) update = true;

                        ImGuiComponents.HelpMarker("This will override your HUD setting for the bar's horizontal position.");
                    }
                }
            }
            public static void Padlock(ref CrossUpConfig config, ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(ref config, CrossUpComponent.Padlock, ref update);

                ImGui.TableNextColumn();
                using (ImRaii.Group()) {
                    ImGui.Text("Padlock Icon");

                    ImGui.SameLine(160f * Scale);
                    using (ImRaii.PushId("xup-resetPadlock")) {
                        if (ImGuiComponents.IconButton(UndoAlt)) {
                            config.Padlock = (0, 0, false);
                            update = true;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-padlockX", ref config.Padlock.x)) update = true;

                    WriteIcon(ArrowsAltH, true);

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-padlockY", ref config.Padlock.y)) update = true;

                    WriteIcon(ArrowsAltV, true);

                    ImGui.SameLine();
                    if (ImGui.Checkbox("Hide##xup-hidePadlock", ref config.Padlock.hide)) update = true;
                }
            }
            public static void SetNum(ref CrossUpConfig config, ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(ref config, CrossUpComponent.SetNum, ref update);

                ImGui.TableNextColumn();
                using (ImRaii.Group()) {
                    ImGui.Text("SET # Text");

                    ImGui.SameLine(160f * Scale);
                    using (ImRaii.PushId("xup-resetSetNumText")) {
                        if (ImGuiComponents.IconButton(UndoAlt)) {
                            config.SetNum = (0, 0, false);
                            update = true;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-setNumTextX", ref config.SetNum.x)) update = true;

                    WriteIcon(ArrowsAltH, true);

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-SetNumTextY", ref config.SetNum.y)) update = true;

                    WriteIcon(ArrowsAltV, true);

                    ImGui.SameLine();
                    if (ImGui.Checkbox("Hide##xup-hideSetNumText", ref config.SetNum.hide)) update = true;
                }
            }
            public static void ChangeSet(ref CrossUpConfig config, ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(ref config, CrossUpComponent.ChangeSet, ref update);

                ImGui.TableNextColumn();
                using (ImRaii.Group()) {
                    ImGui.Text("CHANGE SET Display");

                    ImGui.SameLine(160f * Scale);
                    using (ImRaii.PushId("xup-resetChangeSet")) {
                        if (ImGuiComponents.IconButton(UndoAlt)) {
                            config.ChangeSet = (0, 0);
                            update = true;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-changeSetX", ref config.ChangeSet.x)) update = true;

                    WriteIcon(ArrowsAltH, true);

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(90 * Scale);
                    if (ImGui.InputInt("##xup-changeSetY", ref config.ChangeSet.y)) update = true;

                    WriteIcon(ArrowsAltV, true);
                }
            }
            public static void TriggerText(ref CrossUpConfig config, ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(ref config, CrossUpComponent.TriggerText, ref update);

                ImGui.TableNextColumn();

                using (ImRaii.Group()) {
                    ImGui.Text("Hide L/R Trigger Text");
                    ImGui.SameLine(160f * Scale);

                    if (ImGui.Checkbox("##xup-hideTriggerText", ref config.HideTriggerText)) update = true;
                }
            }
            public static void UnassignedSlots(ref CrossUpConfig config, ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                DrawEnabledCheckbox(ref config, CrossUpComponent.Unassigned, ref update);

                ImGui.TableNextColumn();
                using (ImRaii.Group()) {
                    ImGui.Text("Hide Unassigned Slots");
                    ImGui.SameLine(160f * Scale);

                    if (ImGui.Checkbox("##xup-hideUnassigned", ref config.HideUnassigned)) update = true;
                }
            }
            public static void SelectBg(ref CrossUpConfig config, ref bool update)
            {
                var solid = config.SelectBG.style == 0;
                var frame = config.SelectBG.style == 1;

                var hidden = config.SelectBG.style == 2;
                var normal = config.SelectBG.blend == 0;
                var dodge = config.SelectBG.blend == 2;

                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(ref config, CrossUpComponent.SelectBG, ref update);

                ImGui.TableNextColumn();
                using (ImRaii.Group()) {
                    ImGui.TextColored(ImGuiColors.DalamudGrey3, "SELECTED BAR");

                    ImGui.Text("Backdrop Color");
                    ImGui.SameLine(160f * Scale);
                    using (ImRaii.PushId("xup-resetBG")) {
                        if (ImGuiComponents.IconButton(UndoAlt)) {
                            config.SelectBG.color = new(100f / 255f);
                            update = true;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.ColorEdit3("##xup-bgColor", ref config.SelectBG.color, PickerFlags))
                        update = true;

                    ImGui.Text("Backdrop Style");
                    ImGui.SameLine(160f * Scale);
                    if (ImGui.RadioButton("Solid##xup-bgStyle0", solid)) {
                        config.SelectBG.style = 0;
                        update = true;
                    }

                    ImGui.SameLine();
                    if (ImGui.RadioButton("Frame##xup-bgStyle1", frame)) {
                        config.SelectBG.style = 1;
                        update = true;
                    }

                    ImGui.SameLine();
                    if (ImGui.RadioButton("Hidden##xup-bgStyle2", hidden)) {
                        config.SelectBG.style = 2;
                        update = true;
                    }

                    ImGui.Text("Color Blending");
                    ImGui.SameLine(160f * Scale);
                    if (ImGui.RadioButton("Normal##xup-bgBlend0", normal)) {
                        config.SelectBG.blend = 0;
                        update = true;
                    }

                    ImGui.SameLine();
                    if (ImGui.RadioButton("Dodge##xup-bgBlend2", dodge)) {
                        config.SelectBG.blend = 2;
                        update = true;
                    }
                }
            }
            public static void ButtonColor(ref CrossUpConfig config, ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(ref config, CrossUpComponent.Buttons, ref update);
                ImGui.TableNextColumn();

                using (ImRaii.Group()) {
                    ImGui.TextColored(ImGuiColors.DalamudGrey3, "BUTTONS");

                    ImGui.Text("Button Glow");

                    ImGui.SameLine(160f * Scale);
                    using (ImRaii.PushId("xup-resetButtonGlow")) {
                        if (ImGuiComponents.IconButton(UndoAlt)) {
                            config.Buttons.glow = new(1f);
                            update = true;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.ColorEdit3("##xup-buttonGlow", ref config.Buttons.glow, PickerFlags)) update = true;

                    ImGui.Text("Button Pulse");

                    ImGui.SameLine(160f * Scale);
                    using (ImRaii.PushId("xup-resetButtonPulse")) {
                        if (ImGuiComponents.IconButton(UndoAlt)) {
                            config.Buttons.pulse = new(1f);
                            update = true;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.ColorEdit3("##xup-ButtonPulse", ref config.Buttons.pulse, PickerFlags)) update = true;
                }
            }
            public static void TextAndBorder(ref CrossUpConfig config, ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(ref config, CrossUpComponent.Text, ref update);

                ImGui.TableNextColumn();
                using (ImRaii.Group()) {
                    ImGui.TextColored(ImGuiColors.DalamudGrey3, "TEXT & BORDERS");

                    ImGui.Text("Text Color");

                    ImGui.SameLine(160f * Scale);
                    using (ImRaii.PushId("xup-resetTextColor")) {
                        if (ImGuiComponents.IconButton(UndoAlt)) {
                            config.Text.color = new(1f);
                            update = true;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.ColorEdit3("##xup-textColor", ref config.Text.color, PickerFlags)) update = true;

                    ImGui.Text("Text Glow");

                    ImGui.SameLine(160f * Scale);
                    using (ImRaii.PushId("xup-resetTextGlow")) {
                        if (ImGuiComponents.IconButton(UndoAlt)) {
                            config.Text.glow = new(0.616f, 0.514f, 0.357f);
                            update = true;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.ColorEdit3("##xup-textGlow", ref config.Text.glow, PickerFlags)) update = true;

                    ImGui.Text("Border Color");

                    ImGui.SameLine(160f * Scale);
                    using (ImRaii.PushId("xup-resetBorder")) {
                        if (ImGuiComponents.IconButton(UndoAlt)) {
                            config.Text.border = new(1f);
                            update = true;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.ColorEdit3("##xup-border", ref config.Text.border, PickerFlags)) update = true;
                }
            }
            public static void SepEx(ref CrossUpConfig config, ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();

                ImGui.Spacing();
                DrawEnabledCheckbox(ref config, CrossUpComponent.SepEx, ref update);

                ImGui.TableNextColumn();

                using (ImRaii.Group()) {
                    ImGui.TextColored(ImGuiColors.DalamudGrey3, "EXPANDED HOLD CONTROLS");
                    if (ImGui.Checkbox("Display Expanded Hold Controls Separately##xup-sepEx", ref config.SepEx)) update = true;

                    ImGuiComponents.HelpMarker("NOTE: This feature functions by borrowing the buttons from two of your standard mouse/keyboard hotbars. Please use CrossUp's plugin configuration to select which bars to borrow.\n\nThe hotbars you choose will not be overwritten, but they will be unavailable while the feature is active.");

                    if (ImGui.RadioButton("Show Only One Bar##xup-onlyone", config.OnlyOneEx)) {
                        config.OnlyOneEx = true;
                        update = true;
                    }

                    if (ImGui.RadioButton("Show Both##xup-showBoth", !config.OnlyOneEx)) {
                        config.OnlyOneEx = false;
                        update = true;
                    }
                }
            }
            public static void LRpos(ref CrossUpConfig config, ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                DrawEnabledCheckbox(ref config, CrossUpComponent.LRpos, ref update);

                ImGui.TableNextColumn();

                using (ImRaii.Group()) {
                    ImGui.Text($"{(config.OnlyOneEx ? "" : "L→R ")}Bar Position");
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker("Position is relative to the center of the Cross Hotbar.\n\nDefault: (-214, -88), which matches the Left WXHB's default location.");

                    ImGui.SameLine(160f * Scale);
                    using (ImRaii.PushId("xup-lrReset")) {
                        if (ImGuiComponents.IconButton(UndoAlt)) {
                            config.LRpos = (-214, -88);
                            update = true;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.InputInt("##xup-lrX", ref config.LRpos.x)) update = true;

                    WriteIcon(ArrowsAltH, true);

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.InputInt("##xup-lrY", ref config.LRpos.y)) update = true;

                    WriteIcon(ArrowsAltV, true);
                }
            }
            public static void RLpos(ref CrossUpConfig config, ref bool update)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                DrawEnabledCheckbox(ref config, CrossUpComponent.RLpos, ref update);

                ImGui.TableNextColumn();
                using (ImRaii.Group()) {
                    ImGui.Text("R→L Bar Position");
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker("Position is relative to the center of the Cross Hotbar.\n\nDefault: (214, -88), which matches the Right WXHB's default location.");

                    ImGui.SameLine(160f * Scale);
                    using (ImRaii.PushId("xup-rlReset")) {
                        if (ImGuiComponents.IconButton(UndoAlt)) {
                            config.RLpos = (214, -88);
                            update = true;
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.InputInt("##xup-rlX", ref config.RLpos.x)) update = true;

                    WriteIcon(ArrowsAltH, true);

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(100 * Scale);
                    if (ImGui.InputInt("##xup-rlY", ref config.RLpos.y)) update = true;

                    WriteIcon(ArrowsAltV, true);
                }
            }
        }
    }

    private static void SetUpColumns()
    {
        ImGui.TableSetupColumn("Enabled", ImGuiTableColumnFlags.WidthFixed, 50 * Scale);
        ImGui.TableSetupColumn("Setting", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableHeadersRow();
    }
    private static void WriteIcon(FontAwesomeIcon icon, bool sameLine = false)
    {
        if (sameLine) ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont)) {
            ImGui.Text($"{icon.ToIconString()}");
        }
    }
    private static void DrawEnabledCheckbox(ref CrossUpConfig config, CrossUpComponent component, ref bool update)
    {
        var enabled = config[component];
        if (!ImGui.Checkbox($"###xup-{component}-enabled", ref enabled)) return;
        config[component] = enabled;
        update = true;
    }
}
